using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;
using DsCore.MameOutput;
using System.Collections.Generic;

namespace DemulShooter
{
    class Game_KonamiLethalEnforcers3 : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\konami\le3";

        /*** MEMORY ADDRESSES **/
        private UInt32 _BaseData_PtrOffset = 0x00294150;
        private UInt32 _BaseData_Address = 0;
        private UInt32 _TEST_Offset = 0xE43C;
        private UInt32 _SERVICE_Offset = 0xE43F;
        private UInt32 _P1_X_Offset = 0xE494;
        private UInt32 _P1_Y_Offset = 0xE498;
        private UInt32 _P2_X_Offset = 0xE4A8;
        private UInt32 _P2_Y_Offset = 0xE4AC;
        private UInt32 _P1_Start_Offset = 0xE446;
        private UInt32 _P2_Start_Offset = 0xE447;
        private UInt32 _P1_Trigger_Offset = 0xE448;
        private UInt32 _P2_Trigger_Offset = 0xE449;
        private UInt32 _P1_Option_Offset = 0xE44A;
        private UInt32 _P2_Option_Offset = 0xE44B;
        //private UInt32 _CreditsToAdd_Offset = 0x374628;
        private UInt32 _CreditsToAdd_Ptr_Offset = 0x294150; //better : calls for sound effect and add bookkeeping
        private UInt32 _CreditsTotal_Offset = 0x0037462C;

        private NopStruct _Nop_AxisX_1 = new NopStruct(0x0011D5C4, 2);
        private NopStruct _Nop_AxisX_2 = new NopStruct(0x0011D66F, 2);
        private NopStruct _Nop_AxisX_3 = new NopStruct(0x0011D6A3, 6);  
        private NopStruct _Nop_AxisY_1 = new NopStruct(0x0011D610, 3);
        private NopStruct _Nop_AxisY_2 = new NopStruct(0x0011D67D, 2);
        private NopStruct _Nop_AxisY_3 = new NopStruct(0x0011D6C7, 6);  

        //Original behavior, when player points the gun out of screen, is to wait for the aim to be at least in those boundaries to get out of "Cover" state
        // 33 < X < 607
        // 33 < Y < 448
        //This is problematic for our needs as the device may not be tracked in real time,
        //and use of "pedal mod" would force the player to still be hidden if it's aiming at the border
        //For that, we will push those boundaries to the max
        private UInt32 _Xmin_Patch_Offset = 0x0001349D;
        private UInt32 _Xmax_Patch_Offset = 0x000134B6;
        private UInt32 _Ymin_Patch_Offset = 0x000134CD;
        private UInt32 _Ymax_Patch_Offset = 0x000134E4;
        private UInt32 _Float_640_Offset = 0x0022B218;
        private UInt32 _Float_480_Offset = 0x0022B214;
        private UInt32 _Float_0_Offset = 0x0024F92C;

        

        private HardwareScanCode _Test_Key = HardwareScanCode.DIK_9;
        private HardwareScanCode _Service_Key = HardwareScanCode.DIK_0;
        private HardwareScanCode _P1_Start_Key = HardwareScanCode.DIK_1;
        private HardwareScanCode _P2_Start_Key = HardwareScanCode.DIK_2;
        private HardwareScanCode _Credits_Key = HardwareScanCode.DIK_5;
        //Disabling adding credits while the key stays pressed
        private bool _IsCreditsKeyPressed = false;

        //Outputs
        private UInt32 _Outputs_Offset = 0xE4BC; //Based on BaseData address
        private UInt32 _PlayerInfo_BasePtr_Offset = 0x0029423C;
        private UInt32 _GameScene_Offset = 0x00294234;  //17 = in game (not in attract)
        private UInt32 _Player1_Playing = 0x002945D0;
        private UInt32 _Player2_Playing = 0x002945D4;

        private int _P1_LastWeapon = 0;
        private int _P2_LastWeapon = 0;
        private int _P1_LastAmmo = 0;
        private int _P2_LastAmmo = 0;
        private int _P1_LastLife = 0;
        private int _P2_LastLife = 0;

