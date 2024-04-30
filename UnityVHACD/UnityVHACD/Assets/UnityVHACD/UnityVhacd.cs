using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Vhacd
{
    public class UnityVhacd
    {
#if UNITY_EDITOR
        private const string DLLName = "UnityVHACD";
#elif (UNITY_IOS && !UNITY_EDITOR)
    private const string DLL_NAME = "__Internal";
#endif
        
        public delegate void UserCallback(double overallProgress, double stageProgress, IntPtr stage, IntPtr operation);
        
        [DllImport(DLLName)]
        public static extern IntPtr CreateVHACD(IntPtr param);

        [DllImport(DLLName)]
        public static extern unsafe bool Compute(IntPtr iVhacd, float* points, uint pointCount, uint* triangles,
            uint trianglesCount,
            IntPtr param, IntPtr callback);

        [DllImport(DLLName)]
        public static extern uint GetNConvexHulls(IntPtr iVhacd);

        [DllImport(DLLName)]
        public static extern void GetConvexHull(out ConvexHullSafeHandle handle, IntPtr iVhacd, uint index,
            IntPtr convexHull);

        [DllImport(DLLName)]
        public static extern void ReleaseConvexHull(IntPtr handle);

        [DllImport(DLLName)]
        public static extern void ReleaseVHACD(IntPtr iVhacd);

        private IntPtr _vhacdPtr;

        public UnityVhacd()
        {
            
        }
    }

    public class ConvexHullSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public ConvexHullSafeHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            UnityVhacd.ReleaseConvexHull(handle);
            return true;
        }
    }
}