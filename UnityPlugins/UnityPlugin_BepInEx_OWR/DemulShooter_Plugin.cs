using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using UnityEngine;
using UnityPlugin_BepInEx_Core;
using System.Collections.Generic;

namespace OperationWolf_BepInEx_DemulShooter_Plugin
{
    /*
    /// Need to change HideManagerGameObject = true in BepInEx.cfg, or Unity is destroying the Plugin after Awake()
    */
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class DemulShooter_Plugin : BaseUnityPlugin
    {
        public const String pluginGuid = "argonlefou.demulshooter.owr";
        public const String pluginName = "OperationWolf_BepInEx_DemulShooter_Plugin";
        public const String pluginVersion = "4.0.0.0";

        public static BepInEx.Logging.ManualLogSource MyLogger;

        public static DemulShooter_Plugin Instance = null;

        public enum PlayerType : int
        {
            Player1 = 0,
            Player2
        }

        //Thread-safe operation on input/output data
        public static System.Object MutexLocker_Outputs;
        public static System.Object MutexLocker_Inputs;

        //Custom Outputs data
        public static byte P1_LastLife = 0;
        public static ushort P1_LastAmmo = 0;
        public static ushort P2_LastAmmo = 0;

        //custom Input Data
        public static List<PluginController> PluginControllers;

        //TCP server data for Inputs/Outputs
        private TcpListener _TcpListener;
        private Thread _TcpListenerThread;
        private TcpClient _TcpClient;
        private int _TcpPort = 33610;
        private static NetworkStream _TcpStream;

        public static bool IsMouseLockedRequired = false;
        public static TcpOutputData OutputData;
        private TcpInputData _InputData;

        public static readonly KeyCode P2_Grenade_KeyCode = KeyCode.F;

        public static byte DisableCrosshair = 0;

        public void Awake()
        {
            Instance = this;

            MutexLocker_Outputs = new System.Object();
            MutexLocker_Inputs = new System.Object();

            MyLogger = Logger;
            MyLogger.LogMessage("Plugin Loaded");
            Harmony harmony = new Harmony(pluginGuid);

            OutputData = new TcpOutputData();
            _InputData = new TcpInputData();

            PluginControllers = new List<PluginController>();
            PluginControllers.Add(new PluginController(1));
            PluginControllers.Add(new PluginController(2));

            // Start TcpServer	
            _TcpListenerThread = new Thread(new ThreadStart(TcpClientThreadLoop));
            _TcpListenerThread.IsBackground = true;
            _TcpListenerThread.Start();

            harmony.PatchAll();
        }

        public void Start()
        {
            MyLogger.LogMessage("OpwOlfPlugin.Start() => Removing mouse cursor");
            Cursor.visible = false;
        }

        public void Update()
        {
        }

        public void OnDestroy()
        {
            Logger.LogMessage("OpwOlfPlugin.OnDestroy() => Closing TCP Server....");
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
                MyLogger.LogMessage("OpWolf_Plugin.TcpClientThreadLoop() => TCP Server is listening on Port " + _TcpPort);

                Byte[] bytes = new Byte[1024];
                while (true)
                {
                    using (_TcpClient = _TcpListener.AcceptTcpClient())
                    {
                        MyLogger.LogMessage("OpWolf_Plugin.TcpClientThreadLoop() => TCP Client connected !");
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
                                    MyLogger.LogMessage("OpWolf_Plugin.TcpClientThreadLoop() => client message received as: " + _InputData.ToString());

                                    lock (MutexLocker_Inputs)
                                    {
                                        PluginControllers[(int)PlayerType.Player1].SetAxis(_InputData.P1_X, _InputData.P1_Y);
                                        PluginControllers[(int)PlayerType.Player1].SetButton(PluginController.PluginButton.Trigger, _InputData.P1_Trigger);
                                        PluginControllers[(int)PlayerType.Player1].SetButton(PluginController.PluginButton.Reload, _InputData.P1_Reload);
                                        PluginControllers[(int)PlayerType.Player1].SetButton(PluginController.PluginButton.Action, _InputData.P1_ChangeWeapon);
                                        PluginControllers[(int)PlayerType.Player2].SetAxis(_InputData.P2_X, _InputData.P2_Y);
                                        PluginControllers[(int)PlayerType.Player2].SetButton(PluginController.PluginButton.Trigger, _InputData.P2_Trigger);
                                        PluginControllers[(int)PlayerType.Player2].SetButton(PluginController.PluginButton.Reload, _InputData.P2_Reload);
                                        PluginControllers[(int)PlayerType.Player2].SetButton(PluginController.PluginButton.Action, _InputData.P2_ChangeWeapon);
                                        DisableCrosshair = _InputData.HideCrosshair;
                                    }
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
                MyLogger.LogError("OpWolf_Plugin.TcpClientThreadLoop() => SocketException " + socketException.ToString());
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
                    //Resetting event flags for next packets
                    MyLogger.LogMessage("OpWolf_Plugin.SendOutputs() => Sending data : " + p.ToString());
                    lock (MutexLocker_Outputs)
                    {
                        OutputData.P1_Recoil = 0;
                        OutputData.P1_Damage = 0;
                        OutputData.P2_Recoil = 0;
                        OutputData.P2_Damage = 0;
                    }
                    _TcpStream.Write(Buffer, 0, Buffer.Length);
                }
            }
            catch (Exception Ex)
            {
                MyLogger.LogError("OpWolf_Plugin.SendOutputs() => Socket exception: " + Ex);
            }
        }
    }
}
