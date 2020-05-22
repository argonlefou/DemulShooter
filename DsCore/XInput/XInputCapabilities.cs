using System;
using System.Runtime.InteropServices;

namespace DsCore.XInput
{
    /// <summary>
    /// Describes the capabilities of a connected controller. The XInputGetCapabilities function returns XINPUT_CAPABILITIES.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct XInputCapabilities
    {
        /// <summary>
        /// Controller type.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        [FieldOffset(0)]
        XInputCapabilitiesType Type;
        /// <summary>
        /// Subtype of the game controller. See XINPUT and Controller Subtypes for a list of allowed subtypes.
        /// </summary>
        [MarshalAs(UnmanagedType.I1)]
        [FieldOffset(1)]
        public byte SubType;
        /// <summary>
        /// Features of the controller.
        /// </summary>
        [MarshalAs(UnmanagedType.I2)]
        [FieldOffset(2)]
        public CapabilityFlags Flags;
        /// <summary>
        /// XINPUT_GAMEPAD structure that describes available controller features and control resolutions.
        /// </summary>
        [FieldOffset(4)]
        public XInputGamepad Gamepad;
        /// <summary>
        /// XINPUT_VIBRATION structure that describes available vibration functionality and resolutions.
        /// </summary>
        [FieldOffset(16)]
        public XInputVibration Vibration;
    }

    /// <summary>
    /// Flags for XINPUT_CAPABILITIES
     public enum XInputCapabilitiesType : byte
    {
        /// <summary>
        /// The device is a game controller. 
        /// </summary>
        XINPUT_DEVTYPE_GAMEPAD = 0x01
    };
    public enum CapabilityFlags : short
    {
        /// <summary>
        /// Device has an integrated voice device.
        /// </summary>
        XINPUT_CAPS_VOICE_SUPPORTED = 0x0004,
        /// <summary>
        /// Device supports force feedback functionality. 
        /// Note that these force-feedback features beyond rumble are not currently supported through XINPUT on Windows.
        /// </summary>
        XINPUT_CAPS_FFB_SUPPORTED = 0x0001,
        /// <summary>
        /// Device is wireless.
        /// </summary>
        XINPUT_CAPS_WIRELESS = 0x0002,
        /// <summary>
        /// Device supports plug-in modules. 
        /// Note that plug-in modules like the text input device (TID) are not supported currently through XINPUT on Windows.
        /// </summary>
        XINPUT_CAPS_PMD_SUPPORTED = 0x0008,
        /// <summary>
        /// Device lacks menu navigation buttons (START, BACK, DPAD).
        /// </summary>
        XINPUT_CAPS_NO_NAVIGATION = 0x0010,
    };
   
}
