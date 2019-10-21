using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace DemulShooter
{
    class Game_TtxGungun2 : Game
    {
        private const string FOLDER_GAMEDATA = @"MemoryData\ttx";

        /*** MEMORY ADDRESSES **/
        protected int _P1_X_Offset;
        protected int _P1_Y_Offset;
        protected int _P1_Trigger_Offset;
        protected int _P1_Out_Offset;
        protected int _P2_X_Offset;
        protected int _P2_Y_Offset;
        protected int _P2_Trigger_Offset;
        protected int _P2_Out_Offset;
        protected string _P1_X_Nop_Offset;
        protected string _P1_Y_Nop_Offset;
        protected string _P1_Trigger_Nop_Offset;
        protected string _P1_Trigger_Nop_Offset2;
        protected string _P1_Out_Nop_Offset;
        protected string _P1_Out_Nop_Offset2;
        protected string _P2_X_Nop_Offset;
        protected string _P2_Y_Nop_Offset;
        protected string _P2_Trigger_Nop_Offset;
        protected string _P2_Trigger_Nop_Offset2;
        protected string _P2_Out_Nop_Offset;
        protected string _P2_Out_Nop_Offset2;


        /// <summary>
        /// Constructor
        /// </summary>
        public Game_TtxGungun2(string RomName, bool Verbose)
            : base()
        {
            GetScreenResolution();

            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "game";
            _KnownMd5Prints.Add("Original game.exe", "590677da06b758728a1dd607cbf032de");
            _KnownMd5Prints.Add("Jconfig modified game.exe", "57fb4970df6ef979d7ffc044e6161e84");

            ReadGameData();
            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for Taito Type X " + _RomName + " game to hook.....");
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
                                    case "P1_TRIGGER_OFFSET":
                                        _P1_Trigger_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_OUT_OFFSET":
                                        _P1_Out_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_X_OFFSET":
                                        _P2_X_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_Y_OFFSET":
                                        _P2_Y_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_TRIGGER_OFFSET":
                                        _P2_Trigger_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_OUT_OFFSET":
                                        _P2_Out_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_X_NOP_OFFSET":
                                        _P1_X_Nop_Offset = buffer[1].Trim();
                                        break;
                                    case "P1_Y_NOP_OFFSET":
                                        _P1_Y_Nop_Offset = buffer[1].Trim();
                                        break;
                                    case "P1_TRIGGER_NOP_OFFSET":
                                        _P1_Trigger_Nop_Offset = buffer[1].Trim();
                                        break;
                                    case "P1_TRIGGER_NOP_OFFSET2":
                                        _P1_Trigger_Nop_Offset2 = buffer[1].Trim();
                                        break;
                                    case "P1_OUT_NOP_OFFSET":
                                        _P1_Out_Nop_Offset = buffer[1].Trim();
                                        break;
                                    case "P1_OUT_NOP_OFFSET2":
                                        _P1_Out_Nop_Offset2 = buffer[1].Trim();
                                        break;
                                    case "P2_X_NOP_OFFSET":
                                        _P2_X_Nop_Offset = buffer[1].Trim();
                                        break;
                                    case "P2_Y_NOP_OFFSET":
                                        _P2_Y_Nop_Offset = buffer[1].Trim();
                                        break;
                                    case "P2_TRIGGER_NOP_OFFSET":
                                        _P2_Trigger_Nop_Offset = buffer[1].Trim();
                                        break;
                                    case "P2_TRIGGER_NOP_OFFSET2":
                                        _P2_Trigger_Nop_Offset2 = buffer[1].Trim();
                                        break;
                                    case "P2_OUT_NOP_OFFSET":
                                        _P2_Out_Nop_Offset = buffer[1].Trim();
                                        break;
                                    case "P2_OUT_NOP_OFFSET2":
                                        _P2_Out_Nop_Offset2 = buffer[1].Trim();
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

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// </summary>
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

                    //X => [0, 3FFO] = 16368
                    //Y => [0, 3FF0] = 16368
                    double dMaxX = 16368.0;
                    double dMaxY = 16368.0;

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

        #region MemoryHack

        /// <summary>
        /// Genuine Hack, just blocking Axis and Triggers input to replace them
        /// Reverse back to it when DumbJVSCommand will be working with ParrotLoader, without DumbJVSManager
        /// </summary>
        private void SetHack()
        {
            //NOPing proc
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P1_X_Nop_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P1_Y_Nop_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P1_Trigger_Nop_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P1_Trigger_Nop_Offset2);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P1_Out_Nop_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P1_Out_Nop_Offset2);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P2_X_Nop_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P2_Y_Nop_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P2_Trigger_Nop_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P2_Trigger_Nop_Offset2);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P2_Out_Nop_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P2_Out_Nop_Offset2);

            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            byte[] bufferX = { (byte)(mouse.pTarget.X & 0xFF), (byte)(mouse.pTarget.X >> 8) };
            byte[] bufferY = { (byte)(mouse.pTarget.Y & 0xFF), (byte)(mouse.pTarget.Y >> 8) };

            if (Player == 1)
            {
                //Write Axis
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x00);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_Out_Offset, 0x01);
                    System.Threading.Thread.Sleep(20);
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_Out_Offset, 0x00);
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x00);
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
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x00);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_Out_Offset, 0x01);
                    System.Threading.Thread.Sleep(20);
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_Out_Offset, 0x00);
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x00);
                }  
            }
        }

        #endregion


    }
}
