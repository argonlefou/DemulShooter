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
using Microsoft.Win32;

namespace DemulShooter
{
    class Game_Hod3pc : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\windows\hod3pc";

        /*** MEMORY ADDRESSES **/
        private UInt32 _P1_X_Offset = 0x00367AA4;
        private UInt32 _P1_Y_Offset = 0x00367AA6;
        private UInt32 _P2_X_Offset = 0x00367AD8;
        private UInt32 _P2_Y_Offset = 0x00367ADA;
        private UInt32 _P1_Buttons_Offset = 0x0003ECF88;
        private UInt32 _P2_Buttons_Offset = 0x0003ED0F0;
        private UInt32 _Credits_Offset = 0x003B7DD0;
        private UInt32 _Buttons_Injection_Offset = 0x000B4BC1;
        private UInt32 _Buttons_Injection_Return_Offset = 0x000B4BC6;
        private NopStruct _Nop_X = new NopStruct(0x0006BE70, 4);
        private NopStruct _Nop_Y = new NopStruct(0x0006BE7B, 4);
        private NopStruct _Nop_Trigger_Button = new NopStruct(0x000B4C8C, 3);
        private NopStruct _Nop_Reload_Button = new NopStruct(0x000B4CB1, 4);
        private NopStruct _Nop_Left_Button = new NopStruct(0x000B4C54, 3);
        private NopStruct _Nop_Right_Button = new NopStruct(0x00B4C0E, 3);
        private NopStruct _Nop_NoAutoReload_1 = new NopStruct(0x0008DEDB, 3);
        private NopStruct _Nop_NoAutoReload_2 = new NopStruct(0x0008DF1E, 3);
        private NopStruct _Nop_Arcade_Mode_Display = new NopStruct(0x0008FD29, 2);        

