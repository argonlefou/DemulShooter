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
    class Game_RtWalkingDead : Game
    {
        //Rom loaded + Rom version check
        private UInt32 _RomLoaded_check_Instruction_v105 = 0x0819DE0B;  //Check ForceFeedback setting

        //Outputs Address
        private UInt32 _P1_Struct_Address = 0x090100A0;
        private UInt32 _P2_Struct_Address = 0x09010274;
        /*private UInt32 _P1_Life_Address = 0x090100B0;
        private UInt32 _P2_Life_Address = 0x09010284;
        private UInt32 _P1_Ammo_Address = 0x0901010C;
        private UInt32 _P2_Ammo_Address = 0x090102E0;*/
        private UInt32 _Lamp_Address = 0x08AC2188;

        //Custom Values
        private UInt32 _P1_RecoilStatus_CaveAddress = 0;
        private UInt32 _P2_RecoilStatus_CaveAddress = 0;
        private UInt32 _P1_DamageStatus_CaveAddress = 0;
        private UInt32 _P2_DamageStatus_CaveAddress = 0;
        private UInt32 _NoCrosshair_FldValue_CaveAddress = 0;

        /*** MEMORY ADDRESSES **/
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x0808B1D9, 7);
        private InjectionStruct _Damage_InjectionStruct = new InjectionStruct(0x080762D6, 8);
        private UInt32 _NoCrosshair_Address = 0x0806DFFC;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_RtWalkingDead(String RomName, bool HideCrosshair, bool DisableInputHack, bool Verbose)
            : base(RomName, "BudgieLoader", DisableInputHack, Verbose)
        {
            _HideCrosshair = HideCrosshair;

            _KnownMd5Prints.Add("Walking Dead - 01.05 - Teknoparrot Patched", "5158b185b977b38749845be958caddb6");
            _KnownMd5Prints.Add("Walking Dead - 01.05 - Original dump", "6951619cd907173862c5eb337e263af5");

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
                            byte[] buffer = ReadBytes(_RomLoaded_check_Instruction_v105, 3);
                            if (buffer[0] == 0x8B && buffer[1] == 0x40 && buffer[2] == 0x48)
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Walking Dead - 01.05 binary detected");
                                //Check would be needed on the game MD5 to find if it is the original or patched version.....
                                //_TargetProcess_Md5Hash = _KnownMd5Prints["Walking Dead - 01.05 - Teknoparrot Patched"];
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

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_RecoilStatus_CaveAddress = _OutputsDatabank_Address;
            _P2_RecoilStatus_CaveAddress = _OutputsDatabank_Address + 4;
            _P1_DamageStatus_CaveAddress = _OutputsDatabank_Address + 0x10;
            _P2_DamageStatus_CaveAddress = _OutputsDatabank_Address + 0x14;
            _NoCrosshair_FldValue_CaveAddress = _OutputsDatabank_Address + 0x20;
            WriteBytes(_NoCrosshair_FldValue_CaveAddress, BitConverter.GetBytes((float)(-1.0f)));

            SetHack_Damage();
            SetHack_Recoil();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
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

            //push edi
            CaveMemory.Write_StrBytes("57");
            //sub edi,01
            CaveMemory.Write_StrBytes("83 EF 01");
            //shl edi,02
            CaveMemory.Write_StrBytes("C1 E7 02");
            //add edi,BudgieLoader.exe+56789
            CaveMemory.Write_StrBytes("81 C7");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_DamageStatus_CaveAddress));
            //mov [edi],00000001
            CaveMemory.Write_StrBytes("C7 07 01 00 00 00");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //mov [esp+04],00000040
            CaveMemory.Write_StrBytes("C7 44 24 04 40 00 00 00");
            //return
            CaveMemory.Write_jmp(_Damage_InjectionStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding Damage Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - _Damage_InjectionStruct.InjectionOffset - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            for (int i = 0; i < _Damage_InjectionStruct.NeededNops; i++)
            {
                Buffer.Add(0x90);
            }
            Win32API.WriteProcessMemory(ProcessHandle, _Damage_InjectionStruct.InjectionOffset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        /// <summary>
        /// Intercept call for checking ForceFeedback enabled setting on shoot
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();            
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //shl ebx,02
            CaveMemory.Write_StrBytes("C1 E3 02");
            //add ebx, _P1_RecoilStatus_CaveAddress
            CaveMemory.Write_StrBytes("81 C3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_RecoilStatus_CaveAddress));
            //mov [ebx],00000001
            CaveMemory.Write_StrBytes("C7 03 01 00 00 00");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");

            //mov [esp],088C735C
            CaveMemory.Write_StrBytes("C7 04 24 5C 73 8C 08");

            //return
            CaveMemory.Write_jmp(_Recoil_InjectionStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding Recoil Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - _Recoil_InjectionStruct.InjectionOffset - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            for (int i = 0; i < _Recoil_InjectionStruct.NeededNops; i++)
            {
                Buffer.Add(0x90);
            }
            Win32API.WriteProcessMemory(ProcessHandle, _Recoil_InjectionStruct.InjectionOffset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        protected override void Apply_NoCrosshairMemoryHack()
        {
            if (_HideCrosshair)
            {
                //Replacing the fld instruction (loading Cursor axis as float) by fld (-1)
                WriteBytes(_NoCrosshair_Address, new byte[] { 0xD9, 0x05 });
                WriteBytes(_NoCrosshair_Address + 2, BitConverter.GetBytes(_NoCrosshair_FldValue_CaveAddress));
                WriteByte(_NoCrosshair_Address + 6, 0x90);
            }
            else
            {
                //Setting it back to default if it was changed by a previous instance of DemulSHooter
                WriteBytes(_NoCrosshair_Address, new byte[] { 0xD9, 0x04, 0xCD, 0x8C, 0xEF, 0xAB, 0x08 });
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun, OutputId.P1_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun, OutputId.P2_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpHolder, OutputId.P1_LmpHolder));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpHolder, OutputId.P2_LmpHolder));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpPanel, OutputId.P1_LmpPanel));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpPanel, OutputId.P2_LmpPanel));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpRoof_R, OutputId.LmpRoof_R));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpRoof_G, OutputId.LmpRoof_G));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpRoof_B, OutputId.LmpRoof_B));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpLgMarquee, OutputId.LmpLgMarquee));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpSqMarquee, OutputId.LmpSqMarquee));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpWalker_R, OutputId.LmpWalker_R));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpWalker_G, OutputId.LmpWalker_G));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpWalker_B, OutputId.LmpWalker_B));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpWalkerEyes, OutputId.LmpWalkerEyes));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpWalkerCeiling, OutputId.LmpWalkerCeiling));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpPosts, OutputId.LmpPosts));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpRear_R, OutputId.LmpRear_R));
            
            //In Teknoparrot, Guns hardware is not emulated, so the game is not running original gun recoil procedures 
            /*_Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));*/
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
            //Original Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Lamp_Address + 0x18));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Lamp_Address + 0x1C));
            SetOutputValue(OutputId.P1_LmpGun, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address, 4), 0) * 20));
            SetOutputValue(OutputId.P2_LmpGun, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0x04, 4), 0) * 200));
            SetOutputValue(OutputId.P1_LmpHolder, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0x84, 4), 0) * 200));
            SetOutputValue(OutputId.P2_LmpHolder, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0xB4, 4), 0) * 200));
            SetOutputValue(OutputId.P1_LmpPanel, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0x7C, 4), 0) * 200));
            SetOutputValue(OutputId.P2_LmpPanel, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0x80, 4), 0) * 200));
            SetOutputValue(OutputId.LmpRoof_R, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0x88, 4), 0) * 200));
            SetOutputValue(OutputId.LmpRoof_G, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0x8C, 4), 0) * 200));
            SetOutputValue(OutputId.LmpRoof_B, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0x90, 4), 0) * 200));

            //Long Marquee uses 4 outputs, so we will add them to be sure it won't be missed if the game only triggers a part of them
            byte LongMarqueeStatus = 0;
            for (uint i = 0; i < 3; i++)
            {
                if (ReadByte(_Lamp_Address + 0x94 + 4 * i + 3) != 0x00)
                    LongMarqueeStatus |= 1;
            }
            SetOutputValue(OutputId.LmpLgMarquee, LongMarqueeStatus);
            SetOutputValue(OutputId.LmpSqMarquee, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0xA4, 4), 0) * 200));
            SetOutputValue(OutputId.LmpWalker_R, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0xA8, 4), 0) * 200));
            SetOutputValue(OutputId.LmpWalker_G, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0xAC, 4), 0) * 200));
            SetOutputValue(OutputId.LmpWalker_B, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0xB0, 4), 0) * 200));
            SetOutputValue(OutputId.LmpWalkerEyes, ReadByte(_Lamp_Address + 0x2C));
            SetOutputValue(OutputId.LmpWalkerCeiling, ReadByte(_Lamp_Address + 0x28));
            SetOutputValue(OutputId.LmpPosts, ReadByte(_Lamp_Address + 0x20));
            SetOutputValue(OutputId.LmpRear_R, ReadByte(_Lamp_Address + 0x24));

            //Custom Outputs
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            //Check if the Player is currently playing
            if (ReadByte(_P1_Struct_Address + 0x01) == 1)
            {
                _P1_Ammo = BitConverter.ToInt32(ReadBytes(_P1_Struct_Address + 0x6C, 4), 0);
                if (_P1_Ammo < 0)
                    _P1_Ammo = 0;

                //[Clip] custom Output   
                if (_P1_Ammo > 0)
                    P1_Clip = 1;

                _P1_Life = BitConverter.ToInt32(ReadBytes(_P1_Struct_Address + 0x10, 4), 0);
                if (_P1_Life < 0)
                    _P1_Life = 0;
            }

            //Check if the Player is currently playing
            if (ReadByte(_P2_Struct_Address + 0x01) == 1)
            {
                _P2_Ammo = BitConverter.ToInt32(ReadBytes(_P2_Struct_Address + 0x6C, 4), 0);
                if (_P2_Ammo < 0)
                    _P2_Ammo = 0;

                //[Clip] custom Output   
                if (_P2_Ammo > 0)
                    P2_Clip = 1;

                _P2_Life = BitConverter.ToInt32(ReadBytes(_P2_Struct_Address + 0x10, 4), 0);
                if (_P2_Life < 0)
                    _P2_Life = 0;
            }

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            //Reading our own flags for damage and recoil and resetting them
            if (ReadByte(_P1_RecoilStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                WriteByte(_P1_RecoilStatus_CaveAddress, 0);
            }
            if (ReadByte(_P2_RecoilStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte(_P2_RecoilStatus_CaveAddress, 0);
            }
            if (ReadByte(_P1_DamageStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P1_Damaged, 1);
                WriteByte(_P1_DamageStatus_CaveAddress, 0);
            }
            if (ReadByte(_P2_DamageStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P2_Damaged, 1);
                WriteByte(_P2_DamageStatus_CaveAddress, 0);
            }

            //SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
        
    }
}
