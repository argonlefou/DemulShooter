using System.Runtime.InteropServices;

namespace DsCore.RawInput
{   
    /// <summary>
    /// The HIDP_BUTTON_CAPS structure contains information about the capability of a 
    /// HID control button usage (or a set of buttons associated with a usage range).
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct HidPButtonCaps
    {
        /// <summary>
        /// Specifies the usage page for a usage or usage range.
        /// </summary>
        [FieldOffset(0)]
        public ushort UsagePage;
        /// <summary>
        /// Specifies the report ID of the HID report that contains the usage or usage range.
        /// </summary>
        [FieldOffset(2)]
        public byte ReportID;
        /// <summary>
        /// Indicates, if TRUE, that a button has a set of aliased usages. 
        /// Otherwise, if IsAlias is FALSE, the button has only one usage.
        /// </summary>
        [FieldOffset(3), MarshalAs(UnmanagedType.U1)]
        public bool IsAlias;
        /// <summary>
        /// Contains the data fields (one or two bytes) associated with an input, output, or feature main item.
        /// </summary>
        [FieldOffset(4)]
        public ushort BitField;
        /// <summary>
        /// Specifies the index of the link collection in a top-level collection's link collection array that contains the usage or usage range. 
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
        /// Specifies, if TRUE, that the button usage or usage range provides absolute data. 
        /// Otherwise, if IsAbsolute is FALSE, the button data is the change in state from the previous value.
        /// </summary>
        [FieldOffset(15), MarshalAs(UnmanagedType.U1)]
        public bool IsAbsolute;
        /// <summary>
        /// Reserved for internal system use.
        /// </summary>
        [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public uint[] Reserved;
        /// <summary>
        /// Range or NotRange are UNION
        /// </summary>
        [FieldOffset(16 + 10 * 4)]
        public ButtonCapsRange Range;
        [FieldOffset(16 + 10 * 4)]
        public ButtonCapsNotRange NotRange;

        public override string ToString()
        {
            if (IsRange)
            {
                return string.Format("UsagePage: {0}, LinkCollection: {1}, LinkUsage: {2}, LinkUsagePage: {3}, IsAbsolute: {4}, IsRange: {5}, Range->UsageMin: 0x{6:X2}, Range->UsageMax: 0x{7:X2}",
                                        UsagePage, LinkCollection, LinkUsage, LinkUsagePage, IsAbsolute, IsRange, Range.UsageMin, Range.UsageMax);
            }
            else
            {
                return string.Format("UsagePage: {0}, LinkCollection: {1}, LinkUsage: {2}, LinkUsagePage: {3}, IsAbsolute: {4}, IsRange: {5}, NotRange->Usage: 0x{6:X2}",
                                        UsagePage, LinkCollection, LinkUsage, LinkUsagePage, IsAbsolute, IsRange, NotRange.Usage);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ButtonCapsRange
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
        /// Indicates the inclusive lower bound of a range of string descriptors
        /// (specified by string minimum and string maximum items) whose inclusive upper bound is indicated by Range.StringMax.
        /// </summary>
        public ushort StringMin;
        /// <summary>
        /// Indicates the inclusive upper bound of a range of string descriptors
        /// (specified by string minimum and string maximum items) whose inclusive lower bound is indicated by Range.StringMin.
        /// </summary>
        public ushort StringMax;
        /// <summary>
        /// Indicates the inclusive lower bound of a range of designators 
        /// (specified by designator minimum and designator maximum items) whose inclusive lower bound is indicated by Range.DesignatorMax.
        /// </summary>
        public ushort DesignatorMin;
        /// <summary>
        /// Indicates the inclusive upper bound of a range of designators 
        /// (specified by designator minimum and designator maximum items) whose inclusive lower bound is indicated by Range.DesignatorMin.
        /// </summary>
        public ushort DesignatorMax;
        /// <summary>
        /// Indicates the inclusive lower bound of a sequential range of data indices that correspond, one-to-one and in the same order, 
        /// to the usages specified by the usage range Range.UsageMin to Range.UsageMax.
        /// </summary>
        public ushort DataIndexMin;
        /// <summary>
        ///Indicates the inclusive upper bound of a sequential range of data indices that correspond, one-to-one and in the same order, 
        ///to the usages specified by the usage range Range.UsageMin to Range.UsageMax.
        /// </summary>
        public ushort DataIndexMax;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ButtonCapsNotRange
    {
        /// <summary>
        /// Indicates a usage ID.        
        /// </summary>
        public ushort Usage;
        /// <summary>
        /// Reserved for internal system use.
        /// </summary>
        public ushort Reserved1;
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
