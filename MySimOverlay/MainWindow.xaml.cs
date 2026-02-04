using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MySimOverlay
{
    public partial class MainWindow : Window
    {
        // Settings
        private const int MAX_HISTORY = 100; // How long the tail is
        private readonly List<double> _brakeHistory = new List<double>();
        private DispatcherTimer _renderTimer;

        // Simulation variables (Remove these later when connecting to real game)
        private double _simAngle = 0;

        public MainWindow()
        {
            InitializeComponent();

            // Start the loop (60 FPS)
            _renderTimer = new DispatcherTimer();
            _renderTimer.Interval = TimeSpan.FromMilliseconds(16);
            _renderTimer.Tick += GameLoop;
            _renderTimer.Start();
        }

        private void GameLoop(object sender, EventArgs e)
        {
            // ---------------------------------------------------------
            // 1. GET DATA (Replace this block with real Game API later)
            // ---------------------------------------------------------

            // Simulating a driver pressing the brake on and off
            _simAngle += 0.1;
            double rawBrakeInput = (Math.Sin(_simAngle) + 1) / 2; // Value between 0.0 and 1.0

            // ---------------------------------------------------------
            // 2. PROCESS DATA
            // ---------------------------------------------------------

            // Add new data to history
            _brakeHistory.Add(rawBrakeInput);

            // Keep the list size fixed (remove old data)
            if (_brakeHistory.Count > MAX_HISTORY)
            {
                _brakeHistory.RemoveAt(0);
            }

            // ---------------------------------------------------------
            // 3. DRAW GRAPH
            // ---------------------------------------------------------
            DrawBrakeTrace();
        }

        private void DrawBrakeTrace()
        {
            // Clear the old points
            BrakeLine.Points.Clear();

            double canvasWidth = GraphCanvas.ActualWidth;
            double canvasHeight = GraphCanvas.ActualHeight;

            // Avoid crash if window is minimized or not loaded yet
            if (canvasWidth == 0 || canvasHeight == 0) return;

            // Loop through history and create points
            for (int i = 0; i < _brakeHistory.Count; i++)
            {
                // Calculate X: 
                // We want the newest point (index = Count-1) to be at the RIGHT side.
                // We want the oldest point (index = 0) to be at the LEFT side.
                // Step size is width / total points.
                double step = canvasWidth / MAX_HISTORY;
                double x = i * step;

                // Calculate Y:
                // WPF Coordinate system: 0 is Top, Height is Bottom.
                // So we must "flip" the value: Y = Height - (Value * Height)
                double value = _brakeHistory[i]; // 0.0 to 1.0
                double y = canvasHeight - (value * canvasHeight);

                BrakeLine.Points.Add(new Point(x, y));
            }
        }
    }
}