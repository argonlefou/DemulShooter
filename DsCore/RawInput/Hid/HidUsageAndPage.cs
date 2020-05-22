using System;

namespace DsCore.RawInput
{
    /// <summary>
    /// UsagePages
    /// </summary>
    public enum HID_USAGE_PAGE : ushort
    {        
        GENERIC        = 0x01,
        SIMULATION     = 0x02,
        VR             = 0x03,
        SPORT          = 0x04,
        GAME           = 0x05,
        KEYBOARD       = 0x07,
        LED            = 0x08,
        BUTTON         = 0x09,
        ORDINAL        = 0x0A,
        TELEPHONY      = 0x0B,
        CONSUMER       = 0x0C,
        DIGITIZER      = 0x0D,
        UNICODE        = 0x10,
        ALPHANUMERIC   = 0x14
    }

    /// <summary>
    /// Usages Generic Desktop Page (0x01)
    /// </summary>
    public enum HID_USAGE_GENERIC : ushort
    {
        POINTER      = 0x01,
        MOUSE        = 0x02,
        JOYSTICK     = 0x04,
        GAMEPAD      = 0x05,
        KEYBOARD     = 0x06,
        KEYPAD       = 0x07,
        MULTIAXIS    = 0x08,

        X                                 = 0x30,
        Y                                 = 0x31,
        Z                                 = 0x32,
        RX                                = 0x33,
        RY                                = 0x34,
        RZ                                = 0x35,
        SLIDER                            = 0x36,
        DIAL                              = 0x37,
        WHEEL                             = 0x38,
        HATSWITCH                         = 0x39,
        COUNTED_BUFFER                    = 0x3A,
        BYTE_COUNT                        = 0x3B,
        MOTION_WAKEUP                     = 0x3C,
        START                             = 0x3D,
        SELECT                            = 0x3E,

        VX                                = 0x40,
        VY                                = 0x41,
        VZ                                = 0x42,
        VBRX                              = 0x43,
        VBRY                              = 0x44,
        VBRZ                              = 0x45,
        VNO                               = 0x46,
        FEATURE_NOTIFICATION              = 0x47,

        SYSTEM_CTL                        = 0x80,
        SYSCTL_POWER                      = 0x81,
        SYSCTL_SLEEP                      = 0x82,
        SYSCTL_WAKE                       = 0x83,
        SYSCTL_CONTEXT_MENU               = 0x84,
        SYSCTL_MAIN_MENU                  = 0x85,
        SYSCTL_APP_MENU                   = 0x86,
        SYSCTL_HELP_MENU                  = 0x87,
        SYSCTL_MENU_EXIT                  = 0x88,
        SYSCTL_MENU_SELECT                = 0x89,
        SYSCTL_MENU_RIGHT                 = 0x8A,
        SYSCTL_MENU_LEFT                  = 0x8B,
        SYSCTL_MENU_UP                    = 0x8C,
        SYSCTL_MENU_DOWN                  = 0x8D,
        SYSCTL_COLD_RESTART               = 0x8E,
        SYSCTL_WARM_RESTART               = 0x8F,
        SYSCTL_DPAD_UP                    = 0x90,
        SYSCTL_DPAD_DOWN                  = 0x91,
        SYSCTL_DPAD_RIGHT                 = 0x92,
        SYSCTL_DPAD_LEFT                  = 0x93,

        SYSCTL_DOCK                       = 0xA0,
        SYSCTL_UNDOCK                     = 0xA1,
        SYSCTL_SETUP                      = 0xA2,
        SYSCTL_BREAK                      = 0xA3,
        SYSCTL_DEBUGGER_BREAK             = 0xA4,
        SYSCTL_APP_BREAK                  = 0xA5,
        SYSCTL_APP_DEBUGGER_BREAK         = 0xA6,
        SYSCTL_SYSTEM_SPEAKER_MUTE        = 0xA7,
        SYSCTL_SYSTEM_HIBERNATE           = 0xA8,

        SYSCTL_DISPLAY_INVERT             = 0xB0,
        SYSCTL_DISPLAY_INTERNAL           = 0xB1,
        SYSCTL_DISPLAY_EXTERNAL           = 0xB2,
        SYSCTL_DISPLAY_BOTH               = 0xB3,
        SYSCTL_DISPLAY_DUAL               = 0xB4,
        SYSCTL_DISPLAY_TOGGLE_INT_EXT     = 0xB5,
        SYSCTL_DISPLAY_SWAP               = 0xB6,
        SYSCTL_DISPLAY_LCD_AUTOSCALE      = 0xB7
    }

