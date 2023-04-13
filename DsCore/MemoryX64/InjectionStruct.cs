using System;
using System.Globalization;

namespace DsCore.MemoryX64
{
    public class InjectionStruct
    {
        private UInt64 _InjectionOffset;
        private UInt32 _Length;

        public UInt64 InjectionOffset
        { get { return _InjectionOffset; } }

        public UInt64 InjectionReturnOffset
        { get { return _InjectionOffset + _Length; } }

        public UInt32 Length
        { get { return _Length; } }

        public int NeededNops
        { get { return (int)(_Length - 5); } }

        public InjectionStruct(UInt64 Offset, UInt32 Length)
        {
            _InjectionOffset = Offset;
            _Length = Length;
        }

        public InjectionStruct(String OffsetAndLength)
        {
            _InjectionOffset = 0;
            _Length = 0;
            if (OffsetAndLength != null)
            {
                try
                {
                    _Length = UInt32.Parse((OffsetAndLength.Split('|'))[1]);
                    _InjectionOffset = UInt64.Parse((OffsetAndLength.Split('|'))[0].Substring(2).Trim(), NumberStyles.HexNumber);
                }
                catch
                {
                    Logger.WriteLog("Impossible to load InjectionStruct from following String : " + OffsetAndLength);
                }
            }
        }
    }
}
