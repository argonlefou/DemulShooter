using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;

namespace DemulShooter
{
    internal class Game_ArcadepcRobinHood : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\sega\rhood";

        //MEMORY ADDRESSES
        private UInt32 _Credits_Offset = 0x009B32FC;
        private UInt32 _ShotArrowCount_Offset = 0x009C616C;
        private UInt32 _SceneIndex_Offset = 0x009C6138;
        private UInt32 _Outputs_Struct_Offset = 0x009E6DE8;
        private InjectionStruct _Outputs_InjectionStruct = new InjectionStruct(0x0000137B, 5);
        private enum OutputType : int
        {
            StartLamp = 0x94,
            TicketOut = 0x96,
            Bonus_LowByte = 0x97,
            Bonus_HighByte = 0x98
        }

        //This one is created by the launcher and will be read
        private UInt32 _OutputsRecopy_CaveAddress = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_ArcadepcRobinHood(String RomName)
            : base(RomName, "game")
        {
            //JConfig and Teknoparrot are sharing the same offsets, non need for memory files for now... 
           _KnownMd5Prints.Add("Robin Hood v2.06-A", "6d5b7bcf5211271559ab63403d5d11f2");
           _KnownMd5Prints.Add("Robin Hood v2.04-E", "1bee8a829b18f5e33b0ba22ba467dcc9");

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
                            ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                            Apply_MemoryHacks();
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

        #region Memory Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _OutputsRecopy_CaveAddress = _OutputsDatabank_Address;

            SetHack_RecopyOutputs();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        
        /// Outputs are stored in a roll buffer:
        /// [+0] 4 Bytes for next index
        /// [+0x0C] Output Indexes
        /// [+0x8C] Corresponding values
        ///
        /// Index:
        /// 0x94: Start Light (0=OFF, 1=ON, 0x2F=FLASH)
        /// 0x96; Tickets to send
        /// 0x97: Bonus value, low byte
        /// 0x98: Bonus value, High byte         
        private void SetHack_RecopyOutputs()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push ebx
            CaveMemory.Write_StrBytes("53");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");
            //Loop:
            //cmp [game.exe+Outputs_Struct_Offset],eax
            CaveMemory.Write_StrBytes("39 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Struct_Offset));
            //jng Exit
            CaveMemory.Write_StrBytes("0F 8E 18 00 00 00");
            //movzx ebx,byte ptr [eax+game.exe+Outputs_ID_Table]
            CaveMemory.Write_StrBytes("0F B6 98");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Struct_Offset + 0x0C));
            //add ebx,_Outputs_CaveAddress
            CaveMemory.Write_StrBytes("81 C3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_OutputsRecopy_CaveAddress));
            //mov cl,[eax+game.exe+eax+game.exe+Outputs_Values_Table]
            CaveMemory.Write_StrBytes("8A 88");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Struct_Offset + 0x8C));
            //mov [ebx],cl
            CaveMemory.Write_StrBytes("88 0B");
            //inc eax
            CaveMemory.Write_StrBytes("40");
            //jmp Loop
            CaveMemory.Write_StrBytes("EB DC");
            //mov [game.exe+Outputs_Struct_Offset],00000000
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Struct_Offset));
            CaveMemory.Write_StrBytes("00 00 00 00");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");

            //mov eax, 50
            CaveMemory.Write_StrBytes("B8 50 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Outputs_InjectionStruct, "MouseAxis");
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
            _Outputs.Add(new GameOutput(OutputDesciption.BonusDisplay, OutputId.BonusDisplay));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            byte StartLampState = ReadByte(_OutputsRecopy_CaveAddress + (uint)OutputType.StartLamp);
            switch (StartLampState)
            {
                case 0x00: SetOutputValue(OutputId.P1_CtmLmpStart, 0); break;
                case 0x01: SetOutputValue(OutputId.P1_CtmLmpStart, 1); break;
                case 0x2F: SetOutputValue(OutputId.P1_CtmLmpStart, -1); break;
                default: break;
            }

            //Game is filtering thanks to this value: 0 = Player increase arrow shot, 2/3/4 is attract mode increasing arrow shot
            byte SceneIndex = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _SceneIndex_Offset);
            _P1_Ammo = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _ShotArrowCount_Offset, 4), 0);
            if (SceneIndex == 0)
            {
                if (_P1_Ammo > _P1_LastAmmo)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);
            }
            _P1_LastAmmo = _P1_Ammo;

            int Credits = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset, 4), 0);
            int BonusValueDisplay = BitConverter.ToInt32(ReadBytes(_OutputsRecopy_CaveAddress + (uint)OutputType.Bonus_LowByte, 4), 0);

            SetOutputValue(OutputId.BonusDisplay, BonusValueDisplay);
            SetOutputValue(OutputId.Credits, Credits);
        }

        #endregion
    }
}
