using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DemulShooter
{
    class Game_NuLuigiMansion : Game
    {
        //Input structure constants
        private const UInt64 INPUT_STRUCT_PTR_OFFSET = 0x004F6CB8;
        private const int P1_X_OFFSET   = 0x2C;
        private const int P1_Y_OFFSET   = 0x30;
        private const int P1_BTN_OFFSET = 0x34;
        private const int P2_X_OFFSET   = 0x40;
        private const int P2_Y_OFFSET   = 0x44;
        private const int P2_BTN_OFFSET = 0x48;
        private UInt64 _Input_Struct_Address = 0;

        //NOP for Guns in game
        private const string P1_X_NOP_OFFSET = "0x00017EB8|4";
        private const string P1_Y_NOP_OFFSET = "0x00017EBC|4";
        private const string P2_X_NOP_OFFSET = "0x00017F1D|4";
        private const string P2_Y_NOP_OFFSET = "0x00017F21|4";

        //Codecave injection : button handling procedure
        //The current value is 8 bytes less than the actual procedure because of X64 JMP 
        //This one needs 14 Bytes so we are overwriting X and Y procedures (4 Bytes each) placed just before the BTN procedure
        //Andso, there will be need to NOP X and Y procedure
        private const UInt64 P1_BUTTONS_INJECTION_OFFSET = 0x00017EB8; //exact procedure is 0x00017EC0;
        private const UInt64 P1_BUTTONS_INJECTION_RETURN_OFFSET = 0x00017EC8;
        private const UInt64 P2_BUTTONS_INJECTION_OFFSET = 0x00017F1D; //exact procedure is 0x00017F25;
        private const UInt64 P2_BUTTONS_INJECTION_RETURN_OFFSET = 0x00017F2D;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_NuLuigiMansion(string RomName, bool Verbose)
            : base()
        {
            GetScreenResolution();

            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "vacuum";
            _KnownMd5Prints.Add("VACUUM.EXE Teknoparrot dump", "8ddfab1cd2140670d9437738c9c331c8");

            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for SEGA Nu " + _RomName + " game to hook.....");
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
                            WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            Int64 test = _TargetProcess_MemoryBaseAddress.ToInt64() + (Int64)INPUT_STRUCT_PTR_OFFSET;
                            WriteLog(test.ToString("X16"));
                            byte[] bTampon = ReadBytesX64((IntPtr)test, 16);
                            for (int i = 0; i < 16; i++)
                            {
                                WriteLog(bTampon[i].ToString("X2"));
                            }
                            UInt64 iTampon = BitConverter.ToUInt64(bTampon, 0);
                            WriteLog(iTampon.ToString("X16"));
                            bTampon = ReadBytesX64((IntPtr)iTampon, 16);  
                            _Input_Struct_Address = BitConverter.ToUInt64(bTampon, 0);
                            if (_Input_Struct_Address != 0)
                            {
                                _ProcessHooked = true;
                                WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                WriteLog("Input Struct Address = 0x" + _Input_Struct_Address.ToString("X12"));
                                ChecExeMd5();
                                SetHack();
                            }
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

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// </summary>
        public override bool GameScale(MouseInfo Mouse, int Player)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    //Demul Window size
                    Win32.Rect TotalRes = new Win32.Rect();
                    Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");
                    //X => [0 - 1920]
                    //Y => [0 - 1080]
                    double dMaxX = 1920.0;
                    double dMaxY = 1080.0;

                    Mouse.pTarget.X = Convert.ToInt16(Math.Round(dMaxX * Mouse.pTarget.X / TotalResX));
                    Mouse.pTarget.Y = Convert.ToInt16(Math.Round(dMaxY * Mouse.pTarget.Y / TotalResY));
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
            SetHackP1Buttons();
            SetHackP2Buttons();
           
            /*SetNopsX64(_TargetProcess_MemoryBaseAddress, P1_X_NOP_OFFSET);
            SetNopsX64(_TargetProcess_MemoryBaseAddress, P1_Y_NOP_OFFSET);
            SetNopsX64(_TargetProcess_MemoryBaseAddress, P2_X_NOP_OFFSET);
            SetNopsX64(_TargetProcess_MemoryBaseAddress, P2_Y_NOP_OFFSET);*/

            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }
                
        /// <summary>
        /// Start and Gun buttons are on the same byte, so we can't simply NOP,to keep Teknoparrot Start button working
        /// </summary>
        private void SetHackP1Buttons()
        {
            MemoryX64 CaveMemory = new MemoryX64(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //and r10d, 0x1000
            CaveMemory.Write_StrBytes("41 81 E2 00 10 00 00");
            //and [rax+34], 0xFFFFEFFF
            CaveMemory.Write_StrBytes("81 60 34 FF EF FF FF");
            //or [rax+34], r10d
            CaveMemory.Write_StrBytes("44 09 50 34");
            //mov [rax+38], r11d
            CaveMemory.Write_StrBytes("44 89 58 38");
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + P1_BUTTONS_INJECTION_RETURN_OFFSET);

            WriteLog("Adding P1 Buttons Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Injection de code
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer = new List<byte>();
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + P1_BUTTONS_INJECTION_OFFSET - 8), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten); 
        }

        /// <summary>
        /// Start and Gun buttons are on the same byte, so we can't simply NOP,to keep Teknoparrot Start button working
        /// </summary>
        private void SetHackP2Buttons()
        {
            MemoryX64 CaveMemory = new MemoryX64(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //and r10d, 0x1000
            CaveMemory.Write_StrBytes("41 81 E2 00 10 00 00");
            //and [rax+48], 0xFFFFEFFF
            CaveMemory.Write_StrBytes("81 60 48 FF EF FF FF");
            //or [rax+48], r10d
            CaveMemory.Write_StrBytes("44 09 50 48");
            //mov [rax+4c], r11d
            CaveMemory.Write_StrBytes("44 89 58 4c");
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + P2_BUTTONS_INJECTION_RETURN_OFFSET);

            WriteLog("Adding P2 Buttons Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Injection de code
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer = new List<byte>();
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + P2_BUTTONS_INJECTION_OFFSET - 8), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);  
        }


        public override void SendInput(MouseInfo mouse, int Player)
        {
            if (Player == 1)
            {
                //Write Axis
                WriteBytesX64((IntPtr)(_Input_Struct_Address + P1_X_OFFSET), BitConverter.GetBytes(mouse.pTarget.X));
                WriteBytesX64((IntPtr)(_Input_Struct_Address + P1_Y_OFFSET), BitConverter.GetBytes(mouse.pTarget.Y));

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMaskX64((IntPtr)(_Input_Struct_Address + P1_BTN_OFFSET + 1), 0x20);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    Apply_AND_ByteMaskX64((IntPtr)(_Input_Struct_Address + P1_BTN_OFFSET + 1), 0xDF);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Apply_OR_ByteMaskX64((IntPtr)(_Input_Struct_Address + P1_BTN_OFFSET + 1), 0x40);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Apply_AND_ByteMaskX64((IntPtr)(_Input_Struct_Address + P1_BTN_OFFSET + 1), 0xBF);
                }
            }
            else if (Player == 2)
            {
                //Write Axis
                WriteBytesX64((IntPtr)(_Input_Struct_Address + P2_X_OFFSET), BitConverter.GetBytes(mouse.pTarget.X));
                WriteBytesX64((IntPtr)(_Input_Struct_Address + P2_Y_OFFSET), BitConverter.GetBytes(mouse.pTarget.Y));

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMaskX64((IntPtr)(_Input_Struct_Address + P2_BTN_OFFSET + 1), 0x20);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    Apply_AND_ByteMaskX64((IntPtr)(_Input_Struct_Address + P2_BTN_OFFSET + 1), 0xDF);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Apply_OR_ByteMaskX64((IntPtr)(_Input_Struct_Address + P2_BTN_OFFSET + 1), 0x40);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Apply_AND_ByteMaskX64((IntPtr)(_Input_Struct_Address + P2_BTN_OFFSET + 1), 0xBF);
                }
            }
        }

        #endregion
    }
}
