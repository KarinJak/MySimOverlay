using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.IO; // For saving the file

namespace MySimOverlay
{
    public partial class MainWindow : Window
    {
        private const int MAX_HISTORY = 300;
        private readonly List<double> _brakeHistory = new List<double>();
        private readonly List<double> _throttleHistory = new List<double>();
        private LmuNativeReader _reader;

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int VK_END = 0x23;

        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);

        private bool _isLocked = true;
        private bool _wasKeyPressed = false;
        private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "layout.txt");

        public MainWindow()
        {
            InitializeComponent();
            _reader = new LmuNativeReader();

            this.MouseLeftButtonDown += (s, e) => { if (!_isLocked) this.DragMove(); };

            // LOAD SAVED POSITION
            LoadLayout();

            CompositionTarget.Rendering += GameLoop;
        }

        private void LoadLayout()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    string[] parts = File.ReadAllText(_configPath).Split(',');
                    if (parts.Length == 4)
                    {
                        this.Left = double.Parse(parts[0]);
                        this.Top = double.Parse(parts[1]);
                        this.Width = double.Parse(parts[2]);
                        this.Height = double.Parse(parts[3]);
                    }
                }
                catch { /* Ignore errors if file is corrupted */ }
            }
        }

        private void SaveLayout()
        {
            try
            {
                // Save format: Left,Top,Width,Height
                string layout = $"{this.Left},{this.Top},{this.Width},{this.Height}";
                File.WriteAllText(_configPath, layout);
            }
            catch { /* Ignore errors */ }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            SetWindowExTransparent(_isLocked);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveLayout(); // Save one last time before closing
            Application.Current.Shutdown();
        }

        private void GameLoop(object sender, EventArgs e)
        {
            bool isKeyPressed = (GetAsyncKeyState(VK_END) & 0x8000) != 0;

            if (isKeyPressed && !_wasKeyPressed)
            {
                _isLocked = !_isLocked;
                SetWindowExTransparent(_isLocked);

                if (_isLocked)
                {
                    SaveLayout(); // AUTO-SAVE when you lock the window!
                    MainBorder.Background = new SolidColorBrush(Color.FromArgb(68, 0, 0, 0));
                    MainBorder.BorderThickness = new Thickness(0);
                    this.ResizeMode = ResizeMode.NoResize;
                    DebugText.Visibility = Visibility.Collapsed;
                    CloseButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    MainBorder.Background = new SolidColorBrush(Color.FromArgb(180, 20, 20, 20));
                    MainBorder.BorderBrush = Brushes.Yellow;
                    MainBorder.BorderThickness = new Thickness(2);
                    this.ResizeMode = ResizeMode.CanResizeWithGrip;
                    DebugText.Visibility = Visibility.Visible;
                    CloseButton.Visibility = Visibility.Visible;
                    DebugText.Text = "EDIT MODE\n1. Drag to move\n2. Resize edges\n3. Press END to Lock & Save";
                }
            }
            _wasKeyPressed = isKeyPressed;

            if (!_reader.IsConnected) _reader.Connect();
            var result = _reader.GetInputs();

            _brakeHistory.Add(result.Brake);
            _throttleHistory.Add(result.Throttle);

            if (_brakeHistory.Count > MAX_HISTORY) _brakeHistory.RemoveAt(0);
            if (_throttleHistory.Count > MAX_HISTORY) _throttleHistory.RemoveAt(0);

            DrawTrace(BrakeLine, _brakeHistory);
            DrawTrace(ThrottleLine, _throttleHistory);
        }

        private void SetWindowExTransparent(bool enable)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            if (enable)
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
            else
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
        }

        private void DrawTrace(System.Windows.Shapes.Polyline line, List<double> history)
        {
            if (line == null || GraphCanvas.ActualWidth == 0) return;
            line.Points.Clear();
            double width = GraphCanvas.ActualWidth;
            double height = GraphCanvas.ActualHeight;
            double step = width / MAX_HISTORY;
            var points = new PointCollection(history.Count);
            for (int i = 0; i < history.Count; i++)
            {
                points.Add(new Point(i * step, height - (Math.Clamp(history[i], 0, 1) * height)));
            }
            line.Points = points;
        }
    }
}