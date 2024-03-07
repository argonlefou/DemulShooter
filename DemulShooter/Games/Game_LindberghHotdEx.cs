using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;

namespace DemulShooter
{
    class Game_LindberghHotdEx : Game
    {
        //Rom loaded + Rom version check
        private UInt32 _RomLoaded_check_Instruction = 0x0815C65A;   //Intruction writing Lamps

        //Outputs
        private UInt32 _OutputsPtr_Address = 0x0A9DFEBC;
        private UInt32 _Outputs_Address;
        private UInt32 _GameMgr_Pointer_Address = 0xA9DFE98;
        private UInt32 _Credits_Address = 0xAA3DD80;
        private UInt32 _Freeplay_Address = 0xAA3DD67;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_LindberghHotdEx(String RomName, bool DisableInputHack, bool Verbose)
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
                            byte[] buffer = ReadBytes(_RomLoaded_check_Instruction, 5);
                            if (buffer[0] == 0x8B && buffer[1] == 0x15 && buffer[2] == 0xBC && buffer[3] == 0xFE && buffer[4] == 0x9D)
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
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

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : Is activated permanently while trigger is pressed
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpPanel, OutputId.LmpPanel));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp1, OutputId.Lmp1));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp2, OutputId.Lmp2));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp3, OutputId.Lmp3));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp4, OutputId.Lmp4));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp5, OutputId.Lmp5));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp6, OutputId.Lmp6));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpFoot, OutputId.P1_LmpFoot));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpFoot, OutputId.P2_LmpFoot));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            /*
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));            
             */
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            _Outputs_Address = BitConverter.ToUInt32(ReadBytes(_OutputsPtr_Address, 4), 0);
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Outputs_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.LmpPanel, ReadByte(_Outputs_Address + 1) >> 7 & 0x01);
            SetOutputValue(OutputId.Lmp1, ReadByte(_Outputs_Address + 1) >> 6 & 0x01);
            SetOutputValue(OutputId.Lmp2, ReadByte(_Outputs_Address + 1) >> 5 & 0x01);
            SetOutputValue(OutputId.Lmp3, ReadByte(_Outputs_Address + 1) >> 4 & 0x01);
            SetOutputValue(OutputId.Lmp4, ReadByte(_Outputs_Address + 1) >> 3 & 0x01);
            SetOutputValue(OutputId.Lmp5, ReadByte(_Outputs_Address + 1) >> 2 & 0x01);
            SetOutputValue(OutputId.Lmp6, ReadByte(_Outputs_Address + 1) >> 1 & 0x01);
            SetOutputValue(OutputId.P1_LmpFoot, ReadByte(_Outputs_Address) >> 5 & 0x01);
            SetOutputValue(OutputId.P2_LmpFoot, ReadByte(_Outputs_Address) >> 4 & 0x01);            

            //Custom Outputs
            int P1_Life = 0;
            int P2_Life = 0;

            UInt32 GameManager_Address = ReadPtr(_GameMgr_Pointer_Address);
            if (GameManager_Address != 0)
            {
                P1_Life = ReadByte(GameManager_Address + 0x48);
                if (P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                P2_Life = ReadByte(GameManager_Address + 0x4C);
                if (P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);
            }

            _P1_LastLife = P1_Life;
            _P2_LastLife = P2_Life;

            

            /*_P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;
            if (ReadPtr(_GameInfos_Address) != 0)
            {
                _Life = ReadByte(ReadPtr(_GameInfos_Address) + 0x40);
                //[Damaged] custom Output                
                if (_Life < _LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                _P1_Ammo = ReadByte(ReadPtrChain(_GameInfos_Address, new UInt32[] { 0x34 }) + 0x2D4);
                //[Clip] custom Output   
                if (_P1_Ammo > 0)
                    P1_Clip = 1;
                //No attract mode so we can just reuse the original recoil flag
                SetOutputValue(OutputId.P1_CtmRecoil, P1_Motor_Status);

                _P2_Ammo = ReadByte(ReadPtrChain(_GameInfos_Address, new UInt32[] { 0x38 }) + 0x2D4);
                //[Clip] custom Output   
                if (_P2_Ammo > 0)
                    P2_Clip = 1;
                //No attract mode so we can just reuse the original recoil flag
                SetOutputValue(OutputId.P2_CtmRecoil, P2_Motor_Status);
            }

            _LastLife = _Life;
            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _Life);*/

            SetOutputValue(OutputId.P1_Life, P1_Life);
            SetOutputValue(OutputId.P2_Life, P2_Life);

            //Displaying Credits if no FREEPLAY enabled
            if (ReadByte(_Freeplay_Address) == 0)
                SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
            else
                SetOutputValue(OutputId.Credits, 0);
        }

        #endregion
    }
}
