using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using BepInEx;
using HarmonyLib;
using MegaPixel.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityPlugin_BepInEx_Core;
using UnityPlugin_BepInEx_IniFile;

namespace BepInEx_DemulShooter_Plugin
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class DemulShooter_Plugin : BaseUnityPlugin
    {
        public const String pluginGuid = "argonlefou.demulshooter.hotd2remake";
        public const String pluginName = "Hotd2Remake_BepInEx_DemulShooter_Plugin";
        public const String pluginVersion = "17.0.0.0";
        public const String pluginConfigFile = "Hotd2Remake_BepInEx_DemulShooter_Plugin.ini";

        public static BepInEx.Logging.ManualLogSource MyLogger;

        public static DemulShooter_Plugin Instance = null;

        public static readonly int MAX_PLAYERS = 2;

        //custom Input Data
        public static PluginController[] PluginControllers = new PluginController[MAX_PLAYERS];
        public static bool EnableInputHack = false;         //By default, no input hack on the plugin. Enabled once DemulShooter is connected (without -noinput flag)

        //Using custom Button to get Up/Down events from the keyboard state
        //No EXIT key by default : Escape is used by the game for its pause menu
        public static PluginControllerButton Exit_Key = new PluginControllerButton((int)KeyCode.None);

        //Game's own "Control Panel" setting, giving unlimited continues to both players
        public static bool UnlimitedContinues = false;

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
        private static CanvasGroup[] _HiddenCrosshairs = new CanvasGroup[MAX_PLAYERS];

        //Ready check panel singleton is private in the game
        private static FieldInfo _ReadyCheckPanelInstance = AccessTools.Field(typeof(CR_ReadyCheckPanel), "instance");
        private static bool[] _ReadyPanelTriggerBefore = new bool[MAX_PLAYERS];

        public void Awake()
        {
            Instance = this;

            MyLogger = Logger;
            MyLogger.LogMessage("Plugin Loaded");
            MyLogger.LogMessage("Detected Application Version : " + Application.version);
            Harmony harmony = new Harmony(pluginGuid);

            OutputData = new TcpOutputData(MAX_PLAYERS);
            _OutputDataBefore = new TcpOutputData(MAX_PLAYERS);
            _InputData = new TcpInputData(MAX_PLAYERS);

            for (int i = 0; i < MAX_PLAYERS; i++)
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
                    for (int i = 0; i < MAX_PLAYERS; i++)
                    {
                        PluginControllers[i].InputButtons[(int)PluginController.MyInputButtons.Start].SetKeyCode(Plugin_IniFile.IniReadValue("INPUT_KEYS", "P" + (i + 1).ToString() + "_START"));
                    }

                    Exit_Key.SetKeyCode(Plugin_IniFile.IniReadValue("INPUT_KEYS", "EXIT"));

                    UnlimitedContinues = Plugin_IniFile.IniReadValue("GAME", "UNLIMITED_CONTINUES").Trim().Equals("1");
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

            if (UnlimitedContinues)
            {
                MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): Enabling unlimited continues");
                for (int i = 0; i < MAX_PLAYERS; i++)
                {
                    CR_Data.UNLIMITED_CREDITS_ENABLED_FROM_CONTROL_PANEL[i] = true;
                }
            }

            harmony.PatchAll();
        }

        public void Start()
        {}

        public void Update()
        {
            //Custom Button handling
            Exit_Key.SetButton(GetKey(Exit_Key.KeyCode));
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                PluginControllers[i].SetButton(PluginController.MyInputButtons.Start, GetKey(PluginControllers[i].InputButtons[(int)PluginController.MyInputButtons.Start].KeyCode) ? (byte)1 : (byte)0);
            }

            //Quit
            if (Exit_Key.GetButtonDown())
                UnityEngine.Application.Quit();

            //With DemulShooter in charge of the guns, START buttons are used to join, continue and validate the "Ready" screen
            if (EnableInputHack)
            {
                for (int i = 0; i < MAX_PLAYERS; i++)
                {
                    bool bStart = PluginControllers[i].GetButtonDown(PluginController.MyInputButtons.Start);
                    if (bStart)
                        HandleStartButton((PlayerType)i);

                    //Trigger ButtonDown event is used by CR_Player, so using our own edge detection here
                    bool bTrigger = PluginControllers[i].GetButton(PluginController.MyInputButtons.Trigger);
                    bool bTriggerPressed = bTrigger && !_ReadyPanelTriggerBefore[i];
                    _ReadyPanelTriggerBefore[i] = bTrigger;

                    if (CR_ReadyCheckPanel.IsActive && (bStart || bTriggerPressed))
                        SetPlayerReady((PlayerType)i);
                }
            }

            //Custom option to hide Crosshairs
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                SetCrosshairVisibility((PlayerType)i, CrossHairVisibility);
            }

            UpdatePlayersStatus();

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

        /// <summary>
        /// START button :
        /// - P2 can join a multiplayer campaign at any time, the game only does that natively when a 2nd controller is plugged in
        /// - A dead player uses a continue (or buys one with his score, if possible)
        /// </summary>
        private static void HandleStartButton(PlayerType Player)
        {
            if (!CR_LevelLoader.IsGameplayChapterLoaded || !MP_SaveDataHandler.IsReady || MP_SaveDataHandler.LastPlayedCampaign == null)
                return;

            CR_Player GamePlayer = CR_Player.Players[(int)Player];
            if (GamePlayer == null)
                return;

            //Same as the game, no joining or continuing while in Menu, Pause or Cutscene
            InputMapsFlag ActiveMaps = MP_InputManager.ActiveInputMaps;
            if ((ActiveMaps & (InputMapsFlag.Gameplay | InputMapsFlag.ReadyPanel | InputMapsFlag.DeathScreen)) == 0)
                return;

            if (!GamePlayer.IsActivated)
            {
                if (!MP_SaveDataHandler.LastPlayedCampaign.IsMultiplayerCampaign || MP_GameManager.IsGameplayFinished || MP_GameManager.IsLevelCompleted || MP_ControllerDisconnectedPopup.IsVisible)
                    return;

                //Same as the game : a player out of the game after his last continue can't join again,
                //and nobody can join while every player is dead (the game is stopped, waiting for a continue)
                if (GamePlayer.HasDied || CR_Player.GetAlivePlayerCount() == 0)
                    return;

                MyLogger.LogMessage("DemulShooter_Plugin.HandleStartButton() => " + Player.ToString() + " joining the game");
                if (Player == PlayerType.Player2)
                    CR_GameManager.SetMultiplayerActive(true);
                else
                    GamePlayer.Activate();
                CR_ReadyCheckPanel.TryRefreshingMultiplayerStatus();

                //Clearing button events received while not playing, so that nothing is fired on the first frame
                PluginControllers[(int)Player].GetButtonDown(PluginController.MyInputButtons.Trigger);
                PluginControllers[(int)Player].GetButtonDown(PluginController.MyInputButtons.Reload);
                PluginControllers[(int)Player].GetButtonDown(PluginController.MyInputButtons.Action);
                mCR_Player.IsHoldingFire[(int)Player] = false;

                Instance.StartCoroutine(ShowAmmoAfterJoin(GamePlayer));
            }

            if (GamePlayer.IsActivated && GamePlayer.HasDied && (MP_InputManager.ActiveInputMaps & InputMapsFlag.DeathScreen) != 0)
            {
                if (!GamePlayer.CanBeRevived() && GamePlayer.CanBuyToken())
                    GamePlayer.BuyContinue();
                if (GamePlayer.CanBeRevived())
                {
                    MyLogger.LogMessage("DemulShooter_Plugin.HandleStartButton() => " + Player.ToString() + " continue");
                    GamePlayer.Revive();
                }
            }
        }

        /// <summary>
        /// Ready screen at the beginning of a chapter
        /// A player without a controller assigned is already set ready by the game
        /// </summary>
        private static void SetPlayerReady(PlayerType Player)
        {
            CR_ReadyCheckPanel Panel = _ReadyCheckPanelInstance.GetValue(null) as CR_ReadyCheckPanel;
            if (Panel != null)
                Panel.SetPlayerReady(Player);
        }

        /// <summary>
        /// The game has no option to hide the crosshair. Disabling the GameObject is not possible, as it is used by the game for shooting raycasts
        /// So we're just using a CanvasGroup to make the whole crosshair transparent
        /// </summary>
        private static void SetCrosshairVisibility(PlayerType Player, bool IsVisible)
        {
            if (IsVisible)
            {
                //Only restoring what we have hidden ourself
                if (_HiddenCrosshairs[(int)Player] != null)
                    _HiddenCrosshairs[(int)Player].alpha = 1.0f;
                _HiddenCrosshairs[(int)Player] = null;
                return;
            }

            CR_Crosshair Crosshair = CR_Crosshair.GetCrosshair(Player);
            if (Crosshair == null)
                return;

            CanvasGroup CrosshairCanvasGroup = Crosshair.GetComponent<CanvasGroup>();
            if (CrosshairCanvasGroup == null)
            {
                CrosshairCanvasGroup = Crosshair.gameObject.AddComponent<CanvasGroup>();
                CrosshairCanvasGroup.interactable = false;
                CrosshairCanvasGroup.blocksRaycasts = false;
            }
            CrosshairCanvasGroup.alpha = 0.0f;
            _HiddenCrosshairs[(int)Player] = CrosshairCanvasGroup;
        }

        /// <summary>
        /// Life and Ammo outputs, read from the game every frame
        /// </summary>
        private static void UpdatePlayersStatus()
        {
            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                OutputData.IsPlaying[i] = 0;
                OutputData.Life[i] = 0;
                OutputData.Ammo[i] = 0;

                try
                {
                    CR_Player GamePlayer = CR_Player.Players[i];
                    if (GamePlayer != null && GamePlayer.IsActiveAndAlive && GamePlayer.Health != null)
                    {
                        OutputData.IsPlaying[i] = 1;
                        //Each life torch displayed in game is 2 HP
                        OutputData.Life[i] = (byte)Mathf.CeilToInt(GamePlayer.Health.CurrentHP / 2.0f);
                        if (GamePlayer.WeaponHolder != null && GamePlayer.WeaponHolder.CurrentWeapon != null)
                            OutputData.Ammo[i] = GamePlayer.WeaponHolder.CurrentWeapon.Ammo;
                    }
                }
                catch (Exception Ex)
                {
                    MyLogger.LogWarning("DemulShooter_Plugin.UpdatePlayersStatus() => Error reading P" + (i + 1).ToString() + " status : " + Ex.Message.ToString());
                }
            }
        }

        /// <summary>
        /// The game is only using the InputSystem package : reading the legacy UnityEngine.Input class is throwing an exception
        /// Keys are still set as legacy KeyCode values in the config file, like for every other DemulShooter plugin
        /// </summary>
        public static bool GetKey(int iKeyCode)
        {
            Key k = KeyCodeToKey((KeyCode)iKeyCode);
            if (k == Key.None || Keyboard.current == null)
                return false;
            return Keyboard.current[k].isPressed;
        }

        private static Key KeyCodeToKey(KeyCode k)
        {
            if (k >= KeyCode.A && k <= KeyCode.Z)
                return Key.A + (k - KeyCode.A);
            if (k >= KeyCode.Alpha1 && k <= KeyCode.Alpha9)
                return Key.Digit1 + (k - KeyCode.Alpha1);
            if (k >= KeyCode.Keypad0 && k <= KeyCode.Keypad9)
                return Key.Numpad0 + (k - KeyCode.Keypad0);
            if (k >= KeyCode.F1 && k <= KeyCode.F12)
                return Key.F1 + (k - KeyCode.F1);

            switch (k)
            {
                case KeyCode.Alpha0: return Key.Digit0;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.Return: return Key.Enter;
                case KeyCode.Space: return Key.Space;
                case KeyCode.Tab: return Key.Tab;
                case KeyCode.Backspace: return Key.Backspace;
                case KeyCode.UpArrow: return Key.UpArrow;
                case KeyCode.DownArrow: return Key.DownArrow;
                case KeyCode.LeftArrow: return Key.LeftArrow;
                case KeyCode.RightArrow: return Key.RightArrow;
                case KeyCode.Insert: return Key.Insert;
                case KeyCode.Delete: return Key.Delete;
                case KeyCode.Home: return Key.Home;
                case KeyCode.End: return Key.End;
                case KeyCode.PageUp: return Key.PageUp;
                case KeyCode.PageDown: return Key.PageDown;
                case KeyCode.LeftShift: return Key.LeftShift;
                case KeyCode.RightShift: return Key.RightShift;
                case KeyCode.LeftControl: return Key.LeftCtrl;
                case KeyCode.RightControl: return Key.RightCtrl;
                case KeyCode.LeftAlt: return Key.LeftAlt;
                case KeyCode.RightAlt: return Key.RightAlt;
                case KeyCode.Minus: return Key.Minus;
                case KeyCode.Equals: return Key.Equals;
                case KeyCode.Comma: return Key.Comma;
                case KeyCode.Period: return Key.Period;
                case KeyCode.Slash: return Key.Slash;
                case KeyCode.Backslash: return Key.Backslash;
                case KeyCode.Semicolon: return Key.Semicolon;
                case KeyCode.Quote: return Key.Quote;
                case KeyCode.BackQuote: return Key.Backquote;
                case KeyCode.LeftBracket: return Key.LeftBracket;
                case KeyCode.RightBracket: return Key.RightBracket;
                case KeyCode.KeypadEnter: return Key.NumpadEnter;
                case KeyCode.KeypadPlus: return Key.NumpadPlus;
                case KeyCode.KeypadMinus: return Key.NumpadMinus;
                case KeyCode.KeypadMultiply: return Key.NumpadMultiply;
                case KeyCode.KeypadDivide: return Key.NumpadDivide;
                case KeyCode.KeypadPeriod: return Key.NumpadPeriod;
                default: return Key.None;
            }
        }

        /// <summary>
        /// Game bug : the bullets display is hiding itself when it is started for the first time (CR_AmmoController.Start()),
        /// and only shown again at the end of the next cutscene. A player joining during a chapter had no bullets displayed until then
        /// </summary>
        private static IEnumerator ShowAmmoAfterJoin(CR_Player GamePlayer)
        {
            //Start() is called once the bullets display is active, at the latest on the next frame
            yield return null;
            yield return null;
            if (GamePlayer != null && GamePlayer.IsActivated && GamePlayer.WeaponHolder != null && !CR_Cutscene.IsCutsceneActive)
                GamePlayer.WeaponHolder.ToggleAmmoController(true);
        }

        /// <summary>
        /// The game is only using its own Gameplay input map for players actions, we're checking it before forcing any action from DemulShooter data
        /// </summary>
        public static bool IsGameplayInputActive()
        {
            return (MP_InputManager.ActiveInputMaps & InputMapsFlag.Gameplay) != 0;
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

                                    for (int i = 0; i < MAX_PLAYERS; i++)
                                    {
                                        PluginControllers[i].SetAimingValues(new Vector3(_InputData.Axis_X[i], _InputData.Axis_Y[i]));
                                        PluginControllers[i].SetButton(PluginController.MyInputButtons.Trigger, _InputData.Trigger[i]);
                                        PluginControllers[i].SetButton(PluginController.MyInputButtons.Reload, _InputData.Reload[i]);
                                        PluginControllers[i].SetButton(PluginController.MyInputButtons.Action, _InputData.ChangeWeapon[i]);
                                    }
                                    CrossHairVisibility = _InputData.HideCrosshairs == 1 ? false : true;
                                    EnableInputHack = _InputData.EnableInputsHack == 1 ? true : false;
                                }
                                catch
                                {
                                    //Connnection Error ?
                                    break;
                                }
                            }
                        }
                    }
                    //DemulShooter is gone, giving back the controls to the game
                    EnableInputHack = false;
                    CrossHairVisibility = true;
                    MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): TCP Client disconnected !");
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

                    //Resetting event flags for next packets
                    for (int i = 0; i < MAX_PLAYERS; i++)
                    {
                        OutputData.Recoil[i] = 0;
                        OutputData.Damaged[i] = 0;
                    }
                    _TcpStream.Write(Buffer, 0, Buffer.Length);
                }
            }
            catch (Exception Ex)
            {
                MyLogger.LogMessage(Instance.GetType().Name + "." + MethodBase.GetCurrentMethod().Name + "(): Socket exception: " + Ex);
            }
        }

        /// <summary>
        /// For deebug, printing StackTrace to trace calls to API we're looking for
        /// </summary>
        public static void PrintStackTrace()
        {
            MyLogger.LogMessage(System.Environment.StackTrace);
        }
    }
}
