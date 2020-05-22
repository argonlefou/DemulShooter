using System;
using System.Runtime.InteropServices;

namespace DsCore.RawInput
{
    /// <summary>
    /// Contains the header information that is part of raw input data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputHeader
    {
        /// <summary>
        /// The type of raw input. It can be one of the following values : RIM_TYPEHID, RIM_TYPEKEYBOARD or RIM_TYPEMOUSE.
        /// </summary>
        public RawInputDeviceType dwType;
        /// <summary>
        /// The size, in bytes, of the entire input packet of data. This includes RAWINPUT plus possible extra input reports in the RAWHID variable lenght array.
        /// </summary>
        public int dwSize;
        /// <summary>
        /// A handle to the device generating the raw input data.
        /// </summary>
        public IntPtr hDevice;
        /// <summary>
        /// The value passed in the wParam parameter of the WM_INPUt message
        /// </summary>
        public IntPtr wParam;

        public override string ToString()
        {
           return string.Format("dwType : {0}, DeviceHandle : {1}, dwSize: {2}, WParam: {3}", dwType, hDevice, dwSize, wParam);
        }
    }
}
