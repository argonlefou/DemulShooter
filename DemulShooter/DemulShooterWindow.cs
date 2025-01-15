﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public class DemulShooterWindow : ApplicationContext
    {
        //A Message-Loop only window will be created to be able to receive WM_INPUT messages
        private IntPtr _RawMessageWnd_hWnd = IntPtr.Zero;
        private WndProc delegWndProc;        

        private const string DEMULSHOOTER_CONF_FILENAME = "config.ini";
        private string _UserDefinedIniFile = string.Empty;

        //Timer for Hooking TimeOut
        private System.Timers.Timer _TimerHookTimeout;

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
        private Wm_OutputHelper _Wm_OutputHelper;
        //Output (Network)
        private Net_OutputHelper _Net_OutputHelper;
        private Thread _OutputUpdateLoop;

        //Game options
        private Game _Game;
        private string _Rom = String.Empty;
        private string _Target = String.Empty;
        private UInt32 _Ddinumber = 3;
        private string _DemulVersion = String.Empty;
        private bool _HardFfl = false;
        private double _ForceScalingX = 1.0;
        private bool _UseSingleMouse = false;
        private bool _NoInput = false;

        //InterProcessCommunication (Memory Mapped Files)
        private const String DEMULSHOOTER_INPUTS_MMF_NAME = "DemulShooter_MMF_Inputs";
        private const String DEMULSHOOTER_OUTPUTS_MMF_NAME = "DemulShooter_MMF_Outputs";
        private const String DEMULSHOOTER_INPUTS_MUTEX_NAME = "DemulShooter_Inputs_Mutex";
        private const String DEMULSHOOTER_OUTPUTS_MUTEX_NAME = "DemulShooter_Outputs_Mutex";
        private bool _EnableInputsIpc = false;
        private bool _EnableOutputsIpc = false;
        private DsCore.IPC.MemoryMappedFileHelper_Old _MMF_Inputs;
        private DsCore.IPC.MemoryMappedFileHelper_Old _MMF_Outputs;
        
        public DemulShooterWindow(string[] Args, bool isVerbose, bool enableTrace)
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

            //Creating the timeout Timer
            _TimerHookTimeout = new System.Timers.Timer();
            _TimerHookTimeout.Enabled = false;
            _TimerHookTimeout.Elapsed += tHookTimeOut_Elapsed;

            Logger.IsEnabled = isVerbose;
            Logger.IsTraceEnabled = enableTrace;
            Logger.InitLogFileName();
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
                else if (Args[i].ToLower().StartsWith("-forcescalingx"))
                {
                    String sX = (Args[i].Split('='))[1].Trim();
                    try
                    {
                        if (sX.Contains("/") && sX.Split('/').Length > 1)
                        {
                            double d1 = Double.Parse(sX.Split('/')[0]);
                            double d2 = Double.Parse(sX.Split('/')[1]);
                            _ForceScalingX = d1 / d2;
                        }
                        else 
                            _ForceScalingX = Double.Parse(sX, CultureInfo.InvariantCulture);
                        Logger.WriteLog("-ForceScalingX parameter set to " + _ForceScalingX.ToString());
                    }
                    catch
                    {
                        Logger.WriteLog("Can't set -ForceScalingX option : " + sX + " is not a valid value");
                    }                        
                }
                else if (Args[i].ToLower().Equals("-hardffl"))
                {
                    _HardFfl = true;
                }
                else if (Args[i].ToLower().Equals("-ipcinputs"))
                {
                    _EnableInputsIpc = true;
                }
                else if (Args[i].ToLower().Equals("-ipcoutputs"))
                {
                    _EnableOutputsIpc = true;
                }
                else if (Args[i].ToLower().Equals("-noinput"))
                {
                    _NoInput = true;
                } 
                else if (Args[i].ToLower().StartsWith("-profile"))
                {
                    _UserDefinedIniFile = (Args[i].Split('='))[1].Trim();
                }       
                else if (Args[i].ToLower().StartsWith("-rom"))
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
                else if (Args[i].ToLower().Equals("-usesinglemouse"))
                {
                    _UseSingleMouse = true;
                }
                              
            }
            if (_TrayIcon != null)
                _TrayIcon.Text += "[" + _Target + "] [" + _Rom + "]";

            Logger.WriteLog("Running as Administrator : " + IsRunningAsAdmin().ToString());

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
            if (_UserDefinedIniFile != string.Empty)
                Configurator.GetInstance().ReadDsConfig(_UserDefinedIniFile);
            else
                Configurator.GetInstance().ReadDsConfig(AppDomain.CurrentDomain.BaseDirectory + @"\" + DEMULSHOOTER_CONF_FILENAME);

            foreach (PlayerSettings Player in Configurator.GetInstance().PlayersSettings)
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

            //Info on Monitor (max resolution)

            //// Disabled for now -- may cause trouble on some computer( ???)
            /*
            try
            {
                var scope = new System.Management.ManagementScope();
                var q = new System.Management.ObjectQuery("SELECT * FROM CIM_VideoControllerResolution");
                String Maxres = String.Empty;
                using (var searcher = new System.Management.ManagementObjectSearcher(scope, q))
                {
                    var results = searcher.Get();
                    foreach (var item in results)
                    {
                        Maxres = item["Caption"].ToString();
                    }
                }
                Logger.WriteLog("Monitor maximum resolution = " + Maxres);
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("Error detecting monitor maximum resolution : " + Ex.Message.ToString());
            }
            */

            //Setting up IPC for inputs/outputs
            if (_EnableInputsIpc)
            {
                _MMF_Inputs = new DsCore.IPC.MemoryMappedFileHelper_Old(DEMULSHOOTER_INPUTS_MUTEX_NAME);
                _MMF_Inputs.MMFInit(DEMULSHOOTER_INPUTS_MMF_NAME, 2048);
            }
            if (_EnableOutputsIpc)
            {
                _MMF_Outputs = new DsCore.IPC.MemoryMappedFileHelper_Old(DEMULSHOOTER_OUTPUTS_MUTEX_NAME);
                _MMF_Outputs.MMFInit(DEMULSHOOTER_OUTPUTS_MMF_NAME, 2048);
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
            if (Configurator.GetInstance().OutputEnabled)
            {
                Logger.WriteLog("Starting Output daemon...");
                if (Configurator.GetInstance().Wm_OutputEnabled)
                {
                    Logger.WriteLog("Creating Window Message Output Helper...");
                    _Wm_OutputHelper = new Wm_OutputHelper(_RawMessageWnd_hWnd);
                    _Wm_OutputHelper.Start();
                }
                if (Configurator.GetInstance().Net_OutputEnabled)
                {
                    Logger.WriteLog("Creating Network Output Helper...");
                    _Net_OutputHelper = new Net_OutputHelper(_Rom);
                    _Net_OutputHelper.Start();
                }

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

                 //Coastal Games
                if (_Target.Equals("coastal"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "wws":
                            {
                                _Game = new Game_CoastalWws(_Rom.ToLower());
                            }; break;

                        default : 
                            break;
                    }
                }

                //Chihiro games
                else if (_Target.Equals("chihiro"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "vcop3":
                            {
                                _Game = new Game_CxbxVcop3(_Rom.ToLower());
                            }; break;

                        default : 
                            break;
                    }
                }

                //Demul games
                else if (_Target.StartsWith("demul"))
                {
                    if (_Rom.ToLower().Equals("confmiss") || _Rom.ToLower().Equals("deathcox") || _Rom.ToLower().StartsWith("hotd2")
                        || _Rom.ToLower().Equals("lupinsho") || _Rom.ToLower().Equals("mok"))
                    {
                        _Game = new Game_DemulNaomi(_Rom.ToLower(), _DemulVersion);
                    }
                    else if (_Rom.ToLower().StartsWith("ninjaslt"))
                    {
                        _Game = new Game_DemulJvs(_Rom.ToLower(), _DemulVersion);
                    }
                    else if (_Rom.ToLower().Equals("braveff"))
                    {
                        _Game = new Game_DemulHikaru(_Rom.ToLower(), _DemulVersion);
                    }
                    else if (_Rom.ToLower().Equals("manicpnc") || _Rom.ToLower().Equals("pokasuka"))
                    {
                        _Game = new Game_DemulManicpnc(_Rom.ToLower());
                    }
                    else
                    {
                        _Game = new Game_DemulAtomiswave(_Rom.ToLower(), _DemulVersion);
                    }
                }
    
                //Dolphin
                else if (_Target.Equals("dolphin5"))
                {
                    _Game = new Game_Dolphin5(_Rom.ToLower(), _Ddinumber);
                }

                    //Es4
                else if (_Target.Equals("es4"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "pblankx":
                            {
                                _Game = new Game_Es4PointBlankX(_Rom.ToLower());
                            }; break;

                        default: 
                            break;
                    }
                }

                //Gamewax game
                else if (_Target.Equals("gamewax"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "akuma":
                            {
                                _Game = new Game_WaxAkuma(_Rom.ToLower());
                            } break;

                        default : 
                            break;
                    }
                }

                //GlobalVR game
                else if (_Target.Equals("globalvr"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "aliens":
                            {
                                _Game = new Game_GvrAliens(_Rom.ToLower());
                            } break;
                        case "farcry":
                            {
                                _Game = new Game_GvrFarCry(_Rom.ToLower());
                            } break;
                        case "fearland":
                            {
                                _Game = new Game_GvrFearLand(_Rom.ToLower(), _HardFfl);
                            } break;
                        default:
                            break;
                    }
                }

                    //GlobalVR game
                else if (_Target.Equals("ice"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "gbusters":
                            {
                                _Game = new Game_IceGhostBusters(_Rom.ToLower());
                            } break;
                        default:
                            break;
                    }
                }

                //KONAMI Arcade
                else if (_Target.Equals("konami"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "hcv":
                            {
                                _Game = new Game_KonamiCastlevania(_Rom.ToLower());
                            } break;
                        case "le3":
                            {
                                _Game = new Game_KonamiLethalEnforcers3(_Rom.ToLower());
                            } break;
                        case "wartran":
                            {
                                _Game = new Game_KonamiWartran(_Rom.ToLower());
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
                                _Game = new Game_Lindbergh2spicy(_Rom.ToLower());
                            } break;
                        case "gsquad":
                            {
                                _Game = new Game_LindberghGhostSquadEvo(_Rom.ToLower());
                            } break;
                        case "hotd4":
                            {
                                _Game = new Game_LindberghHotd4(_Rom.ToLower());
                            } break;
                        case "hotd4sp":
                            {
                                _Game = new Game_LindberghHotd4Sp(_Rom.ToLower());
                            } break;
                        case "hotdex":
                            {
                                _Game = new Game_LindberghHotdEx(_Rom.ToLower());
                            } break;
                        case "lgj":
                            {
                                _Game = new Game_LindberghLgj(_Rom.ToLower());
                            } break;
                        case "lgjsp":
                            {
                                _Game = new Game_LindberghLgjsp(_Rom.ToLower());
                            } break;
                        case "rambo":
                            {
                                _Game = new Game_LindberghRambo(_Rom.ToLower());
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
                                _Game = new Game_Model2Bel(_Rom.ToLower());
                            } break;
                        case "gunblade":
                            {
                                _Game = new Game_Model2Gunblade(_Rom.ToLower());
                            } break;
                        case "hotd":
                            {
                                _Game = new Game_Model2Hotd(_Rom.ToLower());
                            } break;
                        case "rchase2":
                            {
                                _Game = new Game_Model2Rchase2(_Rom.ToLower());
                            } break;
                        case "vcop":
                            {
                                _Game = new Game_Model2Vcop(_Rom.ToLower());
                            } break;
                        case "vcop2":
                            {
                                _Game = new Game_Model2Vcop2(_Rom.ToLower());
                            } break;
                        default:
                            break;
                    }
                }
                //All model2 roms share same method
                else if (_Target.Equals("ppmarket"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "policetr2":
                            {
                                _Game = new Game_PpmPoliceTrainer2(_Rom.ToLower());
                            } break;
                        default:
                            break;
                    }
                }

                //Raw Thrill Games
                else if (_Target.Equals("rawthrill"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "aa":
                            {
                                _Game = new Game_RtAliensArmageddon(_Rom.ToLower());
                            } break;
                        case "jp":
                            {
                                _Game = new Game_RtJurassicPark(_Rom.ToLower());
                            } break;
                        case "ts":
                            {
                                _Game = new Game_RtTerminatorSalvation(_Rom.ToLower());
                            } break;
                        case "ttg":
                            {
                                _Game = new Game_RtTargetTerror(_Rom.ToLower());
                            }; break;
                        case "wd":
                            {
                                _Game = new Game_RtWalkingDead(_Rom.ToLower());
                            } break;
                        default :
                            break;
                    }
                }

                else if (_Target.Equals("ringedge2"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "tsr":
                                {
                                    _Game = new Game_Re2Transformers2(_Rom.ToLower());
                                }; break;
                    }
                }

                //Ring system
                else if (_Target.Equals("ringwide"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "sgg":
                            {
                                _Game = new Game_RwSGG(_Rom.ToLower());
                            } break;
                        case "lgi":
                            {
                                _Game = new Game_RwLGI(_Rom.ToLower());
                            } break;
                        case "lgi3d":
                            {
                                _Game = new Game_RwLGI3D(_Rom.ToLower());
                            } break;
                        case "mng":
                            {
                                _Game = new Game_RwGunman(_Rom.ToLower());
                            }; break; 
                        case "og":
                            {
                                _Game = new Game_RwOpGhost(_Rom.ToLower());
                            } break;
                        case "sdr":
                            {
                                _Game = new Game_RwSDR(_Rom.ToLower());
                            }; break;
                        case "tha":
                            {
                                _Game = new Game_RwTransformers(_Rom.ToLower());
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
                                _Game = new Game_TtxBlockKingBallShooter(_Rom.ToLower());
                            } break;
                        case "eadp":
                            {
                                _Game = new Game_TtxEadp(_Rom.ToLower());
                            } break;
                        case "gattack4":
                            {
                                _Game = new Game_TtxGaiaAttack4(_Rom.ToLower());
                            } break;
                        case "gsoz":
                            {
                                _Game = new Game_TtxGundam_V2(_Rom.ToLower());
                            } break;
                        case "gsoz2p":
                            {
                                _Game = new Game_TtxGundam_V2(_Rom.ToLower());
                            } break;
                        case "hmuseum":
                            {
                                _Game = new Game_TtxHauntedMuseum(_Rom.ToLower());
                            } break;
                        case "hmuseum2":
                            {
                                _Game = new Game_TtxHauntedMuseum2(_Rom.ToLower(), _HardFfl);
                            } break;
                        case "mgungun2":
                            {
                                _Game = new Game_TtxGungun2(_Rom.ToLower());
                            } break;
                        case "sha":
                            {
                                _Game = new Game_TtxSha(_Rom.ToLower());
                            } break;
                        default:
                            break;
                    }
                }

                //Windows Games
                else if (_Target.Equals("windows"))
                {
                    switch (_Rom.ToLower())
                    {
                        case "ads":
                            {
                                _Game = new Game_WndAlienSafari(_Rom.ToLower());
                            } break;
                        case "artdead":
                            {
                                _Game = new Game_WndArtIsDead(_Rom.ToLower());
                            } break;
                        case "coltwws":
                            {
                                _Game = new Game_WndColtWildWestShootout(_Rom.ToLower());
                            } break;
                        case "bugbust":
                            {
                                _Game = new Game_WndBugBusters(_Rom.ToLower());
                            } break;
                        case "friction":
                            {
                                _Game = new Game_WndFriction(_Rom.ToLower());
                            } break;
                        case "hfa":
                            {
                                _Game = new Game_WndHeavyFire3Pc(_Rom.ToLower());
                            }; break;
                        case "hfss":
                            {
                                _Game = new Game_WndHeavyFire4Pc(_Rom.ToLower());
                            }; break;                        
                        case "hod2pc":
                            {
                                _Game = new Game_WndHod2pc(_Rom.ToLower());
                            }; break;
                        case "hod3pc":
                            {
                                _Game = new Game_WndHod3pc(_Rom.ToLower());
                            }; break;
                        case "hodo":
                            {
                                _Game = new Game_WndHotdoPc(_Rom.ToLower());
                            }; break;
                        case "pgbeat":
                            {
                                _Game = new Game_WndProjectGreenBeat(_Rom.ToLower());
                            }; break;
                        case "reload":
                            {
                                _Game = new Game_WndReload(_Rom.ToLower());
                            }; break;
                        default:
                            break;
                    }
                }

                //W.I.P Games
                else if (_Target.Equals("wip"))
                {
                    switch (_Rom.ToLower())
                    {

                        case "adcop":
                            {
                                _Game = new Game_WndAdCop95(_Rom.ToLower());
                            }; break;
                        case "adcopsea":
                            {
                                _Game = new Game_WndAdCopOverseas(_Rom.ToLower());
                            }; break;
                        case "bonbon":
                            {
                                _Game = new Game_WndBonbon95(_Rom.ToLower());
                            }; break;
                        
                        default:
                            break;
                    }
                }

                if (_Game != null)
                    _Game.OnGameHooked += new Game.GameHookedHandler(OnGameHooked);

                //starting the TimeOut Timer
                if (Configurator.GetInstance().HookTimeout != 0)
                {
                    _TimerHookTimeout.Interval = (Configurator.GetInstance().HookTimeout * 1000);
                    _TimerHookTimeout.Start();
                }
            }
        }

        /// <summary>
        /// Check if user has Elevated rights (Admin)
        /// </summary>
        /// <returns></returns>
        private bool IsRunningAsAdmin()
        {
            bool isElevated;
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
                return isElevated;
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("Error checking Admin rights for current user : " + Ex.Message.ToString());
                return false;
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
            if (Configurator.GetInstance().OutputEnabled && Configurator.GetInstance().Wm_OutputEnabled && _Wm_OutputHelper != null)
            {
                if (msg == _Wm_OutputHelper.MameOutput_RegisterClient)
                {
                    _Wm_OutputHelper.RegisterClient(wParam, (UInt32)lParam);
                }
                else if (msg == _Wm_OutputHelper.MameOutput_UnregisterClient)
                {
                    _Wm_OutputHelper.UnregisterClient(wParam, (UInt32)lParam);
                }
                else if (msg == _Wm_OutputHelper.MameOutput_GetIdString)
                {
                    uint Id = (uint)lParam;
                    Logger.WriteLog("MameHooker GetIdString message received for ID=" + Id.ToString());
                    if (Id == 0)
                    {
                        /*if (_Game.ProcessHooked)
                            _Wm_OutputHelper.SendIdString(wParam, _Rom, 0);
                        else
                            _Wm_OutputHelper.SendIdString(wParam, "___empty", 0);*/
                        _Wm_OutputHelper.SendIdString(wParam, _Rom, 0);
                        _Wm_OutputHelper.RomNameSent = true;
                    }
                    else
                    {
                        if (_Game != null && _Game.Outputs.Count > 0)
                        {
                            String s = _Game.GetOutputDescriptionFromId(Id);
                            _Wm_OutputHelper.SendIdString(wParam, s, Id);
                        }
                    }
                }
            }

            switch (msg)
            {
                case Win32Define.WM_INPUT:
                    {
                        ProcessRawInputMessage(lParam);
                    }
                    break;
                case Win32Define.WM_QUIT:
                    {
                        Logger.WriteLog("myWndProc() => WM_QUIT message received !");
                    } break;
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
                    foreach (PlayerSettings Player in Configurator.GetInstance().PlayersSettings)
                    {
                        if (Player.DeviceName == Controller.DeviceName)
                        {
                            if (_Game != null && _Game.ProcessHooked)
                            {
                                Controller.ProcessRawInputData(RawInputHandle);

                                Logger.WriteLog("RawData event for Player #" + Player.ID.ToString() + ":");
                                Logger.WriteLog("Device rawinput data (Hex) = [ " + Player.RIController.Computed_X.ToString("X8") + ", " + Player.RIController.Computed_Y.ToString("X8") + " ]");

                                //Overrriding RAWINPUT data (relative movement) for single mouse by a call to GetCursorPos WIN32 API
                                //That way we can get mouse position as if it's Absolute position
                                if (_UseSingleMouse)
                                {
                                    Logger.WriteLog("Switching to Single Mouse procedure :");
                                    POINT p = new POINT();
                                    if (Win32API.GetCursorPos(out p))
                                    {
                                        Logger.WriteLog("WM_MOUSEMOVE Cursor position = [ " + p.X + "," + p.Y + " ]");
                                        Player.RIController.Computed_X = p.X;
                                        Player.RIController.Computed_Y = p.Y;
                                        Logger.WriteLog("OnScreen Cursor Position (Px) = [ " + Player.RIController.Computed_X + ", " + Player.RIController.Computed_Y + " ]");
                                    }
                                    else
                                    {
                                        Logger.WriteLog("WM_MOUSEMOVE : GetCursorPos() returned error");
                                        return;
                                    }
                                }

                                if (_EnableInputsIpc)
                                    _MMF_Inputs.UpdateRawPlayerData(Player.ID, (UInt32)Player.RIController.Computed_X, (UInt32)Player.RIController.Computed_Y);

                                _Game.GetScreenResolution();
                                Logger.WriteLog("PrimaryScreen Size (Px) = [ " + _Game.ScreenWidth + "x" + _Game.ScreenHeight + " ]");

                                if (!_UseSingleMouse)
                                {
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

                                    if (Configurator.GetInstance().Act_Labs_Offset_Enable)
                                    {
                                        Player.RIController.Computed_X += Player.Act_Labs_Offset_X;
                                        Player.RIController.Computed_Y += Player.Act_Labs_Offset_Y;
                                        Logger.WriteLog("ActLabs adaptated OnScreen Cursor Position (Px) = [ " + Player.RIController.Computed_X + ", " + Player.RIController.Computed_Y + " ]");
                                    }
                                }

                                //Change X asxis scaling based on user requirements
                                if (_ForceScalingX != 1.0)
                                {
                                    Logger.WriteLog("Forcing X Scaling = " + _ForceScalingX.ToString());
                                    double HalfScreenSize = (double)_Game.ScreenWidth / 2.0;
                                    double NewX = (((double)Player.RIController.Computed_X - HalfScreenSize) * _ForceScalingX) + HalfScreenSize;
                                    Player.RIController.Computed_X = Convert.ToInt32(NewX);
                                    Logger.WriteLog("Forced scaled OnScreen Cursor Position (Px) = [ " + Player.RIController.Computed_X + ", " + Player.RIController.Computed_Y + " ]");
                                }

                                _Game.IsFullscreen = _Game.GetFullscreenStatus();
                                if (!_Game.IsFullscreen)
                                {
                                    Logger.WriteLog("ClientWindow Style = Windowed");

                                    //Retrieve info for debug/replay purpose
                                    _Game.GetClientwindowInfo();

                                    if (!_Game.ClientScale(Player))
                                    {
                                        Logger.WriteLog("Error converting screen location to client location");
                                        return;
                                    }
                                    Logger.WriteLog("ClientWindow Location (px) = [ " + _Game.clientWindowLocation.X.ToString() + ", " + _Game.clientWindowLocation.Y.ToString() + " ]");
                                    Logger.WriteLog("ClientWindow Size (px) = [ " + (_Game.WindowRect.Right - _Game.WindowRect.Left).ToString() + "x" + (_Game.WindowRect.Bottom - _Game.WindowRect.Top).ToString() + " ]");
                                    Logger.WriteLog("OnClient Cursor Position (Px) = [ " + Player.RIController.Computed_X + ", " + Player.RIController.Computed_Y + " ]");

                                    if (!_Game.GetClientRect())
                                    {
                                        Logger.WriteLog("Error getting client Rect");
                                        return;
                                    }
                                }
                                else
                                {
                                    Logger.WriteLog("ClientWindow Style = FullScreen");

                                    //No need to translate coordinates from screen -> client and risk error.
                                    //As fuulscreen, we will consider window size = screen size

                                    Rect r = new Rect();
                                    r.Top = 0;
                                    r.Left = 0;
                                    r.Bottom = _Game.ScreenHeight;
                                    r.Right = _Game.ScreenWidth;
                                    _Game.ClientRect = r;
                                }

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

                                if (!_NoInput)
                                    _Game.SendInput(Player);

                                if (_EnableInputsIpc)
                                {
                                    _MMF_Inputs.UpdateComputedPlayerData(Player.ID, Player.RIController.Computed_X, Player.RIController.Computed_Y, Player.RIController.Hid_Buttons);
                                    
                                    if (_MMF_Inputs.WriteData() != 0)
                                        Logger.WriteLog("Succesfully copied P" + Player.ID.ToString() + " data to MMF " + _MMF_Inputs.MemoryFileName);
                                }
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
                    
                    if (Configurator.GetInstance().Wm_OutputEnabled && _Wm_OutputHelper != null && _Wm_OutputHelper.RomNameSent)
                        _Wm_OutputHelper.SendValues(_Game.Outputs);
                    if (Configurator.GetInstance().Net_OutputEnabled && _Net_OutputHelper!= null)
                        _Net_OutputHelper.BroadcastValues(_Game.Outputs);
                }

                DsCore.Win32.Win32API.MM_BeginPeriod(1);
                Thread.Sleep(Configurator.GetInstance().OutputPollingDelay);
                DsCore.Win32.Win32API.MM_EndPeriod(1);
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
                _TrayIcon.Icon = DemulShooter.Properties.Resources.DemulShooter_UnHooked_Icon;
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

        private void OnGameHooked(object sender, EventArgs e)
        {
            _TrayIcon.Icon = DemulShooter.Properties.Resources.DemulShooter_Hooked_Icon;
            _TrayIcon.Text += "[Hooked]";

            //Stopping the Timeout timer
            _TimerHookTimeout.Stop();

            if (_Wm_OutputHelper != null)
            {
                _Game.SetMamePauseState(false);
                _Wm_OutputHelper.SendValues(_Game.Outputs);
            }

            //Sending info to the Net_OutputHelper if existing
            if (_Net_OutputHelper != null)
            {
                _Net_OutputHelper.SetGameHookedState(true);
                _Net_OutputHelper.BroadcatStartMessage();
            }
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            Logger.WriteLog("Cleaning things before exiting application...");
            CleanAppBeforeExit();
        }

        /// <summary>
        /// Application Exit cleanup
        /// </summary>
        private void CleanAppBeforeExit()
        {
            if (_OutputUpdateLoop != null)
                _OutputUpdateLoop.Abort();

            if (_Wm_OutputHelper != null)
            {
                //Simply sending MameStop may cause MameHooker to not release dll properly
                //MAME goes from MameStop to MameStart with PAUSE enabled and "__empty" rom
                //Which is not possible here due to the WndProc quitting before getting MameHooker request ??
                // Solution would be to Send a new MameStart with no values before MAmeStopping again
                _Game.SetMamePauseState(true);
                _Wm_OutputHelper.SendValues(_Game.Outputs);
                _Wm_OutputHelper.Stop();
                _Wm_OutputHelper.Start();
                _Wm_OutputHelper.Stop();
            }

            if (_Net_OutputHelper != null)
            {
                _Net_OutputHelper.Stop();
            }

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
            if (_Game != null && _Game.ProcessHooked && !_NoInput)
            {
                try
                {
                    Logger.WriteLog("KeyboardHook Event : wParam = 0x " + wParam.ToString("X8") + ", lParam = 0x" + lParam.ToString("X8"));
                    //First step : use the Hook to determine if a virtual Middle/Right button has been pushed
                    if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                    {
                        Logger.WriteLog("KeyboardHook Event : WM_KEYDOWN event detected");
                        KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                        Logger.WriteLog("KBDLLHOOKSTRUCT : " + s.ToString());
                        foreach (PlayerSettings Player in Configurator.GetInstance().PlayersSettings)
                        {
                            if (Player.isVirtualMouseButtonsEnabled && Player.RIController != null)
                            {
                                if (s.scanCode == Player.DIK_VirtualMouseButton_Left)
                                {
                                    Logger.WriteLog("Player " + Player.ID + "VirtualMouseButton_Left detected");
                                    Player.RIController.Computed_Buttons = RawInputcontrollerButtonEvent.OnScreenTriggerDown;
                                    _Game.SendInput(Player);
                                }
                                else if (s.scanCode == Player.DIK_VirtualMouseButton_Middle)
                                {
                                    Logger.WriteLog("Player " + Player.ID + "VirtualMouseButton_Middle detected");
                                    Player.RIController.Computed_Buttons = RawInputcontrollerButtonEvent.ActionDown;
                                    _Game.SendInput(Player);
                                }
                                else if (s.scanCode == Player.DIK_VirtualMouseButton_Right)
                                {
                                    Logger.WriteLog("Player " + Player.ID + "VirtualMouseButton_Right detected");
                                    Player.RIController.Computed_Buttons = RawInputcontrollerButtonEvent.OffScreenTriggerDown;
                                    _Game.SendInput(Player);
                                }
                            }
                        }
                        Logger.WriteLog("-");
                    }
                    if ((UInt32)wParam == Win32Define.WM_KEYUP)
                    {
                        Logger.WriteLog("KeyboardHook Event : WM_KEYUP event detected");
                        KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                        Logger.WriteLog("KBDLLHOOKSTRUCT : " + s.ToString());
                        foreach (PlayerSettings Player in Configurator.GetInstance().PlayersSettings)
                        {
                            if (Player.isVirtualMouseButtonsEnabled && Player.RIController != null)
                            {
                                if (s.scanCode == Player.DIK_VirtualMouseButton_Left)
                                {
                                    Logger.WriteLog("Player " + Player.ID + "VirtualMouseButton_Left detected");
                                    Player.RIController.Computed_Buttons = RawInputcontrollerButtonEvent.OnScreenTriggerUp;
                                    _Game.SendInput(Player);
                                }
                                else if (s.scanCode == Player.DIK_VirtualMouseButton_Middle)
                                {
                                    Logger.WriteLog("Player " + Player.ID + "VirtualMouseButton_Middle detected");
                                    Player.RIController.Computed_Buttons = RawInputcontrollerButtonEvent.ActionUp;
                                    _Game.SendInput(Player);
                                }
                                else if (s.scanCode == Player.DIK_VirtualMouseButton_Right)
                                {
                                    Logger.WriteLog("Player " + Player.ID + "VirtualMouseButton_Right detected");
                                    Player.RIController.Computed_Buttons = RawInputcontrollerButtonEvent.OffScreenTriggerUp;
                                    _Game.SendInput(Player);
                                }
                            }
                        }
                        Logger.WriteLog("-");
                    }

                    //Second step : forward the event to the Game
                    return _Game.KeyboardHookCallback(_MouseHookID, nCode, wParam, lParam);
                }
                catch (Exception Ex)
                {
                    Logger.WriteLog("Error handling KeyboardHookCallback : " + Ex.Message.ToString());
                    return _Game.KeyboardHookCallback(_MouseHookID, nCode, wParam, lParam);
                }
            }            
            else
                return Win32API.CallNextHookEx(_KeyboardHookID, nCode, wParam, lParam);
        }
        protected void RemoveKeyboardHook()
        {
            Win32API.UnhookWindowsHookEx(_KeyboardHookID);
        }

        //quit application if TimeOut enabled for game hooking
        private void tHookTimeOut_Elapsed(Object Sender, EventArgs e)
        {
            Logger.WriteLog("Hook timeout expired, exiting application.");
            CleanAppBeforeExit();
            Environment.Exit(0);
        }
    }
}
