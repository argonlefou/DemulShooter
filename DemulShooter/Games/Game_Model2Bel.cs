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
    class Game_Model2Bel : Game
    {     
        /*** MEMORY ADDRESSES **/
        private UInt32 _Axis_Injection_Offset = 0x000C8937;
        private UInt32 _Buttons_Injection_Offset = 0x000C88F0;
        private UInt32 _P1_X_CaveAddress;
        private UInt32 _P1_Y_CaveAddress;
        private UInt32 _P2_X_CaveAddress;
        private UInt32 _P2_Y_CaveAddress;
        private UInt32 _Buttons_CaveAddress;
   
        //Outputs
        //private UInt32 _OutputsPtr_Offset = 0x001AA730;
        private UInt32 _PlayerStructPtr_Offset = 0x001AA8F4;
        private UInt32 _Outputs_BaseAddress;
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
        public Game_Model2Bel(String RomName, double _ForcedXratio, bool DisableInputHack, bool Verbose)
            : base(RomName, "emulator", _ForcedXratio, DisableInputHack, Verbose)
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

                    //X and Y => [0x00 - > 0xFE]
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;

                    PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(dMaxY - Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));

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

        private void SetHack()
        {
            CreateDataBank();

            //Buttons : The game is reading the Byte containing Buttons info. Replacing the real address with our own
            byte[] b = BitConverter.GetBytes(_Buttons_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Offset + 1, b);
            
            //Axis : Same Thing
            b = BitConverter.GetBytes(_P1_X_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Axis_Injection_Offset + 2, b);

            //Initial values
            WriteBytes(_P1_X_CaveAddress, new byte[] { 0x55, 0xAA, 0x7F, 0x7F });
            WriteByte(_Buttons_CaveAddress, 0xFF);

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Creating a custom memory bank to store our data
        /// </summary>
        private void CreateDataBank()
        {
            Codecave CaveMemoryInput = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemoryInput.Open();
            CaveMemoryInput.Alloc(0x800);
            //At runtime, the game is reading 4 bytes with players axis values in that order :
            _P1_X_CaveAddress = CaveMemoryInput.CaveAddress;
            _P2_X_CaveAddress = CaveMemoryInput.CaveAddress + 1;
            _P1_Y_CaveAddress = CaveMemoryInput.CaveAddress + 2;
            _P2_Y_CaveAddress = CaveMemoryInput.CaveAddress + 3;
            //And for buttons
            _Buttons_CaveAddress = CaveMemoryInput.CaveAddress + 8;

            Logger.WriteLog("Custom data will be stored at : 0x" + _P1_X_CaveAddress.ToString("X8"));
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
                WriteByte(_P1_X_CaveAddress, bufferX[0]);
                WriteByte(_P1_Y_CaveAddress, bufferY[0]);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFE);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x01);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xEF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x10);
            }
            else if (PlayerData.ID == 2)
            {
                WriteByte(_P2_X_CaveAddress, bufferX[0]);
                WriteByte(_P2_Y_CaveAddress, bufferY[0]);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFD);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x02);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xDF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x20);
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : Is activated for every bullet fired
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
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
            /*SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_Address) & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Outputs_Address) >> 1 & 0x01);
            SetOutputValue(OutputId.P1_GunMotor, ReadByte(_Outputs_Address) >> 2 & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, ReadByte(_Outputs_Address) >> 3 & 0x01);*/

            //custom Outputs  
            UInt32 iTemp = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _PlayerStructPtr_Offset);
            _Outputs_BaseAddress = ReadPtr(iTemp + 0x540);
            _P1_Ammo = BitConverter.ToInt16(ReadBytes(_Outputs_BaseAddress + 0x005B4954, 2), 0);
            if (_P1_Ammo <= 0)
                _P1_Ammo = 0;
            _P2_Ammo = BitConverter.ToInt16(ReadBytes(_Outputs_BaseAddress + 0x005B49D8, 2), 0);
            if (_P2_Ammo <= 0)
                _P2_Ammo = 0;
            _P1_Life = BitConverter.ToUInt16(ReadBytes(_Outputs_BaseAddress + 0x005B4996, 2), 0);
            _P2_Life = BitConverter.ToUInt16(ReadBytes(_Outputs_BaseAddress + 0x005B4A1E, 2), 0);

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

            _P1_LastAmmo = _P1_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastAmmo = _P2_Ammo;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.Credits, ReadByte(_Outputs_BaseAddress + 0x005A87F0));
        }

        #endregion
    }
}
