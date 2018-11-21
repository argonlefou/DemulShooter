using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Media;
using System.Runtime.InteropServices;

namespace DemulShooter
{
    class Game_Hod3pc : Game
    {
        private const string FOLDER_GAMEDATA = @"MemoryData\windows";

        /*** MEMORY ADDRESSES **/
        protected int _P1_X_Address;
        protected int _P1_Y_Address;
        protected int _P2_X_Address;
        protected int _P2_Y_Address;
        protected string _X_NOP_Offset;
        protected string _Y_NOP_Offset;
        protected string _Arcade_Mode_Display_NOP_Offset = "0x0008FD29|2";

        //Keys
        protected short _P1_Trigger_DIK = 0x2D;
        protected short _P1_Reload_DIK = 0x2C;
        protected short _P2_Trigger_DIK = 0x31;
        protected short _P2_Reload_DIK = 0x30;
        protected short _P1_Right_DIK = 0x22;
        protected short _P1_Left_DIK = 0x20;
        protected short _P2_Right_DIK = 0x26;
        protected short _P2_Left_DIK = 0x24;

        //For multi-route selection : midle click = left or write each time
        private string P1_Next_Dir = "left";
        private string P2_Next_Dir = "left";

        private bool _NoAutoReload = false;
        private bool _ArcadeModeDisplay = false;

