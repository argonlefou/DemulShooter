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
    class Game_WndMadBullets : Game
    {
        /*** MEMORY ADDRESSES **/
        private UInt32 _PlayerPlayingPtr_Offset = 0x001D55E8;
        private UInt32 _LifePtr_Offset = 0x001D28C0;
        private UInt32 _AmmoPtr_Offset = 0x001D2890;
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x0004FFA2, 7);
        private InjectionStruct _Damage_InjectionStruct = new InjectionStruct(0x000B1E92, 6);
        private UInt32 _Recoil_CaveAddress = 0;
        private UInt32 _Damage_CaveAddress = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndMadBullets(String RomName) 
            : base (RomName, "game")
        {
            _KnownMd5Prints.Add("Mad Bullets v1.0.2", "69807fe94a14ada9c792f20017d9a2a8");
            //Not sure about the version, mine is from IGG-Games

            _tProcess.Start();            
            Logger.WriteLog("Waiting for Windows Game " + _RomName + " game to hook.....");
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
                            //ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
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

        #region Memory Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _Recoil_CaveAddress = _OutputsDatabank_Address;
            _Damage_CaveAddress = _OutputsDatabank_Address + 0x04;

            SetHack_Recoil();
            SetHack_Damage();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Using Ammo count to compute recoil does not work :
        /// - Game has unlimited ammo bonus, and the number does not change during it
        /// Using this codecave, we can intercept call in function when the bullet is fired
        /// </summary>
        private void SetHack_Recoil()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //cmp dword ptr[edi+0F8h], 0
            CaveMemory.Write_StrBytes("83 BF F8 00 00 00 00");
            //mov byte ptr [_Recoil_CaveAddress],01
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Recoil_CaveAddress));
            CaveMemory.Write_StrBytes("01");

            //Inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
        }

        /// <summary>
        /// Intercepting call in function where player health should be decreased (or not) according to the game mode
        /// </summary>
        private void SetHack_Damage()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov al, [ecx+189h]
            CaveMemory.Write_StrBytes("8A 81 89 01 00 00");
            //mov byte ptr [_Damage_CaveAddress],01
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Damage_CaveAddress));
            CaveMemory.Write_StrBytes("01");

            //Inject it
            CaveMemory.InjectToOffset(_Damage_InjectionStruct, "Damage");
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Player status :
            //[0] = Not Playing
            //[1] = Playing
            UInt32 P1_Status = ReadByte(ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _PlayerPlayingPtr_Offset) + 0x1AC);
            _P1_Ammo = 0;
            _P1_Life = 0;

            if (P1_Status != 0)
            {
                _P1_Ammo = ReadByte(ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _AmmoPtr_Offset) + 0xD7);
                _P1_Life = ReadByte(ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _LifePtr_Offset) + 0x24);

                //Custom Recoil
                if (ReadByte(_Recoil_CaveAddress) == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);
                    WriteByte(_Recoil_CaveAddress, 0);
                }

                //[Clip Empty] custom Output
                if (_P1_Ammo <= 0)
                    SetOutputValue(OutputId.P1_Clip, 0);
                else
                    SetOutputValue(OutputId.P1_Clip, 1);

                //[Damaged] custom Output                
                if (ReadByte(_Damage_CaveAddress) == 1)
                {
                    SetOutputValue(OutputId.P1_Damaged, 1);
                    WriteByte(_Damage_CaveAddress, 0);
                }
            }
            else
            {
                SetOutputValue(OutputId.P1_Clip, 0);
            }

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
        }

        #endregion
    }
}
