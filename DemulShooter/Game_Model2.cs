using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace DemulShooter
{
    class Game_Model2 : Game
    {     

        private const string FOLDER_GAMEDATA = @"MemoryData\model2";

        /*** MEMORY ADDRESSES **/
        protected int _P1_X_Offset;
        protected int _P1_Y_Offset;
        protected int _P1_LBtn_Offset;
        protected int _P1_RBtn_Offset;
        protected int _P2_X_Offset;
        protected int _P2_Y_Offset;
        protected int _P2_LBtn_Offset;
        protected int _P2_RBtn_Offset;
        protected string _X_NOP_Offset;
        protected string _X_NOP_Offset2;
        protected string _Y_NOP_Offset;
        protected string _Y_NOP_Offset2;
        protected string _RBtn_NOP_Offset;
        protected string _RBtn_NOP_Offset2;
        protected string _LBtn_NOP_Offset;
        protected string _LBtn_NOP_Offset2;

        private int _Calc_Addr1 = 0;
        private int _Controls_Base_Address = 0;
       
        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_Model2(string RomName, String EmulatorVersion, bool Verbose) 
            : base ()
        {
            GetScreenResolution();
            
            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            if (EmulatorVersion.Equals("model2"))
            {
                _Target_Process_Name = "emulator";
            }
            else if (EmulatorVersion.Equals("model2m"))
            {
                _Target_Process_Name = "emulator_multicpu";
            }
            _KnownMd5Prints.Add("Model2Emulator 1.1a", "26bd488f9a391dcac1c5099014aa1c9e");
            _KnownMd5Prints.Add("Model2Emulator 1.1a multicpu", "ac59ce7cfb95d6d639c0f0d1afba1192");
            

            ReadGameData();

            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for Model2 " + _RomName + " game to hook.....");
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
                            // Check for a specific value for later calculation
                            // Use this value to know if a game is really loaded onto the emulator
                            //byte[] bTampon = ReadBytes(0x18FB14, 4); => old method, doesn't work with XP
                            byte[] bTampon = ReadBytes((int)_TargetProcess_MemoryBaseAddress + 0x001AA730, 4);
                            _Calc_Addr1 = bTampon[0] + bTampon[1] * 256 + bTampon[2] * 65536 + bTampon[3] * 16777216;
                            if (_Calc_Addr1 != 0)
                            {
                                _ProcessHooked = true;
                                WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                WriteLog("Calculated address 1 = 0x" + _Calc_Addr1.ToString("X8"));
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

        public override bool ClientScale(MouseInfo mouse)
        {
            if (_TargetProcess != null)
                //Convert Screen location to Client location
                //No conversion needed for model2
                return true;
            else
                return false;
        }

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// </summary>
        public override bool GameScale(MouseInfo Mouse, int Player)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    //Model2 Window size
                    //Usually we get ClientRect but for model2 there's a bug and we need to get windowsrect
                    Win32.Rect TotalRes = new Win32.Rect();
                    //Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    Win32.GetWindowRect(_TargetProcess.MainWindowHandle, ref TotalRes);

                    //WriteLog("Client rect == " + TotalRes.Left.ToString() + ";" + TotalRes.Right.ToString() + ";" + TotalRes.Top.ToString() + ";" + TotalRes.Bottom.ToString());

                    double GameResX = 0.0;
                    double GameResY = 0.0;
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;
                    WriteLog("Model2 window resolution = " + TotalResX.ToString() + "x" + TotalResY.ToString());

                    //Just like some TTX games, Model2 is waiting for data according to it's resolution
                    // (i.e there are no fixed boundaries for X and Y axis values)
                    //But when used with DxWnd with a different window resolution than the actual D3D resolution, this will 
                    //block cursors in a limited part of the game
                    //That's why we're going to read the memory to find real X and Y rendering values and scale data accordingly
                    if (TotalResX != 0 && TotalResY != 0)
                    {
                        byte[] buffer = ReadBytes((int)_TargetProcess_MemoryBaseAddress + 0x009CEC00, 4);
                        int _RealResAddress = BitConverter.ToInt32(buffer, 0);
                        WriteLog("Model2 real resolution address = 0x" + _RealResAddress.ToString("X8"));
                        byte[] bufferX = ReadBytes(_RealResAddress + 0x00002D8C, 4);
                        GameResX = (double)BitConverter.ToInt32(bufferX, 0);
                        byte[] bufferY = ReadBytes(_RealResAddress + 0x00002D90, 4);
                        GameResY = (double)BitConverter.ToInt32(bufferY, 0);
                        WriteLog("Model2 render resolution = " + GameResX.ToString() + "x" + GameResY.ToString());

                        if (GameResX != 0 && GameResY != 0)
                        {

                            double RatioX = GameResX / TotalResX;
                            double RatioY = GameResY / TotalResY;

                            Mouse.pTarget.X = Convert.ToInt16(Math.Round(RatioX * Mouse.pTarget.X));
                            Mouse.pTarget.Y = Convert.ToInt16(Math.Round(RatioY * Mouse.pTarget.Y));
                        }
                        //Game rendering resolution autodetection failure ?
                        else
                        {
                            WriteLog("Automatic resolution detection failed, using old game scaling method");
                            GameResX = TotalResX;
                            GameResY = TotalResY;
                        }
                    }
                    // Some user have issue with the emulator and it's window size
                    // In that case, reverting back to OLD method
                    else
                    {
                        WriteLog("Model2 main window size is null, using old game scaling method");
                        GameResX = TotalResX;
                        GameResY = TotalResY;
                    }

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
        //OLD procedure, for reminder
        /*public override bool GameScale(MouseInfo Mouse, int Player)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    //Model2 Window size
                    //Usually we get ClientRect but for model2 there's a bug and we need to get windowsrect
                    Win32.Rect TotalRes = new Win32.Rect();
                    //Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    Win32.GetWindowRect(_TargetProcess.MainWindowHandle, ref TotalRes);
 
                    //WriteLog("Client rect == " + TotalRes.Left.ToString() + ";" + TotalRes.Right.ToString() + ";" + TotalRes.Top.ToString() + ";" + TotalRes.Bottom.ToString());

                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;
                    //WriteLog("Total Res = " + TotalResX.ToString() + ";" + TotalResY.ToString());


                    if (Mouse.pTarget.X < 0)
                        Mouse.pTarget.X = 0;
                    if (Mouse.pTarget.Y < 0)
                        Mouse.pTarget.Y = 0;
                    if (Mouse.pTarget.X > (int)TotalResX)
                        Mouse.pTarget.X = (int)TotalResX;
                    if (Mouse.pTarget.Y > (int)TotalResX)
                        Mouse.pTarget.Y = (int)TotalResX;
                    return true;
                }
                catch (Exception ex)
                {
                    WriteLog("Error scaling mouse coordonates to GameFormat : " + ex.Message.ToString());
                }
            }
            return false;
        }*/

        #endregion

        #region File I/O

        /// <summary>
        /// Read memory values in .cfg file
        /// </summary>
        protected override void ReadGameData()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\model2.cfg"))
            {
                using (StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\model2.cfg"))
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
                                    case "P1_LBTN_OFFSET":
                                        _P1_LBtn_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_RBTN_OFFSET":
                                        _P1_RBtn_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;                                                       
                                    case "P2_X_OFFSET":
                                        _P2_X_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_Y_OFFSET":
                                        _P2_Y_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_LBTN_OFFSET":
                                        _P2_LBtn_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_RBTN_OFFSET":
                                        _P2_RBtn_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "X_NOP_OFFSET":
                                        _X_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "X_NOP_OFFSET_2":
                                        _X_NOP_Offset2 = buffer[1].Trim();
                                        break;
                                    case "Y_NOP_OFFSET":
                                        _Y_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "Y_NOP_OFFSET_2":
                                        _Y_NOP_Offset2 = buffer[1].Trim();
                                        break;
                                    case "LBTN_NOP_OFFSET":
                                        _LBtn_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "LBTN_NOP_OFFSET_2":
                                        _LBtn_NOP_Offset2 = buffer[1].Trim();
                                        break;
                                    case "RBTN_NOP_OFFSET":
                                        _RBtn_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "RBTN_NOP_OFFSET_2":
                                        _RBtn_NOP_Offset2 = buffer[1].Trim();
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
                WriteLog("File not found : " + AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\model2.cfg");
            }
        }       

        #endregion

        #region MemoryHack

        private void SetHack()
        {
            System.Threading.Thread.Sleep(1000);
            
            //1st we need to calculate base address for controls               
            byte[] bTampon = ReadBytes(_Calc_Addr1 + 0x8, 4);
            _Controls_Base_Address = bTampon[0] + bTampon[1] * 256 + bTampon[2] * 65536 + bTampon[3] * 16777216;
            WriteLog("Controls memory base address = " + _Controls_Base_Address.ToString("X8"));            

            //NOP for X-Y axis and buttons
            SetNops((int)_TargetProcess_MemoryBaseAddress, _X_NOP_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _X_NOP_Offset2);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _Y_NOP_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _Y_NOP_Offset2);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _LBtn_NOP_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _LBtn_NOP_Offset2);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _RBtn_NOP_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _RBtn_NOP_Offset2);

            //Init
            byte[] initX = { 0xF8, 0 };
            byte[] initY = { 0xC0, 0 };
            WriteBytes(_Controls_Base_Address + _P1_X_Offset, initX);
            WriteBytes(_Controls_Base_Address + _P1_Y_Offset, initY);
            WriteByte(_Controls_Base_Address + _P1_LBtn_Offset, 0);
            WriteByte(_Controls_Base_Address + _P1_RBtn_Offset, 0);
            WriteBytes(_Controls_Base_Address + _P2_X_Offset, initX);
            WriteBytes(_Controls_Base_Address + _P2_Y_Offset, initY);
            WriteByte(_Controls_Base_Address + _P2_LBtn_Offset, 0);
            WriteByte(_Controls_Base_Address + _P2_RBtn_Offset, 0);

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
                WriteBytes(_Controls_Base_Address + _P1_X_Offset, bufferX);
                WriteBytes(_Controls_Base_Address + _P1_Y_Offset, bufferY);
                
                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte(_Controls_Base_Address + _P1_LBtn_Offset, 0x80);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte(_Controls_Base_Address + _P1_LBtn_Offset, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    if (_RomName.Equals("bel"))
                    {
                        WriteByte(_Controls_Base_Address + _P1_RBtn_Offset, 0x80);
                    }
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    if (_RomName.Equals("bel"))
                    {
                        WriteByte(_Controls_Base_Address + _P1_RBtn_Offset, 0x00);
                    }
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    if (!_RomName.Equals("bel"))
                    {
                        WriteByte(_Controls_Base_Address + _P1_RBtn_Offset, 0x80);
                    }
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    if (!_RomName.Equals("bel"))
                    {
                        WriteByte(_Controls_Base_Address + _P1_RBtn_Offset, 0x00);
                    }
                }
            }
            else if (Player == 2)
            {
                //Write Axis
                WriteBytes(_Controls_Base_Address + _P2_X_Offset, bufferX); 
                WriteBytes(_Controls_Base_Address + _P2_Y_Offset, bufferY);
               
                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte(_Controls_Base_Address + _P2_LBtn_Offset, 0x80);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte(_Controls_Base_Address + _P2_LBtn_Offset, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    if (_RomName.Equals("bel"))
                    {
                        WriteByte(_Controls_Base_Address + _P2_RBtn_Offset, 0x80);
                    }
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    if (_RomName.Equals("bel"))
                    {
                        WriteByte(_Controls_Base_Address + _P2_RBtn_Offset, 0x00);
                    }
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    if (!_RomName.Equals("bel"))
                    {
                        WriteByte(_Controls_Base_Address + _P2_RBtn_Offset, 0x80);
                    }
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    if (!_RomName.Equals("bel"))
                    {
                        WriteByte(_Controls_Base_Address + _P2_RBtn_Offset, 0x00);
                    }
                }
            }
        }
        
        #endregion        
    }
}