        //Custom pedal-mod
        private bool _Pedal1_Enable;
        private HardwareScanCode _Pedal1_Key;
        private bool _isPedal1_Pushed = false;
        private bool _Pedal2_Enable;
        private HardwareScanCode _Pedal2_Key;
        private bool _isPedal2_Pushed = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_KonamiLethalEnforcers3(String RomName, bool Pedal1_Enable, HardwareScanCode Pedal1_Key, bool Pedal2_Enable, HardwareScanCode Pedal2_Key, bool DisableInputHack, bool Verbose) 
            : base (RomName, "game_patched", DisableInputHack, Verbose)
        {
            _Pedal1_Enable = Pedal1_Enable;
            _Pedal1_Key = Pedal1_Key;
            _Pedal2_Enable = Pedal2_Enable;
            _Pedal2_Key = Pedal2_Key;
            _KnownMd5Prints.Add("Lethal Enforcer 3 v2005-04-15-1 - Original game.exe", "1d338c452c7b087bc7aad823a74bb023");
            _KnownMd5Prints.Add("Lethal Enforcer 3 v2005-04-15-1 - DemulShooter compatible game.exe", "ce1359efe2dde0ac02280bfe8c96681d");
        
            _tProcess.Start();
            Logger.WriteLog("Waiting for KONAMI " + _RomName + " game to hook.....");
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
                            _BaseData_Address = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _BaseData_PtrOffset);

