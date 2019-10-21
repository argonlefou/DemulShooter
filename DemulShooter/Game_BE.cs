using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text;

namespace DemulShooter
{
    class Game_BE : Game
    {
        private const string FOLDER_GAMEDATA = @"MemoryData\windows";

        /*** MEMORY ADDRESSES **/
        protected int _P1_X_Address;
        protected int _P1_Y_Address;
        protected int _P2_X_Address;
        protected int _P2_Y_Address;
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

        //Keys to send
        //For player 2 if used, keys are choosed for x360kb.ini:
        //I usually prefer to send VirtualKeycodes (less troublesome when no physical Keyboard is plugged)
        //But with x360kb only DIK keycodes are working
        protected short _P2_Trigger_DIK = 0x14; //T
        protected short _P2_Reload_DIK = 0x16;  //Y
        protected short _P2_Grenade_DIK = 015;  //U
        protected short _P2_CoverLeft_DIK = 0x17;  //I
        protected short _P2_CoverDown_DIK = 0x18;  //I
        protected short _P2_CoverRight_DIK = 0x19;  //I

        //Custom data to inject
        protected float _P1_X_Value;
        protected float _P1_Y_Value;
        protected float _P2_X_Value;
        protected float _P2_Y_Value;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_BE(string RomName, string GamePath, bool Verbose)
            : base()
        {
            GetScreenResolution();

            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "BEGame";
            _KnownMd5Prints.Add("Blue Estate CODEX", "188605d4083377e4ee3552b4c89f52fb");

            // To play as Player2 the game needs a Joypad
            // By using x360kb.ini and xinput1_3.dll in the game's folder, we can add a virtual X360 Joypad to act as player 2
            /*try
            {
                using (StreamWriter sw = new StreamWriter(GamePath + @"\x360kb.ini", false))
                {
                    if (_EnableP2)
                        sw.Write(DemulShooter.Properties.Resources.x360kb_hfirea2p);
                    else
                        sw.Write(DemulShooter.Properties.Resources.x360kb_hfirea1p);
                }
                WriteLog("File \"" + GamePath + "\\x360kb.ini\" successfully written !");
            }
            catch (Exception ex)
            {
                WriteLog("Error trying to write file " + GamePath + "\\x360kb.ini\" :" + ex.Message.ToString());
            }*/

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

                    WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

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

        #region MemoryHack

        private void SetHack()
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

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    //Apply_OR_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    //Apply_AND_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFE);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    //Apply_OR_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x02);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    //Apply_AND_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFD);
                }
            }
            else if (Player == 2)
            {
                //Setting Values in memory for the Codecave to read it
                byte[] buffer = BitConverter.GetBytes(_P2_X_Value);
                WriteBytes(_P2_X_Address, buffer);
                buffer = BitConverter.GetBytes(_P2_Y_Value);
                WriteBytes(_P2_Y_Address, buffer);


                // Player 2 buttons are simulated by x360kb.ini so we just send needed Keyboard strokes
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
                    
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    SendKeyDown(_P2_Reload_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    SendKeyUp(_P2_Reload_DIK);
                }
            }
        }        

        #endregion
    }
}
