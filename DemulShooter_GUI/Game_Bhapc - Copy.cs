using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Runtime.InteropServices;


namespace DemulShooter
{
    class Game_Bhapc : Game
    {
        private const string FOLDER_GAMEDATA = @"MemoryData\windows";

        /*** MEMORY ADDRESSES **/
        protected int _P1_X_Offset = 0x000000B8;
        protected int _P1_Y_Offset = 0x000000BC;
        protected int _P1_StructPtr_Offset = 0x0123A658;
        protected string _P1_Axis_Nop_Offset_1 = "0x00124814|7";
        protected string _P1_Axis_Nop_Offset_2 = "0x00124F7B|7";
        protected int _P1_Axis_Injection_Offset = 0x00124F73;
        protected int _P1_Axis_Injection_Return_Offset = 0x00124F82;

        protected Int64 _P1_StructAddress = 0;

        protected Int64 _P1_X_Address;
        protected Int64 _P1_Y_Address;
        protected Int64 _P2_X_Address;
        protected Int64 _P2_Y_Address;

        //Custom data to inject
        protected float _P1_X_Value;
        protected float _P1_Y_Value;
        protected float _P2_X_Value;
        protected float _P2_Y_Value;


        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Bhapc(string RomName, bool Verbose)
            : base()
        {
            GetScreenResolution();

            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "Buck";

            ReadGameData();

            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for Windows Game " + _RomName + " game to hook.....");
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

                        WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));

