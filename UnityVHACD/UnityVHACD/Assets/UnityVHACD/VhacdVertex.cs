using System.Runtime.InteropServices;

namespace Vhacd
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VhacdVertex
    {
        public double X;
        public double Y;
        public double Z;
    }
}
