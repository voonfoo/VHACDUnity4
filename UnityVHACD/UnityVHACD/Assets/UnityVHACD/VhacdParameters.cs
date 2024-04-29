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
        public int MaxConvexHulls;
        public int MaxResolution;
        public double MinimumVolumePercentErrorAllowed;
        public int MaxRecursionDepth;
        public bool ShrinkWrap;
        public FillMode FillMode;
        public int MaxNumberOfVerticesPerConvexHull;
        public bool IsAsync;
        public int MinEdgeLength;
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

    public enum FillMode
    {
        FloodFill,
        SurfaceOnly,
        RaycastFill
    }
}