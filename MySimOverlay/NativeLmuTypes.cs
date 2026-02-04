using System.Runtime.InteropServices;

namespace MySimOverlay
{
    // -------------------------------------------------------------------------
    // 1. The Core Physics Data (TelemInfoV01)
    // This matches the standard rFactor2/LMU internal telemetry struct.
    // -------------------------------------------------------------------------
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TelemInfoV01
    {
        public int mID;                    // Slot ID
        public double mDeltaTime;          // Time passed since last frame
        public double mElapsedTime;        // Time since session started
        public int mLapNumber;
        public double mLapStartET;
        public double mVehicleName;        // Pointer (don't touch in C#)
        public double mTrackName;          // Pointer (don't touch)

        public double mPos_x; public double mPos_y; public double mPos_z; // Position
        public double mLocalVel_x; public double mLocalVel_y; public double mLocalVel_z; // Velocity
        public double mLocalAccel_x; public double mLocalAccel_y; public double mLocalAccel_z; // Acceleration

        public double mOri_x; public double mOri_y; public double mOri_z; // Orientation (YPR)
        public double mLocalRot_x; public double mLocalRot_y; public double mLocalRot_z; // Rotation

        public double mUnfilteredThrottle; // <--- TARGET ACQUIRED
        public double mUnfilteredBrake;    // <--- TARGET ACQUIRED
        public double mUnfilteredClutch;
        public double mSteering;

        // There is much more data after this (tire temps, etc), 
        // but we stop here because we have what we need.
        // We add padding to ensure the array alignment works (approx 512 bytes per car usually)
        // BUT: Since we are accessing an array, we need the exact size.
        // For safety, we will marshal this carefully in the reader.
    }

    // -------------------------------------------------------------------------
    // 2. The Container: SharedMemoryTelemtryData
    // Matches: struct SharedMemoryTelemtryData { ... TelemInfoV01 telemInfo[104]; };
    // -------------------------------------------------------------------------
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SharedMemoryTelemtryData
    {
        public byte activeVehicles;      // How many cars on track
        public byte playerVehicleIdx;    // Which index [0-103] is YOU
        public byte playerHasVehicle;    // Is the player driving?

        // Padding for alignment (C++ bool/byte alignment can be tricky)
        public byte padding;

        // The huge array of cars. 
        // We cannot define [104] arrays easily in structs without 'unsafe' code.
        // Instead, we will calculate the offset manually in the Reader.
    }

    // -------------------------------------------------------------------------
    // 3. The Main Wrapper: SharedMemoryObjectOut
    // -------------------------------------------------------------------------
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SharedMemoryObjectOut
    {
        // SharedMemoryGeneric generic; (We skip this size)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)] // Approx size of Generic + Path
        public byte[] skippedHeader;

        // We jump straight to finding Telemetry by offset calculation
    }
}