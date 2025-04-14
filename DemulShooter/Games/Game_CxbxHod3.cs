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
    /// <summary>
    /// For this hack, Cxbx-Reloaded must be started with the following command line :
    /// cxbxr-ldr.exe /load [PATH_TO_ROM] 
    /// This will result in one (and only one Process), and easier to target and get window handle,
    /// whereas running Cxbx GUI, then choosing a Rom will create 2 different processes (sources : Cxbx Wiki)
    /// Last tested on build CI-0b69563 (2022-11-20)
    /// </summary>
    class Game_CxbxHod3 : Game
    {
        /*** MEMORY ADDRESSES **/
        private UInt32 _Inputs_OFFSET = 0x00267108;
        private UInt32 _P1_BYTE1_INDEX = 0x0D;
        private UInt32 _P1_BYTE2_INDEX = 0x0E;
        private UInt32 _P2_BYTE1_INDEX = 0x0F;
        private UInt32 _P2_BYTE2_INDEX = 0x10;
        /*private UInt32 _P1_X_INDEX = 0x12;
        private UInt32 _P1_Y_INDEX = 0x14;
        private UInt32 _P2_X_INDEX = 0x16;
        private UInt32 _P2_Y_INDEX = 0x18;
        private NopStruct _Nop_P1_X = new NopStruct(0x00001B62, 4);
        private NopStruct _Nop_P1_Y = new NopStruct(0x00001B6B, 4);
        private NopStruct _Nop_P2_X = new NopStruct(0x00001B74, 4);
        private NopStruct _Nop_P2_Y = new NopStruct(0x00001B7D, 4);*/
        private NopStruct _Nop_P1_BYTE1 = new NopStruct(0x00001B21, 3);
        private NopStruct _Nop_P1_BYTE2 = new NopStruct(0x00001B30, 3);
        private NopStruct _Nop_P2_BYTE1 = new NopStruct(0x00001B3F, 3);
        private NopStruct _Nop_P2_BYTE2 = new NopStruct(0x00001B4E, 3);
        private InjectionStruct _AxisX_InjectionStruct = new InjectionStruct(0x00069F95, 5);
        private InjectionStruct _AxisY_InjectionStruct = new InjectionStruct(0x00069FC5, 5);

        private UInt32 _RomLoaded_CheckIntructionn_Offset = 0x00001B62;

        //Outputs
        private UInt32 _JvsOutputBuffer_Offset = 0x004C78D8;
        //private UInt32 _GameScreen_Offset = 0x0026A410;
        private UInt32 _P1_Status_Offset = 0x00266708;
        private UInt32 _P2_Status_Offset = 0x00266934;
        private UInt32 _P1_Ammo_Offset = 0x00266760;
        private UInt32 _P2_Ammo_Offset = 0x0026698C;
        private UInt32 _P1_Life_Offset = 0x00266714;
        private UInt32 _P2_Life_Offset = 0x00266940;
        private UInt32 _Credits_Offset = 0x004BBA38;

        private UInt32 _P1_X_CaveAddress = 0;
        private UInt32 _P1_Y_CaveAddress = 0;
        private UInt32 _P2_X_CaveAddress = 0;
        private UInt32 _P2_Y_CaveAddress = 0;


        /// <summary>
        /// Constructor
        /// </summary>
        public Game_CxbxHod3(String RomName)
            : base(RomName, "cxbxr-ldr")
        {
            _tProcess.Start();
            Logger.WriteLog("Waiting for Chihiro " + _RomName + " game to hook.....");
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
                            //This is HEX code for the instrution we're testing to see which process is the good one to hook
                            byte[] bTest = new byte[] { 0x66, 0x89, 0x4E, 0x12 };
                            if (CheckBytes((UInt32)_TargetProcess_MemoryBaseAddress + _RomLoaded_CheckIntructionn_Offset, bTest))
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog("WindowHandle = " + _GameWindowHandle.ToString());
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                Apply_MemoryHacks();
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
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X => [-320 ; +320] => 640
                    //Y => [-240; +240] => 480
                    double dMinX = -320.0;
                    double dMaxX = 320.0;
                    double dMinY = -240.0;
                    double dMaxY = 240.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dRangeX * PlayerData.RIController.Computed_X / TotalResX) - dRangeX / 2);
                    PlayerData.RIController.Computed_Y = Convert.ToInt16((Math.Round(dRangeY * PlayerData.RIController.Computed_Y / TotalResY) - dRangeY / 2) * -1);
                    if (PlayerData.RIController.Computed_X < (int)dMinX)
                        PlayerData.RIController.Computed_X = (int)dMinX;
                    if (PlayerData.RIController.Computed_Y < (int)dMinY)
                        PlayerData.RIController.Computed_Y = (int)dMinY;
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
            _P1_X_CaveAddress = _InputsDatabank_Address;
            _P1_Y_CaveAddress = _InputsDatabank_Address + 4;
            _P2_X_CaveAddress = _InputsDatabank_Address + 8;
            _P2_Y_CaveAddress = _InputsDatabank_Address + 12;

            SetHack_Buttons();
            SetHack_Axis_X();
            SetHack_Axis_Y();

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        private void SetHack_Buttons()
        {
            //Simply NOPing procedure parts where the game writes buttons values from JVS data
            //For each player, there are 2 bytes of button data :
            //BYTE1 : 0x80=START, 0x02=TRIGGER, 0x01=OUT_OF_SCREEN
            //BYTE2 : 0x80=PUMP, 0x40= ??
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_BYTE1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_BYTE2);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_BYTE1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_BYTE2);
        }

        private void SetHack_Axis_X()
        {
            //Further down the axis computing, the game translates 0x00-0xFF JVS axis data to -320/+320 value after calibration data
            //Injecting our values at that point            
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            //mov eax,ebp
            CaveMemory.Write_StrBytes("8B C5");
            //shl eax,03
            CaveMemory.Write_StrBytes("C1 E0 03");
            //add eax, _P1_X_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_X_CaveAddress));
            //mov eax,[eax]
            CaveMemory.Write_StrBytes("8B 00");        

            //Inject it
            CaveMemory.InjectToOffset(_AxisX_InjectionStruct, "Axis");            
        }

        private void SetHack_Axis_Y()
        {
            //Further down the axis computing, the game translates 0x00-0xFF JVS axis data to -240/+240 value after calibration data
            //Injecting our values at that point            
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            //mov eax,ebp
            CaveMemory.Write_StrBytes("8B C5");
            //shl eax,03
            CaveMemory.Write_StrBytes("C1 E0 03");
            //add eax, _P1_X_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Y_CaveAddress));
            //mov eax,[eax]
            CaveMemory.Write_StrBytes("8B 00");

            //Inject it
            CaveMemory.InjectToOffset(_AxisY_InjectionStruct, "Axis");
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
                WriteBytes(_P1_X_CaveAddress, bufferX);
                WriteBytes(_P1_Y_CaveAddress, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _Inputs_OFFSET + _P1_BYTE1_INDEX, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _Inputs_OFFSET + _P1_BYTE1_INDEX, 0xFD);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _Inputs_OFFSET + _P1_BYTE2_INDEX, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _Inputs_OFFSET + _P1_BYTE2_INDEX, 0x7F);
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes(_P2_X_CaveAddress, bufferX);
                WriteBytes(_P2_Y_CaveAddress, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _Inputs_OFFSET + _P2_BYTE1_INDEX, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _Inputs_OFFSET + _P2_BYTE1_INDEX, 0xFD);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _Inputs_OFFSET + _P2_BYTE2_INDEX, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _Inputs_OFFSET + _P2_BYTE2_INDEX, 0x7F);
            }
        }

        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (!_DisableInputHack)
            {
                if (nCode >= 0)
                {
                    KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                    if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                    {
                        if (s.scanCode == HardwareScanCode.DIK_1)
                            Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _Inputs_OFFSET + _P1_BYTE1_INDEX, 0x80);
                        else if (s.scanCode == HardwareScanCode.DIK_2)
                            Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _Inputs_OFFSET + _P2_BYTE1_INDEX, 0x80);                        
                    }
                    else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                    {
                        if (s.scanCode == HardwareScanCode.DIK_1)
                            Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _Inputs_OFFSET + _P1_BYTE1_INDEX, 0x7F);
                        else if (s.scanCode == HardwareScanCode.DIK_2)
                            Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _Inputs_OFFSET + _P2_BYTE1_INDEX, 0x7F);    
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
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Clip, OutputId.P2_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _JvsOutputBuffer_Offset + 16) >> 7 & 1);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _JvsOutputBuffer_Offset + 16) >> 4 & 1);

            //Customs Outputs
            //Player Status :
            //[5] : In-Game
            //[4] : Continue Screen
            //[66] : Game Over
            //[9] : Attract Demo
            int P1_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Status_Offset);
            int P2_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Status_Offset);
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            if (P1_Status == 5)
            {
                _P1_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Life_Offset);
                _P1_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Ammo_Offset);

                //Custom Recoil
                if (_P1_Ammo < _P1_LastAmmo)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Clip Empty] custom Output
                if (_P1_Ammo > 0)
                    P1_Clip = 1;

                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }

            if (P2_Status == 5)
            {
                _P2_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Life_Offset);
                _P2_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Ammo_Offset);

                //Custom Recoil
                if (_P2_Ammo < _P2_LastAmmo)
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                //[Clip Empty] custom Output
                if (_P2_Ammo > 0)
                    P2_Clip = 1;

                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);
            }

            _P1_LastAmmo = _P1_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastAmmo = _P2_Ammo;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
