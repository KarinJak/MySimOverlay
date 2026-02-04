using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace MySimOverlay
{
    public partial class MainWindow : Window
    {
        // --- SETTINGS ---
        // Increase history slightly so the line doesn't disappear too fast on high-refresh screens
        private const int MAX_HISTORY = 300;

        // --- DATA ---
        private readonly List<double> _brakeHistory = new List<double>();
        private readonly List<double> _throttleHistory = new List<double>();

        // --- READER ---
        private LmuNativeReader _reader;

        // --- CLICK THROUGH ---
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public MainWindow()
        {
            InitializeComponent();

            _reader = new LmuNativeReader();

            // --- THE SMOOTH FIX ---
            // Instead of a Timer, we hook into the Vertical Sync (V-Sync)
            // This fires exactly when your monitor draws a new frame.
            CompositionTarget.Rendering += GameLoop;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        private void GameLoop(object sender, EventArgs e)
        {
            double brakeInput = 0;
            double throttleInput = 0;

            // 1. Connect
            if (!_reader.IsConnected) _reader.Connect();

            // 2. Get Data (No waiting, instant read)
            var result = _reader.GetInputs();

            // Only update graphs if we actually found data or are searching
            // (If result.Status is "Lock", we are good)
            brakeInput = result.Brake;
            throttleInput = result.Throttle;

            // 3. Update History
            _brakeHistory.Add(brakeInput);
            _throttleHistory.Add(throttleInput);

            // Keep buffer size constant
            if (_brakeHistory.Count > MAX_HISTORY) _brakeHistory.RemoveAt(0);
            if (_throttleHistory.Count > MAX_HISTORY) _throttleHistory.RemoveAt(0);

            // 4. Draw
            DrawTrace(BrakeLine, _brakeHistory);
            DrawTrace(ThrottleLine, _throttleHistory);
        }

        private void DrawTrace(System.Windows.Shapes.Polyline line, List<double> history)
        {
            if (line == null) return;
            line.Points.Clear();

            double canvasWidth = GraphCanvas.ActualWidth;
            double canvasHeight = GraphCanvas.ActualHeight;

            // Optimization: Don't draw if window is hidden
            if (canvasWidth == 0 || canvasHeight == 0) return;

            // Calculate step size based on window width
            double step = canvasWidth / MAX_HISTORY;

            // Create points collection
            var points = new PointCollection(history.Count);

            for (int i = 0; i < history.Count; i++)
            {
                double x = i * step;
                double safeValue = Math.Clamp(history[i], 0.0, 1.0);
                double y = canvasHeight - (safeValue * canvasHeight);

                points.Add(new Point(x, y));
            }

            // Assigning the whole collection at once is faster than .Add() in a loop
            line.Points = points;
        }
    }
}