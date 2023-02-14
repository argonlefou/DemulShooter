using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_Model2Hotd : Game
    {     
        /*** MEMORY ADDRESSES **/
        private UInt32 _Buttons_Injection_Offset = 0x000C88F0;
        private UInt32 _Reload_Injection_Offset = 0x000C89B8;
        private NopStruct _Nop_P1_X = new NopStruct(0x000CAB13, 6);
        private NopStruct _Nop_P1_Y = new NopStruct(0x000CAB02, 6);
        private NopStruct _Nop_P2_X = new NopStruct(0x000CAB37, 6);
        private NopStruct _Nop_P2_Y = new NopStruct(0x000CAB3D, 6);
        private UInt32 _P1_X_Offset = 0x00174CF8;
        private UInt32 _P1_Y_Offset = 0x00174CFC;
        private UInt32 _P2_X_Offset = 0x00174D00;
        private UInt32 _P2_Y_Offset = 0x00174D04;
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _Reload_CaveAddress;

        //Outputs
        private UInt32 _CreditsPtr_Offset = 0x001AA71C;
        private UInt32 _Outputs_Offset = 0x000174CF0;
        private UInt32 _GameInfoPtr_Offset = 0x001AA730;
        private UInt32 _GameInfo_BaseAddress;
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
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_Model2Hotd(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "EMULATOR", DisableInputHack, Verbose)
        {            
            _KnownMd5Prints.Add("Model2Emulator 1.1a", "26bd488f9a391dcac1c5099014aa1c9e");
            _KnownMd5Prints.Add("Model2Emulator 1.1a multicpu", "ac59ce7cfb95d6d639c0f0d1afba1192");

            _tProcess.Start();
            Logger.WriteLog("Waiting for Model2 " + _RomName + " game to hook.....");
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
                    Process[] processes = Process.GetProcesses();
                    foreach (Process p in processes)
                    {
                        if (p.ProcessName.Equals("EMULATOR") || p.ProcessName.Equals("emulator_multicpu"))
                        {
                            _Target_Process_Name = p.ProcessName;
                            _TargetProcess = p;
                            _ProcessHandle = _TargetProcess.Handle;
                            _TargetProcess_MemoryBaseAddress = _TargetProcess.MainModule.BaseAddress;

                            if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                            {
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                CheckExeMd5();
                                if (!_DisableInputHack)
                                    SetHack();
                                else
                                    Logger.WriteLog("Input Hack disabled");
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
                                break;
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
                    //Model2 Window size
                    Rect TotalRes = new Rect();
                    //Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    Win32API.GetWindowRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    //Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X => [0x0000 - > 0x01EF]
                    //Y => [0x0000 - > 0x017F]
                    double dMaxX = 496.0;
                    double dMaxY = 384.0;

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
        /// Creating a custom memory bank to store our data
        /// </summary>
        private void SetHack()
        {
            CreateDataBank();

            //Buttons : The game is reading the Byte containing Buttons info. Replacing the real address with our own
            byte[] b = BitConverter.GetBytes(_Buttons_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Offset + 1, b);
            //Same thing for Reload flag
            b = BitConverter.GetBytes(_Reload_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Reload_Injection_Offset + 1, b);
            
            //Axis
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_X);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_Y);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_X);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_Y);

            //Initial values
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, BitConverter.GetBytes((Int32)0xA5));
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, BitConverter.GetBytes((Int32)192));
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, BitConverter.GetBytes((Int32)0x14A));
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, BitConverter.GetBytes((Int32)192));
            WriteByte(_Buttons_CaveAddress, 0xFF);
            WriteByte(_Reload_CaveAddress, 0x03);

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        private void CreateDataBank()
        {
            Codecave CaveMemoryInput = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemoryInput.Open();
            CaveMemoryInput.Alloc(0x800);
            //Buttons
            _Buttons_CaveAddress = CaveMemoryInput.CaveAddress + 8;
            _Reload_CaveAddress = CaveMemoryInput.CaveAddress + 9;

            Logger.WriteLog("Custom data will be stored at : 0x" + _Buttons_CaveAddress.ToString("X8"));
        }

        #endregion   
     
        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((Int32)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((Int32)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFE);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x01);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    Apply_AND_ByteMask(_Reload_CaveAddress, 0xFE);
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFE);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x01);
                    Apply_OR_ByteMask(_Reload_CaveAddress, 0x01);
                }
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFD);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x02);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    Apply_AND_ByteMask(_Reload_CaveAddress, 0xFD);
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFD);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x02);
                    Apply_OR_ByteMask(_Reload_CaveAddress, 0x02);                    
                }
            }
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
            byte bOutput = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset);
            SetOutputValue(OutputId.P1_LmpStart, bOutput >> 2 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, bOutput >> 3 & 0x01);

            //Custom Outputs
            UInt32 iTemp = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _GameInfoPtr_Offset);
            _GameInfo_BaseAddress = ReadPtr(iTemp + 0x630);

            //Player status in game :
            //[5] : Playing
            //[4] : Continue Screen
            //[9] : Game Over
            int P1_Status = ReadByte(_GameInfo_BaseAddress + 0x0051EC04);
            int P2_Status = ReadByte(_GameInfo_BaseAddress + 0x0051ECD0);
            _P1_Life = ReadByte(_GameInfo_BaseAddress + 0x0051EC10);
            _P2_Life = ReadByte(_GameInfo_BaseAddress + 0x0051ECDC);
            _P1_Ammo = ReadByte(_GameInfo_BaseAddress + 0x0051EC41);
            _P2_Ammo = ReadByte(_GameInfo_BaseAddress + 0x0051ED0D);

            if (P1_Status == 5)
            {
                //Custom Recoil
                if (_P1_Ammo < _P1_LastAmmo)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Clip Empty] custom Output
                if (_P1_Ammo <= 0)
                    SetOutputValue(OutputId.P1_Clip, 0);
                else
                    SetOutputValue(OutputId.P1_Clip, 1);

                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                _P1_LastAmmo = _P1_Ammo;
                _P1_LastLife = _P1_Life;
            }
            else
            {
                SetOutputValue(OutputId.P1_Clip, 0);
                _P1_Ammo = 0;
                _P1_LastAmmo = 0;
                _P1_Life = 0;
                _P1_LastLife = 0;
            }

            if (P2_Status == 5)
            {
                //Custom Recoil
                if (_P2_Ammo < _P2_LastAmmo)
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                //[Clip Empty] custom Output
                if (_P2_Ammo <= 0)
                    SetOutputValue(OutputId.P2_Clip, 0);
                else
                    SetOutputValue(OutputId.P2_Clip, 1);

                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);

                _P2_LastAmmo = _P2_Ammo;
                _P2_LastLife = _P2_Life;
            }
            else
            {
                SetOutputValue(OutputId.P2_Clip, 0);
                _P2_Ammo = 0;
                _P2_LastAmmo = 0;
                _P2_Life = 0;
                _P2_LastLife = 0;
            }

            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.Credits, ReadByte(ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _CreditsPtr_Offset) + 0x5E));
        }

        #endregion
    }
}
