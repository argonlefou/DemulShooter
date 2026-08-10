using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_RwTargetBravo : Game
    {
        private const string GAMEDATA_FOLDER = @"MemoryData\ringwide\tb";

        /*** MEMORY ADDRESSES **/
        private UInt32 _JvsEnabled_Offset = 0x247344;
        private UInt32 _AmLibData_Ptr_Offset = 0x02671580;
        private UInt32 _AmLibData_BaseAddress = 0;
        private UInt32 _GetKeyboardState_Function_Offset = 0x001E735C;

        //JVS emulation mode (TEKNOPARROT + Jconfig)
        //JVS checksum @5992D7 = SUM(64681F:64683E) -> LowNibble in 64683F     
        //Buttons injection will be made at the source of JVS data and need to remove checksum
        //Axis injection will be made after in-game calculation, as we can't access/save calibration from test menu
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _JvsRemoveChecksum_Offset = 0x001A1262;
        private UInt32 _Jvs_ButtonsInjection_Offset = 0x001B72C0;
        private UInt32 _Jvs_Data_Ptr_Offset = 0x026669C0;   //For P2, use [265C208]+0x74, or real thing is [265C20C]+0x18
        private UInt32 _Jvs_Data_BaseAddress = 0;
        private NopStruct _Nop_Axix_X_1 = new NopStruct(0x000A186F, 3);
        private NopStruct _Nop_Axix_X_2 = new NopStruct(0x000A189D, 3);
        private NopStruct _Nop_Axix_Y_1 = new NopStruct(0x000A190D, 3);
        private NopStruct _Nop_Axix_Y_2 = new NopStruct(0x000A18D7, 3);

        //Hardware hardcoded keys to emulate TEST and SERVICE buttons to move in TestMode, although the game won't save changes
        private HardwareScanCode _TestButton = HardwareScanCode.DIK_8;
        private HardwareScanCode _ServiceButton = HardwareScanCode.DIK_9;

        //For Non JVS axis with the same axis hack, add :
        private NopStruct _Nop_Axix_X_3 = new NopStruct(0x000A1927, 3);
        private NopStruct _Nop_Axix_Y_3 = new NopStruct(0x000A192A, 3);

        //DirectInput mode (no JVS emulation)        
        private UInt32 _Axis_Address_Ptr_Offset = 0x026669C4;
        private UInt32 _P2_X_Address;
        private UInt32 _P2_Y_Address;
        private NopStruct _Nop_Axis_X = new NopStruct(0x000A1A84, 3);
        private NopStruct _Nop_Axis_Y = new NopStruct(0x000A1A62, 3);
        private InjectionStruct _Buttons_InjectionStruct = new InjectionStruct(0x000A2906, 6);
        private InjectionStruct _Axis_InjectionStruct = new InjectionStruct(0x000A195E, 6);

        //Outputs
        private UInt32 _JVS_Outputs_Offset = 0x00250B38;
        private UInt32 _InternalLedOutputs_Ptr_Offset = 0x006669AC;
        private InjectionStruct _InternalRecoil_InjectionStruct = new InjectionStruct(0x000318D0, 5);

        //Custom Data
        private UInt32 _P1_X_CaveAddress;
        private UInt32 _P1_Y_CaveAddress;
        private UInt32 _P1_Trigger_CaveAddress;
        private UInt32 _P1_Action_CaveAddress;
        private UInt32 _P1_Change_CaveAddress;
        private UInt32 _P1_Reload_CaveAddress;
        private UInt32 _Recoil_CaveAddress;

        //Keys (no JVS emulation)
        //START_P2 = NumPad +
        //START_P1 = ENTER
        //Service = Y
        private VirtualKeyCode _P2_Trigger_VK = VirtualKeyCode.VK_NUMPAD5;
        private VirtualKeyCode _P2_Reload_VK = VirtualKeyCode.VK_NUMPAD0;
        private VirtualKeyCode _P2_Change_VK = VirtualKeyCode.VK_DECIMAL;
        private VirtualKeyCode _P2_Action_VK = VirtualKeyCode.VK_SUBSTRACT;

        // Test
        private bool _P2OutOfScreen = false;

        //JVS emulation detection
        private bool _IsJvsEnabled = false;

        //Credits settings (these are defaults values)
        private int _Credits_Freeplay = 0;   //0 or 1

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_RwTargetBravo(String RomName)
            : base(RomName, "gs2")
        {
            if (Configurator.GetInstance().OpGhost_EnableFreeplay)
                _Credits_Freeplay = 1;
            _KnownMd5Prints.Add("Target Bravo - original", "a93befe8fa764059b96296f2d40bb5db");
            _KnownMd5Prints.Add("Target Bravo - 1080p patched", "3b508229088a65baf0329469e1aedab3");
            _tProcess.Start();

            Logger.WriteLog("Waiting for RingWide " + _RomName + " game to hook.....");
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
                        _AmLibData_BaseAddress = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _AmLibData_Ptr_Offset);
                        //Modifying Credits parameters :
                        WriteBytes(_AmLibData_BaseAddress + 0x1C8, BitConverter.GetBytes(_Credits_Freeplay));
                        WriteBytes(_AmLibData_BaseAddress + 0x1CC, BitConverter.GetBytes(Configurator.GetInstance().OpGhost_CreditsToStart));
                        WriteBytes(_AmLibData_BaseAddress + 0x1D0, BitConverter.GetBytes(Configurator.GetInstance().OpGhost_CreditsToContinue));
                        WriteBytes(_AmLibData_BaseAddress + 0x1D4, BitConverter.GetBytes(Configurator.GetInstance().OpGhost_CoinsPerCredits));

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero && _AmLibData_BaseAddress != 0)
                        {
                            if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _JvsEnabled_Offset) == 1)
                            {
                                _IsJvsEnabled = true;
                                _Jvs_Data_BaseAddress = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _Jvs_Data_Ptr_Offset);
                                if (_Jvs_Data_BaseAddress != 0)
                                {
                                    _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                    Logger.WriteLog("AmLib data base address = 0x" + _AmLibData_BaseAddress.ToString("X8"));
                                    Logger.WriteLog("JVS emulation detected");
                                    Logger.WriteLog("JVS axis data pointer base address = 0x" + _Jvs_Data_BaseAddress.ToString("X8"));
                                    CheckExeMd5();
                                    ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                                    //if (!_DisableInputHack)
                                    //    SetHack_Jvs();
                                    //else
                                    //    Logger.WriteLog("Input Hack disabled");
                                    _ProcessHooked = true;
                                    RaiseGameHookedEvent();
                                }
                            }
                            else
                            {
                                byte[] buffer = ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Axis_Address_Ptr_Offset, 4);
                                UInt32 Calc_Addr = BitConverter.ToUInt32(buffer, 0);

                                if (Calc_Addr != 0)
                                {
                                    _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                    _P2_X_Address = Calc_Addr + 0x28;
                                    _P2_Y_Address = Calc_Addr + 0x2C;

                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                    Logger.WriteLog("AmLib data base address = 0x" + _AmLibData_BaseAddress.ToString("X8"));
                                    //Logger.WriteLog("P1_X adddress =  0x" + _P1_X_Address.ToString("X8"));
                                    //Logger.WriteLog("P1_Y adddress =  0x" + _P1_Y_Address.ToString("X8"));
                                    Logger.WriteLog("P2_X adddress =  0x" + _P2_X_Address.ToString("X8"));
                                    Logger.WriteLog("P2_Y adddress =  0x" + _P2_Y_Address.ToString("X8"));
                                    CheckExeMd5();
                                    ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                                    Apply_MemoryHacks();
                                    _ProcessHooked = true;
                                    RaiseGameHookedEvent();
                                }
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
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X => [0-1024]
                    //Y => [0-600]
                    double dMaxX = 1024.0;
                    double dMaxY = 600.0;

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

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _P1_Trigger_CaveAddress = _InputsDatabank_Address;
            _P1_Reload_CaveAddress = _InputsDatabank_Address + 0x10;
            _P1_Change_CaveAddress = _InputsDatabank_Address + 0x01;
            _P1_Action_CaveAddress = _InputsDatabank_Address + 0x03;
            _P1_X_CaveAddress = _InputsDatabank_Address + 0x20;
            _P1_Y_CaveAddress = _InputsDatabank_Address + 0x24;

            SetHack_Buttons();
            SetHack_Axis();

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// For this hack we will wait the GetKeyboardState call
        /// And immediately after we will read on our custom memory storage
        /// to replace lpKeystate bytes for mouse buttons (see WINUSER.H for virtualkey codes)
        /// then the game will continue...    
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //call USER32.GetKEyboardState
            CaveMemory.Write_StrBytes("FF 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _GetKeyboardState_Function_Offset));
            //lpkeystate is in ESP register at that point :
            //and [esp + 1], 0x00FF0000
            CaveMemory.Write_StrBytes("81 64 24 01 00 00 FF 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [_P1_Trigger_Address]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Trigger_CaveAddress));
            //We pushed eax so ESP was changed, so now lpkeystate is in. ESP+1+4
            //or [esp + 5], eax
            CaveMemory.Write_StrBytes("09 44 24 05");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Trigger");
            Logger.WriteLog("Adding Trigger CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
        }

        /// <summary>
        /// For this hack we will override the writing of X and Y data issued from
        /// the legit ScrenToClient call, with our own calculated values
        /// </summary>
        private void SetHack_Axis()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //mov ecx, [_P1_X_Address]
            CaveMemory.Write_StrBytes("8B 0D");
            byte[] b = BitConverter.GetBytes(_P1_X_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov edx, [_P1_Y_Address]
            CaveMemory.Write_StrBytes("8B 15");
            b = BitConverter.GetBytes(_P1_Y_CaveAddress);
            CaveMemory.Write_Bytes(b);

            //Inject it
            CaveMemory.InjectToOffset(_Axis_InjectionStruct, "Axis");

            //Noping procedures for P2
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Axis_X);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Axis_Y);

            //Center Crosshair at start
            byte[] bufferX = { 0x00, 0x02, 0, 0 };  //512
            byte[] bufferY = { 0x2C, 0x01, 0, 0 };  //300
            WriteBytes(_P1_X_CaveAddress, bufferX);
            WriteBytes(_P1_Y_CaveAddress, bufferY);
            WriteBytes(_P2_X_Address, bufferX);
            WriteBytes(_P2_Y_Address, bufferY);
        }

        /// <summary>
        /// OutputsHack will only be needed when JVS is disabled to get recoil from internal calls
        /// </summary>
        protected override void Apply_OutputsMemoryHack()
        {
            if (!_IsJvsEnabled)
            {
                //Create Databak to store our value
                Create_OutputsDataBank();
                _Recoil_CaveAddress = _OutputsDatabank_Address;

                SetHack_Recoil();

                Logger.WriteLog("Outputs Memory Hack complete !");
                Logger.WriteLog("-");
            }
        }

        /// <summary>
        /// In that function 'RequestRecoil()' the game calls a procedure to get the WeaponManager corresponding to the event
        /// When JVS emulation is enabled, it returns 3(P1) or 4(P2)
        /// When JVS emulation is disabled, it returns 1(P1) or 2(P2)
        /// When JVS emulation is enabled, output can be obtained by the JVS report bytes, so this injection will only be needed for non-JVS
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov eax,[eax]
            CaveMemory.Write_StrBytes("8B 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //sub eax,01
            CaveMemory.Write_StrBytes("83 E8 01");
            //add eax,_Recoil_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Recoil_CaveAddress));
            //mov byte ptr [eax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //sub eax,03
            CaveMemory.Write_StrBytes("83 E8 03");

            //Inject it
            CaveMemory.InjectToOffset(_InternalRecoil_InjectionStruct, "Recoil");
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary> 
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((UInt16)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((UInt16)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                if (_IsJvsEnabled)
                {
                    WriteBytes(_Jvs_Data_BaseAddress + 0x18, bufferX);
                    WriteBytes(_Jvs_Data_BaseAddress + 0x1C, bufferY);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        Apply_OR_ByteMask(_Buttons_CaveAddress, 0x02);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFD);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        Apply_OR_ByteMask(_Buttons_CaveAddress + 1, 0x40);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        Apply_AND_ByteMask(_Buttons_CaveAddress + 1, 0xBF);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    {
                        Apply_OR_ByteMask(_Buttons_CaveAddress, 0x01);
                        if (!Configurator.GetInstance().OpGhost_SeparateButtons)
                            Apply_OR_ByteMask(_Buttons_CaveAddress + 1, 0x80);
                    }
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    {
                        Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFE);
                        if (!Configurator.GetInstance().OpGhost_SeparateButtons)
                            Apply_AND_ByteMask(_Buttons_CaveAddress + 1, 0x7F);
                    }
                }
                else
                {
                    WriteBytes(_P1_X_CaveAddress, bufferX);
                    WriteBytes(_P1_Y_CaveAddress, bufferY);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        WriteByte(_P1_Trigger_CaveAddress, 0x80);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        WriteByte(_P1_Trigger_CaveAddress, 0x00);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        WriteByte(_P1_Change_CaveAddress, 0x80);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        WriteByte(_P1_Change_CaveAddress, 0x00);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    {
                        if (!Configurator.GetInstance().OpGhost_SeparateButtons)
                            WriteByte(_P1_Action_CaveAddress, 0x80);
                        PlayerData.RIController.Computed_X = 2000;
                        byte[] bufferX_R = { (byte)(PlayerData.RIController.Computed_X & 0xFF), (byte)(PlayerData.RIController.Computed_X >> 8), 0, 0 };
                        WriteBytes(_P1_X_CaveAddress, bufferX_R);
                        System.Threading.Thread.Sleep(20);
                    }
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    {
                        if (!Configurator.GetInstance().OpGhost_SeparateButtons)
                            WriteByte(_P1_Action_CaveAddress, 0x00);
                    }
                }
            }
            else if (PlayerData.ID == 2)
            {
                if (_IsJvsEnabled)
                {
                    //JVS Axis
                    WriteBytes(_Jvs_Data_BaseAddress + 0x74, bufferX);
                    WriteBytes(_Jvs_Data_BaseAddress + 0x78, bufferY);

                    //JVS Inputs
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        Apply_OR_ByteMask(_Buttons_CaveAddress + 2, 0x02);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        Apply_AND_ByteMask(_Buttons_CaveAddress + 2, 0xFD);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        Apply_OR_ByteMask(_Buttons_CaveAddress + 3, 0x40);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        Apply_AND_ByteMask(_Buttons_CaveAddress + 3, 0xBF);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    {
                        Apply_OR_ByteMask(_Buttons_CaveAddress + 2, 0x01);
                        if (!Configurator.GetInstance().OpGhost_SeparateButtons)
                            Apply_OR_ByteMask(_Buttons_CaveAddress + 3, 0x80);
                    }
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    {
                        Apply_AND_ByteMask(_Buttons_CaveAddress + 2, 0xFE);
                        if (!Configurator.GetInstance().OpGhost_SeparateButtons)
                            Apply_AND_ByteMask(_Buttons_CaveAddress + 3, 0x7F);
                    }
                }
                else
                {
                    WriteBytes(_P2_X_Address, bufferX);
                    WriteBytes(_P2_Y_Address, bufferY);

                    //P2 uses keyboard so no autoreload when out of screen, so we add:
                    if (PlayerData.RIController.Computed_X <= 1 || PlayerData.RIController.Computed_X >= 1022 || PlayerData.RIController.Computed_Y <= 1 || PlayerData.RIController.Computed_Y >= 596)
                    {
                        if (!_P2OutOfScreen)
                        {
                            Send_VK_KeyDown(_P2_Reload_VK);
                            _P2OutOfScreen = true;
                        }
                    }
                    else
                    {
                        if (_P2OutOfScreen)
                        {
                            Send_VK_KeyUp(_P2_Reload_VK);
                            _P2OutOfScreen = false;
                        }
                    }

                    //Inputs
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        Send_VK_KeyDown(_P2_Trigger_VK);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        Send_VK_KeyUp(_P2_Trigger_VK);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        Send_VK_KeyDown(_P2_Change_VK);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        Send_VK_KeyUp(_P2_Change_VK);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    {
                        Send_VK_KeyDown(_P2_Reload_VK);
                        if (!Configurator.GetInstance().OpGhost_SeparateButtons)
                            Send_VK_KeyDown(_P2_Action_VK);
                    }
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    {
                        Send_VK_KeyUp(_P2_Reload_VK);
                        if (!Configurator.GetInstance().OpGhost_SeparateButtons)
                            Send_VK_KeyUp(_P2_Action_VK);
                    }
                }
            }
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
                    if (_IsJvsEnabled)
                    {
                        if (s.scanCode == _TestButton)
                        {
                            Apply_OR_ByteMask(_Buttons_CaveAddress, 0x40);
                        }
                        else if (s.scanCode == _ServiceButton)
                        {
                            Apply_OR_ByteMask(_Buttons_CaveAddress + 4, 0x80);
                        }
                        if (Configurator.GetInstance().OpGhost_SeparateButtons)
                        {
                            if (s.scanCode == Configurator.GetInstance().DIK_OpGhost_Action_P1)
                            {
                                Apply_OR_ByteMask(_Buttons_CaveAddress + 1, 0x80);
                            }
                            else if (s.scanCode == Configurator.GetInstance().DIK_OpGhost_Action_P2)
                            {
                                Apply_OR_ByteMask(_Buttons_CaveAddress + 3, 0x80);
                            }
                        }
                    }
                    else
                    {
                        if (Configurator.GetInstance().OpGhost_SeparateButtons)
                        {
                            if (s.scanCode == Configurator.GetInstance().DIK_OpGhost_Action_P1)
                            {
                                WriteByte(_P1_Action_CaveAddress, 0x80);
                            }
                            else if (s.scanCode == Configurator.GetInstance().DIK_OpGhost_Action_P2)
                            {
                                Send_VK_KeyDown(_P2_Action_VK);
                            }
                        }
                    }
                }
                else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                {
                    if (_IsJvsEnabled)
                    {
                        if (s.scanCode == _TestButton)
                        {
                            Apply_AND_ByteMask(_Buttons_CaveAddress, 0xBF);
                        }
                        else if (s.scanCode == _ServiceButton)
                        {
                            Apply_AND_ByteMask(_Buttons_CaveAddress + 4, 0x7F);
                        }
                        if (Configurator.GetInstance().OpGhost_SeparateButtons)
                        {
                            if (s.scanCode == Configurator.GetInstance().DIK_OpGhost_Action_P1)
                            {
                                Apply_AND_ByteMask(_Buttons_CaveAddress + 1, 0x7F);
                            }
                            else if (s.scanCode == Configurator.GetInstance().DIK_OpGhost_Action_P2)
                            {
                                Apply_AND_ByteMask(_Buttons_CaveAddress + 3, 0x7F);
                            }
                        }
                    }
                    else
                    {
                        if (Configurator.GetInstance().OpGhost_SeparateButtons)
                        {
                            if (s.scanCode == Configurator.GetInstance().DIK_OpGhost_Action_P1)
                            {
                                WriteByte(_P1_Action_CaveAddress, 0x00);
                            }
                            else if (s.scanCode == Configurator.GetInstance().DIK_OpGhost_Action_P2)
                            {
                                Send_VK_KeyUp(_P2_Action_VK);
                            }
                        }
                    }
                }
            }
            return Win32API.CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : stays activated when trigger is pulled
            //Gun recoil : not used ??
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputId.LmpBillboard));
            _Outputs.Add(new GameOutput(OutputId.P1_LmpHolder));
            _Outputs.Add(new GameOutput(OutputId.P2_LmpHolder));
            _Outputs.Add(new GameOutput(OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputId.P2_GunRecoil));
            _Outputs.Add(new AsyncGameOutput(OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            if (_IsJvsEnabled)
            {
                byte bOutput = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _JVS_Outputs_Offset);
                SetOutputValue(OutputId.P1_LmpStart, bOutput >> 7 & 0x01);
                SetOutputValue(OutputId.P2_LmpStart, bOutput >> 4 & 0x01);
                SetOutputValue(OutputId.LmpBillboard, bOutput >> 5 & 0x01);
                SetOutputValue(OutputId.P1_LmpHolder, bOutput >> 1 & 0x01);
                SetOutputValue(OutputId.P2_LmpHolder, bOutput & 0x01);
                SetOutputValue(OutputId.P1_GunRecoil, bOutput >> 6 & 0x01);
                SetOutputValue(OutputId.P2_GunRecoil, bOutput >> 3 & 0x01);

                //Custom recoil will be enabled just like original recoil
                SetOutputValue(OutputId.P1_CtmRecoil, bOutput >> 6 & 0x01);
                SetOutputValue(OutputId.P2_CtmRecoil, bOutput >> 3 & 0x01);
            }
            else
            {
                //ReadPtr() + 15E0 = Outputs
                //1 Byte per output (0 / 1)
                // +0 = Billboard
                // +1 = P1 Holder
                // +2 = P2 Holder
                // +3 = Unused --
                // +4 = P1 START
                // +5 = P2 START
                UInt32 LedStatusAddress = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _InternalLedOutputs_Ptr_Offset) + 0x15E0;
                SetOutputValue(OutputId.P1_LmpStart, ReadByte(LedStatusAddress + 4));
                SetOutputValue(OutputId.P2_LmpStart, ReadByte(LedStatusAddress + 5));
                SetOutputValue(OutputId.LmpBillboard, ReadByte(LedStatusAddress));
                SetOutputValue(OutputId.P1_LmpHolder, ReadByte(LedStatusAddress + 1));
                SetOutputValue(OutputId.P2_LmpHolder, ReadByte(LedStatusAddress + 2));

                if (ReadByte(_Recoil_CaveAddress) == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);
                    WriteByte(_Recoil_CaveAddress, 0x00);
                }

                if (ReadByte(_Recoil_CaveAddress + 1) == 1)
                {
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);
                    WriteByte(_Recoil_CaveAddress + 1, 0x00);
                }
            }                

            //Credits will be calculated by using the formula  : Credits = Coins / CoinsByCredits
            //Warning : Need to handle "Divide by 0" error if game is closed brutally ! 
            int Credits = 0;
            try
            {
                Credits = ReadByte((UInt32)_AmLibData_BaseAddress + 0x1E0) / ReadByte((UInt32)_AmLibData_BaseAddress + 0x1D4);
            }
            catch { }
            SetOutputValue(OutputId.Credits, Credits);
        }

        #endregion
    }
}
