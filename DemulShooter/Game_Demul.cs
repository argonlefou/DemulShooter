using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace DemulShooter
{
    class Game_Demul : Game
    {
        private const string FOLDER_GAMEDATA = @"MemoryData\demul";
        private const string SYSTEM_NAOMIJVS = "naomiJvs";
        private const string SYSTEM_NAOMI = "naomi";
        
        /*** MEMORY ADDRESSES **/
        protected int _Paddemul_Injection_Offset;
        protected int _Paddemul_Injection_Return_Offset;
        protected int _Paddemul_PtrButtons_Offset;
        protected int _Paddemul_P1_Buttons_Offset;
        protected int _Paddemul_P1_X_Offset;
        protected int _Paddemul_P1_Y_Offset;
        protected int _Paddemul_P2_Buttons_Offset;
        protected int _Paddemul_P2_X_Offset;
        protected int _Paddemul_P2_Y_Offset;
        protected int _P1_Ammo_Address;
        protected int _P2_Ammo_Address;

        protected bool _WidescreenHack;
        private List<WidescreenData> _ListWidescreenHacks;
       
        /*** Process variables **/
        protected IntPtr _PadDemul_ModuleBaseAddress = IntPtr.Zero;

        protected string _SystemName;
        protected string _DemulVersion;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Demul(string Rom, string SystemName, String DemulVersion, bool Verbose, bool DisableWindow, bool WidescreenHack)
            : base()
        {
            GetScreenResolution();
            _RomName = Rom;
            _SystemName = SystemName;
            _DemulVersion = DemulVersion;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _DisableWindow = DisableWindow;
            _WidescreenHack = WidescreenHack;
            _Target_Process_Name = "demul";
            _ListWidescreenHacks = new List<WidescreenData>();

            ReadGameData();

            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for " + _SystemName + "game to hook.....");

            
            if (_WidescreenHack)
            {
                ReadWidescreenData();
            }
        }

        /// <summary>
        /// Timer event when looking for Demul Process (auto-Hook and auto-close)
        /// </summary>
        protected virtual void tProcess_Tick(Object Sender, EventArgs e)
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

                        ProcessModuleCollection c = _TargetProcess.Modules;
                        foreach (ProcessModule m in c)
                        {
                            if (m.ModuleName.ToLower().Equals("paddemul.dll"))
                            {
                                _PadDemul_ModuleBaseAddress = m.BaseAddress;
                                if (_PadDemul_ModuleBaseAddress != IntPtr.Zero)
                                {
                                    _ProcessHooked = true;

                                    if (_DisableWindow)
                                        //Disabling left-click for resize-bug upper left corner
                                        //DisableWindow(true);                                        
                                        ApplyMouseHook();

                                    WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    WriteLog("Demul.exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8") + ", padDemul.dll = 0x" + _PadDemul_ModuleBaseAddress.ToString("X8"));

                                    if (_DemulVersion.Equals("057") || _DemulVersion.Equals("058"))
                                        SetHack_057();
                                    else if (_DemulVersion.Equals("07a"))
                                        SetHack_07();

                                    break;
                                }
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
                else
                {
                    if (_WidescreenHack && _ListWidescreenHacks.Count > 0)
                    {
                        foreach (WidescreenData d in _ListWidescreenHacks)
                        {
                            int RatioValue = BitConverter.ToInt32(ReadBytes(d.Address, 4), 0);
                            if (RatioValue == d.DefaultValue)
                            {
                                WriteBytes(d.Address, BitConverter.GetBytes(d.WidescreenValue));
                                WriteLog("Widescreen Hack : wrote 0x" + d.WidescreenValue.ToString("X8") + " to address 0x" + d.Address.ToString("X8"));
                            }
                        }                        
                    }
                }
            }
        }  

        #region MemoryHack

        /// <summary>
        /// Code injection to block axis/input default reading from emulator
        /// </summary>
        protected virtual void SetHack_057()
        {        
        }
        protected virtual void SetHack_07()
        {                   
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
                    //Demul Window size
                    Win32.Rect TotalRes = new Win32.Rect();
                    Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    /*
                    //pX and pY in width/heigt % of window
                    double Xpercent = Mouse.pTarget.X * 100.0 / TotalResX;
                    double Ypercent = Mouse.pTarget.Y * 100.0 / TotalResY;

                    //0% = 0x00 and 100% = 0xFF for padDemul.dll
                    Mouse.pTarget.X = Convert.ToInt16(Math.Round(Xpercent * 255.0 / 100.0));
                    Mouse.pTarget.Y = Convert.ToInt16(Math.Round(Ypercent * 255.0 / 100.0));
                    */

                    //Naomi + awave => 0000 - 00FF
                    //JVS => 0000 - 0FFF                    
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;
                    if (_SystemName.Equals(SYSTEM_NAOMIJVS))                        
                    {
                        dMaxX = 4095.0;
                        dMaxY = 4095.0;
                    }

                    Mouse.pTarget.X = Convert.ToInt16(Math.Round(dMaxX * Mouse.pTarget.X / TotalResX));
                    Mouse.pTarget.Y = Convert.ToInt16(Math.Round(dMaxY * Mouse.pTarget.Y / TotalResY));
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

        #region I/O FILE

        /// <summary>
        /// Read memory values in .cfg file
        /// </summary>
        protected override void ReadGameData()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\" + _DemulVersion + @"\" + _SystemName + ".cfg"))
            {
                using (StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\" + _DemulVersion + @"\" + _SystemName + ".cfg"))
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
                                    case "PADDEMUL_INJECTION_OFFSET":
                                        _Paddemul_Injection_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "PADDEMUL_INJECTION_RETURN_OFFSET":
                                        _Paddemul_Injection_Return_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "PADDEMUL_PTR_BUTTONS_OFFSET":
                                        _Paddemul_PtrButtons_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "PADDEMUL_P1_BUTTON_OFFSET":
                                        _Paddemul_P1_Buttons_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "PADDEMUL_P1_X_OFFSET":
                                        _Paddemul_P1_X_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "PADDEMUL_P1_Y_OFFSET":
                                        _Paddemul_P1_Y_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "PADDEMUL_P2_BUTTON_OFFSET":
                                        _Paddemul_P2_Buttons_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "PADDEMUL_P2_X_OFFSET":
                                        _Paddemul_P2_X_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "PADDEMUL_P2_Y_OFFSET":
                                        _Paddemul_P2_Y_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_AMMO_ADDRESS":
                                        _P1_Ammo_Address = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_AMMO_ADDRESS":
                                        _P2_Ammo_Address = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
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
                WriteLog("File not found : " + AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\" + _DemulVersion + @"\" + _SystemName + ".cfg");
            }
        }

        /// <summary>
        /// Read Widescreen memory values in .cfg file
        /// </summary>
        private void ReadWidescreenData()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\Widescreen.cfg"))
            {
                _ListWidescreenHacks.Clear();
                using (StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\Widescreen.cfg"))
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
                                if (buffer[0].ToLower().Trim() == _RomName)
                                {
                                    string[] tampon = buffer[1].Split('|');
                                    WidescreenData d = new WidescreenData();
                                    d.Address = int.Parse(tampon[0].Trim().Substring(2), NumberStyles.HexNumber);
                                    d.DefaultValue = int.Parse(tampon[1].Trim().Substring(2), NumberStyles.HexNumber);
                                    d.WidescreenValue = int.Parse(tampon[2].Trim().Substring(2), NumberStyles.HexNumber);
                                    _ListWidescreenHacks.Add(d);
                                    WriteLog("Widescreen hack memory address = 0x" + d.Address.ToString("X8") + " , value = 0x" + d.WidescreenValue.ToString("X8"));
                                }
                            }
                            catch (Exception ex)
                            {
                                WriteLog("Error reading Widescreen data : " + ex.Message.ToString());
                            }
                        }
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }
            }
            else
            {
                WriteLog("File not found : " + AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\" + _DemulVersion + @"\" + _SystemName + ".cfg");
            }
        }
        #endregion        
    
    }

    class WidescreenData
    {
        public int Address;
        public int DefaultValue;
        public int WidescreenValue;

        public WidescreenData()
        {
            Address = 0;
            DefaultValue = 0;
            WidescreenValue = 0;
        }
    }
}
