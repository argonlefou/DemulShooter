using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using BepInEx;
using HarmonyLib;
using UnityPlugin_BepInEx_Core;
using UnityPlugin_BepInEx_IniFile;
using UnityEngine;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class DemulShooter_Plugin : BaseUnityPlugin
    {
        public const String pluginGuid = "argonlefou.demulshooter.missionimpossible";
        public const String pluginName = "MissionImpossible_BepInEx_DemulShooter_Plugin";
        public const String pluginVersion = "1.0.0.0";
        public const String pluginConfigFile = "MissionImpossible_BepInEx_DemulShooter_Plugin.ini";

        public static BepInEx.Logging.ManualLogSource MyLogger;

        public static DemulShooter_Plugin Instance = null;

        //custom Input Data
        public static PluginController[] PluginControllers = new PluginController[TcpInputData.MAX_PLAYER];
        public static bool EnableInputHack = false;         //By default, no input hack on the plugin. Enabled once DemulShooter is connected (without -noinput flag)
        
        //TCP server data for Inputs/Outputs
        private TcpListener _TcpListener;
        private Thread _TcpListenerThread;
        private TcpClient _TcpClient;
        private int _TcpPort = 33610;
        private static NetworkStream _TcpStream;

        public static TcpOutputData OutputData;
        private TcpOutputData _OutputDataBefore;
        private TcpInputData _InputData;

        public static bool CrossHairVisibility = true;

        //Original size is 1920p Horizontal
        public static readonly float ORIGINAL_WIDTH = 1920.0f;
        public static readonly float ORIGINAL_HEIGHT = 1080.0f;

        //Custom resolution
        public static bool ChangeResolution = false;
        public static int ScreenWidth = 1920;
        public static int ScreenHeight = 1080;
        public static bool Fullscreen = true;

        public void Awake()
        {
            Instance = this;

            MyLogger = Logger;
            MyLogger.LogMessage("Plugin Loaded");
            Harmony harmony = new Harmony(pluginGuid);

            OutputData = new TcpOutputData();
            _OutputDataBefore = new TcpOutputData();
            _InputData = new TcpInputData();

            for (int i = 0; i < 4; i++)
            {
                PluginControllers[i] = new PluginController(i);
            }

            // Start TcpServer	
            _TcpListenerThread = new Thread(new ThreadStart(TcpClientThreadLoop));
            _TcpListenerThread.IsBackground = true;
            _TcpListenerThread.Start();

            MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): Loading custom config : " + BepInEx.Paths.PluginPath + @"/" + pluginConfigFile);
            INIFile Plugin_IniFile = new INIFile(BepInEx.Paths.PluginPath + @"/" + pluginConfigFile);

            if (File.Exists(Plugin_IniFile.FInfo.FullName))
            {
                try
                {
                    if (Plugin_IniFile.IniReadValue("Video", "CHANGE_RES").Equals("1"))
                        ChangeResolution = true;
                    ScreenWidth = Int32.Parse(Plugin_IniFile.IniReadValue("Video", "WIDTH"));
                    ScreenHeight = Int32.Parse(Plugin_IniFile.IniReadValue("Video", "HEIGHT"));
                    if (Plugin_IniFile.IniReadValue("Video", "FULLSCREEN").Equals("0"))
                        Fullscreen = false;
                    else
                        Fullscreen = true;
                }
                catch (Exception Ex)
                {
                    MyLogger.LogError(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): Error reading config file : " + Plugin_IniFile.FInfo.FullName);
                    MyLogger.LogError(Ex.Message.ToString());
                }
            }
            else
            {
                MyLogger.LogWarning(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "():" + Plugin_IniFile.FInfo.FullName + " not found");
            }

            harmony.PatchAll();
        }

        public void Start()
        {}

        public void Update()
        {
            //Using Input.GEtKeyDown + Input.GEtKeyUp cause trouble because the KeyUp is sometimes not detected/executed
            //Buttons then stay pressed and cause issue
            //Recreating our own Up/Down detection :

            //START Buttons
            if (Input.GetKey(KeyCode.Alpha1))
                PluginControllers[0].SetButton(PluginController.MyInputButtons.Start, 1);
            else
                PluginControllers[0].SetButton(PluginController.MyInputButtons.Start, 0);
            if (Input.GetKey(KeyCode.Alpha2))
                PluginControllers[1].SetButton(PluginController.MyInputButtons.Start, 1);
            else
                PluginControllers[1].SetButton(PluginController.MyInputButtons.Start, 0);

            
            if (!EnableInputHack)
            {
                //Player 2 = Mouse + SHIFT
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    if (Input.GetKey(KeyCode.Mouse0))
                        PluginControllers[1].SetButton(PluginController.MyInputButtons.TriggerLeft, 1);
                    else
                        PluginControllers[1].SetButton(PluginController.MyInputButtons.TriggerLeft, 0);
                    if (Input.GetKey(KeyCode.Mouse1))
                        PluginControllers[1].SetButton(PluginController.MyInputButtons.TriggerRight, 1);
                    else
                        PluginControllers[1].SetButton(PluginController.MyInputButtons.TriggerRight, 0);

                    PluginControllers[1].SetAimingValues(Input.mousePosition);
                }
                else
                {
                    if (Input.GetKey(KeyCode.Mouse0))
                        PluginControllers[0].SetButton(PluginController.MyInputButtons.TriggerLeft, 1);
                    else
                        PluginControllers[0].SetButton(PluginController.MyInputButtons.TriggerLeft, 0);
                    if (Input.GetKey(KeyCode.Mouse1))
                        PluginControllers[0].SetButton(PluginController.MyInputButtons.TriggerRight, 1);
                    else
                        PluginControllers[0].SetButton(PluginController.MyInputButtons.TriggerRight, 0);

                    PluginControllers[0].SetAimingValues(Input.mousePosition);
                }                
            }

            //Coin
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                CreditManager.Instance.AddOneCoin();
            }

            //Quit
            if (Input.GetKeyDown(KeyCode.Escape))
                Application.Quit();

            //Retrieving Output data
            if (CreditManager.Instance != null)
                DemulShooter_Plugin.OutputData.Credits = CreditManager.Instance.CurrentCredit;
            if (PlayerManager.Instance != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    Player p = PlayerManager.Instance.GetPlayer(i);
                    if (p != null)
                    {
                        DemulShooter_Plugin.OutputData.Life[i] = (uint)p.Life;
                        if (p.PMode >= PlayerMode.PM_GAME && p.PMode <= PlayerMode.PM_GAME_OVER)
                            DemulShooter_Plugin.OutputData.IsPlaying[i] = 1;
                        else
                            DemulShooter_Plugin.OutputData.IsPlaying[i] = 0;
                    }
                }
            }

            //Checking for a change in output to send or not
            byte[] bOutputData = OutputData.ToByteArray();
            byte[] bOutputDataBefore = _OutputDataBefore.ToByteArray();
            for (int i = 0; i < bOutputData.Length; i++)
            {
                if (bOutputData[i] != bOutputDataBefore[i])
                {
                    SendOutputs();
                    break;
                }
            }

            //Save current state
            _OutputDataBefore.Update(bOutputData);
        }

        public void OnDestroy()
        {
            MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "()");
            _TcpListener.Server.Close();
        }

        private void HarmonyPatch(Harmony hHarmony, Type OriginalClass, String OriginalMethod, Type ReplacementClass, String ReplacementMethod)
        {
            MethodInfo original = AccessTools.Method(OriginalClass, OriginalMethod);
            MethodInfo patch = AccessTools.Method(ReplacementClass, ReplacementMethod);
            hHarmony.Patch(original, new HarmonyMethod(patch));
        }

        /// <summary> 	
        /// Runs in background TcpServerThread; Handles incomming TcpClient requests 	
        /// </summary> 	
        private void TcpClientThreadLoop()
        {
            try
            {
                // Create listener on localhost port 8052. 			
                _TcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), _TcpPort);
                _TcpListener.Start();
                MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): TCP Server is listening on Port " + _TcpPort);

                Byte[] bytes = new Byte[1024];
                while (true)
                {
                    using (_TcpClient = _TcpListener.AcceptTcpClient())
                    {
                        MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): TCP Client connected !");
                        using (_TcpStream = _TcpClient.GetStream())
                        {
                            //Send outputs at connection, if DemulShooter connects during game, between events
                            SendOutputs();
                            while (true)
                            {
                                int Length = 0;
                                try
                                {
                                    Length = _TcpStream.Read(bytes, 0, bytes.Length);
                                    //If Tcpclient gets disconnected, Read should return 0 bytes, so we can handle disconnection to allow a new connection
                                    if (Length == 0)
                                        break;
                                    byte[] InputBuffer = new byte[Length];
                                    Array.Copy(bytes, 0, InputBuffer, 0, Length);
                                    _InputData.Update(InputBuffer);
                                    //- Debug ONLY
                                    //MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): client message received as: " + _InputData.ToString());
                                    //- Debug ONLY

                                    //lock (MutexLocker_Inputs)
                                    //{
                                    for (int i = 0; i < TcpInputData.MAX_PLAYER; i++)
                                    {
                                        PluginControllers[i].SetAimingValues(new Vector3(_InputData.Axis_X[i], _InputData.Axis_Y[i]));
                                        PluginControllers[i].SetButton(PluginController.MyInputButtons.TriggerLeft, _InputData.Trigger[i]);
                                        PluginControllers[i].SetButton(PluginController.MyInputButtons.TriggerRight, _InputData.Action[i]);
                                    }
                                    CrossHairVisibility = _InputData.HideCrosshairs == 1 ? false : true;
                                    EnableInputHack = _InputData.EnableInputsHack == 1 ? true : false;
                                    //}
                                }
                                catch
                                {
                                    //Connnection Error ?
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (SocketException socketException)
            {
                MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): SocketException " + socketException.ToString());
            }
        }

        /// <summary>
        /// Send output data over the TCP connection
        /// When TcpClient is disconnected, _TcpClient is Disposed and can't be acces to check if null or not => need Try/Catch
        /// </summary>
        public static void SendOutputs()
        {
            try
            {
                if (Instance._TcpClient == null)
                    return;

                if (_TcpStream == null)
                    return;

                // Get a stream object for writing. 			
                if (_TcpStream.CanWrite)
                {
                    TcpPacket p = new TcpPacket(OutputData.ToByteArray(), TcpPacket.PacketHeader.Outputs);
                    byte[] Buffer = p.GetFullPacket();
                    //- Debug ONLY
                    //MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "():  Sending data : " + p.ToString());
                    //- Debug ONLY
                    //lock (MutexLocker_Outputs)
                    //{
                    //Resetting event flags for next packets                    
                    for (int i = 0; i < TcpOutputData.MAX_PLAYER; i++)
                    {
                        OutputData.Recoil[i] = 0;
                        OutputData.Damaged[i] = 0;
                    }
                    //}
                    _TcpStream.Write(Buffer, 0, Buffer.Length);
                }
            }
            catch (Exception Ex)
            {
                MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): Socket exception: " + Ex);
            }
        }
    }
}
