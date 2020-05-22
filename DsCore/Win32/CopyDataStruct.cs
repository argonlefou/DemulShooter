using System;
using System.Runtime.InteropServices;

namespace DsCore.Win32
{
    [StructLayout(LayoutKind.Sequential)]
    class CopyDataStruct
    {
        public uint dwData;
        public int cbData;
        public IntPtr lpData;
    }    
}
