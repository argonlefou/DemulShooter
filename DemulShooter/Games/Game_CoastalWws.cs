using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.IPC;
using DsCore.MameOutput;
using DsCore.RawInput;
using DsCore.Win32;


namespace DemulShooter
{
    public class Game_CoastalWws : Game
    {
        //Custom fields to store Float Shoot location
        private float _P1_Shoot_X;
        private float _P1_Shoot_Y;
        private float _P2_Shoot_X;
        private float _P2_Shoot_Y;
        //Custom fields to store Float Crosshair location (different range from target shooting values)
        private float _P1_UIController_X;
        private float _P1_UIController_Y;
        private float _P2_UIController_X;
        private float _P2_UIController_Y;

        private bool _HackEnabled = false;
        private MMFH_WildWestShoutout _Mmfh;

        private HardwareScanCode _TestMode_Key;
        private HardwareScanCode _P1_Coin_Key;
        private HardwareScanCode _P2_Coin_Key;

        //Outputs
        private int _P1_LastLife = 0;
        private int _P2_LastLife = 0;
        private int _P1_Life = 0;
        private int _P2_Life = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_CoastalWws(String RomName, HardwareScanCode P1_Coin_Key, HardwareScanCode P2_Coin_Key, HardwareScanCode TestMode_Key, double ForcedXratio, bool DisableInputHack, bool Verbose)
            : base(RomName, "CowBoy", ForcedXratio, DisableInputHack, Verbose)
        {
            _P1_Coin_Key = P1_Coin_Key;
            _P2_Coin_Key = P2_Coin_Key;
            _TestMode_Key = TestMode_Key;
            _KnownMd5Prints.Add("Wild West Shoutout original dump", "4f543c469818c1db8bc856be84f0131e");
            _tProcess.Start();
            Logger.WriteLog("Waiting for Coastal " + _RomName + " game to hook.....");
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
                            if (_TargetProcess.MainWindowHandle != IntPtr.Zero && _TargetProcess.MainWindowTitle == "CowBoy")
                            {
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", @"CowBoy_Data\Managed\Assembly-CSharp.dll");
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

        #region Screen

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// Game is using 2 differents axis :
        /// 1 For the shooting target and collision (which is the window size boundaries)
        /// 1 To display crosshair on screen (which seems to be 1920 for X and Y changes according to the window ratio
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

                    //Crosshair X => [0 - 1920]
                    //Crosshair Y => [0 -  ??]
                    double dMaxX = 1920.0;
                    double dMaxY = 1920.0 / dRatio;

                    if (PlayerData.ID == 1)
                    {
                        _P1_Shoot_X = Convert.ToSingle(Math.Round(PlayerData.RIController.Computed_X / TotalResX, 2));
                        _P1_Shoot_Y = 1.0f - Convert.ToSingle(Math.Round(PlayerData.RIController.Computed_Y / TotalResY, 2));

                        if (_P1_Shoot_X < 0)
                            _P1_Shoot_X = 0;
                        if (_P1_Shoot_Y < 0)
                            _P1_Shoot_Y = 0;
                        if (_P1_Shoot_X > 1.0f)
                            _P1_Shoot_X = 1.0f;
                        if (_P1_Shoot_Y > 1.0f)
                            _P1_Shoot_Y = 1.0f;

                        _P1_UIController_X = Convert.ToSingle(dMaxX * _P1_Shoot_X);
                        _P1_UIController_Y = Convert.ToSingle(dMaxY * _P1_Shoot_Y);

                        Logger.WriteLog("Shoot Target location for P1 (float) = [ " + _P1_Shoot_X + ", " + _P1_Shoot_Y + " ]");
                        Logger.WriteLog("Crosshair location for P1 (px) = [ " + _P1_UIController_X + ", " + _P1_UIController_Y + " ]");
                    }
                    else if (PlayerData.ID == 2)
                    {
                        _P2_Shoot_X = Convert.ToSingle(Math.Round(PlayerData.RIController.Computed_X / TotalResX, 2));
                        _P2_Shoot_Y = 1.0f - Convert.ToSingle(Math.Round(PlayerData.RIController.Computed_Y / TotalResY, 2));

                        if (_P2_Shoot_X < 0)
                            _P2_Shoot_X = 0;
                        if (_P2_Shoot_Y < 0)
                            _P2_Shoot_Y = 0;
                        if (_P2_Shoot_X > 1.0f)
                            _P2_Shoot_X = 1.0f;
                        if (_P2_Shoot_Y > 1.0f)
                            _P2_Shoot_Y = 1.0f;

                        _P2_UIController_X = Convert.ToSingle(dMaxX * _P2_Shoot_X);
                        _P2_UIController_Y = Convert.ToSingle(dMaxY * _P2_Shoot_Y);

                        Logger.WriteLog("Shoot Target location for P2 (float) = [ " + _P2_Shoot_X + ", " + _P2_Shoot_Y + " ]");
                        Logger.WriteLog("Crosshair location for P2 (px) = [ " + _P2_UIController_X + ", " + _P2_UIController_Y + " ]");
                    }

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
            _Mmfh = new MMFH_WildWestShoutout();
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
            if (_Mmfh.ReadAll() == 0 && _HackEnabled)
            {
                if (PlayerData.ID == 1)
                {
                    Array.Copy(BitConverter.GetBytes(_P1_Shoot_X), 0, _Mmfh.Payload, MMFH_WildWestShoutout.INDEX_P1_INGAME_X, 4);
                    Array.Copy(BitConverter.GetBytes(_P1_Shoot_Y), 0, _Mmfh.Payload, MMFH_WildWestShoutout.INDEX_P1_INGAME_Y, 4);
                    Array.Copy(BitConverter.GetBytes(_P1_UIController_X), 0, _Mmfh.Payload, MMFH_WildWestShoutout.INDEX_P1_UISCREEN_X, 4);
                    Array.Copy(BitConverter.GetBytes(_P1_UIController_Y), 0, _Mmfh.Payload, MMFH_WildWestShoutout.INDEX_P1_UISCREEN_Y, 4);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P1_TRIGGER] = 2;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P1_TRIGGER] = 1;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    {
                        _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P1_RELOAD] = 1;
                    }
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    {
                       _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P1_RELOAD] = 0;
                    }
                }
                else if (PlayerData.ID == 2)
                {
                    Array.Copy(BitConverter.GetBytes(_P2_Shoot_X), 0, _Mmfh.Payload, MMFH_WildWestShoutout.INDEX_P2_INGAME_X, 4);
                    Array.Copy(BitConverter.GetBytes(_P2_Shoot_Y), 0, _Mmfh.Payload, MMFH_WildWestShoutout.INDEX_P2_INGAME_Y, 4);
                    Array.Copy(BitConverter.GetBytes(_P2_UIController_X), 0, _Mmfh.Payload, MMFH_WildWestShoutout.INDEX_P2_UISCREEN_X, 4);
                    Array.Copy(BitConverter.GetBytes(_P2_UIController_Y), 0, _Mmfh.Payload, MMFH_WildWestShoutout.INDEX_P2_UISCREEN_Y, 4);
                    
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P2_TRIGGER] = 2;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P2_TRIGGER] = 1;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    {
                        _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P2_RELOAD] = 1;
                    }
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    {
                        _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P2_RELOAD] = 0;
                    }
                }
                _Mmfh.Writeall();
            }
            else
                Logger.WriteLog("SendInput() => Error reading Mapped Memory file");
        }

        /// <summary>
        /// Low-level Keyboard hook callback.
        /// This is used to detect Pedal action for "Pedal-Mode" hack of DemulShooter
        /// </summary>
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    if (s.scanCode == _TestMode_Key)
                    {
                        _Mmfh.ReadAll();
                        _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_TEST] = 1;
                        _Mmfh.Writeall();
                    }
                    else if (s.scanCode == _P1_Coin_Key)
                    {
                        _Mmfh.ReadAll();
                        Array.Copy(BitConverter.GetBytes((UInt32)1), 0, _Mmfh.Payload, MMFH_WildWestShoutout.INDEX_P1_COIN, 4);
                        _Mmfh.Writeall();
                    }
                    else if (s.scanCode == _P2_Coin_Key)
                    {
                        _Mmfh.ReadAll();
                        Array.Copy(BitConverter.GetBytes((UInt32)1), 0, _Mmfh.Payload, MMFH_WildWestShoutout.INDEX_P2_COIN, 4);
                        _Mmfh.Writeall();
                    }
                }                
            }
            return Win32API.CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
            /*_Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunRecoil, OutputId.P2_GunRecoil));*/
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Credits, OutputId.P1_Credit));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Credits, OutputId.P2_Credit));
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
                _P1_Life = _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P1_LIFE];
                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                int P1_Ammo = _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P1_AMMO];
                SetOutputValue(OutputId.P1_Ammo, P1_Ammo);
                //[Clip Empty] custom Output
                if (P1_Ammo > 0)
                    SetOutputValue(OutputId.P1_Clip, 1);
                else
                    SetOutputValue(OutputId.P1_Clip, 0);

                //Custom Recoil
                //Option  #1 => Through COM message (Recoil on menu and InGame, with a double message possible if Autofire enabled but too quick to be an issue)
                SetOutputValue(OutputId.P1_CtmRecoil, _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P1_GUNTEST]);
                _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P1_GUNTEST] = 0;
                //Option #2 => Custom computed Recoil on Mapped Memory (Only InGame, no recoil in Menu)
                /*SetOutputValue(OutputId.P1_CtmRecoil, _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P1_RECOIL]);
                _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P1_RECOIL] = 0;*/


                _P2_Life = _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P2_LIFE];
                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);

                int P2_Ammo = _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P2_AMMO];
                SetOutputValue(OutputId.P2_Ammo, P2_Ammo);
                //[Clip Empty] custom Output
                if (P2_Ammo > 0)
                    SetOutputValue(OutputId.P2_Clip, 1);
                else
                    SetOutputValue(OutputId.P2_Clip, 0);

                //Custom Recoil
                //Option  #1 => Through COM message (Recoil on menu and InGame, with a double message possible if Autofire enabled but too quick to be an issue)
                SetOutputValue(OutputId.P2_CtmRecoil, _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P2_GUNTEST]);
                _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P2_GUNTEST] = 0;
                //Option #2 => Custom computed Recoil on Mapped Memory (Only InGame, no recoil in Menu)
                /*SetOutputValue(OutputId.P1_CtmRecoil, _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P1_RECOIL]);
                _Mmfh.Payload[MMFH_WildWestShoutout.INDEX_P1_RECOIL] = 0;*/

                _P1_LastLife = _P1_Life;
                _P2_LastLife = _P2_Life;
                SetOutputValue(OutputId.P1_Life, _P1_Life);
                SetOutputValue(OutputId.P2_Life, _P2_Life);

                SetOutputValue(OutputId.P1_Credit, BitConverter.ToInt32(_Mmfh.Payload, MMFH_WildWestShoutout.INDEX_P1_CREDITS));
                SetOutputValue(OutputId.P2_Credit, BitConverter.ToInt32(_Mmfh.Payload, MMFH_WildWestShoutout.INDEX_P2_CREDITS));

                _Mmfh.Writeall();
            }
            else
                Logger.WriteLog("UpdateOutputValues() => Error reading Meped Memory File : " + r.ToString());
        }

        #endregion
    }
}
