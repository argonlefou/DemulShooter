using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    class Game_WndBugBusters : Game
    {
        //Memory values
        private UInt32 _TestMenu_BtnSelect_Offset = 0x00060C58;
        private UInt32 _TestMenu_BtnEnter_Offset = 0x00060C5C;
        private UInt32 _GameMode_Offset = 0x00060BF8;
        private UInt32 _TestMenu_CoinsCounter_Offset = 0x000C3488;

        private UInt32 _P1_X_Offset = 0x0005FBA8;
        private UInt32 _P1_Y_Offset = 0x0005FBAC;
        private UInt32 _P1_Trigger_Offset = 0x0005FBB0;
        private UInt32 _P2_X_Offset = 0x0005FBC4;
        private UInt32 _P2_Y_Offset = 0x0005FBC8;
        private UInt32 _P2_Trigger_Offset = 0x0005FBCC;
        private UInt32 _P1_Life_Offset = 0x00055094;
        private UInt32 _P2_Life_Offset = 0x00055098;
        private UInt32 _P1_Ammo_Offset = 0x0005509C;
        private UInt32 _P2_Ammo_Offset = 0x000550A0;
        private UInt32 _GameCost_Offset = 0x00055090;
        private UInt32 _TotalCoins_Offset = 0x00060C1C;
        private UInt32 _P1_Lamp_Offset = 0x0009553C;
        private UInt32 _P2_Lamp_Offset = 0x00095540;
        private UInt32 _P1_Air_Offset = 0x00095418;
        private UInt32 _P2_Air_Offset = 0x0009541C;

        private UInt32 _P1_Playing_Offset = 0x00060C20;
        private UInt32 _P2_Playing_Offset = 0x00060C24;
        

        private HardwareScanCode _DIK_Test = HardwareScanCode.DIK_F2;
        private HardwareScanCode _DIK_Service = HardwareScanCode.DIK_F3;
        private HardwareScanCode _DIK_Credits = HardwareScanCode.DIK_5;

        private NopStruct _Nop_EnterKeyPress = new NopStruct(0x0000A793, 10);
        private NopStruct _Nop_LbuttonDown = new NopStruct(0x0000A3FF, 2);
        private NopStruct _Nop_LbuttonUp = new NopStruct(0x0000A425, 2);
        private NopStruct _Nop_RbuttonDown = new NopStruct(0x0000A44B, 2);
        private NopStruct _Nop_RbuttonUp = new NopStruct(0x0000A572, 2);
        private NopStruct _Nop_MouseMove = new NopStruct(0x0000A598, 2);
        private NopStruct _Nop_GamePadLoop = new NopStruct(0x00010DD9, 2);
        private NopStruct _Nop_HidLoopTrigger_1 = new NopStruct(0x0001139C, 10);
        private NopStruct _Nop_HidLoopTrigger_2 = new NopStruct(0x000113AE, 10);
        private NopStruct _Nop_InitCoinsCounter = new NopStruct(0x0004091F, 10);
        private NopStruct _Nop_ShowCrosshairInGame = new NopStruct(0x000263C1, 2);

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndBugBusters(String RomName, bool HideGameCrosshair, bool DisableInputHack, bool Verbose)
            : base(RomName, "BBPC", DisableInputHack, Verbose)
        {
            _HideCrosshair = HideGameCrosshair;

            _KnownMd5Prints.Add("Bug Buster v1.0.0.1 - Original exe", "832f2fa9ef018d390b7b477f4240ca8e");
            _KnownMd5Prints.Add("Bug Buster v1.0.0.1 - NO-CD patched", "7d4195bdfbfa843cac8af85616abbc21");
            _tProcess.Start();

            Logger.WriteLog("Waiting for Windows " + _RomName + " game to hook.....");
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
                            Apply_MemoryHacks();
                            _ProcessHooked = true;
                            RaiseGameHookedEvent();
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

                    //X => [0-640]
                    //Y => [0-480]
                    double dMaxX = 640.0;
                    double dMaxY = 480.0;

                    PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));

                    if (PlayerData.RIController.Computed_X < 0)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 0)
                        PlayerData.RIController.Computed_Y = 0;
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
        /// Genuine Hack, just blocking Axis and Triggers input to replace them.
        /// </summary>        
        protected override void Apply_InputsMemoryHack()
        {
            //Removing ENTER keypress event actions from the WndProc WM_KEYDOWN
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_EnterKeyPress);  
          
            //Removing Mouse events
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_LbuttonDown);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_LbuttonUp);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_RbuttonDown);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_RbuttonUp);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_MouseMove);  

            //If a gamepad is detected, looks like it's looping and overriding values
            //This removes the whole loop, maybe just noping +10F5E / +10F70 is enough and less risky ?
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_GamePadLoop); 
 
            //Some other kind of devices reset buttons states too
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_HidLoopTrigger_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_HidLoopTrigger_2); 

            //By default, this PC-version force the coins counter to 9 when displaying the start screen. Removing it....
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_InitCoinsCounter);

            if (_HideCrosshair)
                SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_ShowCrosshairInGame);

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>        
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((UInt32)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((UInt32)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x00);
                }

                //To reload, set back Gaz tank capacity to 1.0f (full)
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Ammo_Offset, new byte[] {0x00, 0x00, 0x80, 0x3F}); //1.0f
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    //
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Ammo_Offset, new byte[] {0x00, 0x00, 0x80, 0x3F}); //1.0f
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                   //
                }
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x00);
                }

                //To reload, set back Gaz tank capacity to 1.0f (full)
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Ammo_Offset, new byte[] { 0x00, 0x00, 0x80, 0x3F }); //1.0f
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    //
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Ammo_Offset, new byte[] { 0x00, 0x00, 0x80, 0x3F }); //1.0f
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    //
                }
            }
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
                    if (s.scanCode == _DIK_Test)
                    {
                        if (GetGameMode() != 12)
                            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _GameMode_Offset, 12);
                        else
                            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _TestMenu_BtnSelect_Offset, 0x01);
                    }
                    else if (s.scanCode == _DIK_Service)
                    {
                        if (GetGameMode() == 12)
                            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _TestMenu_BtnEnter_Offset, 0x01);
                    }
                    else if (s.scanCode == _DIK_Credits)
                    {
                        if (GetGameMode() != 12)
                        {
                            //Add a coins in the game
                            int Coins = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _TotalCoins_Offset, 4), 0);
                            Coins++;
                            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _TotalCoins_Offset, BitConverter.GetBytes(Coins));
                        }
                        else
                        {
                            //Write the coins entry flag, to increment coins counter
                            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _TestMenu_CoinsCounter_Offset, 0x01);
                        }

                    }
                }
                else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                {
                    if (s.scanCode == _DIK_Test)
                    {
                        if (GetGameMode() == 12)
                            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _TestMenu_BtnSelect_Offset, 0x00);
                    }
                    else if (s.scanCode == _DIK_Service)
                    {
                        if (GetGameMode() == 12)
                            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _TestMenu_BtnEnter_Offset, 0x00);
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_AirFront, OutputId.P1_AirFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_AirFront, OutputId.P2_AirFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Clip, OutputId.P2_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));

            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Lamp_Offset));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Lamp_Offset));
            SetOutputValue(OutputId.P1_AirFront, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Air_Offset));
            SetOutputValue(OutputId.P2_AirFront, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Air_Offset));

            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            if (GetGameMode() == 5)
            {
                if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Playing_Offset) == 1)
                {
                    _P1_Ammo = (int)(BitConverter.ToSingle(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Ammo_Offset, 4), 0) * 100);
                    _P1_Life = (int)(BitConverter.ToSingle(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Life_Offset, 4), 0) * 100);
                    if (_P1_Life < 0)
                        _P1_Life = 0;

                    //[Clip Empty] custom Output
                    if (_P1_Ammo > 0)
                        P1_Clip = 1;

                    //[Damaged] custom Output                
                    if (_P1_Life < _P1_LastLife)
                        SetOutputValue(OutputId.P1_Damaged, 1);
                }

                if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Playing_Offset) == 1)
                {
                    _P2_Ammo = (int)(BitConverter.ToSingle(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Ammo_Offset, 4), 0) * 100);
                    _P2_Life = (int)(BitConverter.ToSingle(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Life_Offset, 4), 0) * 100);
                    if (_P2_Life < 0)
                        _P2_Life = 0;

                    //[Clip Empty] custom Output
                    if (_P2_Ammo > 0)
                        P2_Clip = 1;

                    //[Damaged] custom Output                
                    if (_P2_Life < _P2_LastLife)
                        SetOutputValue(OutputId.P2_Damaged, 1);
                }
            }

            _P1_LastAmmo = _P1_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastAmmo = _P2_Ammo;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
            //Rumble created based on Air activation
            //Fan value can be 0 or 2 (when fire spary is activated), so for rumble we will convert it to 0/1
            if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Air_Offset) != 0)
                SetOutputValue(OutputId.P1_GunMotor, 1);
            else
                SetOutputValue(OutputId.P1_GunMotor, 0);
            if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Air_Offset) != 0)
                SetOutputValue(OutputId.P2_GunMotor, 1);
            else
                SetOutputValue(OutputId.P2_GunMotor, 0);

            //Credits
            int Cost = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _GameCost_Offset, 4), 0);
            if (Cost ==0)
            {
                SetOutputValue(OutputId.Credits, 0); // FREEPLAY
            }
            else
            {
                int Coins = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _TotalCoins_Offset, 4), 0);
                SetOutputValue(OutputId.Credits, Coins / Cost);
            }
        }

        #endregion

        /// <summary>
        /// Return Game mode
        /// 0 = ?
        /// 1 = Title Screen
        /// 2 = Attract Mode
        /// 3 = Level select
        /// 4 = Result
        /// 5 = In Game
        /// 6 = 
        /// 7 = Ranking
        /// 8 = How To Play
        /// 9 = 
        /// 10 = Credits
        /// 11 = Mode Select (Story / Free)
        /// 12 = Settings Menu
        /// 13 = Initial value on boot, then goes to 1 ?
        /// </summary>
        /// <returns></returns>
        private int GetGameMode()
        {
            return ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _GameMode_Offset);
        }
    }
}
