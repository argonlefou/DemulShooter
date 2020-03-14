﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

namespace DemulShooter
{
    class Game_CxbxVcop3_2 : Game
    {
        /*** MEMORY ADDRESSES **/

        private int _P1_X_Offset = 0x3578F4;
        private int _P1_Y_Offset = 0x3578F8;
        private int _P2_X_Offset = 0x3579CC;
        private int _P2_Y_Offset = 0x3579D0;
        private int _Buttons_Injection_Offset_P1 = 0x6C9B0;
        private int _Buttons_Injection_Return_Offset_P1 = 0x6C9B8;
        private int _P1_ButtonsStatus = 0;
        private int _P1_BulletTimeStatus = 0;
        private int _P2_ButtonsStatus = 0;
        private int _P2_BulletTimeStatus = 0;

        //Offset to NOP axis instructions
        private const int _X1_OFFSET = 0x0006A3D8;
        private const int _X2_OFFSET = 0x0006A403;
        private const int _Y1_OFFSET = 0x0006A41E;
        private const int _Y2_OFFSET = 0x0006A3F2;
        private const String _X1_NOP_OFFSET = "0x0006A3D8|3";
        private const String _X2_NOP_OFFSET = "0x0006A403|3";
        private const String _Y1_NOP_OFFSET = "0x0006A41E|3";
        private const String _Y2_NOP_OFFSET = "0x0006A3F2|3";

