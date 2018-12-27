using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

namespace DemulShooter
{
    class Game_CxbxVcop3 : Game
    {
        /*** MEMORY ADDRESSES **/
        protected int _P1_X_Offset = 0x3578F4;
        protected int _P1_Y_Offset = 0x3578F8;
        protected int _P2_X_Offset = 0x3579CC;
        protected int _P2_Y_Offset = 0x3579D0;
        protected int _Buttons_Injection_Offset_P1 = 0x6C9B0;
        protected int _Buttons_Injection_Return_Offset_P1 = 0x6C9B8;
        protected int _P1_ButtonsStatus = 0;
        protected int _P1_BulletTimeStatus = 0;
        protected int _P2_ButtonsStatus = 0;
        protected int _P2_BulletTimeStatus = 0;

        protected byte _P1_Start_Key = 0x02; //1
        protected byte _P2_Start_Key = 0x03; //2

        private Process _CodeProcess;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_CxbxVcop3(string RomName, bool Verbose)
            : base()
        {
            GetScreenResolution();

            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "Cxbx";

            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for Global VR " + _RomName + " game to hook.....");
        }

        [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumDelegate lpEnumCallbackFunction, IntPtr lParam);
        public delegate bool EnumDelegate(IntPtr hWnd, int lParam);

        /// <summary>
        /// Timer event when looking for Process (auto-Hook and auto-close)
        /// </summary>
        private void tProcess_Tick(Object Sender, EventArgs e)
        {
            if (!_ProcessHooked)
            {
                try
                {
                    ///This emulator can't be retrieve with the same method than the other ones :
                    ///Finding the process by process name gives an empty window and we can't translate coordinate to it
                    ///
                    ///Instead we're listing all desktop childs windows, looking for "cxbx-reload" in the title
                    ///then check that the corresponding exe is "cxbx.exe"
                    ///                    
                    ///But still we need to wait for the game to be loaded, so it means we need to look for at least 2 processes
                    ///in the case of this emulator
                    Process[] processes = Process.GetProcessesByName(_Target_Process_Name);
                    if (processes.Length > 1)
                    {
                        //To find the window process we need to dig a bit more....
                        EnumDelegate filter = delegate(IntPtr hWnd, int lParam)
                        {
                            StringBuilder strbTitle = new StringBuilder(255);
                            int nLength = Win32.GetWindowText(hWnd, strbTitle, strbTitle.Capacity + 1);
                            string strTitle = strbTitle.ToString();

                            if (Win32.IsWindowVisible(hWnd) && /*string.IsNullOrEmpty(strTitle) == false && */strTitle.ToLower().Contains("cxbx-reloaded"))
                            {
                                //collection.Add(strTitle);
                                uint Pid = 0;
                                uint ID = Win32.GetWindowThreadProcessId(hWnd, out Pid);
                                Process pr = Process.GetProcessById((int)Pid);
                                int capacity2 = 1024;
                                StringBuilder sb2 = new StringBuilder(capacity2);
                                Win32.QueryFullProcessImageName(pr.Handle, 0, sb2, ref capacity2);
                                string FullPath2 = sb2.ToString(0, capacity2);
                                if (FullPath2.ToLower().Contains("cxbx.exe"))
                                {
                                    _TargetProcess = pr;
                                }
                            }
                            return true;
                        };

                        EnumDesktopWindows(IntPtr.Zero, filter, IntPtr.Zero);
                        if (_TargetProcess != null)
                        {
                            _ProcessHandle = _TargetProcess.Handle;
                            _TargetProcess_MemoryBaseAddress = _TargetProcess.MainModule.BaseAddress;
                            if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                            {
                                _ProcessHooked = true;
                                WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                WriteLog("MainWindowHandle = " + _TargetProcess.MainWindowHandle.ToString());
                                SetHack();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLog("Error trying to hook " + _Target_Process_Name + ".exe");
                    WriteLog(ex.Message.ToString());
                }
            }
            else
            {
                Process[] processes = Process.GetProcessesByName(_Target_Process_Name);
                if (processes.Length <= 0)
                {
                    _ProcessHooked = false;
                    RemoveMouseHook();
                    _TargetProcess = null;
                    _ProcessHandle = IntPtr.Zero;
                    _TargetProcess_MemoryBaseAddress = IntPtr.Zero;
                    WriteLog(_Target_Process_Name + ".exe closed");
                    Environment.Exit(0);
                }
            }
        }

        #region Screen

        public override void GetScreenResolution()
        {
            WriteLog("using alternative way of Screen size");
            IntPtr hDesktop = Win32.GetDesktopWindow();
            Win32.Rect DesktopRect = new Win32.Rect();
            Win32.GetWindowRect(hDesktop, ref DesktopRect);
            _screenWidth = DesktopRect.Right;
            _screenHeight = DesktopRect.Bottom;
        }

        public override bool GameScale(MouseInfo Mouse, int Player)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    //Window size
                    Win32.Rect TotalRes = new Win32.Rect();
                    Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    Win32.GetWindowRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    WriteLog("Game client window location (Px) = [ " + TotalRes.Left + " ; " + TotalRes.Top + " ]");

                    //X => [-320 ; +320] = 640
                    //Y => [240; -240] = 480
                    double dMaxX = 640.0;
                    double dMaxY = 480.0;

                    Mouse.pTarget.X = Convert.ToInt32(Math.Round(dMaxX * Mouse.pTarget.X / TotalResX) - dMaxX / 2);
                    Mouse.pTarget.Y = Convert.ToInt32((Math.Round(dMaxY * Mouse.pTarget.Y / TotalResY) - dMaxY / 2) * -1);
                    if (Mouse.pTarget.X < -320)
                        Mouse.pTarget.X = -320;
                    if (Mouse.pTarget.Y < -240)
                        Mouse.pTarget.Y = -240;
                    if (Mouse.pTarget.X > 320)
                        Mouse.pTarget.X = 320;
                    if (Mouse.pTarget.Y > 240)
                        Mouse.pTarget.Y = 240;

                    return true;
                }
                catch (Exception ex)
                {
                    WriteLog("Error scaling mouse coordonates to GameFormat : " + ex.Message.ToString());
                }
            }
            return false;
        }

