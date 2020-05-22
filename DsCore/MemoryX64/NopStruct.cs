using System;
using DsCore;
using System.Globalization;

namespace DsCore.MemoryX64
{
    /// <summary>
    /// Defines how many NOP to write at a given Memory offset
    /// </summary>
    public struct NopStruct
    {
        public UInt64 MemoryOffset;
        public UInt32 Length;

        public NopStruct(UInt64 Offset, UInt32 NopLength)
        {
            MemoryOffset = Offset;
            Length = NopLength;
        }

        public NopStruct(String OffsetAndNumber)
        {
            MemoryOffset = 0;
            Length = 0;
            if (OffsetAndNumber != null)
            {
                try
                {
                    Length = UInt32.Parse((OffsetAndNumber.Split('|'))[1]);
                    MemoryOffset = UInt64.Parse((OffsetAndNumber.Split('|'))[0].Substring(3).Trim(), NumberStyles.HexNumber);
                }
                catch
                {
                    Logger.WriteLog("Impossible to load NopStruct from following String : " + OffsetAndNumber);
                }
            }
        }
    }
}
