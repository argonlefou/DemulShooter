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
    class Game_IceGhostBusters : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\ice\gbusters";

        //Rom loaded + Rom version check
        private UInt32 _RomLoaded_check_Instruction_v115 = 0x08197EF5;
        private UInt32 _RomLoaded_check_Instruction_v117 = 0x08198B4C;

        private UInt32 _CrosshairHack_Address = 0x0806673D;
        private UInt32 _Lamp_LeftGunTip_Address = 0x08F16668;
        private UInt32 _Lamp_LeftGunBack_Address = 0x08F16680;
        private UInt32 _Lamp_RightGunTip_Address = 0x08F16670;
        private UInt32 _Lamp_RightGunBack_Address = 0x08F1668C;
        private UInt32 _AgitatorMotorState_Address = 0x08F16698;
        private UInt32 _AgitatorMotorDirection_Address = 0x08F16694;
        private UInt32 _P1_BallShooterStatus_Address = 0x08F16650;
        private UInt32 _P2_BallShooterStatus_Address = 0x08F16654;
        private InjectionStruct _BallShooter_InjectionStruct = new InjectionStruct(0x08190191, 9);

        private UInt32 _P1_RecoilStatus_CaveAddress = 0;
        private UInt32 _P2_RecoilStatus_CaveAddress = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_IceGhostBusters(String RomName, bool HideCrosshair, bool DisableInputHack, bool Verbose)
            : base(RomName, "BudgieLoader", DisableInputHack, Verbose)
        {
            _HideCrosshair = HideCrosshair;

            //Only for documentation, version check is done by reading code position in memory, as we don't have access to the ELF path
            _KnownMd5Prints.Add("Ghostbusters - v1.15", "8de0a59ff3c10420959038d6769b646c");
            _KnownMd5Prints.Add("Ghostbusters - v1.17", "ce10cf8f57b0fffd7fc5437622033b3a");            

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
                            //And this instruction is also helping us detecting whether the game file is v1.15 or v1.17 binary, to call the corresponding hack
                            byte[] buffer = ReadBytes(_RomLoaded_check_Instruction_v117, 5);
                            if (buffer[0] == 0x89 && buffer[1] == 0x04 && buffer[2] == 0x95 && buffer[3] == 0xC4 && buffer[4] == 0x65)
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Ghostbusters - v1.17 binary detected");
                                _TargetProcess_Md5Hash = _KnownMd5Prints["Ghostbusters - v1.17"];
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                                Apply_MemoryHacks();
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
                            }
                            else
                            {
                                buffer = ReadBytes(_RomLoaded_check_Instruction_v115, 5);
                                if (buffer[0] == 0x89 && buffer[1] == 0x04 && buffer[2] == 0x95 && buffer[3] == 0xC4 && buffer[4] == 0x65)
                                {
                                    _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                    Logger.WriteLog("Ghostbusters - v1.15 binary detected");
                                    _TargetProcess_Md5Hash = _KnownMd5Prints["Ghostbusters - v1.15"];
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                    ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
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

            SetHack_Recoil();
            
            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Intercept call to change Ball Shooting event value
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov eax, [ebp+08]
            CaveMemory.Write_StrBytes("8B 45 08");
            //mov edx,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 55 0C");
            //cmp eax,00000004
            CaveMemory.Write_StrBytes("3D 04 00 00 00");
            //je P1
            CaveMemory.Write_StrBytes("74 09");
            //cmp edx,00000005
            CaveMemory.Write_StrBytes("3D 05 00 00 00");
            //je P2
            CaveMemory.Write_StrBytes("74 0D");
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 14");
            //P1:
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_RecoilStatus_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_RecoilStatus_CaveAddress));
            //mov [eax], edx
            CaveMemory.Write_StrBytes("09 10");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 09");
            //P2:
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P2_RecoilStatus_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_RecoilStatus_CaveAddress));
            //mov [eax], edx
            CaveMemory.Write_StrBytes("09 10");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //Exit:
            //add eax, 24
            CaveMemory.Write_StrBytes("83 C0 24");

            CaveMemory.InjectToAddress(_BallShooter_InjectionStruct, "Recoil");
        }

        protected override void Apply_NoCrosshairMemoryHack()
        {
            if (_HideCrosshair)
            {
                //Replacing crosshair X value in ECX by -300.0f
                WriteBytes(_CrosshairHack_Address, new byte[] { 0xB9, 0x00, 0x00, 0x96, 0xC3, 0x90, 0x90 });
            }
            else
            {
                //Putting back original code for crosshairs
                //mov ecx,[ecx*8+08AD232C]
                WriteBytes(_CrosshairHack_Address, new byte[] { 0x8B, 0x0C, 0xCD, 0x2C, 0x23, 0xAD, 0x08 });
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGunTip, OutputId.P1_LmpGunTip));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGunBack, OutputId.P1_LmpGunBack));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGunTip, OutputId.P2_LmpGunTip));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGunBack, OutputId.P2_LmpGunBack));
            _Outputs.Add(new GameOutput(OutputDesciption.BallAgitator_State, OutputId.BallAgitator_State));
            _Outputs.Add(new GameOutput(OutputDesciption.BallAgitator_Direction, OutputId.BallAgitator_Direction));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_BallShooter, OutputId.P1_BallShooter));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_BallShooter, OutputId.P2_BallShooter));

            //Custom Outputs
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Lamp Values
            SetOutputValue(OutputId.P1_LmpGunTip, GetLampValueAsInt(_Lamp_LeftGunTip_Address));
            SetOutputValue(OutputId.P1_LmpGunBack, GetLampValueAsInt(_Lamp_LeftGunBack_Address));
            SetOutputValue(OutputId.P2_LmpGunTip, GetLampValueAsInt(_Lamp_RightGunTip_Address));
            SetOutputValue(OutputId.P2_LmpGunBack, GetLampValueAsInt(_Lamp_RightGunBack_Address));

            //Ball Agitator motor : one outputs gets the state (spinning or not) and the other one get the direction (0 = Right, 1 = Left)
            SetOutputValue(OutputId.BallAgitator_State, GetMotorValueAsInt(_AgitatorMotorState_Address));
            SetOutputValue(OutputId.BallAgitator_Direction, GetMotorValueAsInt(_AgitatorMotorDirection_Address));

            //BallShooter status
            SetOutputValue(OutputId.P1_BallShooter, ReadByte(_P1_BallShooterStatus_Address));
            SetOutputValue(OutputId.P2_BallShooter, ReadByte(_P2_BallShooterStatus_Address));


            if (ReadByte(_P1_RecoilStatus_CaveAddress) > 0)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                WriteByte(_P1_RecoilStatus_CaveAddress, 0);
            }

            if (ReadByte(_P2_RecoilStatus_CaveAddress) > 0)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte(_P2_RecoilStatus_CaveAddress, 0);
            }
        }

        //Lamp values are between 0 and 0x7FF
        private int GetLampValueAsInt(UInt32 LmpAddress)
        {
            Int32 iLmpValue = BitConverter.ToInt32(ReadBytes(LmpAddress, 4), 0);
            return (int)((float)iLmpValue / 2047.0f * 100.0f);
        }

        //Motor values are between 0 and 0xFFF
        private int GetMotorValueAsInt(UInt32 LmpAddress)
        {
            Int32 iMtrValue = BitConverter.ToInt32(ReadBytes(LmpAddress, 4), 0);
            return (int)((float)iMtrValue / 4095.0f * 100.0f);
        }

        #endregion
    }
}
