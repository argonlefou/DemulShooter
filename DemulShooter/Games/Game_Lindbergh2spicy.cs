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
    class Game_Lindbergh2spicy : Game
    {
        //Address to find InputStruct values (read at instruction 0x82EFC63)
        private UInt32 _InputStruct_Address = 0x0C8B2430;

        //INPUT_STRUCT offset in game
        private UInt32 _Input_X_Offset = 0x17D;
        private UInt32 _Input_Y_Offset = 0x183;

        //NOP for Gun axis and buttons in-game
        private NopStruct _Nop_Axix_X_1 = new NopStruct(0x082F0109, 7);
        private NopStruct _Nop_Axix_X_2 = new NopStruct(0x082EFEFC, 7);
        private NopStruct _Nop_Axix_Y_1 = new NopStruct(0x082F0153, 7);
        private NopStruct _Nop_Axix_Y_2 = new NopStruct(0x082EFF13, 7);

        private UInt32 _Buttons_CaveAddress = 0;
        private UInt32 _Buttons_Injection_Address = 0x082F005F;
        private UInt32 _Buttons_Injection_Return_Address = 0x082F0064;

        //Check instruction for game loaded
        private UInt32 _RomLoaded_Check_Instruction = 0x082EFC63;

        //Outputs
        private UInt32 _OutputsPtr_Address = 0x0A89F944;
        private UInt32 _Outputs_Address;
        private UInt32 _Credits_Address = 0x0C8C0240;
        private UInt32 _PlayerStructPtr_Address = 0x0867B10C;
        private UInt32 _AmmoPtr_Address = 0x0888F8F8;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_Lindbergh2spicy(String RomName)
            : base(RomName, "BudgieLoader")
        {
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
                            //To make sure BurgieLoader has loaded the rom entirely, we're looking for some random instruction to be present in memory before starting 
                            byte[] buffer = ReadBytes(_RomLoaded_Check_Instruction, 5);
                            if (buffer[0] == 0xB8 && buffer[1] == 0x30 && buffer[2] == 0x24 && buffer[3] == 0x8B && buffer[4] == 0x0C)
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                Apply_MemoryHacks();
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();                                
                            }
                            else
                            {
                                Logger.WriteLog("Game not Loaded, waiting...");
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
            _Buttons_CaveAddress = _InputsDatabank_Address;

            SetHack_Buttons();

            SetNops(0, _Nop_Axix_X_1);
            SetNops(0, _Nop_Axix_X_2);
            SetNops(0, _Nop_Axix_Y_1);
            SetNops(0, _Nop_Axix_Y_2);            
            
            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// All butons are set on the same Byte, so we need to filter and block
        /// updates only on wanted bits to block Trigger/Reload from the game and let others (Start, Service, etc...)
        /// working as they should.
        /// </summary>
        private void SetHack_Buttons()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //movzx edx, [esp+24]
            CaveMemory.Write_StrBytes("0F B6 54 24 24");
            //and edx, 0xFFFFFFFC
            CaveMemory.Write_StrBytes("81 E2 FC FF FF FF");
            //or edx, [_Buttons_CaveAddress]  
            CaveMemory.Write_StrBytes("0B 15");
            Buffer.AddRange(BitConverter.GetBytes(_Buttons_CaveAddress));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //jmp back
            CaveMemory.Write_jmp(_Buttons_Injection_Return_Address);

            Logger.WriteLog("Adding Trigger Codecave_1 at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - (_Buttons_Injection_Address) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, _Buttons_Injection_Address, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }        

        #endregion

        #region Inputs

        public override void SendInput(PlayerSettings PlayerData)
        {
            if (PlayerData.ID == 1)
            {
                WriteByte(_InputStruct_Address + _Input_X_Offset, (byte)PlayerData.RIController.Computed_X);
                WriteByte(_InputStruct_Address + _Input_Y_Offset, (byte)PlayerData.RIController.Computed_Y);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFD);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFE);
            }
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpPanel, OutputId.LmpPanel));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp1, OutputId.Lmp1));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp2, OutputId.Lmp2));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp3, OutputId.Lmp3));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp4, OutputId.Lmp4));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp5, OutputId.Lmp5));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp6, OutputId.Lmp6));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            _Outputs_Address = BitConverter.ToUInt32(ReadBytes(_OutputsPtr_Address, 4), 0);
            int RecoilStatus = ReadByte(_Outputs_Address) >> 6 & 0x01;            
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.LmpPanel, ReadByte(_Outputs_Address + 1) >> 7 & 0x01);
            SetOutputValue(OutputId.Lmp1, ReadByte(_Outputs_Address + 1) >> 1 & 0x01);
            SetOutputValue(OutputId.Lmp2, ReadByte(_Outputs_Address + 1) >> 2 & 0x01);
            SetOutputValue(OutputId.Lmp3, ReadByte(_Outputs_Address + 1) >> 3 & 0x01);
            SetOutputValue(OutputId.Lmp4, ReadByte(_Outputs_Address + 1) >> 4 & 0x01);
            SetOutputValue(OutputId.Lmp5, ReadByte(_Outputs_Address + 1) >> 5 & 0x01);
            SetOutputValue(OutputId.Lmp6, ReadByte(_Outputs_Address + 1) >> 6 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, RecoilStatus);

            //Custom Outputs
            _P1_Life = 0;
            _P1_Ammo = 0;
            int P1_Clip = 0;

            //Filter InGame and not in attract Demo
            if (ReadByte(ReadPtr(_PlayerStructPtr_Address) + 0x27) == 1 && ReadByte(ReadPtr(_PlayerStructPtr_Address) + 0x2D) == 1)
            {
                _P1_Life = ReadByte(ReadPtr(_PlayerStructPtr_Address) + 0x78);
                _P1_Ammo = ReadByte(ReadPtr(_AmmoPtr_Address) + 0x04);

                //[Damaged] custom Output  
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                //[Clip] custom Output  
                if (_P1_Ammo > 0)
                    P1_Clip = 1;
            }

            _P1_LastLife = _P1_Life;
            _P1_LastAmmo = _P1_Ammo;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            //Custom Recoil will be ctivated just ike the original one
            SetOutputValue(OutputId.P1_CtmRecoil, RecoilStatus); 
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
    }
}
