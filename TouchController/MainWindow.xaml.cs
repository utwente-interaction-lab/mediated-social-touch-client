using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int WS_EX_TRANSPARENT = 0x00000020;
        const int GWL_EXSTYLE = (-20);
        public const uint WS_EX_LAYERED = 0x00080000;

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

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

        public MainWindow()
        {
            InitializeComponent();

            //  DispatcherTimer setup
            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            dispatcherTimer.Start();
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
            }
            else
            {
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            client = new SocketIO("http://localhost:11000/");
            Trace.WriteLine("WindowLoaded");
            client.On("hi", response =>
            {
                // You can print the returned data first to decide what to do next.
                // output: ["hi client"]
                Console.WriteLine(response);

                string text = response.GetValue<string>();

                // The socket.io server code looks like this:
                // socket.emit('hi', 'hi client');
            });

            client.On("touch", response =>
            {
                // You can print the returned data first to decide what to do next.
                // output: ["hi client"]
                Trace.WriteLine(response);

                string text = response.GetValue<string>();

                // The socket.io server code looks like this:
                // socket.emit('hi', 'hi client');
            });

            client.On("test", response =>
            {
                // You can print the returned data first to decide what to do next.
                // output: ["ok",{"id":1,"name":"tom"}]
                Console.WriteLine(response);

                // Get the first data in the response
                string text = response.GetValue<string>();
                // Get the second data in the response
                var dto = response.GetValue<Touch>(1);

                // The socket.io server code looks like this:
                // socket.emit('hi', 'ok', { id: 1, name: 'tom'});
            });

            client.OnConnected += async (sender, e) =>
            {
                Trace.WriteLine("Connected");
                // Emit a string
                await client.EmitAsync("hi", "socket.io");

                // Emit a string and an object

                //var dto = new Touch { AreaSize = new Vector((int)Math.Round(this.VisualBox.ActualWidth), (int)Math.Round(this.VisualBox.Height)) };
               // await client.EmitAsync("register", "source", dto);
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
