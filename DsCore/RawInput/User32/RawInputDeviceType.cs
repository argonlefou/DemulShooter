
namespace DsCore.RawInput
{
    /// <summary>
    /// Type of RawInput device.
    /// </summary>
    public enum RawInputDeviceType
    {
        /// <summary>
        /// The device is a mouse.
        /// </summary>
        RIM_TYPEMOUSE,
        /// <summary>
        /// The device is a keyboard.
        /// </summary>
        RIM_TYPEKEYBOARD,
        /// <summary>
        /// The device is an HID that is not a keyboard and not a mouse.
        /// </summary>
        RIM_TYPEHID
    }
}
