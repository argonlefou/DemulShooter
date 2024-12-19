using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.IO;
using System.Timers;

namespace UnityPlugin_BepInEx_NHA2
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class NightHunterArcade2_Plugin : BaseUnityPlugin
    {
        public const String pluginGuid = "argonlefou.input.nha2";
        public const String pluginName = "NHA Input Plugi2n";
        public const String pluginVersion = "3.0.0.0";

        static String MAPPED_FILE_NAME = "DemulShooter_MMF_Nha2";
        static String MUTEX_NAME = "DemulShooter_Mutex_Nha2";
        static long MAPPED_FILE_CAPACITY = 2048;
        public static NHA2_MemoryMappedFile_Controller NHA2_Mmf;

        private int[] _LifebarArray;
        public static DemulShooter_PlayerButtonState[] DemulShooter_Buttons;
        public static bool[] Players_RecoilEnabled;

        private static readonly string SPRITE_1P_BLUE_FILE = "BepInEx\\plugins\\Assets\\image_1P_Blue.png";
        private static readonly string SPRITE_1P_RED_FILE = "BepInEx\\plugins\\Assets\\image_1P_Red.png";
        private static readonly string SPRITE_2P_BLUE_FILE = "BepInEx\\plugins\\Assets\\image_2P_Blue.png";
        private static readonly string SPRITE_2P_RED_FILE = "BepInEx\\plugins\\Assets\\image_2P_Red.png";
        public static Sprite Sprite_1P_Blue;
        public static Sprite Sprite_1P_Red;
        public static Sprite Sprite_2P_Blue; 
        public static Sprite Sprite_2P_Red;

        public static BepInEx.Logging.ManualLogSource MyLogger;

        public static ConfManager Configurator;

        public enum InputMode
        {
            Mouse = 0,
            DemulShooter
        }

        public void Awake()
        {
            MyLogger = base.Logger;
            MyLogger.LogMessage("Plugin Loaded");

            Configurator = new ConfManager();

            DemulShooter_Buttons = new DemulShooter_PlayerButtonState[] { new DemulShooter_PlayerButtonState(), new DemulShooter_PlayerButtonState() };
            Players_RecoilEnabled = new bool[]{ false, false };

            if (Configurator.InputMode == InputMode.DemulShooter)
            {
                NHA2_Mmf = new NHA2_MemoryMappedFile_Controller(MAPPED_FILE_NAME, MUTEX_NAME, MAPPED_FILE_CAPACITY);
                int r = NHA2_Mmf.MMFOpen();
                if (r == 0)
                {
                    MyLogger.LogMessage("DemulShooter MMF opened succesfully");
                    r = NHA2_Mmf.ReadAll();
                    if (r != 0)
                        MyLogger.LogError("DemulShooter MMF initial read error : " + r.ToString());
                    else
                        MyLogger.LogMessage("DemulShooter MMF initial read success)");
                }
                else
                {
                    MyLogger.LogError("DemulShooter MMF open error : " + r.ToString());
                }

                _LifebarArray = new int[2];
            }

            Texture2D texture = LoadTextureFromFile(SPRITE_1P_BLUE_FILE);
            Sprite_1P_Blue = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            texture = LoadTextureFromFile(SPRITE_1P_RED_FILE);
            Sprite_1P_Red = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            texture = LoadTextureFromFile(SPRITE_2P_BLUE_FILE);
            Sprite_2P_Blue = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            texture = LoadTextureFromFile(SPRITE_2P_RED_FILE);
            Sprite_2P_Red = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);

            Harmony harmony = new Harmony(pluginGuid);

            harmony.PatchAll();
        }

        public static void ButtonTimerElaped(object sender, EventArgs e)
        {

        }

        // Update() called every Frame, FixedUpdate() called every 0.02sec.
        // Loop function to get data from the Game and pass them to Demulshooter:
        // - Life
        // - Credits
        // - Recoil
        public void Update()
        {
            //Quit
            if (Input.GetKeyDown(KeyCode.Escape))
                Application.Quit();

            if (Configurator.InputMode == InputMode.DemulShooter)
            {
                int r = NHA2_Mmf.ReadAll();
                if (r != 0)
                    MyLogger.LogError("DemulShooter_Plugin.Update() => DemulShooter MMF read error : " + r.ToString());

                //Inputs
                for (int i = 0; i < 2; i++)
                {
                    DemulShooter_Buttons[i].UpdateTrigger(NHA2_Mmf.Payload[NHA2_MemoryMappedFile_Controller.INDEX_P1_TRIGGER + i * 4]);
                    DemulShooter_Buttons[i].UpdateChangeWeapon(NHA2_Mmf.Payload[NHA2_MemoryMappedFile_Controller.INDEX_P1_WEAPON + i * 4]);
                    DemulShooter_Buttons[i].UpdateSpecial(NHA2_Mmf.Payload[NHA2_MemoryMappedFile_Controller.INDEX_P1_SPECIAL + i * 4]);

                    //Resetting button state
                    NHA2_Mmf.Payload[NHA2_MemoryMappedFile_Controller.INDEX_P1_TRIGGER + i * 4] = 0;
                    NHA2_Mmf.Payload[NHA2_MemoryMappedFile_Controller.INDEX_P1_WEAPON + i * 4] = 0;
                    NHA2_Mmf.Payload[NHA2_MemoryMappedFile_Controller.INDEX_P1_SPECIAL + i * 4] = 0;

                    if (DemulShooter_Buttons[i].IsChangeWeaponPressed())
                        input_obj_change_bullet.change_weapon(i + 1); // PlayerNum is 1 or 2

                    if (DemulShooter_Buttons[i].IsSpecialPressed())
                        input_obj_big_power.big_power_work(i + 1); // PlayerNum is 1 or 2
                    

                    //_LifebarArray[i] = game_run_core.my_static_game_run.mygame_players.get_game_player(i + 1).get_curr_blood();
                    //Array.Copy(BitConverter.GetBytes(_LifebarArray[i]), 0, NHA2_Mmf.Payload, NHA2_MemoryMappedFile_Controller.INDEX_P1_LIFE + i * 4, 4);
                    NHA2_Mmf.Payload[NHA2_MemoryMappedFile_Controller.INDEX_P1_LIFE + i * 4] = (byte)game_run_core.my_static_game_run.mygame_players.get_game_player(i + 1).get_curr_blood();

                    if (Players_RecoilEnabled[i])
                    {
                        NHA2_Mmf.Payload[NHA2_MemoryMappedFile_Controller.INDEX_P1_MOTOR + i * 4] = 1;
                        Players_RecoilEnabled[i] = false;
                    }
                }

                //Array.Copy(BitConverter.GetBytes(zhichi_hanshu_houtai.get_no_use_coins()), 0, NHA2_Mmf.Payload, NHA2_MemoryMappedFile_Controller.INDEX_CREDITS, 4);
                NHA2_Mmf.Payload[NHA2_MemoryMappedFile_Controller.INDEX_CREDITS] = (byte)zhichi_hanshu_houtai.get_no_use_coins();
                
                NHA2_Mmf.Writeall();
            }
        }

        public void OnDestroy()
        {
            Logger.LogMessage("Closing Demulshooter MMF....");
            if (NHA2_Mmf.IsOpened)
                NHA2_Mmf.MMFClose();
        }

        private void HarmonyPatch(Harmony hHarmony, Type OriginalClass, String OriginalMethod, Type ReplacementClass, String ReplacementMethod)
        {
            MethodInfo original = AccessTools.Method(OriginalClass, OriginalMethod);
            MethodInfo patch = AccessTools.Method(ReplacementClass, ReplacementMethod);
            hHarmony.Patch(original, new HarmonyMethod(patch));
        }

        private static Texture2D LoadTextureFromFile(string FilePath)
        {
            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(FilePath))
            {
                fileData = File.ReadAllBytes(FilePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
                MyLogger.LogMessage(string.Concat(new object[]
				{
					"Texture created from ",
					FilePath,
					" :  width = ",
					tex.width,
					", height =  ",
					tex.height,
					",",
					tex.dimension.ToString()
				}));
            }
            else
            {
                MyLogger.LogError("TokiPlugin.Awake() => File not found : " + FilePath);
            }
            return tex;
        }
			

        #region DemulShooter Buttons handling

        public class DemulShooter_PlayerButtonState
        {
            private DemulShooter_ButtonState _Button_Fire;
            private DemulShooter_ButtonState _Button_ChangeWeapon;
            private DemulShooter_ButtonState _Button_Special;

            public DemulShooter_PlayerButtonState()
            {
                _Button_Fire = new DemulShooter_ButtonState(false);
                _Button_ChangeWeapon = new DemulShooter_ButtonState(true);
                _Button_Special = new DemulShooter_ButtonState(false);
            }

            //Set
            public void UpdateTrigger(int NewValue)
            {
                _Button_Fire.UpdateButton(NewValue);
            }
            public void UpdateChangeWeapon(int NewValue)
            {
                _Button_ChangeWeapon.UpdateButton(NewValue);
            }
            public void UpdateSpecial(int NewValue)
            {
                _Button_Special.UpdateButton(NewValue);
            }

            //Get
            public bool IsTriggerDown()
            {
                return _Button_Fire.IsButtonDown();
            }
            public bool IsChangeWeaponDown()
            {
                return _Button_ChangeWeapon.IsButtonDown();
            }
            public bool IsSpecialDown()
            {
                return _Button_Special.IsButtonDown();
            }

            public bool IsTriggerPressed()
            {
                return _Button_Fire.IsButtonDown();
            }
            public bool IsChangeWeaponPressed()
            {
                return _Button_ChangeWeapon.IsButtonPressed();
            }
            public bool IsSpecialPressed()
            {
                return _Button_Special.IsButtonPressed();
            }
            
        }

        private class DemulShooter_ButtonState
        {
            private bool _ButtonDown;
            private bool _ButtonPressed;
            //Used to prevent the game from registering many times the same buttons in a row if not released
            private int _LastButtonValue = 0;

            private Timer _TmrButton;
            private bool _IsLocked = false;


            public DemulShooter_ButtonState(bool IsTimerLocked)
            {
                _ButtonDown = false;
                _ButtonPressed = false;
                if (IsTimerLocked)
                {
                    _TmrButton = new Timer();
                    _TmrButton.Interval = 100.0;
                    _TmrButton.Elapsed += TmrButton_Elapsed;
                }
            }

            public void UpdateButton(int NewButtonValue)
            {
                if (NewButtonValue != _LastButtonValue)
                {
                    if (NewButtonValue == 2)
                    {
                        _ButtonPressed = false;
                        _ButtonDown = false;
                        _LastButtonValue = NewButtonValue;
                    }
                    else if (NewButtonValue == 1)
                    {
                        if (_TmrButton != null)
                        {
                            if (!_IsLocked)
                            {
                                _ButtonPressed = true;
                                _ButtonDown = true;
                                _LastButtonValue = NewButtonValue;
                                _IsLocked = true;
                                _TmrButton.Start();
                            }
                        }
                        else
                        {
                            _ButtonPressed = true;
                            _ButtonDown = true;
                            _LastButtonValue = NewButtonValue;
                        }
                        
                    }
                }
                
            }

            //Return maintained button status
            public bool IsButtonDown()
            {
                return _ButtonDown;
            }

            //Return Pressed event, and remove the sattus so that it's false again after
            public bool IsButtonPressed()
            {
                bool result = _ButtonPressed;
                _ButtonPressed = false;
                return result;
            }

            private void TmrButton_Elapsed(object sender, EventArgs e)
            {
                MyLogger.LogWarning("Timer Stop");
                _IsLocked = false;
                _TmrButton.Stop();
            }
        }

        #endregion
    }
}
