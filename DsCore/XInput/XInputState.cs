using System.Runtime.InteropServices;

namespace DsCore.XInput
{
    /// <summary>
    /// Represents the state of a controller.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct XInputState
    {
        /// <summary>
        /// State packet number. The packet number indicates whether there have been any changes in the state of the controller.
        /// If the dwPacketNumber member is the same in sequentially returned XINPUT_STATE structures, 
        /// the controller state has not changed.
        /// </summary>
        [FieldOffset(0)]
        public int dwPacketNumber;
        /// <summary>
        /// XINPUT_GAMEPAD structure containing the current state of an Xbox 360 Controller.
        /// </summary>
        [FieldOffset(4)]
        public XInputGamepad Gamepad;
    }
}
