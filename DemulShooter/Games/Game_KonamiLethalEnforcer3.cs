using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_KonamiLethalEnforcer3 : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\konami\le3";

        /*** MEMORY ADDRESSES **/
        private UInt32 _BaseData_PtrOffset = 0x00294150;
        private UInt32 _TEST_Offset = 0xE43C;
        private UInt32 _P1_X_Offset = 0xE494;
        private UInt32 _P1_Y_Offset = 0xE498;
        private UInt32 _P1_Start_Offset = 0xE446;
        private UInt32 _P2_Start_Offset = 0xE447;
        private UInt32 _P1_Trigger_Offset = 0xE448;
        private UInt32 _P2_Trigger_Offset = 0xE449;
        private NopStruct _Nop_AxisX_1 = new NopStruct(0x0011D5C4, 2);
        private NopStruct _Nop_AxisX_2 = new NopStruct(0x0011D66F, 2);
        private NopStruct _Nop_AxisY_1 = new NopStruct(0x0011D610, 3);
        private NopStruct _Nop_AxisY_2 = new NopStruct(0x0011D67D, 2);
        private UInt32 _BaseData_Address = 0;

        private HardwareScanCode _Test_Key = HardwareScanCode.DIK_9;
        private HardwareScanCode _P1_Start_Key = HardwareScanCode.DIK_1;
        private HardwareScanCode _P2_Start_Key = HardwareScanCode.DIK_2;


        /// <summary>
        /// Constructor
        /// </summary>
        public Game_KonamiLethalEnforcer3(String RomName, bool DisableInputHack, bool Verbose) 
            : base (RomName, "game_patched", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Lethal Enforcer 3 v2005-04-15-1 - Original game.exe", "1d338c452c7b087bc7aad823a74bb023");
            _KnownMd5Prints.Add("Lethal Enforcer 3 v2005-04-15-1 - DemulShooter compatible game.exe", "ce1359efe2dde0ac02280bfe8c96681d");
        
            _tProcess.Start();
            Logger.WriteLog("Waiting for KONAMI " + _RomName + " game to hook.....");
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
                            _BaseData_Address = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _BaseData_PtrOffset);

                            if (_BaseData_Address != 0)
                            {
                                Logger.WriteLog("Data Pointer Address = 0x" + _BaseData_Address.ToString("X8"));
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                CheckExeMd5();
                                ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);

                                if (!_DisableInputHack)
                                {
                                    SetHack();
                                }
                                else
                                    Logger.WriteLog("Input Hack disabled");

                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
                            }
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
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    double dMaxX = 640.0;
                    double dMaxY = 480.0;

                    //Inverted Axis : 0 = bottom right
                    PlayerData.RIController.Computed_X = Convert.ToInt32(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt32(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));
                    if (PlayerData.RIController.Computed_X < 0)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 0)
                        PlayerData.RIController.Computed_Y = 0;
                    if (PlayerData.RIController.Computed_X > (int)dMaxX)
                        PlayerData.RIController.Computed_X = (int)dMaxX;
                    if (PlayerData.RIController.Computed_Y > (int)dMaxY)
                        PlayerData.RIController.Computed_Y = (int)dMaxY;

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

        /// <summary>
        /// Genuine Hack, just blocking Axis and filtering Triggers input to replace them without blocking other input
        /// </summary>
        private void SetHack()
        {
            //NOPing axis proc
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_AxisX_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_AxisX_2);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_AxisY_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_AxisY_1);

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>  
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((float)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((float)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes(_BaseData_Address + _P1_X_Offset, bufferX);
                WriteBytes(_BaseData_Address + _P1_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    SetButtonBistable(_P1_Trigger_Offset);
                    WriteByte(_BaseData_Address + _P1_Trigger_Offset - 0x06, 0x01);   //this byte show event in TEST mode but not used in game ??
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte(_BaseData_Address + _P1_Trigger_Offset - 0x06, 0x00);   //this byte show event in TEST mode but not used in game ??

                /*if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFD);*/
            }            
        }

        //On this game a button event changes the byte value (we can do it by cycling between 1/0)
        private void SetButtonBistable(UInt32 ButtonOffset)
        {
            byte ButtonValue = ReadByte(_BaseData_Address + ButtonOffset);
            WriteByte(_BaseData_Address + ButtonOffset, (byte)(1 - ButtonValue));
        }

        /// <summary>
        /// Low-level Keyboard hook callback.
        /// This is used to detect Pedal action for "Pedal-Mode" hack of DemulShooter
        /// </summary>
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    if (s.scanCode == _P1_Start_Key)
                    {
                        SetButtonBistable(_P1_Start_Offset);
                        WriteByte(_BaseData_Address + _P1_Start_Offset - 0x06, 0x01);   //this byte show event in TEST mode but not used in game ??
                    }
                    else if (s.scanCode == _P2_Start_Key)
                    {
                        SetButtonBistable(_P2_Start_Offset);
                        WriteByte(_BaseData_Address + _P2_Start_Offset - 0x06, 0x01);   //this byte show event in TEST mode but not used in game ??
                    }
                    else if (s.scanCode == _Test_Key)
                    {
                        WriteByte(_BaseData_Address + _TEST_Offset, 0x01);   //this byte show event in TEST mode but not used in game ??
                    }
                }
                else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                {
                    if (s.scanCode == _P1_Start_Key)
                    {
                        WriteByte(_BaseData_Address + _P1_Start_Offset - 0x06, 0x00);   //this byte show event in TEST mode but not used in game ??
                    }
                    else if (s.scanCode == _P2_Start_Key)
                    {
                        WriteByte(_BaseData_Address + _P2_Start_Offset - 0x06, 0x00);   //this byte show event in TEST mode but not used in game ??
                    }
                    else if (s.scanCode == _Test_Key)
                    {
                        WriteByte(_BaseData_Address + _TEST_Offset, 0x00);   //this byte show event in TEST mode but not used in game ??
                    }
                }
            }
            return Win32API.CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
        }


        #endregion
    }
}
