using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;


namespace DemulShooter
{
    class Game_TtxSha : Game
    {
        private const string FOLDER_GAMEDATA = @"MemoryData\ttx";

        /*** MEMORY ADDRESSES **/
        protected int _P1_X_Offset;
        protected int _P1_Y_Offset;
        protected string _P1_X_NOP_Offset;
        protected string _P1_Y_NOP_Offset;
        protected int _P1_OUT_Offset;
        protected string _P1_OUT_NOP_Offset;
        protected int _P2_X_Offset;
        protected int _P2_Y_Offset;
        protected int _P2_OUT_Offset;
        protected string _P2_OUT_NOP_Offset;

        //Triggers
        protected short _P1_Trigger_DIK = 0;
        protected short _P2_Trigger_DIK = 0;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public Game_TtxSha(string RomName, bool Verbose) 
            : base ()
        {
            GetScreenResolution();
            
            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "KSHG_no_cursor";

            ReadGameData();
            ReadKeyConfig();
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
                            //BLock inputs from mouse (P1 controls trigger)
                            ApplyMouseHook();
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
                                    case "P1_X_NOP_OFFSET":
                                        _P1_X_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "P1_Y_NOP_OFFSET":
                                        _P1_Y_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "P1_OUT_OFFSET":
                                        _P1_OUT_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_OUT_NOP_OFFSET":
                                        _P1_OUT_NOP_Offset = buffer[1].Trim();
                                        break;    
                                    case "P2_X_OFFSET":
                                        _P2_X_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_Y_OFFSET":
                                        _P2_Y_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_OUT_OFFSET":
                                        _P2_OUT_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_OUT_NOP_OFFSET":
                                        _P2_OUT_NOP_Offset = buffer[1].Trim();
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


        /// <summary>
        /// Read input config for the game, stored %appdata%\bemani_config\sha_v01.cfg
        /// </summary>
        private void ReadKeyConfig()
        {
            string appData = Environment.GetEnvironmentVariable("appdata").ToString(); ;
            if (File.Exists(appData + @"\bemani_config\sha_v01.cfg"))
            {
                WriteLog("Reading Triggers Keycodes from SHA config file...");
                byte[] fileBytes = File.ReadAllBytes(appData + @"\bemani_config\sha_v01.cfg");
                for (int i = 1; i < fileBytes.Length; i++)
                {
                    if (fileBytes[i] == 0x11 && fileBytes[i - 1] != 0)
                    {
                        _P1_Trigger_DIK = fileBytes[i - 1];
                    }
                    else if (fileBytes[i] == 0x21 && fileBytes[i - 1] != 0)
                    {
                        _P2_Trigger_DIK = fileBytes[i - 1];
                    }
                }
            }
            else
            {
                WriteLog(appData + @"\bemani_config\sha_v01.cfg not found");
            }
            if (_P1_Trigger_DIK != 0)
                WriteLog("Player1 Trigger keycode = 0x" + _P1_Trigger_DIK.ToString("X2"));
            else
                WriteLog("Player1 Trigger keycode not found !");

            if (_P2_Trigger_DIK != 0)
                WriteLog("Player2 Trigger keycode = 0x" + _P2_Trigger_DIK.ToString("X2"));
            else
                WriteLog("Player2 Trigger keycode not found !");
        }

        #endregion       

        #region MemoryHack

        private void SetHack()
        {
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P1_X_NOP_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P1_Y_NOP_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P1_OUT_NOP_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P2_OUT_NOP_Offset);

            //Initialise pour prise en compte des guns direct                
            /*if (_RomName.Equals("sha"))
            {
                byte[] initX = { 0xF7, 0 };
                byte[] initY = { 0xBF, 0 };
                //Write Axis
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, initX);
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, initY);
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, initX);
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, initY);
            }*/
            //SendKeyStroke(_P1_Trigger_DIK, 50);
            //SendKeyStroke(_P2_Trigger_DIK, 50);

            //Set P2 IN_SCREEN
            WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_OUT_Offset, 0x01);
            WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_OUT_Offset, 0x01);
            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        // Mouse callback for low level hook
        protected override IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (UInt32)wParam == Win32.WM_LBUTTONDOWN)
            {
                //Just blocking left clicks
                return new IntPtr(1);
            }
            return Win32.CallNextHookEx(_MouseHookID, nCode, wParam, lParam);
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
                    SendKeyDown(_P1_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    SendKeyUp(_P1_Trigger_DIK);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    //Set out of screen Byte 
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_OUT_Offset, 0x00);
                    //Trigger a shoot to reload !!
                    SendKeyDown(_P1_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    SendKeyUp(_P1_Trigger_DIK);
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P1_OUT_Offset, 0x01);
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
                    SendKeyDown(_P2_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    SendKeyUp(_P2_Trigger_DIK);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    //Set out of screen Byte 
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_OUT_Offset, 0x00);
                    //Trigger a shoot to reload !!
                    SendKeyDown(_P2_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    SendKeyUp(_P2_Trigger_DIK);
                    WriteByte((int)_TargetProcess_MemoryBaseAddress + _P2_OUT_Offset, 0x01);
                }
            }
        }

        #endregion
    }
}
