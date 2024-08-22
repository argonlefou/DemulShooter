using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.RawInput;

namespace DemulShooter
{
    class Game_Model2Rchase2 : Game
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
        private UInt32 _Outputs_Offset = 0x00174CF0;
        private UInt32 _CreditsPtr_Offset = 0x001AA71C;
        private UInt32 _GameInfoPtr_Offset = 0x001AA730;
        private UInt32 _GameInfo_BaseAddress;
        
        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_Model2Rchase2(String RomName, bool DisableInputHack, bool Verbose)
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
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                CheckExeMd5();
                                Apply_MemoryHacks();
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
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X => [0xBF -> 0x3E] 
                    //X and Y => [0xBF -> 0x40]
                    double dMaxX = 130.0;
                    double dMaxY = 128.0;

                    PlayerData.RIController.Computed_X = Convert.ToInt16(dMaxX - Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX)) + 0x3E;
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(dMaxY - Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY)) + 0x40;

                    if (PlayerData.RIController.Computed_X < 0x40)
                        PlayerData.RIController.Computed_X = 0x40;
                    if (PlayerData.RIController.Computed_Y < 0x40)
                        PlayerData.RIController.Computed_Y = 0x40;
                    if (PlayerData.RIController.Computed_X > 0xBF)
                        PlayerData.RIController.Computed_X = 0xBF;
                    if (PlayerData.RIController.Computed_Y > 0xBF)
                        PlayerData.RIController.Computed_Y = 0xBF;

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
        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            //At runtime, the game is reading 4 bytes with players axis values in that order :
            _P2_X_CaveAddress = _InputsDatabank_Address;
            _P1_X_CaveAddress = _InputsDatabank_Address + 1;
            _P2_Y_CaveAddress = _InputsDatabank_Address + 2;
            _P1_Y_CaveAddress = _InputsDatabank_Address + 3;
            //And for buttons
            _Buttons_CaveAddress = _InputsDatabank_Address + 8;

            //Buttons : The game is reading the Byte containing Buttons info. Replacing the real address with our own
            byte[] b = BitConverter.GetBytes(_Buttons_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Offset + 1, b);

            //Axis : Same Thing
            b = BitConverter.GetBytes(_P2_X_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Axis_Injection_Offset + 2, b);

            //Initial values
            WriteBytes(_P2_X_CaveAddress, new byte[] { 0x70, 0x89, 0x6F, 0x6F });
            WriteByte(_Buttons_CaveAddress, 0xFF);

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
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFE);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x01);
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
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFD);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x02);
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
            byte bOutput = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset);
            SetOutputValue(OutputId.P1_LmpStart, bOutput >> 2 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, bOutput >> 3 & 0x01);

            //Custom Outputs
            UInt32 iTemp = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _GameInfoPtr_Offset);
            _GameInfo_BaseAddress = ReadPtr(iTemp + 0x550);

            //Player status in game :
            //[1] : P1 Playing
            //[2] : P2 Playing
            //[0] : Game Over
            int P1_Status = ReadByte(_GameInfo_BaseAddress + 0x00501D88) & 0x01;
            int P2_Status = ReadByte(_GameInfo_BaseAddress + 0x00501D88) >> 1 & 0x01;
            _P1_Life = ReadByte(_GameInfo_BaseAddress + 0x00501E70);
            _P2_Life = ReadByte(_GameInfo_BaseAddress + 0x00501E72);

            if (P1_Status != 0)
            {
                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);
                _P1_LastLife = _P1_Life;
            }
            else
            {
                _P1_Life = 0;
                _P1_LastLife = 0;
            }

            if (P2_Status != 0)
            {
                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);
                _P2_LastLife = _P2_Life;
            }
            else
            {
                _P2_Life = 0;
                _P2_LastLife = 0;
            }

            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
            SetOutputValue(OutputId.Credits, ReadByte(ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _CreditsPtr_Offset) + 0x22));
        }

        #endregion

    }
}
