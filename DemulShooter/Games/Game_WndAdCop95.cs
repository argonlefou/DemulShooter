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
    class Game_WndAdCop95 : Game
    {
        //Memory values
        private UInt32 _Credits_Offset = 0x00060A45;
        private NopStruct _Nop_AxisX = new NopStruct(0x0000D970, 6);
        private NopStruct _Nop_AxisY = new NopStruct(0x0000D982, 6);
        private UInt32 _PlayersDataPtr_Offset = 0x00063607;
        private UInt32 _PlayersStatus_Offset = 0x00060A51;
        private InjectionStruct _Buttons_InjectionStruct = new InjectionStruct(0x0000D948, 9);

        private UInt32 _NoCrosshairPatch_P1_Offset = 0x000030F9;
        private UInt32 _NoCrosshairPatch_P2_Offset = 0x00003187;

        //custom values
        private UInt32 _P1_Buttons_CaveAddress = 0;
        private UInt32 _P2_Buttons_CaveAddress = 0;

        private HardwareScanCode _DIK_Credits = HardwareScanCode.DIK_5;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndAdCop95(String RomName, bool HideCrosshair, bool DisableInputHack, bool Verbose)
            : base(RomName, "adcop95", DisableInputHack, Verbose)
        {
            _HideCrosshair = HideCrosshair;

            _KnownMd5Prints.Add("ADCOP v1.06 - Original exe", "e5ee4b73028672d5b30a5f0f38e0a05a");
            _tProcess.Start();

            Logger.WriteLog("Waiting for Windows " + _RomName + " game to hook.....");
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
                            _GameWindowHandle = _TargetProcess.MainWindowHandle;
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();
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

                    //X => [0-320]
                    //Y => [0-240]
                    double dMaxX = 320.0;
                    double dMaxY = 240.0;

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

        /// <summary>
        /// Genuine Hack, just blocking Axis and Triggers input to replace them.
        /// </summary>        
        protected override void Apply_InputsMemoryHack()
        {
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_AxisX);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_AxisY);

            Create_InputsDataBank();
            _P1_Buttons_CaveAddress = _InputsDatabank_Address;
            _P2_Buttons_CaveAddress = _InputsDatabank_Address + 4;

            SetHack_Buttons();

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }              

        /// <summary>
        /// At that moment the game is checking the button state to run functions based on the button value.
        /// - Player ID in EBP+8 (0 / 1)
        /// - Player Struct in EDI (+3BC and +3C0 are axis)
        /// - EDX+C has button info (2 = shoot, 1=reload, 4=grenade, 8= ??) 
        /// Resetting the custom codecave value afterward is mandatory or all bullets will be fired at once with a button push
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov edi,[ebx+0000008A]
            CaveMemory.Write_StrBytes("8B BB 8A 00 00 00");
            //mov edx,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 55 0C");

            //push eax
            CaveMemory.Write_StrBytes("50");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov eax,[ebp+08]
            CaveMemory.Write_StrBytes("8B 45 08");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _P1_Buttons_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Buttons_CaveAddress));            
            //mov ebx, [eax]
            CaveMemory.Write_StrBytes("8B 18");
            //mov [edx+c], eax
            CaveMemory.Write_StrBytes("89 5A 0C");
            //mov byte ptr[eax], 0
            CaveMemory.Write_StrBytes("C6 00 00");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Buttons");
        }

        protected override void Apply_NoCrosshairMemoryHack()
        {
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _NoCrosshairPatch_P1_Offset, new byte[] { 0x68, 0xFF, 0x07, 0x00, 0x00, 0x90 });
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _NoCrosshairPatch_P2_Offset, new byte[] { 0x68, 0xFF, 0x07, 0x00, 0x00, 0x90 });
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>        
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((UInt32)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((UInt32)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                UInt32 PlayerAddress = ReadPtrChain((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersDataPtr_Offset, new uint[] { 0x8A });
                if (PlayerAddress != 0)
                {
                    WriteBytes(PlayerAddress + 0x3BC, bufferX);
                    WriteBytes(PlayerAddress + 0x3C0, bufferY);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    Apply_OR_ByteMask(_P1_Buttons_CaveAddress, 0x02);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress, 0xFD);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    Apply_OR_ByteMask(_P1_Buttons_CaveAddress, 0x04);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress, 0xFB);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    Apply_OR_ByteMask(_P1_Buttons_CaveAddress, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress, 0xFE);
                }
            }
            else if (PlayerData.ID == 2)
            {
                UInt32 PlayerAddress = ReadPtrChain((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersDataPtr_Offset + 4, new uint[] { 0x8A });
                if (PlayerAddress != 0)
                {
                    WriteBytes(PlayerAddress + 0x3BC, bufferX);
                    WriteBytes(PlayerAddress + 0x3C0, bufferY);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress, 0x02);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress, 0xFD);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress, 0x04);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress, 0xFB);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress, 0xFE);
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
                    if (s.scanCode == _DIK_Credits)
                    {
                        int Coins = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset, 4), 0);
                        Coins++;
                        WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset, BitConverter.GetBytes(Coins));
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
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P1_CtmLmpStart, OutputId.P1_CtmLmpStart, 500));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P2_CtmLmpStart, OutputId.P2_CtmLmpStart, 500));
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
            //Custom Outputs
            //that byte uses bit 0 to set P1 status (playing/dead) and byte1 for P2
            UInt32 PlayersStatus = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersStatus_Offset);

            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            if ((PlayersStatus & 1) == 1)
            {
                //Force Start Lamp to Off
                SetOutputValue(OutputId.P1_CtmLmpStart, 0);

                UInt32 PlayerAddress = ReadPtrChain((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersDataPtr_Offset, new uint[] { 0x8A });
                if (PlayerAddress != 0)
                {
                    _P1_Life = ReadByte(PlayerAddress);
                    _P1_Ammo = ReadByte(PlayerAddress + 0x3D0);

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
            }
            else
            {
                //Enable Start Lamp Blinking
                SetOutputValue(OutputId.P1_CtmLmpStart, -1);
            }

            if ((PlayersStatus >> 1 & 1) == 1)
            {
                //Force Start Lamp to Off
                SetOutputValue(OutputId.P2_CtmLmpStart, 0);

                UInt32 PlayerAddress = ReadPtrChain((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersDataPtr_Offset + 4, new uint[] { 0x8A });
                if (PlayerAddress != 0)
                {
                    _P2_Life = ReadByte(PlayerAddress);
                    _P2_Ammo = ReadByte(PlayerAddress + 0x3D0);

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
            }
            else
            {
                //Enable Start Lamp Blinking
                SetOutputValue(OutputId.P2_CtmLmpStart, -1);
            }

            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset));

            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset));
        }

        #endregion
    }

}
