using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;
using System.Runtime.InteropServices;

namespace DemulShooter
{
    class Game_RwOpGhost : Game
    {
        private const string GAMEDATA_FOLDER = @"MemoryData\ringwide\og";

        /*** MEMORY ADDRESSES **/
        private UInt32 _JvsEnabled_Offset = 0x23D2E7;
        private UInt32 _AmLibData_Ptr_Offset = 0x026662C0;
        private UInt32 _AmLibData_BaseAddress = 0;
                
        //JVS emulation mode (TEKNOPARROT + Jconfig)
        //JVS checksum @5992D7 = SUM(64681F:64683E) -> LowNibble in 64683F     
        //Buttons injection will be made at the source of JVS data and need to remove checksum
        //Axis injection will be made after in-game calculation, as we can't access/save calibration from test menu
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _JvsRemoveChecksum_Offset = 0x001992F2;
        private UInt32 _Jvs_ButtonsInjection_Offset = 0x001AF1E0;
        private NopStruct _Nop_Axix_X_1 = new NopStruct(0x0009DE8F, 3);
        private NopStruct _Nop_Axix_X_2 = new NopStruct(0x0009DEBD, 3);
        private NopStruct _Nop_Axix_Y_1 = new NopStruct(0x0009DF2D, 3);
        private NopStruct _Nop_Axix_Y_2 = new NopStruct(0x0009DEF7, 3);
        private UInt32 _Jvs_Data_Ptr_Offset = 0x0265C208;   //For P2, use [265C208]+0x74, or real thing is [265C20C]+0x18
        private UInt32 _Jvs_Data_BaseAddress = 0;
        //Hardware hardcoded keys to emulate TEST and SERVICE buttons to move in TestMode, although the game won't save changes
        private HardwareScanCode _TestButton = HardwareScanCode.DIK_8;
        private HardwareScanCode _ServiceButton = HardwareScanCode.DIK_9;

        //For Non JVS axis with the same axis hack, add :
        private NopStruct _Nop_Axix_X_3 = new NopStruct(0x0009DF47, 3);
        private NopStruct _Nop_Axix_Y_3 = new NopStruct(0x0009DF4A, 3);

        //DirectInput mode (no JVS emulation)
        private UInt32 _P1_X_CaveAddress;
        private UInt32 _P1_Y_CaveAddress;
        private UInt32 _Axis_Address_Ptr_Offset = 0x0265C20C;
        private UInt32 _P2_X_Address;
        private UInt32 _P2_Y_Address;
        private NopStruct _Nop_Axis_X = new NopStruct(0x0009E0A4, 3);
        private NopStruct _Nop_Axis_Y = new NopStruct(0x0009E082, 3);
        private UInt32 _P1_Trigger_CaveAddress;
        private UInt32 _P1_Action_CaveAddress;
        private UInt32 _P1_Change_CaveAddress;
        private UInt32 _P1_Reload_CaveAddress;
        private UInt32 _Buttons_Injection_Offset = 0x0009EF26;
        private UInt32 _Buttons_Injection_Return_Offset = 0x0009EF2C;
        private UInt32 _Axis_Injection_Offset = 0x0009DF7E;
        private UInt32 _Axis_Injection_Return_Offset = 0x0009DF84;

        //Outputs
        private UInt32 _Outputs_Offset = 0x00246428;
        
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
        private int _Credits_CreditsToStart = 2;
        private int _Credits_CreditsToContinue = 1;
        private int _Credits_CoinsByCredits = 2;

