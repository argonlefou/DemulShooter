using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace DemulShooter
{
    class Game_TtxHauntedMuseum : Game
    {
        private const string FOLDER_GAMEDATA = @"MemoryData\ttx";

        /*** MEMORY ADDRESSES **/
        protected int _P1_X_Address;
        protected int _P1_Y_Address;
        protected int _P2_X_Address;
        protected int _P2_Y_Address;
        protected int _P1_Trigger_Address;
        protected int _P2_Trigger_Address;
        protected int _Axis_Injection_Offset;
        protected int _Axis_Injection_Return_Offset;
        protected int _Buttons_Injection_Offset;
        protected int _Buttons_Injection_Return_Offset;

        protected int _ScreenWidth_Offset = 0x00328520;
        protected int _ScreenHeight_Offset = 0x00328524;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_TtxHauntedMuseum(string RomName, bool Verbose) 
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

            WriteLog("Waiting for TTX " + _RomName + " game to hook.....");
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

                    //This engine (common with other TTX shooter) is waiting for X and Y value in range [0 ; WindowSize]
                    //BUT using the raw window size is troublesome when the game is combined with DxWnd as the
                    //resulting real window is not the same size as the game engine parameters (SCREEN_WITH, RENDER_WIDTH, etc...)
                    //That's why we're going to read the memory to find the INI parameter and scale the X,Y values accordingly
                    byte[] bufferX = ReadBytes((int)_TargetProcess_MemoryBaseAddress + _ScreenWidth_Offset, 4);
                    double GameResX = (double)BitConverter.ToInt32(bufferX, 0);
                    byte[] bufferY = ReadBytes((int)_TargetProcess_MemoryBaseAddress + _ScreenHeight_Offset, 4);
                    double GameResY = (double)BitConverter.ToInt32(bufferY, 0);

                    WriteLog("Game engine render resolution (Px) = [ " + GameResX + "x" + GameResY + " ]");

                    double RatioX = GameResX / TotalResX;
                    double RatioY = GameResY / TotalResY;

                    Mouse.pTarget.X = Convert.ToInt16(Math.Round(RatioX * Mouse.pTarget.X));
                    Mouse.pTarget.Y = Convert.ToInt16(Math.Round(RatioY * Mouse.pTarget.Y));
                    if (Mouse.pTarget.X < 0)
                        Mouse.pTarget.X = 0;
                    if (Mouse.pTarget.Y < 0)
                        Mouse.pTarget.Y = 0;
                    if (Mouse.pTarget.X > (int)GameResX)
                        Mouse.pTarget.X = (int)GameResX;
                    if (Mouse.pTarget.Y > (int)GameResY)
                        Mouse.pTarget.Y = (int)GameResY;

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
                                    case "AXIS_INJECTION_OFFSET":
                                        _Axis_Injection_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "AXIS_INJECTION_RETURN_OFFSET":
                                        _Axis_Injection_Return_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
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

        private void SetHack()
        {
            //Codecave :
            //P1 and P2 share same memory values so we split them :
            //Changing proc so that X and Y will be read on custom memomy values
            //We will feed it with device axis data
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //CodeCave takes less than 39 bytes
            //Memory data will be after
            //So we will have :
            _P1_X_Address = CaveMemory.CaveAddress + 0x40;
            _P1_Y_Address = CaveMemory.CaveAddress + 0x48;
            _P1_Trigger_Address = CaveMemory.CaveAddress + 0x50;
            _P2_X_Address = CaveMemory.CaveAddress + 0x60;
            _P2_Y_Address = CaveMemory.CaveAddress + 0x68;
            _P2_Trigger_Address = CaveMemory.CaveAddress + 0x70;

            WriteLog("Custom data will be stored at : 0x" + _P1_X_Address.ToString("X8"));

           
            //
            //FIRST CODECAVE : SPLIT P1 and P2 AXIS
            //
            List<Byte> Buffer = new List<Byte>();
            //cmp ecx,O1
            CaveMemory.Write_StrBytes("83 F9 01");
            //je P2
            CaveMemory.Write_StrBytes("0F 84 12 00 00 00");
            //mov edx,[P1_X]
            byte[] b = BitConverter.GetBytes(_P1_X_Address);
            CaveMemory.Write_StrBytes("8B 15");
            CaveMemory.Write_Bytes(b);
            //mov [ebx], edx
            CaveMemory.Write_StrBytes("89 13");
            //mov eax,[P1_Y]
            b = BitConverter.GetBytes(_P1_Y_Address);
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(b);
            //jmp exit
            CaveMemory.Write_StrBytes("E9 0D 00 00 00");
            //P2
            //mov edx,[P2_X]
            b = BitConverter.GetBytes(_P2_X_Address);
            CaveMemory.Write_StrBytes("8B 15");
            CaveMemory.Write_Bytes(b);
            //mov [ebx],edx
            CaveMemory.Write_StrBytes("89 13");
            //mov eax,[P2_Y]
            b = BitConverter.GetBytes(_P2_Y_Address);
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(b);

            CaveMemory.Write_jmp((int)_TargetProcess.MainModule.BaseAddress + _Axis_Injection_Return_Offset);

            WriteLog("Adding Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((int)_TargetProcess.MainModule.BaseAddress + _Axis_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_TargetProcess.MainModule.BaseAddress + _Axis_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);

            HackTrigger();

            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        private void HackTrigger()
        {            
            //For this hack we will wait the GetKeyboardState call
            //And immediately after we will read on our custom memory storage
            //to replace lpKeystate bytes for mouse buttons (see WINUSER.H for virtualkey codes)
            //then the game will continue...
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();            
            //(lpKeystate+0x100) address is in ESI register  
            //and [esi-100], 0xFF0000FF
            CaveMemory.Write_StrBytes("81 A6 00 FF FF FF FF 00 00 FF");
            //cmp [_P1_Trigger], 80
            CaveMemory.Write_StrBytes("81 3D");
            byte[] b = BitConverter.GetBytes(_P1_Trigger_Address);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("80 00 00 00");
            //jne P2_Trigger
            CaveMemory.Write_StrBytes("0F 85 0A 00 00 00");
            //or [esi-FF], 80
            CaveMemory.Write_StrBytes("81 8E 01 FF FF FF 80 00 00 00");
            
            //P2_Trigger:
            //cmp [_P1_Trigger], 80
            CaveMemory.Write_StrBytes("81 3D");
            b = BitConverter.GetBytes(_P2_Trigger_Address);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("80 00 00 00");
            //jne originalcode
            CaveMemory.Write_StrBytes("0F 85 0A 00 00 00");
            //or [esi-FF], 80
            CaveMemory.Write_StrBytes("81 8E 02 FF FF FF 80 00 00 00");
            //OriginalCode
            //call game.exe+AFCD0
            CaveMemory.Write_call((int)_TargetProcess.MainModule.BaseAddress + 0xAFCD0);
            //return
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
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            byte[] bufferX = { (byte)(mouse.pTarget.X & 0xFF), (byte)(mouse.pTarget.X >> 8) };
            byte[] bufferY = { (byte)(mouse.pTarget.Y & 0xFF), (byte)(mouse.pTarget.Y >> 8) };

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
            }
            else if (Player == 2)
            {
                //Write Axis
                WriteBytes(_P2_X_Address, bufferX);
                WriteBytes(_P2_Y_Address, bufferY);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte(_P2_Trigger_Address, 0x80);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte(_P2_Trigger_Address, 0x00);
                }                
            }
        }

        #endregion

    }
}
