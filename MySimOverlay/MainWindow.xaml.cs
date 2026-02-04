using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

// --- IMPORTANT: These enable JObject ---
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
// --------------------------------------

namespace MySimOverlay
{
    public partial class MainWindow : Window
    {
        private const int MAX_HISTORY = 100;

        // History lists for both pedals
        private readonly List<double> _brakeHistory = new List<double>();
        private readonly List<double> _throttleHistory = new List<double>();

        private DispatcherTimer _renderTimer;
        private static readonly HttpClient _client = new HttpClient();

        // Ensure this matches your game port (usually 6397 or 5397)
        private const string API_URL = "http://localhost:6397/rest/options/liveInputs";

        public MainWindow()
        {
            InitializeComponent();

            // 50ms = 20 FPS. Good balance for HTTP API.
            _renderTimer = new DispatcherTimer();
            _renderTimer.Interval = TimeSpan.FromMilliseconds(50);
            _renderTimer.Tick += GameLoop;
            _renderTimer.Start();
        }

        private async void GameLoop(object sender, EventArgs e)
        {
            double brakeInput = 0;
            double throttleInput = 0; // NEW: Variable for gas
            string debugMessage = "Waiting...";

            try
            {
                // 1. Get Data from Game
                string jsonString = await _client.GetStringAsync(API_URL);
                JObject root = JObject.Parse(jsonString);

                var liveInputs = root["liveInputs"];
                if (liveInputs != null)
                {
                    // --- STRATEGY: Try Wheel (DirectInput) first, then Game Physics (Processed) ---

                    var directInput = liveInputs["di"]?[0]?["raw inputs"];
                    var processed = liveInputs["processed inputs"];

                    // Check if Wheel (DirectInput) is sending data
                    // We check if either brake OR throttle is active
                    bool isWheelActive = false;
                    if (directInput != null)
                    {
                        double rawBrake = (double)directInput["brakes"];
                        double rawThrottle = (double)directInput["throttle"];
                        if (rawBrake > 0 || rawThrottle > 0) isWheelActive = true;
                    }

                    if (isWheelActive)
                    {
                        // Use Wheel Data
                        brakeInput = (double)directInput["brakes"];
                        throttleInput = (double)directInput["throttle"];
                        debugMessage = $"Src: Wheel | B: {brakeInput:F2} T: {throttleInput:F2}";
                    }
                    else if (processed != null)
                    {
                        // Fallback to Processed Data
                        if (processed["brakes"] != null)
                            brakeInput = (double)processed["brakes"];

                        if (processed["throttle"] != null)
                            throttleInput = (double)processed["throttle"];

                        debugMessage = $"Src: Proc | B: {brakeInput:F2} T: {throttleInput:F2}";
                    }
                }
            }
            catch (Exception ex)
            {
                debugMessage = "Error reading data (Is LMU running?)";
            }

            // 2. Update Debug Text (Ensure you have a TextBlock named DebugText in XAML, or comment this out)
            if (FindName("DebugText") is System.Windows.Controls.TextBlock txt)
            {
                txt.Text = debugMessage;
            }

            // 3. Update Histories
            _brakeHistory.Add(brakeInput);
            _throttleHistory.Add(throttleInput);

            // Keep lists at fixed size
            if (_brakeHistory.Count > MAX_HISTORY) _brakeHistory.RemoveAt(0);
            if (_throttleHistory.Count > MAX_HISTORY) _throttleHistory.RemoveAt(0);

            // 4. Draw Lines
            DrawTrace(BrakeLine, _brakeHistory);
            DrawTrace(ThrottleLine, _throttleHistory);
        }

        // Helper function to draw any line (Reused for both Gas and Brake)
        private void DrawTrace(System.Windows.Shapes.Polyline line, List<double> history)
        {
            // Safety check: if XAML element is missing, don't crash
            if (line == null) return;

            line.Points.Clear();
            double canvasWidth = GraphCanvas.ActualWidth;
            double canvasHeight = GraphCanvas.ActualHeight;

            if (canvasWidth == 0 || canvasHeight == 0) return;

            double step = canvasWidth / MAX_HISTORY;

            for (int i = 0; i < history.Count; i++)
            {
                double x = i * step;
                // Flip Y because 0 is Top
                double y = canvasHeight - (history[i] * canvasHeight);
                line.Points.Add(new Point(x, y));
            }
        }
    }
}