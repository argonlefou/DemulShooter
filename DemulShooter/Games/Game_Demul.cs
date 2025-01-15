using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_Demul : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\demul";
        private const String SYSTEM_NAOMIJVS = "naomiJvs";
        private const String SYSTEM_NAOMI = "naomi";

        /*** MEMORY ADDRESSES **/
        protected UInt32 _Paddemul_Injection_Offset = 0x0002757A;
        protected UInt32 _Paddemul_Injection_Return_Offset = 0x00027582;
        protected UInt32 _Paddemul_PtrButtons_Offset = 0x00037E30;
        protected UInt32 _Paddemul_P1_Buttons_Offset = 0x00037E32;
        protected UInt32 _Paddemul_P1_X_Offset = 0x00037E34;
        protected UInt32 _Paddemul_P1_Y_Offset = 0x00037E36;
        protected UInt32 _Paddemul_P2_Buttons_Offset = 0x00037EB2;
        protected UInt32 _Paddemul_P2_X_Offset = 0x00037EB4;
        protected UInt32 _Paddemul_P2_Y_Offset = 0x00037EB6;

        private List<WidescreenData> _ListWidescreenHacks;
       
        /*** Process variables **/
        protected IntPtr _PadDemul_ModuleBaseAddress = IntPtr.Zero;

        protected String _SystemName;
        protected String _DemulVersion;

        //Custom Outputs
        protected UInt32 _GameRAM_Address = 0x2C000000; //Demul loads the game RAM at 0x2C000000 address        

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Demul(String RomName, String SystemName, String DemulVersion)
            : base(RomName, "demul")
        {
            _SystemName = SystemName;
            _DemulVersion = DemulVersion;
            _ListWidescreenHacks = new List<WidescreenData>();
            _KnownMd5Prints.Add("Demul 0.7a_180428", "ce0a6fd5552903311a8935b6f60e26ad");
            _KnownMd5Prints.Add("Demul 0.582", "ab4e7654e7b3a4743e9753221cc48fcd");
            _KnownMd5Prints.Add("Demul 0.57", "3071ba77ff46d2137f46bfcd8dda5b4f");

            _tProcess.Start();
            Logger.WriteLog("Waiting for " + _SystemName + "game to hook.....");
            
            if (_WidescreenHack)
            {
                ReadWidescreenData();
            }
        }

        /// <summary>
        /// Timer event when looking for Demul Process (auto-Hook and auto-close)
        /// </summary>
        protected override void tProcess_Elapsed(Object Sender, EventArgs e)
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
                                    _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog("Demul.exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8") + ", padDemul.dll = 0x" + _PadDemul_ModuleBaseAddress.ToString("X8"));
                                    CheckExeMd5();
                                    ReadGameData();

                                    if (!_DisableInputHack)
                                    {
                                        if (_DemulVersion.Equals("057") || _DemulVersion.Equals("058"))
                                            SetHack_057();
                                        else if (_DemulVersion.Equals("07a"))
                                            SetHack_07();
                                    }
                                    else
                                        Logger.WriteLog("Input Hack disabled");

                                    _ProcessHooked = true;
                                    RaiseGameHookedEvent();
                                    break;
                                }
                            }
                        }                              
                    }
                }
                catch
                {
                    Logger.WriteLog("Error trying to hook " + _Target_Process_Name + ".exe");
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
                    Logger.WriteLog(_Target_Process_Name + ".exe closed");
                    Application.Exit();
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
                                Logger.WriteLog("Widescreen Hack : wrote 0x" + d.WidescreenValue.ToString("X8") + " to address 0x" + d.Address.ToString("X8"));
                            }
                        }                        
                    }
                }
            }
        }

        #region Game Configuration

        /// <summary>
        /// Read memory values in .cfg file
        /// </summary>
        protected override void ReadGameData()
        {
            String ConfigFile = AppDomain.CurrentDomain.BaseDirectory + GAMEDATA_FOLDER + @"\" + _DemulVersion + @"\" + _SystemName + ".cfg";            
            if (File.Exists(ConfigFile))
            {
                Logger.WriteLog("Reading game memory setting from " + ConfigFile);
                using (StreamReader sr = new StreamReader(ConfigFile))
                {
                    String line;
                    String FieldName = String.Empty;
                    line = sr.ReadLine();
                    while (line != null)
                    {
                        String[] buffer = line.Split('=');
                        if (buffer.Length > 1)
                        {
                            try
                            {
                                FieldName = "_" + buffer[0].Trim();
                                if (buffer[0].Contains("Nop"))
                                {
                                    NopStruct n = new NopStruct(buffer[1].Trim());
                                    this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, n);
                                }
                                else
                                {
                                    UInt32 v = UInt32.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                    this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, v);
                                } 
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog("Error reading game data : " + ex.Message.ToString());
                            }
                        }
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }
            }
            else
            {
                Logger.WriteLog("File not found : " + AppDomain.CurrentDomain.BaseDirectory + @"\" + GAMEDATA_FOLDER + @"\" + _DemulVersion + @"\" + _SystemName + ".cfg");
            }
        }

        /// <summary>
        /// Read Widescreen memory values in .cfg file
        /// </summary>
        private void ReadWidescreenData()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\" + GAMEDATA_FOLDER + @"\Widescreen.cfg"))
            {
                _ListWidescreenHacks.Clear();
                using (StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\" + GAMEDATA_FOLDER + @"\Widescreen.cfg"))
                {
                    String line;
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
                                    String[] tampon = buffer[1].Split('|');
                                    WidescreenData d = new WidescreenData();
                                    d.Address = UInt32.Parse(tampon[0].Trim().Substring(2), NumberStyles.HexNumber);
                                    d.DefaultValue = Int32.Parse(tampon[1].Trim().Substring(2), NumberStyles.HexNumber);
                                    d.WidescreenValue = Int32.Parse(tampon[2].Trim().Substring(2), NumberStyles.HexNumber);
                                    _ListWidescreenHacks.Add(d);
                                    Logger.WriteLog("Widescreen hack memory address = 0x" + d.Address.ToString("X8") + " , value = 0x" + d.WidescreenValue.ToString("X8"));
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog("Error reading Widescreen data : " + ex.Message.ToString());
                            }
                        }
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }
            }
            else
            {
                Logger.WriteLog("File not found : " + AppDomain.CurrentDomain.BaseDirectory + @"\" + GAMEDATA_FOLDER + @"\" + _DemulVersion + @"\" + _SystemName + ".cfg");
            }
        }

        #endregion    

        #region Screen

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// </summary>
        public override bool GameScale(PlayerSettings PlayerData)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    /*
                    //pX and pY in width/heigt % of window
                    double Xpercent = Mouse.pTarget.X * 100.0 / TotalResX;
                    double Ypercent = Mouse.pTarget.Y * 100.0 / TotalResY;

                    //0% = 0x00 and 100% = 0xFF for padDemul.dll
                    Mouse.pTarget.X = Convert.ToInt16(Math.Round(Xpercent * 255.0 / 100.0));
                    Mouse.pTarget.Y = Convert.ToInt16(Math.Round(Ypercent * 255.0 / 100.0));
                    */

                    //Naomi + awave => 0000 - 00FF                                      
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;
                                        
                    //JVS => 0000 - 0FFF  and inverted Y
                    if (_SystemName.Equals(SYSTEM_NAOMIJVS))
                    {
                        dMaxX = 4095.0;
                        dMaxY = 4095.0;
                        PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));                    
                        PlayerData.RIController.Computed_Y = Convert.ToInt16(dMaxY - Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));
                    }
                    else
                    {
                        PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                        PlayerData.RIController.Computed_Y = Convert.ToInt16(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));
                    }                    
                                        
                    if (PlayerData.RIController.Computed_X < 0)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 0)
                        PlayerData.RIController.Computed_Y = 0;
                    if (PlayerData.RIController.Computed_X > (int)dMaxX)
                        PlayerData.RIController.Computed_X = (int)dMaxX;
                    if (PlayerData.RIController.Computed_Y > (int)dMaxY)
                        PlayerData.RIController.Computed_Y = (int)dMaxY;

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Error scaling mouse coordonates to GameFormat : " + ex.Message.ToString());
                }
            }
            return false;
        }

        #endregion

        #region Memory Hack

        /// <summary>
        /// Code injection to block axis/input default reading from emulator
        /// </summary>
        protected virtual void SetHack_057()
        {        
        }
        protected virtual void SetHack_07()
        {                   
        }        

        #endregion

        #region Inputs

        /// <summary>
        ///  Mouse callback for low level hook
        ///  This is used to block LeftClick events on the window, because double clicking on the upper-left corner
        ///  makes demul switch from Fullscreen to Windowed mode
        /// </summary>        
        public override IntPtr MouseHookCallback(IntPtr MouseHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (UInt32)wParam == Win32Define.WM_LBUTTONDOWN)
            {
                //Just blocking left clicks
                if (_DisableWindow)
                    return new IntPtr(1);
            }
            return Win32API.CallNextHookEx(MouseHookID, nCode, wParam, lParam);
        }

        #endregion
    }

    class WidescreenData
    {
        public UInt32 Address;
        public Int32 DefaultValue;
        public Int32 WidescreenValue;

        public WidescreenData()
        {
            Address = 0;
            DefaultValue = 0;
            WidescreenValue = 0;
        }
    }
}
