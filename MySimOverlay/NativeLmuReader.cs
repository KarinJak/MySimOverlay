using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace MySimOverlay
{
    public class LmuNativeReader : IDisposable
    {
        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;
        private const string MAP_NAME = "LMU_Data";

        // CACHE: Once we find the location, we save it here so we don't scan again.
        private int _cachedPlayerOffset = -1;
        private byte[] _buffer = new byte[350000]; // Re-use buffer to save RAM

        public bool IsConnected => _mmf != null;

        public bool Connect()
        {
            try
            {
                // Only try to open if not already open
                if (_mmf == null)
                {
                    _mmf = MemoryMappedFile.OpenExisting(MAP_NAME);
                    _accessor = _mmf.CreateViewAccessor();
                }
                return true;
            }
            catch { return false; }
        }

        public (double Brake, double Throttle, string Status) GetInputs()
        {
            if (_accessor == null) return (0, 0, "Waiting for LMU...");

            try
            {
                // FAST PATH: We already know where the data is
                if (_cachedPlayerOffset != -1)
                {
                    // Read directly from the cached address (Super Fast)
                    double throttle = _accessor.ReadDouble(_cachedPlayerOffset + 388);
                    double brake = _accessor.ReadDouble(_cachedPlayerOffset + 396);

                    // Validate: If data looks crazy (e.g. > 1.5), the memory moved. Reset!
                    if (throttle > 1.5 || brake > 1.5 || throttle < -0.1)
                    {
                        _cachedPlayerOffset = -1;
                        return (0, 0, "Lost signal, rescan...");
                    }

                    return (brake, throttle, "Active");
                }

                // SLOW PATH: We need to find the data (Runs once at start)
                _accessor.ReadArray(0, _buffer, 0, _buffer.Length);

                // Scan for the Telemetry Header (ActiveVehicles, PlayerIdx, HasVehicle)
                // We scan from 100KB to 300KB usually
                for (int i = 80000; i < _buffer.Length - 1000; i += 4)
                {
                    byte active = _buffer[i];
                    byte pIdx = _buffer[i + 1];
                    byte hasVeh = _buffer[i + 2];

                    // Heuristic: Active cars 1-104, Player Index valid, Has Vehicle = 1
                    if (active > 0 && active <= 104 && pIdx < active && hasVeh == 1)
                    {
                        // Calculate potential offset
                        // 904 is the approx size of one vehicle block in memory
                        int calculatedOffset = i + 4 + (pIdx * 904);

                        // Verify by checking if inputs are valid doubles (0.0 to 1.0)
                        double t = BitConverter.ToDouble(_buffer, calculatedOffset + 388);
                        double b = BitConverter.ToDouble(_buffer, calculatedOffset + 396);

                        if (t >= 0.0 && t <= 1.01 && b >= 0.0 && b <= 1.01)
                        {
                            // FOUND IT! Lock this address.
                            _cachedPlayerOffset = calculatedOffset;
                            return (b, t, "Locked");
                        }
                    }
                }

                return (0, 0, "Scanning...");
            }
            catch
            {
                _mmf = null; // Reset connection on error
                _cachedPlayerOffset = -1;
                return (0, 0, "Connection Error");
            }
        }

        public void Dispose()
        {
            _accessor?.Dispose();
            _mmf?.Dispose();
        }
    }
}