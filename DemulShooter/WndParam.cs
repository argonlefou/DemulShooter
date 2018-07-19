using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Security.Cryptography;

namespace DemulShooter
{    
    public partial class WndParam : Form
    {
        private static WndParam _This;
        private const int MAX_CONTROLLERS = 4;

        /*** RAWINPUT data ***/        
        private List<MouseInfo> _MiceList;
        public List<MouseInfo> MiceList
        {
            get { return _MiceList; }
        }
        private int _Act_Labs_Offset_Enable = 0;
        const Int32 INPUT_ABSOLUTE_MIN = 0;
        const Int32 INPUT_ABSOLUTE_MAX = 65536;  
      
        /*** XInput Controllers data ***/
        private const String XINPUT_DEVICE_PREFIX = "XInput Gamepad #";
        private XInputState[] _XInputStates;        
        private int _XInput_PollingInterval = 20; //ms
       
        /**** Directinput data ***/        
        private IntPtr _KeyboardHookID;
        private Win32.HookProc _KeyboardHookProc;        
        private byte _Di_Sha_Exit;
        private byte _Di_Sha_Test;
        private byte _Di_Sha_Service;
        private byte _Di_Sha_P1_S;
        private byte _Di_Sha_P1_T;
        private byte _Di_Sha_P2_S;
        private byte _Di_Sha_P2_T;
        private byte _Di_Crosshair_P1 = 0x08;
        private byte _Di_Crosshair_P2 = 0x09;
        private byte _Di_Crosshair_Visibility = 0x0A;
        private byte _Di_Gsoz_Pedal_P1 = 0x22;  //G
        private byte _Di_Gsoz_Pedal_P2 = 0x23;  //H
        private bool _Start_KeyRecord = false;
        public bool Start_KeyRecord
        {
            set { _Start_KeyRecord = value; }
        }
        private TextBox _Txtbox;
        private String _sTxtbox = string.Empty;

        private int _Gsoz_Pedal_P1_Enabled = 0;
        private int _Gsoz_Pedal_P2_Enabled = 0;

        /*** Controllers ***/
        private List<ControllerDevice> _ControllerDevices;
        private List<Uc_GUI_PlayerDevice> _GUI_PlayerDevices;

        /*** Main varables ***/
        private const string CONF_FILENAME = "config.ini";
        private const string SHA_CONF_FILEPATH = @"\bemani_config\sha_v01.cfg";
        private const string LOG_FILENAME = "debug.txt";
        private bool _VerboseEnable = false;
        private string _Rom = String.Empty;
        private string _Target = String.Empty;
        private int _Ddinumber = 3;
        private string _DemulVersion = String.Empty;
        private bool _DisableWindow = false;
        private bool _WidescreenHack = false;
        private bool _NoAutoReload = false;
        private bool _HardFfl = false;
        private bool _ParrotLoader = false;
        private string _HF3_Path = string.Empty;
        private int _HF3_CoverSensibility = 3;
        private string _HF4_Path = string.Empty;
        private int _HF4_CoverSensibility = 3;
        private bool _HideGameCrosshair = false;
        private Game _Game;
        private ContextMenu _TrayIconMenu;
        private bool _Has_ExplorerExe = false;
        private System.Windows.Forms.NotifyIcon _TrayIcon;
        private Timer CrosshairAimTimer;

        private const int PROCESS_WM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_OPERATION = 0x0008;

