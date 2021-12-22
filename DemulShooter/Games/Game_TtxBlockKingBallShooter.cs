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
        private UInt32 _OutputDamage_Injection_Offset = 0x00241490;
        private UInt32 _OutputDamage_Injection_Return_Offset = 0x00241497;

        private UInt32 _CtmDamage_CaveAddress;

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
                    double dMinX = 0.0;
                    double dMaxX = 65535.0;
                    double dMinY = 0.0;
                    double dMaxY = 65535.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    //In that game trim is needed before, for min/max values of UInt32, so we use X,Y variable at first.
                    //Otherwise, in windowed mode, there are issues with negative or over 65535 values if cursor is out of window
                    //when we convert it to UInt32 in next step
                    double X = 0.0;
                    double Y = 0.0;

                    if (_ForcedXratio != 0)
                    {
                        Logger.WriteLog("Forcing X Ratio to = " + _ForcedXratio.ToString());
                        double ViewportHeight = TotalResY;
                        double ViewportWidth = TotalResY * _ForcedXratio;
                        double SideBarsWidth = (TotalResX - ViewportWidth) / 2;
                        Logger.WriteLog("Game Viewport size (Px) = [ " + ((int)ViewportWidth).ToString() + "x" + ((int)ViewportHeight).ToString() + " ]");
                        Logger.WriteLog("SideBars Width (px) = " + ((int)SideBarsWidth).ToString());
                        dRangeX = dRangeX + (SideBarsWidth * dRangeX / TotalResX) * 2;
                        //X = Math.Round(dRangeX * PlayerData.RIController.Computed_X / TotalResX) - (SideBarsWidth * dMaxX / TotalResX);
                        X = Math.Round((dRangeX / TotalResX) * (PlayerData.RIController.Computed_X - SideBarsWidth));
                    }
                    else
                        X = Math.Round(dRangeX * PlayerData.RIController.Computed_X / TotalResX);
                    
                    Y = Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY);
                    if (X < 0)
                        X = 0;
                    if (Y < 0)
                        Y = 0;
                    if (X > dMaxX)
                        X = dMaxX;
                    if (Y > dMaxY)
                        Y = dMaxY;

                    //Now that we have trimmed 0x0000-0xFFFF values, we can convert to Int16
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

            CreateDataBank();
            SetHack_CustomDamageOutput();

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }
        
        /// <summary>
        /// 1st Memory created to store custom output data
        /// </summary>
        private void CreateDataBank()
        {
            Codecave InputMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            InputMemory.Open();
            InputMemory.Alloc(0x800);
            _CtmDamage_CaveAddress = InputMemory.CaveAddress;
            Logger.WriteLog("Custom Output data will be stored at : 0x" + _CtmDamage_CaveAddress.ToString("X8"));
        }

        /// <summary>
        /// This codecave will intercept the game's procedure to decrease timer due to a hit (= damaged)
        /// </summary>
        private void SetHack_CustomDamageOutput()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov [_CtmDamage_CaveAddress], 1
            CaveMemory.Write_StrBytes("C7 05");
            byte[] b = BitConverter.GetBytes(_CtmDamage_CaveAddress);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("01 00 00 00");
            //fld dword ptr [esp+04]
            CaveMemory.Write_StrBytes("D9 44 24 04");
            //fadd dword ptr [ecx+0C]
            CaveMemory.Write_StrBytes("D8 41 0C");           
            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _OutputDamage_Injection_Return_Offset);

            Logger.WriteLog("Adding Custom Damage output CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _OutputDamage_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _OutputDamage_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
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
            //Need to separate Attract from game
            //_Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            //_Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00546B43));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00546B49));
            SetOutputValue(OutputId.LmpCannonBtn, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00546B45));
            SetOutputValue(OutputId.LmpCannon_R, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00546B4B));
            SetOutputValue(OutputId.LmpCannon_G, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00546B47));
            SetOutputValue(OutputId.LmpCannon_B, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00546B4D));
            
            
            //Custom Outputs:
            //[Damaged] custom Output
            /*if (ReadByte(_CtmDamage_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P1_Damaged, 1);
                SetOutputValue(OutputId.P2_Damaged, 1);
                WriteByte(_CtmDamage_CaveAddress, 0x00);
            }*/
            
            //Credits
            UInt32 CreditsPtr = BitConverter.ToUInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x5469E0, 4), 0);
            SetOutputValue(OutputId.Credits, ReadByte(CreditsPtr + 4));
        }

        #endregion
    }
}
