using System.Runtime.InteropServices;

namespace DsCore.XInput
{
    /// <summary>
    /// Contains information on battery type and charge state.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct XInputBatteryInformation
    {
        /// <summary> 
        /// The type of battery. BatteryType will be one of the following values.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        [FieldOffset(0)]
        public BatteryTypes BatteryType;
        /// <summary> 
        /// The charge state of the battery. This value is only valid for wireless devices with a known battery type. 
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        [FieldOffset(1)]
        public BatteryLevel BatteryLevel;
    }

    /// <summary>
    /// Flags for battery status level
    /// </summary>
    public enum BatteryTypes : byte
    {
        /// <summary> This device is not connected </summary>
        BATTERY_TYPE_DISCONNECTED = 0x00,
        /// <summary> Wired device, no battery </summary>
        BATTERY_TYPE_WIRED = 0x01, 
        /// <summary> Alkaline battery source </summary>
        BATTERY_TYPE_ALKALINE = 0x02,
        /// <summary> Nickel Metal Hydride battery source </summary>
        BATTERY_TYPE_NIMH = 0x03, 
        /// <summary> Cannot determine the battery type </summary>
        BATTERY_TYPE_UNKNOWN = 0xFF,
    };

    /// <summary>
    /// These are only valid for wireless, connected devices, with known battery types
    /// The amount of use time remaining depends on the type of device.
    /// </summary>
    public enum BatteryLevel : byte
    {
        BATTERY_LEVEL_EMPTY = 0x00,
        BATTERY_LEVEL_LOW = 0x01,
        BATTERY_LEVEL_MEDIUM = 0x02,
        BATTERY_LEVEL_FULL = 0x03
    };
}
