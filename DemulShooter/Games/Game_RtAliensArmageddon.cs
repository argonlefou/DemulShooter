using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_RtAliensArmageddon : Game
    {
        //Rom loaded + Rom version check
        private UInt32 _RomLoaded_check_Instruction_v390 = 0x08088211;

        //Outputs Address
        private UInt32 _P1_Struct_Address = 0x08DAB920;
        private UInt32 _P2_Struct_Address = 0x08DABAF0;
        private UInt32 _Lamp_Address = 0x8ECEC5C;    

        private int _P1_LastLife = 0;
        private int _P2_LastLife = 0;
        private int _P1_Life = 0;
        private int _P2_Life = 0;
        private int _P1_Ammo = 0;
        private int _P2_Ammo = 0;
        private int _P1_Last_Ammo = 0;
        private int _P2_Last_Ammo = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_RtAliensArmageddon(String RomName, double _ForcedXratio, bool DisableInputHack, bool Verbose)
            : base(RomName, "BudgieLoader", _ForcedXratio, DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Aliens Armageddon - 03.90 USA", "fe95d8a34331b95d14f788220e6b8fed");

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
                            byte[] buffer = ReadBytes(_RomLoaded_check_Instruction_v390, 3);
                            if (buffer[0] == 0x83 && buffer[1] == 0xEA && buffer[2] == 0x01)
                            {
                                Logger.WriteLog("Aliens Arageddon - 03.90 USA binary detected");
                                _TargetProcess_Md5Hash = _KnownMd5Prints["Aliens Armageddon - 03.90 USA"];
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpHolder, OutputId.P1_LmpHolder));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun, OutputId.P1_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGunGrenadeBtn, OutputId.P1_LmpGunGrenadeBtn));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGunMolding, OutputId.P1_LmpGunMolding));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpHolder, OutputId.P2_LmpHolder));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun, OutputId.P2_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGunGrenadeBtn, OutputId.P2_LmpGunGrenadeBtn));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGunMolding, OutputId.P2_LmpGunMolding));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpSpeaker, OutputId.LmpSpeaker));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpMarqueeBacklight, OutputId.LmpMarqueeBacklight));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpMarqueeUplight, OutputId.LmpMarqueeUplight));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpUpperCtrlPanel, OutputId.LmpUpperCtrlPanel));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpLowerCtrlPanel, OutputId.LmpLowerCtrlPanel));
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
            SetOutputValue(OutputId.P1_LmpStart, BitConverter.ToInt32(ReadBytes(_Lamp_Address + 0x24, 4), 0));
            SetOutputValue(OutputId.P1_LmpHolder, BitConverter.ToInt32(ReadBytes(_Lamp_Address + 0x3C, 4), 0));
            SetOutputValue(OutputId.P1_LmpGun, ReadByte(_Lamp_Address));
            SetOutputValue(OutputId.P1_LmpGunGrenadeBtn, ReadByte(_Lamp_Address + 0x10));
            SetOutputValue(OutputId.P1_LmpGunMolding, BitConverter.ToInt32(ReadBytes(_Lamp_Address + 0x48, 4), 0));

            SetOutputValue(OutputId.P2_LmpStart, BitConverter.ToInt32(ReadBytes(_Lamp_Address + 0x28, 4), 0));
            SetOutputValue(OutputId.P2_LmpHolder, BitConverter.ToInt32(ReadBytes(_Lamp_Address + 0x40, 4), 0));
            SetOutputValue(OutputId.P2_LmpGun, ReadByte(_Lamp_Address + 0x0C));
            SetOutputValue(OutputId.P2_LmpGunGrenadeBtn, ReadByte(_Lamp_Address + 0x14));
            SetOutputValue(OutputId.P2_LmpGunMolding, BitConverter.ToInt32(ReadBytes(_Lamp_Address + 0x4C, 4), 0));

            SetOutputValue(OutputId.LmpSpeaker, BitConverter.ToInt32(ReadBytes(_Lamp_Address + 0x50, 4), 0));
            SetOutputValue(OutputId.LmpBillboard, BitConverter.ToInt32(ReadBytes(_Lamp_Address + 0x38, 4), 0));
            SetOutputValue(OutputId.LmpMarqueeBacklight, BitConverter.ToInt32(ReadBytes(_Lamp_Address + 0x34, 4), 0));
            SetOutputValue(OutputId.LmpMarqueeUplight, BitConverter.ToInt32(ReadBytes(_Lamp_Address + 0x44, 4), 0));
            SetOutputValue(OutputId.LmpUpperCtrlPanel, BitConverter.ToInt32(ReadBytes(_Lamp_Address + 0x2c, 4), 0));
            SetOutputValue(OutputId.LmpLowerCtrlPanel, BitConverter.ToInt32(ReadBytes(_Lamp_Address + 0x30, 4), 0));

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
                _P1_Ammo = BitConverter.ToInt32(ReadBytes(_P1_Struct_Address + 0x68, 4), 0);
                if (_P1_Ammo < 0)
                    _P1_Ammo = 0;

                //[Clip] custom Output   
                if (_P1_Ammo > 0)
                    P1_Clip = 1;

                //Recoil Custom Output
                if (_P1_Ammo < _P1_Last_Ammo)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                _P1_Life = BitConverter.ToInt32(ReadBytes(_P1_Struct_Address + 0x10, 4), 0);
                if (_P1_Life < 0)
                    _P1_Life = 0;

                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }

            //Check if the Player is currently playing
            if (ReadByte(_P2_Struct_Address + 0x01) == 1)
            {
                _P2_Ammo = BitConverter.ToInt32(ReadBytes(_P2_Struct_Address + 0x68, 4), 0);
                if (_P2_Ammo < 0)
                    _P2_Ammo = 0;

                //[Clip] custom Output   
                if (_P2_Ammo > 0)
                    P2_Clip = 1;

                //Recoil Custom Output
                if (_P2_Ammo < _P2_Last_Ammo)
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                _P2_Life = BitConverter.ToInt32(ReadBytes(_P2_Struct_Address + 0x10, 4), 0);
                if (_P2_Life < 0)
                    _P2_Life = 0;

                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);
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
            //SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion


    }
}