                            if (_BaseData_Address != 0)
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Data Pointer Address = 0x" + _BaseData_Address.ToString("X8"));
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                CheckExeMd5();
                                ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);

                                if (!_DisableInputHack)
                                {
                                    SetHack();
                                }
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

                    double dMaxX = 640.0;
                    double dMaxY = 480.0;

                    //If Pedal-Mod is used, we need to clmap value at +1/-1 compared to max values because of how the hack is done
                    //to check "return value" (>0 and <640, same for Y)
                    if ((_Pedal1_Enable && PlayerData.ID == 1) || (_Pedal2_Enable && PlayerData.ID == 2))
                    {
                        PlayerData.RIController.Computed_X = Convert.ToInt32(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                        PlayerData.RIController.Computed_Y = Convert.ToInt32(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));
                        if (PlayerData.RIController.Computed_X < 1)
                            PlayerData.RIController.Computed_X = 1;
                        if (PlayerData.RIController.Computed_Y < 1)
                            PlayerData.RIController.Computed_Y = 1;
                        if (PlayerData.RIController.Computed_X > (int)(dMaxX - 1))
                            PlayerData.RIController.Computed_X = (int)(dMaxX - 1);
                        if (PlayerData.RIController.Computed_Y > (int)(dMaxY - 1))
                            PlayerData.RIController.Computed_Y = (int)(dMaxY - 1);
                    }
                    //If genuine mode, we neeed to put -1 to the axis value to enable the cover action
                    else
                    {
                        PlayerData.RIController.Computed_X = Convert.ToInt32(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                        PlayerData.RIController.Computed_Y = Convert.ToInt32(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));
                        if (PlayerData.RIController.Computed_X < 1)
                            PlayerData.RIController.Computed_X = -1;
                        if (PlayerData.RIController.Computed_Y < 1)
                            PlayerData.RIController.Computed_Y = -1;
                        if (PlayerData.RIController.Computed_X > (int)(dMaxX - 1))
                            PlayerData.RIController.Computed_X = -1;
                        if (PlayerData.RIController.Computed_Y > (int)(dMaxY - 1))
                            PlayerData.RIController.Computed_Y = -1;
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
        /// Genuine Hack, just blocking Axis and filtering Triggers input to replace them without blocking other input
        /// </summary>
        private void SetHack()
        {
            //NOPing axis proc
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_AxisX_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_AxisX_2);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_AxisX_3);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_AxisY_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_AxisY_2);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_AxisY_3);

            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Xmin_Patch_Offset, BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress +_Float_0_Offset));
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Xmax_Patch_Offset, BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress +_Float_640_Offset));
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Ymin_Patch_Offset, BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress +_Float_0_Offset));
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Ymax_Patch_Offset, BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress +_Float_480_Offset));

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>  
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((float)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((float)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                if (_Pedal1_Enable && !_isPedal1_Pushed && ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _GameScene_Offset) == 17) //Scene 17 is in-game (not blocking axis in menus)
                {
                    bufferX = BitConverter.GetBytes(-1.0f);
                    bufferY = BitConverter.GetBytes(-1.0f);
                }

                WriteBytes(_BaseData_Address + _P1_X_Offset, bufferX);
                WriteBytes(_BaseData_Address + _P1_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    SetButtonBistable(_P1_Trigger_Offset);
                    WriteByte(_BaseData_Address + _P1_Trigger_Offset - 0x06, 0x01);   //this byte show event in TEST mode but not used in game ??
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte(_BaseData_Address + _P1_Trigger_Offset - 0x06, 0x00);   //this byte show event in TEST mode but not used in game ??

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    SetButtonBistable(_P1_Option_Offset);
                    WriteByte(_BaseData_Address + _P1_Option_Offset - 0x06, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    WriteByte(_BaseData_Address + _P1_Option_Offset - 0x06, 0x00);
                }
                //Offsreen shoot => put the gun off screen. Let side seems to work best
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    if (!_Pedal1_Enable)
                    {
                        WriteBytes(_BaseData_Address + _P1_X_Offset, BitConverter.GetBytes(-1.0f));
                        WriteBytes(_BaseData_Address + _P1_Y_Offset, BitConverter.GetBytes(-1.0f));
                    }
                }
            }
            else if (PlayerData.ID == 2 && ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _GameScene_Offset) == 17)
            {
                if (_Pedal2_Enable && !_isPedal2_Pushed && ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _GameScene_Offset) == 17)     //Scene 17 is in-game (not blocking axis in menus)
                {
                    bufferX = BitConverter.GetBytes(-1.0f);
                    bufferY = BitConverter.GetBytes(-1.0f);
                }

                WriteBytes(_BaseData_Address + _P1_X_Offset, bufferX);
                WriteBytes(_BaseData_Address + _P1_Y_Offset, bufferY);
                WriteBytes(_BaseData_Address + _P2_X_Offset, bufferX);
                WriteBytes(_BaseData_Address + _P2_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    SetButtonBistable(_P2_Trigger_Offset);
                    WriteByte(_BaseData_Address + _P2_Trigger_Offset - 0x06, 0x01);   //this byte show event in TEST mode but not used in game ??
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte(_BaseData_Address + _P2_Trigger_Offset - 0x06, 0x00);   //this byte show event in TEST mode but not used in game ??

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    SetButtonBistable(_P2_Option_Offset);
                    WriteByte(_BaseData_Address + _P2_Option_Offset - 0x06, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    WriteByte(_BaseData_Address + _P2_Option_Offset - 0x06, 0x00);
                }

                //Offsreen shoot => put the gun off screen. Let side seems to work best
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    if (!_Pedal2_Enable)
                    {
                        WriteBytes(_BaseData_Address + _P2_X_Offset, BitConverter.GetBytes(-1.0f));
                        WriteBytes(_BaseData_Address + _P2_Y_Offset, BitConverter.GetBytes(-1.0f));
                    }
                }
            }
        }

        //On this game a button event changes the byte value (we can do it by cycling between 1/0)
        private void SetButtonBistable(UInt32 ButtonOffset)
        {
            byte ButtonValue = ReadByte(_BaseData_Address + ButtonOffset);
            WriteByte(_BaseData_Address + ButtonOffset, (byte)(1 - ButtonValue));
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
                    if (s.scanCode == _P1_Start_Key)
                    {
                        SetButtonBistable(_P1_Start_Offset);
                        WriteByte(_BaseData_Address + _P1_Start_Offset - 0x06, 0x01);   //this byte show event in TEST mode but not used in game ??
                    }
                    else if (s.scanCode == _P2_Start_Key)
                    {
                        SetButtonBistable(_P2_Start_Offset);
                        WriteByte(_BaseData_Address + _P2_Start_Offset - 0x06, 0x01);   //this byte show event in TEST mode but not used in game ??
                    }
                    else if (s.scanCode == _Test_Key)
                    {
                        WriteByte(_BaseData_Address + _TEST_Offset, 0x01);   //this byte show event in TEST mode but not used in game ??
                    }
                    else if (s.scanCode == _Service_Key)
                    {
                        WriteByte(_BaseData_Address + _SERVICE_Offset, 0x01); //this byte show event in TEST mode but not used in game ??
                    }
                    else if (s.scanCode == _Credits_Key)
                    {
                        if (!_IsCreditsKeyPressed)
                        {
                            WriteByte(ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _CreditsToAdd_Ptr_Offset) + 0xE510, 0x01);
                            _IsCreditsKeyPressed = true;
                        }
                    }
                    if (s.scanCode == _Pedal1_Key && _Pedal1_Enable)
                    {
                        _isPedal1_Pushed = true;
                    }
                    else if (s.scanCode == _Pedal2_Key && _Pedal2_Enable)
                    {
                        _isPedal2_Pushed = true;
                    }
                }
                else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                {
                    if (s.scanCode == _P1_Start_Key)
                    {
                        WriteByte(_BaseData_Address + _P1_Start_Offset - 0x06, 0x00);   //this byte show event in TEST mode but not used in game ??
                    }
                    else if (s.scanCode == _P2_Start_Key)
                    {
                        WriteByte(_BaseData_Address + _P2_Start_Offset - 0x06, 0x00);   //this byte show event in TEST mode but not used in game ??
                    }
                    else if (s.scanCode == _Test_Key)
                    {
                        WriteByte(_BaseData_Address + _TEST_Offset, 0x00);   //this byte show event in TEST mode but not used in game ??
                    }
                    else if (s.scanCode == _Service_Key)
                    {
                        WriteByte(_BaseData_Address + _SERVICE_Offset, 0x00);  //this byte show event in TEST mode but not used in game ??
                    }
                    else if (s.scanCode == _Credits_Key)
                    {
                        _IsCreditsKeyPressed = false;
                    }
                    if (s.scanCode == _Pedal1_Key && _Pedal1_Enable)
                    {
                        _isPedal1_Pushed = false;
                    }
                    else if (s.scanCode == _Pedal2_Key && _Pedal2_Enable)
                    {
                        _isPedal2_Pushed = false;
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpHead, OutputId.P1_LmpHead));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpHead, OutputId.P2_LmpHead));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpFoot, OutputId.P1_LmpFoot));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpFoot, OutputId.P2_LmpFoot));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpFront, OutputId.P1_LmpFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpFront, OutputId.P2_LmpFront));            
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            SetOutputValue(OutputId.P1_LmpStart, (ReadByte(_BaseData_Address + _Outputs_Offset) >> 1) & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_BaseData_Address + _Outputs_Offset) & 0x01);
            SetOutputValue(OutputId.P1_LmpHead, (ReadByte(_BaseData_Address + _Outputs_Offset + 1) >> 1) & 0x01);
            SetOutputValue(OutputId.P2_LmpHead, ReadByte(_BaseData_Address + _Outputs_Offset + 1) & 0x01);
            SetOutputValue(OutputId.P1_LmpFoot, ReadByte(_BaseData_Address + _Outputs_Offset + 2));
            SetOutputValue(OutputId.P2_LmpFoot, ReadByte(_BaseData_Address + _Outputs_Offset + 3));
            SetOutputValue(OutputId.P1_LmpFront, ReadByte(_BaseData_Address + _Outputs_Offset + 12));
            SetOutputValue(OutputId.P2_LmpFront, ReadByte(_BaseData_Address + _Outputs_Offset + 13));


            //Custom Outputs
            int P1_Ammo = 0;
            int P2_Ammo = 0;
            int P1_Life = 0;
            int P2_Life = 0;
            int P1_Weapon = 0;
            int P2_Weapon = 0;
            //To be sure that outputs are not activated in Attract Mode, we can check the game scene ID and activate them only in gameplay 
            if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _GameScene_Offset) == 17)
            {
                //Now, players data have some information even if they are not playing (game over or not started)
                if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Player1_Playing) == 1)
                {
                    UInt32 P1_InfoAddress = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _PlayerInfo_BasePtr_Offset + 0x38C);
                    P1_Weapon = (int)ReadPtrChain(P1_InfoAddress + 0x40, new UInt32[] { 0x15C, 0x24 });

                    if (P1_Weapon == _P1_LastWeapon)
                    {
                        P1_Ammo = (int)ReadPtrChain(P1_InfoAddress + 0x40, new UInt32[] { 0x15C, (UInt32)(4 * P1_Weapon), 0xE8 });

                        //Custom Recoil
                        if (P1_Ammo < _P1_LastAmmo)
                            SetOutputValue(OutputId.P1_CtmRecoil, 1);

                        //[Clip Empty] custom Output
                        if (P1_Ammo <= 0)
                            SetOutputValue(OutputId.P1_Clip, 0);
                        else
                            SetOutputValue(OutputId.P1_Clip, 1);
                    }

                    P1_Life = (int)ReadPtr(P1_InfoAddress + 0x6C);
                    //Custom Dammage
                    if (P1_Life < _P1_LastLife)
                        SetOutputValue(OutputId.P1_Damaged, 1);
                }
                else
                {
                    SetOutputValue(OutputId.P1_Clip, 0);
                }


                //Now, players data have some information even if they are not playing (game over or not started)
                if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Player2_Playing) == 1)
                {
                    UInt32 P2_InfoAddress = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _PlayerInfo_BasePtr_Offset + 0x390);
                    P2_Weapon = (int)ReadPtrChain(P2_InfoAddress + 0x40, new UInt32[] { 0x15C, 0x24 });

                    if (P2_Weapon == _P2_LastWeapon)
                    {
                        P2_Ammo = (int)ReadPtrChain(P2_InfoAddress + 0x40, new UInt32[] { 0x15C, (UInt32)(4 * P1_Weapon), 0xE8 });

                        //Custom Recoil
                        if (P2_Ammo < _P2_LastAmmo)
                            SetOutputValue(OutputId.P2_CtmRecoil, 1);

                        //[Clip Empty] custom Output
                        if (P2_Ammo <= 0)
                            SetOutputValue(OutputId.P2_Clip, 0);
                        else
                            SetOutputValue(OutputId.P2_Clip, 1);
                    }

                    P2_Life = (int)ReadPtr(P2_InfoAddress + 0x6C);
                    //Custom Dammage
                    if (P2_Life < _P2_LastLife)
                        SetOutputValue(OutputId.P2_Damaged, 1);
                }
                else
                {
                    SetOutputValue(OutputId.P2_Clip, 0);
                }
            }
            
            _P1_LastAmmo = P1_Ammo;
            _P1_LastLife = P1_Life;
            _P1_LastWeapon = P1_Weapon;
            _P2_LastAmmo = P2_Ammo;
            _P2_LastLife = P2_Life;
            _P2_LastWeapon = P2_Weapon;

            SetOutputValue(OutputId.P1_Life, P1_Life);
            SetOutputValue(OutputId.P2_Life, P2_Life);
            SetOutputValue(OutputId.P1_Ammo, P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, P2_Ammo);
            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _CreditsTotal_Offset));
        }

        //Seems like there's a memory field to set Lamp to state 0/1/2 and another one [0->0x64] to set analog intensity
        private byte GetLampAnalogValue(UInt32 OutputLampAddress)
        {
            if (ReadByte(OutputLampAddress) != 0)
            {
                return (ReadByte(OutputLampAddress - 0x30));
            }
            else
                return 0;
        }

        #endregion
    }
}
