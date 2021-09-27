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
    class Game_TtxBlockKingBallShooter : Game
    {
        /*** MEMORY ADDRESSES **/
        private UInt32 _AxisX_Offset = 0x005473D0;
        private UInt32 _AxisY_Offset = 0x005473D4;
        private UInt32 _ScreenTouched_Offset = 0x00546A19;
        private NopStruct _Nop_AxisX = new NopStruct(0x002597FF, 6);
        private NopStruct _Nop_AxisY = new NopStruct(0x002597E9, 5);
        private NopStruct _Nop_BtnTriggerReset = new NopStruct(0x00259852, 6);
        //private NopStruct _Nop_BtnTrigger = new NopStruct(0x06368C806, 4);

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_TtxBlockKingBallShooter(String RomName, double _ForcedXratio, bool Verbose)
            : base(RomName, "game", _ForcedXratio, Verbose)
        {
            _KnownMd5Prints.Add("Block King Ball Shooter v1.05 - Original", "42b7ab17909d13ff096f2b08ece6bf2a");
            _KnownMd5Prints.Add("Block King Ball Shooter v1.05 - For JConfig", "7e2fae81627c05a836033918e01046c6");

            _tProcess.Start();
            Logger.WriteLog("Waiting for TTX " + _RomName + " game to hook.....");
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
                            SetHack();
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
                    //Window size
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");


                    //X => [0 ; 0xFFFF] => 65536
                    //Y => [0 ; 0xFFFF] => 65536
                    double dMaxX = 65535.0;
                    double dMaxY = 65535.0;

                    //In that game trim is needed before, for min/max values of UInt32
                    //Otherwise, in windowed mode, there are issues with negative or over 65535 values if cursor is out of window
                    //when we convert it to UInt32 in next step
                    double X = Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX);
                    double Y = Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY);
                    if (X < 0)
                        X = 0;
                    if (Y < 0)
                        Y = 0;
                    if (X > dMaxX)
                        X = dMaxX;
                    if (Y > dMaxY)
                        Y = dMaxY;
                    Logger.WriteLog("Raw Game Format  (Dec) = [ " + X.ToString() + " ; " + Y.ToString() + " ]");

                    PlayerData.RIController.Computed_X = Convert.ToUInt16(X);
                    PlayerData.RIController.Computed_Y = Convert.ToUInt16(Y);

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
            //Blocking input from Jconfig
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_AxisX);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_AxisY);

            //Blocking "Screen touched clear to 0" loop
            //SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_BtnTriggerReset);

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
            byte[] bufferX = BitConverter.GetBytes((UInt16)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((UInt16)PlayerData.RIController.Computed_Y);          
                           
            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _AxisX_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _AxisY_Offset, bufferY);
                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _ScreenTouched_Offset, 0x01);
                System.Threading.Thread.Sleep(20);
                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _ScreenTouched_Offset, 0x00);
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
            _Outputs.Add(new GameOutput(OutputDesciption.LmpCannonBtn, OutputId.LmpCannonBtn));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpCannon_R, OutputId.LmpCannon_R));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpCannon_G, OutputId.LmpCannon_G));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpCannon_B, OutputId.LmpCannon_B));
            
            /*
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));*/
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            /*SetOutputValue(OutputId.P1_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00546A2B) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00546A31) >> 7 & 0x01);
            SetOutputValue(OutputId.LmpCannonBtn, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00546A2D) >> 7 & 0x01);
            SetOutputValue(OutputId.LmpCannon_R, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00546A33) >> 7 & 0x01);
            SetOutputValue(OutputId.LmpCannon_G, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00546A2F) >> 7 & 0x01);
            SetOutputValue(OutputId.LmpCannon_B, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00546A35) >> 7 & 0x01);*/
            
            /*
            //Custom Outputs:
            //[Damaged] custom Output
            if (ReadByte(_P1_DamageStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P1_Damaged, 1);
                WriteByte(_P1_DamageStatus_CaveAddress, 0x00);
            }
            if (ReadByte(_P2_DamageStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P2_Damaged, 1);
                WriteByte(_P2_DamageStatus_CaveAddress, 0x00);
            }
            //[Recoil] custom Output
            if (ReadByte(_P1_RecoilStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                WriteByte(_P1_RecoilStatus_CaveAddress, 0x00);
            }
            if (ReadByte(_P2_RecoilStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte(_P2_RecoilStatus_CaveAddress, 0x00);
            }*/
            
            //Credits
            UInt32 CreditsPtr = BitConverter.ToUInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x5469E0, 4), 0);
            SetOutputValue(OutputId.Credits, ReadByte(CreditsPtr + 4));
        }

        #endregion
    }
}
