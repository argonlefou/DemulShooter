using System;

namespace DsCore.RawInput
{
    /// <summary>
    /// Mode flag that sepcifies how to interpret the information provided by usUsagePage and usUsage.
    /// </summary>
    [Flags]
    public enum RawInputDeviceFlags
    {
        /// <summary>No flags.</summary>
        RIDEV_NONE = 0,
        /// <summary>If set, this removes the top level collection from the inclusion list. This tells the operating system to stop reading from a device which matches the top level collection.</summary>
        RIDEV_REMOVE = 0x00000001,
        /// <summary>If set, this specifies the top level collections to exclude when reading a complete usage page. This flag only affects a TLC whose usage page is already specified with PageOnly.</summary>
        EXCLUDE = 0x00000010,
        /// <summary>If set, this specifies all devices whose top level collection is from the specified UsagePage. Note that Usage must be zero. To exclude a particular top level collection, use Exclude.</summary>
        RIDEV_PAGEONLY = 0x00000020,
        /// <summary>If set, this prevents any devices specified by UsagePage or Usage from generating legacy messages. This is only for the mouse and keyboard.</summary>
        RIDEV_NOLEGACY = 0x00000030,
        /// <summary>If set, this enables the caller to receive the input even when the caller is not in the foreground. Note that WindowHandle must be specified.</summary>
        RIDEV_INPUTSINK = 0x00000100,
        /// <summary>If set, the mouse button click does not activate the other window.</summary>
        RIDEV_CAPTUREMOUSE = 0x00000200,
        /// <summary>If set, the application-defined keyboard device hotkeys are not handled. However, the system hotkeys; for example, ALT+TAB and CTRL+ALT+DEL, are still handled. By default, all keyboard hotkeys are handled. NoHotKeys can be specified even if NoLegacy is not specified and WindowHandle is NULL.</summary>
        RIDEV_NOHOTKEYS = 0x00000200,
        /// <summary>If set, application keys are handled.  NoLegacy must be specified.  Keyboard only.</summary>
        RIDEV_APPKEYS = 0x00000400,
        /// If set, this enables the caller to receive input in the background only if the foreground application
        /// does not process it. In other words, if the foreground application is not registered for raw input,
        /// then the background application that is registered will receive the input.
        /// </summary>
        RIDEV_EXINPUTSINK = 0x00001000,
        /// <summary>
        /// If set, this enables the caller to receive WM_INPUT_DEVICE_CHANGE notifications for device arrival and removal.
        /// </summary>
        RIDEV_DEVNOTIFY = 0x00002000
    }
}