        private bool _NoAutoReload = false;
        private bool _ArcadeModeDisplay = false;

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
        public Game_Hod3pc(String RomName, bool NoAutoReload, bool ArcadeModeDisplay, double ForcedXratio, bool Verbose) 
            : base (RomName, "hod3pc", ForcedXratio, Verbose)
        {
            _NoAutoReload = NoAutoReload;
            _ArcadeModeDisplay = ArcadeModeDisplay;
            _KnownMd5Prints.Add("hod3pc SEGA Windows", "4bf19dcb7f0182596d93f038189f2301");
            _KnownMd5Prints.Add("hod3pc RELOADED cracked", "3a4501d39bbb7271712421fb992ad37b");
            _KnownMd5Prints.Add("hod3pc REVELATION No-CD", "b8af47f16d5e43cddad8df0a6fdb46f5");
            _KnownMd5Prints.Add("hod3pc MYTH Release", "0228818e9412fc218fcd24bfd829a5a0");
            _KnownMd5Prints.Add("hod3pc TEST", "733da4e3cfe24b015e5f795811d481e6");
            _KnownMd5Prints.Add("hod3pc Unknown Release #1", "51dd72f83c0de5b27c0358ad11e2a036");
            _KnownMd5Prints.Add("hod3pc Unknown Release #2", "e4819dcf2105b85a7e7bc9dc66159f5c");

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
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();
                            ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                            SetHack();
                            _ProcessHooked = true;
                            RaiseGameHookedEvent();

                            //Try to load the "coin" sound
                            try
                            {
                                String strCoinSndPath = _TargetProcess.MainModule.FileName;
                                strCoinSndPath = strCoinSndPath.Substring(0, strCoinSndPath.Length - 10);
                                strCoinSndPath += @"..\media\coin002.aif";
                                _SndPlayer = new SoundPlayer(strCoinSndPath);
                            }
                            catch
                            {
                                Logger.WriteLog("Unable to find/open the coin002.aif file for Hotd3");
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
                    //Window size
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X => [-320 ; +320] => 640
                    //Y => [-240; +240] => 480
                    double dMinX = -320.0;
                    double dMaxX = 320.0;
                    double dMinY = -240.0;
                    double dMaxY = 240.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    //In case of forced scren ration (4/3)
                    if (_ForcedXratio > 0)
                    {
                        Logger.WriteLog("Forcing X Ratio to = " + _ForcedXratio.ToString());
                        double GameHeight = TotalResY;
                        double GameWidth = TotalResY * _ForcedXratio;
                        Logger.WriteLog("Game Viewport size (Px) = [ " + GameWidth + "x" + GameHeight + " ]");
                        
                        double HorizontalRatio = TotalResX / GameWidth;
                        dRangeX = dRangeX * HorizontalRatio;
                        dMaxX = (dRangeX / 2);
                        dMinX = -dMaxX;
                        Logger.WriteLog("Horizontal Ratio = " + HorizontalRatio.ToString());
                        Logger.WriteLog("New dMaxX = " + dMaxX.ToString());
                        Logger.WriteLog("New dMinX = " + dMinX.ToString());
                    }

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

        private void SetHack()
        {
            //Block Axis
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_X);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Y);

            //Block Buttons
            SetHack_Buttons();
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Trigger_Button);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Reload_Button);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Left_Button);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Right_Button);

            //Cancel Auto-reload when bullets = 0
            if (_NoAutoReload)
            {
                SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_NoAutoReload_1);
                SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_NoAutoReload_2);
                Logger.WriteLog("NoAutoReload Hack done");
            }

            //Hide guns at screen, like real arcade machine
            if (_ArcadeModeDisplay)
            {
                SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Arcade_Mode_Display);
                Logger.WriteLog("NoGuns Hack done");
            }

            //Gun data Init              
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, new byte[] { 0x00, 0x00 });
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, new byte[] { 0x00, 0x00 });
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, new byte[] { 0x00, 0x00 });
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, new byte[] { 0x00, 0x00 });   

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        private void SetHack_Buttons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<byte> Buffer;   
            //mov ebp,ecx
            CaveMemory.Write_StrBytes("8B E9");
            //and [ebp+00], 0101000C
            CaveMemory.Write_StrBytes("81 65 00 0C 00 01 01");
            CaveMemory.Write_jmp((UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Return_Offset);

            Logger.WriteLog("Adding Buttons Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);      
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
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset + 2, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset + 2, 0x0E);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    //Check aiming position to set the action of MiddleButton according to wich half of the screen the user is pointing
                    if (PlayerData.RIController.Computed_X < 0)
                        Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x04);
                    else
                        Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x08);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x03);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset + 3, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset + 3, 0x0E);
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset + 2, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset + 2, 0x0E);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    //Check aiming position to set the action of MiddleButton according to wich half of the screen the user is pointing
                    if (PlayerData.RIController.Computed_X < 0)
                        Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset, 0x04);
                    else
                        Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset, 0x08);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset, 0x03);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset + 3, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset + 3, 0x0E);
            }
        }

        /// <summary>
        /// Low-level mouse hook callback.
        /// This is used to block middle and right click events in game
        /// </summary>
        public override IntPtr MouseHookCallback(IntPtr MouseHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (UInt32)wParam == Win32Define.WM_MBUTTONDOWN)
            {
                //Just blocking middle clicks                
                return new IntPtr(1);
            }
            else if (nCode >= 0 && (UInt32)wParam == Win32Define.WM_RBUTTONDOWN)
            {
                //Just blocking right clicks => if not P1 reload play animation but does not reload
                return new IntPtr(1);
            }
            return Win32API.CallNextHookEx(MouseHookID, nCode, wParam, lParam);
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
                    if (s.scanCode == HardwareScanCode.DIK_5)
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
            UInt32 P1_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x003B8038);
            UInt32 P2_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x003B8264);
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

                _P1_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x003B8044);
                _P1_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x003B8090);
            
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

                _P2_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x003B8270);
                _P2_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x003B82BC);

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
