using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_RtTerminatorSalvation : Game
    {
        //Rom loaded + Rom version check
        private UInt32 _RomLoaded_check_Instruction_v125 = 0x0811F6A0;

        //Outputs Address
        private UInt32 _P1_Ammo_Address = 0x88DBF68;
        private UInt32 _P2_Ammo_Address = 0x88DBF7C;
        private UInt32 _P1_WeaponNumber_Address = 0x88A5F80;
        private UInt32 _P2_WeaponNumber_Address = 0x88A5FE0;

        private UInt32 _Lamp_Address = 0x88AE748;
        private UInt32 _GameState_Address = 0x088CD028;
        private UInt32 _P1_PlayerStruct_Address = 0x088CC240;
        private UInt32 _P2_PlayerStruct_Address = 0x088CC2BC;
        private UInt32 _Recoil_Injection_Address = 0x0811FCF2;
        private UInt32 _Recoil_Injection_ReturnAddress = 0x0811FCFC;
        private UInt32 _P1_CustomRecoil_CaveAddress = 0;
        private UInt32 _P2_CustomRecoil_CaveAddress = 0;

        private int _P1_LastLife = 0;
        private int _P2_LastLife = 0;
        private int _P1_Life = 0;
        private int _P2_Life = 0;
        private int _P1_Ammo = 0;
        private int _P2_Ammo = 0;
        private int _P1_Last_Ammo = 0;
        private int _P2_Last_Ammo = 0;
        private int _P1_Last_Weapon = 0;
        private int _P2_Last_Weapon = 0;


        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_RtTerminatorSalvation(String RomName, double _ForcedXratio, bool DisableInputHack, bool Verbose)
            : base(RomName, "BudgieLoader", _ForcedXratio, DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Terminator Salvation - 01.25 USA", "b1e68e0f4dc1db9ec04a1c0e83c9913e");

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
                            byte[] buffer = ReadBytes(_RomLoaded_check_Instruction_v125, 5);
                            if (buffer[0] == 0x83 && buffer[1] == 0x2C && buffer[2] == 0xB5 && buffer[3] == 0x68 && buffer[4] == 0xBF)
                            {
                                Logger.WriteLog("Terminator Salvation - 01.25 USA binary detected");
                                _TargetProcess_Md5Hash = _KnownMd5Prints["Terminator Salvation - 01.25 USA"];
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));

                                SetHack_Output();
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
        private void SetHack_Output()
        {
            //Create Databak to store our value
            CreateDataBank();

            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[_P1_CustomRecoil_CaveAddress]
            byte[] b = BitConverter.GetBytes(_P1_CustomRecoil_CaveAddress);
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(b);
            //mov [eax+esi*4], 1
            CaveMemory.Write_StrBytes("C7 04 B0 01 00 00 00");
            //pop eax"
            CaveMemory.Write_StrBytes("58");
            //cmp dword ptr[edi+10], 09
            CaveMemory.Write_StrBytes("83 7F 10 09");
            //ja 0811FF5E
            CaveMemory.Write_ja(0x0811FF5E);
            CaveMemory.Write_jmp(_Recoil_Injection_ReturnAddress);

            Logger.WriteLog("Adding Recoil Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - _Recoil_Injection_Address - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, _Recoil_Injection_Address, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);        

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Creating a zone in memory where we will save recoil status, updated by the game.
        /// This memory will then be read by the game thanks to the following hacks.
        /// </summary>
        private void CreateDataBank()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            _P1_CustomRecoil_CaveAddress = CaveMemory.CaveAddress;
            _P2_CustomRecoil_CaveAddress = CaveMemory.CaveAddress + 0x04;

            Logger.WriteLog("Custom data will be stored at : 0x" + _P1_CustomRecoil_CaveAddress.ToString("X8"));
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpHolder, OutputId.P1_LmpHolder));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun_B, OutputId.P1_LmpGun_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGunGrenadeBtn, OutputId.P1_LmpGunGrenadeBtn));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun, OutputId.P1_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpHolder, OutputId.P2_LmpHolder));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun_B, OutputId.P2_LmpGun_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGunGrenadeBtn, OutputId.P2_LmpGunGrenadeBtn));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun, OutputId.P2_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpBillboard, OutputId.LmpBillboard));  
            //In Teknoparrot, Guns hardware is not emulated, so the game is not running original gun recoil procedures 
            /*_Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));*/
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Clip, OutputId.P2_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            //_Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Lamp_Address + 0x20) & 0x01);            
            SetOutputValue(OutputId.P1_LmpHolder, ReadByte(_Lamp_Address + 0x08) & 0x01);
            SetOutputValue(OutputId.P1_LmpGun_B, ReadByte(_Lamp_Address + 0x60) & 0x01);
            SetOutputValue(OutputId.P1_LmpGunGrenadeBtn, ReadByte(_Lamp_Address + 0x70) & 0x01);
            SetOutputValue(OutputId.P1_LmpGun, ReadByte(_Lamp_Address + 0x68) & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Lamp_Address + 0x30) & 0x01);
            SetOutputValue(OutputId.P2_LmpHolder, ReadByte(_Lamp_Address + 0x10) & 0x01);
            SetOutputValue(OutputId.P2_LmpGun_B, ReadByte(_Lamp_Address + 0x48) & 0x01);
            SetOutputValue(OutputId.P2_LmpGunGrenadeBtn, ReadByte(_Lamp_Address + 0x58) & 0x01);
            SetOutputValue(OutputId.P2_LmpGun, ReadByte(_Lamp_Address + 0x50) & 0x01);
            SetOutputValue(OutputId.LmpBillboard, ReadByte(_Lamp_Address) & 0x01);

            //Custom Outputs
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            //Check GameState : 
            //2 = Intro
            //4 = Title
            //3 = Chapter Select
            //2 = Play
            int GameState = BitConverter.ToInt32(ReadBytes(_GameState_Address, 4), 0);
            if (GameState == 1)
            {
                //Check Player Status:
                //0 = Not Playing
                //3 = Playing
                int P1_Status = BitConverter.ToInt32(ReadBytes(_P1_PlayerStruct_Address, 4), 0);
                if (P1_Status == 3)
                {
                    _P1_Ammo = BitConverter.ToInt32(ReadBytes(_P1_Ammo_Address, 4), 0);
                    if (_P1_Ammo < 0)
                        _P1_Ammo = 0;

                    _P1_Life = BitConverter.ToInt32(ReadBytes(_P1_PlayerStruct_Address + 0x58, 4), 0);
                    if (_P1_Life < 0)
                        _P1_Life = 0;

                    //To get recoil, we can't use original mechanism as Teknoparrot is not emulating the gun board
                    //and the game is not doing the outputs
                    //Getting custom recoil by checking ammunition has one drawback this infinite ammo weapons
                    //Se we"ll check what kind of weapon is used and calculate accordingly
                    int P1_Weapon = BitConverter.ToInt32(ReadBytes(_P1_WeaponNumber_Address, 4), 0);

                    //For infinite ammo weapon, using memory hack to get data                   
                    if (P1_Weapon == 5 || P1_Weapon == 8 || P1_Weapon == 9)
                    {
                        if (ReadByte(_P1_CustomRecoil_CaveAddress) == 1)
                            SetOutputValue(OutputId.P1_CtmRecoil, 1); 
                    }
                    //For other weapon, just check difference between ammo, and make sure that this smaller value is not due to a gun change
                    //0 = Shotgun
                    //1 = Regular gun
                    //2 = Gaitlin
                    //3-4 Grenade ?
                    else
                    {
                        if (_P1_Last_Weapon == P1_Weapon)
                        {
                            if (_P1_Ammo < _P1_Last_Ammo)
                                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                        }
                    }
                    //Clearing memory hack flag, even if not used
                    WriteByte(_P1_CustomRecoil_CaveAddress, 0);

                    _P1_Last_Weapon = P1_Weapon;

                    //[Clip] custom Output   
                    if (_P1_Ammo > 0)
                        P1_Clip = 1;

                    //[Damaged] custom Output                
                    if (_P1_Life < _P1_LastLife)
                        SetOutputValue(OutputId.P1_Damaged, 1);                   
                }

                int P2_Status = BitConverter.ToInt32(ReadBytes(_P2_PlayerStruct_Address, 4), 0);
                if (P2_Status == 3)
                {
                    _P2_Ammo = BitConverter.ToInt32(ReadBytes(_P2_Ammo_Address, 4), 0);
                    if (_P2_Ammo < 0)
                        _P2_Ammo = 0;

                    _P2_Life = BitConverter.ToInt32(ReadBytes(_P2_PlayerStruct_Address + 0x58, 4), 0);
                    if (_P2_Life < 0)
                        _P2_Life = 0;

                    //To get recoil, we can't use original mechanism as Teknoparrot is not emulating the gun board
                    //and the game is not doing the outputs
                    //Getting custom recoil by checking ammunition has one drawback this infinite ammo weapons
                    //Se we"ll check what kind of weapon is used and calculate accordingly
                    int P2_Weapon = BitConverter.ToInt32(ReadBytes(_P2_WeaponNumber_Address, 4), 0);

                    //For infinite ammo weapon, using memory hack to get data                   
                    if (P2_Weapon == 5 || P2_Weapon == 8 || P2_Weapon == 9)
                    {
                        if (ReadByte(_P2_CustomRecoil_CaveAddress) == 1)
                            SetOutputValue(OutputId.P2_CtmRecoil, 1);
                    }
                    //For other weapon, just check difference between ammo
                    //0 = Shotgun
                    //1 = Regular gun
                    //2 = Gaitlin
                    //3-4 Grenade ?
                    else
                    {
                        if (_P2_Last_Weapon == P2_Weapon)
                        {
                            if (_P2_Ammo < _P2_Last_Ammo)
                                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                        }
                    }
                    //Clearing memory hack flag, even if not used
                    WriteByte(_P2_CustomRecoil_CaveAddress, 0);

                    _P2_Last_Weapon = P2_Weapon;

                    //[Clip]Custom Output
                    if (_P2_Ammo > 0)
                        P2_Clip = 1;

                    //[Damaged] custom Output                
                    if (_P2_Life < _P2_LastLife)
                        SetOutputValue(OutputId.P2_Damaged, 1);
                }
            }            

            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;
            _P1_Last_Ammo = _P1_Ammo;
            _P2_Last_Ammo = _P2_Ammo;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            //Recoil : reading updated value from the game            
            if (ReadByte(_P2_CustomRecoil_CaveAddress) == 1)
            {
                if (_P2_Ammo > 0)
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte(_P2_CustomRecoil_CaveAddress, 0);
            }
            //SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
    

    }
}
