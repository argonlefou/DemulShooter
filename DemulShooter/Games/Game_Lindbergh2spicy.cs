using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    class Game_Lindbergh2spicy : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\lindbergh\2spicy";

        //Inputs
        private InjectionStruct _JvsRawAxes_InjectionStruct = new InjectionStruct(0x0831F80D, 7);
        //private InjectionStruct _AdjustedAxes_InjectionStruct = new InjectionStruct(????, 6);
        private InjectionStruct _Buttons_InjectionStruct = new InjectionStruct(0x0831F5CC, 7);

        //Outputs
        private UInt32 _OutputsPtr_Address = 0x0A89F944;
        private UInt32 _Credits_Address = 0x0C8C0240;
        private UInt32 _PlayerStructPtr_Address = 0x0867B10C;
        private UInt32 _AmmoPtr_Address = 0x0888F8F8;

        //Custom Data
        private UInt32 _JvsRawAxes_CaveAddress;
        private UInt32 _AdjustedAxes_CaveAddress;
        private UInt32 _Buttons_CaveAddress;

        //Check instruction for game loaded
        private UInt32 _RomLoaded_Check_Address = 0x082EFC63;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_Lindbergh2spicy(String RomName)
            : base(RomName, "LinuxLoader")
        {
            _KnownMd5Prints.Add("2 Spicy (SBMV)", "b2183415493f9901dd45197364d51ebb");

            _tProcess.Start();
            Logger.WriteLog("Waiting for Lindbergh " + _RomName + " game to hook.....");
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

                            if (_Target_Process_Name.ToLower().Contains("budgieloader"))
                            {
                                if (!FindGameWindow_Contains("TeknoBudgie"))
                                {
                                    Logger.WriteLog("Game Window not found, waiting...");
                                    return;
                                }
                            }
                            else if (_Target_Process_Name.ToLower().Contains("linuxloader"))
                            {
                                if (!FindGameWindow_Contains("FPS"))
                                {
                                    Logger.WriteLog("Game Window not found, waiting...");
                                    return;
                                }
                            }

                            //To make sure LinuxLoader has loaded the rom entirely, we're looking for some random instruction to be present in memory before starting                            
                            //And this instruction is also helping us detecting whether the game file is Rev.A or Rev.B or Rev.C binary, to call the corresponding hack
                            byte[] buffer = ReadBytes(_RomLoaded_Check_Address, 3);
                            if (buffer.SequenceEqual(new byte[] { 0xB8, 0x30, 0x24 }))
                            {
                                Logger.WriteLog("2 Spicy (SBMV) binary detected");
                                _TargetProcess_Md5Hash = _KnownMd5Prints["2 Spicy (SBMV)"];
                            }
                            else
                            {
                                Logger.WriteLog("Game not Loaded, waiting...");
                                return;
                            }

                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                            Apply_MemoryHacks();
                            _ProcessHooked = true;
                            RaiseGameHookedEvent();
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

                    //X => [0x0A - 0xF5]
                    //Y => [0x06 - 0xFA]                   
                    double dMaxX = 236.0;
                    double dMaxY = 245.0;

                    PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX) + 0x0A);
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY) + 0x06);                    
                    if (PlayerData.RIController.Computed_X < 10)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 6)
                        PlayerData.RIController.Computed_Y = 0;
                    if (PlayerData.RIController.Computed_X > 245)
                        PlayerData.RIController.Computed_X = 255;
                    if (PlayerData.RIController.Computed_Y > 250)
                        PlayerData.RIController.Computed_Y = 255;

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
            _JvsRawAxes_CaveAddress = _InputsDatabank_Address;
            _AdjustedAxes_CaveAddress = _InputsDatabank_Address + 0x10;
            _Buttons_CaveAddress = _InputsDatabank_Address + 0x20;

            SetHack_JvsRawAxes();

            //This one is different and does not have the usual lindbergh shooter function 
            //SetHack_AdjustedAxes();

            SetHack_Buttons();

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// At the end of amJvspAckAnalogInput(), replacing Axis value before memory copy
        /// </summary>
        private void SetHack_JvsRawAxes()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //lea ecx,[ebx-9]
            CaveMemory.Write_StrBytes("8D 4B F7");
            //movzx ecx,word ptr [ecx+_Axes_CaveAddress]
            CaveMemory.Write_StrBytes("0F B6 89");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_JvsRawAxes_CaveAddress));
            //lea ebx,[ebx+esi+00000102]
            CaveMemory.Write_StrBytes("8D 9C 33 02 01 00 00");
            //mov [ebx],cx
            CaveMemory.Write_StrBytes("66 89 0B");

            //Inject it
            CaveMemory.InjectToAddress(_JvsRawAxes_InjectionStruct, "Axes");
        }
       
        /// <summary>
        /// At the end of amJvspAckSwInput(), removing the wanted buttons bit states from the source memory (Trigger, Reload, and Grenade)
        /// and changing the bits with custom values before memorycopy
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //cmp esi,05
            CaveMemory.Write_StrBytes("83 FE 05");
            //je originalcode
            CaveMemory.Write_StrBytes("74 28");
            //and byte ptr [edx+esi+00000102],FC
            CaveMemory.Write_StrBytes("80 A4 32 02 01 00 00 FC");
            //and byte ptr [edx+esi+00000103],7F
            CaveMemory.Write_StrBytes("80 A4 32 03 01 00 00 7F");
            //movzx ecx,word ptr [esi+_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("0F B7 8E");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //or [edx+esi+00000102],cl
            CaveMemory.Write_StrBytes("08 8C 32 02 01 00 00");
            //shr ecx,08
            CaveMemory.Write_StrBytes("C1 E9 08");
            //or [edx+esi+00000102],cl
            CaveMemory.Write_StrBytes("08 8C 32 03 01 00 00");
            //originalcode:
            //lea eax,[edx+esi+00000102]
            CaveMemory.Write_StrBytes("8D 84 32 02 01 00 00");

            //Inject it
            CaveMemory.InjectToAddress(_Buttons_InjectionStruct, "Buttons");
        }

        #endregion

        #region Inputs

        public override void SendInput(PlayerSettings PlayerData)
        {
            if (PlayerData.ID == 1)
            {
                WriteByte(_JvsRawAxes_CaveAddress, (byte)PlayerData.RIController.Computed_X);
                WriteByte(_JvsRawAxes_CaveAddress + 0x02, (byte)PlayerData.RIController.Computed_Y);

                WriteByte(_AdjustedAxes_CaveAddress, (byte)PlayerData.RIController.Computed_X);
                WriteByte(_AdjustedAxes_CaveAddress + 1, (byte)PlayerData.RIController.Computed_Y);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 6, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 6, 0xFD);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    if ((byte)PlayerData.RIController.Computed_X > 0x7F)
                        Apply_OR_ByteMask(_Buttons_CaveAddress + 6, 0x04);
                    else
                        Apply_OR_ByteMask(_Buttons_CaveAddress + 6, 0x08);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 6, 0xF3); //Remove both PEDAL bits

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 6, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 6, 0xFE);
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
                    if (s.scanCode == HardwareScanCode.DIK_LEFT)
                    {
                        Apply_OR_ByteMask(_Buttons_CaveAddress + 6, 0x04);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_RIGHT)
                    {
                        Apply_OR_ByteMask(_Buttons_CaveAddress + 6, 0x08);
                    }
                }
                else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                {
                    if (s.scanCode == HardwareScanCode.DIK_LEFT)
                    {
                        Apply_AND_ByteMask(_Buttons_CaveAddress + 6, 0xFB);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_RIGHT)
                    {
                        Apply_AND_ByteMask(_Buttons_CaveAddress + 6, 0xF7);
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
            //Gun recoil : Is activated for every bullet shot
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputId.LmpPanel));
            _Outputs.Add(new GameOutput(OutputId.Lmp1));
            _Outputs.Add(new GameOutput(OutputId.Lmp2));
            _Outputs.Add(new GameOutput(OutputId.Lmp3));
            _Outputs.Add(new GameOutput(OutputId.Lmp4));
            _Outputs.Add(new GameOutput(OutputId.Lmp5));
            _Outputs.Add(new GameOutput(OutputId.Lmp6));
            _Outputs.Add(new GameOutput(OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputId.P1_Ammo));
            _Outputs.Add(new AsyncGameOutput(OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputId.P1_Life));
            _Outputs.Add(new AsyncGameOutput(OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            UInt32 Outputs_Address = BitConverter.ToUInt32(ReadBytes(_OutputsPtr_Address, 4), 0);
            int RecoilStatus = ReadByte(Outputs_Address) >> 6 & 0x01;            
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.LmpPanel, ReadByte(Outputs_Address + 1) >> 7 & 0x01);
            SetOutputValue(OutputId.Lmp1, ReadByte(Outputs_Address + 1) >> 1 & 0x01);
            SetOutputValue(OutputId.Lmp2, ReadByte(Outputs_Address + 1) >> 2 & 0x01);
            SetOutputValue(OutputId.Lmp3, ReadByte(Outputs_Address + 1) >> 3 & 0x01);
            SetOutputValue(OutputId.Lmp4, ReadByte(Outputs_Address + 1) >> 4 & 0x01);
            SetOutputValue(OutputId.Lmp5, ReadByte(Outputs_Address + 1) >> 5 & 0x01);
            SetOutputValue(OutputId.Lmp6, ReadByte(Outputs_Address + 1) >> 6 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, RecoilStatus);

            //Custom Outputs
            _P1_Life = 0;
            _P1_Ammo = 0;

            //Filter InGame and not in attract Demo
            if (ReadByte(ReadPtr(_PlayerStructPtr_Address) + 0x27) != 0 && ReadByte(ReadPtr(_PlayerStructPtr_Address) + 0x2D) == 1)
            {
                _P1_Life = ReadByte(ReadPtr(_PlayerStructPtr_Address) + 0x78);
                _P1_Ammo = ReadByte(ReadPtr(_AmmoPtr_Address) + 0x04);

                //[Damaged] custom Output  
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }

            _P1_LastLife = _P1_Life;
            _P1_LastAmmo = _P1_Ammo;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            //Custom Recoil will be ctivated just ike the original one
            SetOutputValue(OutputId.P1_CtmRecoil, RecoilStatus); 
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
    }
}
