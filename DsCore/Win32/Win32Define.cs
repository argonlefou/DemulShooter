using System;
using System.Collections.Generic;
using System.Text;

namespace DsCore.Win32
{
    public static class Win32Define
    {
        //Mouse and Keyboard Hook
        public const int WH_MOUSE_LL = 14;
        public const int WH_KEYBOARD_LL = 13;

        //Windows messages
        public const UInt32 WM_QUIT = 0x0012;
        public const UInt32 WM_COPYDATA = 0x004A;
        public const UInt32 WM_INPUT = 0x00FF;
        public const UInt32 WM_KEYDOWN = 0x0100;
        public const UInt32 WM_KEYUP = 0x0101;
        public const UInt32 WM_SYSKEYDOWN = 0x0104;
        public const UInt32 WM_MOUSEMOVE = 0x0200;
        public const UInt32 WM_LBUTTONDOWN = 0x0201;
        public const UInt32 WM_LBUTTONUP = 0x0202;
        public const UInt32 WM_RBUTTONDOWN = 0x0204;
        public const UInt32 WM_RBUTTONUP = 0x0205;
        public const UInt32 WM_MBUTTONDOWN = 0x0207;
        public const UInt32 WM_MBUTTONUP = 0x0208;
        public const UInt32 WM_MOUSEWHEEL = 0x020A;
        public const UInt32 WM_USB_DEVICECHANGE = 0x0219;  
      
        //Mapped Memory File
        public const UInt32 ERROR_ALREADY_EXISTS = 183;
        public const Int32 INVALID_HANDLE_VALUE = -1;

        public const UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public const UInt32 SECTION_QUERY = 0x0001;
        public const UInt32 SECTION_MAP_WRITE = 0x0002;
        public const UInt32 SECTION_MAP_READ = 0x0004;
        public const UInt32 SECTION_MAP_EXECUTE = 0x0008;
        public const UInt32 SECTION_EXTEND_SIZE = 0x0010;
        public const UInt32 SECTION_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | SECTION_QUERY |
            SECTION_MAP_WRITE |
            SECTION_MAP_READ |
            SECTION_MAP_EXECUTE |
            SECTION_EXTEND_SIZE);
        public const UInt32 FILE_MAP_ALL_ACCESS = SECTION_ALL_ACCESS;
    }
}
