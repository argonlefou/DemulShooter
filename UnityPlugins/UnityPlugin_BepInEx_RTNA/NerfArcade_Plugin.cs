using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityPlugin_BepInEx_Core;

namespace UnityPlugin_BepInEx_RTNA
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class NerfArcade_Plugin : BaseUnityPlugin
    {
        public const String pluginGuid = "argonlefou.nerf.arcade";
        public const String pluginName = "UnityPlugin_BepInEx_RTNA";
        public const String pluginVersion = "1.0.0.0";
        public const String pluginConfigFile = "RTNA_BepInEx_DemulShooter_Plugin.ini";

        public static BepInEx.Logging.ManualLogSource MyLogger;

        public static NerfArcade_Plugin Instance = null;

        //custom Input Data
        public static PluginController[] PluginControllers = new PluginController[4];
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

        public static byte CabTemplate = 0;
        
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

            MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): Loading custom config : " + @"./BepInEx/plugins/" + pluginConfigFile);
            INIFile Plugin_IniFile = new INIFile(@"./BepInEx/plugins/" + pluginConfigFile);
            if (File.Exists(Plugin_IniFile.FInfo.FullName))
            {
                try
                {
                    CabTemplate = byte.Parse(Plugin_IniFile.IniReadValue("System", "CAB_TEMPLATE"));
                    MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): CabTemplate set to : " + CabTemplate + " -- " + ((FactorySetUpConsts.TEMPLATE_IDS)CabTemplate).ToString());
                }
                catch (Exception Ex)
                {
                    MyLogger.LogError(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): Error reading config file : " + Plugin_IniFile.FInfo.FullName);
                    MyLogger.LogError(Ex.Message.ToString());
                }
            }
            else
            {
                MyLogger.LogWarning(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): " + Plugin_IniFile.FInfo.FullName + " not found");
            }

            harmony.PatchAll();
        }

        public void Start()
        {
            MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): Removing mouse cursor");
            Cursor.visible = false;
        }

        public void Update()
        {
            //Quit
            if (Input.GetKeyDown(KeyCode.Escape))
                Application.Quit();


            //Get Lamp status as Int16 : 0-100%
            OutputData.P1_Lmp_Start = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.P1Start) * 100.0f);
            OutputData.P1_Lmp_SeatPuck = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.P1SeatPuck) * 100.0f);
            OutputData.P1_Lmp_SeatMarquee = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.P1SeatMarquee) * 100.0f);
            OutputData.P1_Lmp_SeatRear_R = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.P1SeatSpeaker_Red) * 100.0f);
            OutputData.P1_Lmp_SeatRear_O = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.P1SeatSpeaker_Green) * 100.0f);
            OutputData.P1_Lmp_SeatRear_B = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.P1SeatSpeaker_Blue) * 100.0f);
            OutputData.P2_Lmp_Start = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.P2Start) * 100.0f);
            OutputData.P2_Lmp_SeatPuck = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.P2SeatPuck) * 100.0f);
            OutputData.P2_Lmp_SeatMarquee = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.P2SeatMarquee) * 100.0f);
            OutputData.P2_Lmp_SeatRear_R = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.P2SeatSpeaker_Red) * 100.0f);
            OutputData.P2_Lmp_SeatRear_O = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.P2SeatSpeaker_Green) * 100.0f);
            OutputData.P2_Lmp_SeatRear_B = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.P2SeatSpeaker_Blue) * 100.0f);
            OutputData.Cab_Lmp_R = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.SeatCabRearTMolding_Red) * 100.0f);
            OutputData.Cab_Lmp_G = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.SeatCabRearTMolding_Green) * 100.0f);
            OutputData.Cab_Lmp_B = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.SeatCabRearTMolding_Blue) * 100.0f);
            OutputData.Cab_Lmp_RearSeat = (UInt16)(SingletonMonoBehaviour<IOManager>.Instance.GetLastLightValue(CabinetLight.SeatCabDownLighting) * 100.0f);

            OutputData.Credits[0] = (uint)AuditManager.CoinAudits.m_PlayerCoinAudits[0].m_TotalUnspent;
            OutputData.Credits[1] = (uint)AuditManager.CoinAudits.m_PlayerCoinAudits[1].m_TotalUnspent;

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
                                        PluginControllers[i].SetButton(PluginController.MyInputButtons.Trigger, _InputData.Trigger[i]);
                                        PluginControllers[i].SetButton(PluginController.MyInputButtons.Action, _InputData.Action[i]);
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
