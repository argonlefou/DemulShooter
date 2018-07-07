using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace DemulShooter
{
    class Game_RwOpGhost : Game
    {
        private const string FOLDER_GAMEDATA = @"MemoryData\ringwide";

        /*** MEMORY ADDRESSES **/
        protected int _P1_X_Address;
        protected int _P2_X_Address;
        protected int _P1_Y_Address;
        protected int _P2_Y_Address;
        protected string _Axis_X_NOP_Offset;
        protected string _Axis_Y_NOP_Offset;
        protected int _P1_Trigger_Address;
        protected int _P1_Action_Address;
        protected int _P1_Change_Address;
        protected int _P1_Reload_Address;
        protected int _Buttons_Injection_Offset;
        protected int _Buttons_Injection_Return_Offset;
        protected int _Axis_Injection_Offset;
        protected int _Axis_Injection_Return_Offset;

        //Keys
        //protected short _P2_Trigger_DIK = 0x4C; //NumPad 5
        //protected short _P2_Reload_DIK = 0x52;  //NumPad 0
        //protected short _P2_Change_DIK = 0x53;  //NumPad .
        //protected short _P2_Action_DIK = 0x4A;  //NumPad -
        //START_P2 = NumPad +
        //START_P1 = ENTER
        //Service = Y
        protected byte _P2_Trigger_VK = Win32.VK_NUMPAD5;
        protected byte _P2_Reload_VK = Win32.VK_NUMPAD0;
        protected byte _P2_Change_VK = Win32.VK_DECIMAL;
        protected byte _P2_Action_VK = Win32.VK_SUBSTRACT;


        // Test
        private bool P2OutOfScreen = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_RwOpGhost(string RomName, bool ParrotLoaderFullHack, bool Verbose) 
            : base ()
        {
            GetScreenResolution();
            
            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "gs2";
            
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
                            byte[] bTampon = ReadBytes((int)_TargetProcess_MemoryBaseAddress + 0x0265C20C, 4);
                            int Calc_Addr = bTampon[0] + bTampon[1] * 256 + bTampon[2] * 65536 + bTampon[3] * 16777216;

                            if (Calc_Addr != 0)
                            {
                                _P2_X_Address = Calc_Addr + 0x28;
                                _P2_Y_Address = Calc_Addr + 0x2C;

                                _ProcessHooked = true;
                                WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                //WriteLog("P1_X adddress =  0x" + _P1_X_Address.ToString("X8"));
                                //WriteLog("P1_Y adddress =  0x" + _P1_Y_Address.ToString("X8"));
                                WriteLog("P2_X adddress =  0x" + _P2_X_Address.ToString("X8"));
                                WriteLog("P2_Y adddress =  0x" + _P2_Y_Address.ToString("X8"));
                                CreateMemoryBank();
                                SetHack_Buttons();
                                SetHack_Axis();
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

                    //X => [0-1024]
                    //Y => [0-600]
                    double dMaxX = 1024.0;
                    double dMaxY = 600.0;

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
                                    case "AXIS_X_NOP_OFFSET":
                                        _Axis_X_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "AXIS_Y_NOP_OFFSET":
                                        _Axis_Y_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "BUTTONS_INJECTION_OFFSET":
                                        _Buttons_Injection_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "BUTTONS_INJECTION_RETURN_OFFSET":
                                        _Buttons_Injection_Return_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "AXIS_INJECTION_OFFSET":
                                        _Axis_Injection_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "AXIS_INJECTION_RETURN_OFFSET":
                                        _Axis_Injection_Return_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
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

        private void CreateMemoryBank()
        {
            //1st Memory created to store custom button data
            //This memory will be read by the codecave to overwrite the GetKeystate API results
            //And by the other codecave to overwrite mouse axis value
            //So we will have :
            Memory InputMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            InputMemory.Open();
            InputMemory.Alloc(0x800);
            _P1_Trigger_Address = InputMemory.CaveAddress;
            _P1_Reload_Address = InputMemory.CaveAddress + 0x10;
            _P1_Change_Address = InputMemory.CaveAddress + 0x01;
            _P1_Action_Address = InputMemory.CaveAddress + 0x03;
            _P1_X_Address = InputMemory.CaveAddress + 0x20;
            _P1_Y_Address = InputMemory.CaveAddress + 0x24;
            WriteLog("Custom Axis data will be stored at : 0x" + _P1_Trigger_Address.ToString("X8"));
        }

        //For this hack we will wait the GetKeyboardState call
        //And immediately after we will read on our custom memory storage
        //to replace lpKeystate bytes for mouse buttons (see WINUSER.H for virtualkey codes)
        //then the game will continue...            
        private void SetHack_Buttons()
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //call USER32.GetKEyboardState
            CaveMemory.Write_StrBytes("FF 15");
            byte[] b = BitConverter.GetBytes((int)_TargetProcess_MemoryBaseAddress + 0x001DF304);
            CaveMemory.Write_Bytes(b);
            //lpkeystate is in ESP register at that point :
            //and [esp + 1], 0x00FF0000
            CaveMemory.Write_StrBytes("81 64 24 01 00 00 FF 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [_P1_Trigger_Address]
            CaveMemory.Write_StrBytes("A1");
            b = BitConverter.GetBytes(_P1_Trigger_Address);
            CaveMemory.Write_Bytes(b);
            //We pushed eax so ESP was changed, so now lpkeystate is in. ESP+1+4
            //or [esp + 5], eax
            CaveMemory.Write_StrBytes("09 44 24 05");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            CaveMemory.Write_jmp((int)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Return_Offset);

            WriteLog("Adding Trigger CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((int)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        }
        
        private void SetHack_Axis()
        {
            //For this hack we will override the writing of X and Y data issued from
            //the legit ScrenToClient call, with our own calculated values
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //mov ecx, [_P1_X_Address]
            CaveMemory.Write_StrBytes("8B 0D");
            byte[] b = BitConverter.GetBytes(_P1_X_Address);
            CaveMemory.Write_Bytes(b);
            //mov edx, [_P1_Y_Address]
            CaveMemory.Write_StrBytes("8B 15");
            b = BitConverter.GetBytes(_P1_Y_Address);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_jmp((int)_TargetProcess.MainModule.BaseAddress + _Axis_Injection_Return_Offset);

            WriteLog("Adding Axis CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((int)_TargetProcess.MainModule.BaseAddress + _Axis_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_TargetProcess.MainModule.BaseAddress + _Axis_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);

            //Noping procedures for P2
            SetNops((int)_TargetProcess_MemoryBaseAddress, _Axis_X_NOP_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _Axis_Y_NOP_Offset);

            //Center Crosshair at start
            byte[] bufferX = { 0x00, 0x02, 0, 0 };  //512
            byte[] bufferY = { 0x2C, 0x01, 0, 0 };  //300
            WriteBytes(_P1_X_Address, bufferX);
            WriteBytes(_P1_Y_Address, bufferY);
            WriteBytes(_P2_X_Address, bufferX);
            WriteBytes(_P2_Y_Address, bufferY);

            WriteLog("Memory Hack complete !");
            WriteLog("-");

            //Win32.keybd_event(Win32.VK_NUMLOCK, 0x45, Win32.KEYEVENTF_EXTENDEDKEY | 0, 0);
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            byte[] bufferX = { (byte)(mouse.pTarget.X & 0xFF), (byte)(mouse.pTarget.X >> 8), 0, 0 };
            byte[] bufferY = { (byte)(mouse.pTarget.Y & 0xFF), (byte)(mouse.pTarget.Y >> 8), 0, 0 };

            if (Player == 1)
            {
                //Write Axis
                WriteBytes(_P1_X_Address, bufferX);
                WriteBytes(_P1_Y_Address, bufferY);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte(_P1_Trigger_Address, 0x80);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte(_P1_Trigger_Address, 0x00);
                }
                if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    WriteByte(_P1_Change_Address, 0x80);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    WriteByte(_P1_Change_Address, 0x00);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    WriteByte(_P1_Action_Address, 0x80);
                    mouse.pTarget.X = 2000;
                    byte[] bufferX_R = { (byte)(mouse.pTarget.X & 0xFF), (byte)(mouse.pTarget.X >> 8), 0, 0 };
                    WriteBytes(_P1_X_Address, bufferX_R);
                    System.Threading.Thread.Sleep(20);

                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    WriteByte(_P1_Action_Address, 0x00);
                }
            }
            else if (Player == 2)
            {
                //Write Axis
                WriteBytes(_P2_X_Address, bufferX);
                WriteBytes(_P2_Y_Address, bufferY);

                //P2 uses keyboard so no autoreload when out of screen, so we add:
                if (mouse.pTarget.X <= 1 || mouse.pTarget.X >= 1022 || mouse.pTarget.Y <= 1 || mouse.pTarget.Y >= 596)
                {
                    if (!P2OutOfScreen)
                    {
                        //SendKeyDown(_P2_Reload_DIK);
                        Send_VK_KeyDown(_P2_Reload_VK);
                        P2OutOfScreen = true;
                    }
                }
                else
                {
                    if (P2OutOfScreen)
                    {
                        //SendKeyUp(_P2_Reload_DIK);
                        Send_VK_KeyUp(_P2_Reload_VK);
                        P2OutOfScreen = false;
                    }
                }                    

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    //SendKeyDown(_P2_Trigger_DIK);
                    Send_VK_KeyDown(_P2_Trigger_VK);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    //SendKeyUp(_P2_Trigger_DIK);
                    Send_VK_KeyUp(_P2_Trigger_VK);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    //SendKeyDown(_P2_Change_DIK);
                    Send_VK_KeyDown(_P2_Change_VK);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    //SendKeyUp(_P2_Change_DIK);
                    Send_VK_KeyUp(_P2_Change_VK);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    //SendKeyDown(_P2_Reload_DIK);
                    //SendKeyDown(_P2_Action_DIK);
                    Send_VK_KeyDown(_P2_Reload_VK);
                    Send_VK_KeyDown(_P2_Action_VK);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    //SendKeyUp(_P2_Reload_DIK);
                    //SendKeyUp(_P2_Action_DIK);
                    Send_VK_KeyUp(_P2_Reload_VK);
                    Send_VK_KeyUp(_P2_Action_VK);
                }
            }
        }

        #endregion

    }
}