        /// <summary>
        /// Construcor
        /// </summary>
        public WndParam(bool VerboseEnable)
        {           
            InitializeComponent();

            _This = this;

            //Stop program if Demulshooter already running
            Process[] pDemulShooter = Process.GetProcessesByName("DemulShooter");
            if (pDemulShooter.Length > 1)
            {
                MessageBox.Show("Another instance of DemulShooter is already running.\nPlease terminate it before launching a new one", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
            
            //Tray Icon
            //Looking for explorer.exe process, if not skip TrayIcon to make the program work
            Process[] pExplorer = Process.GetProcessesByName("explorer");
            if (pExplorer.Length > 0)
            {
                _TrayIcon = new NotifyIcon();
                _TrayIcon.Text = "DemulShooter";
                _TrayIcon.Icon = DemulShooter.Properties.Resources.DemulShooter_Icon;
                _TrayIconMenu = new ContextMenu();                
                _TrayIconMenu.MenuItems.Add("Exit", OnExit);
                _TrayIcon.ContextMenu = _TrayIconMenu;
                _Has_ExplorerExe = true;
            }

            // Retrieve all rawinput devices
            _MiceList = new List<MouseInfo>();
            GetRawInputDevices();

            //Init Devices list
            _ControllerDevices = new List<ControllerDevice>();
            for (int i = 0; i < MAX_CONTROLLERS; i++)
            {
                ControllerDevice c = new ControllerDevice(i+1, this);
                c.InitVibrationTimer();
                _ControllerDevices.Add(c);
            }

            // Create P1-P4 GUI settings tab page
            // Each page is linked with it's player real device class
            _GUI_PlayerDevices = new List<Uc_GUI_PlayerDevice>();
            for (int i = 0; i < MAX_CONTROLLERS; i++)
            {
                Uc_GUI_PlayerDevice PlayerDevice = new Uc_GUI_PlayerDevice(_ControllerDevices[i], this);
                _GUI_PlayerDevices.Add(PlayerDevice);
                tabControl1.TabPages[i].Controls.Add(PlayerDevice);
            }            
            
            //Fill Controllers combobox
            foreach (MouseInfo mouse in _MiceList)
            {
                _GUI_PlayerDevices[0].AddDevice(mouse.devName);
                _GUI_PlayerDevices[1].AddDevice(mouse.devName);
                _GUI_PlayerDevices[2].AddDevice(mouse.devName);
                _GUI_PlayerDevices[3].AddDevice(mouse.devName);
            }
            //Second step : add XInput Gamepads
            _XInputStates = new XInputState[XInputConstants.MAX_CONTROLLER_COUNT];
            for (int i = XInputConstants.FIRST_CONTROLLER_INDEX; i < XInputConstants.MAX_CONTROLLER_COUNT; i++)
            {
                XInputCapabilities capabilities = new XInputCapabilities();
                int ret = XInput.XInputGetCapabilities(i, XInputConstants.XINPUT_FLAG_GAMEPAD, ref capabilities);
                if (ret == XInputConstants.ERROR_SUCCES)
                {
                    if (XInput.XInputGetState(i, ref _XInputStates[i]) == XInputConstants.ERROR_SUCCES)
                    {
                        _GUI_PlayerDevices[0].AddDevice(XINPUT_DEVICE_PREFIX + (i + 1));
                        _GUI_PlayerDevices[1].AddDevice(XINPUT_DEVICE_PREFIX + (i + 1));
                        _GUI_PlayerDevices[2].AddDevice(XINPUT_DEVICE_PREFIX + (i + 1));
                        _GUI_PlayerDevices[3].AddDevice(XINPUT_DEVICE_PREFIX + (i + 1));
                    }
                }
            }

            // Reading conf file
            ReadConf();

            // Refresh GUI with config values
            foreach (Uc_GUI_PlayerDevice GuiDevice in _GUI_PlayerDevices)
            {
                GuiDevice.RefreshSettings();
            }
            Txt_ActLabs_X1.Text = _ControllerDevices[0].Act_Labs_OffsetX.ToString();
            Txt_ActLabs_X2.Text = _ControllerDevices[1].Act_Labs_OffsetX.ToString();
            Txt_ActLabs_X3.Text = _ControllerDevices[2].Act_Labs_OffsetX.ToString();
            Txt_ActLabs_X4.Text = _ControllerDevices[3].Act_Labs_OffsetX.ToString();
            Txt_ActLabs_Y1.Text = _ControllerDevices[0].Act_Labs_OffsetY.ToString();
            Txt_ActLabs_Y2.Text = _ControllerDevices[1].Act_Labs_OffsetY.ToString();
            Txt_ActLabs_Y3.Text = _ControllerDevices[2].Act_Labs_OffsetY.ToString();
            Txt_ActLabs_Y4.Text = _ControllerDevices[3].Act_Labs_OffsetY.ToString();

            CrosshairAimTimer = new Timer();
            CrosshairAimTimer.Tick += new EventHandler(CrosshairAimTimer_Tick);                   
            
            //Look for cmdline arguments :
            //If no arguments, opening the GUI window
            //If arguments, running in command-line mode for gaming
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                _VerboseEnable = VerboseEnable;

                WriteLog("");
                WriteLog("-------------------- Program Start ----------------------");

                foreach (ControllerDevice Device in _ControllerDevices)
                {
                    WriteLog("P" + Device.Player + " device = " + Device.DeviceName);
                    if (Device.GamepadID != -1)
                        WriteLog("P" + Device.Player + " Gamepad ID = " + Device.GamepadID);
                    else
                        WriteLog("P" + Device.Player + " Device Handle = " + Device.MouseHandle);
                }

                for (int i = 1; i < args.Length; i++)
                {
                    WriteLog("Cmdline arg " + i + " : " + args[i]);
                    if (args[i].ToLower().StartsWith("-rom"))
                    {
                        _Rom = (args[i].Split('='))[1].Trim();
                    }
                    else if (args[i].ToLower().StartsWith("-target"))
                    {
                        _Target = (args[i].Split('='))[1].Trim();
                        if (_Target.StartsWith("demul"))
                        {
                            _DemulVersion = _Target.Substring(5, 3);
                        }
                    }
                    else if (args[i].ToLower().Equals("-noresize"))
                    {
                        _DisableWindow = true;
                    }
                    else if (args[i].ToLower().Equals("-widescreen"))
                    {
                        _WidescreenHack = true;
                    }
                    else if (args[i].ToLower().Equals("-noautoreload"))
                    {
                        _NoAutoReload = true;
                    }
                    else if (args[i].ToLower().Equals("-nocrosshair"))
                    {
                        _HideGameCrosshair = true;
                    }
                    else if (args[i].ToLower().Equals("-hardffl"))
                    {
                        _HardFfl = true;
                    }
                    else if (args[i].ToLower().Equals("-parrotloader"))
                    {
                        _ParrotLoader = true;
                    }
                    else if (args[i].ToLower().StartsWith("-ddinumber"))
                    {
                        try
                        {
                            //-1 to transform to a 0-based index for later calculation
                            _Ddinumber = int.Parse((args[i].Split('='))[1].Trim()) - 1;
                        }
                        catch
                        {
                            WriteLog("-ddinumber parameter not good, it will keep default value");
                        }
                    }
                }
                if (_Target.Length > 0 && (_Rom.Length > 0 || _Target.StartsWith("dolphin")))
                {
                    SetVisibility(false);

                    //Install Low Level keyboard hook to detect virtual buttons if set in config
                    bool EnableVirtualMiddleProc = false;
                    foreach (ControllerDevice Device in _ControllerDevices)
                    {
                        if (Device.EnableVirtualMiddleClick != 0)
                        {
                            EnableVirtualMiddleProc = true;
                            WriteLog("Enabling virtual Middle Mouse button: P" + Device.Player + "=[" + GetKeyStringFromScanCode(Device.DiK_VirtualMiddleButton) + "]");
                        }
                    }
                    if (EnableVirtualMiddleProc)
                    {
                        _KeyboardHookProc = new Win32.HookProc(VirtualButtonsKeyboardHookCallback);
                        using (Process curProcess = Process.GetCurrentProcess())
                        using (ProcessModule curModule = curProcess.MainModule)
                        _KeyboardHookID = Win32.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, _KeyboardHookProc, Win32.GetModuleHandle(curModule.ModuleName), 0);                           
                    }            

                    //Running Xinput daemon if needed
                    bool EnableXInputProc = false;
                    foreach (ControllerDevice Device in _ControllerDevices)
                    {
                        if (Device.GamepadID != -1)
                        {
                            EnableXInputProc = true;
                            break;
                        }
                    }
                    if (EnableXInputProc)
                        Bgw_XInput.RunWorkerAsync();

                    //All model2 roms share same method
                    if (_Target.StartsWith("model2"))
                    {
                        _Game = new Game_Model2(_Rom.ToLower(), _Target, _VerboseEnable);
                    }
                    //TTX game
                    else if (_Target.Equals("ttx"))
                    {
                        switch (_Rom.ToLower())
                        {
                            case "sha":
                                {
                                    _Game = new Game_TtxSha(_Rom.ToLower(), _VerboseEnable);
                                } break;
                            case "eadp":
                                {
                                    _Game = new Game_TtxEadp(_Rom.ToLower(), _VerboseEnable);
                                } break;
                            case "gattack4":
                                {
                                    _Game = new Game_TtxGaiaAttack4(_Rom.ToLower(), _VerboseEnable);
                                } break;
                            case "gsoz":
                                {
                                    _Game = new Game_TtxGundam(_Rom.ToLower(), _Gsoz_Pedal_P1_Enabled, _Di_Gsoz_Pedal_P1, _Gsoz_Pedal_P2_Enabled, _Di_Gsoz_Pedal_P2, _VerboseEnable);
                                } break;
                            case "gsoz2p":
                                {
                                    if (_Gsoz_Pedal_P1_Enabled != 0 || _Gsoz_Pedal_P2_Enabled != 00)
                                    {
                                        WriteLog("Enabling DirectX keyboard registering for Pedal(s)");
                                        //RegisterDInputKeyboard();
                                    }
                                    _Game = new Game_TtxGundam(_Rom.ToLower(), _Gsoz_Pedal_P1_Enabled, _Di_Gsoz_Pedal_P1, _Gsoz_Pedal_P2_Enabled, _Di_Gsoz_Pedal_P2, _VerboseEnable);
                                } break;
                            case "hmuseum":
                                {
                                    _Game = new Game_TtxHauntedMuseum(_Rom.ToLower(), _VerboseEnable);
                                } break;
                            case "hmuseum2":
                                {
                                    _Game = new Game_TtxHauntedMuseum2(_Rom.ToLower(), _HardFfl, _VerboseEnable);
                                } break;
                            case "mgungun2":
                                {
                                    _Game = new Game_TtxGungun2(_Rom.ToLower(), _VerboseEnable);
                                } break;
                        }                       
                    }

                    //GlobalVR game
                    else if (_Target.Equals("globalvr"))
                    {
                       switch (_Rom.ToLower())
                        {
                            case "alienshasp":
                                {
                                    _Game = new Game_GvrAliensHasp(_Rom.ToLower(), _VerboseEnable);
                                }break;
                            case "aliens":
                                {
                                    _Game = new Game_GvrAliens(_Rom.ToLower(), _VerboseEnable);
                                } break;
                            case "farcry":
                                {
                                    _Game = new Game_GvrFarCry(_Rom.ToLower(), _VerboseEnable);
                                } break;
                              case "fearland":
                                {
                                    _Game = new Game_GvrFearLand(_Rom.ToLower(), _HardFfl, _VerboseEnable);
                                }break;                              
                              default: 
                                  break;
                       }
                    }

                    //Ring system
                    else if (_Target.Equals("ringwide"))
                    {
                        switch (_Rom.ToLower())
                        {
                            case "sgg":
                                {
                                    _Game = new Game_RwSGGv1(_Rom.ToLower(), _ParrotLoader, _VerboseEnable);
                                } break;
                            case "lgi":
                                {
                                    _Game = new Game_RwLGI(_Rom.ToLower(), _ParrotLoader, _VerboseEnable);
                                }break;
                            case "lgi3d":
                                {
                                    _Game = new Game_RwLGI3D(_Rom.ToLower(), _ParrotLoader, _VerboseEnable);
                                } break;
                            case "og":
                                {
                                    _Game = new Game_RwOpGhost(_Rom.ToLower(), _ParrotLoader, _VerboseEnable);
                                } break;
                            case "sdr":
                                {
                                    _Game = new Game_RwSDR(_Rom.ToLower(), _ParrotLoader, _VerboseEnable);
                                };break;
                            default:
                                    break;
                        }
                    }

                    //Dolphin
                    else if (_Target.Equals("dolphin4"))
                    {
                        _Game = new Game_Dolphin4(_Rom.ToLower(), _Ddinumber , _VerboseEnable);
                    }
                    else if (_Target.Equals("dolphin5"))
                    {
                        _Game = new Game_Dolphin5(_Rom.ToLower(), _Ddinumber , _VerboseEnable);
                    }

                    //Windows
                    else if (_Target.Equals("windows"))
                    {
                        switch (_Rom.ToLower())
                        {
                            case "artdead":
                                {
                                    _Game = new ArtDead_Game(_Rom.ToLower(), _VerboseEnable);
                                } break;
                            case "hfa":
                                {
                                    _Game = new Game_HeavyFire3Pc("hfa", _HF3_Path, _HF3_CoverSensibility, false, _VerboseEnable);
                                }; break;
                            case "hfa2p":
                                {
                                    _Game = new Game_HeavyFire3Pc("hfa", _HF3_Path, _HF3_CoverSensibility, true, _VerboseEnable);
                                }; break;
                            case "hfa_s":
                                {
                                    _Game = new Game_HeavyFire3Pc("hfa_s", _HF3_Path, _HF3_CoverSensibility, false, _VerboseEnable);
                                }; break;
                            case "hfa2p_s":
                                {
                                    _Game = new Game_HeavyFire3Pc("hfa_s", _HF3_Path, _HF3_CoverSensibility, false, _VerboseEnable);
                                }; break;
                            case "hfss":
                                {
                                    _Game = new Game_HeavyFire4Pc("hfss", _HF4_Path, _HF4_CoverSensibility, false, _VerboseEnable);
                                }; break;
                            case "hfss2p":
                                {
                                    _Game = new Game_HeavyFire4Pc("hfss", _HF4_Path, _HF4_CoverSensibility, true, _VerboseEnable);
                                }; break;
                            case "hfss_s":
                                {
                                    _Game = new Game_HeavyFire4Pc("hfss_s", _HF4_Path, _HF4_CoverSensibility, false, _VerboseEnable);
                                }; break;
                            case "hfss2p_s":
                                {
                                    _Game = new Game_HeavyFire4Pc("hfss_s", _HF4_Path, _HF4_CoverSensibility, false, _VerboseEnable);
                                }; break;
                            /*case "bestate":
                                {
                                    _Game = new Game_BE(_Rom.ToLower(), _VerboseEnable);
                                }break;*/
                            case "hod2pc":
                                {
                                    _Game = new Game_Hod2pc(_Rom.ToLower(), _NoAutoReload, _VerboseEnable);
                                }; break;
                            case "hod3pc":
                                {
                                    _Game = new Game_Hod3pc(_Rom.ToLower(), _NoAutoReload, _VerboseEnable);
                                };break;
                            case "reload":
                                {
                                    _Game = new Game_Reload(_Rom.ToLower(),_HideGameCrosshair ,_VerboseEnable);
                                }; break;
                        }
                    }

                    //TESTING
                    else if (_Target.Equals("wip"))
                    {
                        switch (_Rom.ToLower())
                        {
                            case "bestate":
                                {
                                    _Game = new Game_BE_OLD(_Rom.ToLower(), _VerboseEnable);
                                } break;
                            case "wartran":
                                {
                                    _Game = new Game_Wartran(_Rom.ToLower(), _VerboseEnable);
                                }; break;
                            case "bhapc":
                                {
                                    _Game = new Game_Bhapc(_Rom.ToLower(), _VerboseEnable);
                                }; break;
                        }
                    }
                    //Demul games
                    else
                    {
                        if (_Rom.ToLower().Equals("confmiss") || _Rom.ToLower().Equals("deathcox") || _Rom.ToLower().StartsWith("hotd2")
                            || _Rom.ToLower().Equals("lupinsho") || _Rom.ToLower().Equals("mok") || _Rom.ToLower().Equals("pokasuka"))
                        {
                            _Game = new Game_DemulNaomi(_Rom.ToLower(), _DemulVersion, _VerboseEnable, _DisableWindow, _WidescreenHack);
                        }
                        else if (_Rom.ToLower().StartsWith("ninjaslt"))
                        {
                            _Game = new Game_DemulJvs(_Rom.ToLower(), _DemulVersion, _VerboseEnable, _DisableWindow, _WidescreenHack);
                        }
                        else if (_Rom.ToLower().Equals("braveff"))
                        {
                            _Game = new Game_DemulHikaru(_Rom.ToLower(), _DemulVersion, _VerboseEnable, _DisableWindow, _WidescreenHack);
                        }
                        else
                        {
                            _Game = new Game_DemulAtomiswave(_Rom.ToLower(), _DemulVersion, _VerboseEnable, _DisableWindow, _WidescreenHack);
                        }                            
                    }
                }
                else
                {
                    WriteLog("Not enough parameters, please specify target system and rom");
                    MessageBox.Show("Not enough parameters, please specify target system and rom\n\nFor help : DemulShooter.exe -h" , "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(0);
                }
            }
            else
            {
                SetVisibility(true);
                Cbo_PageSettings.SelectedIndex = 0;
                Read_Sha_Conf();
                Bgw_XInput.RunWorkerAsync();

                //Install Low Level keyboard hook to config desired keyboard keys on GUI
                _KeyboardHookProc = new Win32.HookProc(GuiKeyboardHookCallback);
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                _KeyboardHookID = Win32.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, _KeyboardHookProc, Win32.GetModuleHandle(curModule.ModuleName), 0);               
            }           

            //Register to RawInput
            RawInputDevice[] rid = new RawInputDevice[1];
            rid[0].UsagePage = HidUsagePage.GENERIC;
            rid[0].Usage = HidUsage.Mouse;
            rid[0].Flags = RawInputDeviceFlags.INPUTSINK;
            rid[0].Target = this.Handle;
            if (!Win32.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                MessageBox.Show("Failed to register raw input device(s).", "DemulShooter Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }            
        }

        #region Keyboard Handling

        //Keyboard hook for the GUI part, to detect buttons for config
        private static IntPtr GuiKeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if ((UInt32)wParam == Win32.WM_KEYDOWN)
                {
                    if (_This._Start_KeyRecord)
                    {
                        Win32.KBDLLHOOKSTRUCT s = (Win32.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.KBDLLHOOKSTRUCT));
                        _This._Txtbox.Text = _This.GetKeyStringFromVkCode(s.vkCode);
                        
                        if (_This._Txtbox == _This.TXT_P1_S)
                            _This._Di_Sha_P1_S = (byte)s.scanCode;
                        else if (_This._Txtbox == _This.TXT_P1_T)
                            _This._Di_Sha_P1_T = (byte)s.scanCode;
                        else if (_This._Txtbox == _This.TXT_P2_S)
                            _This._Di_Sha_P2_S = (byte)s.scanCode;
                        else if (_This._Txtbox == _This.TXT_P2_T)
                            _This._Di_Sha_P2_T = (byte)s.scanCode;
                        else if (_This._Txtbox == _This.TXT_EXIT)
                            _This._Di_Sha_Exit = (byte)s.scanCode;
                        else if (_This._Txtbox == _This.TXT_TEST)
                            _This._Di_Sha_Test = (byte)s.scanCode;
                        else if (_This._Txtbox == _This.TXT_SERVICE)
                            _This._Di_Sha_Service = (byte)s.scanCode;
                        else if (_This._Txtbox == _This.TXT_CH_P1)
                            _This._Di_Crosshair_P1 = (byte)s.scanCode;
                        else if (_This._Txtbox == _This.TXT_CH_P2)
                            _This._Di_Crosshair_P2 = (byte)s.scanCode;
                        else if (_This._Txtbox == _This.TXT_CH_VIS)
                            _This._Di_Crosshair_Visibility = (byte)s.scanCode;
                        else if (_This._Txtbox == _This.TXT_GSOZ_PEDAL_1)
                            _This._Di_Gsoz_Pedal_P1 = (byte)s.scanCode;
                        else if (_This._Txtbox == _This.TXT_GSOZ_PEDAL_2)
                            _This._Di_Gsoz_Pedal_P2 = (byte)s.scanCode;
                        else if (_This._Txtbox == _This._GUI_PlayerDevices[0].VirtualMiddleButton)
                            _This._ControllerDevices[0].DiK_VirtualMiddleButton = (byte)s.scanCode;
                        else if (_This._Txtbox == _This._GUI_PlayerDevices[1].VirtualMiddleButton)
                            _This._ControllerDevices[1].DiK_VirtualMiddleButton = (byte)s.scanCode;
                        else if (_This._Txtbox == _This._GUI_PlayerDevices[2].VirtualMiddleButton)
                            _This._ControllerDevices[2].DiK_VirtualMiddleButton = (byte)s.scanCode;
                        else if (_This._Txtbox == _This._GUI_PlayerDevices[3].VirtualMiddleButton)
                            _This._ControllerDevices[3].DiK_VirtualMiddleButton = (byte)s.scanCode;
                        
                        _This._Txtbox = null;
                        _This._Start_KeyRecord = false;

                        return new IntPtr(1);
                    }
                }
            }
            return Win32.CallNextHookEx(_This._KeyboardHookID, nCode, wParam, lParam);
        }

