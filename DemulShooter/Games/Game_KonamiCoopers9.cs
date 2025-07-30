using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;

namespace DemulShooter.Games
{
    class Game_KonamiCoopers9 : Game
    {
        //Outputs variable  
        private UInt32 _Outputs_Offset = 0x01FE7F48;
        private UInt32 _P1_Life_Offset = 0x024FCD10;
        private UInt32 _P2_Life_Offset = 0x024FCDD8;
        private UInt32 _P1_Ammo_Offset = 0x024FCDC2;
        private UInt32 _P2_Ammo_Offset = 0x024FCE8A;
        private UInt32 _Credits_Offset = 0x01FE8188;
        //private UInt32 _GameScene_Offset = 0x01FE7C97;
        private UInt32 _PlayersStatus_Offset = 0x01FE7F4E;
        //<-- Injection to use if reading the outputs byte is not fast enough to get recoil change -->
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x00419495, 6);

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_KonamiCoopers9(String RomName)
            : base(RomName, "game")
        {
            //JConfig and Teknoparrot are sharing the same offsets, non need for memory files for now... 
            _KnownMd5Prints.Add("Cooper9 - TeknoParrot", "541d457e167451235d521f4e3a19cdde");
            _KnownMd5Prints.Add("Cooper9 - JConfig", "6d1fa58318e7fa4c26e26913e9968ca9");

            _tProcess.Start();
            Logger.WriteLog("Waiting for Konami " + _RomName + " game to hook.....");
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
                    foreach (Process p in Process.GetProcessesByName(_Target_Process_Name))
                    {
                        _TargetProcess = p;
                        _ProcessHandle = _TargetProcess.Handle;
                        _TargetProcess_MemoryBaseAddress = _TargetProcess.MainModule.BaseAddress;

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            _GameWindowHandle = _TargetProcess.MainWindowHandle;
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();
                            /*if (!_DisableInputHack)
                                SetHack();
                            else
                                Logger.WriteLog("Input Hack disabled");*/
                            _ProcessHooked = true;
                            RaiseGameHookedEvent();
                            break;
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
            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset) & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset) >> 1 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset) >> 7 & 0x01);
            SetOutputValue(OutputId.P1_CtmRecoil, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_CtmRecoil, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset) >> 7 & 0x01);

            byte P1_Life = 0;
            byte P2_Life = 0;
            byte P1_Ammo = 0;
            byte P2_Ammo = 0;

            //Checking if the player is playing, to remove risks of outputs triggered during attract
            if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersStatus_Offset) == 1)
            {
                //Player1
                P1_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Life_Offset);                
                if (P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                if (P1_Life > 0)
                    P1_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Ammo_Offset);
                
            }
            if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersStatus_Offset + 0x01) == 1)
            { 
                //Player2
                P2_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Life_Offset);
                if (P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);

                if (P2_Life > 0)
                    P2_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Ammo_Offset);                    
            }

            SetOutputValue(OutputId.P1_Life, P1_Life);
            SetOutputValue(OutputId.P2_Life, P2_Life);
            SetOutputValue(OutputId.P1_Ammo, P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, P2_Ammo);

            _P1_LastAmmo = P1_Ammo;
            _P2_LastAmmo = P2_Ammo;
            _P1_LastLife = P1_Life;
            _P2_LastLife = P2_Life;           

            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
