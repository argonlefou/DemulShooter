using System;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MemoryX64;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooterX64
{
    class Game_Es3Lla : Game
    {
        private UInt64 _P1_X_Address;
        private UInt64 _P1_Y_Address;
        private UInt64 _P2_X_Address;
        private UInt64 _P2_Y_Address;
        private NopStruct _Nop_Axis = new NopStruct(0x000475E3, 2);

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Es3Lla(string RomName, bool DisableInputHack, bool Verbose): base(RomName, "DomeShooterGame-Win64-Shipping", DisableInputHack, Verbose)
        {
            _tProcess.Start();
            Logger.WriteLog("Waiting for ES3 " + _RomName + " game to hook.....");
        }

        /// <summary>
        ///  Timer event when looking for Process (auto-Hook and auto-close)
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

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero && _TargetProcess.MainWindowHandle != IntPtr.Zero)
                        {
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            if (_DisableInputHack)
                                SetHack();
                            else
                                Logger.WriteLog("Input Hack disabled");
                            _ProcessHooked = true;                            
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Error trying to hook " + _Target_Process_Name + ".exe");
                    Logger.WriteLog(ex.Message.ToString());
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

                    //X => [0-1600] = 1600
                    //Y => [0-1800] = 1800
                    double dMaxX = 1600.0;
                    double dMaxY = 1800.0;

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

        private void SetHack()
        {
            SetNops(_TargetProcess_MemoryBaseAddress, _Nop_Axis);
            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        private void GetP1AxisAddress()
        {
            byte[] b = ReadBytes((IntPtr)(_TargetProcess_MemoryBaseAddress.ToInt64() + 0x16C7080), 16);
            UInt64 i = BitConverter.ToUInt64(b, 0);
            
            b = ReadBytes((IntPtr)(i + 0xC8), 16);
            i = BitConverter.ToUInt64(b, 0);
            
            b = ReadBytes((IntPtr)(i + 0x558), 16);
            i = BitConverter.ToUInt64(b, 0);
            
            _P1_X_Address = i + 0x2C0;
            _P1_Y_Address = i + 0x2C4;

            _P2_X_Address = _P1_X_Address + 0x700;
            _P2_Y_Address = _P1_X_Address + 0x700;

            //Logger.WriteLog(_P1_X_Address.ToString("X16"));
            //Logger.WriteLog(_P1_Y_Address.ToString("X16"));
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
            GetP1AxisAddress();                

            if (PlayerData.ID == 1)
            {
                //Write Axis
                WriteBytes((IntPtr)_P1_X_Address, bufferX);
                WriteBytes((IntPtr)_P1_Y_Address, bufferY);

                //Inputs
                /*if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P1_BTN_Offset, 0x10);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    Apply_AND_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P1_BTN_Offset, 0xEF);
                }
                if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P1_BTN_Offset, 0x20);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Apply_AND_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P1_BTN_Offset, 0xDF);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P1_BTN_Offset, 0x40);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    Apply_AND_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P1_BTN_Offset, 0xBF);
                }*/
            }
            else if (PlayerData.ID == 2)
            {
                //Write Axis
                WriteBytes((IntPtr)_P2_X_Address, bufferX);
                WriteBytes((IntPtr)_P2_Y_Address, bufferY);

                //Inputs
                /*if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P2_BTN_Offset, 0x10);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    Apply_AND_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P2_BTN_Offset, 0xEF);
                }
                if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P2_BTN_Offset, 0x20);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Apply_AND_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P2_BTN_Offset, 0xDF);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P2_BTN_Offset, 0x40);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    Apply_AND_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P2_BTN_Offset, 0xBF);
                }*/
            }
        }

        #endregion
    }
}
