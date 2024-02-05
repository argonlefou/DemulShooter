using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Text;
using UnityEngine;
using UnityPlugin_BepInEx_Core;
using System.Collections.Generic;
using Artoncode.Core;

namespace UnityPlugin_BepInEx_PBX
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class PointBlankX_Plugin : BaseUnityPlugin
    {
        public const String pluginGuid = "argonlefou.demulshooter.pbx";
        public const String pluginName = "PBX DemulShooter Plugin";
        public const String pluginVersion = "1.0.0.0";

        public static BepInEx.Logging.ManualLogSource MyLogger;

        public static PointBlankX_Plugin Instance = null;

        public static readonly string PLAYER_1_NAME = "P1";
        public static readonly string PLAYER_2_NAME = "P2";

        //Custom Outputs data
        public static byte P1_LastLife = 0;
        public static ushort P1_LastAmmo = 0;
        public static ushort P2_LastAmmo = 0;

        //custom Input Data
        public static Vector2 P1_Axis = new Vector2(0,0);
        public static Vector2 P2_Axis = new Vector2(0,0);
        public static byte P1_Trigger_ButtonState = 0;
        public static byte P2_Trigger_ButtonState = 0;

        //TCP server data for Inputs/Outputs
        private TcpListener _TcpListener;
        private Thread _TcpListenerThread;
        private TcpClient _TcpClient;
        private int _TcpPort = 1234;
        private static NetworkStream _TcpStream;

        public static bool IsMouseLockedRequired = false;
        public static TcpOutputData OutputData;
        private TcpOutputData _OutputDataBefore;
        private TcpInputData _InputData;

        public static readonly KeyCode P1_Start_KeyCode = KeyCode.Alpha1;
        public static readonly KeyCode P2_Start_KeyCode = KeyCode.Alpha2;
        public static readonly KeyCode Credits_KeyCode = KeyCode.Alpha5;

        public static bool CrossHairVisibility = true;

        public void Awake()
        {
            Instance = this;

            MyLogger = Logger;
            MyLogger.LogMessage("Plugin Loaded");
            Harmony harmony = new Harmony(pluginGuid);

            OutputData = new TcpOutputData();
            _OutputDataBefore = new TcpOutputData();
            _InputData = new TcpInputData();

            // Start TcpServer	
            _TcpListenerThread = new Thread(new ThreadStart(TcpClientThreadLoop));
            _TcpListenerThread.IsBackground = true;
            _TcpListenerThread.Start();

            harmony.PatchAll();
        }

        public void Start()
        {
            MyLogger.LogMessage("PointBlankX_Plugin.Start() => Removing mouse cursor");
            Cursor.visible = false;
        }

        public void Update()
        {
            Singleton<GlobalData>.shared().isCrosshairVisible = CrossHairVisibility;

            //Updating Credits
            int iCredits = (int)Singleton<GlobalData>.shared().coinPool + (int)Singleton<GlobalData>.shared().serviceCredit;
            OutputData.Credits = (byte)iCredits;

            //Updating Life and Bullets            
            OutputData.P1_Life = 0;
            OutputData.P2_Life = 0;
            OutputData.P1_Ammo = 0;
            OutputData.P1_Ammo = 0;
            Player p = Player.getPlayer(PLAYER_1_NAME);
            if (p != null && p.playerData.state == PlayerData.PlayerState.Active)
            {
                OutputData.P1_Life = (byte)p.getHealth();
                int Ammo = p.getAmmo();
                if (Ammo > 0)
                    OutputData.P1_Ammo = (ushort)Ammo;
            }
            p = Player.getPlayer(PLAYER_2_NAME);
            if (p != null && p.playerData.state == PlayerData.PlayerState.Active)
            {
                OutputData.P2_Life = (byte)p.getHealth();
                int Ammo = p.getAmmo();
                if (Ammo > 0)
                    OutputData.P2_Ammo = (ushort)Ammo;
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


            //Debug Keys
            if (Input.GetKeyDown(KeyCode.K))
            {
                MyLogger.LogWarning("PointBlankX_Plugin.Update() =>  isCrosshairVisible : " + Singleton<GlobalData>.shared().isCrosshairVisible);
                Singleton<GlobalData>.shared().isCrosshairVisible = true;
            }
        }

        public void OnDestroy()
        {
            Logger.LogMessage("PointBlankX_Plugin.OnDestroy() => Closing TCP Server....");
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
                MyLogger.LogMessage("PointBlankX_Plugin.TcpClientThreadLoop() => TCP Server is listening on Port " + _TcpPort);

                Byte[] bytes = new Byte[1024];
                while (true)
                {
                    using (_TcpClient = _TcpListener.AcceptTcpClient())
                    {
                        MyLogger.LogMessage("PointBlankX_Plugin.TcpClientThreadLoop() => TCP Client connected !");
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
                                    MyLogger.LogMessage("PointBlankX_Plugin.TcpClientThreadLoop() => client message received as: " + _InputData.ToString());

                                    //lock (MutexLocker_Inputs)
                                    //{
                                        P1_Axis = new Vector2(_InputData.P1_X, _InputData.P1_Y);
                                        P1_Trigger_ButtonState = _InputData.P1_Trigger;
                                        P2_Axis = new Vector2(_InputData.P2_X, _InputData.P2_Y);
                                        P2_Trigger_ButtonState = _InputData.P2_Trigger;
                                        CrossHairVisibility = _InputData.HideCrosshairs == 1 ? false : true;
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
                MyLogger.LogError("PointBlankX_Plugin.TcpClientThreadLoop() => SocketException " + socketException.ToString());
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
                    MyLogger.LogMessage("PointBlankX_Plugin.SendOutputs() => Sending data : " + p.ToString());
                    //lock (MutexLocker_Outputs)
                    //{
                        OutputData.P1_Recoil = 0;
                        OutputData.P2_Recoil = 0;
                    //}
                    _TcpStream.Write(Buffer, 0, Buffer.Length);
                }
            }
            catch (Exception Ex)
            {
                MyLogger.LogError("PointBlankX_Plugin.SendOutputs() => Socket exception: " + Ex);
            }
        }
    }
}