                        if (_TargetProcess_MemoryBaseAddress != null)
                        {
                            byte[] bBuffer = ReadBytes_x64((Int64)_TargetProcess_MemoryBaseAddress + _P1_StructPtr_Offset, 8);
                            _P1_StructAddress = BitConverter.ToInt64(bBuffer, 0);                           
                            if (_P1_StructAddress != 0)
                            {                                
                                _ProcessHooked = true;
                                WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
                                WriteLog("P1_StructAddress = 0x" + _P1_StructAddress.ToString("X16"));
                                ApplyKeyboardHook();
                                SetHack();
                            }
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

                    //X => [0 - ClientWidth]
                    //Y => [ClientHeight - 0]

                    Mouse.pTarget.Y = (int)TotalResY - Mouse.pTarget.Y;

                    if (Mouse.pTarget.X < 0)
                        Mouse.pTarget.X = 0;
                    if (Mouse.pTarget.Y < 0)
                        Mouse.pTarget.Y = 0;
                    if (Mouse.pTarget.X > (int)TotalResX)
                        Mouse.pTarget.X = (int)TotalResX;
                    if (Mouse.pTarget.Y > (int)TotalResY)
                        Mouse.pTarget.Y = (int)TotalResY;

                    if (Player == 1)
                    {
                        _P1_X_Value = (float)(Mouse.pTarget.X);
                        _P1_Y_Value = (float)(Mouse.pTarget.Y);
                    }
                    else if (Player == 2)
                    {
                        _P2_X_Value = (float)(Mouse.pTarget.X);
                        _P2_Y_Value = (float)(Mouse.pTarget.Y);
                    }

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

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "ReadProcessMemory")]
        public static extern bool ReadProcessMemory_x64(int hProcess, Int64 lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "WriteProcessMemory")]
        public static extern bool WriteProcessMemory_x64(int hProcess, Int64 lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

        protected Byte[] ReadBytes_x64(Int64 X64_Address, int Bytes)
        {
            byte[] Buffer = new byte[Bytes];
            int bytesRead = 0;
            if (!ReadProcessMemory_x64((int)_ProcessHandle, X64_Address, Buffer, Buffer.Length, ref bytesRead))
            {
                WriteLog("Cannot read memory at address 0x" + X64_Address.ToString("X8"));
            }
            return Buffer;
        }

        protected bool WriteByte_x64(Int64 Address, byte Value)
        {
            int bytesWritten = 0;
            Byte[] Buffer = { Value };
            if (WriteProcessMemory_x64((int)_ProcessHandle, Address, Buffer, 1, ref bytesWritten))
            {
                if (bytesWritten == 1)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
        protected bool WriteBytes_x64(Int64 Address, byte[] Buffer)
        {
            int bytesWritten = 0;
            if (WriteProcessMemory_x64((int)_ProcessHandle, Address, Buffer, Buffer.Length, ref bytesWritten))
            {
                if (bytesWritten == Buffer.Length)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        protected void SetNops_x64(Int64 BaseAddress, string OffsetAndNumber)
        {
            if (OffsetAndNumber != null)
            {
                try
                {
                    int n = int.Parse((OffsetAndNumber.Split('|'))[1]);
                    int address = int.Parse((OffsetAndNumber.Split('|'))[0].Substring(3).Trim(), NumberStyles.HexNumber);
                    for (int i = 0; i < n; i++)
                    {
                        WriteByte_x64(BaseAddress + address + i, 0x90);
                    }
                }
                catch
                {
                    WriteLog("Impossible de traiter le NOP : " + OffsetAndNumber);
                }
            }
        }

        private void SetHack()
        {
            SetNops_x64((Int64)_TargetProcess_MemoryBaseAddress, _P1_Axis_Nop_Offset_1);
            //SetNops_x64((Int64)_TargetProcess_MemoryBaseAddress, _P1_Axis_Nop_Offset_2);            
            SetHack_Data();
            SetHack_P1Axis();



            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        /*** Creating a custom memory bank to store our data ***/
        private void SetHack_Data()
        {
            //1st Codecave : storing our Axis Data
            Memory DataCaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            DataCaveMemory.Open();
            DataCaveMemory.Alloc(0x800);

            _P1_X_Address = (Int64)DataCaveMemory.CaveAddress;
            _P1_Y_Address = (Int64)DataCaveMemory.CaveAddress + 0x04;
            _P2_X_Address = (Int64)DataCaveMemory.CaveAddress + 0x10;
            _P2_Y_Address = (Int64)DataCaveMemory.CaveAddress + 0x14;

            WriteLog("Custom data will be stored at : 0x" + _P1_X_Address.ToString("X16"));
        }

        /*** Creating a custom memory bank to store our data ***/
        private void SetHack_P1Axis()
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            WriteLog("Adding P1_InGame_Y CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

            List<Byte> Buffer = new List<Byte>();
            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov rax, _P1_X_Address
            CaveMemory.Write_StrBytes("48 A1");
            byte[] b = BitConverter.GetBytes(_P1_X_Address);
            CaveMemory.Write_Bytes(b);
            //mov [rsp+000000B0], rax
            CaveMemory.Write_StrBytes("48 89 84 24 B0 00 00 00");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //mov rcx, [rsp+0x000000B0]
            CaveMemory.Write_StrBytes("48 8B 8C 24 B0 00 00 00");
            //mov [rax+0x000000B8], rcx
            CaveMemory.Write_StrBytes("48 89 88 B8 00 00 00");
            //jmp Buck.exe+_P1_Axis_Injection_Return_Offset
            CaveMemory.Write_StrBytes("FF 25 00 00 00 00");
            b = BitConverter.GetBytes((Int64)_TargetProcess_MemoryBaseAddress + _P1_Axis_Injection_Return_Offset);
            CaveMemory.Write_Bytes(b);
            WriteLog("Adding P1_Axis CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));            

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            Int64 jumpTo = 0;
            jumpTo = (Int64)CaveMemory.CaveAddress - ((Int64)_TargetProcess_MemoryBaseAddress + _P1_Axis_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            //WriteProcessMemory_x64((int)ProcessHandle, (Int64)_TargetProcess_MemoryBaseAddress + _P1_Axis_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        }

        // Mouse callback for low level hook
        /*protected override IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (UInt32)wParam == Win32.WM_MBUTTONDOWN)
            {
                //Just blocking middle clicks                
                return new IntPtr(1);
            }
            else if (nCode >= 0 && (UInt32)wParam == Win32.WM_RBUTTONDOWN)
            {
                //Just blocking right clicks => if not P1 reload play animation but does not reload
                return new IntPtr(1);
            }
            return Win32.CallNextHookEx(_MouseHookID, nCode, wParam, lParam);
        }*/

        public override void SendInput(MouseInfo mouse, int Player)
        {
            if (Player == 1)
            {
                //Setting Values in memory for the Codecave to read it
                byte[] buffer = BitConverter.GetBytes(_P1_X_Value);
                WriteBytes_x64(_P1_X_Address, buffer);
                buffer = BitConverter.GetBytes(_P1_Y_Value);
                WriteBytes_x64(_P1_Y_Address, buffer);                
                
                /*//Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    SendKeyDown(_P1_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    SendKeyUp(_P1_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    if (P1_Next_Dir.Equals("left"))
                        SendKeyDown(_P1_Left_DIK);
                    else
                        SendKeyDown(_P1_Right_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    if (P1_Next_Dir.Equals("left"))
                    {
                        SendKeyUp(_P1_Left_DIK);
                        P1_Next_Dir = "right";
                    }
                    else
                    {
                        SendKeyUp(_P1_Right_DIK);
                        P1_Next_Dir = "left";
                    }
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    SendKeyDown(_P1_Reload_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    SendKeyUp(_P1_Reload_DIK);
                }*/
            }
            else if (Player == 2)
            {
                
                /*
                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    SendKeyDown(_P2_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    SendKeyUp(_P2_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    if (P2_Next_Dir.Equals("left"))
                        SendKeyDown(_P2_Left_DIK);
                    else
                        SendKeyDown(_P2_Right_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    if (P2_Next_Dir.Equals("left"))
                    {
                        SendKeyUp(_P2_Left_DIK);
                        P2_Next_Dir = "right";
                    }
                    else
                    {
                        SendKeyUp(_P2_Right_DIK);
                        P2_Next_Dir = "left";
                    }
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    SendKeyDown(_P2_Reload_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    SendKeyUp(_P2_Reload_DIK);
                }*/
            }
        }

        #endregion

        // Keyboard will be used to use Grenade
        protected override IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                Win32.KBDLLHOOKSTRUCT s = (Win32.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32.WM_KEYDOWN)
                {
                    switch (s.scanCode)
                    {
                        case 0x22: //G
                            {
                                try
                                {
                                    byte[] bBuffer = ReadBytes_x64((Int64)_TargetProcess_MemoryBaseAddress + 0x01285E70, 8);
                                    Int64 MouseDeltaX = BitConverter.ToInt64(bBuffer, 0) + 8;
                                    WriteLog("MouseDeltaX = 0x" + MouseDeltaX.ToString("X16"));
                                    bBuffer = ReadBytes_x64(MouseDeltaX, 4);
                                    //WriteLog(bBuffer[0].ToString("X2") + ", " + bBuffer[1].ToString("X2") + ", " + bBuffer[2].ToString("X2") + ", " + bBuffer[3].ToString("X2"));
                                    float f = BitConverter.ToSingle(bBuffer, 0);
                                    WriteLog("MouseDeltaX value = " + f.ToString());
                                    f = f + 10.0f;
                                    bBuffer = BitConverter.GetBytes(f);
                                    WriteBytes_x64(MouseDeltaX, bBuffer);

                                } catch (Exception ex)
                                {
                                    WriteLog("Convertion error : " + ex.Message.ToString());
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
                /*else if ((UInt32)wParam == Win32.WM_KEYUP)
                {
                    switch (s.scanCode)
                    {
                        case _P1_Grenade_ScanCode:
                            {
                                Apply_AND_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFB);
                            } break;
                        default:
                            break;
                    }
                }*/
            }
            return Win32.CallNextHookEx(_KeyboardHookID, nCode, wParam, lParam);
        }
    }
}
