using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace DemulShooter
{
    class Game_RwSGGv1 : Game
    {
        private const string FOLDER_GAMEDATA = @"MemoryData\ringwide";

        /*** MEMORY ADDRESSES **/
        protected int _P1_X_Offset;
        protected int _P1_Y_Offset;        
        protected int _P2_X_Offset;
        protected int _P2_Y_Offset;
        protected string _Axis_NOP_Offset;        
        protected int _P1_Buttons_Offset;
        protected int _P2_Buttons_Offset;        
        protected int _Buttons_Injection_Offset;
        protected int _Buttons_Injection_Return_Offset;

        private bool _ParrotLoaderFullHack = false;
        private const byte _ParrotLoader_P1_Start_ScanCode = 0x02;  //[1]
        private const byte _ParrotLoader_P2_Start_ScanCode = 0x03;  //[2]
        private const byte _ParrotLoader_Service_ScanCode = 0x09;   //[8]
        private const byte _ParrotLoader_Test_ScanCode = 0x0A;      //[9]

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_RwSGGv1(string RomName, bool ParrotLoaderFullHack, bool Verbose)
            : base()
        {
            GetScreenResolution();

            _RomName = RomName;
            _ParrotLoaderFullHack = ParrotLoaderFullHack;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "RingGunR_RingWide";
            _KnownMd5Prints.Add("GoldenGun Dump", "9a94458ca852b8b33d8b17b2cfdd663d");

            ReadGameData();
            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for RingWide " + _RomName + " game to hook.....");
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
                            
                            SetAxisHack();
                            ChecExeMd5();
                            if (_ParrotLoaderFullHack)
                            {
                                SetButtonsHack2();                                                              
                            }
                            else
                                SetButtonsHack1();
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

        /// <summary>
        /// Convert screen location of pointer to Client area location
        /// This game does not return a MainWindow Handle
        /// This game does not work in windowed mode
        /// So we keep screen resolution data
        /// </summary>
        public override bool ClientScale(MouseInfo mouse)
        {
            return true;
        }

        public override bool GameScale(MouseInfo Mouse, int Player)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    /*Win32.Rect TotalRes = new Win32.Rect();
                    Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;*/

                    double TotalResX = _screenWidth;
                    double TotalResY = _screenHeight;

                    //X => [07-F9] = 242
                    //Y => [07-F9] = 242
                    double dMaxX = 242.0;
                    double dMaxY = 242.0;

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

                    Mouse.pTarget.X += 7;
                    Mouse.pTarget.Y += 7;

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

        #region File I/O

        /// <summary>
        /// Read memory values in .cfg file
        /// </summary>
        protected override void ReadGameData()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\" + _RomName + ".cfg"))
            {
                using (StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\" + _RomName + ".cfg"))
                {
                    string line;
                    line = sr.ReadLine();
                    while (line != null)
                    {
                        string[] buffer = line.Split('=');
                        if (buffer.Length > 1)
                        {
                            try
                            {
                                switch (buffer[0].ToUpper().Trim())
                                {
                                    case "P1_X_OFFSET":
                                        _P1_X_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_Y_OFFSET":
                                        _P1_Y_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_BUTTONS_OFFSET":
                                        _P1_Buttons_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_X_OFFSET":
                                        _P2_X_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_Y_OFFSET":
                                        _P2_Y_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_BUTTONS_OFFSET":
                                        _P2_Buttons_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "AXIS_NOP_OFFSET":
                                        _Axis_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "BUTTONS_INJECTION_OFFSET":
                                        _Buttons_Injection_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "BUTTONS_INJECTION_RETURN_OFFSET":
                                        _Buttons_Injection_Return_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    default: break;
                                }
                            }
                            catch (Exception ex)
                            {
                                WriteLog("Error reading game data : " + ex.Message.ToString());
                            }
                        }
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }
            }
            else
            {
                WriteLog("File not found : " + AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\" + _RomName + ".cfg");
            }
        }

        #endregion       
        
        #region MemoryHack

        private void SetAxisHack()
        {
            //NOPing Axis proc
            SetNops((int)_TargetProcess_MemoryBaseAddress, _Axis_NOP_Offset);

            //Centering Crosshair at start
            WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, 0x80);
            WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, 0x80);
            WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, 0x80);
            WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, 0x80);
        }

        /*** START, Service and Trigger are on the same byte
        * Trigger is on the 4 lower bits
        * So this hack will just block those lower bits so that other TP buttons will still work ***/
        private void SetButtonsHack1()
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //cmp esi, 0
            CaveMemory.Write_StrBytes("83 FE 00");
            //jne original code
            CaveMemory.Write_StrBytes("0F 85 12 00 00 00");
            //and [ecx-01],FFFFFF0F
            CaveMemory.Write_StrBytes("81 61 FF 0F FF FF FF");
            //and dl,F0
            CaveMemory.Write_StrBytes("80 E2 F0");
            //or [ecx-01],dl
            CaveMemory.Write_StrBytes("08 51 FF");
            //jmp
            CaveMemory.Write_StrBytes("E9 03 00 00 00");
            //original code
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

            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }   


        //This hack will block all inputs
        //Used for -parrotloader option
        private void SetButtonsHack2()
        {
            //NOPing Buttons writing proc
            SetNops((int)_TargetProcess_MemoryBaseAddress, "0x0018B03B|3");

            //Enabling Keyboard Polling
            ApplyKeyboardHook();

            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        // Keyboard only used with -parrotloader switch to overwrite all parrot controls
        protected override IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                Win32.KBDLLHOOKSTRUCT s = (Win32.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32.WM_KEYDOWN)
                {
                    switch (s.scanCode)
                    {
                        case _ParrotLoader_P1_Start_ScanCode:
                            {
                                Apply_OR_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P1_Buttons_Offset, 0x80);
                            } break;
                        case _ParrotLoader_P2_Start_ScanCode:
                            {
                                Apply_OR_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P2_Buttons_Offset, 0x80);
                            } break;
                        case _ParrotLoader_Service_ScanCode:
                            {
                                Apply_OR_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P1_Buttons_Offset, 0x40);
                            } break;
                        default:
                            break;
                    }
                }
                else if ((UInt32)wParam == Win32.WM_KEYUP)
                {
                    switch (s.scanCode)
                    {
                        case _ParrotLoader_P1_Start_ScanCode:
                            {
                                Apply_AND_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P1_Buttons_Offset, 0x7F);
                            } break;
                        case _ParrotLoader_P2_Start_ScanCode:
                            {
                                Apply_AND_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P2_Buttons_Offset, 0x7F);
                            } break;
                        case _ParrotLoader_Service_ScanCode:
                            {
                                Apply_AND_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P1_Buttons_Offset, 0xBF);
                            } break;
                        default:
                            break;
                    }
                }
            }
            return Win32.CallNextHookEx(_KeyboardHookID, nCode, wParam, lParam);
        } 

        /// <summary>
        /// Keyboard polling
        /// Only used with -parrotloader switch to overwrite all parrot controls
        /// </summary>
        private void _Tmr_DIKeyboard_Tick(Object Sender, EventArgs e)
        {
            //If app is closed, may be throwing an error
            try
            {
                Apply_AND_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P1_Buttons_Offset, 0x3F);
                Apply_AND_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P2_Buttons_Offset, 0x3F);

                /*foreach (Key k in _Di_Keyboard.GetPressedKeys())
                {
                    //D1 -> StartP1
                    if ((byte)k == 0x02)
                    {
                        Apply_OR_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P1_Buttons_Offset, 0x80);
                    }
                    //D2 -> StartP2
                    else if ((byte)k == 0x03)
                    {
                        Apply_OR_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P2_Buttons_Offset, 0x80);
                    }
                    //D8 -> Service
                    else if ((byte)k == 0x09)
                    {
                        Apply_OR_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P1_Buttons_Offset, 0x40);
                    }
                    //D9 -> test                  
                    //else if ((byte)k == 0x0A)
                    //{
                    //    WriteByte(_Data_Base_Address + 0x04, 0x80);
                    //}
                    //D8 -> Service
                }*/
            }      
            catch {}
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            byte[] bufferX = { (byte)(mouse.pTarget.X & 0xFF), (byte)(mouse.pTarget.X >> 8) };
            byte[] bufferY = { (byte)(mouse.pTarget.Y & 0xFF), (byte)(mouse.pTarget.Y >> 8) };
            
            if (Player == 1)
            {
                //Write Axis
                WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX[0]);
                WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY[0]);
                
                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P1_Buttons_Offset, 0x02);
                    System.Threading.Thread.Sleep(20);
                    Apply_AND_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P1_Buttons_Offset, 0xFD);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    Apply_AND_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P1_Buttons_Offset, 0xFD);
                }
                if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P1_Buttons_Offset, 0x02);             
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {                    
                    Apply_AND_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P1_Buttons_Offset, 0xFD);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P1_Buttons_Offset, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    Apply_AND_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P1_Buttons_Offset, 0xFE);
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
                    Apply_OR_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P2_Buttons_Offset, 0x02);
                    System.Threading.Thread.Sleep(20);
                    Apply_AND_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P2_Buttons_Offset, 0xFD);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    Apply_AND_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P2_Buttons_Offset, 0xFD); 
                }
                if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P2_Buttons_Offset, 0x02); 
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Apply_AND_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P2_Buttons_Offset, 0xFD); 
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P2_Buttons_Offset, 0x01); 
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    Apply_AND_ByteMask((int)_TargetProcess.MainModule.BaseAddress + _P2_Buttons_Offset, 0xFE); 
                }
            }
        }

        

        #endregion
    }
}
