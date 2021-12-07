using System;
using System.Runtime.InteropServices;
using System.Text;
using DsCore.RawInput;

namespace DsCore.Win32
{
    public static class Win32API
    {
        #region hid.dll

        [DllImport("hid", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool HidD_GetManufacturerString(IntPtr HidDeviceObject, [Out] byte[] Buffer, uint BufferLength);

        [DllImport("hid", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool HidD_GetProductString(IntPtr HidDeviceObject, [Out] byte[] Buffer, uint BufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern NtStatus HidP_GetCaps(IntPtr pPreparsedData, out HidPCaps Capabilities);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern NtStatus HidP_GetButtonCaps(HidPReportType ReportType, [Out] HidPButtonCaps[] ButtonCaps, ref ushort ButtonCapsLength, IntPtr pPreparsedData);
        
        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        public static extern NtStatus HidP_GetValueCaps(HidPReportType ReportType, [Out] HidPValueCaps[] Values, ref ushort ValueCapsLength, IntPtr pPreparsedData);

        [DllImport("hid")]
        public static extern NtStatus HidP_GetUsages(HidPReportType ReportType, ushort UsagePage, ushort LinkCollection, [Out] ushort[] UsageList, ref uint UsageLength, IntPtr pPreparsedData, byte[] Report, uint ReportLength);

        [DllImport("hid")]
        //public static extern NtStatus HidP_GetUsageValue(HidPReportType ReportType, ushort UsagePage, ushort LinkCollection, ushort Usage, out long UsageValue, IntPtr pPreparsedData, byte[] Report, uint ReportLength);
        public static extern NtStatus HidP_GetUsageValue(HidPReportType ReportType, ushort UsagePage, ushort LinkCollection, ushort Usage, out int UsageValue, IntPtr pPreparsedData, byte[] Report, uint ReportLength);

        [DllImport("hid")]
        public static extern NtStatus HidP_GetScaledUsageValue(HidPReportType ReportType, ushort UsagePage, ushort LinkCollection, ushort Usage, out int UsageValue, IntPtr pPreparsedData, byte[] Report, uint ReportLength);

        #endregion

        #region user32.dll

        #region GDI

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        #endregion

        #region RawInput API

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(IntPtr hRawInput, RawInputUiCommand uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(IntPtr hRawInput, RawInputUiCommand uiCommand, out RawInputHeader pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(IntPtr hRawInput, RawInputUiCommand uiCommand, out RawInputDataMouse pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, RawInputUiCommand uiCommand, IntPtr pData, ref uint dataSize);

        [DllImport("user32", SetLastError = true)]
        public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, RawInputUiCommand uiCommand, out RawInputDeviceInfo pData, uint pcbSize);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern uint GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint NumberDevices, uint Size);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern bool RegisterRawInputDevices(RawInputDevice[] pRawInputDevice, uint NumberDevices, uint Size);

        #endregion

        #region Low Level Hook API

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out IntPtr ProcessId);

        /** ScanCode <-> VK_CODE mapping **/
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint MapVirtualKey(uint uCode, VirtualKeyMapType uMapType);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
        public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);        

        #endregion

        #region Screen API

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "CreateWindowEx")]
        public static extern IntPtr CreateWindowEx(
           int dwExStyle,
           UInt16 regResult,
            //string lpClassName,
           string lpWindowName,
           UInt32 dwStyle,
           int x,
           int y,
           int nWidth,
           int nHeight,
           IntPtr hWndParent,
           IntPtr hMenu,
           IntPtr hInstance,
           IntPtr lpParam);


        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hWnd, ref Rect rectangle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(SystemMetricsIndex smIndex);
       
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, ref Rect rectangle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowText(IntPtr Hwnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out UInt32 lpdwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "RegisterClassEx")]
        public static extern UInt16 RegisterClassEx([In] ref WNDCLASSEX lpWndClass);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool UpdateWindow(IntPtr hWnd);

        #endregion

        #region Windows Messages

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        #endregion

        /** VirtualKey Sending **/
        /** Used for Operation Ghost which have hardcoded Numpad Keys that are not working with DIK if no keyboard plugged **/
        [DllImport("user32.dll", SetLastError = true)]
        public static extern void keybd_event(VirtualKeyCode bVk, byte bScan, KeybdInputFlags dwFlags, int dwExtraInfo);

        

        /** DIRECTINPUT SendKey **/
        [DllImport("user32.dll", SetLastError = true)]
        public static extern UInt32 SendInput(UInt32 nInputs, [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] INPUT[] pInputs, Int32 cbSize);

        [DllImport("user32.dll")]
        public static extern int ShowCursor(bool bShow);

        #endregion

        #region kernel32.dll

        #region I/O API

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateFile(string lpFileName, DesiredAccess dwDesiredAccess, ShareMode dwShareMode, IntPtr lpSecurityAttributes, CreateDisposition dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);
        
        [Flags]
        public enum DesiredAccess : uint
        {
            None,
            Write = 0x40000000,
            Read = 0x80000000
        }

        [Flags]
        public enum ShareMode : uint
        {
            None,
            Read = 0x00000001,
            Write = 0x00000002,
            Delete = 0x00000004
        }

        public enum CreateDisposition : uint
        {
            CreateNew = 1,
            CreateAlways,
            OpenExisting,
            OpenAlways,
            TruncateExisting
        }

        #endregion

        #region Memory API

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
 
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, UInt32 lpBaseAddress, byte[] lpBuffer, UInt32 dwSize, ref UInt32 lpNumberOfBytesRead);   

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "ReadProcessMemory")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean ReadProcessMemoryX64([In] IntPtr hProcess, [In] IntPtr lpBaseAddress, [Out] Byte[] lpBuffer, [In] UIntPtr nSize, [Out] out UIntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, UInt32 dwSize, MemoryAllocType flAllocationType, MemoryPageProtect flProtect);

        [DllImport("kernel32.dll")]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, UInt32 dwSize, MemoryFreeType dwFreeType);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, UInt32 lpBaseAddress, byte[] lpBuffer, UInt32 dwSize, ref UInt32 lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "WriteProcessMemory")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean WriteProcessMemoryX64([In] IntPtr hProcess, [In] IntPtr lpBaseAddress, [Out] Byte[] lpBuffer, [In] UIntPtr nSize, [Out] out UIntPtr lpNumberOfBytesWritten);

        #endregion

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool QueryFullProcessImageName([In]IntPtr hProcess, [In]int dwFlags, [Out]StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        #endregion
    }
}
