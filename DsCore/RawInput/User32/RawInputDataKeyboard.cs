using System;
using System.Runtime.InteropServices;

namespace DsCore.RawInput
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputDataKeyboard
    {
        public RawInputHeader header;           // 64 bit header size is 24  32 bit the header size is 16
        public RawKeyboard data;                // Creating the rest in a struct allows the header size to align correctly for 32 or 64 bit
    }

    /// <summary>
    /// Contains information about the state of the keyboard
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RawKeyboard
    {
        /// <summary>
        /// The Scan code from the key depression.
        /// </summary>
        public ushort Makecode;
        /// <summary>
        /// Flags for scan code information.
        /// </summary>        
        public RawKeyboardFlags Flags;
        /// <summary>
        /// Reserved, must be Zero
        /// </summary>
        public ushort Reserved;
        /// <summary>
        /// Windows message compatible virtual-key code.
        /// </summary>
        public ushort VKey;
        /// <summary>
        /// The corresponding window message, for example WM_KEYDOWN, WM_SYSKEYDOWN.
        /// </summary>
        public uint Message;
        /// <summary>
        /// The device-specific additional information for the event.
        /// </summary>
        public uint ExtraInformation;

        public override string ToString()
        {
            return string.Format("Makecode: {0}, Makecode(hex) : {0:X}, Flags: {1}, Reserved: {2}, VKeyName: {3}, Message: {4}, ExtraInformation {5}",
                                                Makecode, Flags, Reserved, VKey, Message, ExtraInformation);
        }
    }

    [Flags]
    public enum RawKeyboardFlags : ushort
    {
        /// <summary>
        /// The key is down.
        /// </summary>
        RI_KEY_MAKE,
        /// <summary>
        /// The key is up.
        /// </summary>
        RI_KEY_BREAK,
        /// <summary>
        /// The scan code has E0 prefix.
        /// </summary>
        RI_KEY_E0,
        /// <summary>
        /// The scan code has E1 prefix.
        /// </summary>
        RI_KEY_E1 = 4,
    }
}