        //Keyboard hook for demulshooter's running part
        //This one is used to detect virtual Middle Buttons
        private static IntPtr VirtualButtonsKeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0  && _This._Game.ProcessHooked)
            {
                if ((UInt32)wParam == Win32.WM_KEYDOWN)
                {
                    Win32.KBDLLHOOKSTRUCT s = (Win32.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.KBDLLHOOKSTRUCT));
                    foreach (ControllerDevice Device in _This._ControllerDevices)
                    {
                        if (s.scanCode == Device.DiK_VirtualMiddleButton && Device.EnableVirtualMiddleClick != 0)
                        {
                            if (Device.LastMouseInfo == null)
                                Device.LastMouseInfo = new MouseInfo();
                            Device.LastMouseInfo.button = Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN;
                            _This._Game.SendInput(Device.LastMouseInfo, Device.Player);
                        }
                    }
                }
                if ((UInt32)wParam == Win32.WM_KEYUP)
                {
                    Win32.KBDLLHOOKSTRUCT s = (Win32.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.KBDLLHOOKSTRUCT));
                    foreach (ControllerDevice Device in _This._ControllerDevices)
                    {
                        if (s.scanCode == Device.DiK_VirtualMiddleButton && Device.EnableVirtualMiddleClick != 0)
                        {
                            if (Device.LastMouseInfo == null)
                                Device.LastMouseInfo = new MouseInfo();
                            Device.LastMouseInfo.button = Win32.RI_MOUSE_MIDDLE_BUTTON_UP;
                            _This._Game.SendInput(Device.LastMouseInfo, Device.Player);
                        }
                    } 
                }
            }
            return Win32.CallNextHookEx(_This._KeyboardHookID, nCode, wParam, lParam);
        }

        private String GetKeyStringFromScanCode(int ScanCode)
        {
            uint Vk =Win32.MapVirtualKey((uint)ScanCode, Win32.MAPVK_VSC_TO_VK);
            return GetKeyStringFromVkCode((int)Vk);
        }

        private String GetKeyStringFromVkCode(int vkCode)
        {
            KeysConverter kc = new KeysConverter();
            return kc.ConvertToString((Keys)vkCode);
        }

        #endregion

        #region RAW_INPUT

        /// <summary>
        /// Enumerates the Raw Input Devices and places their corresponding RawInputDevice structures into a List<string>
        /// </summary>
        private void GetRawInputDevices()
        {
            _MiceList.Clear();
            uint deviceCount = 0;
		    var dwSize = (Marshal.SizeOf(typeof(Rawinputdevicelist)));

			if (Win32.GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, (uint)dwSize) == 0)
			{
				var pRawInputDeviceList = Marshal.AllocHGlobal((int)(dwSize * deviceCount));
				Win32.GetRawInputDeviceList(pRawInputDeviceList, ref deviceCount, (uint)dwSize);

				for (var i = 0; i < deviceCount; i++)
				{
					uint pcbSize = 0;
                    // On Window 8 64bit when compiling against .Net > 3.5 using .ToInt32 you will generate an arithmetic overflow. Leave as it is for 32bit/64bit applications
					var rid = (Rawinputdevicelist)Marshal.PtrToStructure(new IntPtr((pRawInputDeviceList.ToInt64() + (dwSize * i))), typeof(Rawinputdevicelist));
                    if (rid.dwType == DeviceType.RimTypemouse)
                    {
                        Win32.GetRawInputDeviceInfo(rid.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);
                        if (pcbSize <= 0) continue;

                        var pData = Marshal.AllocHGlobal((int)pcbSize);
                        Win32.GetRawInputDeviceInfo(rid.hDevice, RawInputDeviceInfo.RIDI_DEVICENAME, pData, ref pcbSize);
                        var deviceName = Marshal.PtrToStringAnsi(pData);

                        MouseInfo mouse = new MouseInfo();
                        mouse.devHandle = rid.hDevice;
                        mouse.devName = (string)deviceName;
                        _MiceList.Add(mouse);
                    }
				}
				Marshal.FreeHGlobal(pRawInputDeviceList);
				return;
			}
            throw new Win32Exception(Marshal.GetLastWin32Error());
		}        

        /// <summary>
        /// Get movements/click and process to Demul memory or GUI when visible
        /// </summary>
        private bool ProcessRawInput(IntPtr hDevice)
        {
            var dwSize = 0;
            Win32.GetRawInputData(hDevice, DataCommand.RID_INPUT, IntPtr.Zero, ref dwSize, Marshal.SizeOf(typeof(Rawinputheader)));

            InputData rawBuffer;
            if (dwSize != Win32.GetRawInputData(hDevice, DataCommand.RID_INPUT, out rawBuffer, ref dwSize, Marshal.SizeOf(typeof(Rawinputheader))))
            {
                //Debug.WriteLine("Error getting the rawinput buffer");
                return false;
            }

            if (rawBuffer.header.dwType == DeviceType.RimTypemouse)
            //On Windows10 rawBuffer.data.mouse.usFlags==1 is never true.....so I removed the test
            //if (rawBuffer.header.dwType == DeviceType.RimTypemouse && rawBuffer.data.mouse.usFlags == 1)        //usFlags : 1=ABSOLUTE 0= RELATIVE
            {
                foreach (ControllerDevice Device in _ControllerDevices)
                {
                    if (rawBuffer.header.hDevice == Device.MouseHandle)
                    {
                        foreach (MouseInfo mouse in _MiceList)
                        {
                            if (rawBuffer.header.hDevice == mouse.devHandle)
                            {
                                int player = Device.Player;

                                if (_Game != null && _Game.ProcessHooked)
                                {
                                    WriteLog("RawData event for Device #" + player.ToString() + ":");
                                    WriteLog("Device rawinput data (Hex) = [ " + rawBuffer.data.mouse.lLastX.ToString("X4") + ", " + rawBuffer.data.mouse.lLastY.ToString("X4") + " ]");
                                }

                                MouseInfo mymouse = new MouseInfo();
                                mymouse.pTarget.X = rawBuffer.data.mouse.lLastX;
                                mymouse.pTarget.Y = rawBuffer.data.mouse.lLastY;
                                mymouse.button = rawBuffer.data.mouse.usButtonFlags;
                                ProcessInput(mymouse, player);

                                break;
                            }
                        }
                    }
                }
            }
            return true;
        }

        #endregion 
       
        #region XInput

        /// <summary>
        /// Threaded work for infinite XInput polling
        /// </summary>
        private void Bgw_XInput_DoWork(object sender, DoWorkEventArgs e)
        {
            XInputState tState = new XInputState();
            MouseInfo[] MouseArray = new MouseInfo[4];
            MouseArray[0] = null;
            MouseArray[1] = null;
            MouseArray[2] = null;
            MouseArray[3] = null;

            while (true)
            {
                System.Threading.Thread.Sleep(_XInput_PollingInterval);
                // We could have looped through all _ControllerDevices items
                // But Xinput limitation is 4 gamepad so for now we just limit to 4 players and stop the loop at P4
                for (int i = 0; i < 4; i++)
                {
                    if (_ControllerDevices[i].GamepadID != -1)
                    {
                        if (XInput.XInputGetState(_ControllerDevices[i].GamepadID, ref tState) == 0)
                        {
                            MouseArray[i] = new MouseInfo();

                            //Setting Axis values, and setting button to nothing
                            if (_ControllerDevices[i].Gamepad_Stick.Equals("L"))
                            {
                                MouseArray[i].pTarget.X = tState.Gamepad.sThumbLX + 32768;
                                MouseArray[i].pTarget.Y = -tState.Gamepad.sThumbLY + 32768;
                            }
                            else if (_ControllerDevices[i].Gamepad_Stick.Equals("R"))
                            {
                                MouseArray[i].pTarget.X = tState.Gamepad.sThumbRX + 32768;
                                MouseArray[i].pTarget.Y = -tState.Gamepad.sThumbRY + 32768;
                            }
                            MouseArray[i].button = 0;

                            //Looking for a button change "event"
                            int XorDif = _XInputStates[_ControllerDevices[i].GamepadID].Gamepad.wButtons ^ tState.Gamepad.wButtons;
                            if (XorDif != 0)
                            {
                                _XInputStates[_ControllerDevices[i].GamepadID].Gamepad.wButtons = tState.Gamepad.wButtons;

                                if (XorDif == _ControllerDevices[i].Gamepad_LeftClick)
                                {
                                    if ((_XInputStates[_ControllerDevices[i].GamepadID].Gamepad.wButtons & _ControllerDevices[i].Gamepad_LeftClick) != 0)
                                    {
                                        MouseArray[i].button = Win32.RI_MOUSE_LEFT_BUTTON_DOWN;
                                    }
                                    else
                                        MouseArray[i].button = Win32.RI_MOUSE_LEFT_BUTTON_UP;
                                }
                                else if (XorDif == _ControllerDevices[i].Gamepad_MiddleClick)
                                {
                                    if ((_XInputStates[_ControllerDevices[i].GamepadID].Gamepad.wButtons & _ControllerDevices[i].Gamepad_MiddleClick) != 0)
                                        MouseArray[i].button = Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN;
                                    else
                                        MouseArray[i].button = Win32.RI_MOUSE_MIDDLE_BUTTON_UP;
                                }
                                else if (XorDif == _ControllerDevices[i].Gamepad_RightClick)
                                {
                                    if ((_XInputStates[_ControllerDevices[i].GamepadID].Gamepad.wButtons & _ControllerDevices[i].Gamepad_RightClick) != 0)
                                        MouseArray[i].button = Win32.RI_MOUSE_RIGHT_BUTTON_DOWN;
                                    else
                                        MouseArray[i].button = Win32.RI_MOUSE_RIGHT_BUTTON_UP;
                                }
                            }
                        }
                    }
                }                
                Bgw_XInput.ReportProgress(1, MouseArray);
            }
        }

        /// <summary>
        /// Invoked to main thread function to update cursors / buttons from XInput data
        /// Argument e.ProgressPErcentage is used to pass "Player" number (1 or 2)
        /// According to "state" of the Gamepad (buttons values and axis), recreating a MouseInfo struct to be treated afterwards
        /// </summary>
        private void Bgw_XInput_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int player = e.ProgressPercentage;
            MouseInfo[] MouseArray = (MouseInfo[])e.UserState;

            // Same thing here :
            // MouseArray will only ave 4 item at maximum
            for (int i = 0; i < 4; i++)
            {
                if (MouseArray[i] != null)
                {
                    if (_Game != null && _Game.ProcessHooked)
                        WriteLog("Gamepad #" + _ControllerDevices[i].GamepadID + " data (Hex) = [ " + MouseArray[i].pTarget.X.ToString("X4") + ", " + MouseArray[i].pTarget.Y.ToString("X4") + " ]");

                    if (MouseArray[i].button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN && _ControllerDevices[i].Gamepad_VibrationEnabled == 1)
                    {
                        XInputVibration vibration = new XInputVibration() { LeftMotorSpeed = (ushort)_ControllerDevices[i].Gamepad_VibrationStrength, RightMotorSpeed = (ushort)_ControllerDevices[i].Gamepad_VibrationStrength };
                        XInput.XInputSetState(_ControllerDevices[i].GamepadID, ref vibration);
                        _ControllerDevices[i].RunVibrationTimer();
                    }

                    ProcessInput(MouseArray[i], _ControllerDevices[i].Player); 
                }
            }
        }

        #endregion

        /// <summary>
        /// Feeded by RawInput or XInput events/polling
        /// Convert received data to game data to inject
        /// </summary>
        private void ProcessInput(MouseInfo mouse, int player)
        {
            if (_Game != null && _Game.ProcessHooked)
            {
                _Game.GetScreenResolution();
                WriteLog("PrimaryScreen Size (Px) = [ " + _Game.ScreenWidth + "x" + _Game.ScreenHeight + " ]");

                /*_Game.GetScreenresolution2();
                WriteLog("Desktop Resolution (Px) = [ " + _Game.ScreenWidth + ", " + _Game.ScreenHeight + " ]");*/

                mouse.pTarget.X = _Game.ScreenScale(mouse.pTarget.X, INPUT_ABSOLUTE_MIN, INPUT_ABSOLUTE_MAX, 0, _Game.ScreenWidth);
                mouse.pTarget.Y = _Game.ScreenScale(mouse.pTarget.Y, INPUT_ABSOLUTE_MIN, INPUT_ABSOLUTE_MAX, 0, _Game.ScreenHeight);             

                //Saving cursor equivalent pos for BlueEstate shark display
                _Game.screenCursorPosX = mouse.pTarget.X;
                _Game.screenCursorPosY = mouse.pTarget.Y;
                WriteLog("OnScreen Cursor Position (Px) = [ " + mouse.pTarget.X + ", " + mouse.pTarget.Y + " ]");

                if (_Act_Labs_Offset_Enable == 1)
                {
                    mouse.pTarget.X += _ControllerDevices[player - 1].Act_Labs_OffsetX;
                    mouse.pTarget.Y += _ControllerDevices[player - 1].Act_Labs_OffsetY;                    
                    
                    if (_Game.ProcessHooked)
                        WriteLog("ActLabs adaptated OnScreen Cursor Position (Px) = [ " + mouse.pTarget.X + ", " + mouse.pTarget.Y + " ]");
                }

                if (!_Game.ClientScale(mouse))
                {
                    WriteLog("Error converting screen location to client location");
                    return;
                }
                WriteLog("OnClient Cursor Position (Px) = [ " + mouse.pTarget.X + ", " + mouse.pTarget.Y + " ]");
                

                if (!_Game.GameScale(mouse, player))
                {
                    WriteLog("Error converting client location to game location");
                    return;
                }

                WriteLog("Game Position (Hex) = [ " + mouse.pTarget.X.ToString("X4") + ", " + mouse.pTarget.Y.ToString("X4") + " ]");
                WriteLog("Game Position (Dec) = [ " + mouse.pTarget.X.ToString() + ", " + mouse.pTarget.Y.ToString() + " ]");
                WriteLog("MouseButton (Hex) = 0x" + mouse.button.ToString("X4"));
                WriteLog("-");
                _ControllerDevices[player - 1].LastMouseInfo = mouse;
                
                _Game.SendInput(mouse, player);
            }       
            // GUI only -> show crosshair when shoot
            else
            {
                if (Chk_DspCorrectedCrosshair.Checked == true && _TrayIcon.Visible == false)
                {
                    int X = ScreenScale(mouse.pTarget.X, INPUT_ABSOLUTE_MIN, INPUT_ABSOLUTE_MAX, 0, Screen.PrimaryScreen.Bounds.Width);
                    int Y = ScreenScale(mouse.pTarget.Y, INPUT_ABSOLUTE_MIN, INPUT_ABSOLUTE_MAX, 0, Screen.PrimaryScreen.Bounds.Height);

                    if (_Act_Labs_Offset_Enable == 1)
                    {
                        X += _ControllerDevices[player - 1].Act_Labs_OffsetX;
                        Y += _ControllerDevices[player - 1].Act_Labs_OffsetY; 
                    }

                    if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                    {
                        Color CrosshairColor = Color.Crimson;
                        if (player == 2)
                            CrosshairColor = Color.Blue;
                        else if (player == 3)
                            CrosshairColor = Color.LimeGreen;
                        else if (player == 4)
                            CrosshairColor = Color.Gold;

                        // Draw Crosshair
                        IntPtr desktopPtr = Win32.GetDC(IntPtr.Zero);
                        Graphics g = Graphics.FromHdc(desktopPtr);
                        SolidBrush b = new SolidBrush(CrosshairColor); 
                        Pen p = new Pen(b, 2);
                        g.DrawEllipse(p, X - 20, Y - 20, 40, 40);
                        g.DrawEllipse(p, X - 2, Y - 2, 4, 4);

                        g.Dispose();
                        Win32.ReleaseDC(IntPtr.Zero, desktopPtr);

                        CrosshairAimTimer.Interval = 300;
                        CrosshairAimTimer.Start();
                    }                                   
                }
            }
        }

        #region SCREEN

        /// <summary>
        /// Contains value inside min-max range
        /// </summary>
        protected int Clamp(int val, int minVal, int maxVal)
        {
            if (val > maxVal) return maxVal;
            else if (val < minVal) return minVal;
            else return val;
        }

        /// <summary>
        /// Transforming 0x0000-0xFFFF absolute rawdata to 0-100% position on screen axis
        /// </summary>
        public int ScreenScale(int val, int fromMinVal, int fromMaxVal, int toMinVal, int toMaxVal)
        {
            return ScreenScale(val, fromMinVal, fromMinVal, fromMaxVal, toMinVal, toMinVal, toMaxVal);
        }
        protected int ScreenScale(int val, int fromMinVal, int fromOffVal, int fromMaxVal, int toMinVal, int toOffVal, int toMaxVal)
        {
            double fromRange;
            double frac;
            if (fromMaxVal > fromMinVal)
            {
                val = Clamp(val, fromMinVal, fromMaxVal);
                if (val > fromOffVal)
                {
                    fromRange = (double)(fromMaxVal - fromOffVal);
                    frac = (double)(val - fromOffVal) / fromRange;
                }
                else if (val < fromOffVal)
                {
                    fromRange = (double)(fromOffVal - fromMinVal);
                    frac = (double)(val - fromOffVal) / fromRange;
                }
                else
                    return toOffVal;
            }
            else if (fromMinVal > fromMaxVal)
            {
                val = Clamp(val, fromMaxVal, fromMinVal);
                if (val > fromOffVal)
                {
                    fromRange = (double)(fromMinVal - fromOffVal);
                    frac = (double)(fromOffVal - val) / fromRange;
                }
                else if (val < fromOffVal)
                {
                    fromRange = (double)(fromOffVal - fromMaxVal);
                    frac = (double)(fromOffVal - val) / fromRange;
                }
                else
                    return toOffVal;
            }
            else
                return toOffVal;
            double toRange;
            if (toMaxVal > toMinVal)
            {
                if (frac >= 0)
                    toRange = (double)(toMaxVal - toOffVal);
                else
                    toRange = (double)(toOffVal - toMinVal);
                return toOffVal + (int)(toRange * frac);
            }
            else
            {
                if (frac >= 0)
                    toRange = (double)(toOffVal - toMaxVal);
                else
                    toRange = (double)(toMinVal - toOffVal);
                return toOffVal - (int)(toRange * frac);
            }
        }

        #endregion

        #region FILES I/O

        /// <summary>
        /// Main application config file
        /// </summary>
        private void ReadConf()
        {
            try
            {
                using (StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                {
                    string line = sr.ReadLine();
                    string[] buffer;
                    while (line != null)
                    {
                        if (!line.StartsWith(";"))
                        {
                            buffer = line.Split('=');
                            if (buffer.Length == 2)
                            {
                                string StrKey = buffer[0].Trim().ToLower();
                                string StrValue = buffer[1].Trim();                                

                                // There will never be more than 9 players (even more than 4) so we can assume that 
                                // removing only first 2 char is enough without verification
                                if (StrKey.StartsWith("p1"))
                                {
                                    if (!_ControllerDevices[0].ParseIniParameter(StrKey.Substring(2), StrValue))
                                        WriteLog("Error parsing " + buffer[0].Trim() + " value in INI file : " + buffer[1].Trim() + " is not valid");
                                }
                                else if (StrKey.StartsWith("p2"))
                                {
                                    if (!_ControllerDevices[1].ParseIniParameter(StrKey.Substring(2), StrValue))
                                        WriteLog("Error parsing " + buffer[0].Trim() + " value in INI file : " + buffer[1].Trim() + " is not valid");
                                }
                                else if (StrKey.StartsWith("p3"))
                                {
                                    if (!_ControllerDevices[2].ParseIniParameter(StrKey.Substring(2), StrValue))
                                        WriteLog("Error parsing " + buffer[0].Trim() + " value in INI file : " + buffer[1].Trim() + " is not valid");
                                }
                                else if (StrKey.StartsWith("p4"))
                                {
                                    if (!_ControllerDevices[3].ParseIniParameter(StrKey.Substring(2), StrValue))
                                        WriteLog("Error parsing " + buffer[0].Trim() + " value in INI file : " + buffer[1].Trim() + " is not valid");
                                }
                                else if (StrKey.Equals("m2_p1_ch"))
                                {
                                    if (byte.TryParse(StrValue, out _Di_Crosshair_P1))
                                        TXT_CH_P1.Text = GetKeyStringFromScanCode(_Di_Crosshair_P1);
                                    else
                                        WriteLog("Error parsing " + buffer[0].Trim() + " value in INI file : " + buffer[1].Trim() + " is not valid");
                                }
                                else if (StrKey.Equals("m2_p2_ch"))
                                {
                                    if (byte.TryParse(StrValue, out _Di_Crosshair_P2))
                                        TXT_CH_P2.Text = GetKeyStringFromScanCode(_Di_Crosshair_P2);
                                    else
                                        WriteLog("Error parsing " + buffer[0].Trim() + " value in INI file : " + buffer[1].Trim() + " is not valid");
                                }
                                else if (StrKey.Equals("m2_ch_vis"))
                                {
                                    if (byte.TryParse(StrValue, out _Di_Crosshair_Visibility))
                                        TXT_CH_VIS.Text = GetKeyStringFromScanCode(_Di_Crosshair_Visibility);
                                    else
                                        WriteLog("Error parsing " + buffer[0].Trim() + " value in INI file : " + buffer[1].Trim() + " is not valid");
                                }
                                else if (StrKey.Equals("gsoz_p1_pedal_enable"))
                                {
                                    if (int.TryParse(StrValue, out _Gsoz_Pedal_P1_Enabled))
                                    {
                                        if (_Gsoz_Pedal_P1_Enabled != 0)
                                        {
                                            Chk_GundamP1Pedal.Checked = true;
                                            TXT_GSOZ_PEDAL_1.Enabled = true;
                                        }
                                        else
                                        {
                                            Chk_GundamP1Pedal.Checked = false;
                                            TXT_GSOZ_PEDAL_1.Enabled = false;
                                        }
                                    }
                                    else
                                        WriteLog("Error parsing " + buffer[0].Trim() + " value in INI file : " + buffer[1].Trim() + " is not valid");
                                }
                                else if (StrKey.Equals("gsoz_p2_pedal_enable"))
                                {
                                    if (int.TryParse(StrValue, out _Gsoz_Pedal_P2_Enabled))
                                    {
                                        if (_Gsoz_Pedal_P2_Enabled != 0)
                                        {
                                            Chk_GundamP2Pedal.Checked = true;
                                            TXT_GSOZ_PEDAL_2.Enabled = true;
                                        }
                                        else
                                        {
                                            Chk_GundamP2Pedal.Checked = false;
                                            TXT_GSOZ_PEDAL_2.Enabled = false;
                                        }
                                    }
                                    else
                                        WriteLog("Error parsing " + buffer[0].Trim() + " value in INI file : " + buffer[1].Trim() + " is not valid");
                                }
                                else if (StrKey.Equals("gsoz_p1_pedal_key"))
                                {
                                    if (byte.TryParse(StrValue, out _Di_Gsoz_Pedal_P1))
                                        TXT_GSOZ_PEDAL_1.Text = GetKeyStringFromScanCode(_Di_Gsoz_Pedal_P1);
                                    else
                                        WriteLog("Error parsing " + buffer[0].Trim() + " value in INI file : " + buffer[1].Trim() + " is not valid");
                                }
                                else if (StrKey.Equals("gsoz_p2_pedal_key"))
                                {
                                    if (byte.TryParse(StrValue, out _Di_Gsoz_Pedal_P2))
                                        TXT_GSOZ_PEDAL_2.Text = GetKeyStringFromScanCode(_Di_Gsoz_Pedal_P2);
                                    else
                                        WriteLog("Error parsing " + buffer[0].Trim() + " value in INI file : " + buffer[1].Trim() + " is not valid");
                                }
                                else if (StrKey.Equals("act_labs_offset_enable"))
                                {
                                    if (int.TryParse(StrValue, out _Act_Labs_Offset_Enable))
                                    {
                                        if (_Act_Labs_Offset_Enable == 1)
                                            Cb_ActLabsOffset.Checked = true;
                                    }
                                    else
                                        WriteLog("Error parsing " + buffer[0].Trim() + " value in INI file : " + buffer[1].Trim() + " is not valid");
                                }
                                else if (StrKey.Equals("hf3_path"))
                                {
                                    _HF3_Path = StrValue;
                                    Txt_HF3_Browse.Text = _HF3_Path;
                                }
                                else if (StrKey.Equals("hf3_coversensibility"))
                                {
                                    if (int.TryParse(StrValue, out _HF3_CoverSensibility))
                                        TrackBar_HF3_Cover.Value = _HF3_CoverSensibility;
                                    else
                                        WriteLog("Error parsing " + buffer[0].Trim() + " value in INI file : " + buffer[1].Trim() + " is not valid");
                                }
                                else if (StrKey.Equals("hf4_path"))
                                {
                                    _HF4_Path = StrValue;
                                    Txt_HF4_Browse.Text = _HF4_Path;
                                }
                                else if (StrKey.Equals("hf4_coversensibility"))
                                {
                                    if (int.TryParse(StrValue, out _HF4_CoverSensibility))
                                        TrackBar_HF4_Cover.Value = _HF4_CoverSensibility;
                                    else
                                        WriteLog("Error parsing " + buffer[0].Trim() + " value in INI file : " + buffer[1].Trim() + " is not valid");
                                }
                            }
                        }
                        line = sr.ReadLine();
                    }
                    sr.Close();
                    WriteLog("Conf read OK");
                }
            }
            catch (Exception ex)
            {
                WriteLog("Error reading " + CONF_FILENAME + " : " + ex.Message);
            }
        }

        /// <summary>
        /// Read Silent Hill the Arcade Key mapping
        /// </summary>
        private void Read_Sha_Conf()
        {
            string appData = Environment.GetEnvironmentVariable("appdata").ToString(); ;
            if (File.Exists(appData + SHA_CONF_FILEPATH))
            {
                WriteLog("Reading Triggers Keycodes from SHA config file...");
                byte[] fileBytes = File.ReadAllBytes(appData + @"\bemani_config\sha_v01.cfg");
                int Offset = 0x622;
                int n = (int)fileBytes[0];
                for (int i = 0; i < n; i++)
                {
                    int j = Offset + (i * 4);
                    switch (fileBytes[j + 1])
                    {
                        case 0x01:
                            {
                                _Di_Sha_Test = fileBytes[j];
                                TXT_TEST.Text = GetKeyStringFromScanCode(fileBytes[j]);
                            } break;
                        case 0x02:
                            {
                                _Di_Sha_Service = fileBytes[j];
                                TXT_SERVICE.Text = GetKeyStringFromScanCode(fileBytes[j]);
                            } break;
                        case 0x10:
                            {
                                _Di_Sha_P1_S = fileBytes[j];
                                TXT_P1_S.Text = GetKeyStringFromScanCode(fileBytes[j]);
                            } break;
                        case 0x11:
                            {
                                _Di_Sha_P1_T = fileBytes[j];
                                TXT_P1_T.Text = GetKeyStringFromScanCode(fileBytes[j]);
                            } break;
                        case 0x20:
                            {
                                _Di_Sha_P2_S = fileBytes[j];
                                TXT_P2_S.Text = GetKeyStringFromScanCode(fileBytes[j]);
                            } break;
                        case 0x21:
                            {
                                _Di_Sha_P2_T = fileBytes[j];
                                TXT_P2_T.Text = GetKeyStringFromScanCode(fileBytes[j]);
                            } break;
                        case 0xFF:
                            {
                                _Di_Sha_Exit = fileBytes[j];
                                TXT_EXIT.Text = GetKeyStringFromScanCode(fileBytes[j]);
                            } break;
                    }
                }
            }
            else
            {
                WriteLog("Silent Hill the Arcade : " + appData + @"\bemani_config\sha_v01.cfg not found");
            }
        }

        /// <summary>
        /// Write Conf file
        /// </summary>
        private void WriteConf()
        {
            try
            {
                using (StreamWriter sr = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME, false))
                {
                    foreach (ControllerDevice Device in _ControllerDevices)
                    {
                        sr.WriteLine(";Player" + Device.Player + " Device configuration");
                        sr.WriteLine("P" + Device.Player + "Device = " + Device.DeviceName);
                        sr.WriteLine("P" + Device.Player + "GamepadLeftClick = " + Device.Gamepad_LeftClick.ToString());
                        sr.WriteLine("P" + Device.Player + "GamepadRightClick = " + Device.Gamepad_RightClick.ToString());
                        sr.WriteLine("P" + Device.Player + "GamepadMiddleClick = " + Device.Gamepad_MiddleClick.ToString());
                        sr.WriteLine("P" + Device.Player + "GamepadStick = " + Device.Gamepad_Stick.ToString());
                        sr.WriteLine("P" + Device.Player + "GamepadVibrationEnabled = " + Device.Gamepad_VibrationEnabled.ToString());
                        sr.WriteLine("P" + Device.Player + "GamepadVibrationLength = " + Device.Gamepad_VibrationLength.ToString());
                        sr.WriteLine("P" + Device.Player + "GamepadVibrationStrength = " + Device.Gamepad_VibrationStrength.ToString());
                        sr.WriteLine("");
                    }
                    
                    sr.WriteLine(";Model2 emulator keyboard keys to change in-game crosshairs");
                    sr.WriteLine("M2_P1_CH = " + (byte)_Di_Crosshair_P1);
                    sr.WriteLine("M2_P2_CH = " + (byte)_Di_Crosshair_P2);
                    sr.WriteLine("M2_CH_VIS = " + (byte)_Di_Crosshair_Visibility);
                    sr.WriteLine("");
                    sr.WriteLine(";Enable Pedal-Mode for TTX Gundam Zeon, and set Keys");
                    sr.WriteLine("GSOZ_P1_PEDAL_ENABLE = " + _Gsoz_Pedal_P1_Enabled.ToString());
                    sr.WriteLine("GSOZ_P1_PEDAL_KEY = " + (byte)_Di_Gsoz_Pedal_P1);
                    sr.WriteLine("GSOZ_P2_PEDAL_ENABLE = " + _Gsoz_Pedal_P2_Enabled.ToString());
                    sr.WriteLine("GSOZ_P2_PEDAL_KEY = " + (byte)_Di_Gsoz_Pedal_P2);
                    sr.WriteLine("");
                    sr.WriteLine(";Offset for devices lacking calibration (Act Labs gun, etc...)");
                    sr.WriteLine("Act_Labs_Offset_Enable = " + _Act_Labs_Offset_Enable.ToString());
                    foreach (ControllerDevice Device in _ControllerDevices)
                    {
                        sr.WriteLine("P" + Device.Player + "Act_Labs_Offset_X = " + Device.Act_Labs_OffsetX.ToString());
                        sr.WriteLine("P" + Device.Player + "Act_Labs_Offset_Y = " + Device.Act_Labs_OffsetY.ToString());
                    }
                    sr.WriteLine("");
                    sr.WriteLine(";Virtual MiddleButton keys for users who don't have more than a trigger with Aimtrak");
                    foreach (ControllerDevice Device in _ControllerDevices)
                    {
                        sr.WriteLine("P" + Device.Player + "VirtualMiddle_Enable = " + Device.EnableVirtualMiddleClick.ToString());
                        sr.WriteLine("P" + Device.Player + "VirtualMiddle_Key = " + Device.DiK_VirtualMiddleButton.ToString());
                    }
                    sr.WriteLine("");
                    sr.WriteLine(";Heavy Fire Afghanistan settings");
                    sr.WriteLine("HF3_Path = " + _HF3_Path);
                    sr.WriteLine("HF3_CoverSensibility = " + _HF3_CoverSensibility);
                    sr.WriteLine("");
                    sr.WriteLine(";Heavy Fire Shattered Spear settings");
                    sr.WriteLine("HF4_Path = " + _HF4_Path);
                    sr.WriteLine("HF4_CoverSensibility = " + _HF4_CoverSensibility);
                    sr.Close();
                }
                WriteLog("Conf write OK");
                MessageBox.Show("Configuration saved !");
            }
            catch (Exception ex)
            {
                WriteLog("Error writing " + CONF_FILENAME + " : " + ex.Message);
            }
        }

        /// <summary>
        /// Write Silent Hill the Arcade key mapping
        /// </summary>
        private void Write_Sha_Config()
        {
            try
            {
                string appData = Environment.GetEnvironmentVariable("appdata").ToString();
                if (!Directory.Exists(appData + @"\bemani_config"))
                {
                    Directory.CreateDirectory(appData + @"\bemani_config");
                }
                using (FileStream s = new FileStream(appData + SHA_CONF_FILEPATH, FileMode.Create))
                {
                    using (BinaryWriter w = new BinaryWriter(s))
                    {
                        byte n = 0;

                        for (int i = 0; i < 0x620; i++)
                        {
                            w.Write((byte)0x00);
                        }
                        if (_Di_Sha_Exit != 0)
                        {
                            w.Write((short)0);
                            w.Write(_Di_Sha_Exit);
                            w.Write((byte)0xFF);
                            n++;
                        }
                        if (_Di_Sha_Test != 0)
                        {
                            w.Write((short)0);
                            w.Write(_Di_Sha_Test);
                            w.Write((byte)0x01);
                            n++;
                        }
                        if (_Di_Sha_Service != 0)
                        {
                            w.Write((short)0);
                            w.Write(_Di_Sha_Service);
                            w.Write((byte)0x02);
                            n++;
                        }
                        if (_Di_Sha_P1_S != 0)
                        {
                            w.Write((short)0);
                            w.Write(_Di_Sha_P1_S);
                            w.Write((byte)0x10);
                            n++;
                        }
                        if (_Di_Sha_P1_T != 0)
                        {
                            w.Write((short)0);
                            w.Write(_Di_Sha_P1_T);
                            w.Write((byte)0x11);
                            n++;
                        }
                        if (_Di_Sha_P2_S != 0)
                        {
                            w.Write((short)0);
                            w.Write(_Di_Sha_P2_S);
                            w.Write((byte)0x20);
                            n++;
                        }
                        if (_Di_Sha_P2_T != 0)
                        {
                            w.Write((short)0);
                            w.Write(_Di_Sha_P2_T);
                            w.Write((byte)0x21);
                            n++;
                        }

                        w.Seek(0, SeekOrigin.Begin);
                        w.Write(n);
                    }
                }
                MessageBox.Show("Key mapping saved !");
            }
            catch (Exception ex)
            {
                WriteLog("Impossible to save SHA config file : " + ex.Message.ToString());
            }
        }

        /// <summary>
        /// Writing Log only if verbose arg given in cmdline
        /// </summary>
        private void WriteLog(String Data)
        {
            if (_VerboseEnable)
            {
                try
                {
                    using (StreamWriter sr = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + @"\" + LOG_FILENAME, true))
                    {
                        sr.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffffff") + " : " + Data);
                        sr.Close();
                    }
                }
                catch { }
            }
        }        

        #endregion        

        #region "GUI"

        /// <summary>
        /// Set visibility of Main Window and Systray Icon
        /// </summary>
        private void SetVisibility(bool FormVisible)
        {
            if (!FormVisible)
            {
                this.Location = new System.Drawing.Point(-10000, -10000); 
                this.ShowInTaskbar = false;
                if (_Has_ExplorerExe)
                {
                    _TrayIcon.Visible = true;
                    _TrayIcon.Text += "[" + _Target + "] [" + _Rom + "]";
                }
            }
            else
            {
                this.Location = new System.Drawing.Point((Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2, (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2);
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                if (_Has_ExplorerExe)
                    _TrayIcon.Visible = false;
            }
        }

        /// <summary>
        /// Exit from TrayIcon menu entry
        /// </summary>
        private void OnExit(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        /// <summary>
        /// Page Selection
        /// </summary>
        private void Cbo_PageSettings_SelectionChangeCommitted(object sender, EventArgs e)
        {
            tabControl1.SelectTab(Cbo_PageSettings.SelectedIndex);
        }

        /// <summary>
        /// Various GUI actions
        /// </summary>
        /// 
        private void Txt_ActLabs_X1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _ControllerDevices[0].Act_Labs_OffsetX = Convert.ToInt32(Txt_ActLabs_X1.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_X1.Text + " is not a valid X offset value. Please enter a non-decimal number");
                Txt_ActLabs_X1.Text = _ControllerDevices[0].Act_Labs_OffsetX.ToString();
            }
        }
        private void Txt_ActLabs_Y1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _ControllerDevices[0].Act_Labs_OffsetY = Convert.ToInt32(Txt_ActLabs_Y1.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_Y1.Text + " is not a valid Y offset value. Please enter a non-decimal number");
                Txt_ActLabs_Y1.Text = _ControllerDevices[0].Act_Labs_OffsetY.ToString();
            }
        }
        private void Txt_ActLabs_X2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _ControllerDevices[1].Act_Labs_OffsetX = Convert.ToInt32(Txt_ActLabs_X2.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_X2.Text + " is not a valid X offset value. Please enter a non-decimal number");
                Txt_ActLabs_X2.Text = _ControllerDevices[1].Act_Labs_OffsetX.ToString();
            }
        }
        private void Txt_ActLabs_Y2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _ControllerDevices[1].Act_Labs_OffsetY = Convert.ToInt32(Txt_ActLabs_Y2.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_Y2.Text + " is not a valid Y offset value. Please enter a non-decimal number");
                Txt_ActLabs_Y2.Text = _ControllerDevices[1].Act_Labs_OffsetY.ToString();
            }
        }
        private void Txt_ActLabs_X3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _ControllerDevices[2].Act_Labs_OffsetX = Convert.ToInt32(Txt_ActLabs_X3.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_X3.Text + " is not a valid X offset value. Please enter a non-decimal number");
                Txt_ActLabs_X3.Text = _ControllerDevices[2].Act_Labs_OffsetX.ToString();
            }
        }
        private void Txt_ActLabs_Y3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _ControllerDevices[2].Act_Labs_OffsetY = Convert.ToInt32(Txt_ActLabs_Y3.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_Y3.Text + " is not a valid Y offset value. Please enter a non-decimal number");
                Txt_ActLabs_Y3.Text = _ControllerDevices[2].Act_Labs_OffsetY.ToString();
            }
        }
        private void Txt_ActLabs_X4_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _ControllerDevices[3].Act_Labs_OffsetX = Convert.ToInt32(Txt_ActLabs_X4.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_X4.Text + " is not a valid X offset value. Please enter a non-decimal number");
                Txt_ActLabs_X4.Text = _ControllerDevices[3].Act_Labs_OffsetX.ToString();
            }
        }
        private void Txt_ActLabs_Y4_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _ControllerDevices[3].Act_Labs_OffsetY = Convert.ToInt32(Txt_ActLabs_Y4.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_Y4.Text + " is not a valid Y offset value. Please enter a non-decimal number");
                Txt_ActLabs_Y4.Text = _ControllerDevices[3].Act_Labs_OffsetY.ToString();
            }
        }

        /// <summary>
        /// Activate / Deactivate act labs offset
        /// </summary>
        private void Cb_ActLabsOffset_CheckedChanged(object sender, EventArgs e)
        {
            if (Cb_ActLabsOffset.Checked)
            {
                Txt_ActLabs_X1.Enabled = true;
                Txt_ActLabs_Y1.Enabled = true;
                Txt_ActLabs_X2.Enabled = true;
                Txt_ActLabs_Y2.Enabled = true;
                Txt_ActLabs_X3.Enabled = true;
                Txt_ActLabs_Y3.Enabled = true;
                Txt_ActLabs_X4.Enabled = true;
                Txt_ActLabs_Y4.Enabled = true;
                _Act_Labs_Offset_Enable = 1;
            }
            else
            {
                Txt_ActLabs_X1.Enabled = false;
                Txt_ActLabs_Y1.Enabled = false;
                Txt_ActLabs_X2.Enabled = false;
                Txt_ActLabs_Y2.Enabled = false;
                Txt_ActLabs_X3.Enabled = false;
                Txt_ActLabs_Y3.Enabled = false;
                Txt_ActLabs_X4.Enabled = false;
                Txt_ActLabs_Y4.Enabled = false;
                _Act_Labs_Offset_Enable = 0;
            }
        }

        /// <summary>
        /// Save config to file
        /// </summary>
        private void Btn_Save_Cfg_Click(object sender, EventArgs e)
        {
            WriteConf();          
        }
        private void Btn_ActLabs_Save_Click(object sender, EventArgs e)
        {
            WriteConf();
        }
        private void Btn_Save_Gsoz_Click(object sender, EventArgs e)
        {
            WriteConf();
        }

        //Register DirectInput Keycodes
        public void TXT_DirectInput_MouseClick(object sender, MouseEventArgs e)
        {
            if (_Txtbox != null && _Txtbox != ((TextBox)sender))
            {
                _Txtbox.Text = _sTxtbox;
            }
            _Txtbox = ((TextBox)sender);
            _sTxtbox = _Txtbox.Text;
            _Txtbox.Text = "";
            _Start_KeyRecord = true;
        }
        private void Chk_GundamP1Pedal_CheckedChanged(object sender, EventArgs e)
        {
            if (Chk_GundamP1Pedal.Checked)
            {
                TXT_GSOZ_PEDAL_1.Enabled = true;
                _Gsoz_Pedal_P1_Enabled = 1;
            }
            else
            {
                TXT_GSOZ_PEDAL_1.Enabled = false;
                _Gsoz_Pedal_P1_Enabled = 0;
            } 
        }
        private void Chk_GundamP2Pedal_CheckedChanged(object sender, EventArgs e)
        {
            if (Chk_GundamP2Pedal.Checked)
            {
                TXT_GSOZ_PEDAL_2.Enabled = true;
                _Gsoz_Pedal_P2_Enabled = 1;
            }
            else
            {
                TXT_GSOZ_PEDAL_2.Enabled = false;
                _Gsoz_Pedal_P2_Enabled = 0;
            } 
        } 

        /// <summary>
        /// Modify/Create SHA key mapping
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Sha_Keys_Click(object sender, EventArgs e)
        {
            Write_Sha_Config();
        }

        /// <summary>
        /// Overwrite crosshair cursor with empty bmp
        /// </summary>
        private void Btn_SHcur_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "Please select \"Silent Hill The Arcade\" game folder";
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    if (!File.Exists(folderBrowserDialog1.SelectedPath + @"\sv\CrossHair.cur"))
                    {
                        MessageBox.Show(folderBrowserDialog1.SelectedPath + "\\sv\\CrossHair.cur\n\nFile not found", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);                        
                    }
                    else
                    {
                        File.Copy(folderBrowserDialog1.SelectedPath + @"\sv\CrossHair.cur", folderBrowserDialog1.SelectedPath + @"\sv\CrossHair.cur.bak");
                        using (StreamWriter sw = new StreamWriter(folderBrowserDialog1.SelectedPath + @"\sv\CrossHair.cur", false))
                        {
                            sw.Write(DemulShooter.Properties.Resources.Crosshair);
                        }
                        string message = "File \"" + folderBrowserDialog1.SelectedPath + "\\sv\\CrossHair.cur\" successfully written !";
                        message += "\n\nThe existing CrossHair.cur was backed-up to \"CrossHair.cur.bak\"";
                        MessageBox.Show(message);
                    }                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Can't install WiimoteNew.ini : \n\n" + ex.Message.ToString(), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }           
        }

        /// <summary>
        /// Install Dolphin Wiimote configuration
        /// </summary>
        private void Btn_Dolphin5_Click(object sender, EventArgs e)
        {
            InstallWiimoteconfigFile(DemulShooter.Properties.Resources.WiimoteNew_v5);
        }
        private void InstallWiimoteconfigFile(String ResourceFile)
        {
            string Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Dolphin Emulator\Config";
            bool IsAimtrak = false;
            bool Overwritten = false;
            try
            {
                if (File.Exists(Path + @"\WiimoteNew.ini"))
                {
                    File.Copy(Path + @"\WiimoteNew.ini", Path + @"\WiimoteNew.bak.ini", true);
                    Overwritten = true;
                }
                //Copying file
                using (StreamWriter sw = new StreamWriter(Path + @"\WiimoteNew.ini", false))
                {
                    sw.Write(ResourceFile);
                    sw.Close();
                }
                //Modifying Devices Names according to config.ini
                int P2_Atrak_ID = 0;
                try
                {
                    string VID = (_ControllerDevices[1].DeviceName.ToUpper().Split(new string[] { "VID_" }, System.StringSplitOptions.RemoveEmptyEntries))[1].Substring(0, 4);
                    string PID = (_ControllerDevices[1].DeviceName.ToUpper().Split(new string[] { "PID_" }, System.StringSplitOptions.RemoveEmptyEntries))[1].Substring(0, 4);
                    if (VID.Equals("D209") && PID.StartsWith("16"))
                    {
                        IsAimtrak = true;
                        P2_Atrak_ID = int.Parse(PID.Substring(2, 2));
                        int bID = 0x30 + P2_Atrak_ID;
                        using (BinaryWriter fileWriter = new BinaryWriter(File.Open(Path + @"\WiimoteNew.ini", System.IO.FileMode.Open)))
                        {
                            fileWriter.BaseStream.Position = 0x2E7; // set the offset
                            fileWriter.Write((byte)bID);
                            fileWriter.BaseStream.Position = 0x315; // set the offset
                            fileWriter.Write((byte)bID);
                            fileWriter.BaseStream.Position = 0x343; // set the offset
                            fileWriter.Write((byte)bID);
                            fileWriter.BaseStream.Position = 0x372; // set the offset
                            fileWriter.Write((byte)bID);
                        }                        
                    }
                }
                catch 
                {
                    /*String m = "P2 Wiimote axis device could not be modified, default is :\n \"DInput/0/ATRAK Device #2\"";
                    MessageBox.Show(m, "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);*/
                }                

                //Dialog Message
                string message = "File \"" + Path + "\\WiimoteNew.ini\" successfully written !";
                if (Overwritten)
                    message += "\n\nThe existing WiimoteNew.ini was backed-up to \"WiimoteNew.bak.ini\"\n\n";
                MessageBox.Show(message, "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                if (IsAimtrak)
                {
                    message = "Aimtrak detected for Player2 device, Aimtrak ID = " + P2_Atrak_ID;
                    message += "\n\nAccording to DemulShooter config, P2 Wiimote axis device modified to : \n\"DInput/0/ATRAK Device #" + P2_Atrak_ID + "\"";
                    MessageBox.Show(message, "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Information);                
                }
                else
                {
                    message = "No Aimtrak detected for Player2.";
                    message += "\n\nP2 Wiimote axis device will keep default value :\n \"DInput/0/ATRAK Device #2\"";
                    MessageBox.Show(message, "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Warning);                
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't install WiimoteNew.ini : \n\n" + ex.Message.ToString(), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Install m2emulator lua scripts for crosshair
        /// </summary>
        private void Btn_M2Scripts_Click(object sender, EventArgs e)
        {
            InstallM2Scripts();
        }
        private void InstallM2Scripts()
        {
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK)
                return;
            try
            {
                StreamWriter streamWriter = new StreamWriter(folderBrowserDialog1.SelectedPath + "\\scripts\\demulshooter.lua", false);
                streamWriter.WriteLine("-- This file is automatically generated by Demulshooter\n");
                streamWriter.WriteLine("P1_ChangeCrosshairKey=0x" + this._Di_Crosshair_P1.ToString("X2"));
                streamWriter.WriteLine("P2_ChangeCrosshairKey=0x" + this._Di_Crosshair_P2.ToString("X2"));
                streamWriter.WriteLine("CrosshairVisibilityKey=0x" + this._Di_Crosshair_Visibility.ToString("X2"));
                streamWriter.WriteLine();
                streamWriter.WriteLine("function lines_from(file)");
                streamWriter.WriteLine("\tlines = {}");
                streamWriter.WriteLine("\tfor line in io.lines(file) do");
                streamWriter.WriteLine("\t\tlines[#lines + 1] = line");
                streamWriter.WriteLine("\tend");
                streamWriter.WriteLine("\treturn lines");
                streamWriter.WriteLine("end");
                streamWriter.Close();
                Directory.CreateDirectory(folderBrowserDialog1.SelectedPath + "\\artwork\\crosshairs");
                foreach (FileInfo file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\m2emulator\\artwork\\crosshairs").GetFiles())
                    file.CopyTo(folderBrowserDialog1.SelectedPath + "\\artwork\\crosshairs\\" + file.Name, true);
                foreach (FileInfo file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\m2emulator\\scripts").GetFiles())
                    file.CopyTo(folderBrowserDialog1.SelectedPath + "\\scripts\\" + file.Name, true);
                MessageBox.Show("Scripts installed !");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't install m2emulator lua scripts : \n\n" + ex.Message.ToString(), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        /// <summary>
        /// Heavy Fire Afghanistan tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Btn_HF3_Browse_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "Please select \"Heavy Fire Afghanistan\" installation folder";
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _HF3_Path = folderBrowserDialog1.SelectedPath;
                Txt_HF3_Browse.Text = _HF3_Path;
            }
        }
        private void Txt_HF3_Browse_TextChanged(object sender, EventArgs e)
        {
            _HF3_Path = Txt_HF3_Browse.Text;
            if (Txt_HF3_Browse.Text.Length > 0)
            {
                Lbl_HFA_Version.Text = "Game version : ";
                Lbl_HFA_Command.Text = "DemulShooter parameter : ";
                Lbl_HFA_Version.Visible = true;
                Lbl_HFA_Command.Visible = true;

                //Getting game information
                string sMD5 = string.Empty;
                if (File.Exists(_HF3_Path + @"\HeavyFire3_Final.exe"))
                {
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = File.OpenRead(_HF3_Path + @"\HeavyFire3_Final.exe"))
                        {
                            //Getting md5 calculation of destination file
                            sMD5 = BitConverter.ToString(md5.ComputeHash(stream));
                            if (sMD5.Equals("18-41-E5-71-28-6A-CB-17-A9-85-46-A4-39-C0-8E-57"))
                            {
                                Lbl_HFA_Version.Text += "Steam release";
                                Lbl_HFA_Command.Text += "-rom=hfa_s or -rom=hfa2p_s";
                            }
                            else
                            {
                                Lbl_HFA_Version.Text += "Unknown version";
                                Lbl_HFA_Command.Visible = false;
                            }
                        }
                    }                      
                }
                else if (File.Exists(_HF3_Path + @"\HeavyFire3.exe"))
                {
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = File.OpenRead(_HF3_Path + @"\HeavyFire3.exe"))
                        {
                            sMD5 = BitConverter.ToString(md5.ComputeHash(stream));
                            if (sMD5.Equals("3F-49-95-1A-E8-23-28-17-A9-1E-F5-50-33-74-D6-B3"))
                            {
                                Lbl_HFA_Version.Text += "SKIDROW release";
                                Lbl_HFA_Command.Text += "-rom=hfa or -rom=hfa2p";
                            }
                            else
                            {
                                Lbl_HFA_Version.Text += "Unknown version";
                                Lbl_HFA_Command.Visible = false;
                            }
                        }
                    }
                }                
                else
                {
                    Lbl_HFA_Version.Text = "No known executable name found ! ";
                    Lbl_HFA_Command.Visible = false;
                }                
            }
            else
            {
                Lbl_HFA_Version.Visible = false;
                Lbl_HFA_Command.Visible = false;
            }
        }
        private void Btn_HF3_Install_Click(object sender, EventArgs e)
        {
            InstallHeavyFireDll(_HF3_Path);                  
        }
        private void InstallHeavyFireDll(string Path)
        {
            if (Path != string.Empty)
            {
                //Installing dinput8.dll DirectInput blocker  
                try
                {
                    File.WriteAllBytes(Path + @"\dinput8.dll", DemulShooter.Properties.Resources.dinput8_blocker);
                    MessageBox.Show(Path + @"\dinput8.dll succesfully installed", "DirectInput Blocker installation", MessageBoxButtons.OK, MessageBoxIcon.Information);                                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error installing " + Path + "\\dinput8.dll : \n\n" + ex.Message.ToString(), "DirectInput Blocker installation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                //Installing xinput1_3.dll
                try
                {
                    File.WriteAllBytes(Path + @"\xinput1_3.dll", DemulShooter.Properties.Resources.xinput1_3);
                    MessageBox.Show(Path + @"\xinput1_3.dll succesfully installed", "Xinput simulator installation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error installing " + Path + "\\xinput1_3.dll : \n\n" + ex.Message.ToString(), "Xinput simulator installation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }                        
            }
        }
        private void TrackBar_HF3_Cover_ValueChanged(object sender, EventArgs e)
        {
            _HF3_CoverSensibility = TrackBar_HF3_Cover.Value;
        }
        private void Btn_HF3_Save_Click(object sender, EventArgs e)
        {
            WriteConf();
        }

        /// <summary>
        /// Heavy Fire Shattered Spear tab
        /// </summary>
        private void Btn_HF4_Browse_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "Please select \"Heavy Fire Shattered Spear\" installation folder";
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _HF4_Path = folderBrowserDialog1.SelectedPath;
                Txt_HF4_Browse.Text = _HF4_Path;
            }
        }
        private void Txt_HF4_Browse_TextChanged(object sender, EventArgs e)
        {
            _HF4_Path = Txt_HF4_Browse.Text;
            if (Txt_HF4_Browse.Text.Length > 0)
            {
                Lbl_HF4_Version.Text = "Game version : ";
                Lbl_HF4_Command.Text = "DemulShooter parameter : ";
                Lbl_HF4_Version.Visible = true;
                Lbl_HF4_Command.Visible = true;

                //Getting game information
                string sMD5 = string.Empty;
                if (File.Exists(_HF4_Path + @"\hf4.exe"))
                {
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = File.OpenRead(_HF4_Path + @"\hf4.exe"))
                        {
                            //Getting md5 calculation of destination file
                            sMD5 = BitConverter.ToString(md5.ComputeHash(stream));
                            if (sMD5.Equals("7F-8B-F2-0A-AB-A8-0A-C1-23-9E-FC-55-3D-94-A5-3F"))
                            {
                                Lbl_HF4_Version.Text += "Steam release";
                                Lbl_HF4_Command.Text += "-rom=hfss_s or -rom=hfss2p_s";
                            }
                            else
                            {
                                Lbl_HF4_Version.Text += "Unknown version";
                                Lbl_HF4_Command.Visible = false;
                            }
                        }
                    }
                }
                else if (File.Exists(_HF4_Path + @"\HeavyFire4.exe"))
                {
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = File.OpenRead(_HF4_Path + @"\HeavyFire4.exe"))
                        {
                            sMD5 = BitConverter.ToString(md5.ComputeHash(stream));
                            if (sMD5.Equals("94-76-F9-BB-A4-8A-EA-6C-A0-4D-06-15-8B-E0-7F-1C"))
                            {
                                Lbl_HF4_Version.Text += "SKIDROW release";
                                Lbl_HF4_Command.Text += "-rom=hfss or -rom=hfss2p";
                            }
                            else
                            {
                                Lbl_HF4_Version.Text += "Unknown version";
                                Lbl_HF4_Command.Visible = false;
                            }
                        }
                    }
                }
                else
                {
                    Lbl_HF4_Version.Text = "No known executable name found ! ";
                    Lbl_HF4_Command.Visible = false;
                }
            }
            else
            {
                Lbl_HF4_Version.Visible = false;
                Lbl_HF4_Command.Visible = false;
            }
        }
        private void Btn_HF4_Install_Click(object sender, EventArgs e)
        {
            InstallHeavyFireDll(_HF4_Path);

            //Getting game information
            string sMD5 = string.Empty;
            if (File.Exists(_HF4_Path + @"\hf4.exe"))
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(_HF4_Path + @"\hf4.exe"))
                    {
                        //Getting md5 calculation of destination file
                        sMD5 = BitConverter.ToString(md5.ComputeHash(stream));
                        if (sMD5.Equals("7F-8B-F2-0A-AB-A8-0A-C1-23-9E-FC-55-3D-94-A5-3F"))
                            MessageBox.Show("Steam version of game found.\n\nUse -rom=hfss_s or -rom=hfss2p_s for DemulShooter", "Game exe informations", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Unknown version of the game, this may not work with DemulShooter", "Game exe informations", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            else if (File.Exists(_HF4_Path + @"\HeavyFire4.exe"))
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(_HF4_Path + @"\HeavyFire4.exe"))
                    {
                        sMD5 = BitConverter.ToString(md5.ComputeHash(stream));
                        if (sMD5.Equals("94-76-F9-BB-A4-8A-EA-6C-A0-4D-06-15-8B-E0-7F-1C"))
                            MessageBox.Show("SKIDROW version of game found.\n\nUse -rom=hfss or -rom=hfss2p for DemulShooter", "Game exe informations", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Unknown version of the game, this may not work with DemulShooter", "Game exe informations", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            else
            {
                MessageBox.Show("Can't find any known game executable. Please verify your path", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }    
        }
        private void TrackBar_HF4_Cover_ValueChanged(object sender, EventArgs e)
        {
            _HF4_CoverSensibility = TrackBar_HF4_Cover.Value;
        }
        private void Btn_HF4_Save_Click(object sender, EventArgs e)
        {
            WriteConf();
        }

        #endregion                  
        
        

        private void CrosshairAimTimer_Tick(object sender, EventArgs e)
        {
            Win32.InvalidateRect(IntPtr.Zero, IntPtr.Zero, true);
            CrosshairAimTimer.Stop();
        }

        #region WINDOW MESSAGE LOOP

        /*************** WM Boucle *********************/
        protected const int WM_INPUT = 0x00FF;
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_INPUT:
                    //read in new mouse values.
                    ProcessRawInput(m.LParam);
                    break;
            }
            base.WndProc(ref m);
        }

        #endregion

                                                              
    }        
}