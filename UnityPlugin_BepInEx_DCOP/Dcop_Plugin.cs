using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityPlugin_BepInEx_Core;

namespace UnityPlugin_BepInEx_DCOP
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Dcop_Plugin : BaseUnityPlugin
    {
        public const String pluginGuid = "argonlefou.demulshooter.dcop";
        public const String pluginName = "DCOP DemulShooter Plugin";
        public const String pluginVersion = "1.0.0.0";

        public static BepInEx.Logging.ManualLogSource MyLogger;

        public static Dcop_Plugin Instance = null;

        //TCP server data for Inputs/Outputs
        private TcpListener _TcpListener;
        private Thread _TcpListenerThread;
        private TcpClient _TcpClient;
        private int _TcpPort = 33610;
        private static NetworkStream _TcpStream;

        public static TcpOutputData OutputData;
        private TcpOutputData _OutputDataBefore;

        //Game is setting Arduino PINS to set outputs :
        // Pin 02 => Game Gun
        // Pin 03 => Direct Hit (YELLOW LIGHT)
        // Pin 08 => Police Light Bar
        // Pin 16 => Red Light (CONTINUE / GAME OVER)
        // Pin 19 => Game gun (YELLOW FLASH)
        public enum ArduinoPin
        {
            PlayerGun_Solenoid = 2,
            DirectHit_Light = 3,
            Police_LightBar = 8,
            DirectHit_Light2 = 12,
            ArduinoReady_Light = 13,
            EnemyGun_Solenoid = 15,
            RedLightContinue = 16,
            EnemyGun2_Solenoid = 17,
            WhiteStrobe_Flasher = 18,
            GameGun_Light = 19
        }

        public void Awake()
        {
            Instance = this;

            MyLogger = Logger;
            MyLogger.LogMessage("Plugin Loaded");
            Harmony harmony = new Harmony(pluginGuid);

            OutputData = new TcpOutputData();
            _OutputDataBefore = new TcpOutputData();

            // Start TcpServer	
            _TcpListenerThread = new Thread(new ThreadStart(TcpClientThreadLoop));
            _TcpListenerThread.IsBackground = true;
            _TcpListenerThread.Start();

            harmony.PatchAll();
        }

        public void Start()
        {
        }

        public void Update()
        {
            //Singleton<GlobalData>.shared().isCrosshairVisible = CrossHairVisibility;            

            

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
            Logger.LogMessage("Dcop_Plugin.OnDestroy() => Closing TCP Server....");
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
                MyLogger.LogMessage("Dcop_Plugin.TcpClientThreadLoop() => TCP Server is listening on Port " + _TcpPort);

                Byte[] bytes = new Byte[1024];
                while (true)
                {
                    using (_TcpClient = _TcpListener.AcceptTcpClient())
                    {
                        MyLogger.LogMessage("Dcop_Plugin.TcpClientThreadLoop() => TCP Client connected !");
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
                                    //_InputData.Update(InputBuffer);
                                    //MyLogger.LogMessage("PointBlankX_Plugin.TcpClientThreadLoop() => client message received as: " + _InputData.ToString());

                                    ////lock (MutexLocker_Inputs)
                                    ////{
                                    //P1_Axis = new Vector2(_InputData.P1_X, _InputData.P1_Y);
                                    //P1_Trigger_ButtonState = _InputData.P1_Trigger;
                                    //P2_Axis = new Vector2(_InputData.P2_X, _InputData.P2_Y);
                                    //P2_Trigger_ButtonState = _InputData.P2_Trigger;
                                    //CrossHairVisibility = _InputData.HideCrosshairs == 1 ? false : true;
                                    ////}
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
                MyLogger.LogError("Dcop_Plugin.TcpClientThreadLoop() => SocketException " + socketException.ToString());
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
                    MyLogger.LogMessage("Dcop_Plugin.SendOutputs() => Sending data : " + p.ToString());
                    //lock (MutexLocker_Outputs)
                    //{
                    OutputData.GunLight = 0;
                    //}
                    _TcpStream.Write(Buffer, 0, Buffer.Length);
                }
            }
            catch (Exception Ex)
            {
                MyLogger.LogError("Dcop_Plugin.SendOutputs() => Socket exception: " + Ex);
            }
        }
    }
}
