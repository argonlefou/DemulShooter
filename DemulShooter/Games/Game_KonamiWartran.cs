using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;

namespace DemulShooter
{
    class Game_KonamiWartran : Game
    {
        //Outputs variable  
        private UInt32 _OutputLamps_Offset = 0x0149E37C;
        private UInt32 _P1_WeaponStruct_Offset = 0x01475A90;
        private UInt32 _P2_WeaponStruct_Offset = 0x01475AB4;
        private UInt32 _P3_WeaponStruct_Offset = 0x01475AD8;
        private UInt32 _P4_WeaponStruct_Offset = 0x01475AFC;
        private UInt32 _P1_Life_Offset = 0x0130600C;
        private UInt32 _P2_Life_Offset = 0x01306208;
        private UInt32 _P3_Life_Offset = 0x01306404;
        private UInt32 _P4_Life_Offset = 0x01306600;
        private UInt32 _Credits_Offset = 0x001AC55C;
        //
        private byte _P1_LastWeaponType = 0;
        private byte _P2_LastWeaponType = 0;
        private byte _P3_LastWeaponType;
        private byte _P4_LastWeaponType;
        private byte _bP1_LastAmmo;
        private byte _bP2_LastAmmo;
        private byte _bP3_LastAmmo;
        private byte _bP4_LastAmmo;
        private byte _bP1_LastLife;
        private byte _bP2_LastLife;
        private byte _bP3_LastLife;
        private byte _bP4_LastLife;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_KonamiWartran(String RomName)
            : base(RomName, "game")
        {
            _KnownMd5Prints.Add("Wartran Original dump", "4c987ea93e8dbd5a6aa624e504a4706c");

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
                            /*if (!_DisableInputHack)
                                SetHack();
                            else
                                Logger.WriteLog("Input Hack disabled");*/
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

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : Is activated for every bullet fired AND when player gets
            _Outputs = new List<GameOutput>();

            //Gun motor  : Is activated for every bullet fired AND when player gets
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_LmpStart, OutputId.P3_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_LmpStart, OutputId.P4_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Ammo, OutputId.P3_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Ammo, OutputId.P4_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Clip, OutputId.P2_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Clip, OutputId.P3_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Clip, OutputId.P4_Clip));            
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P3_CtmRecoil, OutputId.P3_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P4_CtmRecoil, OutputId.P4_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Life, OutputId.P3_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Life, OutputId.P4_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P3_Damaged, OutputId.P3_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P4_Damaged, OutputId.P4_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _OutputLamps_Offset) >> 3 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _OutputLamps_Offset) >> 2 & 0x01);
            SetOutputValue(OutputId.P3_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _OutputLamps_Offset) >> 1 & 0x01);
            SetOutputValue(OutputId.P4_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _OutputLamps_Offset) & 0x01);

            byte P1_WeaponType = 0;
            byte P2_WeaponType = 0;
            byte P3_WeaponType = 0;
            byte P4_WeaponType = 0;
            byte P1_Life = 0;
            byte P2_Life = 0;
            byte P3_Life = 0;
            byte P4_Life = 0;
            byte P1_Ammo = 0;
            byte P2_Ammo = 0;
            byte P3_Ammo = 0;
            byte P4_Ammo = 0;

            //Player1
            P1_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Life_Offset);
            SetOutputValue(OutputId.P1_Life, P1_Life);
            if (P1_Life < _bP1_LastLife)
                SetOutputValue(OutputId.P1_Damaged, 1);

            if (P1_Life > 0)
            {
                //there are a lot of weapons in the game, so we need to check if the type has changed before counting the ammo decrease
                P1_WeaponType = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_WeaponStruct_Offset + 0x0E);
                P1_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_WeaponStruct_Offset + 0x10 + P1_WeaponType);
                if (P1_WeaponType == _P1_LastWeaponType)
                {
                    if (P1_Ammo < _bP1_LastAmmo)
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);  
                    if (P1_Ammo > 0)
                        SetOutputValue(OutputId.P1_Clip, 1);  
                }
            }
            SetOutputValue(OutputId.P1_Ammo, P1_Ammo);

            //Player2
            P2_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Life_Offset);
            SetOutputValue(OutputId.P2_Life, P2_Life);
            if (P2_Life < _bP2_LastLife)
                SetOutputValue(OutputId.P2_Damaged, 1);

            if (P2_Life > 0)
            {
                //there are a lot of weapons in the game, so we need to check if the type has changed before counting the ammo decrease
                P2_WeaponType = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_WeaponStruct_Offset + 0x0E);
                P2_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_WeaponStruct_Offset + 0x10 + P2_WeaponType);
                if (P2_WeaponType == _P2_LastWeaponType)
                {
                    if (P2_Ammo < _bP2_LastAmmo)
                        SetOutputValue(OutputId.P2_CtmRecoil, 1);
                    if (P2_Ammo > 0)
                        SetOutputValue(OutputId.P2_Clip, 1);
                }
            }
            SetOutputValue(OutputId.P2_Ammo, P2_Ammo);

            //Player3
            P3_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P3_Life_Offset);
            SetOutputValue(OutputId.P3_Life, P3_Life);
            if (P3_Life < _bP3_LastLife)
                SetOutputValue(OutputId.P3_Damaged, 1);

            if (P3_Life > 0)
            {
                //there are a lot of weapons in the game, so we need to check if the type has changed before counting the ammo decrease
                P3_WeaponType = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P3_WeaponStruct_Offset + 0x0E);
                P3_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P3_WeaponStruct_Offset + 0x10 + P3_WeaponType);
                if (P3_WeaponType == _P3_LastWeaponType)
                {
                    if (P3_Ammo < _bP3_LastAmmo)
                        SetOutputValue(OutputId.P3_CtmRecoil, 1);
                    if (P3_Ammo > 0)
                        SetOutputValue(OutputId.P3_Clip, 1);
                }
            }
            SetOutputValue(OutputId.P3_Ammo, P3_Ammo);

            //Player4
            P4_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P4_Life_Offset);
            SetOutputValue(OutputId.P4_Life, P4_Life);
            if (P4_Life < _bP4_LastLife)
                SetOutputValue(OutputId.P4_Damaged, 1);

            if (P4_Life > 0)
            {
                //there are a lot of weapons in the game, so we need to check if the type has changed before counting the ammo decrease
                P4_WeaponType = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P4_WeaponStruct_Offset + 0x0E);
                P4_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P4_WeaponStruct_Offset + 0x10 + P4_WeaponType);
                if (P4_WeaponType == _P4_LastWeaponType)
                {
                    if (P4_Ammo < _bP4_LastAmmo)
                        SetOutputValue(OutputId.P4_CtmRecoil, 1);
                    if (P4_Ammo > 0)
                        SetOutputValue(OutputId.P4_Clip, 1);
                }
            }
            SetOutputValue(OutputId.P4_Ammo, P4_Ammo);


            _bP1_LastAmmo = P1_Ammo;
            _bP2_LastAmmo = P2_Ammo;
            _bP3_LastAmmo = P3_Ammo;
            _bP4_LastAmmo = P4_Ammo;
            _bP1_LastLife = P1_Life;
            _bP2_LastLife = P2_Life;
            _bP3_LastLife = P3_Life;
            _bP4_LastLife = P4_Life;
            _P1_LastWeaponType = P1_WeaponType;
            _P2_LastWeaponType = P2_WeaponType;
            _P3_LastWeaponType = P3_WeaponType;
            _P4_LastWeaponType = P4_WeaponType;

            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset));
        }


        #endregion
    }
}
