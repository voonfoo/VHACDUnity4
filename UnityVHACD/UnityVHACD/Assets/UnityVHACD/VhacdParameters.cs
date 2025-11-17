using System;
using System.Runtime.InteropServices;

namespace Vhacd
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VhacdParameters
    {
        public IntPtr Callback;
        public IntPtr Logger;
        public IntPtr TaskRunner;
        public uint MaxConvexHulls;
        public uint MaxResolution;
        public double MinimumVolumePercentErrorAllowed;
        public uint MaxRecursionDepth;
        [MarshalAs(UnmanagedType.I1)]
        public bool ShrinkWrap;
        public FillMode FillMode;
        public uint MaxNumberOfVerticesPerConvexHull;
        [MarshalAs(UnmanagedType.I1)]
        public bool IsAsync;
        public uint MinEdgeLength;
        [MarshalAs(UnmanagedType.I1)]
        public bool FindBestPlane;

        public static VhacdParameters Default => new()
        {
            Callback = IntPtr.Zero,
            Logger = IntPtr.Zero,
            TaskRunner = IntPtr.Zero,
            MaxConvexHulls = 64,
            MaxResolution = 400000,
            MinimumVolumePercentErrorAllowed = 1,
            MaxRecursionDepth = 10,
            ShrinkWrap = true,
            FillMode = FillMode.FloodFill,
            MaxNumberOfVerticesPerConvexHull = 64,
            IsAsync = true,
            MinEdgeLength = 2,
            FindBestPlane = false
        };
    }

    public enum FillMode : int
    {
        FloodFill = 0,
        SurfaceOnly = 1,
        RaycastFill = 2
    }
}