    /// <summary>
    /// Usages from Simulation Controls Page (0x02)
    /// </summary>
    public enum HID_USAGE_SIMULATION : ushort
    {
        RUDDER = 0xBA,
        THROTTLE = 0xBB
    }

    //
    // Virtual Reality Controls Page (0x03)
    //


    //
    // Sport Controls Page (0x04)
    //


    //
    // Game Controls Page (0x05)
    //


    //
    // Keyboard/Keypad Page (0x07)
    //

    /// <summary>
    /// LED page (0x08)
    /// </summary>
    public enum HID_USAGE_LED : ushort
    {
        UNDEFINED              = 0x00,
        NUM_LOCK               = 0x01,
        CAPS_LOCK              = 0x02,
        SCROLL_LOCK            = 0x03,
        COMPOSE                = 0x04,
        KANA                   = 0x05,
        POWER                  = 0x06,
        SHIFT                  = 0x07,
        DO_NOT_DISTURB         = 0x08,
        MUTE                   = 0x09,
        TONE_ENABLE            = 0x0A,
        HIGH_CUT_FILTER        = 0x0B,
        LOW_CUT_FILTER         = 0x0C,
        EQUALIZER_ENABLE       = 0x0D,
        SOUND_FIELD_ON         = 0x0E,
        SURROUND_FIELD_ON      = 0x0F,
        REPEAT                 = 0x10,
        STEREO                 = 0x11,
        SAMPLING_RATE_DETECT   = 0x12,
        SPINNING               = 0x13,
        CAV                    = 0x14,
        CLV                    = 0x15,
        RECORDING_FORMAT_DET   = 0x16,
        OFF_HOOK               = 0x17,
        RING                   = 0x18,
        MESSAGE_WAITING        = 0x19,
        DATA_MODE              = 0x1A,
        BATTERY_OPERATION      = 0x1B,
        BATTERY_OK             = 0x1C,
        BATTERY_LOW            = 0x1D,
        SPEAKER                = 0x1E,
        HEAD_SET               = 0x1F,
        HOLD                   = 0x20,
        MICROPHONE             = 0x21,
        COVERAGE               = 0x22,
        NIGHT_MODE             = 0x23,
        SEND_CALLS             = 0x24,
        CALL_PICKUP            = 0x25,
        CONFERENCE             = 0x26,
        STAND_BY               = 0x27,
        CAMERA_ON              = 0x28,
        CAMERA_OFF             = 0x29,
        ON_LINE                = 0x2A,
        OFF_LINE               = 0x2B,
        BUSY                   = 0x2C,
        READY                  = 0x2D,
        PAPER_OUT              = 0x2E,
        PAPER_JAM              = 0x2F,
        REMOTE                 = 0x30,
        FORWARD                = 0x31,
        REVERSE                = 0x32,
        STOP                   = 0x33,
        REWIND                 = 0x34,
        FAST_FORWARD           = 0x35,
        PLAY                   = 0x36,
        PAUSE                  = 0x37,
        RECORD                 = 0x38,
        ERROR                  = 0x39,
        SELECTED_INDICATOR     = 0x3A,
        IN_USE_INDICATOR       = 0x3B,
        MULTI_MODE_INDICATOR   = 0x3C,
        INDICATOR_ON           = 0x3D,
        INDICATOR_FLASH        = 0x3E,
        INDICATOR_SLOW_BLINK   = 0x3F,
        INDICATOR_FAST_BLINK   = 0x40,
        INDICATOR_OFF          = 0x41,
        FLASH_ON_TIME          = 0x42,
        SLOW_BLINK_ON_TIME     = 0x43,
        SLOW_BLINK_OFF_TIME    = 0x44,
        FAST_BLINK_ON_TIME     = 0x45,
        FAST_BLINK_OFF_TIME    = 0x46,
        INDICATOR_COLOR        = 0x47,
        RED                    = 0x48,
        GREEN                  = 0x49,
        AMBER                  = 0x4A,
        GENERIC_INDICATOR      = 0x4B,
        SYSTEM_SUSPEND         = 0x4C, 
        EXTERNAL_POWER         = 0x4D
    }

    //
    //  Button Page (0x09)
    //
    //  There is no need to label these usages.
    //


    //
    //  Ordinal Page (0x0A)
    //
    //  There is no need to label these usages.
    //


    //
    //  Telephony Device Page (0x0B)
    //
}
