using System;
using System.Runtime.InteropServices;

namespace DsCore.RawInput
{
    /// <summary>
    /// Device infirmation for the raw input device.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputDevice
    {
        /// <summary>
        /// Top level collection Usage page for the raw input device.
        /// </summary>
        public HidUsagePage UsagePage;
        /// <summary>
        /// Top level collection Usage for the raw input device.
        /// </summary>
        public HidUsage Usage;
        /// <summary>
        /// Mode flag that specifies how to interpret the information provided by usUsagePage and usUsage.
        /// </summary>
        public RawInputDeviceFlags dwFlags;
        /// <summary>
        /// A handle to the target window. If NULL it follows the keyboard focus.
        /// </summary>
        public IntPtr hwndTarget;

        public override string ToString()
        {
            return string.Format("UsagePage/Page: {0}/{1}, flags: {2}, target: {3}", UsagePage, Usage, dwFlags, hwndTarget);
        }
    }
}
