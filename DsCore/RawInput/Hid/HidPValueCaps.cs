using System.Runtime.InteropServices;

namespace DsCore.RawInput
{    
    /// <summary>
    /// The HIDP_VALUE_CAPS structure contains information that describes the capability of a set of HID control values 
    /// (either a single usage or a usage range).
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct HidPValueCaps
    {
        /// <summary>
        /// Specifies the usage page of the usage or usage range.
        /// </summary>
        [FieldOffset(0)]
        public ushort UsagePage;
        /// <summary>
        /// Specifies the report ID of the HID report that contains the usage or usage range.
        /// </summary>
        [FieldOffset(2), MarshalAs(UnmanagedType.U1)]
        public byte ReportID;
        /// <summary>
        /// Indicates, if TRUE, that the usage is member of a set of aliased usages.
        /// Otherwise, if IsAlias is FALSE, the value has only one usage.
        /// </summary>
        [FieldOffset(3), MarshalAs(UnmanagedType.U1)]
        public bool IsAlias;
        /// <summary>
        /// Contains the data fields (one or two bytes) associated with an input, output, or feature main item.
        /// </summary>
        [FieldOffset(4)]
        public ushort BitField;
        /// <summary>
        /// Specifies the index of the link collection in a top-level collection's link collection array 
        /// that contains the usage or usage range.
        /// If LinkCollection is zero, the usage or usage range is contained in the top-level collection.
        /// </summary>
        [FieldOffset(6)]
        public ushort LinkCollection;
        /// <summary>
        /// Specifies the usage of the link collection that contains the usage or usage range.
        /// If LinkCollection is zero, LinkUsage specifies the usage of the top-level collection.
        /// </summary>
        [FieldOffset(8)]
        public ushort LinkUsage;
        /// <summary>
        /// Specifies the usage page of the link collection that contains the usage or usage range.
        /// If LinkCollection is zero, LinkUsagePage specifies the usage page of the top-level collection.
        /// </summary>
        [FieldOffset(10)]
        public ushort LinkUsagePage;
        /// <summary>
        /// Specifies, if TRUE, that the structure describes a usage range.
        /// Otherwise, if IsRange is FALSE, the structure describes a single usage.
        /// </summary>
        [FieldOffset(12), MarshalAs(UnmanagedType.U1)]
        public bool IsRange;
        /// <summary>
        /// Specifies, if TRUE, that the usage or usage range has a set of string descriptors.
        /// Otherwise, if IsStringRange is FALSE, the usage or usage range has zero or one string descriptor.
        /// </summary>
        [FieldOffset(13), MarshalAs(UnmanagedType.U1)]
        public bool IsStringRange;
        /// <summary>
        /// Specifies, if TRUE, that the usage or usage range has a set of designators.
        /// Otherwise, if IsDesignatorRange is FALSE, the usage or usage range has zero or one designator.
        /// </summary>
        [FieldOffset(14), MarshalAs(UnmanagedType.U1)]
        public bool IsDesignatorRange;
        /// <summary>
        /// Specifies, if TRUE, that the usage or usage range provides absolute data.
        /// Otherwise, if IsAbsolute is FALSE, the value is the change in state from the previous value.
        /// </summary>
        [FieldOffset(15), MarshalAs(UnmanagedType.U1)]
        public bool IsAbsolute;
        /// <summary>
        /// Specifies, if TRUE, that the usage supports a NULL value, which indicates that the data is not valid and should be ignored.
        /// Otherwise, if HasNull is FALSE, the usage does not have a NULL value.
        /// </summary>
        [FieldOffset(16), MarshalAs(UnmanagedType.U1)]
        public bool HasNull;
        /// <summary>
        /// Reserved for internal system use.
        /// </summary>
        [FieldOffset(17), MarshalAs(UnmanagedType.U1)]
        public byte Reserved;
        /// <summary>
        /// Specifies the size, in bits, of a usage's data field in a report.
        /// If ReportCount is greater than one, each usage has a separate data field of this size.
        /// </summary>
        [FieldOffset(18)]
        public ushort BitSize;
        /// <summary>
        /// Specifies the number of usages that this structure describes.
        /// </summary>
        [FieldOffset(20)]
        public ushort ReportCount;
        /// <summary>
        // Flattened Array Reserved2 : Reserved for internal system use.
        /// </summary>
        [FieldOffset(22)]
        public ushort Reserved2a;
        [FieldOffset(24)]
        public ushort Reserved2b;
        [FieldOffset(26)]
        public ushort Reserved2c;
        [FieldOffset(28)]
        public ushort Reserved2d;
        [FieldOffset(30)]
        public ushort Reserved2e;
        /// <summary>
        /// Specifies the usage's exponent, as described by the USB HID standard.
        /// </summary>
        [FieldOffset(32)]
        public uint UnitsExp;
        /// <summary>
        /// Specifies the usage's units, as described by the USB HID Standard.
        /// </summary>
        [FieldOffset(36)]
        public uint Units;
        /// <summary>
        /// Specifies a usage's signed lower bound.
        /// </summary>
        [FieldOffset(40)]
        public int LogicalMin;
        /// <summary>
        /// Specifies a usage's signed upper bound.
        /// </summary>
        [FieldOffset(44)]
        public int LogicalMax;
        /// <summary>
        /// Specifies a usage's signed lower bound after scaling is applied to the logical minimum value.
        /// </summary>
        [FieldOffset(48)]
        public int PhysicalMin;
        /// <summary>
        /// Specifies a usage's signed ûpper bound after scaling is applied to the logical maximum value.
        /// </summary>
        [FieldOffset(52)]
        public int PhysicalMax;
        /// <summary>
        /// Range or NotRange UNION
        /// </summary>
        [FieldOffset(56)]
        public ValueCapsRange Range;
        [FieldOffset(56)]
        public ValuenCapsNotRange NotRange;

        public override string ToString()
        {
            if (IsRange)
            {
                return string.Format("IsAbsolute: {0}, IsRange: {1}, LogicallMin: 0x{2:X8}, LogicalMax: 0x{3:X8}, ReportCount: {4}, Range->UsageMin: 0x{5:X2}, Range->UsageMax: 0x{6:X2}",
                                        IsAbsolute, IsRange, LogicalMin, LogicalMax, ReportCount, Range.UsageMin, Range.UsageMax);
            }
            else
            {
                return string.Format("IsAbsolute: {0}, IsRange: {1}, LogicallMin: 0x{2:X8}, LogicalMax: 0x{3:X8}, ReportCount: {4}, NotRange->Usage: 0x{5:X2}",
                                        IsAbsolute, IsRange, LogicalMin, LogicalMax, ReportCount, NotRange.Usage);
            }
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct ValueCapsRange
    {
        /// <summary>
        /// Indicates the inclusive lower bound of usage range whose inclusive upper bound is specified by Range.UsageMax.
        /// </summary>
        public ushort UsageMin;
        /// <summary>
        /// Indicates the inclusive upper bound of a usage range whose inclusive lower bound is indicated by Range.UsageMin.
        /// </summary>
        public ushort UsageMax;
        /// <summary>
        /// Indicates the inclusive lower bound of a range of string descriptors (specified by string minimum and string maximum items)
        /// whose inclusive upper bound is indicated by Range.StringMax.
        /// </summary>
        public ushort StringMin;
        /// <summary>
        /// Indicates the inclusive upper bound of a range of string descriptors (specified by string minimum and string maximum items)  
        ///whose inclusive lower bound is indicated by Range.StringMin.
        /// </summary>
        public ushort StringMax;
        /// <summary>
        /// Indicates the inclusive lower bound of a range of  (specified by designator minimum and designator maximum items)  
        ///whose inclusive lower bound is indicated by Range.DesignatorMax.
        /// </summary>
        public ushort DesignatorMin;
        /// <summary>
        /// Indicates the inclusive upper bound of a range of designators (specified by designator minimum and designator maximum items)
        /// whose inclusive lower bound is indicated by Range.DesignatorMin.
        /// </summary>
        public ushort DesignatorMax;
        /// <summary>
        /// Indicates the inclusive lower bound of a sequential range of data indices that correspond, one-to-one and in the same order, 
        /// to the usages specified by the usage range Range.UsageMin to Range.UsageMax.
        /// </summary>
        public ushort DataIndexMin;
        /// <summary>
        /// Indicates the inclusive upper bound of a sequential range of data indices that correspond, one-to-one and in the same order, 
        /// to the usages specified by the usage range Range.UsageMin to Range.UsageMax.
        /// </summary>
        public ushort DataIndexMax;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct ValuenCapsNotRange
    {
        /// <summary>
        /// Reserved for internal system use.
        /// </summary>
        public ushort Reserved1;
        /// <summary>
        /// Indicates a usage ID.
        /// </summary>
        public ushort Usage;
        /// <summary>
        /// Indicates a string descriptor ID for the usage specified by NotRange.Usage.
        /// </summary>
        public ushort StringIndex;
        /// <summary>
        /// Reserved for internal system use.
        /// </summary>
        public ushort Reserved2;
        /// <summary>
        /// Indicates a designator ID for the usage specified by NotRange.Usage.
        /// </summary>
        public ushort DesignatorIndex;
        /// <summary>
        /// Reserved for internal system use.
        /// </summary>
        public ushort Reserved3;
        /// <summary>
        /// Indicates the data index of the usage specified by NotRange.Usage.
        /// </summary>
        public ushort DataIndex;
        /// <summary>
        /// Reserved for internal system use.
        /// </summary>
        public ushort Reserved4;
    }
}