        protected byte _P1_Start_Key = 0x02; //1
        protected byte _P2_Start_Key = 0x03; //2
        
        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_CxbxVcop3_2(string RomName, bool Verbose)
            : base()
        {
            GetScreenResolution();

            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "Cxbx";

            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for Chihiro " + _RomName + " game to hook.....");
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

        /*public override void GetScreenResolution()
        {
            WriteLog("using alternative way of Screen size");
            IntPtr hDesktop = Win32.GetDesktopWindow();
            Win32.Rect DesktopRect = new Win32.Rect();
            Win32.GetWindowRect(hDesktop, ref DesktopRect);
            _screenWidth = DesktopRect.Right;
            _screenHeight = DesktopRect.Bottom;
        }*/

        

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

                    Win32.GetWindowRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    WriteLog("Game client window location (Px) = [ " + TotalRes.Left + " ; " + TotalRes.Top + " ]");

                    //X => [-320 ; +320] = 640
                    //Y => [240; -240] = 480
                    double dMaxX = 640.0;
                    double dMaxY = 480.0;

                    Mouse.pTarget.X = Convert.ToInt32(Math.Round(dMaxX * Mouse.pTarget.X / TotalResX) - dMaxX / 2);
                    Mouse.pTarget.Y = Convert.ToInt32((Math.Round(dMaxY * Mouse.pTarget.Y / TotalResY) - dMaxY / 2) * -1);
                    if (Mouse.pTarget.X < -320)
                        Mouse.pTarget.X = -320;
                    if (Mouse.pTarget.Y < -240)
                        Mouse.pTarget.Y = -240;
                    if (Mouse.pTarget.X > 320)
                        Mouse.pTarget.X = 320;
                    if (Mouse.pTarget.Y > 240)
                        Mouse.pTarget.Y = 240;

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
            //This is HEX code for the instrution we're testing to see which process is the good one to hook
            byte[] bTest = new byte[] { 0x8B, 0xE8 };

            WriteLog("Testing instruction at 0x" + ((int)_TargetProcess_MemoryBaseAddress + _X1_OFFSET - 2).ToString("X8"));
            byte[] b = ReadBytes((int)_TargetProcess_MemoryBaseAddress + _X1_OFFSET - 2, 2);
            WriteLog("Waiting for : 0x" + bTest[0].ToString("X2") + ", 0x" + bTest[1].ToString("X2"));
            WriteLog("Read values : 0x" + b[0].ToString("X2") + ", 0x" + b[1].ToString("X2"));
            if (b[0] == bTest[0] && b[1] == bTest[1])
            {
                WriteLog("Correct process for code injection");
                WriteLog("WindowHandle = " + _TargetProcess.MainWindowHandle.ToString());

                //Creating data bank
                //Codecave :
                Memory CaveMemoryInput = new Memory(_TargetProcess, _TargetProcess_MemoryBaseAddress);
                CaveMemoryInput.Open();
                CaveMemoryInput.Alloc(0x800);
                _P1_ButtonsStatus = CaveMemoryInput.CaveAddress;
                _P2_ButtonsStatus = CaveMemoryInput.CaveAddress + 0x08;
                _P1_BulletTimeStatus = CaveMemoryInput.CaveAddress + 0x10;
                _P2_BulletTimeStatus = CaveMemoryInput.CaveAddress + 0x18;
                WriteLog("Custom data will be stored at : 0x" + _P1_ButtonsStatus.ToString("X8"));

                //For P1 :
                //modify [edx+0x21] so that it corresponds to our values
                //EDX + 0x21 ==> 0x10 (START)
                //EDX + 0x22 ==>
                //EDX + 0x23 ==> 0xFF (TRIGGER)
                //EDX + 0x24 ==> 0xFF (RELOAD)
                //EDX + 0x29 ==> 0xFF (Bullet Time)
                //[ESP + 0x10] (after our push) contains controller ID (0->4)
                Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess_MemoryBaseAddress);
                CaveMemory.Open();
                CaveMemory.Alloc(0x800);
                //mov edx, [esp+04]
                CaveMemory.Write_StrBytes("8B 54 24 04");
                //push eax
                CaveMemory.Write_StrBytes("50");
                //cmp [esp+0x10], 0
                CaveMemory.Write_StrBytes("83 7C 24 10 00");
                //je Player1
                CaveMemory.Write_StrBytes("0F 84 10 00 00 00");
                //cmp [esp + 0x10], 1
                CaveMemory.Write_StrBytes("83 7C 24 10 01");
                //je Player2
                CaveMemory.Write_StrBytes("0F 84 15 00 00 00");
                //jmp originalcode
                CaveMemory.Write_StrBytes("E9 1B 00 00 00");
                //Player1 :
                //mov eax, [_P1_ButtonsStatus]
                b = BitConverter.GetBytes(_P1_ButtonsStatus);
                CaveMemory.Write_StrBytes("A1");
                CaveMemory.Write_Bytes(b);
                //mov ecx, [_P1_BulletTimeStatus]
                b = BitConverter.GetBytes(_P1_BulletTimeStatus);
                CaveMemory.Write_StrBytes("8B 0D");
                CaveMemory.Write_Bytes(b);
                //jmp originalcode
                CaveMemory.Write_StrBytes("E9 0B 00 00 00");
                //Player2:
                //mov eax, [_P2_ButtonsStatus]
                b = BitConverter.GetBytes(_P2_ButtonsStatus);
                CaveMemory.Write_StrBytes("A1");
                CaveMemory.Write_Bytes(b);
                //mov ecx, [_P2_BulletTimeStatus]
                b = BitConverter.GetBytes(_P2_BulletTimeStatus);
                CaveMemory.Write_StrBytes("8B 0D");
                CaveMemory.Write_Bytes(b);
                //originalcode:
                //mov [edx+0x21], eax
                CaveMemory.Write_StrBytes("89 42 21");
                //pop eax
                CaveMemory.Write_StrBytes("58");
                //mov [edx+0x29], ecx
                CaveMemory.Write_StrBytes("89 4A 29");
                //mov cx, [edx+0x21]
                CaveMemory.Write_StrBytes("66 8B 4A 21");
                //return
                CaveMemory.Write_jmp((int)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Return_Offset_P1);

                WriteLog("Adding Trigger CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

                //Code injection
                IntPtr ProcessHandle = _TargetProcess.Handle;
                int bytesWritten = 0;
                int jumpTo = 0;
                jumpTo = CaveMemory.CaveAddress - ((int)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Offset_P1) - 5;
                List<byte> Buffer = new List<byte>();
                Buffer.Add(0xE9);
                Buffer.AddRange(BitConverter.GetBytes(jumpTo));
                Buffer.Add(0x90);
                Buffer.Add(0x90);
                Buffer.Add(0x90);
                Win32.WriteProcessMemory((int)ProcessHandle, (int)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Offset_P1, Buffer.ToArray(), Buffer.Count, ref bytesWritten);

                //Noping Axis procedures
                SetNops((int)_TargetProcess_MemoryBaseAddress, _X1_NOP_OFFSET);
                SetNops((int)_TargetProcess_MemoryBaseAddress, _X2_NOP_OFFSET);
                SetNops((int)_TargetProcess_MemoryBaseAddress, _Y1_NOP_OFFSET);
                SetNops((int)_TargetProcess_MemoryBaseAddress, _Y2_NOP_OFFSET);

                ApplyKeyboardHook();

                _ProcessHooked = true;
            }
        }
        // Keyboard callback used for pedal-mode
        protected override IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                Win32.KBDLLHOOKSTRUCT s = (Win32.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32.WM_KEYDOWN)
                {
                    if (s.scanCode == _P1_Start_Key)
                        Apply_OR_ByteMask(_P1_ButtonsStatus, 0x10);
                    else if (s.scanCode == _P2_Start_Key)
                        Apply_OR_ByteMask(_P2_ButtonsStatus, 0x10);
                }
                else if ((UInt32)wParam == Win32.WM_KEYUP)
                {
                    if (s.scanCode == _P1_Start_Key)
                        Apply_AND_ByteMask(_P1_ButtonsStatus, 0x00);
                    else if (s.scanCode == _P2_Start_Key)
                        Apply_AND_ByteMask(_P2_ButtonsStatus, 0x00);
                }
            }
            return Win32.CallNextHookEx(_KeyboardHookID, nCode, wParam, lParam);
        }


        public override void SendInput(MouseInfo mouse, int Player)
        {
            byte[] bufferX = BitConverter.GetBytes(mouse.pTarget.X);
            byte[] bufferY = BitConverter.GetBytes(mouse.pTarget.Y);

            if (Player == 1)
            {
                //Write Axis
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);

                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_P1_ButtonsStatus + 2, 0xFF);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_P1_ButtonsStatus + 2, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_P1_BulletTimeStatus, 0xFF);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_P1_BulletTimeStatus, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_P1_ButtonsStatus + 3, 0xFF);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_P1_ButtonsStatus + 3, 0x00);
                }
            }
            else if (Player == 2)
            {
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);

                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_P2_ButtonsStatus + 2, 0xFF);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_P2_ButtonsStatus + 2, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_P2_BulletTimeStatus, 0xFF);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_P2_BulletTimeStatus, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_P2_ButtonsStatus + 3, 0xFF);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_P2_ButtonsStatus + 3, 0x00);
                }
            }
        }

        #endregion
    }
}