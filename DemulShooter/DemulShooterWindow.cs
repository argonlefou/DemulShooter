using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.RawInput;
using DsCore.Win32;
using System.Threading;

namespace DemulShooter
{
    delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public class DemulShooterWindow : ApplicationContext
    {
        //A Message-Loop only window will be created to be able to receive WM_INPUT messages
        private IntPtr _RawMessageWnd_hWnd = IntPtr.Zero;
        private WndProc delegWndProc;        

        private Configurator _Configurator;
        private const string DEMULSHOOTER_CONF_FILENAME = "config.ini";

        //Tray Icon
        private ContextMenu _TrayIconMenu;
        private System.Windows.Forms.NotifyIcon _TrayIcon;

        //Available RawInput devices (filters by thir Type)
        private RawInputController[] _AvailableControllers;

        //Low-Level Hooks
        private Win32API.HookProc _MouseHookProc;
        private IntPtr _MouseHookID = IntPtr.Zero;
        private Win32API.HookProc _KeyboardHookProc;
        private IntPtr _KeyboardHookID = IntPtr.Zero;        

        //Output (MameHooker)
        private MameOutputHelper _OutputHelper;
        private Thread _OutputUpdateLoop;

        //Game options
        private Game _Game;
        private string _Rom = String.Empty;
        private string _Target = String.Empty;
        private UInt32 _Ddinumber = 3;
        private string _DemulVersion = String.Empty;
        private bool _DisableWindow = false;
        private bool _WidescreenHack = false;
        private bool _NoAutoReload = false;
        private bool _NoAutoFire = false;
        private bool _NoGuns = false;
        private bool _HideGameCrosshair = false;
        private bool _HardFfl = false;
        private double _ForceXratio = 0.0;
        //private bool _UseSingleMouse = true;
        
        public DemulShooterWindow(string[] Args, bool isVerbose)
        {
            //Stop program if Demulshooter already running
            Process[] pDemulShooter = Process.GetProcessesByName("DemulShooter");
            if (pDemulShooter.Length > 1)
            {
                MessageBox.Show("Another instance of DemulShooter is already running.\nPlease terminate it before launching a new one", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            //Creating TrayIcon and TrayIcon "Exit" menu
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            InitializeComponent();

            Logger.IsEnabled = isVerbose;
            Logger.WriteLog("");
            Logger.WriteLog("---------------- Program Start -- DemulShooter v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString() + " ----------------");

            // Parsing commandline arguments
            for (int i = 0; i < Args.Length; i++)
            {
                Logger.WriteLog("Cmdline arg " + i + " : " + Args[i]);
                if (Args[i].ToLower().StartsWith("-ddinumber"))
                {
                    try
                    {
                        //-1 to transform to a 0-based index for later calculation
                        _Ddinumber = UInt32.Parse((Args[i].Split('='))[1].Trim()) - 1;
                    }
                    catch
                    {
                        Logger.WriteLog("-ddinumber parameter not good, it will keep default value");
                    }
                }
                else if (Args[i].ToLower().Equals("-hardffl"))
                {
                    _HardFfl = true;
                }
                else if (Args[i].ToLower().StartsWith("-forcexratio"))
                {
                    String sX = (Args[i].Split('='))[1].Trim();
                    try
                    {
                        if (sX.Contains("/") && sX.Split('/').Length > 1)
                        {
                            double d1 = Double.Parse(sX.Split('/')[0]);
                            double d2 = Double.Parse(sX.Split('/')[1]);
                            _ForceXratio = d1 / d2;
                        }
                        else 
                            _ForceXratio = Double.Parse(sX, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        Logger.WriteLog("Can't set -ForcedXratio option : " + sX + " is not a valid value");
                    }                        
                }
                else if (Args[i].ToLower().Equals("-nocrosshair"))
                {
                    _HideGameCrosshair = true;
                }
                else if (Args[i].ToLower().Equals("-noautoreload"))
                {
                    _NoAutoReload = true;
                }
                else if (Args[i].ToLower().Equals("-noautofire"))
                {
                    _NoAutoFire = true;
                }
                else if (Args[i].ToLower().Equals("-noguns"))
                {
                    _NoGuns = true;
                }
                else if (Args[i].ToLower().Equals("-noresize"))
                {
                    _DisableWindow = true;
                }                
                if (Args[i].ToLower().StartsWith("-rom"))
                {
                    _Rom = (Args[i].Split('='))[1].Trim();
                }
                else if (Args[i].ToLower().StartsWith("-target"))
                {
                    _Target = (Args[i].Split('='))[1].Trim();
                    if (_Target.StartsWith("demul"))
                    {
                        _DemulVersion = _Target.Substring(5, 3);
                    }
                }
                else if (Args[i].ToLower().Equals("-widescreen"))
                {
                    _WidescreenHack = true;
                }                
            }
            if (_TrayIcon != null)
                _TrayIcon.Text += "[" + _Target + "] [" + _Rom + "]";

            //Finding plugged devices
            _AvailableControllers = RawInputHelper.GetRawInputDevices(new RawInputDeviceType[] { RawInputDeviceType.RIM_TYPEHID, RawInputDeviceType.RIM_TYPEMOUSE });
            Logger.WriteLog("Found " + _AvailableControllers.Length + " available RawInput devices :");
            foreach (RawInputController c in _AvailableControllers)
            {
                try
                {
                    Logger.WriteLog(" + [" + c.DeviceType.ToString() + "] " + c.DeviceName);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("ERROR : " + ex.Message.ToString());
                }
            }  

            //Reading config file to get parameters
            _Configurator = new Configurator();
            _Configurator.ReadDsConfig(AppDomain.CurrentDomain.BaseDirectory + @"\" + DEMULSHOOTER_CONF_FILENAME);
            foreach (PlayerSettings Player in _Configurator.PlayersSettings)
            {
                Logger.WriteLog("P" + Player.ID + " mode = " + Player.Mode);
                if (Player.Mode == PlayerSettings.PLAYER_MODE_RAWINPUT)
                {
                    bool bControllerfound = false;                        
                    Logger.WriteLog("P" + Player.ID + " device = " + Player.DeviceName);
                    foreach (RawInputController Controller in _AvailableControllers)
                    {
                        //Usually , the device name never change and it's easy to find back a controller
                        if (Controller.DeviceName == Player.DeviceName)
                        {
                            Player.RIController = Controller;
                            Player.RIController.Selected_AxisX = Player.HidAxisX;
                            Player.RIController.Selected_AxisY = Player.HidAxisY;
                            Player.RIController.Selected_OnScreenTriggerButton = Player.HidButton_OnScreenTrigger;
                            Player.RIController.Selected_ActionButton = Player.HidButton_Action;
                            Player.RIController.Selected_OffScreenTriggerButton = Player.HidButton_OffScreenTrigger;
                            Logger.WriteLog("P" + Player.ID + " device plugged and found, Handle = 0x" + Controller.DeviceHandle);
                            Logger.WriteLog("P" + Player.ID + " device : " + Controller.ManufacturerName + " / " + Controller.ProductName);
                            bControllerfound = true;
                            break;
                        }
                    }

                    //Unfortunatelly, on a few cases (SONY DS4 for example), a part of the device name changes so we will check again
                    //with what seems to be a fixed part of the name                        
                    if (!bControllerfound)
                    {
                        /*foreach (RawInputController Controller in _AvailableControllers)
                        {
                            //Usually , the device name never change and it's easy to find back a controller
                            if (Controller.DeviceName == Player.DeviceName)
                            {
                                Player.RIController = Controller;
                                Player.RIController.Set_AxisX(Player.HidAxisX);
                                Player.RIController.Set_AxisY(Player.HidAxisY);
                                Player.RIController.Set_Button_OnScreenTrigger_Index(Player.HidButton_OnScreenTrigger_Index);
                                Player.RIController.Set_Button_Action_Index(Player.HidButton_Action_Index);
                                Player.RIController.Set_Button_OffScreenTrigger_Index(Player.HidButton_OffScreenTrigger_Index);
                                Logger.WriteLog("P" + Player.ID + " device plugged and found, Handle = 0x" + Controller.DeviceHandle);
                                Logger.WriteLog("P" + Player.ID + " device : " + Controller.ProductName + " / " + Controller.DeviceName);
                                bControllerfound = true;
                                break;
                            }
                        }*/
                    }
                                                
                }
                else
                    Logger.WriteLog("P" + Player.ID + " Gamepad ID = " + Player.GamepadID);
            }

            CreateRawMessageWindow();
            //Register to RawInput thanks to the previously created window Handle
            RawInputDevice[] rid = new RawInputDevice[3];
            rid[0].UsagePage = HidUsagePage.GENERIC;
            rid[0].Usage = HidUsage.Joystick;
            rid[0].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
            rid[0].hwndTarget = _RawMessageWnd_hWnd;

            rid[1].UsagePage = HidUsagePage.GENERIC;
            rid[1].Usage = HidUsage.Mouse;
            rid[1].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
            rid[1].hwndTarget = _RawMessageWnd_hWnd;

            rid[2].UsagePage = HidUsagePage.GENERIC;
            rid[2].Usage = HidUsage.Gamepad;
            rid[2].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
            rid[2].hwndTarget = _RawMessageWnd_hWnd;
            if (!Win32API.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                MessageBox.Show("Failed to register raw input device(s).", "DemulShooter Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            //Starting Mame-style output daemon
            if (_Configurator.OutputEnabled)
            {
                Logger.WriteLog("Starting Output daemon...");
                _OutputHelper = new MameOutputHelper(_RawMessageWnd_hWnd, _Configurator.OutputCustomRecoilDelay, _Configurator.OutputCustomDamagedDelay);
                _OutputHelper.Start();
                _OutputUpdateLoop = new Thread(new ThreadStart(ReadAndSendOutput_Thread));
                _OutputUpdateLoop.Start();
            }

            //Starting the fun...
            if (_Target.Length > 0 && (_Rom.Length > 0 || _Target.StartsWith("dolphin")))
            {
                //Install Low-Level mouse hook
                ApplyMouseHook();

                //Install Low Level keyboard hook
                ApplyKeyboardHook();

                /*
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
                */

                if (_Target.Equals("chihiro"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "vcop3":
                            {
                                _Game = new Game_CxbxVcop3(_Rom.ToLower(), _ForceXratio, isVerbose);
                            }; break;
                        case "vcop3_old":
                            {
                                _Game = new Game_CxbxVcop3_Old(_Rom.ToLower(), _ForceXratio, isVerbose);
                            }; break;
                    }
                }

                //Demul games
                else if (_Target.StartsWith("demul"))
                {
                    if (_Rom.ToLower().Equals("confmiss") || _Rom.ToLower().Equals("deathcox") || _Rom.ToLower().StartsWith("hotd2")
                        || _Rom.ToLower().Equals("lupinsho") || _Rom.ToLower().Equals("mok"))
                    {
                        _Game = new Game_DemulNaomi(_Rom.ToLower(), _DemulVersion, _ForceXratio, isVerbose, _DisableWindow, _WidescreenHack);
                    }
                    else if (_Rom.ToLower().StartsWith("ninjaslt"))
                    {
                        _Game = new Game_DemulJvs(_Rom.ToLower(), _DemulVersion, _ForceXratio, isVerbose, _DisableWindow, _WidescreenHack);
                    }
                    else if (_Rom.ToLower().Equals("braveff"))
                    {
                        _Game = new Game_DemulHikaru(_Rom.ToLower(), _DemulVersion, _ForceXratio, isVerbose, _DisableWindow, _WidescreenHack);
                    }
                    else if (_Rom.ToLower().Equals("manicpnc") || _Rom.ToLower().Equals("pokasuka"))
                    {
                        _Game = new Game_DemulManicpnc(_Rom.ToLower(), _ForceXratio, isVerbose, _DisableWindow, _WidescreenHack);
                    }
                    else
                    {
                        _Game = new Game_DemulAtomiswave(_Rom.ToLower(), _DemulVersion, _ForceXratio, isVerbose, _DisableWindow, _WidescreenHack);
                    }
                }
    
                //Dolphin
                else if (_Target.Equals("dolphin5"))
                {
                    _Game = new Game_Dolphin5(_Rom.ToLower(), _Ddinumber, _ForceXratio, isVerbose);
                }

                //GlobalVR game
                else if (_Target.Equals("globalvr"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "aliens":
                            {
                                _Game = new Game_GvrAliens(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "farcry":
                            {
                                _Game = new Game_GvrFarCryV2(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "fearland":
                            {
                                _Game = new Game_GvrFearLand(_Rom.ToLower(), _HardFfl, _ForceXratio, isVerbose);
                            } break;
                        default:
                            break;
                    }
                }

                //Lindbergh
                else if (_Target.Equals("lindbergh"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "2spicy":
                            {
                                _Game = new Game_Lindbergh2spicy(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "hotd4":
                            {
                                _Game = new Game_LindberghHotd4(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "lgj":
                            {
                                _Game = new Game_LindberghLgj(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "lgjsp":
                            {
                                _Game = new Game_LindberghLgjsp(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "rambo":
                            {
                                _Game = new Game_LindberghRambo(_Rom.ToLower(), _HideGameCrosshair, _ForceXratio, isVerbose);
                            } break;
                        default:
                            break;
                    }
                }

                //All model2 roms share same method
                else if (_Target.Equals("model2"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "bel":
                            {
                                _Game = new Game_Model2Bel(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "gunblade":
                            {
                                _Game = new Game_Model2Gunblade(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "hotd":
                            {
                                _Game = new Game_Model2Hotd(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "rchase2":
                            {
                                _Game = new Game_Model2Rchase2(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "vcop":
                            {
                                _Game = new Game_Model2Vcop(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "vcop2":
                            {
                                _Game = new Game_Model2Vcop2(_Rom.ToLower(), _ForceXratio, isVerbose);
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
                                _Game = new Game_RwSGG(_Rom.ToLower(), _NoAutoFire, _ForceXratio, isVerbose);
                            } break;
                        case "lgi":
                            {
                                _Game = new Game_RwLGI(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "lgi3d":
                            {
                                _Game = new Game_RwLGI3D(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "og":
                            {
                                _Game = new Game_RwOpGhost(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "sdr":
                            {
                                _Game = new Game_RwSDR(_Rom.ToLower(), _ForceXratio, isVerbose);
                            }; break;
                        case "tha":
                            {
                                _Game = new Game_RwTransformers(_Rom.ToLower(), _ForceXratio, isVerbose);
                            }; break;
                        default:
                            break;
                    }
                }

                //TTX game
                else if (_Target.Equals("ttx"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "bkbs":
                            {
                                _Game = new Game_TtxBlockKingBallShooter(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "eadp":
                            {
                                _Game = new Game_TtxEadp(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "gattack4":
                            {
                                _Game = new Game_TtxGaiaAttack4(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "gsoz":
                            {
                                _Game = new Game_TtxGundam(_Rom.ToLower(), _Configurator.Gsoz_Pedal_P1_Enabled, _Configurator.DIK_Gsoz_Pedal_P1, _Configurator.Gsoz_Pedal_P2_Enabled, _Configurator.DIK_Gsoz_Pedal_P2, _ForceXratio, isVerbose);
                            } break;
                        case "gsoz2p":
                            {
                                _Game = new Game_TtxGundam(_Rom.ToLower(), _Configurator.Gsoz_Pedal_P1_Enabled, _Configurator.DIK_Gsoz_Pedal_P1, _Configurator.Gsoz_Pedal_P2_Enabled, _Configurator.DIK_Gsoz_Pedal_P2, _ForceXratio, isVerbose);
                            } break;
                        case "hmuseum":
                            {
                                _Game = new Game_TtxHauntedMuseum(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "hmuseum2":
                            {
                                _Game = new Game_TtxHauntedMuseum2(_Rom.ToLower(), _HardFfl,_ForceXratio, isVerbose);
                            } break;
                        case "mgungun2":
                            {
                                _Game = new Game_TtxGungun2(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "sha":
                            {
                                _Game = new Game_TtxSha(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;                        
                    }
                }

                //Windows Games
                else if (_Target.Equals("windows"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "artdead":
                            {
                                _Game = new Game_ArtIsDead(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                        case "friction":
                            {
                                _Game = new Game_Friction(_Rom.ToLower(), _ForceXratio, isVerbose);
                            }break;
                        case "hfa":
                            {
                                _Game = new Game_HeavyFire3Pc("hfa", _Configurator.HF3_Path, _Configurator.HF3_CoverSensibility, false, isVerbose);
                            }; break;
                        case "hfa2p":
                            {
                                _Game = new Game_HeavyFire3Pc("hfa", _Configurator.HF3_Path, _Configurator.HF3_CoverSensibility, true, isVerbose);
                            }; break;
                        case "hfss":
                            {
                                _Game = new Game_HeavyFire4Pc("hfss", _Configurator.HF4_Path, _Configurator.HF4_CoverSensibility, false, isVerbose);
                            }; break;
                        case "hfss2p":
                            {
                                _Game = new Game_HeavyFire4Pc("hfss", _Configurator.HF4_Path, _Configurator.HF4_CoverSensibility, true, isVerbose);
                            }; break;
                        case "hod2pc":
                            {
                                _Game = new Game_Hod2pc(_Rom.ToLower(), _ForceXratio, isVerbose);
                            }; break;
                        case "hod3pc":
                            {
                                _Game = new Game_Hod3pc(_Rom.ToLower(), _NoAutoReload, _NoGuns,_ForceXratio, isVerbose);
                            }; break;
                        case "hodo":
                            {
                                _Game = new Game_HotdoPc(_Rom.ToLower(), isVerbose);
                            }; break;
                        case "reload":
                            {
                                _Game = new Game_Reload(_Rom.ToLower(), _HideGameCrosshair, isVerbose);
                            }; break;
                    }
                }

                //W.I.P Games
                else if (_Target.Equals("wip"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "wartran":
                            {
                                _Game = new Game_Wartran(_Rom.ToLower(), _ForceXratio, isVerbose);
                            } break;
                    }
                }
            }
        }

        /// <summary>
        /// Create a messageLoop-only Window (invsible) to treat WM_* messages
        /// </summary>
        /// <returns></returns>
        public bool CreateRawMessageWindow()
        {
            delegWndProc = myWndProc;

            WNDCLASSEX wind_class = new WNDCLASSEX();
            wind_class.cbSize = Marshal.SizeOf(typeof(WNDCLASSEX));
            wind_class.style = 0;
            wind_class.hbrBackground = IntPtr.Zero;
            wind_class.cbClsExtra = 0;
            wind_class.cbWndExtra = 0;
            wind_class.hInstance = Marshal.GetHINSTANCE(this.GetType().Module); // alternative: Process.GetCurrentProcess().Handle;
            wind_class.hIcon = IntPtr.Zero;
            wind_class.hCursor = IntPtr.Zero;
            wind_class.lpszMenuName = null;
            wind_class.lpszClassName = "RI_MsgLoop";
            wind_class.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(delegWndProc);
            wind_class.hIconSm = IntPtr.Zero;
            ushort regResult = Win32API.RegisterClassEx(ref wind_class);

            if (regResult == 0)
            {
                uint error = Win32API.GetLastError();
                return false;
            }
            string wndClass = wind_class.lpszClassName;

            //This version worked and resulted in a non-zero hWnd
            //_hWnd = CreateWindowEx(0, regResult, "Hello Win32", WS_OVERLAPPEDWINDOW | WS_VISIBLE, 0, 0, 300, 400, IntPtr.Zero, IntPtr.Zero, wind_class.hInstance, IntPtr.Zero);

            IntPtr HWND_MESSAGE = new IntPtr(-3);
            _RawMessageWnd_hWnd = Win32API.CreateWindowEx(0, regResult, "DemulShooter_RawInputWnd", 0, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, wind_class.hInstance, IntPtr.Zero);


            if (_RawMessageWnd_hWnd == ((IntPtr)0))
            {
                uint error = Win32API.GetLastError();
                return false;
            }
            return true;

            //The explicit message pump is not necessary, messages are obviously dispatched by the framework.
            //However, if the while loop is implemented, the functions are called... Windows mysteries...
            //MSG msg;
            //while (GetMessage(out msg, IntPtr.Zero, 0, 0) != 0)
            //{
            //    TranslateMessage(ref msg);
            //    DispatchMessage(ref msg);
            //}
        }

        /// <summary>
        /// Window Message-Loop
        /// </summary>
        private IntPtr myWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (_Configurator.OutputEnabled && _OutputHelper != null)
            {
                if (msg == _OutputHelper.MameOutput_RegisterClient)
                {
                    _OutputHelper.RegisterClient(wParam, (UInt32)lParam);
                }
                else if (msg == _OutputHelper.MameOutput_UnregisterClient)
                {
                    _OutputHelper.UnregisterClient(wParam, (UInt32)lParam);
                }
                else if (msg == _OutputHelper.MameOutput_GetIdString)
                {
                    uint Id = (uint)lParam;
                    if (Id == 0)
                    {
                        _OutputHelper.SendIdString(wParam, _Rom, 0);
                    }
                    else
                    {
                        if (_Game != null && _Game.Outputs.Count > 0)
                        {
                            String s = _Game.GetOutputDescriptionFromId(Id);
                            _OutputHelper.SendIdString(wParam, s, Id);
                        }
                    }
                }
            }

            switch (msg)
            {
                case Win32Define.WM_INPUT:
                    {
                        ProcessRawInputMessage(lParam);
                    }break;                

                default:
                    break;
            }
            return Win32API.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        /// <summary>
        /// Processing of the RawInput message :
        /// - Detection of the device creating the event
        /// - Retrieve data
        /// - Scale Raw axis values to Screen -> ClientWindow -> Game values
        /// - Send axis values and buttons to the Game
        /// </summary>
        /// <param name="RawInputHandle"></param>
        private void ProcessRawInputMessage(IntPtr RawInputHandle)
        {
            foreach (RawInputController Controller in _AvailableControllers)
            {
                if (Controller.isSourceOfRawInputMessage(RawInputHandle))
                {
                    foreach (PlayerSettings Player in _Configurator.PlayersSettings)
                    {
                        if (Player.DeviceName == Controller.DeviceName)
                        {
                            if (_Game != null && _Game.ProcessHooked)
                            {
                                Controller.ProcessRawInputData(RawInputHandle);

                                Logger.WriteLog("RawData event for Player #" + Player.ID.ToString() + ":");
                                Logger.WriteLog("Device rawinput data (Hex) = [ " + Player.RIController.Computed_X.ToString("X8") + ", " + Player.RIController.Computed_Y.ToString("X8") + " ]");

                                _Game.GetScreenResolution();
                                Logger.WriteLog("PrimaryScreen Size (Px) = [ " + _Game.ScreenWidth + "x" + _Game.ScreenHeight + " ]");

                                //If manual calibration override for analog guns
                                if (Player.RIController.DeviceType == RawInputDeviceType.RIM_TYPEHID && Player.AnalogAxisRangeOverride)
                                {
                                    Logger.WriteLog("Overriding player axis range values : X => [ " + Player.AnalogManual_Xmin.ToString() + ", " + Player.AnalogManual_Xmax.ToString() + " ], Y => [ " + Player.AnalogManual_Ymin.ToString() + ", " + Player.AnalogManual_Ymax.ToString() + " ]");
                                    Player.RIController.Computed_X = _Game.ScreenScale(Player.RIController.Computed_X, Player.AnalogManual_Xmin, Player.AnalogManual_Xmax, 0, _Game.ScreenWidth);
                                    Player.RIController.Computed_Y = _Game.ScreenScale(Player.RIController.Computed_Y, Player.AnalogManual_Ymin, Player.AnalogManual_Ymax, 0, _Game.ScreenHeight);                                
                                }                                
                                else
                                {
                                    Player.RIController.Computed_X = _Game.ScreenScale(Player.RIController.Computed_X, Player.RIController.Axis_X_Min, Player.RIController.Axis_X_Max, 0, _Game.ScreenWidth);
                                    Player.RIController.Computed_Y = _Game.ScreenScale(Player.RIController.Computed_Y, Player.RIController.Axis_Y_Min, Player.RIController.Axis_Y_Max, 0, _Game.ScreenHeight);
                                }
                                
                                //Optionnal invert axis
                                if (Player.InvertAxis_X)
                                    Player.RIController.Computed_X = _Game.ScreenWidth - Player.RIController.Computed_X;
                                if (Player.InvertAxis_Y)
                                    Player.RIController.Computed_Y = _Game.ScreenHeight - Player.RIController.Computed_Y;
                                
                                Logger.WriteLog("OnScreen Cursor Position (Px) = [ " + Player.RIController.Computed_X + ", " + Player.RIController.Computed_Y + " ]");

                                if (_Configurator.Act_Labs_Offset_Enable)
                                {
                                    Player.RIController.Computed_X += Player.Act_Labs_Offset_X;
                                    Player.RIController.Computed_Y += Player.Act_Labs_Offset_Y;
                                    Logger.WriteLog("ActLabs adaptated OnScreen Cursor Position (Px) = [ " + Player.RIController.Computed_X + ", " + Player.RIController.Computed_Y + " ]");
                                }

                                if (!_Game.ClientScale(Player))
                                {
                                    Logger.WriteLog("Error converting screen location to client location");
                                    return;
                                }
                                Logger.WriteLog("OnClient Cursor Position (Px) = [ " + Player.RIController.Computed_X + ", " + Player.RIController.Computed_Y + " ]");

                                if (!_Game.GameScale(Player))
                                {
                                    Logger.WriteLog("Error converting client location to game location");
                                    return;
                                }

                                Logger.WriteLog("Game Position (Hex) = [ " + Player.RIController.Computed_X.ToString("X4") + ", " + Player.RIController.Computed_Y.ToString("X4") + " ]");
                                Logger.WriteLog("Game Position (Dec) = [ " + Player.RIController.Computed_X.ToString() + ", " + Player.RIController.Computed_Y.ToString() + " ]");
                                if (Controller.Computed_Buttons != 0)
                                    Logger.WriteLog("Controller Buttons Events : " + Player.RIController.Computed_Buttons.ToString());
                                Logger.WriteLog("-");

                                _Game.SendInput(Player);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Output handling thread :
        /// This infinite loop will check the targeted game values and send them to registerd Output clients
        /// </summary>
        private void ReadAndSendOutput_Thread()
        {
            while (true)
            {
                if (_Game != null && _Game.ProcessHooked)
                {
                    _Game.UpdateOutputValues();
                    _OutputHelper.SendValues(_Game.Outputs);
                }

                Thread.Sleep(_Configurator.OutputPollingDelay);
            }
        }

        /// <summary>
        /// GUI Init : mostly TrayIcon + Menu
        /// </summary>
        private void InitializeComponent()
        {
            //Tray Icon
            //Looking for explorer.exe process, if not skip TrayIcon to make the program work
            Process[] pExplorer = Process.GetProcessesByName("explorer");
            if (pExplorer.Length > 0)
            {
                _TrayIcon = new NotifyIcon();
                _TrayIcon.Text = "DemulShooter";
                _TrayIcon.Icon = DemulShooter.Properties.Resources.DemulShooter_Icon;
                _TrayIconMenu = new ContextMenu();
                _TrayIconMenu.MenuItems.Add("Exit", OnTrayExitSelected);
                _TrayIcon.ContextMenu = _TrayIconMenu;
                _TrayIcon.Visible = true;
            }
        }

        /// <summary>
        /// Exit from TrayIcon menu entry
        /// </summary>
        private void OnTrayExitSelected(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Application Exit cleanup
        /// </summary>
        private void OnApplicationExit(object sender, EventArgs e)
        {
            if (_OutputUpdateLoop != null)
                _OutputUpdateLoop.Abort();

            if (_OutputHelper != null)
                _OutputHelper.Stop();

            //Cleanup so that the icon will be removed when the application is closed
            if (_TrayIcon != null)
            {
                _TrayIcon.Visible = false;
                _TrayIcon.Dispose();
            }
        }

        /// <summary>
        /// Low-level mouse hook.
        /// Some game will need this to block mouse inputs so that we can inject our own values.
        /// </summary>
        protected void ApplyMouseHook()
        {
            _MouseHookProc = new Win32API.HookProc(MouseHookCallback);
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
                _MouseHookID = Win32API.SetWindowsHookEx(Win32Define.WH_MOUSE_LL, _MouseHookProc, Win32API.GetModuleHandle(curModule.ModuleName), 0);
            if (_MouseHookID == IntPtr.Zero)
            {
                Logger.WriteLog("MouseHook Error : " + Marshal.GetLastWin32Error());
            }
            else
            {
                Logger.WriteLog("LowLevelMouseHook installed !");
            } 
        }
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            /*if (_UseSingleMouse)
            {
                if (nCode >= 0 && (UInt32)wParam == Win32Define.WM_MOUSEMOVE)
                {
                    MSLLHOOKSTRUCT s = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    int x = s.pt.X;
                    int y = s.pt.Y;
                }
            }*/

            if (_Game != null && _Game.ProcessHooked)                
                return _Game.MouseHookCallback(_MouseHookID, nCode, wParam, lParam);
            else
                return Win32API.CallNextHookEx(_MouseHookID, nCode, wParam, lParam);
        }
        protected void RemoveMouseHook()
        {
            Win32API.UnhookWindowsHookEx(_MouseHookID);
        }

        /// <summary>
        /// Low-level Keyboard hook.
        /// </summary>
        protected void ApplyKeyboardHook()
        {
            _KeyboardHookProc = new Win32API.HookProc(KeyboardHookCallback);
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
                _KeyboardHookID = Win32API.SetWindowsHookEx(Win32Define.WH_KEYBOARD_LL, _KeyboardHookProc, Win32API.GetModuleHandle(curModule.ModuleName), 0);
            if (_KeyboardHookID == IntPtr.Zero)
            {
                Logger.WriteLog("KeyboardHook Error : " + Marshal.GetLastWin32Error());
            }
            else
            {
                Logger.WriteLog("LowLevel-KeyboardHook installed !");
            }
        }
        protected virtual IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (_Game != null && _Game.ProcessHooked)
            {
                //First step : use the Hook to determine if a virtual Middle/Right button has been pushed
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                    foreach (PlayerSettings Player in _Configurator.PlayersSettings)
                    {
                        if (Player.isVirtualMouseButtonsEnabled)
                        {
                            if (s.scanCode == Player.DIK_VirtualMouseButton_Left)
                            {
                                Player.RIController.Computed_Buttons = RawInputcontrollerButtonEvent.OnScreenTriggerDown;
                                _Game.SendInput(Player);
                            }
                            else if (s.scanCode == Player.DIK_VirtualMouseButton_Middle)
                            {
                                Player.RIController.Computed_Buttons = RawInputcontrollerButtonEvent.ActionDown;
                                _Game.SendInput(Player);
                            }
                            else if (s.scanCode == Player.DIK_VirtualMouseButton_Right)
                            {
                                Player.RIController.Computed_Buttons = RawInputcontrollerButtonEvent.OffScreenTriggerDown;
                                _Game.SendInput(Player);
                            }
                        }
                    }
                }
                if ((UInt32)wParam == Win32Define.WM_KEYUP)
                {
                    KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                    foreach (PlayerSettings Player in _Configurator.PlayersSettings)
                    {
                        if (Player.isVirtualMouseButtonsEnabled)
                        {
                            if (s.scanCode == Player.DIK_VirtualMouseButton_Left)
                            {
                                Player.RIController.Computed_Buttons = RawInputcontrollerButtonEvent.OnScreenTriggerUp;
                                _Game.SendInput(Player);
                            }
                            else if (s.scanCode == Player.DIK_VirtualMouseButton_Middle)
                            {
                                Player.RIController.Computed_Buttons = RawInputcontrollerButtonEvent.ActionUp;
                                _Game.SendInput(Player);
                            }
                            else if (s.scanCode == Player.DIK_VirtualMouseButton_Right)
                            {
                                Player.RIController.Computed_Buttons = RawInputcontrollerButtonEvent.OffScreenTriggerUp;
                                _Game.SendInput(Player);
                            }
                        }
                    }
                }

                //Second step : forward the event to the Game
                return _Game.KeyboardHookCallback(_MouseHookID, nCode, wParam, lParam);
            }            
            else
                return Win32API.CallNextHookEx(_KeyboardHookID, nCode, wParam, lParam);
        }
        protected void RemoveKeyboardHook()
        {
            Win32API.UnhookWindowsHookEx(_KeyboardHookID);
        }     
    }
}
