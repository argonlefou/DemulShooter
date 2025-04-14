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
    class Game_CxbxGsquad : Game
    {
        /*** MEMORY ADDRESSES **/
        private InjectionStruct _JvsButtons_InjectionStruct = new InjectionStruct(0x000752B2, 7);
        private InjectionStruct _JvsAxis_InjectionStruct = new InjectionStruct(0x000752D2, 6);
        private UInt32 _Buttons_CaveAddress = 0;
        private UInt32 _Axis_CaveAddress = 0;
        private UInt32 _P1_Calibration_Offset = 0x0004344B4;
        private UInt32 _P2_Calibration_Offset = 0x0004344CC;

        //Outputs
        private UInt32 _JvsOutputBuffer_Offset = 0x004F10F0;
        private UInt32 _P1_Status_Offset = 0x00455A28;
        private UInt32 _LifePtr_Offset = 0x00455B14;
        private UInt32 _AmmoPtr_Offset = 0x00441828;
        private UInt32 _Credits_Offset = 0x004A9EA0;

        private UInt32 _RomLoaded_CheckIntructionn_Offset = 0x000752BE;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_CxbxGsquad(String RomName)
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
                            byte[] bTest = new byte[] { 0x8D, 0x54, 0x24, 0x0F };
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

                    //X => [00 ; 0xFF] => 256
                    //Y => [00; 0xFF] => 256
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;

                    PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));
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
            _Buttons_CaveAddress = _InputsDatabank_Address;
            _Axis_CaveAddress = _InputsDatabank_Address + 0x10;

            SetHack_Buttons();
            SetHack_Axis();
            SetHack_Calibration();
            
            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Replacing the memory read from JVS message received by our own buffer
        /// </summary>      
        private void SetHack_Buttons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            //mov eax,[_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //mov [edi],eax
            CaveMemory.Write_StrBytes("89 07");
            //mov cl,[_Buttons_CaveAddress + 4]
            CaveMemory.Write_StrBytes("8A 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress + 4));

            //Inject it
            CaveMemory.InjectToOffset(_JvsButtons_InjectionStruct, "Buttons");
        }

        /// <summary>
        /// Replacing the memory read from JVS message received by our own buffer 
        /// </summary>
        private void SetHack_Axis()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            //mov ecx, _Axis_CaveAddress
            CaveMemory.Write_StrBytes("B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axis_CaveAddress));
            //mov edx,[ecx]
            CaveMemory.Write_StrBytes("8B 11");

            //Inject it
            CaveMemory.InjectToOffset(_JvsAxis_InjectionStruct, "Axis");
        }

        /// /// <summary>
        /// JVS data is computed to [-320 / +320] and [-240 / +240] values thanks to calibration values
        /// Overwriting calibration data with values tested to have perfect accuracy
        /// </summary>
        private void SetHack_Calibration()
        {
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Calibration_Offset, new byte[] { 0xFD, 0xFF, 0xE0, 0x00, 0xCF, 0xFE, 0x02, 0x00, 0x00, 0x00, 0x02, 0x00, 0x2C, 0x01, 0x02, 0x00, 0x00, 0x00, 0x23, 0xFF });
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Calibration_Offset, new byte[] { 0xFD, 0xFF, 0xE0, 0x00, 0xCF, 0xFE, 0x02, 0x00, 0x00, 0x00, 0x02, 0x00, 0x2C, 0x01, 0x02, 0x00, 0x00, 0x00, 0x23, 0xFF });
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
                WriteByte(_Axis_CaveAddress + 1, (byte)PlayerData.RIController.Computed_X);
                WriteByte(_Axis_CaveAddress + 3, (byte)PlayerData.RIController.Computed_Y);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 1, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 1, 0xFD);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 1, 0x01);
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 1, 0x02);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 1, 0xFD);
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 1, 0xFE);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 2, 0x40);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 2, 0xBF);
            }
            else if (PlayerData.ID == 2)
            {
                WriteByte(_Axis_CaveAddress + 5, (byte)PlayerData.RIController.Computed_X);
                WriteByte(_Axis_CaveAddress + 7, (byte)PlayerData.RIController.Computed_Y);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 3, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 3, 0xFD);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 3, 0x01);
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 3, 0x02);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 3, 0xFD);
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 3, 0xFE);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 4, 0x40);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 4, 0xBF);
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
                            Apply_OR_ByteMask(_Buttons_CaveAddress + 1, 0x80);
                        else if (s.scanCode == HardwareScanCode.DIK_2)
                            Apply_OR_ByteMask(_Buttons_CaveAddress + 3, 0x80);
                        else if (s.scanCode == HardwareScanCode.DIK_O)
                            Apply_OR_ByteMask(_Buttons_CaveAddress + 2, 0x80);  //P1 ACTION
                        else if (s.scanCode == HardwareScanCode.DIK_P)
                            Apply_OR_ByteMask(_Buttons_CaveAddress + 4, 0x80);  //P2 ACTION

                        else if (s.scanCode == HardwareScanCode.DIK_9)
                            Apply_OR_ByteMask(_Buttons_CaveAddress, 0x04);  //SERVICE
                        else if (s.scanCode == HardwareScanCode.DIK_0)
                            Apply_OR_ByteMask(_Buttons_CaveAddress, 0x80);  //TEST

                    }
                    else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                    {
                        if (s.scanCode == HardwareScanCode.DIK_1)
                            Apply_AND_ByteMask(_Buttons_CaveAddress + 1, 0x7F);
                        else if (s.scanCode == HardwareScanCode.DIK_2)
                            Apply_AND_ByteMask(_Buttons_CaveAddress + 3, 0x7F);
                        else if (s.scanCode == HardwareScanCode.DIK_O)
                            Apply_AND_ByteMask(_Buttons_CaveAddress + 2, 0x7F);
                        else if (s.scanCode == HardwareScanCode.DIK_P)
                            Apply_AND_ByteMask(_Buttons_CaveAddress + 4, 0x7F);
                        else if (s.scanCode == HardwareScanCode.DIK_9)
                            Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFB);
                        else if (s.scanCode == HardwareScanCode.DIK_0)
                            Apply_AND_ByteMask(_Buttons_CaveAddress, 0x7F);
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
            _Outputs.Add(new GameOutput(OutputDesciption.LmpBillboard, OutputId.LmpBillboard));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpHolder, OutputId.P1_LmpHolder));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpHolder, OutputId.P2_LmpHolder)); //Never activated ?
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunRecoil, OutputId.P2_GunRecoil));
            //_Outputs.Add(new GameOutput(OutputDesciption.LmpCoin, OutputId.LmpCoin));
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
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _JvsOutputBuffer_Offset + 16) >> 2 & 1);
            SetOutputValue(OutputId.LmpBillboard, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _JvsOutputBuffer_Offset + 16) >> 4 & 1);
            SetOutputValue(OutputId.P1_LmpHolder, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _JvsOutputBuffer_Offset + 16) >> 3 & 1);
            SetOutputValue(OutputId.P2_LmpHolder, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _JvsOutputBuffer_Offset + 16) >> 5 & 1);
            SetOutputValue(OutputId.P1_GunRecoil, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _JvsOutputBuffer_Offset + 16) >> 6 & 1);
            SetOutputValue(OutputId.P2_GunRecoil, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _JvsOutputBuffer_Offset + 16) >> 1 & 1);

            //Customs Outputs
            //Custom recoil will be triggered by the original recoil pulse
            SetOutputValue(OutputId.P1_CtmRecoil, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _JvsOutputBuffer_Offset + 16) >> 6 & 1);
            SetOutputValue(OutputId.P2_CtmRecoil, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _JvsOutputBuffer_Offset + 16) >> 1 & 1);

            //Player Status :
            //[2] : In-Game
            int P1_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Status_Offset);
            int P2_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Status_Offset + 0x1AC);
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            if (P1_Status == 2)
            {
                _P1_Life = (int)ReadPtrChain((UInt32)_TargetProcess_MemoryBaseAddress + _LifePtr_Offset, new UInt32[] { 0xB8 });
                _P1_Ammo = (int)ReadPtrChain((UInt32)_TargetProcess_MemoryBaseAddress + _AmmoPtr_Offset, new UInt32[] { 0x38, 0x0C });

                //[Clip Empty] custom Output
                if (_P1_Ammo > 0)
                    P1_Clip = 1;

                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }

            if (P2_Status == 2)
            {
                _P2_Life = (int)ReadPtrChain((UInt32)_TargetProcess_MemoryBaseAddress + _LifePtr_Offset + 0x1AC, new UInt32[] { 0xB8 });
                _P2_Ammo = (int)ReadPtrChain((UInt32)_TargetProcess_MemoryBaseAddress + _AmmoPtr_Offset, new UInt32[] { 0x38 + 0x650, 0x0C });

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
