
namespace DsCore.RawInput
{
    /// <summary>
    /// Top level collection Usage page for the raw input device.
    /// </summary>
    public enum HidUsagePage : ushort
    {
        /// <summary>Unknown usage page.</summary>
        UNDEFINED = 0x00,
        /// <summary>Generic desktop controls.</summary>
        GENERIC = 0x01,
        /// <summary>Simulation controls.</summary>
        SIMULATION = 0x02,
        /// <summary>Virtual reality controls.</summary>
        VR = 0x03,
        /// <summary>Sports controls.</summary>
        SPORT = 0x04,
        /// <summary>Games controls.</summary>
        GAME = 0x05,
        /// <summary>Keyboard controls.</summary>
        KEYBOARD = 0x07
    }
}
