using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
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
        protected int _P1_MouseDeltaX_Ptr_Offset = 0x01285E70;
        protected string _P1_Axis_Nop_Offset_1 = "0x00124814|7";
        protected string _P1_Axis_Nop_Offset_2 = "0x00124F7B|7";

        protected Int64 _P1_StructAddress = 0;
        protected Int64 _P1_MouseDeltaX_Address = 0;
        
        //Custom data to inject
        protected float _P1_X_Value;
        protected float _P1_Y_Value;
        protected float _P2_X_Value;
        protected float _P2_Y_Value;

        //Even when changing coordinates, it's not working in-game as long as ONE of the native RAWINPUT axis delta is not modified
        //So we will modify one of them with some custom delta value (value itself doesn't matter, it's just to change it)
        protected float _P1_LastX;
        protected float _P1_DeltaX;

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
                            bBuffer = ReadBytes_x64((Int64)_TargetProcess_MemoryBaseAddress + _P1_MouseDeltaX_Ptr_Offset, 8);
                            _P1_MouseDeltaX_Address = BitConverter.ToInt64(bBuffer, 0) + 8;

                            if (_P1_StructAddress != 0 && _P1_MouseDeltaX_Address != 0)
                            {                                
                                _ProcessHooked = true;
                                WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
                                WriteLog("P1_StructAddress = 0x" + _P1_StructAddress.ToString("X16"));
                                WriteLog("P1_MouseDeltaXAddress = 0x" + _P1_MouseDeltaX_Address.ToString("X16"));
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

                    WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X => [0 -> ClientWidth]
                    //Y => [ClientHeight -> 0]

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
                        _P1_DeltaX = _P1_X_Value - _P1_LastX;
                        _P1_LastX = _P1_X_Value;
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

        /*** I aded here some existing function, and rewrote them for 64bits address handling ***/

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
            SetNops_x64((Int64)_TargetProcess_MemoryBaseAddress, _P1_Axis_Nop_Offset_2);            

            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }
        
        public override void SendInput(MouseInfo mouse, int Player)
        {
            if (Player == 1)
            { 
                //Setting Values in memory for the Codecave to read it
                byte[] buffer = BitConverter.GetBytes(_P1_X_Value);
                WriteBytes_x64((Int64)_P1_StructAddress + _P1_X_Offset, buffer);
                buffer = BitConverter.GetBytes(_P1_Y_Value);
                WriteBytes_x64((Int64)_P1_StructAddress + _P1_Y_Offset, buffer);
                //Modifying native RAWINPUT mouse delta handling so that the game accepts our values
                buffer = BitConverter.GetBytes(_P1_DeltaX);
                WriteBytes_x64(_P1_MouseDeltaX_Address, buffer);


                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Win32.SendMessage(_TargetProcess.MainWindowHandle, 0x0204, IntPtr.Zero, IntPtr.Zero);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Win32.SendMessage(_TargetProcess.MainWindowHandle, 0x0205, IntPtr.Zero, IntPtr.Zero);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    
                }
            }
            else if (Player == 2)
            {               
                
                
            }
        }

        #endregion
                
    }
}
