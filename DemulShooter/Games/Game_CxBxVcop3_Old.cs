using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    /// <summary>
    /// First and old hack, where Cxbx-Reloaded is started by it's GUI to choose and run a Rom
    /// There are 2 Processes to analyse to find which one has the window handle and which one has the game code
    /// This is not used anymore but kept in DemulShooter for documentation.
    /// </summary>
    class Game_CxbxVcop3_Old : Game
    {
        /*** MEMORY ADDRESSES **/
        private UInt32 _P1_X_Offset = 0x3578F4;
        private UInt32 _P1_Y_Offset = 0x3578F8;
        private UInt32 _P2_X_Offset = 0x3579CC;
        private UInt32 _P2_Y_Offset = 0x3579D0;
        private UInt32 _Buttons_Injection_Offset = 0x6C9B0;
        private UInt32 _Buttons_Injection_Return_Offset = 0x6C9B8;
        private UInt32 _P1_Buttons_CaveAddress = 0;
        private UInt32 _P1_BulletTime_CaveAddress = 0;
        private UInt32 _P2_Buttons_CaveAddress = 0;
        private UInt32 _P2_BulletTime_CaveAddress = 0;
        private UInt32 _RomLoaded_CheckIntructionn_Offset = 0x0006A3D8;

        //Offset to NOP axis instructions
        private NopStruct _Nop_X_1 = new NopStruct(0x0006A3D8, 3);
        private NopStruct _Nop_X_2 = new NopStruct(0x0006A403, 3);
        private NopStruct _Nop_Y_1 = new NopStruct(0x0006A41E, 3);
        private NopStruct _Nop_Y_2 = new NopStruct(0x0006A3F2, 3);
        
        private const HardwareScanCode P1_START_KEY = HardwareScanCode.DIK_1;
        private const HardwareScanCode P2_START_KEY = HardwareScanCode.DIK_2;

        //Cxbx emulation is done with 2 Processes :
        //- The first one with a MainWindowHandle that will be used to translate coordinates thanks to WIN32 API, 
        // but with no code loaded for the rom (Axis, buttons). This will be the new _WindowProcess;
        //- The second one with the rom loaded inside, but without Window to translate, 
        // this will be our usual to hook and inject data
        private Process _WindowProcess;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_CxbxVcop3_Old(String RomName, double ForcedXratio, bool Verbose)
            : base(RomName, "Cxbx", ForcedXratio, Verbose)
        {
            //No need to check MD5 as the emulator has a lot of builds compatible

            _tProcess.Start();
            Logger.WriteLog("Waiting for Chihiro " + _RomName + " game to hook.....");
        }

        [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumDelegate lpEnumCallbackFunction, IntPtr lParam);
        public delegate bool EnumDelegate(IntPtr hWnd, int lParam);

        /// <summary>
        /// Timer event when looking for Process (auto-Hook and auto-close)
        /// </summary>
        protected override void tProcess_Elapsed(Object Sender, EventArgs e)
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
                            int nLength = Win32API.GetWindowText(hWnd, strbTitle, strbTitle.Capacity + 1);
                            String strTitle = strbTitle.ToString();

                            if (Win32API.IsWindowVisible(hWnd) && /*string.IsNullOrEmpty(strTitle) == false && */strTitle.ToLower().Contains("cxbx-reloaded"))
                            {
                                //collection.Add(strTitle);
                                uint Pid = 0;
                                uint ID = Win32API.GetWindowThreadProcessId(hWnd, out Pid);
                                Process pr = Process.GetProcessById((int)Pid);
                                int capacity2 = 1024;
                                StringBuilder sb2 = new StringBuilder(capacity2);
                                Win32API.QueryFullProcessImageName(pr.Handle, 0, sb2, ref capacity2);
                                String FullPath2 = sb2.ToString(0, capacity2);
                                if (FullPath2.ToLower().Contains("cxbx.exe"))
                                {
                                    _WindowProcess = pr;
                                }
                            }
                            return true;
                        };

                        EnumDesktopWindows(IntPtr.Zero, filter, IntPtr.Zero);
                        if (_WindowProcess != null)
                        {
                            Logger.WriteLog("Attached to Window Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _WindowProcess.Handle.ToString());
                            Logger.WriteLog("MainWindowHandle = " + _WindowProcess.MainWindowHandle.ToString());
                            SetHack();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Error trying to hook " + _Target_Process_Name + ".exe");
                    Logger.WriteLog(ex.Message.ToString());
                }
            }
            else
            {
                Process[] processes = Process.GetProcessesByName(_Target_Process_Name);
                if (processes.Length <= 0)
                {
                    _ProcessHooked = false;
                    _TargetProcess = null;
                    _ProcessHandle = IntPtr.Zero;
                    _TargetProcess_MemoryBaseAddress = IntPtr.Zero;
                    Logger.WriteLog(_Target_Process_Name + ".exe closed");
                    Application.Exit();
                }
            }
        }

        #region Screen

        public override void GetScreenResolution()
        {
            Logger.WriteLog("using alternative way of Screen size");
            GetScreenresolution2();
        }

        /// <summary>
        /// Convert screen location of pointer to Client area location
        /// We need to override this because the targeted window is not the usual one
        /// </summary>
        public override bool ClientScale(PlayerSettings PlayerData)
        {
            //Convert Screen location to Client location
            if (_TargetProcess != null)
            {
                POINT p = new POINT(PlayerData.RIController.Computed_X, PlayerData.RIController.Computed_Y);
                if (Win32API.ScreenToClient(_TargetProcess.MainWindowHandle, ref p))
                {
                    PlayerData.RIController.Computed_X = (p.X);
                    PlayerData.RIController.Computed_Y = (p.Y);
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        public override bool GameScale(PlayerSettings PlayerData)
        {
            if (_WindowProcess.Handle != IntPtr.Zero)
            {
                try
                {
                    //Window size
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_WindowProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    Win32API.GetWindowRect(_WindowProcess.MainWindowHandle, ref TotalRes);
                    Logger.WriteLog("Game client window location (Px) = [ " + TotalRes.Left + " ; " + TotalRes.Top + " ]");

                    //X => [-320 ; +320] = 640
                    //Y => [240; -240] = 480
                    double dMaxX = 640.0;
                    double dMaxY = 480.0;

                    PlayerData.RIController.Computed_X = Convert.ToInt32(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX) - dMaxX / 2);
                    PlayerData.RIController.Computed_Y = Convert.ToInt32((Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY) - dMaxY / 2) * -1);
                    if (PlayerData.RIController.Computed_X < -320)
                        PlayerData.RIController.Computed_X = -320;
                    if (PlayerData.RIController.Computed_Y < -240)
                        PlayerData.RIController.Computed_Y = -240;
                    if (PlayerData.RIController.Computed_X > 320)
                        PlayerData.RIController.Computed_X = 320;
                    if (PlayerData.RIController.Computed_Y > 240)
                        PlayerData.RIController.Computed_Y = 240;

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Error scaling mouse coordonates to GameFormat : " + ex.Message.ToString());
                }
            }
            return false;
        }

        #endregion

        #region Memory Hack

        private void SetHack()
        {
            //This is HEX code for the instrution we're testing to see which process is the good one to hook
            byte[] bTest = new byte[] { 0x8B, 0xE8 };

            Process[] proc = Process.GetProcessesByName(_Target_Process_Name);
            int i = 0;
            foreach (Process p in proc)
            {                
                _ProcessHandle = p.Handle;
                Logger.WriteLog("Testing process #" + i.ToString());
                Logger.WriteLog("Process ID = " + p.Id);
                Logger.WriteLog("Testing instruction at 0x" + ((UInt32)p.MainModule.BaseAddress + _RomLoaded_CheckIntructionn_Offset - 2).ToString("X8"));
                byte[] b = ReadBytes((UInt32)p.MainModule.BaseAddress + _RomLoaded_CheckIntructionn_Offset - 2, 2);
                Logger.WriteLog("Waiting for : 0x" + bTest[0].ToString("X2") + ", 0x" + bTest[1].ToString("X2"));
                Logger.WriteLog("Read values : 0x" + b[0].ToString("X2") + ", 0x" + b[1].ToString("X2"));
                if (b[0] == bTest[0] && b[1] == bTest[1])
                {
                    Logger.WriteLog("Correct process for code injection");
                    _TargetProcess = p;
                    _TargetProcess_MemoryBaseAddress = p.MainModule.BaseAddress;
                    Logger.WriteLog("WindowHandle (should be 0) = " + p.MainWindowHandle.ToString());

                    //Creating data bank
                    //Codecave :
                    Codecave CaveMemoryInput = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
                    CaveMemoryInput.Open();
                    CaveMemoryInput.Alloc(0x800);
                    _P1_Buttons_CaveAddress = CaveMemoryInput.CaveAddress;
                    _P2_Buttons_CaveAddress = CaveMemoryInput.CaveAddress + 0x08;
                    _P1_BulletTime_CaveAddress = CaveMemoryInput.CaveAddress + 0x10;
                    _P2_BulletTime_CaveAddress = CaveMemoryInput.CaveAddress + 0x18;
                    Logger.WriteLog("Custom data will be stored at : 0x" + _P1_Buttons_CaveAddress.ToString("X8"));

                    //For P1 :
                    //modify [edx+0x21] so that it corresponds to our values
                    //EDX + 0x21 ==> 0x10 (START)
                    //EDX + 0x22 ==>
                    //EDX + 0x23 ==> 0xFF (TRIGGER)
                    //EDX + 0x24 ==> 0xFF (RELOAD)
                    //EDX + 0x29 ==> 0xFF (Bullet Time)
                    //[ESP + 0x10] (after our push) contains controller ID (0->4)
                    Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
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
                    //mov eax, [_P1_Buttons_CaveAddress]
                    b = BitConverter.GetBytes(_P1_Buttons_CaveAddress);
                    CaveMemory.Write_StrBytes("A1");
                    CaveMemory.Write_Bytes(b);
                    //mov ecx, [_P1_BulletTime_CaveAddress]
                    b = BitConverter.GetBytes(_P1_BulletTime_CaveAddress);
                    CaveMemory.Write_StrBytes("8B 0D");
                    CaveMemory.Write_Bytes(b);
                    //jmp originalcode
                    CaveMemory.Write_StrBytes("E9 0B 00 00 00");
                    //Player2:
                    //mov eax, [_P2_Buttons_CaveAddress]
                    b = BitConverter.GetBytes(_P2_Buttons_CaveAddress);
                    CaveMemory.Write_StrBytes("A1");
                    CaveMemory.Write_Bytes(b);
                    //mov ecx, [_P2_BulletTime_CaveAddress]
                    b = BitConverter.GetBytes(_P2_BulletTime_CaveAddress);
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
                    CaveMemory.Write_jmp((UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Return_Offset);

                    Logger.WriteLog("Adding Trigger CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

                    //Code injection
                    IntPtr ProcessHandle = _TargetProcess.Handle;
                    UInt32 bytesWritten = 0;
                    UInt32 jumpTo = 0;
                    jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Offset) - 5;
                    List<byte> Buffer = new List<byte>();
                    Buffer.Add(0xE9);                    
                    Buffer.AddRange(BitConverter.GetBytes(jumpTo));
                    Buffer.Add(0x90);
                    Buffer.Add(0x90);
                    Buffer.Add(0x90);
                    Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

                    //Noping Axis procedures
                    SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_X_1);
                    SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_X_2);
                    SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Y_1);
                    SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Y_2);

                    _ProcessHooked = true;
                }
                else
                {
                    Logger.WriteLog("Windows Process, not good for code injection");
                    Logger.WriteLog("WindowHandle = " + p.MainWindowHandle.ToString());
                }
                i++;
            }
            _ProcessHandle = _TargetProcess.Handle;
        }
        
        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes(PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes(PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_P1_Buttons_CaveAddress + 2, 0xFF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress + 2, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask(_P1_BulletTime_CaveAddress, 0xFF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_P1_BulletTime_CaveAddress, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_P1_Buttons_CaveAddress + 3, 0xFF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress + 3, 0x00);
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress + 2, 0xFF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress + 2, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask(_P2_BulletTime_CaveAddress, 0xFF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_P2_BulletTime_CaveAddress, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress + 3, 0xFF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress + 3, 0x00);
            }
        }

        /// <summary>
        /// Low-level keyboard hook callback.
        /// For Vcop 3 we are using this to send P1 and P2 Start
        /// </summary>
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    if (s.scanCode == P1_START_KEY)
                        Apply_OR_ByteMask(_P1_Buttons_CaveAddress, 0x10);
                    else if (s.scanCode == P2_START_KEY)
                        Apply_OR_ByteMask(_P2_Buttons_CaveAddress, 0x10);
                }
                else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                {
                    if (s.scanCode == P1_START_KEY)
                        Apply_AND_ByteMask(_P1_Buttons_CaveAddress, 0x00);
                    else if (s.scanCode == P2_START_KEY)
                        Apply_AND_ByteMask(_P2_Buttons_CaveAddress, 0x00);
                }
            }
            return Win32API.CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
        }       

        #endregion
    }
}
