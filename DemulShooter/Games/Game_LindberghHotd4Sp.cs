using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DemulShooter
{
    class Game_LindberghHotd4Sp : Game
    {

        //Outputs Address
        private UInt32 _Outputs_Address = 0x084C35CB;
        private UInt32 _Outputs_2_PtrAddress = 0x0A69F8BC;
        private UInt32 _GameInfos_Address = 0x0A69F92C;
        private UInt32 _Credits_Address = 0x0A6AEFA4;     //also 0x0A6C2E40   

        private int _LastLife = 0;
        private int _Life = 0;
        private int _P1_LastAmmo = 0;
        private int _P2_LastAmmo = 0;
        private int _P1_Ammo = 0;
        private int _P2_Ammo = 0;

        //Rom loaded + Rom version check
        private UInt32 _RomLoaded_check_Instruction = 0x080505A2;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_LindberghHotd4Sp(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "BudgieLoader", DisableInputHack, Verbose)
        {
            _tProcess.Start();
            Logger.WriteLog("Waiting for Lindbergh " + _RomName + " game to hook.....");
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
                            byte[] buffer = ReadBytes(_RomLoaded_check_Instruction, 5);
                            if (buffer[0] == 0x0D && buffer[1] == 0x80 && buffer[2] == 0x00 && buffer[3] == 0x00 && buffer[4] == 0x00)
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));                                
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

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : Is activated permanently while trigger is pressed
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_AirFront, OutputId.P1_AirFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_AirRear, OutputId.P1_AirRear));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_AirFront, OutputId.P2_AirFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_AirRear, OutputId.P2_AirRear));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpStop, OutputId.LmpStop));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpAction, OutputId.LmpAction));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpReset, OutputId.LmpReset));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpError, OutputId.LmpError));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpSafety, OutputId.LmpSafety));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpFloor, OutputId.LmpFloor));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpSpot, OutputId.LmpSpot));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Clip, OutputId.P2_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            int P1_Motor_Status = ReadByte(_Outputs_Address) >> 4 & 0x01;
            int P2_Motor_Status = ReadByte(_Outputs_Address) >> 5 & 0x01;

            //SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P1_GunMotor, P1_Motor_Status);
            SetOutputValue(OutputId.P2_GunMotor, P2_Motor_Status);
            SetOutputValue(OutputId.P1_AirFront, ReadByte(_Outputs_Address) & 0x01);
            SetOutputValue(OutputId.P2_AirFront, ReadByte(_Outputs_Address) >> 1 & 0x01);
            SetOutputValue(OutputId.P1_AirRear, ReadByte(_Outputs_Address) >> 2 & 0x01);
            SetOutputValue(OutputId.P2_AirRear, ReadByte(_Outputs_Address) >> 3 & 0x01);
            SetOutputValue(OutputId.LmpStop, ReadByte(_Outputs_Address + 2) >> 3 & 0x01);
            SetOutputValue(OutputId.LmpReset, ReadByte(_Outputs_Address + 2) & 0x01);
            SetOutputValue(OutputId.LmpError, ReadByte(_Outputs_Address + 2) >> 1 & 0x01);
            SetOutputValue(OutputId.LmpSafety, ReadByte(_Outputs_Address + 2) >> 2 & 0x01);
            SetOutputValue(OutputId.LmpFloor, ReadByte(_Outputs_Address + 2) >> 4 & 0x01);
            SetOutputValue(OutputId.LmpSpot, ReadByte(_Outputs_Address + 2) >> 5 & 0x01);
            //Billboard : 0x80=RED, 0x81=GREEN, 0x82=BLUE
            int BillBoardOn = ReadByte(_Outputs_Address + 1) >> 7 & 0x01;
            if (BillBoardOn != 0)
                SetOutputValue(OutputId.LmpBillboard, ReadByte(_Outputs_Address + 1));  
            else
                SetOutputValue(OutputId.LmpBillboard, 0);

            //Start and Action Buttons are on another memory location
            Byte bOutputs2 = ReadByte(ReadPtr(_Outputs_2_PtrAddress));
            SetOutputValue(OutputId.LmpAction, bOutputs2 >> 4 & 0x01);
            SetOutputValue(OutputId.P1_LmpStart, bOutputs2 >> 7 & 0x01);

            //Custom Outputs
            _Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;
            if (ReadPtr(_GameInfos_Address) != 0)
            {
                _Life = ReadByte(ReadPtr(_GameInfos_Address) + 0x40);
                //[Damaged] custom Output                
                if (_Life < _LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                _P1_Ammo = ReadByte(ReadPtrChain(_GameInfos_Address, new UInt32[]{ 0x34 }) + 0x2D4);
                //[Clip] custom Output   
                if (_P1_Ammo > 0)
                    P1_Clip = 1;
                //No attract mode so we can just reuse the original recoil flag
                SetOutputValue(OutputId.P1_CtmRecoil, P1_Motor_Status);

                _P2_Ammo = ReadByte(ReadPtrChain(_GameInfos_Address, new UInt32[] { 0x38 }) + 0x2D4);
                //[Clip] custom Output   
                if (_P2_Ammo > 0)
                    P2_Clip = 1;
                //No attract mode so we can just reuse the original recoil flag
                SetOutputValue(OutputId.P2_CtmRecoil, P2_Motor_Status);
            }

            _LastLife = _Life;
            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _Life);
            SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
    }
}
