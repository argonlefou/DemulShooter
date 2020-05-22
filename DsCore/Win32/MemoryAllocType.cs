using System;

namespace DsCore.Win32
{
    [Flags]
    public enum MemoryAllocType
    {
        MEM_COMMIT = 0x1000,
        MEM_RESERVE = 0x2000,
        MEM_RESET = 0x8000,
        MEM_RESET_UNDO = 0x1000000,
    }
}
