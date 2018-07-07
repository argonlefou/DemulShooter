using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace DemulShooter
{
    class Game_Dolphin4 : Game
    {
        private const string FOLDER_GAMEDATA = @"MemoryData\dolphin";

        /*** MEMORY ADDRESSES **/
        protected int _ControlsPtr_Offset;
        protected int _KeybMouse_X_Offset;
        protected int _KeybMouse_Y_Offset;
        protected int _KeybMouse_LBTN_Offset;
        protected int _KeybMouse_MBTN_Offset;
        protected int _KeybMouse_RBTN_Offset;        
        protected int _KeybMouse_X_Injection_Offset;
        protected int _KeybMouse_X_Return_Offset;
        protected int _KeybMouse_Y_Injection_Offset;
        protected int _KeybMouse_Y_Return_Offset;
        protected string _KeybMouse_BTN_NOP_Offset;
        protected int _ATRAK_X_Offset;
        protected int _ATRAK_Y_Offset;
        protected string _ATRAK_AXIS_NOP_Offset;

        private const int DIK_KEY_LCLICK = 0x1F; //S key
        private const int DIK_KEY_MCLICK = 0x20; //D Key
        private const int DIK_KEY_RCLICK = 0x21; //F Key

        /*** Process variables **/
        protected int _DinputNumber = 0;
        protected int _DinputControls_BaseAddress = 0;
        protected int _BasePtr = 0;
        protected int _KeybMouse_BaseAddress = 0;
        protected int _ATRAK_BaseAddress = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Dolphin4(string RomName, int DinputNumber, bool Verbose) 
            : base ()
        {
            GetScreenResolution();
            
            _RomName = RomName;
            _DinputNumber = DinputNumber;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "Dolphin";

            ReadGameData();
            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for Dolphin " + _RomName + " game to hook.....");
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
                            System.Threading.Thread.Sleep(2000);
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


        #region File I/O

        /// <summary>
        /// Read memory values in .cfg file
        /// </summary>
        protected override void ReadGameData()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\4.0.2_x86.cfg"))
            {
                using (StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\4.0.2_x86.cfg"))
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
                                    case "CONTROLS_PTR_OFFSET":
                                        _ControlsPtr_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "KEYBMOUSE_X_OFFSET":
                                        _KeybMouse_X_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "KEYBMOUSE_Y_OFFSET":
                                        _KeybMouse_Y_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "KEYBMOUSE_LBTN_OFFSET":
                                        _KeybMouse_LBTN_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "KEYBMOUSE_MBTN_OFFSET":
                                        _KeybMouse_MBTN_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "KEYBMOUSE_RBTN_OFFSET":
                                        _KeybMouse_RBTN_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "KEYBMOUSE_X_INJECTION_OFFSET":
                                        _KeybMouse_X_Injection_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "KEYBMOUSE_X_RETURN_OFFSET":
                                        _KeybMouse_X_Return_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "KEYBMOUSE_Y_INJECTION_OFFSET":
                                        _KeybMouse_Y_Injection_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "KEYBMOUSE_Y_RETURN_OFFSET":
                                        _KeybMouse_Y_Return_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "KEYBMOUSE_BTN_NOP_OFFSET":
                                        _KeybMouse_BTN_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "ATRAK_X_OFFSET":
                                        _ATRAK_X_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "ATRAK_Y_OFFSET":
                                        _ATRAK_Y_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "ATRAK_AXIS_NOP_OFFSET":
                                        _ATRAK_AXIS_NOP_Offset = buffer[1].Trim();
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
                WriteLog("File not found : " + AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\4.0.2_x86.cfg");
            }
        }

        #endregion

        #region MemoryHack

        private void SetHack()
        {
            //Calulation of base addresses for Dinput Keyboard/Mouse
            byte[] bTampon = ReadBytes((int)_TargetProcess_MemoryBaseAddress + _ControlsPtr_Offset, 8);
            _BasePtr = bTampon[0] + bTampon[1] * 256 + bTampon[2] * 65536 + bTampon[3] * 16777216;
            WriteLog("ControlsPtr address = 0x" + _BasePtr.ToString("X8"));
            try
            {
                bTampon = ReadBytes((int)_BasePtr, 8);
                _KeybMouse_BaseAddress = bTampon[0] + bTampon[1] * 256 + bTampon[2] * 65536 + bTampon[3] * 16777216;
            }
            catch { }
            WriteLog("DInput Keyboard/Mouse address = 0x" + _KeybMouse_BaseAddress.ToString("X8"));
            //ATRAK #2 -> 2e manette en Ptr+10 (1ere en Ptr + 4) si 2 aimtrak connectés !! 
            WriteLog("DInput Player2 device number in the list : " + _DinputNumber + 1);     
            try
            {
                bTampon = ReadBytes((int)_BasePtr + (0x4 * _DinputNumber), 8);
                _ATRAK_BaseAddress = bTampon[0] + bTampon[1] * 256 + bTampon[2] * 65536 + bTampon[3] * 16777216;
            }
            catch { }
            WriteLog("DInput Device#2 address = 0x" + _ATRAK_BaseAddress.ToString("X8"));
            
            //Nops
            SetNops((int)_TargetProcess_MemoryBaseAddress, _KeybMouse_BTN_NOP_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _ATRAK_AXIS_NOP_Offset);

            //CodeCave Axe X
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, @
            Buffer.Add(0xB8);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress + 18));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //fstp dword ptr [eax]
            CaveMemory.Write_StrBytes("D9 18");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //fild dword ptr [esp+08]
            CaveMemory.Write_StrBytes("DB 44 24 08");
            //jmp Exit
            CaveMemory.Write_jmp((int)_TargetProcess_MemoryBaseAddress + _KeybMouse_X_Return_Offset);

            //Injection de code
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((int)_TargetProcess_MemoryBaseAddress + _KeybMouse_X_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_TargetProcess_MemoryBaseAddress + _KeybMouse_X_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);

            WriteLog("Adding CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            Buffer = new List<Byte>();
            //push edx
            CaveMemory.Write_StrBytes("52");
            //mov edx, @
            Buffer.Add(0xBA);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress + 17));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //fstp dword ptr [edx]
            CaveMemory.Write_StrBytes("D9 1A");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //add esp,1C { 28 }
            CaveMemory.Write_StrBytes("83 C4 1C");
            //jmp Exit
            CaveMemory.Write_jmp((int)_TargetProcess_MemoryBaseAddress + _KeybMouse_Y_Return_Offset);

            //Injection de code
            jumpTo = CaveMemory.CaveAddress - ((int)_TargetProcess_MemoryBaseAddress + _KeybMouse_Y_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_TargetProcess_MemoryBaseAddress + _KeybMouse_Y_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);

            WriteLog("Adding CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Initialise pour prise en compte des guns direct
            if (_KeybMouse_BaseAddress != 0)
            {
                WriteBytes(_KeybMouse_BaseAddress + _KeybMouse_X_Offset, new byte[] { 0, 0, 0, 0 });
                WriteBytes(_KeybMouse_BaseAddress + _KeybMouse_Y_Offset, new byte[] { 0, 0, 0, 0 });
                WriteByte(_KeybMouse_BaseAddress + _KeybMouse_LBTN_Offset, 0x00);
                WriteByte(_KeybMouse_BaseAddress + _KeybMouse_RBTN_Offset, 0x00);
                WriteByte(_KeybMouse_BaseAddress + _KeybMouse_MBTN_Offset, 0x00);
            }
            if (_ATRAK_BaseAddress != 0)
            {
                WriteBytes(_ATRAK_BaseAddress + _ATRAK_X_Offset, new byte[] { 0, 0, 0, 0 });
                WriteBytes(_ATRAK_BaseAddress + _ATRAK_Y_Offset, new byte[] { 0, 0, 0, 0 });
            }

            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            if (Player == 1 && _KeybMouse_BaseAddress != 0)
            {
                float fX = (float)mouse.pTarget.X / (float)1000;
                float fY = (float)mouse.pTarget.Y / (float)1000;
                byte[] bufferX = BitConverter.GetBytes(fX);
                byte[] bufferY = BitConverter.GetBytes(fY);

                //Write Axis
                WriteBytes(_KeybMouse_BaseAddress + _KeybMouse_X_Offset, bufferX);
                WriteBytes(_KeybMouse_BaseAddress + _KeybMouse_Y_Offset, bufferY);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte(_KeybMouse_BaseAddress + _KeybMouse_LBTN_Offset, 0x80);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte(_KeybMouse_BaseAddress + _KeybMouse_LBTN_Offset, 0x00);
                }
                if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    WriteByte(_KeybMouse_BaseAddress + _KeybMouse_MBTN_Offset, 0x80);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    WriteByte(_KeybMouse_BaseAddress + _KeybMouse_MBTN_Offset, 0x00);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    WriteByte(_KeybMouse_BaseAddress + _KeybMouse_RBTN_Offset, 0x80);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    WriteByte(_KeybMouse_BaseAddress + _KeybMouse_RBTN_Offset, 0x00);
                }
            }
            else if (Player == 2 && _ATRAK_BaseAddress != 00)
            {
                //Converting 0xFF80 to 0xFFFFFF80 and so on...
                byte[] bufferX = { (byte)(mouse.pTarget.X & 0xFF), (byte)(mouse.pTarget.X >> 8), (byte)(mouse.pTarget.X >> 8), (byte)(mouse.pTarget.X >> 8) };
                byte[] bufferY = { (byte)(mouse.pTarget.Y & 0xFF), (byte)(mouse.pTarget.Y >> 8), (byte)(mouse.pTarget.Y >> 8), (byte)(mouse.pTarget.Y >> 8) };

                //Write Axis
                WriteBytes(_ATRAK_BaseAddress + _ATRAK_X_Offset, bufferX);
                WriteBytes(_ATRAK_BaseAddress + _ATRAK_Y_Offset, bufferY);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    SendKeyDown(DIK_KEY_LCLICK);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    SendKeyUp(DIK_KEY_LCLICK);
                }
                if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    SendKeyDown(DIK_KEY_MCLICK);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    SendKeyUp(DIK_KEY_MCLICK);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    SendKeyDown(DIK_KEY_RCLICK);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    SendKeyUp(DIK_KEY_RCLICK);
                }
            }
        }

        #endregion

        #region Screen

        public override bool GameScale(MouseInfo Mouse, int Player)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    //Model2 Window size
                    Win32.Rect TotalRes = new Win32.Rect();
                    Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //Direct input mouse : float from -1 to +1
                    if (Player == 1)
                    {
                        //Convert client coordonnate to [-1, 1-] coordonates
                        double dX = Mouse.pTarget.X / TotalResX * 2.0 - 1.0;
                        double dY = Mouse.pTarget.Y / TotalResY * 2.0 - 1.0;
                        if (dX < -1)
                            dX = -1;
                        else if (dX > 1)
                            dX = 1;
                        if (dY < -1)
                            dY = -1;
                        else if (dY > 1)
                            dY = 1;

                        Mouse.pTarget.X =(int)(dX * 1000);
                        Mouse.pTarget.Y = (int)(dY * 1000);
                    }
                    //Dinput ATRAK : 
                    //min = FFFFFF80 max 0000080 , change from FFFFFF to 000000 at zero
                    //min = top left
                    else if (Player == 2)
                    {
                        double dMax = 254.0;

                        if (Mouse.pTarget.X < 0)
                            Mouse.pTarget.X = 0xFF80;
                        else if (Mouse.pTarget.X > (int)TotalResX)
                            Mouse.pTarget.X = 0x0080;
                        else
                            Mouse.pTarget.X = Convert.ToInt32(Math.Round(dMax * Mouse.pTarget.X / TotalResX) - 0x7F);

                        if (Mouse.pTarget.Y < 0)
                            Mouse.pTarget.Y = 0xFF80;
                        else if (Mouse.pTarget.Y > (int)TotalResY)
                            Mouse.pTarget.Y = 0x0080;
                        else
                            Mouse.pTarget.Y = Convert.ToInt32(Math.Round(dMax * Mouse.pTarget.Y / TotalResY) - 0x7F);
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
    }
}
