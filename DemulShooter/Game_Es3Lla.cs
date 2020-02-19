using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace DemulShooter
{
    class Game_Es3Lla : Game
    {
        private UInt64 _P1_X_Address;
        private UInt64 _P1_Y_Address;
        private UInt64 _P2_X_Address;
        private UInt64 _P2_Y_Address;
        private string _AXIS_NOP_Offset = "0x000475E3|2";

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Es3Lla(string RomName, bool Verbose)
            : base()
        {
            GetScreenResolution();

            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "DomeShooterGame-Win64-Shipping";

            ReadGameData();
            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for ES3 " + _RomName + " game to hook.....");
        }

    /// <summary>
        /// Timer event when looking for Demul Process (auto-Hook and auto-close)
        /// </summary>
        private void tProcess_Tick(Object Sender, EventArgs e)
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
                            _ProcessHooked = true;
                            WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            SetHack();
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLog("Error trying to hook " + _Target_Process_Name + ".exe");
                    WriteLog(ex.Message.ToString());
                }
            }
            else
            {
                Process[] processes = Process.GetProcessesByName(_Target_Process_Name);
                if (processes.Length <= 0)
                {
                    _ProcessHooked = false;
                    RemoveMouseHook();
                    _TargetProcess = null;
                    _ProcessHandle = IntPtr.Zero;
                    _TargetProcess_MemoryBaseAddress = IntPtr.Zero;
                    WriteLog(_Target_Process_Name + ".exe closed");
                    Environment.Exit(0);
                }
            }
        }

        #region Screen

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// </summary>
        public override bool GameScale(MouseInfo Mouse, int Player)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    //Window size
                    Win32.Rect TotalRes = new Win32.Rect();
                    Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X => [0-1600] = 1600
                    //Y => [0-1800] = 1800
                    double dMaxX = 1600.0;
                    double dMaxY = 1800.0;

                    Mouse.pTarget.X = Convert.ToInt32(Math.Round(dMaxX * Mouse.pTarget.X / TotalResX));
                    Mouse.pTarget.Y = Convert.ToInt32(Math.Round(dMaxY * Mouse.pTarget.Y / TotalResY));
                    if (Mouse.pTarget.X < 0)
                        Mouse.pTarget.X = 0;
                    if (Mouse.pTarget.Y < 0)
                        Mouse.pTarget.Y = 0;
                    if (Mouse.pTarget.X > (int)dMaxX)
                        Mouse.pTarget.X = (int)dMaxX;
                    if (Mouse.pTarget.Y > (int)dMaxY)
                        Mouse.pTarget.Y = (int)dMaxY;
                    return true;
                }
                catch (Exception ex)
                {
                    WriteLog("Error scaling mouse coordonates to GameFormat : " + ex.Message.ToString());
                }
            }
            return false;
        }

        #endregion

        #region MemoryHack

        private void SetHack()
        {
            SetNopsX64(_TargetProcess_MemoryBaseAddress, _AXIS_NOP_Offset);

            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        private void GetP1AxisAddress()
        {
            byte[] b = ReadBytesX64((IntPtr)(_TargetProcess_MemoryBaseAddress.ToInt64() + (Int64)0x16C7080), 16);
            UInt64 i = BitConverter.ToUInt64(b, 0);
            
            b = ReadBytesX64((IntPtr)(i + (Int64)0xC8), 16);
            i = BitConverter.ToUInt64(b, 0);
            
            b = ReadBytesX64((IntPtr)(i + (Int64)0x558), 16);
            i = BitConverter.ToUInt64(b, 0);
            
            _P1_X_Address = i + 0x2C0;
            _P1_Y_Address = i + 0x2C4;

            _P2_X_Address = _P1_X_Address + 0x700;
            _P2_Y_Address = _P1_X_Address + 0x700;

            //WriteLog(_P1_X_Address.ToString("X16"));
            //WriteLog(_P1_Y_Address.ToString("X16"));
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            byte[] bufferX = { (byte)(mouse.pTarget.X & 0xFF), (byte)(mouse.pTarget.X >> 8) };
            byte[] bufferY = { (byte)(mouse.pTarget.Y & 0xFF), (byte)(mouse.pTarget.Y >> 8) };
            GetP1AxisAddress();                

            if (Player == 1)
            {
                //Write Axis
                WriteBytesX64((IntPtr)_P1_X_Address, bufferX);
                WriteBytesX64((IntPtr)_P1_Y_Address, bufferY);

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
            else if (Player == 2)
            {
                //Write Axis
                WriteBytesX64((IntPtr)_P2_X_Address, bufferX);
                WriteBytesX64((IntPtr)_P2_Y_Address, bufferY);

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
