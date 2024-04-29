using System.Runtime.InteropServices;

namespace Vhacd
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VhacdConvexHull
    {
        public VhacdVertex* Points;
        public uint NPoints;
        public VhacdTriangle* Triangles;
        public uint NTriangles;
    }
}
