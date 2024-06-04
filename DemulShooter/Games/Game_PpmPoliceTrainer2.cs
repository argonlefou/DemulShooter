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
    class Game_PpmPoliceTrainer2 : Game
    {
        //Rom loaded + Rom version check
        private UInt32 _RomLoaded_check_Instruction_v001g11 = 0x0806C18D;  //Set life when player join

        //Outputs Address
        private UInt32 _P1_Life_Address = 0x08236C80;
        private UInt32 _P1_Ammo_Address = 0x08236C88;
        private UInt32 _P2_Life_Address = 0x08236F08;
        private UInt32 _P2_Ammo_Address = 0x08236F10;
        private UInt32 _Lamps_Address = 0x82307F0;
        private UInt32 _Credits_Address = 0x08230864;
        //private UInt32 _Buttons_Address = 0x082307C8;
        private InjectionStruct _P1Recoil_InjectionStruct = new InjectionStruct(0x08056454, 6);
        private InjectionStruct _P2Recoil_InjectionStruct = new InjectionStruct(0x08056482, 6);

        //custom values
        private UInt32 _P1_RecoilStatus_CaveAddress = 0;
        private UInt32 _P2_RecoilStatus_CaveAddress = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_PpmPoliceTrainer2(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "BudgieLoader", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Police Trainer 2 - 0.0.1g11 - Original dump", "e40bd5c6a7f2c3a84281e115c25d3f20");
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
                        //Looks like that game is opening a couple of BudgieLoader processes, we need to find the good one...
                        foreach (Process p in processes)
                        {
                            _TargetProcess = p;
                            _ProcessHandle = _TargetProcess.Handle;
                            _TargetProcess_MemoryBaseAddress = _TargetProcess.MainModule.BaseAddress;

                            if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                            {
                                //To make sure BurgieLoader has loaded the rom entirely, we're looking for some random instruction to be present in memory before starting                            
                                byte[] buffer = ReadBytes(_RomLoaded_check_Instruction_v001g11, 3);
                                if (buffer[0] == 0x89 && buffer[1] == 0x03 && buffer[2] == 0x83)
                                {
                                    _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                    Logger.WriteLog("Police Trainer 2 - 0.0.1g11 binary detected");
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));

                                    SetHack_Outputs();

                                    _ProcessHooked = true;
                                    RaiseGameHookedEvent();
                                    break;
                                }
                                else
                                {
                                    Logger.WriteLog("Game not Loaded, waiting...");
                                }
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

        private void SetHack()
        {
            //Not used
        }

        private void SetHack_Outputs()
        {
            CreateDataBank_Outputs();
            SetHack_Recoil_P1();
            SetHack_Recoil_P2();            
        }

        private void CreateDataBank_Outputs()
        {
            //Creating data bank
            //Codecave :
            Codecave CaveMemoryInput = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemoryInput.Open();
            CaveMemoryInput.Alloc(0x800);
            _P1_RecoilStatus_CaveAddress = CaveMemoryInput.CaveAddress;
            _P2_RecoilStatus_CaveAddress = CaveMemoryInput.CaveAddress + 4;
            Logger.WriteLog("Custom output data will be stored at : 0x" + CaveMemoryInput.CaveAddress.ToString("X8"));
        }

        /// <summary>
        /// The game is stacking Trigger press on a counter, but both P1 & P2 are stored in the same one
        /// We can intercept the counter update to split it into 2 different ones, to detect trigger press
        /// </summary>
        private void SetHack_Recoil_P1()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //cmp dword ptr [_P1_Life_Address],00
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Life_Address));
            CaveMemory.Write_StrBytes("00");
            //js Next
            CaveMemory.Write_StrBytes("78 0A");
            //push eax
            CaveMemory.Write_StrBytes("50");            
            //mov eax,_P1_RecoilStatus_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_RecoilStatus_CaveAddress));
            //mov byte ptr[eax], 1
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //Next:            
            //mov [08230900],edx
            CaveMemory.Write_StrBytes("89 15 00 09 23 08");
            //return
            CaveMemory.Write_jmp(_P1Recoil_InjectionStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding P1 Recoil Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress -_P1Recoil_InjectionStruct.InjectionOffset - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            for (int i = 0; i < _P1Recoil_InjectionStruct.NeededNops; i++)
            {
                Buffer.Add(0x90);
            }
            Win32API.WriteProcessMemory(ProcessHandle, _P1Recoil_InjectionStruct.InjectionOffset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        /// <summary>
        /// The game is stacking Trigger press on a counter, but both P1 & P2 are stored in the same one
        /// We can intercept the counter update to split it into 2 different ones, to detect trigger press
        /// </summary>
        private void SetHack_Recoil_P2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //cmp dword ptr [_P2_Life_Address],00
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Life_Address));
            CaveMemory.Write_StrBytes("00");
            //js Next
            CaveMemory.Write_StrBytes("78 0A");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,_P2_RecoilStatus_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_RecoilStatus_CaveAddress));
            //mov byte ptr[eax], 1
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //Next:            
            //mov [08230900],edx
            CaveMemory.Write_StrBytes("89 15 00 09 23 08");
            //return
            CaveMemory.Write_jmp(_P2Recoil_InjectionStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding P2 Recoil Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - _P2Recoil_InjectionStruct.InjectionOffset - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            for (int i = 0; i < _P2Recoil_InjectionStruct.NeededNops; i++)
            {
                Buffer.Add(0x90);
            }
            Win32API.WriteProcessMemory(ProcessHandle, _P2Recoil_InjectionStruct.InjectionOffset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpFront, OutputId.P1_LmpFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpFront, OutputId.P2_LmpFront));
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
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            SetOutputValue(OutputId.P1_LmpFront, ReadByte(_Lamps_Address) >> 6 & 0x01 );
            SetOutputValue(OutputId.P2_LmpFront, ReadByte(_Lamps_Address) >> 7 & 0x01 );

            //Custom Outputs
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            //Check if the Player is currently playing
            if (BitConverter.ToInt32(ReadBytes(_P1_Life_Address, 4), 0) > 0)    //Life = -1 when player not playing
            {
                _P1_Ammo = BitConverter.ToInt32(ReadBytes(_P1_Ammo_Address, 4), 0);
                if (_P1_Ammo < 0)
                    _P1_Ammo = 0;

                //[Clip] custom Output   
                if (_P1_Ammo > 0)
                    P1_Clip = 1;

                _P1_Life = BitConverter.ToInt32(ReadBytes(_P1_Life_Address, 4), 0);
                if (_P1_Life < 0)
                    _P1_Life = 0;

                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }

            //Check if the Player is currently playing
            if (BitConverter.ToInt32(ReadBytes(_P2_Life_Address, 4), 0) > 0)    //Life = -1 when player not playing
            {
                _P2_Ammo = BitConverter.ToInt32(ReadBytes(_P2_Ammo_Address, 4), 0);
                if (_P2_Ammo < 0)
                    _P2_Ammo = 0;

                //[Clip] custom Output   
                if (_P2_Ammo > 0)
                    P2_Clip = 1;

                _P2_Life = BitConverter.ToInt32(ReadBytes(_P2_Life_Address, 4), 0);
                if (_P2_Life < 0)
                    _P2_Life = 0;

                 if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }

            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

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

            SetOutputValue(OutputId.Credits, BitConverter.ToInt32(ReadBytes(_Credits_Address, 4), 0));
        }

        #endregion
    }
}
