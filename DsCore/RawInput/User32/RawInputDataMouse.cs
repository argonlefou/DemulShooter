using System;
using System.Runtime.InteropServices;

namespace DsCore.RawInput
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RawInputDataMouse
    {
        public RawInputHeader header;           // 64 bit header size is 24  32 bit the header size is 16
        public RawMouse data;                    // Creating the rest in a struct allows the header size to align correctly for 32 or 64 bit
    }

    /// <summary>
    /// Contains information about the state of the mouse
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RawMouse
    {
        /// <summary>
        /// The mouse state.
        /// </summary>
        [FieldOffset(0)]
        public RawMouseFlags usFlags;
        /// <summary>
        /// The transition state of the mouse button.
        /// </summary>
        [FieldOffset(4)]
        public uint ulButtons;
        /// <summary>
        /// The transition state of the mouse button.
        /// </summary>
        [FieldOffset(4)]
        public RawMouseButtonFlags usButtonFlags;
        /// <summary>
        /// If usButtonFlag is RI_MOUSE_WHEEL, this member is a signed value that specifies the wheel delta.
        /// </summary>
        [FieldOffset(6)]
        public ushort usButtonData;
        /// <summary>
        /// The raw state of the mouse buttons.
        /// </summary>
        [FieldOffset(8)]
        public uint ulRawButtons;
        /// <summary>
        /// The motion in the X direction. This is signed relative motion or absolute motion, depending on the value of usFlags.
        /// </summary>
        [FieldOffset(12)]
        public int lLastX;
        /// <summary>
        /// The motion in the Y direction. This is signed relative motion or absolute motion, depending on the value of usFlags.
        /// </summary>
        [FieldOffset(16)]
        public int lLastY;
        /// <summary>
        /// The device-specific additional information for the event.
        /// </summary
        [FieldOffset(20)]
        public uint ulExtraInformation;

        public override string ToString()
        {
            return string.Format("X: {0}, Y: {1}, Flags: {2}, Buttons: {3}, Data: {4}", lLastX, lLastY, usFlags, usButtonFlags, usButtonData);
        }
    }

    [Flags]
    public enum RawMouseFlags : ushort
    {
        /// <summary>
        /// Mouse movement data is relative to the last mouse position.
        /// </summary>
        MoveRelative = 0,
        /// <summary>
        /// Mouse movement data is based on absolute position.
        /// </summary>
        MoveAbsolute = 1,
        /// <summary>
        /// Mouse coordinates are mapped to the virtual desktop (for a multiple monitor system).
        /// </summary>
        VirtualDesktop = 2,
        /// <summary>
        /// Mouse attributes changed; application needs to query the mouse attributes.
        /// </summary>
        AttributesChanged = 4,
    }

    [Flags]
    public enum RawMouseButtonFlags : ushort
    {
        /// <summary>
        /// Nothing.
        /// </summary>
        RI_MOUSE_NO_BUTTONS,
        /// <summary>
        /// Left button changed to down.
        /// </summary>
        RI_MOUSE_LEFT_BUTTON_DOWN = 0x0001,
        /// <summary>
        /// Left button changed to up.
        /// </summary>
        RI_MOUSE_LEFT_BUTTON_UP = 0x0002,
        /// <summary>
        /// Right button changed to down.
        /// </summary>
        RI_MOUSE_RIGHT_BUTTON_DOWN = 0x0004,
        /// <summary>
        /// Right button changed to up.
        /// </summary>
        RI_MOUSE_RIGHT_BUTTON_UP = 0x0008,
        /// <summary>
        /// Middle button changed to down.
        /// </summary>        
        RI_MOUSE_MIDDLE_BUTTON_DOWN = 0x0010,
        /// <summary>
        /// Middle button changed to up.
        /// </summary>        
        RI_MOUSE_MIDDLE_BUTTON_UP = 0x0020,
        /// <summary>
        /// XBUTTON1 changed to down.
        /// </summary>        
        RI_MOUSE_BUTTON_4_DOWN = 0x0040,
        /// <summary>
        /// XBUTTON1 changed to up.
        /// </summary>  
        RI_MOUSE_BUTTON_4_UP = 0x0080,
        /// <summary>
        /// XBUTTON2 changed to down.
        /// </summary>  
        RI_MOUSE_BUTTON_5_DOWN = 0x0100,
        /// <summary>
        /// XBUTTON2 changed to up.
        /// </summary>  
        RI_MOUSE_BUTTON_5_UP = 0x0200,
        /// <summary>
        /// Raw input comes from a mouse wheel. The wheel delta is stored in us ButtonData.
        /// </summary>  
        RI_MOUSE_WHEEL = 0x0400,
        /// <summary>
        /// Raw input comes from a mouse horizontal wheel. The wheel delta is stored in us ButtonData.
        /// </summary>  
        RI_MOUSE_HORIZONTAL_WHEEL = 0x0800,
    }
}
