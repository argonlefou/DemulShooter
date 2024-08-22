using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;

namespace DemulShooter
{
    class Game_LindberghGhostSquadEvo : Game
    {
        //MEMORY ADDRESSES
        //private UInt32 _GameMode_Address = 0x08660420;
        private UInt32 _P1_GameState_Address = 0x086617E8;
        private UInt32 _P2_GameState_Address = 0x8661994;
        private UInt32 _P1_LifePtr_Address = 0x086618D4;
        private UInt32 _P2_LifePtr_Address = 0x08661A80;
        private UInt32 _Outputs_Address = 0x08656330;
        private UInt32 _Credits_Address = 0x00AE9FBA0;
        private InjectionStruct _PlayerDamage_Injection = new InjectionStruct(0x810C9D9, 6);

        //Outputs
        private UInt32 _P1_Damage_CaveAddress = 0;
        private UInt32 _P2_Damage_CaveAddress = 0;        

        private UInt32 _RomLoaded_Check_Instruction = 0x0807C9A0;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_LindberghGhostSquadEvo(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "BudgieLoader", DisableInputHack, Verbose)
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
                            if (buffer[0] == 0xA1 && buffer[1] == 0x90 && buffer[2] == 0x05 && buffer[3] == 0x1B && buffer[4] == 0x08)
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

        #region Memory Hack

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_Damage_CaveAddress = _OutputsDatabank_Address;
            _P2_Damage_CaveAddress = _OutputsDatabank_Address + 0x01;

            SetHack_Damage();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        //Intercept a call to set_player_damage_internal() function to get dammage event
        //Player Index is in edx
        private void SetHack_Damage()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[ebp+08]
            CaveMemory.Write_StrBytes("8B 45 08");
            //add eax,_P1_Damage_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Damage_CaveAddress));
            //mov byte ptr [eax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //mov [ebx+000000B8],edx
            CaveMemory.Write_StrBytes("89 93 B8 00 00 00");
            
            //Inject it it
            CaveMemory.InjectToAddress(_PlayerDamage_Injection, "Damage");
        }

        #endregion

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : Is activated for every bullet fired
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpHolder, OutputId.P1_LmpHolder));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpHolder, OutputId.P2_LmpHolder));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunRecoil, OutputId.P2_GunRecoil));
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
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Outputs_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_LmpHolder, ReadByte(_Outputs_Address) >> 5 & 0x01);
            SetOutputValue(OutputId.P2_LmpHolder, ReadByte(_Outputs_Address) >> 2 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, ReadByte(_Outputs_Address) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, ReadByte(_Outputs_Address) >> 3 & 0x01);

            //Custom Outputs
            _P1_Life = 0;
            _P2_Life = 0;

            // Checking the Player state to not trigger Damage event during Attract mode:
            // 0x00 = Not playing
            // 0x02 = In-Game
            // 0x08 = Continue
            // 0x10 = Game-Over
            // 0x80 = Attract Mode
            if (ReadByte(_P1_GameState_Address) == 2)
            {
                _P1_Life = ReadByte(ReadPtr(_P1_LifePtr_Address) + 0xB8);
                //P1_Ammo = ReadByte(P1_StructAddress + 0x400);

                //[Damaged] custom Output                
                if (ReadByte(_P1_Damage_CaveAddress) == 1)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }
            WriteByte(_P1_Damage_CaveAddress, 0);

            // Checking the Player state to not trigger Damage event during Attract mode:
            // 0x00 = Not playing
            // 0x02 = In-Game
            // 0x08 = Continue
            // 0x10 = Game-Over
            // 0x80 = Attract Mode
            if (ReadByte(_P2_GameState_Address) == 2)
            {
                _P2_Life = ReadByte(ReadPtr(_P2_LifePtr_Address) + 0xB8);
                //P1_Ammo = ReadByte(P1_StructAddress + 0x400);

                //[Damaged] custom Output                
                if (ReadByte(_P2_Damage_CaveAddress) == 1)
                    SetOutputValue(OutputId.P2_Damaged, 1);
            }
            WriteByte(_P2_Damage_CaveAddress, 0);            

            //Custom recoil will be recoil just like the original one
            SetOutputValue(OutputId.P1_CtmRecoil, ReadByte(_Outputs_Address) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_CtmRecoil, ReadByte(_Outputs_Address) >> 3 & 0x01);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            SetOutputValue(OutputId.Credits, (int)(ReadByte(_Credits_Address)));
        }

        #endregion
    }
}
