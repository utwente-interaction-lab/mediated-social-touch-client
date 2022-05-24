using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using TouchController.Devices;

namespace TouchController
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        public Dictionary<string, Ellipse> VisibleTouch = new Dictionary<string, Ellipse>();

        public ITouchDevice activeTouchDevice;

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
        private SocketIO client;

        public TouchPoints RemoteTouchPoints { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            //  DispatcherTimer setup
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();

            //activeTouchDevice = new MidiTouchDevice(new int[] { 48, 50, 49, 51, 52, 54, 53 });
            activeTouchDevice = new bHapticsTouchDevice();

            activeTouchDevice.TryToConnect();

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
            if (activeTouchDevice?.IsConnected() != true) return;

            var touchPoints = GetTouchPointsFromCursor();

            if (RemoteTouchPoints != null)
            {
                touchPoints = RemoteTouchPoints;
            }else
            {
                DrawTouchPoints(touchPoints);
            }

            activeTouchDevice.UpdateTouchPoints(touchPoints);
        }

        private TouchPoints GetTouchPointsFromCursor()
        {
            Point p1 = GetMousePosition();
            Rect r = new Rect();

            // Get control position relative to window
            var p2 = VisualBox.TransformToAncestor(this).Transform(new Point(0, 0));

            // Add window position to get global control position
            r.X = p2.X + VisualBox.Margin.Left;
            r.Y = p2.Y + VisualBox.Margin.Top;

            // Set control width/height
            var size = GetElementPixelSize(VisualBox);
            r.Width = Math.Round(size.Width);
            r.Height = Math.Round(size.Height);

            if (!r.Contains(p1)) return null;

            return new TouchPoints()
            {
                Height = (float)r.Height,
                Width = (float)r.Width,
                Intensity = 100,
                Points = new List<TouchPoint> { new TouchPoint()
                    {
                       Id = "cursor", X = (float)p1.X, Y = (float)p1.Y
                    }
                }
            };
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            client = new SocketIO("http://62.68.75.165:3000/");
            Trace.WriteLine("WindowLoaded");


            client.On("touchpoints", response =>
            {
                Trace.WriteLine(response);
                var jsonString = response.ToString();
                TouchPoints touchPoints = JsonSerializer.Deserialize<TouchPoints[]>(jsonString)[0];

                DrawTouchPoints(touchPoints);
                RemoteTouchPoints = touchPoints;
            });

            client.OnConnected += async (s, _) =>
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

        private void DrawTouchPoints(TouchPoints touchPoints)
        {
            if (touchPoints == null) return;

            Application.Current.Dispatcher.Invoke(delegate
            {
                foreach (var circle in VisibleTouch)
                {
                    VisualGrid.Children.Remove(circle.Value);
                }
                VisibleTouch.Clear();

                foreach (var p in touchPoints.Points)
                {
                    var m = new Thickness(VisualBox.Margin.Left + (p.X / touchPoints.Width) * VisualBox.Width,
                        VisualBox.Margin.Top + (p.Y / touchPoints.Height) * VisualBox.Height, 0, 0);
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
        }


        // Making the window transparent while being on top

        const int WS_EX_TRANSPARENT = 0x00000020;
        const int GWL_EXSTYLE = (-20);
        public const uint WS_EX_LAYERED = 0x00080000;

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowExTransparent(hwnd);
        }
    }
}
