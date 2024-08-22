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
    class Game_KonamiCastlevania : Game
    {
        //MultiWindow process, so we can't use MAinWindowHandle
        private IntPtr _hWnd_GameWindow = IntPtr.Zero;

        /*** Game Data for Memory Hack ***/
        /*** MEMORY ADDRESSES **/
        private UInt32 _ViewportWidth_Offset = 0x0032508C;
        private UInt32 _ViewportHeight_Offset = 0x00325090;
        private UInt32 _Credits_Offset = 0x003280F8;
        private UInt32 _Buttons_CaveAddress = 0;        

        private UInt32 _CheckIOEmulated_Offset = 0x00083978;
        private UInt32 _P1_X_Offset = 0x003281A0;
        private UInt32 _P1_Y_Offset = 0x003281A4;
        private UInt32 _P2_X_Offset = 0x0032CFB4;
        private UInt32 _P2_Y_Offset = 0x0032CFB8;
        //private UInt32 _Buttons_Offset = 0x00328138; //Unused, may be used for Emulated IO
        private UInt32 _Buttons_Injection_Offset = 0x00080E84;
        private UInt32 _Buttons_Injection_Return_Offset = 0x00080E89;
        //by default, putting a credit does not produce sound, compared to SERVICE key (but credit does not go into BOOKEEPING log)
        //By doing this we can force the game to play credit sound with CREDIT KEY
        private NopStruct _Nop_CreditsSound = new NopStruct(0x00083065, 4);
        private UInt32 _CreditsSoundMod_Offset = 0x00083061;

        private HardwareScanCode _Test_Key = HardwareScanCode.DIK_9;
        private HardwareScanCode _Service_Key = HardwareScanCode.DIK_0;
        private HardwareScanCode _P1_Start_Key = HardwareScanCode.DIK_1;
        private HardwareScanCode _P2_Start_Key = HardwareScanCode.DIK_2;
        private HardwareScanCode _Credits_Key = HardwareScanCode.DIK_5;
        //Disabling adding credits while the key stays pressed
        private bool _IsCreditsKeyPressed = false;

        //Outputs
        private UInt32 _Outputs_Offset = 0x00331F24;
        private UInt32 _P1_Life_Offset = 0x00326B48;
        private UInt32 _P2_Life_Offset = 0x00326D08;
        private UInt32 _P1_Ammo_Offset = 0x00326B58;
        private UInt32 _P2_Ammo_Offset = 0x00326D18;


        /// <summary>
        /// Constructor
        /// </summary>
        public Game_KonamiCastlevania(String RomName, bool DisableInputHack, bool Verbose) 
            : base (RomName, "HCV", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Castlevania Arcade v2009-04-22 - clean dump", "8a6dd00e254df6ad68a944da8add6235");
        
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

                        //Looking for the game's window based on it's Title
                        _hWnd_GameWindow = IntPtr.Zero;
                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            // The game may start with other Windows than the main one (BepInEx console, other stuff.....) so we need to filter
                            // the displayed window according to the Title, if DemulShooter is started before the game,  to hook the correct one
                            if (FindGameWindow_Contains("CROSS") || FindGameWindow_Equals("TeknoParrot - Castlevania: The Arcade"))
                            {
                                CheckExeMd5();
                                Apply_MemoryHacks();
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
                            }
                            else
                            {
                                Logger.WriteLog("Game Window not found");
                                return;
                            } 
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

        #region Screen

        /// <summary>
        /// The process is creating 2 windows, so we need to fill the required window handle instead of the default one (not knowing which one it is)
        /// </summary>
        /// <param name="PlayerData"></param>
        /// <returns></returns>
        public override bool ClientScale(PlayerSettings PlayerData)
        {
            //Convert Screen location to Client location
            if (_TargetProcess != null)
            {
                //Window size
                Rect TotalRes = new Rect();
                Win32API.GetWindowRect(_hWnd_GameWindow, ref TotalRes);

                Logger.WriteLog("Window position (Px) = [ " + TotalRes.Left + ";" + TotalRes.Top + " ]");

                PlayerData.RIController.Computed_X = PlayerData.RIController.Computed_X - TotalRes.Left;
                PlayerData.RIController.Computed_Y = PlayerData.RIController.Computed_Y - TotalRes.Top;
                Logger.WriteLog("Onclient window position (Px) = [ " + PlayerData.RIController.Computed_X + "x" + PlayerData.RIController.Computed_Y + " ]");

            }
            return true;
        }

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

                    //Reading Resolution in game's memory
                    double dMaxX = (double)BitConverter.ToUInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _ViewportWidth_Offset, 4), 0);
                    double dMaxY = (double)BitConverter.ToUInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _ViewportHeight_Offset, 4), 0);

                    //Inverted Axis : 0 = bottom right
                    PlayerData.RIController.Computed_X = Convert.ToInt32(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt32(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));
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

        #region MemoryHack

        protected override void Apply_InputsMemoryHack()
        {
            //Replacing the check for Emulated IO so that values are generated by real board
            //+83978 -> JE -----> JNE
            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _CheckIOEmulated_Offset, 0xEB);

            //Replacing the check for Emulated SENSORS so that values are generated by real board
            //Causes LAG (input check with hardware timeout ??)
            //+83963 -> JE -----> JNE
            //WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x83963, 0xEB);

            //Forcing Sound with Credits insertion
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_CreditsSound);
            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _CreditsSoundMod_Offset, 0x29); //ORiginal code is writing in Ptr+2A, and Service is using Ptr+29

            Create_InputsDataBank();
            _Buttons_CaveAddress = _InputsDatabank_Address;

            SetHack_Buttons();
            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        private void SetHack_Buttons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            //mov esi, _Buttons_CaveAddress
            CaveMemory.Write_StrBytes("8B 35");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //mov edx, edi+48
            CaveMemory.Write_StrBytes("8B 57 48");
            //mov eax, edx
            CaveMemory.Write_StrBytes("8B C2");
            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Return_Offset);
            Logger.WriteLog("Adding Trigger CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Offset) - 5;
            List<byte> Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

        }

        #endregion

        #region Input

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>   
        public override void SendInput(PlayerSettings PlayerData)
        {
            if (!_DisableInputHack)
            {
                byte[] bufferX = BitConverter.GetBytes((float)PlayerData.RIController.Computed_X);
                byte[] bufferY = BitConverter.GetBytes((float)PlayerData.RIController.Computed_Y);

                if (PlayerData.ID == 1)
                {
                    //WriteBytes(0x7281B8, bufferX);    //If using Original Sensor procedure, but causes LAG
                    //WriteBytes(0x7281BC, bufferY);    //If using Original Sensor procedure, but causes LAG     
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);   //USed with EmulatedSensor
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);   //USed with EmulatedSensor

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        Apply_OR_ByteMask(_Buttons_CaveAddress, 0x20);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        Apply_AND_ByteMask(_Buttons_CaveAddress, 0xDF);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        Apply_OR_ByteMask(_Buttons_CaveAddress, 0x80);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        Apply_AND_ByteMask(_Buttons_CaveAddress, 0x7F);
                }

                else if (PlayerData.ID == 2)
                {
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);   //USed with EmulatedSensor
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);   //USed with EmulatedSensor

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        Apply_OR_ByteMask(_Buttons_CaveAddress, 0x40);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        Apply_AND_ByteMask(_Buttons_CaveAddress, 0xBF);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        Apply_OR_ByteMask(_Buttons_CaveAddress + 1, 0x01);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        Apply_AND_ByteMask(_Buttons_CaveAddress + 1, 0xFE);
                }
            }
        }

        /// <summary>
        /// Low-level Keyboard hook callback.
        /// This is used to replace system keys
        /// </summary>
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (!_DisableInputHack)
            {
                if (nCode >= 0)
                {
                    KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                    if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                    {
                        if (s.scanCode == _P1_Start_Key)
                        {
                            Apply_OR_ByteMask(_Buttons_CaveAddress, 0x08);
                        }
                        else if (s.scanCode == _P2_Start_Key)
                        {
                            Apply_OR_ByteMask(_Buttons_CaveAddress, 0x10);
                        }
                        else if (s.scanCode == _Test_Key)
                        {
                            Apply_OR_ByteMask(_Buttons_CaveAddress, 0x01);
                        }
                        else if (s.scanCode == _Service_Key)
                        {
                            Apply_OR_ByteMask(_Buttons_CaveAddress, 0x02);
                        }
                        else if (s.scanCode == _Credits_Key)
                        {
                            if (!_IsCreditsKeyPressed)
                            {
                                Apply_OR_ByteMask(_Buttons_CaveAddress, 0x04);
                                /*int Credits = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset, 4), 0);
                                Credits++;
                                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset, BitConverter.GetBytes(Credits));*/
                                WriteByte(0x72811c, 1);
                                _IsCreditsKeyPressed = true;
                            } 
                        }
                    }
                    else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                    {
                        if (s.scanCode == _P1_Start_Key)
                        {
                            Apply_AND_ByteMask(_Buttons_CaveAddress, 0xF7);
                        }
                        else if (s.scanCode == _P2_Start_Key)
                        {
                            Apply_AND_ByteMask(_Buttons_CaveAddress, 0xEF);
                        }
                        else if (s.scanCode == _Test_Key)
                        {
                            Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFE);
                        }
                        else if (s.scanCode == _Service_Key)
                        {
                            Apply_AND_ByteMask(_Buttons_CaveAddress, 0x0D);
                        }
                        else if (s.scanCode == _Credits_Key)
                        {
                            Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFB);
                            WriteByte(0x72811c, 0);
                            _IsCreditsKeyPressed = false;
                        }
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpCard_R, OutputId.P1_LmpCard_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpCard_G, OutputId.P1_LmpCard_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpCard_R, OutputId.P2_LmpCard_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpCard_G, OutputId.P2_LmpCard_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Whip_R, OutputId.P1_Whip_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Whip_G, OutputId.P1_Whip_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Whip_B, OutputId.P1_Whip_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Whip_R, OutputId.P2_Whip_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Whip_G, OutputId.P2_Whip_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Whip_B, OutputId.P2_Whip_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));            
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            byte bOutput = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset);
            SetOutputValue(OutputId.P1_LmpStart, GetLampAnalogValue((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset));
            SetOutputValue(OutputId.P2_LmpStart, GetLampAnalogValue((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset + 4));
            SetOutputValue(OutputId.P1_LmpCard_R, GetLampAnalogValue((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset + 8));
            SetOutputValue(OutputId.P1_LmpCard_G, GetLampAnalogValue((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset + 12));
            SetOutputValue(OutputId.P2_LmpCard_R, GetLampAnalogValue((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset + 16));
            SetOutputValue(OutputId.P2_LmpCard_G, GetLampAnalogValue((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset + 20));
            SetOutputValue(OutputId.P1_Whip_R, GetLampAnalogValue((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset + 24));
            SetOutputValue(OutputId.P1_Whip_G, GetLampAnalogValue((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset + 28));
            SetOutputValue(OutputId.P1_Whip_B, GetLampAnalogValue((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset + 32));
            SetOutputValue(OutputId.P2_Whip_R, GetLampAnalogValue((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset + 36));
            SetOutputValue(OutputId.P2_Whip_G, GetLampAnalogValue((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset + 40));
            SetOutputValue(OutputId.P2_Whip_B, GetLampAnalogValue((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset + 44));           


            //Custom Outputs
            _P1_Life = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Life_Offset, 4), 0);
            _P1_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Ammo_Offset);
            //When not playing, default value is 0x2710 (10 000)
            if (_P1_Life != 10000)
            {
                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                if (_P1_Life > 0)
                {
                    //Custom Recoil
                    if (_P1_Ammo < _P1_LastAmmo)
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (_P1_Ammo <= 0)
                        SetOutputValue(OutputId.P1_Clip, 0);
                    else
                        SetOutputValue(OutputId.P1_Clip, 1);
                }
                else
                {
                    _P1_Ammo = 0;
                }
            }
            else
            {
                SetOutputValue(OutputId.P1_Clip, 0);
                _P1_Ammo = 0;
                _P1_Life = 0;                
            }

            _P2_Life = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Life_Offset, 4), 0);
            _P2_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Ammo_Offset);
            //When not playing, default value is 0x2710 (10 000)
            if (_P2_Life != 10000)
            {
                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);

                if (_P2_Life > 0)
                {
                    //Custom Recoil
                    if (_P2_Ammo < _P2_LastAmmo)
                        SetOutputValue(OutputId.P2_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (_P2_Ammo <= 0)
                        SetOutputValue(OutputId.P2_Clip, 0);
                    else
                        SetOutputValue(OutputId.P2_Clip, 1);
                }
                else
                {
                    _P2_Ammo = 0;
                }
            }
            else
            {
                SetOutputValue(OutputId.P2_Clip, 0);
                _P2_Ammo = 0;
                _P2_Life = 0;
            }
                            
            _P1_LastAmmo = _P1_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastAmmo = _P2_Ammo;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset));
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
