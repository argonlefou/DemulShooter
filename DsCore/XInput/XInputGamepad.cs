using System.Runtime.InteropServices;

namespace DsCore.XInput
{
    /// <summary>
    /// Describes the current state of the Xbox 360 Controller.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct XInputGamepad
    {
        /// <summary>
        /// Bitmask of the device digital buttons, as follows. A set bit indicates that the corresponding button is pressed.
        /// </summary>
        [MarshalAs(UnmanagedType.I2)]
        [FieldOffset(0)]
        public XinputButtonFlags wButtons;
        /// <summary>
        /// The current value of the left trigger analog control. The value is between 0 and 255.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        [FieldOffset(2)]
        public byte bLeftTrigger;
        /// <summary>
        /// The current value of the right trigger analog control. The value is between 0 and 255.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        [FieldOffset(3)]
        public byte bRightTrigger;
        /// <summary>
        /// Left thumbstick x-axis value. Each of the thumbstick axis members is a signed value between 
        /// -32768 and 32767 describing the position of the thumbstick. 
        /// A value of 0 is centered. 
        /// Negative values signify down or to the left. 
        /// Positive values signify up or to the right. 
        /// The constants XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE or XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE 
        /// can be used as a positive and negative value to filter a thumbstick input.
        /// </summary>
        [MarshalAs(UnmanagedType.I2)]
        [FieldOffset(4)]
        public short sThumbLX;
        /// <summary>
        /// Left thumbstick y-axis value. The value is between -32768 and 32767.
        /// </summary>
        [MarshalAs(UnmanagedType.I2)]
        [FieldOffset(6)]
        public short sThumbLY;
        /// <summary>
        /// Right thumbstick x-axis value. The value is between -32768 and 32767.
        /// </summary>
        [MarshalAs(UnmanagedType.I2)]
        [FieldOffset(8)]
        public short sThumbRX;
        /// <summary>
        /// Right thumbstick y-axis value. The value is between -32768 and 32767.
        /// </summary>
        [MarshalAs(UnmanagedType.I2)]
        [FieldOffset(10)]
        public short sThumbRY;
    }
}
