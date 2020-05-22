using System;
using System.Runtime.InteropServices;

namespace DsCore.Win32
{
    /// <summary>
    /// Contains information about a low-level keyboard input event.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        /// <summary>
        /// A virtual-key code. The code must be a value in the range 1 to 254.
        /// </summary>
        public int vkCode;
        /// <summary>
        /// A hardware scan code for the key.
        /// </summary>
        public HardwareScanCode scanCode;
        /// <summary>
        /// The extended-key flag, event-injected flags, context code, and transition-state flag. 
        /// </summary>
        public int flags;
        /// <summary>
        /// The time stamp for this message, equivalent to what GetMessageTime would return for this message.
        /// </summary>
        public int time;
        /// <summary>
        /// Additional information associated with the message.
        /// </summary>
        public UIntPtr dwExtraInfo;
    }
}
