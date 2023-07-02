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
    class Game_TtxGungun2 : Game
    {
        /*** MEMORY ADDRESSES **/
        private UInt32 _P1_X_Offset = 0x002B8104;
        private UInt32 _P1_Y_Offset = 0x002B8106;
        private UInt32 _P1_Trigger_Offset = 0x002B8108;
        private UInt32 _P1_Out_Offset = 0x002B8102;
        private UInt32 _P2_X_Offset = 0x002B810E;
        private UInt32 _P2_Y_Offset = 0x002B8110;
        private UInt32 _P2_Trigger_Offset = 0x002B8112;
        private UInt32 _P2_Out_Offset = 0x002B810C;
        private NopStruct _Nop_P1_X = new NopStruct(0x00106513, 7);
        private NopStruct _Nop_P1_Y = new NopStruct(0x00106538, 7);
        private NopStruct _Nop_P1_Trigger_1 = new NopStruct(0x001065E3, 7);
        private NopStruct _Nop_P1_Trigger_2 = new NopStruct(0x001065DA, 7);
        private NopStruct _Nop_P1_Out_1 = new NopStruct(0x0010658C, 7);
        private NopStruct _Nop_P1_Out_2 = new NopStruct(0x0010659C, 7);
        private NopStruct _Nop_P2_X = new NopStruct(0x00106555, 7);
        private NopStruct _Nop_P2_Y = new NopStruct(0x0010657A, 7);
        private NopStruct _Nop_P2_Trigger_1 = new NopStruct(0x001065FC, 7);
        private NopStruct _Nop_P2_Trigger_2 = new NopStruct(0x001065F3, 7);
        private NopStruct _Nop_P2_Out_1 = new NopStruct(0x001065C3, 7);
        private NopStruct _Nop_P2_Out_2 = new NopStruct(0x001065B3, 7);

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_TtxGungun2(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "game", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Music Gun Gun 2 v1.01 JPN - Original", "590677da06b758728a1dd607cbf032de");
            _KnownMd5Prints.Add("Music Gun Gun 2 v1.01 JPN - For JConfig", "57fb4970df6ef979d7ffc044e6161e84");

            _tProcess.Start();
            Logger.WriteLog("Waiting for Taito Type X " + _RomName + " game to hook.....");
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
                            if (!_DisableInputHack)
                                SetHack();
                            else
                                Logger.WriteLog("Input Hack disabled");
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

                    //X => [0, 3FFO] = 16368
                    //Y => [0, 3FF0] = 16368
                    double dMaxX = 16368.0;
                    double dMaxY = 16368.0;

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

        #region Memory Hack

        /// <summary>
        /// Genuine Hack, just blocking Axis and Triggers input to replace them.
        /// </summary>
        private void SetHack()
        {
            //NOPing proc
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_X);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_Y);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_Trigger_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_Trigger_2);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_Out_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_Out_2);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_X);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_Y);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_Trigger_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_Trigger_2);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_Out_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_Out_2);

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }        

        #endregion

        #region Inputs

        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0) 
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0) 
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x00);                

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0) 
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Out_Offset, 0x01);
                    System.Threading.Thread.Sleep(20);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Out_Offset, 0x00);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x00);
                }
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Out_Offset, 0x01);
                    System.Threading.Thread.Sleep(20);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Out_Offset, 0x00);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x00);
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun, OutputId.P1_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun, OutputId.P2_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpLBtn, OutputId.LmpLBtn));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpMBtn, OutputId.LmpMBtn));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpRBtn, OutputId.LmpRBtn));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            byte Outputs1 = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002BC311);
            byte Outputs2 = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002BC312);
            byte[] buffer = ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x002DBE08, 4);
            int iCredits = ReadByte(BitConverter.ToUInt32(buffer, 0) + 0x18);            

            SetOutputValue(OutputId.P1_LmpStart, Outputs2 >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, Outputs2 >> 6 & 0x01);
            SetOutputValue(OutputId.P1_LmpGun, Outputs2 >> 5 & 0x01);
            SetOutputValue(OutputId.P2_LmpGun, Outputs2 >> 4 & 0x01);
            SetOutputValue(OutputId.LmpLBtn, Outputs1 >> 5 & 0x01);
            SetOutputValue(OutputId.LmpMBtn, Outputs1 >> 6 & 0x01);
            SetOutputValue(OutputId.LmpRBtn, Outputs1 >> 4 & 0x01);
            SetOutputValue(OutputId.Credits, iCredits);
        }

        #endregion
    }
}
