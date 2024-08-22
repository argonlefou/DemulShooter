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

namespace DemulShooter
{
    class Game_GvrAliens : Game
    {
        /*** MEMORY ADDRESSES **/
        private UInt32 _P1_X_Offset = 0x0556AEA8;
        private UInt32 _P1_Y_Offset = 0x0556AEAC;
        private UInt32 _P1_Btn_Offset = 0x0556ACBC;
        private UInt32 _P1_2_Offset = 0x0556B1C0;
        private UInt32 _P2_Y_Offset = 0x0556B1C4;
        private UInt32 _P2_Btn_Offset = 0x0556AFD4;
        private NopStruct _Nop_X = new NopStruct(0x0002EE9C, 6);
        private NopStruct _Nop_Y = new NopStruct(0x0002EEA5, 6);
        private NopStruct _Nop_Btn_1 = new NopStruct(0x0002EE75, 6);
        private NopStruct _Nop_Btn_2 = new NopStruct(0x0002EE81, 6);
        private NopStruct _Nop_Btn_3 = new NopStruct(0x0002EE8D, 3);
        private NopStruct _Nop_Btn_4 = new NopStruct(0x00048825, 2);
        private UInt32 _OutputRecoil_Injection_Offset = 0x00048D6C;
        private UInt32 _OutputRecoil_Injection_Return_Offset = 0x00048D72;

        private UInt32 _CtmRecoil_CaveAddress;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_GvrAliens(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "aliens dehasped", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Aliens Extermination v1.03 US - Dehasped + No StaticPath patched", "30c8725c19d07fbebb68903f8f068052");
            _KnownMd5Prints.Add("Aliens Extermination v1.03 US - Dehasped", "9ed286aafe474a16c07f7d62b5c06ecb");
            _tProcess.Start();
            Logger.WriteLog("Waiting for Global VR " + _RomName + " game to hook.....");
        }

        /// <summary>
        /// Timer event when looking for Demul Process (auto-Hook and auto-close)
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

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero && _TargetProcess.MainWindowHandle != IntPtr.Zero)
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

