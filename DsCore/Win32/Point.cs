using System.Runtime.InteropServices;

namespace DsCore.Win32
{
    /// <summary>
    /// The POINT structure defines the x- and y-coordinates of a point.
    /// https://docs.microsoft.com/en-us/windows/win32/api/windef/ns-windef-point
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}
