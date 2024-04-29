using System.Runtime.InteropServices;

namespace Vhacd
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VhacdTriangle
    {
        public uint Index0;
        public uint Index1;
        public uint Index2;
    }
}