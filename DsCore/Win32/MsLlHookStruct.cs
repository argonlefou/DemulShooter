using System;
using System.Runtime.InteropServices;

namespace DsCore.Win32
{
    /// <summary>
    /// Contains information about a low-level mouse input event.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        /// <summary>
        /// The x- and y-coordinates of the cursor, in per-monitor-aware screen coordinates.
        /// </summary>
        public POINT pt;
        /// <summary>
        /// Complementary Data
        /// </summary>
        public int mouseData;
        /// <summary>
        /// The event-injected flags. 
        /// </summary>
        public int flags;
        /// <summary>
        /// The time stamp for this message.
        /// </summary>
        public int time;
        /// <summary>
        /// Additional information associated with the message.
        /// </summary>
        public UIntPtr dwExtraInfo;
    }
}
