using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Messages;
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TouchController
{


    public class Touch
    {
        public Vector AreaSize;
        public List<TouchPoint> Points = new();
    }

    public class TouchPoint
    {
        public Vector Position;
        public Vector Velocity;
        public float Strength;
    }

    public class TouchJson
    {
        [JsonPropertyName("width")]
        public float Width { get; set; }
        [JsonPropertyName("height")]
        public float Height { get; set; }
        [JsonPropertyName("touchpoints")]
        public TouchPointJson Points { get; set; }
    }

    public class TouchPointJson
    {
        [JsonPropertyName("id")]
        public object Id { get; set; }
        [JsonPropertyName("x")]
        public float X { get; set; }
        [JsonPropertyName("y")]
        public float Y { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int WS_EX_TRANSPARENT = 0x00000020;
        const int GWL_EXSTYLE = (-20);
        public const uint WS_EX_LAYERED = 0x00080000;

        public bool[] Midi;
        public int MidiSize = 7;
        public int[] Mapping = new int[] { 48, 50, 49, 51, 52,  54, 53, };

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        public Dictionary<string, Ellipse> VisibleTouch = new Dictionary<string, Ellipse>();

        public static Point GetMousePosition()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public int X;
            public int Y;
        };

        private bool _isClickThrough = true;
        private SocketIO? client;
        private IMidiOutputDevice outputDevice;

        public MainWindow()
        {
            InitializeComponent();

            //  DispatcherTimer setup
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 5);
            dispatcherTimer.Start();

            Midi = new bool[MidiSize];


            foreach (var outputDeviceInfo in MidiDeviceManager.Default.OutputDevices)
            {
                Console.WriteLine($"Opening {outputDeviceInfo.Name}");

                outputDevice = outputDeviceInfo.CreateDevice();

                outputDevice.Open();
            }
        }
        public Size GetElementPixelSize(UIElement element)
        {
            Matrix transformToDevice;
            var source = PresentationSource.FromVisual(element);
            if (source != null)
                transformToDevice = source.CompositionTarget.TransformToDevice;
            else
                using (var s = new HwndSource(new HwndSourceParameters()))
                    transformToDevice = s.CompositionTarget.TransformToDevice;

            if (element.DesiredSize == new Size())
                element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            return (Size)transformToDevice.Transform((Vector)element.DesiredSize);
        }

        private async void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Point p1 = GetMousePosition();
            Point p2 = new Point();
            Rect r = new Rect();

            // Get control position relative to window
            p2 = VisualBox.TransformToAncestor(this).Transform(new Point(0, 0));

            // Add window position to get global control position
            r.X = p2.X + VisualBox.Margin.Left;
            r.Y = p2.Y + VisualBox.Margin.Top;

            // Set control width/height
            var size = GetElementPixelSize(VisualBox);
            r.Width = Math.Round(size.Width);
            r.Height = Math.Round(size.Height);



            if (r.Contains(p1) && client != null)
            {
                Trace.WriteLine("x" + p1);
                var dto = new Touch { AreaSize = new Vector((int)Math.Round(size.Width), (int)Math.Round(size.Height)) };
                await client.EmitAsync("touch", "source", dto);
                 
                var newMidi = new bool[MidiSize];
                var activeFloat = (p1.X - r.Left) / r.Width * MidiSize;
                var activeIndex = (int)Math.Round(activeFloat) - 1;
                Trace.WriteLine(activeIndex);
                if (activeIndex >= 0)
                    newMidi[activeIndex] = true;
                UpdateMidi(newMidi);
            }
            else
            {
                UpdateMidi(new bool[MidiSize]);
            }

        }

        private void UpdateMidi(bool[] vs)
        {
            var line = "";
            for (int i = 0; i < vs.Length; i++)
            {
                line += vs[i] + ",";
            }

 // Trace.WriteLine(line);

            for (int i = 0; i < vs.Length; i++)
            {
                if (!vs[i] && Midi[i])
                {
                    var message = new NoteOffMessage(RtMidi.Core.Enums.Channel.Channel1, (RtMidi.Core.Enums.Key)(Mapping[i]), 0);
                    outputDevice.Send(message);
                }
            }

            for (int i = 0; i < vs.Length; i++)
            {
                if (vs[i] && !Midi[i])
                {
                    var message = new NoteOnMessage(RtMidi.Core.Enums.Channel.Channel1, (RtMidi.Core.Enums.Key)(Mapping[i]), 10);
                    outputDevice.Send(message);
                }
            }

            Midi = vs;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            client = new SocketIO("http://168.119.29.140:3000/");
            Trace.WriteLine("WindowLoaded");


            client.On("touchpoints", response =>
            {
                // You can print the returned data first to decide what to do next.
                // output: ["hi client"]
                Trace.WriteLine(response);
                // The socket.io server code looks like this:
                // socket.emit('hi', 'hi client');
                var jsonString = response.ToString();
                TouchJson? jsonObject = JsonSerializer.Deserialize<TouchJson[]>(jsonString)[0];

                Application.Current.Dispatcher.Invoke((Action)delegate
                {

                    foreach (var circle in VisibleTouch)
                    {
                        VisualGrid.Children.Remove(circle.Value);
                    }
                    VisibleTouch.Clear();

                    if (jsonObject != null)
                        foreach (var p in new List<TouchPointJson>() { jsonObject.Points })
                        {

                            var m = new Thickness(VisualBox.Margin.Left + (p.X / jsonObject.Width) * VisualBox.Width ,
                                VisualBox.Margin.Top + (p.Y / jsonObject.Height) * VisualBox.Height, 0,0);
                            var circle = new Ellipse()
                            {
                                Width = 15,
                                Height = 15,
                                Margin = m,
                                Fill = new SolidColorBrush(Colors.White),
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Top
                            };
                            VisualGrid.Children.Add(circle);
                            VisibleTouch.Add(p.Id.ToString(), circle);
                        }
                });

            });

            client.OnConnected += async (sender, e) =>
            {
                Trace.WriteLine("Connected");
                // Set this client as output device
                await client.EmitAsync("device", "output");
            };
            try
            {
                await client.ConnectAsync();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
            // do some other stuff
        }


        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        public static void SetWindowExNotTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowExTransparent(hwnd);
        }
    }
}
