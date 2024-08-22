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
    class Game_RtTargetTerror : Game
    {
        //Rom loaded + Rom version check
        private UInt32 _RomLoaded_check_Instruction_v212 = 0x080C73CB;

        //Memory value
        private UInt32 _gPlayers_Address = 0x081DDF00;
        private UInt32 _Player_State_Offset = 0x18;
        private UInt32 _Player_Life_Offset = 0x30;
        private UInt32 _Player_Weapon_Offset = 0x34;
        private UInt32 _Player_Ammo_Offset = 0x3A;
        private UInt32 _GS_PLAYING_minigame_loaded_Address = 0x8134AD8;
        //private UInt32 _CoinsNumber_Address = 0x081DFCE5;
        //private UInt32 _CoinValue_Address = 0x081DC304;
        private InjectionStruct _MiniGameRecoil_InjectionStruct = new InjectionStruct(0x080628D0, 6);

        //Custom data
        private UInt32 _P1_CustomRecoil_CaveAddress = 0;
        private UInt32 _P2_CustomRecoil_CaveAddress = 0;

        private int _P1_Last_Weapon = 0;
        private int _P2_Last_Weapon = 0;


         /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_RtTargetTerror(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "BudgieLoader", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Target Terror Gold - 2.12", "ff96481d27c47424bd7759f0213faf8f");

            _tProcess.Start();
            Logger.WriteLog("Waiting for Raw Thrill " + _RomName + " game to hook.....");
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
                            byte[] buffer = ReadBytes(_RomLoaded_check_Instruction_v212, 5);
                            if (buffer[0] == 0xC7 && buffer[1] == 0x05 && buffer[2] == 0xE5 && buffer[3] == 0xFC && buffer[4] == 0x1D)
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Target Terror Gold - 2.12 binary detected");
                                _TargetProcess_Md5Hash = _KnownMd5Prints["Target Terror Gold - 2.12"];
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

        //Teknoparrot does not emulate the gun board, so the game is not proceding to the real hardware motor engine
        //To get recoil, we cannot compute Ammo difference, because some weapons have infinite ammo
        //This game's procedure is called everytime a bullet is fire to choose what the game is going to do according to the current weapon.
        //Player index is in ESI
        protected override void Apply_OutputsMemoryHack()
        {
            //Create Databak to store our value
            Create_OutputsDataBank();
            _P1_CustomRecoil_CaveAddress = _OutputsDatabank_Address;
            _P2_CustomRecoil_CaveAddress = _OutputsDatabank_Address + 0x04;

            SetHack_MiniGameRecoil();
            
            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// There a a few different minigames, and usual weapon/ammo stuf can't be unsed in them
        /// On top of that, each one of them has it's own structure, scriipt and way of handling Trigger
        /// Most common point is the call to FBShot_Add(), we can intercept the call and use that Flag later 
        /// </summary>
        private void SetHack_MiniGameRecoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov ecx,[esp+08]
            CaveMemory.Write_StrBytes("8B 4C 24 08");
            //shl ecx,02
            CaveMemory.Write_StrBytes("C1 E1 02");
            //add ecx, _P1_CustomRecoil_CaveAddress
            CaveMemory.Write_StrBytes("81 C1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_CustomRecoil_CaveAddress));
            //mov byte ptr[ecx], 1
            CaveMemory.Write_StrBytes("C6 01 01");
            //mov edx,[0812F864]
            CaveMemory.Write_StrBytes("8B 15 64 F8 12 08");
            //Inject it
            CaveMemory.InjectToAddress(_MiniGameRecoil_InjectionStruct, "Recoil");
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
            //_Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {            
            //Custom Outputs
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            //Gamestate:
            // 02 = Title screen / Attract DEMO
            // 04 = Level Select
            // 05 = InGame
            // 08 = Game Over screen
            // 12 = Test Mode
            
            UInt32 P1_Address = _gPlayers_Address;
            UInt32 P2_Address = _gPlayers_Address + 0x558;

            //Check Player Status:
            //0 = Playing, dead
            //1 = Playing, alive
            //2 = Continue screen
            //3 = Title/Attract
            if (ReadByte(P1_Address + _Player_State_Offset) == 1)
            {
                //Force Start Lamp to Off
                SetOutputValue(OutputId.P1_CtmLmpStart, 0);

                //If a mini game is loaded, using the custom recoil flag
                if (ReadByte(_GS_PLAYING_minigame_loaded_Address) == 01)
                {
                    if (ReadByte(_P1_CustomRecoil_CaveAddress) == 1)
                    {
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);
                        WriteByte(_P1_CustomRecoil_CaveAddress, 0);
                    }
                }
                else
                {
                    _P1_Ammo = ReadByte(P1_Address + _Player_Ammo_Offset);
                    if (_P1_Ammo < 0)
                        _P1_Ammo = 0;

                    _P1_Life = ReadByte(P1_Address + _Player_Life_Offset);
                    if (_P1_Life < 0)
                        _P1_Life = 0;

                    //To get recoil, we"ll check what kind of weapon is used and calculate accordingly
                    //Some weapons have standard recoil based on Ammo, other don't (Flame Thrower)
                    // 1 = Beretta
                    // 2 = Machinegun
                    // 3 = Shotgun
                    // 4 = Grenade Launcher
                    // 5 = RPP
                    // 6 = Flame Thrower
                    // 7 - Shocker
                    // 8 - FreezeGun

                    // - NOTE - Recoil in Hunt game (bonus game) is not activated here. Look at 0x080B4390 for ammo decrease (need to verify for P2, also)
                    //Maybe check game mode ???
                    int P1_Weapon = ReadByte(P1_Address + _Player_Weapon_Offset);
                    if (P1_Weapon != 6)
                    {
                        if (_P1_Last_Weapon == P1_Weapon)
                        {
                            if (_P1_Ammo < _P1_LastAmmo)
                                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                        }
                    }
                    _P1_Last_Weapon = P1_Weapon;

                    //[Clip] custom Output   
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

            //Same thing for P2
            if (ReadByte(P2_Address + _Player_State_Offset) == 1)
            {
                //Force Start Lamp to Off
                SetOutputValue(OutputId.P2_CtmLmpStart, 0);

                //If a mini game is loaded, using the custom recoil flag
                if (ReadByte(_GS_PLAYING_minigame_loaded_Address) == 01)
                {
                    if (ReadByte(_P2_CustomRecoil_CaveAddress) == 1)
                    {
                        SetOutputValue(OutputId.P2_CtmRecoil, 1);
                        WriteByte(_P2_CustomRecoil_CaveAddress, 0);
                    }
                }
                else
                {

                    _P2_Ammo = ReadByte(P2_Address + _Player_Ammo_Offset);
                    if (_P2_Ammo < 0)
                        _P2_Ammo = 0;

                    _P2_Life = ReadByte(P2_Address + _Player_Life_Offset);
                    if (_P2_Life < 0)
                        _P2_Life = 0;

                    int P2_Weapon = ReadByte(P2_Address + _Player_Weapon_Offset);
                    if (P2_Weapon != 6)
                    {
                        if (_P2_Last_Weapon == P2_Weapon)
                        {
                            if (_P2_Ammo < _P2_LastAmmo)
                                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                        }
                    }
                    _P2_Last_Weapon = P2_Weapon;

                    //[Clip] custom Output   
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

            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;
            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
            
            // For Credits, we need to compute the coin number * coin value
            /*float fCoinValue = BitConverter.ToSingle(ReadBytes(_CoinValue_Address, 4), 0);
            UInt32 fCoinNumber = BitConverter.ToUInt32(ReadBytes(_CoinsNumber_Address, 4), 0);
            float fCredits = fCoinValue * (float)fCoinNumber;
            SetOutputValue(OutputId.Credits, (int)fCredits);*/
        }

        #endregion   
    }
}