        #endregion

        #region MemoryHack

        private void SetHack()
        {
            byte[] bTest = new byte[] { 0x89, 0x6E, 0x0C };
            byte[] bNop = new byte[] { 0x90, 0x90, 0x90 };

            Process[] proc = Process.GetProcessesByName(_Target_Process_Name);
            int i = 0;
            foreach (Process p in proc)
            {
                int x1 = (int)p.MainModule.BaseAddress + 0x6A3D8;
                int x2 = (int)p.MainModule.BaseAddress + 0x6A403;
                int y1 = (int)p.MainModule.BaseAddress + 0x6A41E;
                int y2 = (int)p.MainModule.BaseAddress + 0x6A3F2;
                _ProcessHandle = p.Handle;

                WriteLog("Testing process #" + i.ToString());
                WriteLog("Process ID = " + p.Id);
                byte[] b = ReadBytes(x1, 3);
                WriteLog("bTest = 0x" + bTest[0].ToString("X2") + ", 0x" + bTest[1].ToString("X2") + ", 0x" + bTest[2].ToString("X2"));
                WriteLog("b = 0x" + b[0].ToString("X2") + ", 0x" + b[1].ToString("X2") + ", 0x" + b[2].ToString("X2"));
                if (b[0] == bTest[0] && b[1] == bTest[1] && b[2] == bTest[2])
                {
                    WriteLog("Correct process !!");
                    _CodeProcess = p;

                    //Creating data bank
                    //Codecave :
                    Memory CaveMemoryInput = new Memory(p, p.MainModule.BaseAddress);
                    CaveMemoryInput.Open();
                    CaveMemoryInput.Alloc(0x800);
                    _P1_ButtonsStatus = CaveMemoryInput.CaveAddress;
                    _P2_ButtonsStatus = CaveMemoryInput.CaveAddress + 0x08;
                    _P1_BulletTimeStatus = CaveMemoryInput.CaveAddress + 0x10;
                    _P2_BulletTimeStatus = CaveMemoryInput.CaveAddress + 0x18;
                    WriteLog("Custom data will be stored at : 0x" + _P1_ButtonsStatus.ToString("X8"));

                    //For P1 :
                    //modify [edx+0x21] so that it corresponds to our values
                    //EDX + 0x21 ==> 0x10 (START)
                    //EDX + 0x22 ==>
                    //EDX + 0x23 ==> 0xFF (TRIGGER)
                    //EDX + 0x24 ==> 0xFF (RELOAD)
                    //EDX + 0x29 ==> 0xFF (Bullet Time)
                    //[ESP + 0x10] (after our push) contains controller ID (0->4)
                    Memory CaveMemory = new Memory(_CodeProcess, _CodeProcess.MainModule.BaseAddress);
                    CaveMemory.Open();
                    CaveMemory.Alloc(0x800);                    
                    //mov edx, [esp+04]
                    CaveMemory.Write_StrBytes("8B 54 24 04");
                    //push eax
                    CaveMemory.Write_StrBytes("50");
                    //cmp [esp+0x10], 0
                    CaveMemory.Write_StrBytes("83 7C 24 10 00");
                    //je Player1
                    CaveMemory.Write_StrBytes("0F 84 10 00 00 00");
                    //cmp [esp + 0x10], 1
                    CaveMemory.Write_StrBytes("83 7C 24 10 01");
                    //je Player2
                    CaveMemory.Write_StrBytes("0F 84 15 00 00 00");
                    //jmp originalcode
                    CaveMemory.Write_StrBytes("E9 1B 00 00 00");
                    //Player1 :
                    //mov eax, [_P1_ButtonsStatus]
                    b = BitConverter.GetBytes(_P1_ButtonsStatus);
                    CaveMemory.Write_StrBytes("A1");
                    CaveMemory.Write_Bytes(b);
                    //mov ecx, [_P1_BulletTimeStatus]
                    b = BitConverter.GetBytes(_P1_BulletTimeStatus);
                    CaveMemory.Write_StrBytes("8B 0D");
                    CaveMemory.Write_Bytes(b);
                    //jmp originalcode
                    CaveMemory.Write_StrBytes("E9 0B 00 00 00");
                    //Player2:
                    //mov eax, [_P2_ButtonsStatus]
                    b = BitConverter.GetBytes(_P2_ButtonsStatus);
                    CaveMemory.Write_StrBytes("A1");
                    CaveMemory.Write_Bytes(b);
                    //mov ecx, [_P2_BulletTimeStatus]
                    b = BitConverter.GetBytes(_P2_BulletTimeStatus);
                    CaveMemory.Write_StrBytes("8B 0D");
                    CaveMemory.Write_Bytes(b);
                    //originalcode:
                    //mov [edx+0x21], eax
                    CaveMemory.Write_StrBytes("89 42 21");
                    //pop eax
                    CaveMemory.Write_StrBytes("58");
                    //mov [edx+0x29], ecx
                    CaveMemory.Write_StrBytes("89 4A 29");
                    //mov cx, [edx+0x21]
                    CaveMemory.Write_StrBytes("66 8B 4A 21");
                    //return
                    CaveMemory.Write_jmp((int)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Return_Offset_P1);

                    WriteLog("Adding Trigger CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

                    //Code injection
                    IntPtr ProcessHandle = _CodeProcess.Handle;
                    int bytesWritten = 0;
                    int jumpTo = 0;
                    jumpTo = CaveMemory.CaveAddress - ((int)_CodeProcess.MainModule.BaseAddress + _Buttons_Injection_Offset_P1) - 5;
                    List<byte> Buffer = new List<byte>();
                    Buffer.Add(0xE9);                    
                    Buffer.AddRange(BitConverter.GetBytes(jumpTo));
                    Buffer.Add(0x90);
                    Buffer.Add(0x90);
                    Buffer.Add(0x90);
                    Win32.WriteProcessMemory((int)ProcessHandle, (int)_CodeProcess.MainModule.BaseAddress + _Buttons_Injection_Offset_P1, Buffer.ToArray(), Buffer.Count, ref bytesWritten);

                    //Noping Axis procedures
                    WriteBytes(x1, bNop);
                    WriteBytes(x2, bNop);
                    WriteBytes(y1, bNop);
                    WriteBytes(y2, bNop);

                    ApplyKeyboardHook();

                    break;
                }
                else
                {
                    WriteLog("Not the good one...");
                }
                i++;
            }
        }
        // Keyboard callback used for pedal-mode
        protected override IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                Win32.KBDLLHOOKSTRUCT s = (Win32.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32.WM_KEYDOWN)
                {
                    if (s.scanCode == _P1_Start_Key)
                        Apply_OR_ByteMask(_P1_ButtonsStatus, 0x10);
                    else if (s.scanCode == _P2_Start_Key)
                        Apply_OR_ByteMask(_P2_ButtonsStatus, 0x10);
                }
                else if ((UInt32)wParam == Win32.WM_KEYUP)
                {
                    if (s.scanCode == _P1_Start_Key)
                        Apply_AND_ByteMask(_P1_ButtonsStatus, 0x00);
                    else if (s.scanCode == _P2_Start_Key)
                        Apply_AND_ByteMask(_P2_ButtonsStatus, 0x00);
                }
            }
            return Win32.CallNextHookEx(_KeyboardHookID, nCode, wParam, lParam);
        }


