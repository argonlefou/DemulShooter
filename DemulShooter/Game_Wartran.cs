using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace DemulShooter
{
    class Game_Wartran : Game
    {
        /** All hardcoded for now, game is not playable **/
        /** Only for TEST menu and debug **/

        /*** MEMORY ADDRESSES **/
        protected int _P1_X_Offset = 0x1630504;
        protected int _P1_Y_Offset = 0x1630505;
        protected int _P2_X_Offset = 0x1630506;
        protected int _P2_Y_Offset = 0x1630507;
        protected int _OutOfScreen_Offset = 0x1630508;
        
        protected string _Buttons_NOP_Offset = "0x00079797|5";

        protected int _Buttons_Address = 0x014994F4;
        private enum ControlsButton
        {
            Test,
            Coin1,
            Coin2,
            Service,
            NA,
            P1_Start,
            P1_Trigger,
            P2_Start,
            P2_Trigger,
            P3_Start,
            P3_Trigger,
            P4_Start,
            P4_Trigger,
        };

        private const byte _P1_Start_ScanCode = 0x02;  //[1]
        private const byte _P2_Start_ScanCode = 0x03;  //[2]
        private const byte _P3_Start_ScanCode = 0x04;  //[3]
        private const byte _P4_Start_ScanCode = 0x05;  //[4]
        private const byte _Coin1_ScanCode = 0x06;     //[5]
        private const byte _Coin2_ScanCode = 0x07;     //[6]
        private const byte _Service_ScanCode = 0x0A;   //[9]
        private const byte _Test_ScanCode = 0x0B;      //[0]

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Wartran(string RomName, bool Verbose) 
            : base ()
        {
            GetScreenResolution();
            
            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "game";

            ReadGameData();
            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for Global VR " + _RomName + " game to hook.....");
        }

        /// <summary>
        /// Timer event when looking for Process (auto-Hook and auto-close)
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

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            _ProcessHooked = true;
                            WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            SetHack();
                            ApplyKeyboardHook();
                        }
                    }
                }
                catch
                {
                    WriteLog("Error trying to hook " + _Target_Process_Name + ".exe");
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
                    WriteLog(_Target_Process_Name + ".exe closed");
                    Environment.Exit(0);
                }
            }
        }

        #region Screen        

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

                    //X => [00-FF] = 255
                    //Y => [00-FF] = 255
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;

                    //Inverted axis : origin in lower-right
                    Mouse.pTarget.X = Convert.ToInt32(dMaxX - Math.Round(dMaxX * Mouse.pTarget.X / TotalResX));
                    Mouse.pTarget.Y = Convert.ToInt32(dMaxY - Math.Round(dMaxY * Mouse.pTarget.Y / TotalResY)); 
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
            SetNops((int)_TargetProcess_MemoryBaseAddress, _Buttons_NOP_Offset);
        }

        // Keyboard callback used to bind keyboard keys to start and other buttons
        protected override IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                Win32.KBDLLHOOKSTRUCT s = (Win32.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32.WM_KEYDOWN)
                {
                    switch (s.scanCode)
                    {
                        case _P1_Start_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.P1_Start, 0x01);
                            }break;
                        case _P2_Start_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.P2_Start, 0x01);
                            } break;
                        case _P3_Start_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.P3_Start, 0x01);
                            } break;
                        case _P4_Start_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.P4_Start, 0x01);
                            } break;
                        case _Coin1_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.Coin1, 0x01);
                            } break;
                        case _Coin2_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.Coin2, 0x01);
                            } break;
                        case _Service_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.Service, 0x01);
                            } break;
                        case _Test_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.Test, 0x01);
                            } break;
                        default:
                            break;
                    }  
                }
                else if ((UInt32)wParam == Win32.WM_KEYUP)
                {
                    switch (s.scanCode)
                    {
                        case _P1_Start_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.P1_Start, 0x00);
                            } break;
                        case _P2_Start_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.P2_Start, 0x00);
                            } break;
                        case _P3_Start_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.P3_Start, 0x00);
                            } break;
                        case _P4_Start_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.P4_Start, 0x00);
                            } break;
                        case _Coin1_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.Coin1, 0x00);
                            } break;
                        case _Coin2_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.Coin2, 0x00);
                            } break;
                        case _Service_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.Service, 0x00);
                            } break;
                        case _Test_ScanCode:
                            {
                                WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.Test, 0x00);
                            } break;
                        default:
                            break;
                    }
                }
            }
            return Win32.CallNextHookEx(_KeyboardHookID, nCode, wParam, lParam);
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            if (Player == 1)
            {
                //Write Axis
                WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, (byte)(mouse.pTarget.X & 0xFF));
                WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, (byte)(mouse.pTarget.Y & 0xFF));

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.P1_Trigger, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.P1_Trigger, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask((int)_TargetProcess_MemoryBaseAddress + _OutOfScreen_Offset, 0x80);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    Apply_AND_ByteMask((int)_TargetProcess_MemoryBaseAddress + _OutOfScreen_Offset, 0x7F);
                }
            }
            else if (Player == 2)
            {
                //Write Axis
                WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, (byte)(mouse.pTarget.X & 0xFF));
                WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, (byte)(mouse.pTarget.Y & 0xFF));

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.P2_Trigger, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _Buttons_Address + (int)ControlsButton.P2_Trigger, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask((int)_TargetProcess_MemoryBaseAddress + _OutOfScreen_Offset, 0x40);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    Apply_AND_ByteMask((int)_TargetProcess_MemoryBaseAddress + _OutOfScreen_Offset, 0xBF);
                }
            }
        }

        #endregion

    }
}
