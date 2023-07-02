using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_WndHod2pc : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\windows\hod2pc";

        /*** MEMORY ADDRESSES **/
        private UInt32 _P1_X_Offset = 0x005C8FC4;
        private UInt32 _P1_Y_Offset = 0x005C8FC6;
        private UInt32 _P2_X_Offset = 0x005C8FEC ;
        private UInt32 _P2_Y_Offset = 0x005C8FEE;
        private UInt32 _Credits_Offset = 0x005C8E60;
        private NopStruct _Nop_X = new NopStruct(0x0000D1AD, 4);
        private NopStruct _Nop_Y = new NopStruct(0x0000D1B7, 4);
        //Outputs
        private UInt32 _P1_Status_Offset = 0x005A5C62;
        private UInt32 _P1_Life_Offset = 0x005A5C66;
        private UInt32 _P1_Ammo_Offset = 0x005A5C7C;
        private UInt32 _P2_Status_Offset = 0x005A5C92;
        private UInt32 _P2_Life_Offset = 0x005A5C96;
        private UInt32 _P2_Ammo_Offset = 0x005A5CAC;

        //Keys
        private VirtualKeyCode _P1_Trigger_VK = VirtualKeyCode.VK_RSHIFT;
        private VirtualKeyCode _P1_Reload_VK = VirtualKeyCode.VK_RCONTROL;
        private VirtualKeyCode _P2_Trigger_VK = VirtualKeyCode.VK_LSHIFT;
        private VirtualKeyCode _P2_Reload_VK = VirtualKeyCode.VK_LCONTROL;

        //Play the "Coins" sound when adding coin
        SoundPlayer _SndPlayer;

        //Custom Outputs
        private int _P1_LastLife = 0;
        private int _P2_LastLife = 0;
        private int _P1_LastAmmo = 0;
        private int _P2_LastAmmo = 0;
        private int _P1_Life = 0;
        private int _P2_Life = 0;
        private int _P1_Ammo = 0;
        private int _P2_Ammo = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndHod2pc(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "hod2", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("hod2.exe PC-ISOZONE - uncracked", "97c9e516a287aab33a455a396dadaa45");
            _KnownMd5Prints.Add("hod2.exe PC-ISOZONE - cracked", "eb51d3856997581ed3aa8ecb7d6d8d07");
            _KnownMd5Prints.Add("hod2 Unknown Release #1", "fd53bc12b72958c819cf6931787df3cb");
            _KnownMd5Prints.Add("hod2 Unknown Release #2", "a54ec23a78d07a78653bbd31919db0b5");
            _KnownMd5Prints.Add("hod2 Unknown Release #3", "258f350774317f785b430be32d2abe9a");

            _tProcess.Start();            
            Logger.WriteLog("Waiting for Windows Game " + _RomName + " game to hook.....");
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
                            _GameWindowHandle = _TargetProcess.MainWindowHandle;
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();
                            ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                            if (!_DisableInputHack)
                                SetHack();
                            else
                                Logger.WriteLog("Input Hack disabled");
                            _ProcessHooked = true;
                            RaiseGameHookedEvent();    

                            //Try to load the "coin" sound
                            try
                            {
                                String strCoinSndPath = _TargetProcess.MainModule.FileName;
                                strCoinSndPath = strCoinSndPath.Substring(0, strCoinSndPath.Length - 8);
                                strCoinSndPath += @"sound\SE\START_COIN\coin1_16.wav";
                                _SndPlayer = new SoundPlayer(strCoinSndPath);
                            }
                            catch
                            {
                                Logger.WriteLog("Unable to find/open the coin1_16.wav file for Hotd2");
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
        /// </summary>
        public override bool GameScale(PlayerSettings PlayerData)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X => [-320 ; +320] => 640
                    //Y => [-240; +240] => 480
                    double dMinX = -320.0;
                    double dMaxX = 320.0;
                    double dMinY = -240.0;
                    double dMaxY = 240.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;                    

                    PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dRangeX * PlayerData.RIController.Computed_X / TotalResX) - dRangeX / 2);
                    PlayerData.RIController.Computed_Y = Convert.ToInt16((Math.Round(dRangeY * PlayerData.RIController.Computed_Y / TotalResY) - dRangeY / 2) * -1);
                    if (PlayerData.RIController.Computed_X < (int)dMinX)
                        PlayerData.RIController.Computed_X = (int)dMinX;
                    if (PlayerData.RIController.Computed_Y < (int)dMinY)
                        PlayerData.RIController.Computed_Y = (int)dMinY;
                    if (PlayerData.RIController.Computed_X > (int)dMaxX)
                        PlayerData.RIController.Computed_X = (int)dMaxX;
                    if (PlayerData.RIController.Computed_Y > (int)dMaxY)
                        PlayerData.RIController.Computed_Y = (int)dMaxY;

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
        /// Simple Hack : NOPing Axis procedures
        /// </summary>
        private void SetHack()
        {
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_X);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Y);
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>   
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0) 
                    Send_VK_KeyDown(_P1_Trigger_VK);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0) 
                    Send_VK_KeyUp(_P1_Trigger_VK);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0) 
                    Send_VK_KeyDown(_P1_Reload_VK);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0) 
                    Send_VK_KeyUp(_P1_Reload_VK);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0) 
                    Send_VK_KeyDown(_P1_Reload_VK);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0) 
                    Send_VK_KeyUp(_P1_Reload_VK);
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Send_VK_KeyDown(_P2_Trigger_VK);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Send_VK_KeyUp(_P2_Trigger_VK);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Send_VK_KeyDown(_P1_Reload_VK);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Send_VK_KeyUp(_P2_Reload_VK);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Send_VK_KeyDown(_P2_Reload_VK);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Send_VK_KeyUp(_P2_Reload_VK);
            }
        }

        /// <summary>
        /// Low-level Keyboard hook callback.
        /// This is used to add coin to the game
        /// </summary>
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    if (s.scanCode ==  HardwareScanCode.DIK_5)
                    {
                        byte Credits = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset);
                        Credits++;
                        WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset, Credits);
                        if (_SndPlayer != null)
                            _SndPlayer.Play();
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
            //Player status :
            //[4] = Continue Screen
            //[5] = InGame
            //[6] = Game Over
            //[7] = Time attack Scoreboard
            //[9] = Menu or Attract Mode
            // We will use these values to compute ourselve Recoil and P1/P2 Start Button Lights
            UInt32 P1_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Status_Offset);
            UInt32 P2_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Status_Offset);           
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            if (P1_Status == 5)
            {
                //Force Start Lamp to Off
                SetOutputValue(OutputId.P1_CtmLmpStart, 0);

                _P1_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Life_Offset);
                _P1_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Ammo_Offset);            

                //Custom Recoil
                if (_P1_Ammo < _P1_LastAmmo)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Clip Empty] custom Output
                if (_P1_Ammo > 0)
                    P1_Clip = 1;

                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }
            else
            {                
                //Enable Start Lamp Blinking
                SetOutputValue(OutputId.P1_CtmLmpStart, -1);
            }

            if (P2_Status == 5)
            {
                //Force Start Lamp to Off
                SetOutputValue(OutputId.P2_CtmLmpStart, 0);

                _P2_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Life_Offset);
                _P2_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Ammo_Offset);

                //Custom Recoil
                if (_P2_Ammo < _P2_LastAmmo)
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                //[Clip Empty] custom Output
                if (_P2_Ammo > 0)
                    P2_Clip = 1;

                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);
            }
            else
            {
                //Enable Start Lamp Blinking
                SetOutputValue(OutputId.P2_CtmLmpStart, -1);
            }

            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;            

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
