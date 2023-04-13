using System;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using System.Collections.Generic;
using DsCore.MameOutput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_GvrFarCry : Game
    {
        /*** MEMORY ADDRESSES **/
        private InjectionStruct _PlayerOff_InjectionStruct = new InjectionStruct(0x000CBA22, 8);
        private InjectionStruct _PlayerOn_InjectionStruct = new InjectionStruct(0x000CBDA3, 8);
        private InjectionStruct _ReadLife_InjectionStruct = new InjectionStruct(0x00125D82, 7);
        private InjectionStruct _ReadShots_InjectionStruct = new InjectionStruct(0x000ED237, 8);
        private InjectionStruct _ReadKills_InjectionStruct = new InjectionStruct(0x003AEBA4, 5);
        private InjectionStruct _ReadRumble_InjectionStruct = new InjectionStruct(0x003AEA6D, 7);
        private UInt32 _P1_Playing_CaveAddress = 0;
        private UInt32 _P2_Playing_CaveAddress = 0;
        private UInt32 _P1_Shots_CaveAddress = 0;
        private UInt32 _P2_Shots_CaveAddress = 0;
        private UInt32 _P1_Life_CaveAddress = 0;
        private UInt32 _P2_Life_CaveAddress = 0;
        private UInt32 _P1_KillsDigit1_CaveAddress = 0;
        private UInt32 _P1_KillsDigit2_CaveAddress = 0;
        private UInt32 _P2_KillsDigit1_CaveAddress = 0;
        private UInt32 _P2_KillsDigit2_CaveAddress = 0;
        private UInt32 _P1_Rumble_CaveAddress = 0;
        private UInt32 _P2_Rumble_CaveAddress = 0;
        //Unused 
        //private UInt32 _P1_Kills_Offset = 0x00649510;
        //private UInt32 _P2_Kills_Offset = 0x0064955C;

        //Outputs
        private int _P1_LastShots = 0;
        private int _P2_LastShots = 0;
        private int _P1_LastLife = 0;
        private int _P2_LastLife = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_GvrFarCry(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "FarCry_r", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Far Cry Paradise", "263325AC3F7685CBA12B280D8E927A5D");
            _KnownMd5Prints.Add("Far Cry Paradise by Mohkerz", "557d065632eaa3c8adb5764df1609976");
            _KnownMd5Prints.Add("Far Cry Paradise - TeknoParrot", "87648806d0b4b5a5384e7eedf4882d7e");
            
            _tProcess.Start();
            Logger.WriteLog("Waiting for Global VR " + _RomName + " game to hook.....");
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
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();
                            /*if (!_DisableInputHack)
                                SetHack();
                            else*/

                            Logger.WriteLog("Input Hack disabled");
                            SetHack_Outputs();
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

        private void SetHack_Outputs()
        {
            CreateDataBank();
            SetHack_ReadPlayerPlaying();
            SetHack_ReadPlayerNotPlaying();
            SetHack_ReadPlayerLife();
            SetHack_ReadPlayerShots();
            SetHack_ReadPlayerKillsDigit();
            SetHack_ReadRumble();
            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        //Creating a custom memory space to store custom values
        private void CreateDataBank()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            _P1_Playing_CaveAddress = CaveMemory.CaveAddress;
            _P2_Playing_CaveAddress = CaveMemory.CaveAddress + 0x04;
            _P1_Shots_CaveAddress = CaveMemory.CaveAddress + 0x08;
            _P2_Shots_CaveAddress = CaveMemory.CaveAddress + 0x0C;
            _P1_Life_CaveAddress = CaveMemory.CaveAddress + 0x10;
            _P2_Life_CaveAddress = CaveMemory.CaveAddress + 0x14;
            _P1_KillsDigit1_CaveAddress = CaveMemory.CaveAddress + 0x18;
            _P1_KillsDigit2_CaveAddress = CaveMemory.CaveAddress + 0x1C;
            _P2_KillsDigit1_CaveAddress = CaveMemory.CaveAddress + 0x20;
            _P2_KillsDigit2_CaveAddress = CaveMemory.CaveAddress + 0x24;
            _P1_Rumble_CaveAddress = CaveMemory.CaveAddress + 0x28;
            _P2_Rumble_CaveAddress = CaveMemory.CaveAddress + 0x2C;

            Logger.WriteLog("Custom data will be stored at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
        }

        /// <summary>
        /// There is some emory address changing from 3 to 2 when player is not playing -> playing (bit mask bit 0)
        /// Unfortunatelly no pointer was found, but this instruction sets the Bit 0 to 1 when player is not playing
        /// We will use this instruction to set our own FLAG
        /// EBP = 1 if it is for Player 2
        /// </summary>
        private void SetHack_ReadPlayerNotPlaying()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov eax,[esp+1C]
            CaveMemory.Write_StrBytes("8B 44 24 1C");
            //or byte ptr [eax+18]
            CaveMemory.Write_StrBytes("80 48 18 01");
            //push ebp
            CaveMemory.Write_StrBytes("55");
            //shl ebp, 2
            CaveMemory.Write_StrBytes("C1 E5 02");
            //add ebp, [_P1_Playing_CaveAddress]
            CaveMemory.Write_StrBytes("81 C5");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Playing_CaveAddress));
            //mov [ebp], 0
            CaveMemory.Write_StrBytes("C7 45 00 00 00 00 00");
            //pop ebp
            CaveMemory.Write_StrBytes("5D");
            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _PlayerOff_InjectionStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding player not playing status CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
            Write_Codecave(CaveMemory, _PlayerOff_InjectionStruct);
        }

        /// <summary>
        /// There is some emory address changing from 3 to 2 when player is not playing -> playing (bit mask bit 0)
        /// Unfortunatelly no pointer was found, but this instruction sets the Bit 0 to 0 when player is playing
        /// We will use this instruction to set our own FLAG
        /// EBP = 1 if it is for Player 2
        /// </summary>
        private void SetHack_ReadPlayerPlaying()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov eax,[esp+1C]
            CaveMemory.Write_StrBytes("8B 44 24 1C");
            //and byte ptr [eax+18],-02
            CaveMemory.Write_StrBytes("80 60 18 FE");
            //push ebp
            CaveMemory.Write_StrBytes("55");
            //shl ebp, 2
            CaveMemory.Write_StrBytes("C1 E5 02");
            //add ebp, [_P1_Playing_CaveAddress]
            CaveMemory.Write_StrBytes("81 C5");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Playing_CaveAddress));
            //mov [ebp], 1
            CaveMemory.Write_StrBytes("C7 45 00 01 00 00 00");
            //pop ebp
            CaveMemory.Write_StrBytes("5D");
            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _PlayerOn_InjectionStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding player playing status CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
            Write_Codecave(CaveMemory, _PlayerOn_InjectionStruct);
        }

        /// <summary>
        /// This is called to compare player life, ESI is player index
        /// We can get the life by that instruction, as a pointer has not been found to the memory location
        /// </summary>
        private void SetHack_ReadPlayerLife()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //cmp esi,01
            CaveMemory.Write_StrBytes("83 FE 01"); 
            //je Player2 
            CaveMemory.Write_StrBytes("74 08");   
            //mov [_P1_Life_CaveAddress],edx
            CaveMemory.Write_StrBytes("89 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Life_CaveAddress));
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 06");
            //mov [_P2_Life_CaveAddress],edx
            CaveMemory.Write_StrBytes("89 15");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Life_CaveAddress));
            //cmp edx,[edi+ecx*4+00000610]
            CaveMemory.Write_StrBytes("3B 94 8F 10 06 00 00");

            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _ReadLife_InjectionStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding Custom Life output CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
            Write_Codecave(CaveMemory, _ReadLife_InjectionStruct);
        }

        /// <summary>
        /// This instructions put the current mission fired bullet in CX
        /// ZF=1 if player 1
        /// ZF=0 if player 2
        /// We can use this count to create recoil
        /// </summary>
        private void SetHack_ReadPlayerShots()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //lea eax,[eax+ecx+28]
            CaveMemory.Write_StrBytes("8D 44 08 28");
            //mov cx,[eax+32]
            CaveMemory.Write_StrBytes("66 8B 48 32");
            //jne Player 2 (ZF=0)
            CaveMemory.Write_StrBytes("75 09");
            //mov [_P1_Shots_CaveAddress], cx
            CaveMemory.Write_StrBytes("66 89 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Shots_CaveAddress));
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 07");
            //Player 2
            //mov [_P2_Shots_CaveAddress], cx
            CaveMemory.Write_StrBytes("66 89 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Shots_CaveAddress));

            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _ReadShots_InjectionStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding Player Shots output CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
            Write_Codecave(CaveMemory, _ReadShots_InjectionStruct);
        }

        /// <summary>
        /// The game is sending the KILLS number as 2 digits to the Gun device
        /// We intercept that WRITE call to get the data
        /// We can get the device index (0 or 1) at ESP+1C
        /// Byte Buffered Message is in [ESP+4]: 0x03, Digit1, Digit2
        /// </summary>
        private void SetHack_ReadPlayerKillsDigit()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //cmp eax,-01
            CaveMemory.Write_StrBytes("83 F8 FF");              
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop esi
            CaveMemory.Write_StrBytes("5E");               

            //cmp [esp+1C], 01
            CaveMemory.Write_StrBytes("80 7C 24 1C 01");
            //je Player2
            CaveMemory.Write_StrBytes("74 16");
            //mov bl, [esp+5]
            CaveMemory.Write_StrBytes("8A 5C 24 05");
            //mov [_P1_KillsDigit1_CaveAddress], bl
            CaveMemory.Write_StrBytes("88 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_KillsDigit1_CaveAddress));
            //mov bl, [esp+6]
            CaveMemory.Write_StrBytes("8A 5C 24 06");
            //mov [_P1_KillsDigit2_CaveAddress], bl
            CaveMemory.Write_StrBytes("88 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_KillsDigit2_CaveAddress));
            //jmp exit
            CaveMemory.Write_StrBytes("EB 14");
            //Player2:
            //mov bl, [esp+5]
            CaveMemory.Write_StrBytes("8A 5C 24 05");
            //mov [_P2_KillsDigit1_CaveAddress], bl
            CaveMemory.Write_StrBytes("88 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_KillsDigit1_CaveAddress));
            //mov bl, [esp+6]
            CaveMemory.Write_StrBytes("8A 5C 24 06");
            //mov [_P1_KillsDigit2_CaveAddress], bl
            CaveMemory.Write_StrBytes("88 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_KillsDigit2_CaveAddress));
            //Exit:
            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _ReadKills_InjectionStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding Kill digits output CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
            Write_Codecave(CaveMemory, _ReadKills_InjectionStruct);
        }

        /// <summary>
        // <summary>
        /// The game is sending the Rumble state to the Gun device
        /// We intercept that WRITE call to get the data
        /// We can get the device index (0 or 1) at ESP+10
        /// Byte Buffered Message is in [ESP]: 0x02, 0x07 (second byte is using the 3 lower bits to mask data - unknowed type)
        /// </summary>
        /// </summary>
        private void SetHack_ReadRumble()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov eax,[edx*4+Farcry_R.exe+649500]
            CaveMemory.Write_StrBytes("8B 04 95 00 95 A4 00");
            
            //cmp [esp+10], 01
            CaveMemory.Write_StrBytes("80 7C 24 10 01");
            //je Player2
            CaveMemory.Write_StrBytes("74 0C");
            //mov [_P1_Rumble_CaveAddress],00000001
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Rumble_CaveAddress));
            CaveMemory.Write_StrBytes("01 00 00 00");
            //jmp exit
            CaveMemory.Write_StrBytes("EB 0A");
            //Player2:
            //mov [_P2_Rumble_CaveAddress],00000001
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Rumble_CaveAddress));
            CaveMemory.Write_StrBytes("01 00 00 00");
            //Exit:
            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _ReadRumble_InjectionStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding Rumble output CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
            Write_Codecave(CaveMemory, _ReadRumble_InjectionStruct);
        }

        #endregion            

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LedKills1, OutputId.P1_LedKills1));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LedKills2, OutputId.P1_LedKills2));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LedKills1, OutputId.P2_LedKills1));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LedKills2, OutputId.P2_LedKills2));            
            //Custom Outputs
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P1_CtmLmpStart, OutputId.P1_CtmLmpStart, 500));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P2_CtmLmpStart, OutputId.P2_CtmLmpStart, 500));
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
            //Original outputs :
            //Data is Written to the gun (with WriteFile API)
            //Gun recoil is written @ +3AEA90 (skipped because Handle = -1, but data is in memory)
            //Gun is rumbling on shoot or grenade
            int P1_Rumble = ReadByte(_P1_Rumble_CaveAddress);
            int P2_Rumble = ReadByte(_P2_Rumble_CaveAddress);
            SetOutputValue(OutputId.P1_GunMotor, P1_Rumble);
            SetOutputValue(OutputId.P2_GunMotor, P2_Rumble);
            

            //Guns have 2-digits LED to display kill. This is maxed to 99 and will roll back to 0 after 100th kill (and get player a medal/bonus)
            //Each digits goes from 0 to 9 during gameplay, and 0xA when not playing (IOboard should handle the values to hide digits or just display - or / with 0xA ?)
            SetOutputValue(OutputId.P1_LedKills1, ReadByte(_P1_KillsDigit1_CaveAddress));
            SetOutputValue(OutputId.P1_LedKills2, ReadByte(_P1_KillsDigit2_CaveAddress));
            SetOutputValue(OutputId.P2_LedKills1, ReadByte(_P2_KillsDigit1_CaveAddress));
            SetOutputValue(OutputId.P2_LedKills2, ReadByte(_P2_KillsDigit2_CaveAddress));

            //Custom Outputs
            int P1_Life = 0;
            int P2_Life = 0;
            int P1_CurrentShots = 0;
            int P2_CurrentShots = 0;

            //Customs Outputs
            //Player Status :
            //[0] : Inactive
            //[1] : In-Game
            int P1_Status = ReadByte(_P1_Playing_CaveAddress);
            if (P1_Status == 1)
            {
                //Force Start Lamp to Off
                SetOutputValue(OutputId.P1_CtmLmpStart, 0);

                P1_Life = ReadByte(_P1_Life_CaveAddress);

                if (P1_Life < 0)
                    P1_Life = 0;

                if (P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                /*P1_CurrentShots = BitConverter.ToInt32(ReadBytes(_P1_Shots_CaveAddress, 4), 0);                
                if (P1_CurrentShots > _P1_LastShots)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);*/
            }
            else if (P1_Status == 0)
            {
                //Enable Start Lamp Blinking
                SetOutputValue(OutputId.P1_CtmLmpStart, -1);
            }


            int P2_Status = ReadByte(_P2_Playing_CaveAddress);
            if (P2_Status == 1)
            {
                //Force Start Lamp to Off
                SetOutputValue(OutputId.P2_CtmLmpStart, 0);

                P2_Life = ReadByte(_P2_Life_CaveAddress);

                if (P2_Life < 0)
                    P2_Life = 0;

                if (P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);

                /*P2_CurrentShots = BitConverter.ToInt32(ReadBytes(_P2_Shots_CaveAddress, 4), 0);
                if (P2_CurrentShots > _P2_LastShots)
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);*/
            }
            else if (P2_Status == 0)
            {
                //Enable Start Lamp Blinking
                SetOutputValue(OutputId.P2_CtmLmpStart, -1);
            }

            //Custom outputs will be based on Genuine rumble
            if (P1_Rumble != 0)
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
            if (P2_Rumble != 0)
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
            
            SetOutputValue(OutputId.P1_Life, P1_Life);
            SetOutputValue(OutputId.P2_Life, P2_Life);

            _P1_LastLife = P1_Life;
            _P2_LastLife = P2_Life;
            _P1_LastShots = P1_CurrentShots;
            _P2_LastShots = P2_CurrentShots;
            WriteByte(_P1_Rumble_CaveAddress, 0x00);
            WriteByte(_P2_Rumble_CaveAddress, 0x00);

            

            //SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00C4D758));
        }

        #endregion

    }
}
