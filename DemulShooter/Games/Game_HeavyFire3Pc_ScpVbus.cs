using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    /*** HeavyFire4 => Heavy Fire Afghanistan ***/
    class Game_HeavyFire3Pc_ScpVbus : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\windows\hfa";
        private const String HFA_STEAM_FILENAME = "HeavyFire3_Final.exe";
        private const String HFA_FILENAME = "HeavyFire3.exe";
        private String _ExecutableFilePath = String.Empty;
        private bool _EnableP2 = false;

        /*** MEMORY ADDRESSES **/
        //Initialized with "Steam" version of the game, as I don't have the MD5 hash of the file to load it if needed
        private UInt32 _P1_X_CaveAddress;
        private UInt32 _P1_Y_CaveAddress;
        private UInt32 _P1_Buttons_Offset = 0x0024B43C;
        private NopStruct _Nop_P1_Buttons = new NopStruct(0x001164F2, 7);
        private UInt32 _P2_X_CaveAddress;
        private UInt32 _P2_Y_CaveAddress;
        private UInt32 _P1_X_Injection_Offset = 0x0001E85F;
        private UInt32 _P1_X_Injection_Return_Offset = 0x0001E866;
        private UInt32 _P1_Y_Injection_Offset = 0x0001E87C;
        private UInt32 _P1_Y_Injection_Return_Offset = 0x0001E883;
        private UInt32 _P2_X_Injection_Offset = 0x0001F727;
        private UInt32 _P2_X_Injection_Return_Offset = 0x0001F72E;
        private UInt32 _P2_Y_Injection_Offset = 0x0001F740;
        private UInt32 _P2_Y_Injection_Return_Offset = 0x0001F747;

        //Keys to send
        //Cover Left = A
        //Cover Bottom = S
        //Cover Right = D
        //QTE = Space
        private const VirtualKeyCode _QTE_W_VK = VirtualKeyCode.VK_W;
        private const VirtualKeyCode _CoverLeft_VK = VirtualKeyCode.VK_A;
        private const VirtualKeyCode _CoverRight_VK = VirtualKeyCode.VK_D;
        private const VirtualKeyCode _CoverBottom_VK = VirtualKeyCode.VK_S;
        private const VirtualKeyCode _QTE_Space_VK = VirtualKeyCode.VK_SPACE;

        //Keys to read
        private const HardwareScanCode _P2_UpArrow_ScanCode = HardwareScanCode.DIK_NUMPAD8;
        private const HardwareScanCode _P2_DownArrow_ScanCode = HardwareScanCode.DIK_NUMPAD2;
        private const HardwareScanCode _P1_Grenade_ScanCode = HardwareScanCode.DIK_G;
        private const HardwareScanCode _P2_Grenade_ScanCode = HardwareScanCode.DIK_H;

        //Custom data to inject
        protected float _P1_X_Value;
        protected float _P1_Y_Value;
        protected float _P2_X_Value;
        protected float _P2_Y_Value;

        protected float _Axis_X_Min;
        protected float _Axis_X_Max;
        protected float _CoverDelta = 0.3f;
        protected bool _CoverLeftEnabled = false;
        protected bool _CoverBottomEnabled = false;
        protected bool _CoverRightEnabled = false;
        protected bool _QTE_Spacebar_Enabled = false;
        protected bool _QTE_W_Enabled = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_HeavyFire3Pc_ScpVbus(String RomName, String GamePath, int CoverSensibility, bool EnableP2, bool Verbose) : 
            base (RomName, "HeavyFire3", 00.0, Verbose)
        {
            _EnableP2 = EnableP2;
            if (EnableP2)
            {
                /*** Creating a virtual X360 gamepad for player 2 ***/
                _XOutputManager = new XOutput();
                InstallX360Gamepad(2);
            }

            //Steam version of the game has a different .exe name
            if (File.Exists(GamePath + @"\" + HFA_STEAM_FILENAME))
            {
                _Target_Process_Name = "HeavyFire3_Final";
                _ExecutableFilePath = GamePath + @"\" + HFA_STEAM_FILENAME;
            }

            _KnownMd5Prints.Add("Heavy Fire 3 - MASTIFF", "3f49951ae8232817a91ef5503374d6b3");
            _KnownMd5Prints.Add("Heavy Fire 3 - STEAM", "");

            _tProcess.Start();            
            Logger.WriteLog("Waiting for Windows Game " + _RomName + " game to hook.....");
        }

        /// <summary>
        /// Timer event when looking for Process (auto-Hook and auto-close)
        /// </summary>
        protected override void tProcess_Elapsed(Object Sender, EventArgs e)
        {
            if (!_ProcessHooked)
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName(_Target_Process_Name);
                    if (processes.Length > 0)
                    {
                        _TargetProcess = processes[0];
                        _ProcessHandle = _TargetProcess.Handle;
                        _TargetProcess_MemoryBaseAddress = _TargetProcess.MainModule.BaseAddress;

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();
                            ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                            SetHack();
                            _ProcessHooked = true;
                            
                        }
                    }
                }
                catch
                {
                    Logger.WriteLog("Error trying to hook " + _Target_Process_Name + ".exe");
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

        /// <summary>
        /// For P2 we need to plug a virtual Gamepad
        /// </summary>
        protected override void InstallX360Gamepad(int Player)
        {
            if (_XOutputManager != null)
            {
                if (_XOutputManager.isVBusExists())
                {
                    if (_XOutputManager.PlugIn(1))
                    {
                        if (Player == 2)
                        {
                            Logger.WriteLog("Plugged P2 virtual Gamepad to port 1");
                            _Player2_X360_Gamepad_Port = 1;
                        }
                    }
                    else
                    {
                        Logger.WriteLog("Failed to plug virtual GamePad to port 1. (Port already used ?)");
                        if (_XOutputManager.UnplugAll(true))
                        {
                            Logger.WriteLog("Force Unpluged all gamepads.");
                            System.Threading.Thread.Sleep(1000);
                            if (_XOutputManager.PlugIn(1))
                            {
                                if (Player == 2)
                                {
                                    Logger.WriteLog("Plugged P2 virtual Gamepad to port 1");
                                    _Player2_X360_Gamepad_Port = 1;
                                }
                            }
                            else
                            {
                                Logger.WriteLog("Failed to plug virtual GamePad to port 1.");
                            }
                        }
                        else
                        {
                            Logger.WriteLog("Failed to force Unplug virtual GamePad port 1.");
                        }
                    }
                }
                else
                {
                    Logger.WriteLog("ScpBus driver not found or not installed");
                }
            }
            else
            {
                Logger.WriteLog("XOutputManager Creation Failed !");
            }
        }
        
        #region Screen

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// </summary>
        public override bool GameScale(PlayerSettings PlayerData)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    int TotalResX = TotalRes.Right - TotalRes.Left;
                    int TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //Y => [-1 ; 1] float
                    //X => depend on ration Width/Height (ex : [-1.7777; 1.7777] with 1920x1080)

                    float fRatio = (float)TotalResX / (float)TotalResY;
                    _Axis_X_Min = -fRatio;
                    _Axis_X_Max = fRatio;

                    float _Y_Value = (2.0f * PlayerData.RIController.Computed_Y / TotalResY) - 1.0f;
                    float _X_Value = (fRatio * 2 * PlayerData.RIController.Computed_X / TotalResX) - fRatio;

                    if (_X_Value < _Axis_X_Min)
                        _X_Value = _Axis_X_Min;
                    if (_Y_Value < -1.0f)
                        _Y_Value = -1.0f;
                    if (_X_Value > _Axis_X_Max)
                        _X_Value = _Axis_X_Max;
                    if (_Y_Value > 1.0f)
                        _Y_Value = 1.0f;

                    if (PlayerData.ID == 1)
                    {
                        _P1_X_Value = _X_Value;
                        _P1_Y_Value = _Y_Value;
                    }
                    else if (PlayerData.ID == 2)
                    {
                        _P2_X_Value = _X_Value;
                        _P2_Y_Value = _Y_Value;
                    }

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
            CreateDataBank();
            SetHack_P1X();
            SetHack_P1Y();
            SetHack_P2X();
            SetHack_P2Y();
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_Buttons);
        }

        /*** Creating a custom memory bank to store our data ***/
        private void CreateDataBank()
        {
            //1st Codecave : storing our Axis Data
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            _P1_X_CaveAddress = CaveMemory.CaveAddress;
            _P1_Y_CaveAddress = CaveMemory.CaveAddress + 0x04;

            _P2_X_CaveAddress = CaveMemory.CaveAddress + 0x20;
            _P2_Y_CaveAddress = CaveMemory.CaveAddress + 0x24;

            Logger.WriteLog("Custom data will be stored at : 0x" + _P1_X_CaveAddress.ToString("X8"));            
        }

        /// <summary>
        /// All Axis codecave are the same :
        /// The game use some fstp [XXX] instruction, but we can't just NOP it as graphical glitches may appear.
        /// So we just add another set of instructions instruction immediatelly after to change the register 
        /// to our own desired value
        /// </summary>
        private void SetHack_P1X()
        {   
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //fstp [esi+edi*8+F0000000]
            CaveMemory.Write_StrBytes("D9 9C FE F0 00 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [P1_X]
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P1_X_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov [esi+edi*8+F000000], eax
            CaveMemory.Write_StrBytes("89 84 FE F0 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Return_Offset);

            Logger.WriteLog("Adding P1 X Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        private void SetHack_P1Y()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //fstp [esi+edi*8+F4000000]
            CaveMemory.Write_StrBytes("D9 9C FE F4 00 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [P1_Y]
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P1_Y_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov [esi+edi*8+F400000], eax
            CaveMemory.Write_StrBytes("89 84 FE F4 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Y_Injection_Return_Offset);

            Logger.WriteLog("Adding P1 Y Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Y_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Y_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        private void SetHack_P2X()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //fstp [esi+edi*8+F0000000]
            CaveMemory.Write_StrBytes("D9 9C FE F0 00 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [P2_X]
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P2_X_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov [esi+edi*8+F000000], eax
            CaveMemory.Write_StrBytes("89 84 FE F0 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_X_Injection_Return_Offset);

            Logger.WriteLog("Adding P2 X Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_X_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _P2_X_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        private void SetHack_P2Y()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //fstp [esi+edi*8+F4000000]
            CaveMemory.Write_StrBytes("D9 9C FE F4 00 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [P2_Y]
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P2_Y_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov [esi+edi*8+F400000], eax
            CaveMemory.Write_StrBytes("89 84 FE F4 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Y_Injection_Return_Offset);

            Logger.WriteLog("Adding P2 Y Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Y_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Y_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>  
        public override void SendInput(PlayerSettings PlayerData)
        {
            if (PlayerData.ID == 1)
            {
                //Setting Values in memory for the Codecave to read it
                byte[] buffer = BitConverter.GetBytes(_P1_X_Value);
                WriteBytes(_P1_X_CaveAddress, buffer);
                buffer = BitConverter.GetBytes(_P1_Y_Value);
                WriteBytes(_P1_Y_CaveAddress, buffer);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFE);
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFE);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    if (_P1_X_Value < _Axis_X_Min + _CoverDelta)
                    {
                        Send_VK_KeyDown(_CoverLeft_VK);
                        _CoverLeftEnabled = true;
                    }
                    else if (_P1_Y_Value > (1.0f - _CoverDelta))
                    {
                        Send_VK_KeyDown(_CoverBottom_VK);
                        _CoverBottomEnabled = true;
                    }
                    else if (_P1_X_Value > _Axis_X_Max - _CoverDelta)
                    {
                        Send_VK_KeyDown(_CoverRight_VK);
                        _CoverRightEnabled = true;
                    }
                    else if (_P1_Y_Value < (-1.0f + _CoverDelta))
                    {
                        Send_VK_KeyDown(_QTE_W_VK);
                        _QTE_W_Enabled = true;
                    }
                    else
                    {
                        Send_VK_KeyDown(_QTE_Space_VK);
                        _QTE_Spacebar_Enabled = true;
                    }
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {                    
                    if (_CoverLeftEnabled)
                    {
                        Send_VK_KeyUp(_CoverLeft_VK);
                        _CoverLeftEnabled = false;
                    }
                    if (_CoverBottomEnabled)
                    {
                        Send_VK_KeyUp(_CoverBottom_VK);
                        _CoverBottomEnabled = false;
                    }
                    if (_CoverRightEnabled)
                    {
                        Send_VK_KeyUp(_CoverRight_VK);
                        _CoverRightEnabled = false;
                    }
                    if (_QTE_W_Enabled)
                    {
                        Send_VK_KeyUp(_QTE_W_VK);
                        _QTE_W_Enabled = false;
                    }
                    if (_QTE_Spacebar_Enabled)
                    {
                        Send_VK_KeyUp(_QTE_Space_VK);
                        _QTE_Spacebar_Enabled = false;
                    }
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFD);
            }
            else if (PlayerData.ID == 2)
            {
                // Make sure no NULL pointer exception if P2 gamepad is not existing
                if (_EnableP2)
                {
                    //Setting Values in memory for the Codecave to read it
                    byte[] buffer = BitConverter.GetBytes(_P2_X_Value);
                    WriteBytes(_P2_X_CaveAddress, buffer);
                    buffer = BitConverter.GetBytes(_P2_Y_Value);
                    WriteBytes(_P2_Y_CaveAddress, buffer);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        _XOutputManager.SetButton_A(_Player2_X360_Gamepad_Port, true);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        _XOutputManager.SetButton_A(_Player2_X360_Gamepad_Port, false);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    {
                        if (_P2_X_Value < _Axis_X_Min + _CoverDelta)
                        {
                            _XOutputManager.SetRAxis_X(_Player2_X360_Gamepad_Port, -32767);
                        }
                        else if (_P2_Y_Value > (1.0f - _CoverDelta))
                        {
                            _XOutputManager.SetRAxis_Y(_Player2_X360_Gamepad_Port, -32767);
                        }
                        else if (_P2_X_Value > _Axis_X_Max - _CoverDelta)
                        {
                            _XOutputManager.SetRAxis_X(_Player2_X360_Gamepad_Port, 32767);
                        }
                    }
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    {
                        _XOutputManager.SetRAxis_X(_Player2_X360_Gamepad_Port, 0);
                        _XOutputManager.SetRAxis_Y(_Player2_X360_Gamepad_Port, 0);
                    }

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                        _XOutputManager.SetButton_B(_Player2_X360_Gamepad_Port, true);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                        _XOutputManager.SetButton_B(_Player2_X360_Gamepad_Port, false);                    
                }
            }
        }

        /// <summary>
        /// Low-level Keyboard hook callback.
        /// This will be used to use Grenade and simulate P2 direction for menus
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    switch (s.scanCode)
                    {
                        case _P2_UpArrow_ScanCode:
                            {
                                if (_EnableP2)
                                    _XOutputManager.SetDPad_Up(_Player2_X360_Gamepad_Port);
                            } break;
                        case _P2_DownArrow_ScanCode:
                            {
                                if (_EnableP2) 
                                    _XOutputManager.SetDPad_Down(_Player2_X360_Gamepad_Port);
                            } break;
                        case _P1_Grenade_ScanCode:
                            {
                                Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x04);
                            } break;
                        case _P2_Grenade_ScanCode:
                            {
                                if (_EnableP2) 
                                    _XOutputManager.SetButton_L2(_Player2_X360_Gamepad_Port, 0xFF);
                            } break;
                        default:
                            break;
                    }
                }
                else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                {
                    switch (s.scanCode)
                    {
                        case _P2_UpArrow_ScanCode:
                            {
                                if (_EnableP2) 
                                    _XOutputManager.SetDPad_Off(_Player2_X360_Gamepad_Port);
                            } break;
                        case _P2_DownArrow_ScanCode:
                            {
                                if (_EnableP2) 
                                    _XOutputManager.SetDPad_Off(_Player2_X360_Gamepad_Port);
                            } break;
                        case _P1_Grenade_ScanCode:
                            {
                                Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFB);
                            } break;
                        case _P2_Grenade_ScanCode:
                            {
                                if (_EnableP2) 
                                    _XOutputManager.SetButton_L2(_Player2_X360_Gamepad_Port, 0x00);
                            } break;
                        default:
                            break;
                    }
                }
            }
            return Win32API.CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
        }

        #endregion
    }
}
