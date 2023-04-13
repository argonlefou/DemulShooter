using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.IPC;
using DsCore.MameOutput;
using DsCore.RawInput;
using DsCore.Win32;


namespace DemulShooterX64
{
    public class Game_UnisNightHunterArcade : Game
    {
        //Outputs
        private int _P1_Life = 0;
        private int _P2_Life = 0;
        private int _P1_LastLife = 0;
        private int _P2_LastLife = 0;

        private bool _HackEnabled = false;
        private MMFH_NightHunterArcade _Mmfh;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_UnisNightHunterArcade(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "qumo2_en", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Night Hunter v2.0.6 - Clean Dump", "1d6cbc2b0ebaf0bacbcbea84aa0f4a27");
            _KnownMd5Prints.Add("Night Hunter v2.0.6 - Dongle patched", "d223a04447e84073a178a14d50349da8");
            _tProcess.Start();
            Logger.WriteLog("Waiting for UNIS game " + _RomName + " game to hook.....");
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
                            // The game may start with other Windows than the main one (BepInEx console, other stuff.....) so we need to filter
                            // the displayed window according to the Title, if DemulShooter is started before the game,  to hook the correct one
                            if (_TargetProcess.MainWindowHandle != IntPtr.Zero && _TargetProcess.MainWindowTitle == "test_gun")
                            {
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", @"qumo2_en_Data\Managed\Assembly-CSharp.dll");
                                CheckMd5(AssemblyDllPath);
                                if (!_DisableInputHack)
                                    SetHack();
                                else
                                    Logger.WriteLog("Input Hack disabled");
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
                            }
                        }
                    }
                }
                catch (Exception Ex)
                {
                    Logger.WriteLog("Error trying to hook " + _Target_Process_Name + ".exe : " + Ex.Message.ToString());
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

        #region Screen

        /// <summary>
        /// Using the MOUSE INPUT type for the game, we need to send data in range [WindowWidth, WindowHeight]
        /// </summary>
        /// <param name="PlayerData"></param>
        /// <returns></returns>
        public override bool GameScale(PlayerSettings PlayerData)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    //Game Window size
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    //Win32API.GetWindowRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    //Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    PlayerData.RIController.Computed_Y = (int)TotalResY - PlayerData.RIController.Computed_Y;

                    if (PlayerData.RIController.Computed_X < 0)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 0)
                        PlayerData.RIController.Computed_Y = 0;
                    if (PlayerData.RIController.Computed_X > (int)TotalResX)
                        PlayerData.RIController.Computed_X = (int)TotalResX;
                    if (PlayerData.RIController.Computed_Y > (int)TotalResY)
                        PlayerData.RIController.Computed_Y = (int)TotalResY;                   
                    
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Error scaling mouse coordonates to GameFormat : " + ex.Message.ToString());
                }
            }
            return false;
        }

        #endregion

        #region Memory Hack

        /// <summary>
        /// Creating a custom memory bank to store our data
        /// </summary>
        private void SetHack()
        {
            //Creating Shared Memory access
            _Mmfh = new MMFH_NightHunterArcade();
            int r = _Mmfh.MMFOpen();
            if (r == 0)
            {
                _HackEnabled = true;
            }
            else
                Logger.WriteLog("SetHack() => Error opening Mapped Memory File");
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>
        public override void SendInput(PlayerSettings PlayerData)
        {
            if (_HackEnabled)
            {
                Array.Copy(BitConverter.GetBytes(PlayerData.RIController.Computed_X), 0, _Mmfh.Payload, MMFH_NightHunterArcade.INDEX_P1_INGAME_X + 8 * (PlayerData.ID - 1), 4);
                Array.Copy(BitConverter.GetBytes(PlayerData.RIController.Computed_Y), 0, _Mmfh.Payload, MMFH_NightHunterArcade.INDEX_P1_INGAME_Y + 8 * (PlayerData.ID - 1), 4);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    _Mmfh.Payload[MMFH_NightHunterArcade.INDEX_P1_TRIGGER + 4 * (PlayerData.ID - 1)] = 1;
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    _Mmfh.Payload[MMFH_NightHunterArcade.INDEX_P1_TRIGGER + 4 * (PlayerData.ID - 1)] = 2;

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    _Mmfh.Payload[MMFH_NightHunterArcade.INDEX_P1_WEAPON + 4 * (PlayerData.ID - 1)] = 1;
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    _Mmfh.Payload[MMFH_NightHunterArcade.INDEX_P1_WEAPON + 4 * (PlayerData.ID - 1)] = 2;

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    _Mmfh.Payload[MMFH_NightHunterArcade.INDEX_P1_SPECIAL + 4 * (PlayerData.ID - 1)] = 1;
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    _Mmfh.Payload[MMFH_NightHunterArcade.INDEX_P1_SPECIAL + 4 * (PlayerData.ID - 1)] = 2;

                int r = _Mmfh.WriteInputs();
                if (r != 0)
                    Logger.WriteLog("SendInput() => Error writing Mapped Memory Inputs : " + r.ToString());
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : Is activated for every bullet fired AND when player gets
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            _P1_Life = 0;
            _P2_Life = 0;

            int r = _Mmfh.ReadAll();
            if (r == 0)
            {
                _P1_Life = BitConverter.ToInt32(_Mmfh.Payload, MMFH_NightHunterArcade.INDEX_P1_LIFE);
                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);


                if (_Mmfh.Payload[MMFH_NightHunterArcade.INDEX_P1_MOTOR] == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);
                    _Mmfh.Payload[MMFH_NightHunterArcade.INDEX_P1_MOTOR] = 0;
                }

                _P2_Life = BitConverter.ToInt32(_Mmfh.Payload, MMFH_NightHunterArcade.INDEX_P2_LIFE);
                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);


                if (_Mmfh.Payload[MMFH_NightHunterArcade.INDEX_P2_MOTOR] == 1)
                {
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);
                    _Mmfh.Payload[MMFH_NightHunterArcade.INDEX_P2_MOTOR] = 0;
                }

                _P1_LastLife = _P1_Life;
                _P2_LastLife = _P2_Life;
                SetOutputValue(OutputId.P1_Life, _P1_Life);
                SetOutputValue(OutputId.P2_Life, _P2_Life);

                SetOutputValue(OutputId.Credits, BitConverter.ToInt32(_Mmfh.Payload, MMFH_NightHunterArcade.INDEX_CREDITS));

                int r2 = _Mmfh.Writeall();
                if (r2 != 0)
                    Logger.WriteLog("UpdateOutputValues() => Error writing Mapped Memory File : " + r.ToString());
            }
            else
                Logger.WriteLog("UpdateOutputValues() => Error reading Mapped Memory File : " + r.ToString());
        }

        #endregion
    }
}
