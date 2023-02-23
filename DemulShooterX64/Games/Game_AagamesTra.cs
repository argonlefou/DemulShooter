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
    class Game_AagamesTra : Game
    {
        private bool _HideCrosshair = false;

        //Float "POINT" for Unity float axis values
        private struct Vector3{
            public float x;
            public float y;
        }

        //Custom fields to store Float Shoot location
        private Vector3[] _PlayerShootAxis;
        //Custom fields to store Float Crosshair location (different range from target shooting values)
        private POINT[] _PlayerCrosshairAxis;

        //Outputs
        private float _P1_LastLife = 0.0f;
        private float _P2_LastLife = 0.0f;
        private float _P3_LastLife = 0.0f;
        private float _P4_LastLife = 0.0f;
        private float _P1_Life = 0.0f;
        private float _P2_Life = 0.0f;
        private float _P3_Life = 0.0f;
        private float _P4_Life = 0.0f;

        private bool _HackEnabled = false;
        private MMFH_TombRaider _Mmfh;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_AagamesTra(String RomName, bool HideCorsshair, bool DisableInputHack, bool Verbose)
            : base(RomName, "Game", DisableInputHack, Verbose)
        {
            _HideCrosshair = HideCorsshair;
            _KnownMd5Prints.Add("Tomb Raider Arcade - Clean Dump (dongle patched)", "7fccfcaa13ebe72043a32601de6d1a6b");
            _PlayerCrosshairAxis = new POINT[4];
            _PlayerShootAxis = new Vector3[4];
            _tProcess.Start();
            Logger.WriteLog("Waiting for Adrenaline Amusements game " + _RomName + " game to hook.....");
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
                            if (_TargetProcess.MainWindowHandle != IntPtr.Zero && _TargetProcess.MainWindowTitle == "SquareEnix_TombRaider")
                            {
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", @"Game_Data\Managed\Assembly-CSharp.dll");
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
        /// Game is using 2 differents axis :
        /// 1 For the shooting target and collision (which is [0-1] float)
        /// 1 To display crosshair on screen (which seems to be 1080 for Y and X change according to the screen ratio)
        /// Origin is Bottom-Left (inverted from Windows Lightgun data origins, Top-Left)
        /// The regular PlayerData.RIController.Computed_X and PlayerData.RIController.Computed_Y will store shooting values
        /// Other private fields will store Crosshair display values
        /// </summary>
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

                    double dRatio = TotalResX / TotalResY;
                    Logger.WriteLog("Game Window ratio = " + dRatio);

                    //Crosshair X => [0 - ?]
                    //Crosshair Y => [0 -  1080]
                    double dMaxY = 1080.0;
                    double dMaxX = dMaxY * dRatio;

                    _PlayerShootAxis[PlayerData.ID - 1].x = Convert.ToSingle(Math.Round(PlayerData.RIController.Computed_X / TotalResX, 2));
                    _PlayerShootAxis[PlayerData.ID - 1].y = 1.0f - Convert.ToSingle(Math.Round(PlayerData.RIController.Computed_Y / TotalResY, 2));

                    if (_PlayerShootAxis[PlayerData.ID - 1].x < 0)
                        _PlayerShootAxis[PlayerData.ID - 1].x = 0;
                    if (_PlayerShootAxis[PlayerData.ID - 1].y < 0)
                        _PlayerShootAxis[PlayerData.ID - 1].y = 0;
                    if (_PlayerShootAxis[PlayerData.ID - 1].x > 1.0f)
                        _PlayerShootAxis[PlayerData.ID - 1].x = 1.0f;
                    if (_PlayerShootAxis[PlayerData.ID - 1].y > 1.0f)
                        _PlayerShootAxis[PlayerData.ID - 1].y = 1.0f;

                    if (!_HideCrosshair)
                    {
                        _PlayerCrosshairAxis[PlayerData.ID - 1].X = Convert.ToInt32(dMaxX * _PlayerShootAxis[PlayerData.ID - 1].x);
                        _PlayerCrosshairAxis[PlayerData.ID - 1].Y = Convert.ToInt32(dMaxY * _PlayerShootAxis[PlayerData.ID - 1].y);
                    }
                    else
                    {
                        _PlayerCrosshairAxis[PlayerData.ID - 1].X = -100;
                        _PlayerCrosshairAxis[PlayerData.ID - 1].Y = -100;
                    }

                    Logger.WriteLog("Shoot Target location for P" + PlayerData.ID + " (float) = [ " + _PlayerShootAxis[PlayerData.ID - 1].x + ", " + _PlayerShootAxis[PlayerData.ID - 1].y + " ]");
                    Logger.WriteLog("Crosshair location for P" + PlayerData.ID + " (px) = [ " + _PlayerCrosshairAxis[PlayerData.ID - 1].X + ", " + _PlayerCrosshairAxis[PlayerData.ID - 1].Y + " ]");                 

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
            _Mmfh = new MMFH_TombRaider();
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
                Array.Copy(BitConverter.GetBytes(_PlayerShootAxis[PlayerData.ID - 1].x), 0, _Mmfh.Payload, MMFH_TombRaider.INDEX_P1_INGAME_X + 16 * (PlayerData.ID -1), 4);
                Array.Copy(BitConverter.GetBytes(_PlayerShootAxis[PlayerData.ID - 1].y), 0, _Mmfh.Payload, MMFH_TombRaider.INDEX_P1_INGAME_Y + 16 * (PlayerData.ID -1), 4);
                Array.Copy(BitConverter.GetBytes(_PlayerCrosshairAxis[PlayerData.ID - 1].X), 0, _Mmfh.Payload, MMFH_TombRaider.INDEX_P1_UISCREEN_X + 16 * (PlayerData.ID - 1), 4);
                Array.Copy(BitConverter.GetBytes(_PlayerCrosshairAxis[PlayerData.ID - 1].Y), 0, _Mmfh.Payload, MMFH_TombRaider.INDEX_P1_UISCREEN_Y + 16 * (PlayerData.ID - 1), 4);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    _Mmfh.Payload[MMFH_TombRaider.INDEX_P1_TRIGGER + 4 * (PlayerData.ID - 1)] = 1;
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    _Mmfh.Payload[MMFH_TombRaider.INDEX_P1_TRIGGER + 4 * (PlayerData.ID - 1)] = 0;

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    _Mmfh.Payload[MMFH_TombRaider.INDEX_P1_RELOAD + 4 * (PlayerData.ID - 1)] = 1;
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    _Mmfh.Payload[MMFH_TombRaider.INDEX_P1_RELOAD + 4 * (PlayerData.ID - 1)] = 0;

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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Ammo, OutputId.P3_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Ammo, OutputId.P4_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Clip, OutputId.P2_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Clip, OutputId.P3_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Clip, OutputId.P4_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P3_CtmRecoil, OutputId.P3_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P4_CtmRecoil, OutputId.P4_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Life, OutputId.P3_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Life, OutputId.P4_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P3_Damaged, OutputId.P3_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P4_Damaged, OutputId.P4_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Credits, OutputId.P1_Credit));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Credits, OutputId.P2_Credit));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Credits, OutputId.P3_Credit));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Credits, OutputId.P4_Credit));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            _P1_Life = 0.0f;
            _P2_Life = 0.0f;
            _P3_Life = 0.0f;
            _P4_Life = 0.0f;

            int r = _Mmfh.ReadAll();
            if (r == 0)
            {
                _P1_Life = BitConverter.ToSingle(_Mmfh.Payload, MMFH_TombRaider.INDEX_P1_LIFE);
                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                int P1_Ammo = _Mmfh.Payload[MMFH_TombRaider.INDEX_P1_AMMO];
                SetOutputValue(OutputId.P1_Ammo, P1_Ammo);
                //[Clip Empty] custom Output
                if (P1_Ammo > 0)
                    SetOutputValue(OutputId.P1_Clip, 1);
                else
                    SetOutputValue(OutputId.P1_Clip, 0);

                if (_Mmfh.Payload[MMFH_TombRaider.INDEX_P1_MOTOR] == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);
                    _Mmfh.Payload[MMFH_TombRaider.INDEX_P1_MOTOR] = 0;
                }

                _P2_Life = BitConverter.ToSingle(_Mmfh.Payload, MMFH_TombRaider.INDEX_P2_LIFE);
                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);

                int P2_Ammo = _Mmfh.Payload[MMFH_TombRaider.INDEX_P2_AMMO];
                SetOutputValue(OutputId.P2_Ammo, P2_Ammo);
                //[Clip Empty] custom Output
                if (P2_Ammo > 0)
                    SetOutputValue(OutputId.P2_Clip, 1);
                else
                    SetOutputValue(OutputId.P2_Clip, 0);

                if (_Mmfh.Payload[MMFH_TombRaider.INDEX_P2_MOTOR] == 1)
                {
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);
                    _Mmfh.Payload[MMFH_TombRaider.INDEX_P2_MOTOR] = 0;
                }

                _P3_Life = BitConverter.ToSingle(_Mmfh.Payload, MMFH_TombRaider.INDEX_P3_LIFE);
                //[Damaged] custom Output                
                if (_P3_Life < _P3_LastLife)
                    SetOutputValue(OutputId.P3_Damaged, 1);

                int P3_Ammo = _Mmfh.Payload[MMFH_TombRaider.INDEX_P3_AMMO];
                SetOutputValue(OutputId.P3_Ammo, P3_Ammo);
                //[Clip Empty] custom Output
                if (P3_Ammo > 0)
                    SetOutputValue(OutputId.P3_Clip, 1);
                else
                    SetOutputValue(OutputId.P3_Clip, 0);

                if (_Mmfh.Payload[MMFH_TombRaider.INDEX_P3_MOTOR] == 1)
                {
                    SetOutputValue(OutputId.P3_CtmRecoil, 1);
                    _Mmfh.Payload[MMFH_TombRaider.INDEX_P3_MOTOR] = 0;
                }

                _P4_Life = BitConverter.ToSingle(_Mmfh.Payload, MMFH_TombRaider.INDEX_P4_LIFE);
                //[Damaged] custom Output                
                if (_P4_Life < _P4_LastLife)
                    SetOutputValue(OutputId.P4_Damaged, 1);

                int P4_Ammo = _Mmfh.Payload[MMFH_TombRaider.INDEX_P4_AMMO];
                SetOutputValue(OutputId.P4_Ammo, P4_Ammo);
                //[Clip Empty] custom Output
                if (P4_Ammo > 0)
                    SetOutputValue(OutputId.P4_Clip, 1);
                else
                    SetOutputValue(OutputId.P4_Clip, 0);

                if (_Mmfh.Payload[MMFH_TombRaider.INDEX_P4_MOTOR] == 1)
                {
                    SetOutputValue(OutputId.P4_CtmRecoil, 1);
                    _Mmfh.Payload[MMFH_TombRaider.INDEX_P4_MOTOR] = 0;
                }

                _P1_LastLife = _P1_Life;
                _P2_LastLife = _P2_Life;
                _P3_LastLife = _P3_Life;
                _P4_LastLife = _P4_Life;
                SetOutputValue(OutputId.P1_Life, (int)_P1_Life);
                SetOutputValue(OutputId.P2_Life, (int)_P2_Life);
                SetOutputValue(OutputId.P3_Life, (int)_P3_Life);
                SetOutputValue(OutputId.P4_Life, (int)_P4_Life);

                SetOutputValue(OutputId.P1_Credit, BitConverter.ToInt32(_Mmfh.Payload, MMFH_TombRaider.INDEX_P1_CREDITS));
                SetOutputValue(OutputId.P2_Credit, BitConverter.ToInt32(_Mmfh.Payload, MMFH_TombRaider.INDEX_P2_CREDITS));
                SetOutputValue(OutputId.P3_Credit, BitConverter.ToInt32(_Mmfh.Payload, MMFH_TombRaider.INDEX_P3_CREDITS));
                SetOutputValue(OutputId.P4_Credit, BitConverter.ToInt32(_Mmfh.Payload, MMFH_TombRaider.INDEX_P4_CREDITS));

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
