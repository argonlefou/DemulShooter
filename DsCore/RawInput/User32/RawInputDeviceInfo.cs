using System.Runtime.InteropServices;

namespace DsCore.RawInput
{
    /// <summary>
    /// Defines the raw input data coming from any device.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RawInputDeviceInfo
    {
        /// <summary>
        /// The size, in bytes, of the RawInputDeviceInfo structure.
        /// </summary>
        [FieldOffset(0)]
        public int cbSize;
        /// <summary>
        /// The type of raw input data. this member can be one of the following values : RIM_TYPEHID, RIM_TYPEKEYBOARD or RIM_TYPEMOUSE
        /// </summary>
        [FieldOffset(4)]
        public RawInputDeviceType dwType;
        /// <summary>
        /// The HID device is defined by the structure RawInputMouseInfo.
        /// </summary>
        [FieldOffset(8)]
        public RawInputMouseInfo mouse;
        /// <summary>
        /// The HID device is defined by the structure RawInputKeyboardInfo.
        /// </summary>
        [FieldOffset(8)]
        public RawInputKeyboardInfo keyboard;
        /// <summary>
        /// The HID device is defined by the structure RawInputHidInfo.
        /// </summary>
        [FieldOffset(8)]
        public RawInputHidInfo hid;

        public override string ToString()
        {
            string str = string.Format("cbSize: {0}, dwType: {1}", cbSize, dwType); 
            switch (dwType)
            {
                case RawInputDeviceType.RIM_TYPEMOUSE:
                    {
                        str = str + string.Format(", dwId: {0}, dwNumberOfButtons: {1}, dwSampleRate: {2}, fHasHorizontalWheel: {3}", mouse.dwId, mouse.dwNumberOfButtons, mouse.dwSampleRate, mouse.fHasHorizontalWheel);
                    }break;
                case RawInputDeviceType.RIM_TYPEHID:
                    {
                        str = str + string.Format(", dwVendorId: 0x{0:X4}, dwProductId: 0x{1:X4}, dwVersionNumber: {2}, usUsagePage: {3}, usUsage: {4}", hid.dwProductId, hid.dwVendorId, hid.dwVersionNumber, hid.usUsagePage, hid.usUsage);
                    } break;
            }
            return str;
            
        }
    }

    /// <summary>
    /// Defines the raw input data coming from the specified mouse.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputMouseInfo
    {
        /// <summary>
        /// The identifier of the mouse device.
        /// </summary>
        public int dwId;
        /// <summary>
        /// The number of buttons for the mouse.
        /// </summary>
        public int dwNumberOfButtons;
        /// <summary>
        /// the number of data points per second. This information may not be applicable for every mouse device.
        /// </summary>
        public int dwSampleRate;
        /// <summary>
        /// TRUE if the mouse has a wheel for horizontal scrolling; otherwise FALSE.
        /// </summary>
        [MarshalAs(UnmanagedType.Bool)]
        public bool fHasHorizontalWheel;
    }

    /// <summary>
    /// Defines the raw input data coming from the specified keyboard.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputKeyboardInfo
    {
        /// <summary>
        /// The type of the keyboard.
        /// </summary>
        public int dwType;
        /// <summary>
        /// The subtype of the keyboard.
        /// </summary>
        public int dwSubType;
        /// <summary>
        /// The scan code mode.
        /// </summary>
        public int dwKeyboardMode;
        /// <summary>
        /// The number of function keys on the keyboard.
        /// </summary>
        public int dwNumberOfFunctionKeys;
        /// <summary>
        /// The number of LED indicators on the keyboard.
        /// </summary>
        public int dwNumberOfIndicators;
        /// <summary>
        /// The total number of keys on the keyboard.
        /// </summary>
        public int dwNumberOfKeysTotal;
    }

    /// <summary>
    /// Defines the raw input data coming from the specified Human Interface Device (HID).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputHidInfo
    {
        /// <summary>
        /// The vendor identifier for the HID.
        /// </summary>
        public int dwVendorId;
        /// <summary>
        /// The product idendifier for the HID.
        /// </summary>
        public int dwProductId;
        /// <summary>
        /// the version number for the HID.
        /// </summary>
        public int dwVersionNumber;
        /// <summary>
        /// The top-level collection Usage Page for the device.
        /// </summary>
        public ushort usUsagePage;
        /// <summary>
        /// The top-level collection Usage for the device.
        /// </summary>
        public ushort usUsage;
    }    
}