        //Play the "Coins" sound when adding coin
        SoundPlayer _SndPlayer;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Hod3pc(string RomName, bool NoAutoReload, bool ArcadeModeDisplay, bool Verbose) 
            : base ()
        {
            GetScreenResolution();
            
            _RomName = RomName;
            _NoAutoReload = NoAutoReload;
            _ArcadeModeDisplay = ArcadeModeDisplay;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "hod3pc";

            ReadGameData();
            ReadKeyConfig();

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
                            _ProcessHooked = true;
                            WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            SetHack();
                            ApplyMouseHook();
                            ApplyKeyboardHook();
                            try
                            {
                                String strCoinSndPath = _TargetProcess.MainModule.FileName;
                                strCoinSndPath = strCoinSndPath.Substring(0, strCoinSndPath.Length - 10);
                                strCoinSndPath += @"..\media\coin002.aif";
                                _SndPlayer = new SoundPlayer(strCoinSndPath);
                            }
                            catch
                            {
                                WriteLog("Unable to find/open the coin002.aif file for Hotd2");
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
                                    case "P1_X_ADDRESS":
                                        _P1_X_Address = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_Y_ADDRESS":
                                        _P1_Y_Address = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_X_ADDRESS":
                                        _P2_X_Address = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_Y_ADDRESS":
                                        _P2_Y_Address = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "X_NOP_OFFSET":
                                        _X_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "Y_NOP_OFFSET":
                                        _Y_NOP_Offset = buffer[1].Trim();
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
            WriteLog("Reading Keyconfig...");

            GetHOD3RegValue(ref _P1_Trigger_DIK, "Keyboard1pShoot");
            GetHOD3RegValue(ref _P1_Reload_DIK, "Keyboard1pReload");
            GetHOD3RegValue(ref _P1_Right_DIK, "Keyboard1pRight");
            GetHOD3RegValue(ref _P1_Left_DIK, "Keyboard1pLeft");
            GetHOD3RegValue(ref _P2_Trigger_DIK, "Keyboard2pShoot");
            GetHOD3RegValue(ref _P2_Reload_DIK, "Keyboard2pReload");
            GetHOD3RegValue(ref _P2_Right_DIK, "Keyboard2pRight");
            GetHOD3RegValue(ref _P2_Left_DIK, "Keyboard2pLeft");

            WriteLog("P1_Trigger keycode = 0x" + _P1_Trigger_DIK.ToString("X2"));
            WriteLog("P1_Reload keycode = 0x" + _P1_Reload_DIK.ToString("X2"));
            WriteLog("P1_Right keycode = 0x" + _P1_Right_DIK.ToString("X2"));
            WriteLog("P1_Left keycode = 0x" + _P1_Left_DIK.ToString("X2"));
            WriteLog("P2_Trigger keycode = 0x" + _P2_Trigger_DIK.ToString("X2"));
            WriteLog("P2_Reload keycode = 0x" + _P2_Reload_DIK.ToString("X2"));
            WriteLog("P2_Right keycode = 0x" + _P2_Right_DIK.ToString("X2"));
            WriteLog("P2_Left keycode = 0x" + _P2_Left_DIK.ToString("X2"));
        }
        private void GetHOD3RegValue(ref short DataToFill, string Value)
        {
            string Key1 = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\SEGA\hod3\Settings";
            string Key2 = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\SEGA\hod3\Settings2";
            if (Registry.GetValue(Key1, Value, null) != null)
            {
                try
                {
                    DataToFill = Convert.ToInt16((int)Registry.GetValue(Key1, Value, null));
                }
                catch
                {
                    WriteLog("Can't read registry value : " + Key1 + "\\" + Value);
                }
            }
            else
            {
                WriteLog("Registry value : " + Key1 + "\\" + Value + " not found !");
                if (Registry.GetValue(Key2, Value, null) != null)
                {
                    try
                    {
                        DataToFill = Convert.ToInt16((int)Registry.GetValue(Key2, Value, null));
                    }
                    catch
                    {
                        WriteLog("Can't read registry value : " + Key2 + "\\" + Value);
                    }
                }
                else
                {
                    WriteLog("Registry value : " + Key2 + "\\" + Value + " not found !");
                }
            }
        }

        #endregion

        #region MemoryHack

        private void SetHack()
        {
            SetNops((int)_TargetProcess_MemoryBaseAddress, _X_NOP_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _Y_NOP_Offset);

            if (_NoAutoReload)
            {
                SetNops((int)_TargetProcess_MemoryBaseAddress, "0008DEDB|3");
                SetNops((int)_TargetProcess_MemoryBaseAddress, "0008DF1E|3");
                WriteLog("NoAutoReload Hack done");
            }

            //Hide guns at screen, like real arcade machine
            if (_ArcadeModeDisplay)
            {
                SetNops((int)_TargetProcess_MemoryBaseAddress, _Arcade_Mode_Display_NOP_Offset);
            }

            //Initialise pour prise en compte des guns direct                
            WriteBytes(_P1_X_Address, new byte[] { 0x00, 0x00 });
            WriteBytes(_P1_Y_Address, new byte[] { 0x00, 0x00 });
            WriteBytes(_P2_X_Address, new byte[] { 0x00, 0x00 });
            WriteBytes(_P2_Y_Address, new byte[] { 0x00, 0x00 });

            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        // Mouse callback for low level hook
        protected override IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (UInt32)wParam == Win32.WM_MBUTTONDOWN)
            {
                //Just blocking middle clicks                
                return new IntPtr(1);
            }
            else if (nCode >= 0 && (UInt32)wParam == Win32.WM_RBUTTONDOWN)
            {
                //Just blocking right clicks => if not P1 reload play animation but does not reload
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
                WriteBytes(_P1_X_Address, bufferX);
                WriteBytes(_P1_Y_Address, bufferY);
                
                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    SendKeyDown(_P1_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    SendKeyUp(_P1_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    if (P1_Next_Dir.Equals("left"))
                        SendKeyDown(_P1_Left_DIK);
                    else
                        SendKeyDown(_P1_Right_DIK);    
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    if (P1_Next_Dir.Equals("left"))
                    {
                        SendKeyUp(_P1_Left_DIK);
                        P1_Next_Dir = "right";
                    }
                    else
                    {
                        SendKeyUp(_P1_Right_DIK);
                        P1_Next_Dir = "left";
                    }  
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    SendKeyDown(_P1_Reload_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    SendKeyUp(_P1_Reload_DIK);
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
                    SendKeyDown(_P2_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    SendKeyUp(_P2_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    if (P2_Next_Dir.Equals("left"))
                        SendKeyDown(_P2_Left_DIK);
                    else
                        SendKeyDown(_P2_Right_DIK);    
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    if (P2_Next_Dir.Equals("left"))
                    {
                        SendKeyUp(_P2_Left_DIK);
                        P2_Next_Dir = "right";
                    }
                    else
                    {
                        SendKeyUp(_P2_Right_DIK);
                        P2_Next_Dir = "left";
                    }  
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
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

                    //X => [FED8-0128] = 592
                    //Y => [00D8-FF28] = 432
                    double dMaxX = 592.0;
                    double dMaxY = 432.0;

                    Mouse.pTarget.X = Convert.ToInt16(Math.Round(dMaxX * Mouse.pTarget.X / TotalResX) - dMaxX / 2);
                    Mouse.pTarget.Y = Convert.ToInt16((Math.Round(dMaxY * Mouse.pTarget.Y / TotalResY) - dMaxY / 2) * -1);
                    if (Mouse.pTarget.X < -296)
                        Mouse.pTarget.X = -296;
                    if (Mouse.pTarget.Y < -216)
                        Mouse.pTarget.Y = -216;
                    if (Mouse.pTarget.X > 296)
                        Mouse.pTarget.X = 296;
                    if (Mouse.pTarget.Y > 216)
                        Mouse.pTarget.Y = 216;

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

        // Keyboard callback used for adding coins
        protected override IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                Win32.KBDLLHOOKSTRUCT s = (Win32.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32.WM_KEYDOWN)
                {
                    if (s.scanCode == 0x06 /* [5] Key */)
                    {
                        byte Credits = ReadByte((int)_TargetProcess_MemoryBaseAddress + 0x3B7DD0);
                        Credits++;
                        WriteByte((int)_TargetProcess_MemoryBaseAddress + 0x3B7DD0, Credits);
                        if (_SndPlayer != null)
                            _SndPlayer.Play();
                    }
                }
            }
            return Win32.CallNextHookEx(_KeyboardHookID, nCode, wParam, lParam);
        } 
    }
}