                    //X => [0-FFFF] = 65535
                    //Y => [0-FFFF] = 65535
                    double dMaxX = 65535.0;
                    double dMaxY = 65535.0;

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
        /// Easy hack, just blocking instructions to be able to inject custom values
        /// </summary>
        protected override void Apply_InputsMemoryHack()
        {
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_X);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Y);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Btn_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Btn_2);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Btn_3);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Btn_4);            

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _CtmRecoil_CaveAddress = _OutputsDatabank_Address;

            SetHack_CustomRecoilOutput();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }        

        /// <summary>
        /// This codecave will intercept the game's procedure changing the output values
        /// Genuine recoil is set by the game in memory (0x50909E0) with values 0x05 for P1 and 0x0A for P2.
        /// But I have no clue how it is unset ("Px Gun Power" setting not used) to be sure it's long enough to work, so this method will ensure a custom recoil not missed
        /// </summary>
        private void SetHack_CustomRecoilOutput()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //or [_CtmRecoil_CaveAddress], eax
            CaveMemory.Write_StrBytes("09 05");
            byte[] b = BitConverter.GetBytes(_CtmRecoil_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov ["aliens dehasped.exe"+4C909E0],ecx
            CaveMemory.Write_StrBytes("89 0D E0 09 09 05");
            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _OutputRecoil_Injection_Return_Offset);

            Logger.WriteLog("Adding Custom recoil output CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _OutputRecoil_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _OutputRecoil_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Btn_Offset, 0x10);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Btn_Offset, 0xEF);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Btn_Offset, 0x20);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Btn_Offset, 0xDF);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Btn_Offset, 0x40);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Btn_Offset, 0xBF);
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_2_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Btn_Offset, 0x10);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Btn_Offset, 0xEF);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Btn_Offset, 0x20);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Btn_Offset, 0xDF);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Btn_Offset, 0x40);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Btn_Offset, 0xBF);
            }
        }

        #endregion
        
        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun recoil : is handled by the game like it should (On/Off with every bullets)
            //Gun motor  : is activated when player gets hit
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunRecoil, OutputId.P2_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LedAmmo1, OutputId.P1_LedAmmo1));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LedAmmo2, OutputId.P1_LedAmmo2));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LedAmmo1, OutputId.P2_LedAmmo1));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LedAmmo2, OutputId.P2_LedAmmo2));
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
            //Original Outputs
            SetOutputValue(OutputId.P1_LedAmmo1, ReadByte(0x0527AEA1));
            SetOutputValue(OutputId.P1_LedAmmo2, ReadByte(0x0527AEA0));
            SetOutputValue(OutputId.P2_LedAmmo1, ReadByte(0x0527AEA3));
            SetOutputValue(OutputId.P2_LedAmmo2, ReadByte(0x0527AEA2));
            //Gun recoil : values are set when bullet is fired, but I have no clue when the game is setting it back to 0
            //Gun recoil force adjustment doesn't seem to play here, mean time between ON and OFF state seems to be ~30 / ~35 ms
            //Which may be too quick to work good with recoil...
            //But the Enable/Disable kickback in TEST menu will work here
            SetOutputValue(OutputId.P1_GunRecoil, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x04E7AEA4) >> 2 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x04E7AEA5) >> 2 & 0x01);

            //Custom Outputs
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 1;
            int P2_Clip = 1;

            //Game status :
            //[2] = Menu / Continue screen
            //[4] = In game
            //[64] = Attract demo
            // We will use these values to compute ourselve Recoil and P1/P2 Start Button Lights
            byte GameStatus = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x0047C960);
            if (GameStatus == 4)
            {
                //Playr status:
                //1: GameOver
                //4: Ingame
                //8: Continue screen
                int P1_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x0556C844);
                int P2_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x0556CBD8);

                if (P1_Status == 4)
                {
                    //Force Start Lamp to Off
                    SetOutputValue(OutputId.P1_CtmLmpStart, 0);

                    _P1_Life = (int)(100 * BitConverter.ToSingle(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x0556C884, 4), 0));

                    //Can't use original digits for Ammo because the game use it to display continue countdown                
                    _P1_Ammo = (int)BitConverter.ToSingle(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x0556CB08, 4), 0);

                    //[Clip Empty] custom Output
                    if (ReadByte(0x0527AEA1) == 0x0A && ReadByte(0x0527AEA0) == 0x0A)
                        P1_Clip = 0;

                    //[Damaged] custom Output                
                    if (_P1_Life < _P1_LastLife)
                        SetOutputValue(OutputId.P1_Damaged, 1);
                }
                else
                {
                    //Enable Start Lamp Blinking
                    SetOutputValue(OutputId.P1_CtmLmpStart, -1);
                }

                if (P2_Status == 4)
                {
                    //Force Start Lamp to Off
                    SetOutputValue(OutputId.P2_CtmLmpStart, 0);

                    _P2_Life = (int)(100 * BitConverter.ToSingle(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x0556CC18, 4), 0));
                
                    //Can't use original digits for Ammo because the game use it to display continue countdown                
                    _P2_Ammo = (int)BitConverter.ToSingle(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x00556CE9C, 4), 0);

                    //[Clip Empty] custom Output
                    if (ReadByte(0x0527AEA3) == 0x0A && ReadByte(0x0527AEA2) == 0x0A)
                        P2_Clip = 0;

                    //[Damaged] custom Output                
                    if (_P2_Life < _P2_LastLife)
                        SetOutputValue(OutputId.P2_Damaged, 1);
                }
                else
                {
                    //Enable Start Lamp Blinking
                    SetOutputValue(OutputId.P2_CtmLmpStart, -1);
                }
            }

            //Custom Gun Recoil output
            //Based on genuine output but sure not to be missed because of too short pulse timing
            if ((byte)(ReadByte(_CtmRecoil_CaveAddress) & 0x01) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                Apply_AND_ByteMask(_CtmRecoil_CaveAddress, 0x0A);
            }
            
            if ((byte)(ReadByte(_CtmRecoil_CaveAddress) >> 1 & 0x01) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                Apply_AND_ByteMask(_CtmRecoil_CaveAddress, 0x05);
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
            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x0046D748) + ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x0046D7C0));
        }

        #endregion
    }
}
