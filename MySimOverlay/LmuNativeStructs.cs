using System.Runtime.InteropServices;

namespace MySimOverlay
{
    // ------------------------------------------------------------------------
    // HELPER TYPES
    // ------------------------------------------------------------------------
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LMUVect3
    {
        public double x, y, z;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LMUWheel
    {
        public double mSuspensionDeflection;
        public double mRideHeight;
        public double mSuspForce;
        public double mBrakeTemp;
        public double mBrakePressure;
        public double mRotation;
        public double mLateralPatchVel;
        public double mLongitudinalPatchVel;
        public double mLateralGroundVel;
        public double mLongitudinalGroundVel;
        public double mCamber;
        public double mLateralForce;
        public double mLongitudinalForce;
        public double mTireLoad;
        public double mGripFract;
        public double mPressure;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] mTemperature;
        public double mWear;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] mTerrainName;
        public byte mSurfaceType;
        public byte mFlat;
        public byte mDetached;
        public byte mStaticUndeflectedRadius;
        public double mVerticalTireDeflection;
        public double mWheelYLocation;
        public double mToe;
        public double mTireCarcassTemperature;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public double[] mTireInnerLayerTemperature;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)] public byte[] mExpansion;
    }

    // ------------------------------------------------------------------------
    // TELEMETRY (The Data You Want)
    // ------------------------------------------------------------------------
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    public struct LMUVehicleTelemetry
    {
        public int mID;
        public double mDeltaTime;
        public double mElapsedTime;
        public int mLapNumber;
        public double mLapStartET;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] mVehicleName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] mTrackName;
        public LMUVect3 mPos;
        public LMUVect3 mLocalVel;
        public LMUVect3 mLocalAccel;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public LMUVect3[] mOri;
        public LMUVect3 mLocalRot;
        public LMUVect3 mLocalRotAccel;
        public int mGear;
        public double mEngineRPM;
        public double mEngineWaterTemp;
        public double mEngineOilTemp;
        public double mClutchRPM;

        // --- INPUTS ---
        public double mUnfilteredThrottle;
        public double mUnfilteredBrake;
        public double mUnfilteredSteering;
        public double mUnfilteredClutch;

        // We stop mapping here because we have the pedals.
        // To be safe regarding memory size, we just map the rest as a blob 
        // roughly matching the Python struct size to avoid overflow errors.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 400)] public byte[] mRestOfStruct;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LMUTelemetryData
    {
        public byte activeVehicles;
        public byte playerVehicleIdx;
        public byte playerHasVehicle;

        // Match LMUConstants.MAX_MAPPED_VEHICLES = 104
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 104)]
        public LMUVehicleTelemetry[] telemInfo;
    }

    // ------------------------------------------------------------------------
    // SCORING & PATHS (Simplified for offset calculation)
    // ------------------------------------------------------------------------

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LMUScoringData
    {
        // Python: LMUScoringInfo (approx 512 bytes)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)] public byte[] scoringInfo;

        // Python: scoringStreamSize (12 bytes)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)] public byte[] scoringStreamSize;

        // Python: vehScoringInfo (104 * ~600 bytes) -> Approx 62400 bytes
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 62400)] public byte[] vehScoringInfo;

        // Python: scoringStream (65536 bytes)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 65536)] public byte[] scoringStream;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LMUPathData
    {
        // 5 paths * 260 bytes = 1300 bytes
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1300)] public byte[] rawPaths;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LMUGeneric
    {
        // Approx size from Python
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 336)] public byte[] rawGeneric;
    }

    // ------------------------------------------------------------------------
    // ROOT OBJECT
    // ------------------------------------------------------------------------
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LMUObjectOut
    {
        public LMUGeneric generic;
        public LMUPathData paths;
        public LMUScoringData scoring;
        public LMUTelemetryData telemetry;
    }
}