using System;

namespace DsCore.Win32
{
    [Flags]
    public enum MemoryFreeType
    {
        MEM_COALESCE_PLACEHOLDERS = 0x00000001,
        MEM_PRESERVE_PLACEHOLDER = 0x00000002,
        MEM_DECOMMIT = 0x4000,
        MEM_RELEASE = 0x8000,
    }
}