        public override void SendInput(MouseInfo mouse, int Player)
        {
            byte[] bufferX = BitConverter.GetBytes(mouse.pTarget.X);
            byte[] bufferY = BitConverter.GetBytes(mouse.pTarget.Y);

            if (Player == 1)
            {
                //Write Axis
                WriteBytes((int)_CodeProcess.MainModule.BaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((int)_CodeProcess.MainModule.BaseAddress + _P1_Y_Offset, bufferY);

                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_P1_ButtonsStatus + 2, 0xFF);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_P1_ButtonsStatus + 2, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_P1_BulletTimeStatus, 0xFF);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_P1_BulletTimeStatus, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_P1_ButtonsStatus + 3, 0xFF);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_P1_ButtonsStatus + 3, 0x00);
                }
            }
            else if (Player == 2)
            {
                WriteBytes((int)_CodeProcess.MainModule.BaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((int)_CodeProcess.MainModule.BaseAddress + _P2_Y_Offset, bufferY);

                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_P2_ButtonsStatus + 2, 0xFF);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_P2_ButtonsStatus + 2, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_P2_BulletTimeStatus, 0xFF);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_P2_BulletTimeStatus, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_P2_ButtonsStatus + 3, 0xFF);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_P2_ButtonsStatus + 3, 0x00);
                }
            }
        }

        #endregion
    }
}