        //Separating Action button
        private bool _SeparateActionButton = false;
        private HardwareScanCode _P1_Action_Key = HardwareScanCode.DIK_G;
        private HardwareScanCode _P2_Action_Key = HardwareScanCode.DIK_H;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_RwOpGhost(String RomName, bool EnableFreeplay, int StartCredits, int ContinueCredits, int CoinsCredit, bool SeparateButtons, HardwareScanCode P1_Action_Key, HardwareScanCode P2_Action_Key, bool DisableInputHack, bool Verbose)
            : base(RomName, "gs2", DisableInputHack, Verbose)
        {
            if (EnableFreeplay)
                _Credits_Freeplay = 1;
            _Credits_CreditsToStart = StartCredits;
            _Credits_CreditsToContinue = ContinueCredits;
            _Credits_CoinsByCredits = CoinsCredit;
            _SeparateActionButton = SeparateButtons;
            _P1_Action_Key = P1_Action_Key;
            _P2_Action_Key = P2_Action_Key;
            _KnownMd5Prints.Add("Operation Ghost - For TeknoParrot", "40f795933abc4f441c98acc778610aa2");
            _KnownMd5Prints.Add("Operation Ghost - For JConfig", "19a949581145ed8478637d286a4b85a0");
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
                        WriteBytes(_AmLibData_BaseAddress + 0x1CC, BitConverter.GetBytes(_Credits_CreditsToStart));
                        WriteBytes(_AmLibData_BaseAddress + 0x1D0, BitConverter.GetBytes(_Credits_CreditsToContinue));
                        WriteBytes(_AmLibData_BaseAddress + 0x1D4, BitConverter.GetBytes(_Credits_CoinsByCredits));

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero && _AmLibData_BaseAddress != 0)
                        {
                            if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _JvsEnabled_Offset) == 1)
                            {
                                _IsJvsEnabled = true;
                                _Jvs_Data_BaseAddress = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _Jvs_Data_Ptr_Offset);
                                if (_Jvs_Data_BaseAddress != 0)
                                {
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                    Logger.WriteLog("AmLib data base address = 0x" + _AmLibData_BaseAddress.ToString("X8"));
                                    Logger.WriteLog("JVS emulation detected");
                                    Logger.WriteLog("JVS axis data pointer base address = 0x" + _Jvs_Data_BaseAddress.ToString("X8"));
                                    CheckExeMd5();
                                    if (!_DisableInputHack)
                                        SetHack_Jvs();
                                    else
                                        Logger.WriteLog("Input Hack disabled");
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
                                    if (!_DisableInputHack)
                                        SetHack();
                                    else
                                        Logger.WriteLog("Input Hack disabled");
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
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

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

        private void SetHack()
        {
            CreateDataBank();
            SetHack_Buttons();
            SetHack_Axis();
        }

        /// <summary>
        /// 1st Memory created to store custom button data
        /// This memory will be read by the codecave to overwrite the GetKeystate API results
        /// And by the other codecave to overwrite mouse axis value
        /// </summary>
        private void CreateDataBank()
        {        
            Codecave InputMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            InputMemory.Open();
            InputMemory.Alloc(0x800);
            _P1_Trigger_CaveAddress = InputMemory.CaveAddress;
            _P1_Reload_CaveAddress = InputMemory.CaveAddress + 0x10;
            _P1_Change_CaveAddress = InputMemory.CaveAddress + 0x01;
            _P1_Action_CaveAddress = InputMemory.CaveAddress + 0x03;
            _P1_X_CaveAddress = InputMemory.CaveAddress + 0x20;
            _P1_Y_CaveAddress = InputMemory.CaveAddress + 0x24;
            Logger.WriteLog("Custom Axis data will be stored at : 0x" + _P1_Trigger_CaveAddress.ToString("X8"));
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
            byte[] b = BitConverter.GetBytes((int)_TargetProcess_MemoryBaseAddress + 0x001DF304);
            CaveMemory.Write_Bytes(b);
            //lpkeystate is in ESP register at that point :
            //and [esp + 1], 0x00FF0000
            CaveMemory.Write_StrBytes("81 64 24 01 00 00 FF 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [_P1_Trigger_Address]
            CaveMemory.Write_StrBytes("A1");
            b = BitConverter.GetBytes(_P1_Trigger_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //We pushed eax so ESP was changed, so now lpkeystate is in. ESP+1+4
            //or [esp + 5], eax
            CaveMemory.Write_StrBytes("09 44 24 05");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Return_Offset);

            Logger.WriteLog("Adding Trigger CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
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
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _Axis_Injection_Return_Offset);

            Logger.WriteLog("Adding Axis CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _Axis_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _Axis_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

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

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");

            //Win32.keybd_event(Win32.VK_NUMLOCK, 0x45, Win32.KEYEVENTF_EXTENDEDKEY | 0, 0);
        }

        #endregion

        #region Memory Hack for JVS

        private void SetHack_Jvs()
        {
            CreateDataBank_Jvs();

            //NOPing axis instructions
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Axix_X_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Axix_X_2);
            //SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Axix_X_3);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Axix_Y_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Axix_Y_2);
            //SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Axix_Y_3);

            //Hacking buttons values in JVS data source
            //Seems like there is a checksum verification for JVS data integrity, so we will remove that
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _JvsRemoveChecksum_Offset, new byte[] { 0xEB, 0x11, 0x90, 0x90, 0x90, 0x90, 0x90 });

            //Now we can filter and modify buttons values according to what we need to send
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //and [646829], 00800080
            CaveMemory.Write_StrBytes("81 25 29 68 64 00 80 00 80 00");
            //mov eax, [_Buttons]
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_Buttons_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //or [646829], eax
            CaveMemory.Write_StrBytes("09 05 29 68 64 00");
            
            //mov eax [_ButtonsTestAddress]
            CaveMemory.Write_StrBytes("A1");
            b = BitConverter.GetBytes(_Buttons_CaveAddress + 4);
            CaveMemory.Write_Bytes(b);
            //or byte ptr[646828], al
            CaveMemory.Write_StrBytes("08 05 28 68 64 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //jmp dword ptr [edx*4+gs2.exe+1AF274]
            CaveMemory.Write_StrBytes("FF 24 95 74 F2 5A 00");

            Logger.WriteLog("Adding JVS Buttons CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _Jvs_ButtonsInjection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _Jvs_ButtonsInjection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// 1st Memory created to store custom buttons data
        /// This memory will be read by the codecave to overwrite the original data read from EAX
        /// </summary>
        private void CreateDataBank_Jvs()
        {
            Codecave InputMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            InputMemory.Open();
            InputMemory.Alloc(0x800);            
            _Buttons_CaveAddress = InputMemory.CaveAddress;            
            
            Logger.WriteLog("Custom JVS Buttons data will be stored at : 0x" + _P1_X_CaveAddress.ToString("X8"));
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
                        if (!_SeparateActionButton)
                            Apply_OR_ByteMask(_Buttons_CaveAddress + 1, 0x80);                       
                    }
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    {
                        Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFE);
                        if (!_SeparateActionButton)
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
                        if (!_SeparateActionButton)
                            WriteByte(_P1_Action_CaveAddress, 0x80);
                        PlayerData.RIController.Computed_X = 2000;
                        byte[] bufferX_R = { (byte)(PlayerData.RIController.Computed_X & 0xFF), (byte)(PlayerData.RIController.Computed_X >> 8), 0, 0 };
                        WriteBytes(_P1_X_CaveAddress, bufferX_R);
                        System.Threading.Thread.Sleep(20);
                    }
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    {
                        if (!_SeparateActionButton)
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
                        if (!_SeparateActionButton)
                            Apply_OR_ByteMask(_Buttons_CaveAddress + 3, 0x80);
                    }
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    {
                        Apply_AND_ByteMask(_Buttons_CaveAddress + 2, 0xFE);
                        if (!_SeparateActionButton)
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
                        if (!_SeparateActionButton)
                            Send_VK_KeyDown(_P2_Action_VK);
                    }
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    {
                        Send_VK_KeyUp(_P2_Reload_VK);
                        if (!_SeparateActionButton)
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
                        if (_SeparateActionButton)
                        {
                            if (s.scanCode == _P1_Action_Key)
                            {
                                Apply_OR_ByteMask(_Buttons_CaveAddress + 1, 0x80);
                            }
                            else if (s.scanCode == _P2_Action_Key)
                            {
                                Apply_OR_ByteMask(_Buttons_CaveAddress + 3, 0x80);
                            }
                        }
                    }
                    else
                    {
                        if (_SeparateActionButton)
                        {
                            if (s.scanCode == _P1_Action_Key)
                            {
                                WriteByte(_P1_Action_CaveAddress, 0x80);
                            }
                            else if (s.scanCode == _P2_Action_Key)
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
                        if (_SeparateActionButton)
                        {
                            if (s.scanCode == _P1_Action_Key)
                            {
                                Apply_AND_ByteMask(_Buttons_CaveAddress + 1, 0x7F);
                            }
                            else if (s.scanCode == _P2_Action_Key)
                            {
                                Apply_AND_ByteMask(_Buttons_CaveAddress + 3, 0x7F);
                            }
                        }
                    }
                    else
                    {
                        if (_SeparateActionButton)
                        {
                            if (s.scanCode == _P1_Action_Key)
                            {
                                WriteByte(_P1_Action_CaveAddress, 0x00);
                            }
                            else if (s.scanCode == _P2_Action_Key)
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpBillboard, OutputId.LmpBillboard));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpHolder, OutputId.P1_LmpHolder));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpHolder, OutputId.P2_LmpHolder));            
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunRecoil, OutputId.P2_GunRecoil));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            byte bOutput = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset);
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
