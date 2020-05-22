
namespace DsCore.RawInput
{
    /// <summary>
    /// Top level collection usage for the raw input device.
    /// </summary>
    public enum HidUsage : ushort
    {
        /// <summary>Unknown usage.</summary>
        Undefined = 0x00,
        /// <summary>Pointer</summary>
        Pointer = 0x01,
        /// <summary>Mouse</summary>
        Mouse = 0x02,
        /// <summary>Joystick</summary>
        Joystick = 0x04,
        /// <summary>Game Pad</summary>
        Gamepad = 0x05,
        /// <summary>Keyboard</summary>
        Keyboard = 0x06,
        /// <summary>Keypad</summary>
        Keypad = 0x07,
        /// <summary>Muilt-axis Controller</summary>
        SystemControl = 0x80,
        /// <summary>Tablet PC controls</summary>
        Tablet = 0x80,
        /// <summary>Consumer</summary>
        Consumer = 0x0C
    }
}
