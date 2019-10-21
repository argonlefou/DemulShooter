using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace DemulShooter
{
    class Game_RwTransformers : Game
    {
        /*** MEMORY ADDRESSES **/
        protected int _P1_X_Offset = 0x0098CCBB;
        protected int _P1_Y_Offset = 0x0098CCB5;
        protected int _P1_Buttons_Offset = 0x0098CC9C;
        protected int _P2_X_Offset = 0x0098CCC7;
        protected int _P2_Y_Offset = 0x0098CCC1;
        protected int _P2_Buttons_Offset = 0x0098CCA8;
        protected int _Buttons_Injection_Offset = 0x00419FDB;
        protected int _Buttons_Injection_Return_Offset = 0x00419FE0;

        protected string _Axis_NOP_Offset_1 = "0x0041A059|3";
        protected string _Axis_NOP_Offset_2 = "0x0041A05F|4";

        int _P1_XMin = 0;
        int _P1_XMax = 0;
        int _P1_YMin = 0;
        int _P1_YMax = 0;
        int _P2_XMin = 0;
        int _P2_XMax = 0;
        int _P2_YMin = 0;
        int _P2_YMax = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_RwTransformers(string RomName, bool Verbose) 
            : base ()
        {
            GetScreenResolution();

            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "TF_Gun_R_Ring_dumped";
            _KnownMd5Prints.Add("Transformers Final dumped", "7e11f7e78ed566a277edba1a8aab0749");

            ReadGameData();
            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for RingWide " + _RomName + " game to hook.....");
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

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            byte[] bTampon = ReadBytes((int)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, 2);
                            int P1_X = BitConverter.ToInt16(bTampon,0);

                            if (P1_X != 0)
                            {
                                //Reading player 1 and 2 axis boundaries
                                _P1_XMin = ReadByte((int)_TargetProcess_MemoryBaseAddress + 0x009BB890);
                                _P1_XMax = ReadByte((int)_TargetProcess_MemoryBaseAddress + 0x009BB891);
                                _P1_YMin = ReadByte((int)_TargetProcess_MemoryBaseAddress + 0x009BB892);
                                _P1_YMax = ReadByte((int)_TargetProcess_MemoryBaseAddress + 0x009BB893);
                                _P2_XMin = ReadByte((int)_TargetProcess_MemoryBaseAddress + 0x009BB898);
                                _P2_XMax = ReadByte((int)_TargetProcess_MemoryBaseAddress + 0x009BB899);
                                _P2_YMin = ReadByte((int)_TargetProcess_MemoryBaseAddress + 0x009BB89A);
                                _P2_YMax = ReadByte((int)_TargetProcess_MemoryBaseAddress + 0x009BB89B);

                                _ProcessHooked = true;
                                
                                WriteLog("P1 Axis boundaries : X=" + _P1_XMin.ToString() + "-" + _P1_XMax.ToString() + "; Y=" + _P1_YMin.ToString() + "-" + _P1_YMax.ToString());
                                WriteLog("P2 Axis boundaries : X=" + _P2_XMin.ToString() + "-" + _P2_XMax.ToString() + "; Y=" + _P2_YMin.ToString() + "-" + _P2_YMax.ToString());
                                WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
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

        public override bool GameScale(MouseInfo Mouse, int Player)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    Win32.Rect TotalRes = new Win32.Rect();
                    Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //Player 1 and 2 axis limits are different
                    //We can't access the TEST menu so it may be a different calibration for each one so we have to adapt:
                    int dMinX = 0;
                    int dMaxX = 0;
                    int dMinY = 0;
                    int dMaxY = 0;
                    double DeltaX = 0.0;
                    double DeltaY = 0.0;
                    

                    if (Player == 1)
                    {
                        //X => [76-215] = 140
                        //Y => [99-202] = 104
                        //Axes inversés : 0 = Bas et Droite   
                        dMinX = _P1_XMin;
                        dMaxX = _P1_XMax;
                        dMinY = _P1_YMin;
                        dMaxY = _P1_YMax;
                        DeltaX = _P1_XMax - _P1_XMin + 1;
                        DeltaY = _P1_YMax - _P1_YMin + 1;
                    }
                    else if (Player == 2)
                    {
                        //X => [55-197] = 143
                        //Y => [96-205] = 110
                        //Axes inversés : 0 = Bas et Droite
                        dMinX = _P2_XMin;
                        dMaxX = _P2_XMax;
                        dMinY = _P2_YMin;
                        dMaxY = _P2_YMax;
                        DeltaX = _P2_XMax - _P2_XMin + 1;
                        DeltaY = _P2_YMax - _P2_YMin + 1;
                    }

                    Mouse.pTarget.X = Convert.ToInt32(DeltaX - Math.Round(DeltaX * Mouse.pTarget.X / TotalResX)) + dMinX;
                    Mouse.pTarget.Y = Convert.ToInt32(DeltaY - Math.Round(DeltaY * Mouse.pTarget.Y / TotalResY)) + dMinY;
                    if (Mouse.pTarget.X < dMinX)
                        Mouse.pTarget.X = dMinX;
                    if (Mouse.pTarget.Y < dMinY)
                        Mouse.pTarget.Y = dMinY;
                    if (Mouse.pTarget.X > dMaxX)
                        Mouse.pTarget.X = dMaxX;
                    if (Mouse.pTarget.Y > dMaxY)
                        Mouse.pTarget.Y = dMaxY;
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

        /// <summary>
        /// Genuine Hack, just blocking Axis and filtering Triggers input to replace them without blocking other input
        /// </summary>
        private void SetHack()
        {
            //NOPing axis proc
            SetNops((int)_TargetProcess_MemoryBaseAddress, _Axis_NOP_Offset_1);
            SetHackInput();
                        
            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        //Hacking buttons proc : 
        //Same byte is used for both triggers, start and service (for each player)
        //0b10000000 is start
        //0b01000000 is Px Service
        //0b00000001 is TriggerL
        //0b00000010 is TriggerR
        //So we need to make a mask to accept Start button moodification and block other so we can inject            
        private void SetHackInput()
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov al,[ecx-01]
            CaveMemory.Write_StrBytes("8A 41 FF");
            //and al,03
            CaveMemory.Write_StrBytes("24 03");
            //and dl,C0
            CaveMemory.Write_StrBytes("80 E2 C0");
            //add dl,al
            CaveMemory.Write_StrBytes("00 C2");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //mov [ecx-01],dl
            CaveMemory.Write_StrBytes("88 51 FF");
            //not dl
            CaveMemory.Write_StrBytes("F6 D2");
            //Jump back
            CaveMemory.Write_jmp((int)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Return_Offset);

            WriteLog("Adding CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Injection de code
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((int)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
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

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    byte b = ReadByte((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset);
                    b |= 0x01;
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, b);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    byte b = ReadByte((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset);
                    b &= 0xFE;
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, b);
                }
                if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    byte b = ReadByte((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset);
                    b |= 0x02;
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, b);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    byte b = ReadByte((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset);
                    b &= 0xFD;
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, b);
                }
            }
            else if (Player == 2)
            {
                //Write Axis
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    byte b = ReadByte((int)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset);
                    b |= 0x01;
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset, b);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    byte b = ReadByte((int)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset);
                    b &= 0xFE;
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset, b);
                }
                if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    byte b = ReadByte((int)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset);
                    b |= 0x02;
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset, b);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    byte b = ReadByte((int)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset);
                    b &= 0xFD;
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset, b);
                }
            }
        }

        #endregion
    }
}
