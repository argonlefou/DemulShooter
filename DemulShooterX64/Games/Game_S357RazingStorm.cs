using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;

namespace DemulShooterX64.Games
{
    public class Game_S357RazingStorm : Game
    {
        //MEMORY ADDRESSES       

        //Outputs
        private UInt64 _Outputs_Address = 0x300D043C8;
        private UInt64 _Credits_Address = 0x300D048F8;
        private int _P1_LastRumble = 0;
        private int _P2_LastRumble = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_S357RazingStorm(String RomName) : base(RomName, "rpcs3")
        {
            _KnownMd5Prints.Add("RPCS3 v0.0.37-2ef2310e Alpha (TeknoParrot)", "56B69E6A8D95FE0AC7BF7BB5D57321DC");
            _tProcess.Start();
            Logger.WriteLog("Waiting for RPCS3 " + _RomName + " game to hook.....");
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

                        //Looking for the game's window based on it's Title
                        _GameWindowHandle = IntPtr.Zero;
                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            // The game may start with other Windows than the main one (BepInEx console, other stuff.....) so we need to filter
                            // the displayed window according to the Title, if DemulShooter is started before the game,  to hook the correct one
                            if (FindGameWindow_Contains("RPCS3 via TeknoParrot"))
                            {
                                CheckExeMd5();
                                if (_DisableInputHack)
                                    Logger.WriteLog("Input Hack disabled");
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
                            }
                            else
                            {
                                Logger.WriteLog("Game Window not found");
                                return;
                            }

                            /*_GameCode_Address = ReadPtrChain((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _GameCode_Ptr_Offset), new UInt64[] { 0x230, 0x00, 0x00, 0x30, 0x00 });
                            if (_GameCode_Address != 0)
                            {
                                Logger.WriteLog("EBOT.BIN PPU cache memory segment address = 0x" + _GameCode_Address.ToString("X16"));
                                byte[] TestLoadedInstruction = ReadBytes((IntPtr)_GameCode_Address, 4);

                                if (TestLoadedInstruction[0] == 0x48 && TestLoadedInstruction[1] == 0x83 && TestLoadedInstruction[2] == 0xEC && TestLoadedInstruction[3] == 0x28)
                                {
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
                                    Logger.WriteLog("MainWindowHandle = 0x" + _TargetProcess.MainWindowHandle.ToString("X16"));
                                    Logger.WriteLog("MainWindowTitle" + _TargetProcess.MainWindowTitle);

                                    CheckExeMd5();
                                    if (_DisableInputHack)
                                        Logger.WriteLog("Input Hack disabled");
                                    _ProcessHooked = true;
                                    RaiseGameHookedEvent();
                                }
                                else
                                {
                                    string s = string.Empty;
                                    foreach (byte b in TestLoadedInstruction)
                                        s += b.ToString("X2") + " ";
                                    Logger.WriteLog("Read bytes at address 0x" + _GameCode_Address.ToString("X16") + " : " + s);

                                    s = string.Empty;
                                    foreach (byte b in _GameLoadedInstruction)
                                        s += b.ToString("X2") + " ";
                                    Logger.WriteLog("Expected : " + s);

                                    Logger.WriteLog("ROM not Loaded...");
                                }
                            }
                            else
                            {
                                Logger.WriteLog("ROM not Loaded...");
                            }*/
                        }
                    }
                }
                catch (Exception)
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
            //Gun motor : Is activated for every bullet fired AND when player gets
            _Outputs = new List<GameOutput>();

            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun, OutputId.P1_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun, OutputId.P2_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpFront, OutputId.P1_LmpFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpFront, OutputId.P2_LmpFront));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            byte bOutput1 = ReadByte((IntPtr)_Outputs_Address);
            byte bOutput2 = ReadByte((IntPtr)_Outputs_Address + 1);

            //Rumble is using bit-changing values on a bi-stable state (not monostable) on the higher 4bits of the byte 
            // [Bit3] [Bit2] [Bit1] [Bit0] 
            //P1 Recoil activation swap 1 from Bit3/Bit2 (i.e : 8 -> 4-> 8-> 4) 
            //P2 Recoil activation swap 1 from Bit1/Bit0 (i.e : 1 -> 2-> 1-> 2) 
            int P1_Rumble = bOutput1 & 0xC0;
            int P2_Rumble = bOutput1 & 0x30;

            if (_P1_LastRumble != 0 && P1_Rumble != _P1_LastRumble && P1_Rumble != 0)
            {
                SetOutputValue(OutputId.P1_GunMotor, 1);
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
            }
            else
                SetOutputValue(OutputId.P1_GunMotor, 0);

            if (_P2_LastRumble != 0 && P2_Rumble != _P2_LastRumble && P2_Rumble != 0)
            {
                SetOutputValue(OutputId.P2_GunMotor, 1);
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
            }
            else
                SetOutputValue(OutputId.P2_GunMotor, 0);

            _P1_LastRumble = P1_Rumble;
            _P2_LastRumble = P2_Rumble;

            SetOutputValue(OutputId.P1_LmpStart, bOutput1 >> 3 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, bOutput1 >> 2 & 0x01);
            SetOutputValue(OutputId.P1_LmpGun, bOutput2 >> 1 & 0x01);
            SetOutputValue(OutputId.P2_LmpGun, bOutput2 & 0x01);
            SetOutputValue(OutputId.P1_LmpFront, bOutput2 >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpFront, bOutput2 >> 6 & 0x01);

            SetOutputValue(OutputId.Credits, ReadByte((IntPtr)_Credits_Address));
        }

        #endregion
    }
}
