using System.Runtime.InteropServices;

namespace DsCore.XInput
{
    /// <summary>
    /// Specifies motor speed levels for the vibration function of a controller.
    /// The left motor is the low-frequency rumble motor. The right motor is the high-frequency rumble motor.
    /// The two motors are not the same, and they create different vibration effects.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XInputVibration
    {
        /// <summary>
        /// Speed of the left motor. Valid values are in the range 0 to 65,535. 
        /// Zero signifies no motor use
        /// 65 535 signifies 100 percent motor use.
        /// </summary>
        [MarshalAs(UnmanagedType.I2)]
        public ushort LeftMotorSpeed;
        /// <summary>
        /// Speed of the right motor. Valid values are in the range 0 to 65,535. 
        /// Zero signifies no motor use; 
        /// 65 535 signifies 100 percent motor use.
        /// </summary>
        [MarshalAs(UnmanagedType.I2)]
        public ushort RightMotorSpeed;
    }
}
