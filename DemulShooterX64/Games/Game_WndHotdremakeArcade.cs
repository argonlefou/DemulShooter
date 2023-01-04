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
    public class Game_WndHotdremakeArcade : Game
    {
        private const string GAME_WINDOW_TITLE = "The House of the Dead Remake";
        private bool _HackEnabled = false;
        private MMFH_HotdRemakeArcade _Mmfh;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndHotdremakeArcade(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "Game", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("House of the dead Remake - GOG v1.0.0.4", "4cd32190b44d7c2d6b8ca52d0d372692"); //exe = D5FCA0B03AE64D1D0E75B795BD6CCA9F
            _KnownMd5Prints.Add("House of the dead Remake - STEAM (v1.0)", "7fccfcaa13ebe72043a32601de6d1a6b");
            _tProcess.Start();
            Logger.WriteLog("Waiting for Windows game " + _RomName + " game to hook.....");
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
                            if (_TargetProcess.MainWindowHandle != IntPtr.Zero && _TargetProcess.MainWindowTitle == GAME_WINDOW_TITLE)
                            {
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", @"The House of the Dead Remake_Data\Managed\Assembly-CSharp.dll");
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
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// this one is easy as the value is simply the pixel corresponding point on the window
        /// </summary>
        /*public override bool GameScale(PlayerSettings PlayerData)
        {
            
        }*/

        #endregion

        #region Memory Hack

        /// <summary>
        /// Creating a custom memory bank to store our data
        /// </summary>
        private void SetHack()
        {
            //Creating Shared Memory access
            _Mmfh = new MMFH_HotdRemakeArcade();
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
                Array.Copy(BitConverter.GetBytes((float)PlayerData.RIController.Computed_X), 0, _Mmfh.Payload, (int)MMFH_HotdRemakeArcade.Payload_Inputs_Index.P1_AxisX + (PlayerData.ID - 1), 4);
                Array.Copy(BitConverter.GetBytes((float)PlayerData.RIController.Computed_Y), 0, _Mmfh.Payload, (int)MMFH_HotdRemakeArcade.Payload_Inputs_Index.P1_AxisY + (PlayerData.ID - 1), 4);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    _Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Inputs_Index.P1_Trigger + (PlayerData.ID - 1)] = 2;
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    _Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Inputs_Index.P1_Trigger + (PlayerData.ID - 1)] = 1;

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    _Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Inputs_Index.P1_Reload + (PlayerData.ID - 1)] = 1;
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    _Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Inputs_Index.P1_Reload + (PlayerData.ID - 1)] = 0;

                int r = _Mmfh.WriteInputs();
                if (r != 0)
                    Logger.WriteLog("SendInput() => Error writing Mapped Memory Inputs : " + r.ToString());
            }
        }

        /// <summary>
        ///  Mouse callback for low level hook
        ///  This is used to block LeftClick events on the window, because double clicking on the upper-left corner
        ///  makes demul switch from Fullscreen to Windowed mode
        /// </summary>        
        /*public override IntPtr MouseHookCallback(IntPtr MouseHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (UInt32)wParam == Win32Define.WM_LBUTTONDOWN)
            {
                if (_ProcessHooked && Win32API.GetForegroundWindow() == _TargetProcess.MainWindowHandle)
                {
                    //Just blocking left clicks
                    return new IntPtr(1);
                }
            }
            return Win32API.CallNextHookEx(MouseHookID, nCode, wParam, lParam);
        }*/

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : Is activated for every bullet fired AND when player gets
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P1_CtmLmpStart, OutputId.P1_CtmLmpStart, 500));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P2_CtmLmpStart, OutputId.P2_CtmLmpStart, 500));
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
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            int r = _Mmfh.ReadAll();
            if (r == 0)
            {
                //P1 Life
                SetOutputValue(OutputId.P1_Life, _Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Outputs_Index.P1_Life]);
                
                //P1_Ammo
                int P1_Ammo = _Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Outputs_Index.P1_Ammo];
                SetOutputValue(OutputId.P1_Ammo, P1_Ammo);
                //[Clip Empty] custom Output
                if (P1_Ammo > 0)
                    SetOutputValue(OutputId.P1_Clip, 1);
                else
                    SetOutputValue(OutputId.P1_Clip, 0);

                //P1_Recoil
                if (_Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Outputs_Index.P1_Recoil] == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);
                    _Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Outputs_Index.P1_Recoil] = 0;
                }

                //P1_Damaged
                if (_Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Outputs_Index.P1_Damaged] == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);
                    _Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Outputs_Index.P1_Damaged] = 0;
                }


                //P2 Life
                SetOutputValue(OutputId.P2_Life, _Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Outputs_Index.P2_Life]);

                //P1_Ammo
                int P2_Ammo = _Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Outputs_Index.P2_Ammo];
                SetOutputValue(OutputId.P2_Ammo, P2_Ammo);
                //[Clip Empty] custom Output
                if (P2_Ammo > 0)
                    SetOutputValue(OutputId.P2_Clip, 1);
                else
                    SetOutputValue(OutputId.P2_Clip, 0);

                //P1_Recoil
                if (_Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Outputs_Index.P2_Recoil] == 1)
                {
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);
                    _Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Outputs_Index.P2_Recoil] = 0;
                }

                //P1_Damaged
                if (_Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Outputs_Index.P2_Damaged] == 1)
                {
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);
                    _Mmfh.Payload[(int)MMFH_HotdRemakeArcade.Payload_Outputs_Index.P2_Damaged] = 0;
                }

                //Credits
                SetOutputValue(OutputId.Credits, BitConverter.ToInt32(_Mmfh.Payload, (int)MMFH_HotdRemakeArcade.Payload_Outputs_Index.Credits));

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
