using System;
using System.Collections.Generic;
using System.Text;

namespace DsCore.Win32
{
    [Flags]
    public enum FileMapAccessType : uint
    {
        Copy = 0x01,
        Write = 0x02,
        Read = 0x04,
        AllAccess = 0x08,
        Execute = 0x20,
    }
}
