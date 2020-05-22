using System;
using System.Runtime.InteropServices;

namespace DsCore.RawInput
{
    /// <summary>
    /// Contains information about a raw input device.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputDeviceList
    {
        /// <summary>
        /// A handle to the raw input device.
        /// </summary>
        public IntPtr hDevice;
        /// <summary>
        /// The type of device. this can be one of the following values : RIM_TYPEHID, RIM_TYPEKEYBOARD or RIM_TYPEMOUSE.
        /// </summary>
        public RawInputDeviceType dwType;
    }
}
