using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_RtJurassicPark : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\lindbergh\jpark";

        //Outputs Address
        private UInt32 _ReticleShowODR_Call_Address = 0x0819295A;
        private UInt32 _Lasers_Injection_Address = 0x0817FEF9;
        private UInt32 _Lasers_Injection_Return_Address = 0x0817FEFF;
        private UInt32 _Damage_Injection_Addres = 0x08174C6D;
        private UInt32 _Damage_Injection_Return_Addres = 0x08174C73;
        private UInt32 _Recoil01_Injection_Address = 0x081778C3;
        private UInt32 _Recoil01_Injection_Return_Address = 0x081778C9;
        private UInt32 _Recoil02_Injection_Address = 0x0817AB08;
        private UInt32 _Recoil02_Injection_Return_Address = 0x0817AB10;
        private UInt32 _gGameRecord_Address = 0x08D1F720;
        private UInt32 _gLampsVal_Address = 0x08B4CF60;
        private UInt32 P1_Weapon_Address = 0x8D20560;
        private UInt32 P2_Weapon_Address = 0x8D20564;
        private UInt32 P1_AmmoElectro_Address = 0x8D1F8A0;
        private UInt32 P2_AmmoElectro_Address = 0x8D1F8A4;
        private UInt32 P1_AmmoShotGun_Address = 0x8B4BF64;
        private UInt32 P2_AmmoShotGun_Address = 0x8B4BF88;
        private UInt32 P1_AmmoFreezeRay_Address = 0x8D1FAA8;
        private UInt32 P2_AmmoFreezeRay_Address = 0x8D1FAAC;
        private UInt32 P1_AmmoMinoGun_Address = 0x8B4BE44;
        private UInt32 P2_AmmoMinoGun_Address = 0x8B4BE74;

        private UInt32 _P1_RecoilStatus_CaveAddress = 0;
        private UInt32 _P2_RecoilStatus_CaveAddress = 0;
        private UInt32 _P1_DamageStatus_CaveAddress = 0;
        private UInt32 _P2_DamageStatus_CaveAddress = 0;


        //Rom loaded + Rom version check
        private UInt32 _RomLoaded_check_Instruction_v108 = 0x08188F10;
        //private UInt32 _RomLoaded_check_Instruction_v133 = 0x08153065;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_RtJurassicPark(String RomName)
            : base(RomName, "BudgieLoader")
        {
            //Only for documentation, version check is done by reading code position in memory, as we don't have access to the ELF path
            _KnownMd5Prints.Add("Jurassic Park - v1.08 (unpatched ?)", "f24794f1bc8bf93031206578e4bdabf5");
            _KnownMd5Prints.Add("Jurassic Park - v1.08", "c62483935c2ea3c8387f33b3c8b89c6b");            

            _tProcess.Start();
            Logger.WriteLog("Waiting for Lindbergh " + _RomName + " game to hook.....");
        }

        // <summary>
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
                            //And this instruction is also helping us detecting whether the game file is v1.08 or v1.33 binary, to call the corresponding hack
                            byte[] buffer = ReadBytes(_RomLoaded_check_Instruction_v108, 5);
                            if (buffer[0] == 0x83 && buffer[1] == 0xEC && buffer[2] == 0x0C && buffer[3] == 0x8B && buffer[4] == 0x4C)
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Jurassic Park - v1.08 binary detected");
                                _TargetProcess_Md5Hash = _KnownMd5Prints["Jurassic Park - v1.08"];
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                                Apply_MemoryHacks();
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
                            }
                            else
                            {
                                /*buffer = ReadBytes(_RomLoaded_check_Instruction_RevC, 5);
                                if (buffer[0] == 0xE8 && buffer[1] == 0x42 && buffer[2] == 0x0D && buffer[3] == 0x00 && buffer[4] == 0x00)
                                {
                                    _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                    Logger.WriteLog("House Of The Dead 4 - Rev.C binary detected");
                                    _TargetProcess_Md5Hash = _KnownMd5Prints["House of The Dead 4 - Rev.C"];
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                    ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                                    if (!_DisableInputHack)
                                        SetHack();
                                    else
                                        Logger.WriteLog("Input Hack disabled");
                                    _ProcessHooked = true;
                                    RaiseGameHookedEvent();
                                }
                                else
                                {
                                    Logger.WriteLog("Game not Loaded, waiting...");
                                }*/
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

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_RecoilStatus_CaveAddress = _OutputsDatabank_Address;
            _P2_RecoilStatus_CaveAddress = _OutputsDatabank_Address + 4;
            _P1_DamageStatus_CaveAddress = _OutputsDatabank_Address + 0x10;
            _P2_DamageStatus_CaveAddress = _OutputsDatabank_Address + 0x14;

            SetHack_Damage();
            SetHack_Recoil1();
            SetHack_Recoil2();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }
        
        /// <summary>
        /// Remove Laser aiming display
        /// </summary>
        private void SetHack_Lasers()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov [esp+20],00000000
            CaveMemory.Write_StrBytes("C7 44 24 20 00 00 00 00");
            //mov [esp+24],00000000
            CaveMemory.Write_StrBytes("C7 44 24 24 00 00 00 00");
            //mov eax,[ebx+00000228]
            CaveMemory.Write_StrBytes("8B 83 28 02 00 00");

            CaveMemory.Write_jmp(_Lasers_Injection_Return_Address);

            Logger.WriteLog("Adding Lasers Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - _Lasers_Injection_Address - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, _Lasers_Injection_Address, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        /// <summary>
        /// Intercep call to log damage taken by a player to set a custom flag to "1"
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //shl ebx,02
            CaveMemory.Write_StrBytes("C1 E3 02");
            //add abx, _P1_DamageStatus_CaveAddress
            CaveMemory.Write_StrBytes("81 C3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_DamageStatus_CaveAddress));
            //mov [ebx],00000001
            CaveMemory.Write_StrBytes("C7 03 01 00 00 00");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //fld dword ptr [08D1F798]
            CaveMemory.Write_StrBytes("D9 05 98 F7 D1 08");

            CaveMemory.Write_jmp(_Damage_Injection_Return_Addres);

            Logger.WriteLog("Adding Damage Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - _Damage_Injection_Addres - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, _Damage_Injection_Addres, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        /// <summary>
        /// Intercept call for FFPulse() function to create our own flag
        /// Called when following weapon starts shooting :
        /// - Default Gun
        /// - Jeep Gun
        /// - Shotgun
        /// - FreezeGun
        /// - Minigun
        /// 
        /// Does not work for ElectroGun
        /// </summary>
        private void SetHack_Recoil1()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov eax,[esp+10]
            CaveMemory.Write_StrBytes("8B 44 24 10");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _P1_RecoilStatus_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_RecoilStatus_CaveAddress));
            //mov [eax],00000001
            CaveMemory.Write_StrBytes("C7 00 01 00 00 00");            
            //sub esp,00000330
            CaveMemory.Write_StrBytes("81 EC 30 03 00 00");

            CaveMemory.Write_jmp(_Recoil01_Injection_Return_Address);

            Logger.WriteLog("Adding FFPulse Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - _Recoil01_Injection_Address - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, _Recoil01_Injection_Address, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        /// <summary>
        /// Intercept call for Start SHoot for ElectroGun
        /// </summary>
        private void SetHack_Recoil2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //shl ebx,02
            CaveMemory.Write_StrBytes("C1 E3 02");
            //add abx, _P1_RecoilStatus_CaveAddress
            CaveMemory.Write_StrBytes("81 C3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_RecoilStatus_CaveAddress));
            //mov [ebx],00000001
            CaveMemory.Write_StrBytes("C7 03 01 00 00 00");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //mov [esp+04],00000004
            CaveMemory.Write_StrBytes("C7 44 24 04 04 00 00 00");

            CaveMemory.Write_jmp(_Recoil02_Injection_Return_Address);

            Logger.WriteLog("Adding ElectroGun Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - _Recoil02_Injection_Address - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, _Recoil02_Injection_Address, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        protected override void Apply_NoCrosshairMemoryHack()
        {
            if (_HideCrosshair)
            {
                //Replacing call to mShowODR() for reticle object by a call to mHideODR()
                WriteBytes(_ReticleShowODR_Call_Address + 1, new byte[] { 0x11, 0x86 });
                //Codecave to remove Lasers
                SetHack_Lasers();
            }
            else
            {
                //Setting it back to mShowODR() if it was changed by a previous instance of DemulSHooter
                WriteBytes(_ReticleShowODR_Call_Address + 1, new byte[] { 0xF1, 0x85 });
                //Again, putting back original code for lasers instead of Codecave
                WriteBytes(_Lasers_Injection_Address, new byte[] { 0x8B, 0x83, 0x28, 0x02, 0x00, 0x00 });
            }
        }

        #endregion

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
            _Outputs.Add(new GameOutput(OutputDesciption.LmpDinoHead, OutputId.LmpDinoHead));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpLogo, OutputId.LmpLogo));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpDinoEyes, OutputId.LmpDinoEyes));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpRoof, OutputId.LmpRoof));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpMarquee, OutputId.LmpMarquee));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpDash, OutputId.LmpDash));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpFoliage, OutputId.LmpFoliage));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun_R, OutputId.P1_LmpGun_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun_G, OutputId.P1_LmpGun_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun_B, OutputId.P1_LmpGun_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun_R, OutputId.P2_LmpGun_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun_G, OutputId.P2_LmpGun_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun_B, OutputId.P2_LmpGun_B));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpSeat_R, OutputId.LmpSeat_R));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpSeat_G, OutputId.LmpSeat_G));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpSeat_B, OutputId.LmpSeat_B));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpBenchLogo, OutputId.LmpBenchLogo)); 
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpHolder, OutputId.P1_LmpHolder));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpHolder, OutputId.P2_LmpHolder));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpSeatBase, OutputId.LmpSeatBase));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpEstop, OutputId.LmpEstop));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpCompressor, OutputId.LmpCompressor));

            /*_Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));*/  

            //Custom Outputs
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            SetOutputValue(OutputId.P1_LmpStart, GetLampValueAsInt(0));
            SetOutputValue(OutputId.P2_LmpStart, GetLampValueAsInt(1));
            SetOutputValue(OutputId.LmpDinoHead, GetLampValueAsInt(2));
            SetOutputValue(OutputId.LmpLogo, GetLampValueAsInt(3));
            SetOutputValue(OutputId.LmpDinoEyes, GetLampValueAsInt(4));
            SetOutputValue(OutputId.LmpRoof, GetLampValueAsInt(5));
            SetOutputValue(OutputId.LmpMarquee, GetLampValueAsInt(6));
            SetOutputValue(OutputId.LmpDash, GetLampValueAsInt(7));
            SetOutputValue(OutputId.LmpFoliage, GetLampValueAsInt(8));
            SetOutputValue(OutputId.P1_LmpGun_R, GetLampValueAsInt(9));
            SetOutputValue(OutputId.P1_LmpGun_G, GetLampValueAsInt(10));
            SetOutputValue(OutputId.P1_LmpGun_B, GetLampValueAsInt(11));
            SetOutputValue(OutputId.P2_LmpGun_R, GetLampValueAsInt(12));
            SetOutputValue(OutputId.P2_LmpGun_G, GetLampValueAsInt(13));
            SetOutputValue(OutputId.P2_LmpGun_B, GetLampValueAsInt(14));
            SetOutputValue(OutputId.LmpSeat_R, GetLampValueAsInt(15));
            SetOutputValue(OutputId.LmpSeat_G, GetLampValueAsInt(16));
            SetOutputValue(OutputId.LmpSeat_B, GetLampValueAsInt(17));
            SetOutputValue(OutputId.LmpBenchLogo, GetLampValueAsInt(18));
            SetOutputValue(OutputId.P1_LmpHolder, GetLampValueAsInt(19));
            SetOutputValue(OutputId.P2_LmpHolder, GetLampValueAsInt(20));
            SetOutputValue(OutputId.LmpSeatBase, GetLampValueAsInt(21));
            SetOutputValue(OutputId.LmpEstop, GetLampValueAsInt(22));
            SetOutputValue(OutputId.LmpCompressor, GetLampValueAsInt(23));

            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;

            if (GameFlow_Get_PlayerActive(0))
            {
                switch(ReadByte(P1_Weapon_Address))
                {
                    //default Gun / Jeep Gun
                    case 0:
                        {
                            _P1_Ammo = 99;
                        }break;
                    //Electro Gun
                    case 4:
                        {
                            _P1_Ammo = (int)ReadPtr(P1_AmmoElectro_Address);
                        }break;
                    //Shotgun
                    case 6:
                        {
                            _P1_Ammo = (int)ReadPtr(P1_AmmoShotGun_Address);
                        } break;
                    //FreezeRay
                    case 7:
                        {
                            _P1_Ammo = (int)ReadPtr(P1_AmmoFreezeRay_Address);
                        } break;
                    //Minigun
                    case 8:
                        {
                            _P1_Ammo = (int)ReadPtr(P1_AmmoMinoGun_Address);
                        } break;
                }

                UInt32 PlayerRecord_Address = GameFlow_Get_PlayerRecord(0);
                if (PlayerRecord_Address != 0)
                {
                    _P1_Life = (int)BitConverter.ToSingle(ReadBytes(PlayerRecord_Address + 0xC, 4), 0);
                }

                //[Damaged] custom Output
                if (ReadByte(_P1_DamageStatus_CaveAddress) == 1)
                {
                    SetOutputValue(OutputId.P1_Damaged, 1);
                }
                
                //[Recoil] custom Output
                if (ReadByte(_P1_RecoilStatus_CaveAddress) == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);
                }
            }

            if (GameFlow_Get_PlayerActive(1))
            {
                switch (ReadByte(P2_Weapon_Address))
                {
                    //default Gun
                    case 0:
                        {
                            _P2_Ammo = 0;
                        } break;
                    //Electro Gun
                    case 4:
                        {
                            _P2_Ammo = (int)ReadPtr(P2_AmmoElectro_Address);
                        } break;
                    //Shotgun
                    case 6:
                        {
                            _P2_Ammo = (int)ReadPtr(P2_AmmoShotGun_Address);
                        } break;
                    //FreezeRay
                    case 7:
                        {
                            _P2_Ammo = (int)ReadPtr(P2_AmmoFreezeRay_Address);
                        } break;
                    //Minigun
                    case 8:
                        {
                            _P2_Ammo = (int)ReadPtr(P2_AmmoMinoGun_Address);
                        } break;
                }

                UInt32 PlayerRecord_Address = GameFlow_Get_PlayerRecord(1);
                if (PlayerRecord_Address != 0)
                {
                    _P2_Life = (int)BitConverter.ToSingle(ReadBytes(PlayerRecord_Address + 0xC, 4), 0);
                }

                //[Damaged] custom Output
                if (ReadByte(_P2_DamageStatus_CaveAddress) == 1)
                {
                    SetOutputValue(OutputId.P2_Damaged, 1);                    
                }
                
                //[Recoil] custom Output
                if (ReadByte(_P2_RecoilStatus_CaveAddress) == 1)
                {
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);                    
                }
            }

            //Reset custom Outputs, even if the player is not active.
            //If not, the value would stay UP if player not active and upper loop not entered
            WriteByte(_P1_DamageStatus_CaveAddress, 0x00);
            WriteByte(_P1_RecoilStatus_CaveAddress, 0x00);
            WriteByte(_P2_DamageStatus_CaveAddress, 0x00);
            WriteByte(_P2_RecoilStatus_CaveAddress, 0x00);


            //
            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
        }

        private int GetLampValueAsInt(int LmpId)
        {
            float fLmpValue = BitConverter.ToSingle(ReadBytes(_gLampsVal_Address + (uint)(4 * LmpId), 4), 0);
            int iLmpValue = (int)(fLmpValue * 100);
            return iLmpValue;
        }

        /// <summary>
        /// Redoing the original function in order to get the pointer struct info for a player
        /// The game initially stores data in a chained list, meaning that the first player ti enter the game is in the first item, etc....
        /// So item 0 can be either P1 or P2, and we can't just read a byte to know
        /// </summary>
        /// <param name="PlayerId"></param>
        private UInt32 GameFlow_Get_PlayerRecord(int PlayerId)
        {
            UInt32 Buffer1 = ReadPtr(_gGameRecord_Address);
            UInt32 Buffer2 = 0;

            if (Buffer1 != 0)
            {
                while (true)
                {
                    Buffer2 = ReadPtr(Buffer1 + 0x8);
                    if ((int)ReadByte(Buffer2) == PlayerId)
                    {
                        if (ReadByte(Buffer2 + 0x10) != 0)
                            break;
                    }
                    Buffer1 = ReadPtr(Buffer1);
                    if (Buffer1 == 0)
                        return 0;
                }
                return Buffer2;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Redoing the original function in order to know if a player is playing
        /// The game initially stores data in a chained list, meaning that the first player ti enter the game is in the first item, etc....
        /// So item 0 can be either P1 or P2, and we can't just read a byte to know
        /// </summary>
        /// <param name="PlayerId"></param>
        private bool GameFlow_Get_PlayerActive(int PlayerId)
        {
            UInt32 Buffer1 = ReadPtr(_gGameRecord_Address);
            UInt32 Buffer2 = 0;

            if (Buffer1 != 0)
            {
                while (true)
                {
                    Buffer2 = ReadPtr(Buffer1 + 0x8);
                    if ((int)ReadByte(Buffer2) == PlayerId)
                    {
                        if (ReadByte(Buffer2 + 0x10) != 0)
                            break;
                    }
                    Buffer1 = ReadPtr(Buffer1);
                    if (Buffer1 == 0)
                        return false;
                }
                return true;
            }
            else
            {
                return false;
            } 
        }

        #endregion
    }
}
