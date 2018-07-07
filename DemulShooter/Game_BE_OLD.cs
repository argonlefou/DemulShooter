using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace DemulShooter
{
    class Game_BE_OLD : Game
    {
        private const string FOLDER_GAMEDATA = @"MemoryData\windows";

        /*** Memory LOcations ***/
        private int _P1_X_Address;
        private int _P1_Y_Address;
        private int _P2_X_Address;
        private int _P2_Y_Address;
        protected int _P1_X_Injection_Offset_1;
        protected int _P1_X_Injection_Return_Offset_1;
        protected int _P1_X_Injection_Offset_2;
        protected int _P1_X_Injection_Return_Offset_2;
        protected int _P1_Y_Injection_Offset_1;
        protected int _P1_Y_Injection_Return_Offset_1;
        protected int _P1_Y_Injection_Offset_2;
        protected int _P1_Y_Injection_Return_Offset_2;
        protected int _P2_Axis_Injection_Offset_1;
        protected int _P2_Axis_Injection_Return_Offset_1;
        protected int _P2_Axis_Injection_Offset_2;
        protected int _P2_Axis_Injection_Return_Offset_2;
        
        /*** Custom Data to inject ***/
        private float _P1_X_Value;
        private float _P1_Y_Value;
        private float _P2_X_Value;
        private float _P2_Y_Value;
       
        
        /// <summary>
        /// Constructor
        /// </summary>
        public Game_BE_OLD(string RomName, bool Verbose) 
            : base ()
        {
            GetScreenResolution();
            
            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "BEGame";

            ReadGameData();
            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            /* Creating X360 controller */
            //_XOutputManager = new XOutput();
            //InstallX360Gamepad(2);

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

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            /* Wait until Splash Screen is closed and real windows displayed */ 
                            /* Game Windows classname = "LaunchUnrealUWindowsClient" */
                            StringBuilder ClassName = new StringBuilder(256);
                            int nRet = Win32.GetClassName(_TargetProcess.MainWindowHandle, ClassName, ClassName.Capacity);
                            if (nRet != 0 && ClassName.ToString() != "SplashScreenClass")
                            {
                                _ProcessHooked = true;
                                WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));

                                 
                                /* Memory Hack */
                                SetHack_V2();                                
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

        protected override void InstallX360Gamepad(int Player)
        {
            if (_XOutputManager != null)
            {
                if (_XOutputManager.isVBusExists())
                {
                    if (_XOutputManager.PlugIn(1))
                    {
                        if (Player == 2)
                        {
                            WriteLog("Plugged P2 virtual Gamepad to port 1");
                            _Player2_X360_Gamepad_Port = 1;
                        }
                    }
                    else
                    {
                        WriteLog("Failed to plug virtual GamePad to port 1. (Port already used ?)");
                        if (_XOutputManager.UnplugAll(true))
                        {
                            WriteLog("Force Unpluged all gamepads.");
                            System.Threading.Thread.Sleep(1000);
                            if (_XOutputManager.PlugIn(1))
                            {
                                if (Player == 2)
                                {
                                    WriteLog("Plugged P2 virtual Gamepad to port 1");
                                    _Player2_X360_Gamepad_Port = 1;
                                }
                            }
                            else
                            {
                                WriteLog("Failed to plug virtual GamePad to port 1.");
                            }
                        }
                        else
                        {
                            WriteLog("Failed to force Unplug virtual GamePad port 1.");
                        }
                    }
                }
                else
                {
                    WriteLog("ScpBus driver not found or not installed");
                }
            }
            else
            {
                WriteLog("XOutputManager Creation Failed !");
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
                    int TotalResX = TotalRes.Right - TotalRes.Left;
                    int TotalResY = TotalRes.Bottom - TotalRes.Top;

                    //X => [-1 ; 1] float
                    //Y => [-1 ; 1] float

                    float X_Value = (2.0f * Mouse.pTarget.X / TotalResX) - 1.0f;
                    float Y_Value = (2.0f * Mouse.pTarget.Y / TotalResY) - 1.0f;

                    if (X_Value < -1.0f)
                        X_Value = -1.0f;
                    if (Y_Value < -1.0f)
                        Y_Value = -1.0f;
                    if (X_Value > 1.0f)
                        X_Value = 1.0f;
                    if (Y_Value > 1.0f)
                        Y_Value = 1.0f;

                    if (Player == 1)
                    {
                        _P1_X_Value = X_Value;
                        _P1_Y_Value = Y_Value;
                    }
                    else if (Player == 2)
                    {
                        _P2_X_Value = X_Value;
                        _P2_Y_Value = Y_Value;
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
                                    case "P1_X_INJECTION_OFFSET_1":
                                        _P1_X_Injection_Offset_1 = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_X_INJECTION_RETURN_OFFSET_1":
                                        _P1_X_Injection_Return_Offset_1 = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_X_INJECTION_OFFSET_2":
                                        _P1_X_Injection_Offset_2 = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_X_INJECTION_RETURN_OFFSET_2":
                                        _P1_X_Injection_Return_Offset_2 = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_Y_INJECTION_OFFSET_1":
                                        _P1_Y_Injection_Offset_1 = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_Y_INJECTION_RETURN_OFFSET_1":
                                        _P1_Y_Injection_Return_Offset_1 = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_Y_INJECTION_OFFSET_2":
                                        _P1_Y_Injection_Offset_2 = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_Y_INJECTION_RETURN_OFFSET_2":
                                        _P1_Y_Injection_Return_Offset_2 = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_AXIS_INJECTION_OFFSET_1":
                                        _P2_Axis_Injection_Offset_1 = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_AXIS_INJECTION_RETURN_OFFSET_1":
                                        _P2_Axis_Injection_Return_Offset_1 = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_AXIS_INJECTION_OFFSET_2":
                                        _P2_Axis_Injection_Offset_2 = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_AXIS_INJECTION_RETURN_OFFSET_2":
                                        _P2_Axis_Injection_Return_Offset_2 = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
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

        private void SetHack_V2()
        {
            SetHack_Data();
            SetHack_P1X();
            SetHack_P1X_2();
            SetHack_P1Y((int)_TargetProcess.MainModule.BaseAddress + _P1_Y_Injection_Offset_1);
            SetHack_P1Y((int)_TargetProcess.MainModule.BaseAddress + _P1_Y_Injection_Offset_2);
            SetHack_P2Axis((int)_TargetProcess.MainModule.BaseAddress + _P2_Axis_Injection_Offset_1);
            SetHack_P2Axis((int)_TargetProcess.MainModule.BaseAddress + _P2_Axis_Injection_Offset_2);
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

            _P1_X_Address = DataCaveMemory.CaveAddress;
            _P1_Y_Address = DataCaveMemory.CaveAddress + 0x04;

            _P2_X_Address = DataCaveMemory.CaveAddress + 0x20;
            _P2_Y_Address = DataCaveMemory.CaveAddress + 0x24;

            WriteLog("Custom data will be stored at : 0x" + _P1_X_Address.ToString("X8"));
        }

        /// <summary>
        /// The game use some fstp [XXX] instruction, but we can't just NOP it as graphical glitches may appear.
        /// So we just add another set of instructions instruction immediatelly after to change the register 
        /// to our own desired value
        /// </summary>
        private void SetHack_P1X()
        {
            List<Byte> Buffer = new List<Byte>();
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //fstp dwordptr [esi+000002B0]
            CaveMemory.Write_StrBytes("D9 9E B0 02 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [P1_X]
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P1_X_Address);
            CaveMemory.Write_Bytes(b);
            //mov [esi+000002B0], eax
            CaveMemory.Write_StrBytes("89 86 B0 02 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            CaveMemory.Write_jmp((int)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Return_Offset_1);

            WriteLog("Adding P1 X Axis Codecave_1 at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((int)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Offset_1) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Offset_1, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        }


        //This Codecave is modifying the xmm0 value with our own
        private void SetHack_P1X_2()
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_X_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P1_X_Address);
            CaveMemory.Write_Bytes(b);
            //movss xmm0, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [esi+000002B0],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 86 B0 02 00 00");
            //return
            CaveMemory.Write_jmp((int)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Return_Offset_2);

            WriteLog("Adding P1_X CodeCave_2 at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((int)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Offset_2) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Offset_2, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        }


        //This Codecave is modifying the xmm0 value with our own
        //This instruction is called 2 times so there will be 2 instance of this codecave at different places
        //The instruction lenght is fixed (8) so we won't use the Injection_Return_Offset, but Injection_Offset + 0x08 
        private void SetHack_P1Y(int OriginalProcAddress)
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_Y_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P1_Y_Address);
            CaveMemory.Write_Bytes(b);
            //movss xmm0, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [esi+000002B4],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 86 B4 02 00 00");
            //return
            CaveMemory.Write_jmp(OriginalProcAddress + 0x08);

            WriteLog("Adding P1_Y CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - OriginalProcAddress - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, OriginalProcAddress, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        }


        //This Codecave is modifying the xmm0 value with our own
        //This instruction is called 2 times so there will be 2 instance of this codecave at different places
        //The instruction lenght is fixed (9) so we won't use the Injection_Return_Offset, but Injection_Offset + 0x09 
        private void SetHack_P2Axis(int OriginalProcAddress)
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //cmp ecx, 01
            CaveMemory.Write_StrBytes("83 F9 01");
            //je AxisY
            CaveMemory.Write_StrBytes("0F 84 0A 00 00 00");
            //mov eax, _P2_X_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P2_X_Address);
            CaveMemory.Write_Bytes(b);
            //jmp originalcode
            CaveMemory.Write_StrBytes("E9 05 00 00 00");
            //AxisY:
            //mov eax, _P2_Y_Address
            CaveMemory.Write_StrBytes("B8");
            b = BitConverter.GetBytes(_P2_Y_Address);
            CaveMemory.Write_Bytes(b);
            //originalcode:
            //movss xmm0, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [esi+ecx*4+000002B0],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 84 8E B0 02 00 00");
            //return
            CaveMemory.Write_jmp(OriginalProcAddress + 0x08);

            WriteLog("Adding P2_Axis CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - OriginalProcAddress - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, OriginalProcAddress, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            if (Player == 1)
            {
                //Setting Values in memory for the Codecave to read it
                byte[] buffer = BitConverter.GetBytes(_P1_X_Value);
                WriteBytes(_P1_X_Address, buffer);
                buffer = BitConverter.GetBytes(_P1_Y_Value);
                WriteBytes(_P1_Y_Address, buffer);
            }
            else if (Player == 2)
            {
                //Setting Values in memory for the Codecave to read it
                byte[] buffer = BitConverter.GetBytes(_P2_X_Value);
                WriteBytes(_P2_X_Address, buffer);
                buffer = BitConverter.GetBytes(_P2_Y_Value);
                WriteBytes(_P2_Y_Address, buffer);

                //changing the Right Axis Gamepad Value so that the game can update positioning
                //If the game does not detect a right axis change, no update is done !!
                //Value if [-1; 1] float so we convert to [0,32000] int for Xoutput format
                WriteLog("Float P2 X -----> " + _P2_X_Value.ToString());
                float fx = (_P2_X_Value + 1.0f) * 16000.0f;
                float fy = (_P2_Y_Value + 1.0f) * 16000.0f;
                WriteLog("           -----> " + fx.ToString());
                short ix = (short)fx;
                short iy = (short)fy;
                WriteLog("           -----> " + ix.ToString());
                buffer = BitConverter.GetBytes(ix);
                //WriteBytes(_P2_X_Address+0x30, buffer);
              
                //_XOutputManager.SetRAxis_X(_Player2_X360_Gamepad_Port,ix);
                //_XOutputManager.SetRAxis_Y(_Player2_X360_Gamepad_Port, iy);

                //Inputs
                //
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    _XOutputManager.SetButton_R2(_Player2_X360_Gamepad_Port, 0xFF);
                    _XOutputManager.SetButton_A(_Player2_X360_Gamepad_Port, true);  //used to validate in menu
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    _XOutputManager.SetButton_R2(_Player2_X360_Gamepad_Port, 0x00);
                    _XOutputManager.SetButton_A(_Player2_X360_Gamepad_Port, false); //used to validate in menu
                }
                if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                                    
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    _XOutputManager.SetButton_B(_Player2_X360_Gamepad_Port, true);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    _XOutputManager.SetButton_B(_Player2_X360_Gamepad_Port, false);   
                }
            }
        }

        #endregion
    }
}
