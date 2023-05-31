using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DsCore;
using DsCore.Win32;

namespace DsCore.Config
{
    public class Configurator
    {
        //Maximum number of player allowed for DemulShooter to handle
        public const int MAX_PLAYERS = 4;

        //P1-P4 settings
        private PlayerSettings[] _PlayersSettings;
        public PlayerSettings[] PlayersSettings
        {
            get { return _PlayersSettings; }
        }

        //Timeout setting for Hooking procedure
        private int _HookTimeout = 0;
        public int HookTimeout
        { get { return _HookTimeout; } }

        //ActLbs offset
        private bool _Act_Labs_Offset_Enable = false;
        private bool _Act_Labs_Display_Crosshair = true;
        #region Accessors
        public bool Act_Labs_Offset_Enable
        {
            get { return _Act_Labs_Offset_Enable; }
            set { _Act_Labs_Offset_Enable = value; }
        }
        public bool Act_Labs_Display_Crosshair
        {
            get { return _Act_Labs_Display_Crosshair; }
            set { _Act_Labs_Display_Crosshair = value; }
        }
        #endregion

        //Silent Hill Data
        private const string SHA_CONF_FILEPATH = @"\bemani_config\sha_v01.cfg";
        private HardwareScanCode _DIK_Sha_Exit;
        private HardwareScanCode _DIK_Sha_Test;
        private HardwareScanCode _DIK_Sha_Service;
        private HardwareScanCode _DIK_Sha_P1_Start;
        private HardwareScanCode _DIK_Sha_P1_Trigger;
        private HardwareScanCode _DIK_Sha_P2_Start;
        private HardwareScanCode _DIK_Sha_P2_Trigger;
        #region Accessors
        public HardwareScanCode DIK_Sha_Exit
        {
            get { return _DIK_Sha_Exit; }
            set { _DIK_Sha_Exit = value; }
        }
        public HardwareScanCode DIK_Sha_Test
        {
            get { return _DIK_Sha_Test; }
            set { _DIK_Sha_Test = value; }
        }
        public HardwareScanCode DIK_Sha_Service
        {
            get { return _DIK_Sha_Service; }
            set { _DIK_Sha_Service = value; }
        }
        public HardwareScanCode DIK_Sha_P1_Start
        {
            get { return _DIK_Sha_P1_Start; }
            set { _DIK_Sha_P1_Start = value; }
        }
        public HardwareScanCode DIK_Sha_P1_Trigger
        {
            get { return _DIK_Sha_P1_Trigger; }
            set { _DIK_Sha_P1_Trigger = value; }
        }
        public HardwareScanCode DIK_Sha_P2_Start
        {
            get { return _DIK_Sha_P2_Start; }
            set { _DIK_Sha_P2_Start = value; }
        }
        public HardwareScanCode DIK_Sha_P2_Trigger
        {
            get { return _DIK_Sha_P2_Trigger; }
            set { _DIK_Sha_P2_Trigger = value; }
        }
        #endregion

        //Model 2 Data
        private HardwareScanCode _DIK_M2_Crosshair_P1 = HardwareScanCode.DIK_7;
        private HardwareScanCode _DIK_M2_Crosshair_P2 = HardwareScanCode.DIK_8;
        private HardwareScanCode _DIK_M2_Crosshair_Visibility = HardwareScanCode.DIK_9;
        #region Accessors
        public HardwareScanCode DIK_M2_Crosshair_P1
        {
            get { return _DIK_M2_Crosshair_P1; }
            set { _DIK_M2_Crosshair_P1 = value; }
        }
        public HardwareScanCode DIK_M2_Crosshair_P2
        {
            get { return _DIK_M2_Crosshair_P2; }
            set { _DIK_M2_Crosshair_P2 = value; }
        }
        public HardwareScanCode DIK_M2_Crosshair_Visibility
        {
            get { return _DIK_M2_Crosshair_Visibility; }
            set { _DIK_M2_Crosshair_Visibility = value; }
        }
        #endregion

        //Gundam Data
        private HardwareScanCode _DIK_Gsoz_Pedal_P1 = HardwareScanCode.DIK_G;
        private HardwareScanCode _DIK_Gsoz_Pedal_P2 = HardwareScanCode.DIK_H;
        private bool _Gsoz_Pedal_P1_Enabled = false;
        private bool _Gsoz_Pedal_P2_Enabled = false;
        #region Accessors
        public bool Gsoz_Pedal_P1_Enabled
        {
            get { return _Gsoz_Pedal_P1_Enabled; }
            set { _Gsoz_Pedal_P1_Enabled = value; }
        }
        public bool Gsoz_Pedal_P2_Enabled
        {
            get { return _Gsoz_Pedal_P2_Enabled; }
            set { _Gsoz_Pedal_P2_Enabled = value; }
        }
        public HardwareScanCode DIK_Gsoz_Pedal_P1
        {
            get { return _DIK_Gsoz_Pedal_P1; }
            set { _DIK_Gsoz_Pedal_P1 = value; }
        }
        public HardwareScanCode DIK_Gsoz_Pedal_P2
        {
            get { return _DIK_Gsoz_Pedal_P2; }
            set { _DIK_Gsoz_Pedal_P2 = value; }
        }
        #endregion

        //Dolphin Data
        private HardwareScanCode _DIK_Dolphin_P2_LClick = HardwareScanCode.DIK_S;
        private HardwareScanCode _DIK_Dolphin_P2_MClick = HardwareScanCode.DIK_D;
        private HardwareScanCode _DIK_Dolphin_P2_RClick = HardwareScanCode.DIK_F;
        #region Accessors
        public HardwareScanCode DIK_Dolphin_P2_LClick
        {
            get { return _DIK_Dolphin_P2_LClick; }
            set { _DIK_Dolphin_P2_LClick = value; }
        }
        public HardwareScanCode DIK_Dolphin_P2_MClick
        {
            get { return _DIK_Dolphin_P2_MClick; }
            set { _DIK_Dolphin_P2_MClick = value; }
        }
        public HardwareScanCode DIK_Dolphin_P2_RClick
        {
            get { return _DIK_Dolphin_P2_RClick; }
            set { _DIK_Dolphin_P2_RClick = value; }
        }
        #endregion

        //HeavyFire games need to get Path and cover sensibility settings
        private String _HF3_Path = string.Empty;
        private int _HF3_CoverSensibility = 3;
        private String _HF4_Path = string.Empty;
        private int _HF4_CoverSensibility = 3;
        #region Accessors
        public String HF3_Path
        {
            get { return _HF3_Path; }
            set { _HF3_Path = value; }
        }
        public int HF3_CoverSensibility
        {
            get { return _HF3_CoverSensibility; }
            set { _HF3_CoverSensibility = value; }
        }
        public String HF4_Path
        {
            get { return _HF4_Path; }
            set { _HF4_Path = value; }
        }
        public int HF4_CoverSensibility
        {
            get { return _HF4_CoverSensibility; }
            set { _HF4_CoverSensibility = value; }
        }
        #endregion


        //Gundam Data
        private HardwareScanCode _DIK_Le3_Pedal_P1 = HardwareScanCode.DIK_G;
        private HardwareScanCode _DIK_Le3_Pedal_P2 = HardwareScanCode.DIK_H;
        private bool _Le3_Pedal_P1_Enabled = false;
        private bool _Le3_Pedal_P2_Enabled = false;
        #region Accessors
        public bool Le3_Pedal_P1_Enabled
        {
            get { return _Le3_Pedal_P1_Enabled; }
            set { _Le3_Pedal_P1_Enabled = value; }
        }
        public bool Le3_Pedal_P2_Enabled
        {
            get { return _Le3_Pedal_P2_Enabled; }
            set { _Le3_Pedal_P2_Enabled = value; }
        }
        public HardwareScanCode DIK_Le3_Pedal_P1
        {
            get { return _DIK_Le3_Pedal_P1; }
            set { _DIK_Le3_Pedal_P1 = value; }
        }
        public HardwareScanCode DIK_Le3_Pedal_P2
        {
            get { return _DIK_Le3_Pedal_P2; }
            set { _DIK_Le3_Pedal_P2 = value; }
        }
        #endregion

        //Wild West Shoutout Path and Keys
        private String _Wws_Path = string.Empty;
        private HardwareScanCode _DIK_Wws_Test = HardwareScanCode.DIK_0;
        private HardwareScanCode _DIK_Wws_P1Coin = HardwareScanCode.DIK_5;
        private HardwareScanCode _DIK_Wws_P2Coin = HardwareScanCode.DIK_6;
        #region Accessors
        public String Wws_Path
        {
            get { return _Wws_Path; }
            set { _Wws_Path = value; }
        }
        public HardwareScanCode DIK_Wws_Test
        {
            get { return _DIK_Wws_Test; }
            set { _DIK_Wws_Test = value; }
        }
        public HardwareScanCode DIK_Wws_P1Coin
        {
            get { return _DIK_Wws_P1Coin; }
            set { _DIK_Wws_P1Coin = value; }
        }
        public HardwareScanCode DIK_Wws_P2Coin
        {
            get { return _DIK_Wws_P2Coin; }
            set { _DIK_Wws_P2Coin = value; }
        }
        #endregion

        //Rabbids Hollywood Arcade Path
        private String _Rha_Path = string.Empty;
        #region Accessors
        public String Rha_Path
        {
            get { return _Rha_Path; }
            set { _Rha_Path = value; }
        }
        #endregion

        //RPCS3 Settings (only for System 357)
        private HardwareScanCode _DIK_Rpcs3_P1_Start = HardwareScanCode.DIK_1;
        private HardwareScanCode _DIK_Rpcs3_P2_Start = HardwareScanCode.DIK_2;
        private HardwareScanCode _DIK_Rpcs3_Service = HardwareScanCode.DIK_0;
        private HardwareScanCode _DIK_Rpcs3_Up = HardwareScanCode.DIK_NUMPAD8;    //Scancodes for Up and Down arrows are the one of Numpad Arrows (bug in my side ??)
        private HardwareScanCode _DIK_Rpcs3_Down = HardwareScanCode.DIK_NUMPAD2;
        private HardwareScanCode _DIK_Rpcs3_Enter = HardwareScanCode.DIK_8;
        private HardwareScanCode _DIK_Rpcs3_3D_Switch = HardwareScanCode.DIK_SPACE;     //Only for Dark Escape 4D
        #region Accessors
        public HardwareScanCode DIK_Rpcs3_P1_Start
        {
            get { return _DIK_Rpcs3_P1_Start; }
            set { _DIK_Rpcs3_P1_Start = value; }
        }
        public HardwareScanCode DIK_Rpcs3_P2_Start
        {
            get { return _DIK_Rpcs3_P2_Start; }
            set { _DIK_Rpcs3_P2_Start = value; }
        }
        public HardwareScanCode DIK_Rpcs3_Service
        {
            get { return _DIK_Rpcs3_Service; }
            set { _DIK_Rpcs3_Service = value; }
        }
        public HardwareScanCode DIK_Rpcs3_Up
        {
            get { return _DIK_Rpcs3_Up; }
            set { _DIK_Rpcs3_Up = value; }
        }
        public HardwareScanCode DIK_Rpcs3_Down
        {
            get { return _DIK_Rpcs3_Down; }
            set { _DIK_Rpcs3_Down = value; }
        }
        public HardwareScanCode DIK_Rpcs3_Enter
        {
            get { return _DIK_Rpcs3_Enter; }
            set { _DIK_Rpcs3_Enter = value; }
        }
        public HardwareScanCode DIK_Rpcs3_3D_Switch
        {
            get { return _DIK_Rpcs3_3D_Switch; }
            set { _DIK_Rpcs3_3D_Switch = value; }
        }
        #endregion

        //Specific setting for Operation Ghost CREDITS (the game is not using the E2PROM file at launch)
        //And Game Test options are available in gs2.ini config file
        private bool _OpGHost_EnableFreeplay = false;
        private int _OpGhost_CreditsToStart = 2;
        private int _OpGhost_CreditsToContinue = 1;
        private int _OpGhost_CoinsByCredits = 2;
        //Other options
        private bool _OpGhost_SeparateButtons = false;
        private HardwareScanCode _DIK_OpGhost_Action_P1 = HardwareScanCode.DIK_G;
        private HardwareScanCode _DIK_OpGhost_Action_P2 = HardwareScanCode.DIK_H;
        #region Accessors
        public bool OpGhost_EnableFreeplay
        {
            get { return _OpGHost_EnableFreeplay; }
            set { _OpGHost_EnableFreeplay = value; }
        }
        public int OpGhost_CreditsToStart
        {
            get { return _OpGhost_CreditsToStart; }
            set { _OpGhost_CreditsToStart = value; }
        }
        public int OpGhost_CreditsToContinue
        {
            get { return _OpGhost_CreditsToContinue; }
            set { _OpGhost_CreditsToContinue = value; }
        }
        public int OpGhost_CoinsByCredits
        {
            get { return _OpGhost_CoinsByCredits; }
            set { _OpGhost_CoinsByCredits = value; }
        }
        public bool OpGhost_SeparateButtons
        {
            get { return _OpGhost_SeparateButtons; }
            set { _OpGhost_SeparateButtons = value; }
        }
        public HardwareScanCode DIK_OpGhost_Action_P1
        {
            get { return _DIK_OpGhost_Action_P1; }
            set { _DIK_OpGhost_Action_P1 = value; }
        }
        public HardwareScanCode DIK_OpGhost_Action_P2
        {
            get { return _DIK_OpGhost_Action_P2; }
            set { _DIK_OpGhost_Action_P2 = value; }
        }

        #endregion

        //Outputs settings
        private bool _OutputEnabled = false;
        private int _OutputPollingDelay = 1;
        private int _OutputCustomDamagedDelay = 200;
        private int _OutputCustomRecoilOnDelay = 10;
        private int _OutputCustomRecoilOffDelay = 30;
        #region Accessors
        public bool OutputEnabled
        { 
            get { return _OutputEnabled; } 
            set {_OutputEnabled = value;}
        }
        public int OutputPollingDelay
        {
            get { return _OutputPollingDelay; }
            set { _OutputPollingDelay = value; }
        }
        public int OutputCustomDamagedDelay
        {
            get { return _OutputCustomDamagedDelay; }
            set { _OutputCustomDamagedDelay = value; }
        }
        public int OutputCustomRecoilOnDelay
        {
            get { return _OutputCustomRecoilOnDelay; }
            set { _OutputCustomRecoilOnDelay = value; }
        }
        public int OutputCustomRecoilOffDelay
        {
            get { return _OutputCustomRecoilOffDelay; }
            set { _OutputCustomRecoilOffDelay = value; }
        }
        #endregion

        /// <summary>
        /// This class is used to acces/modify all players/games settings
        /// </summary>        
        public Configurator()
        {
            _PlayersSettings = new PlayerSettings[MAX_PLAYERS];
            for (int i = 0; i < _PlayersSettings.Length; i++)
            {
                _PlayersSettings[i] = new PlayerSettings(i + 1);
            }
        }

        /// <summary>
        /// Return a PlayerSettings class corresponding to the wanted player ID
        /// </summary>
        /// <param name="RequestedPlayerID">Wanted Player ID (from 1 to 4)</param>
        /// <returns></returns>
        public PlayerSettings GetPlayerSettings(int RequestedPlayerID)
        {
            foreach (PlayerSettings s in _PlayersSettings)
            {
                if (s.ID == RequestedPlayerID)
                    return s;
            }
            return null;
        }


        /// <summary>
        /// Main application config file
        /// </summary>        
        /// <param name="ConfigFilePath">Path to DemulShooter config file</param>
        public void ReadDsConfig(String ConfigFilePath)
        {
            try
            {
                using (StreamReader sr = new StreamReader(ConfigFilePath))
                {
                    String line = sr.ReadLine();
                    String[] buffer;
                    while (line != null)
                    {
                        if (!line.StartsWith(";"))
                        {
                            buffer = line.Split('=');
                            if (buffer.Length == 2)
                            {
                                String StrKey = buffer[0].Trim();
                                String StrValue = buffer[1].Trim();
                                
                                // There will never be more than 9 players (even more than 4) so we can assume that 
                                // removing only first 2 char is enough without verification
                                if (StrKey.ToLower().StartsWith("p1"))
                                {
                                    if (!GetPlayerSettings(1).ParseIniParameter(StrKey.ToLower().Substring(2), StrValue))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().StartsWith("p2"))
                                {
                                    if (!GetPlayerSettings(2).ParseIniParameter(StrKey.ToLower().Substring(2), StrValue))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().StartsWith("p3"))
                                {
                                    if (!GetPlayerSettings(3).ParseIniParameter(StrKey.ToLower().Substring(2), StrValue))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().StartsWith("p4"))
                                {
                                    if (!GetPlayerSettings(4).ParseIniParameter(StrKey.ToLower().Substring(2), StrValue))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("act_labs_offset_enable"))
                                {
                                    if (!bool.TryParse(StrValue, out _Act_Labs_Offset_Enable))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("act_labs_display_crosshair"))
                                {
                                    if (!bool.TryParse(StrValue, out _Act_Labs_Display_Crosshair))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("m2_p1_ch"))
                                {
                                    try
                                    {
                                        _DIK_M2_Crosshair_P1 = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("m2_p2_ch"))
                                {
                                    try
                                    {
                                        _DIK_M2_Crosshair_P2 = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("m2_ch_vis"))
                                {
                                    try
                                    {
                                        _DIK_M2_Crosshair_Visibility = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("gsoz_p1_pedal_enable"))
                                {
                                    if (!bool.TryParse(StrValue, out _Gsoz_Pedal_P1_Enabled))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("gsoz_p2_pedal_enable"))
                                {
                                    if (!bool.TryParse(StrValue, out _Gsoz_Pedal_P2_Enabled))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("gsoz_p1_pedal_key"))
                                {
                                    try
                                    {
                                        _DIK_Gsoz_Pedal_P1 = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("gsoz_p2_pedal_key"))
                                {
                                    try
                                    {
                                        _DIK_Gsoz_Pedal_P2 = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("dolphin_p2_lclick"))
                                {
                                    try
                                    {
                                        _DIK_Dolphin_P2_LClick = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("dolphin_p2_mclick"))
                                {
                                    try
                                    {
                                        _DIK_Dolphin_P2_MClick = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("dolphin_p2_rclick"))
                                {
                                    try
                                    {
                                        _DIK_Dolphin_P2_RClick = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                } 
                                else if (StrKey.ToLower().Equals("hf3_path"))
                                {
                                    _HF3_Path = StrValue;
                                }
                                else if (StrKey.ToLower().Equals("hf3_coversensibility"))
                                {
                                    if (!int.TryParse(StrValue, out _HF3_CoverSensibility))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("hf4_path"))
                                {
                                    _HF4_Path = StrValue;
                                }
                                else if (StrKey.ToLower().Equals("hf4_coversensibility"))
                                {
                                    if (!int.TryParse(StrValue, out _HF4_CoverSensibility))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }

                                else if (StrKey.ToLower().Equals("wws_path"))
                                {
                                    _Wws_Path = StrValue;
                                }
                                else if (StrKey.ToLower().Equals("wws_p1_coin_key"))
                                {
                                    try
                                    {
                                        _DIK_Wws_P1Coin = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("wws_p2_coin_key"))
                                {
                                    try
                                    {
                                        _DIK_Wws_P2Coin = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("wws_p1_test_key"))
                                {
                                    try
                                    {
                                        _DIK_Wws_Test = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }

                                else if (StrKey.ToLower().Equals("rha_path"))
                                {
                                    _Rha_Path = StrValue;
                                }

                                else if (StrKey.ToLower().Equals("opghost_enablefreeplay"))
                                {
                                    if (!bool.TryParse(StrValue, out _OpGHost_EnableFreeplay))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("opghost_creditstostart"))
                                {
                                    if (!int.TryParse(StrValue, out _OpGhost_CreditsToStart))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("opghost_creditstocontinue"))
                                {
                                    if (!int.TryParse(StrValue, out _OpGhost_CreditsToContinue))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("opghost_coinsbycredits"))
                                {
                                    if (!int.TryParse(StrValue, out _OpGhost_CoinsByCredits))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("opghost_separatebuttons"))
                                {
                                    if (!bool.TryParse(StrValue, out _OpGhost_SeparateButtons))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("opghost_p1_action_key"))
                                {
                                    try
                                    {
                                        _DIK_OpGhost_Action_P1 = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("opghost_p2_action_key"))
                                {
                                    try
                                    {
                                        _DIK_OpGhost_Action_P2 = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("rpcs3_p1_start"))
                                {
                                    try
                                    {
                                        _DIK_Rpcs3_P1_Start = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("rpcs3_p2_start"))
                                {
                                    try
                                    {
                                        _DIK_Rpcs3_P2_Start = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("rpcs3_service"))
                                {
                                    try
                                    {
                                        _DIK_Rpcs3_Service = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("rpcs3_up"))
                                {
                                    try
                                    {
                                        _DIK_Rpcs3_Up = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("rpcs3_down"))
                                {
                                    try
                                    {
                                        _DIK_Rpcs3_Down = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("rpcs3_enter"))
                                {
                                    try
                                    {
                                        _DIK_Rpcs3_Enter = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("rpcs3_3d"))
                                {
                                    try
                                    {
                                        _DIK_Rpcs3_3D_Switch = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("le3_p1_pedal_enable"))
                                {
                                    if (!bool.TryParse(StrValue, out _Le3_Pedal_P1_Enabled))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("le3_p2_pedal_enable"))
                                {
                                    if (!bool.TryParse(StrValue, out _Le3_Pedal_P2_Enabled))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("le3_p1_pedal_key"))
                                {
                                    try
                                    {
                                        _DIK_Le3_Pedal_P1 = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("le3_p2_pedal_key"))
                                {
                                    try
                                    {
                                        _DIK_Le3_Pedal_P2 = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                                    }
                                    catch { Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid"); }
                                }
                                else if (StrKey.ToLower().Equals("outputenabled"))
                                {
                                    if (!bool.TryParse(StrValue, out _OutputEnabled))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("outputpollingdelay"))
                                {
                                    if (!int.TryParse(StrValue, out _OutputPollingDelay))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("outputcustomdamageddelay"))
                                {
                                    if (!int.TryParse(StrValue, out _OutputCustomDamagedDelay))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("outputcustomrecoilondelay"))
                                {
                                    if (!int.TryParse(StrValue, out _OutputCustomRecoilOnDelay))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("outputcustomrecoiloffdelay"))
                                {
                                    if (!int.TryParse(StrValue, out _OutputCustomRecoilOffDelay))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.ToLower().Equals("hooktimeout"))
                                {
                                    if (!int.TryParse(StrValue, out _HookTimeout))
                                        Logger.WriteLog("Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                            }
                        }
                        line = sr.ReadLine();
                    }
                    sr.Close();
                    Logger.WriteLog("Configuration file succesfuly loaded");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog("Error reading " + ConfigFilePath + " : " + ex.Message);
            }
        }

        /// <summary>
        /// Read Silent Hill the Arcade Key mapping
        /// </summary>
        public void Read_Sha_Conf()
        {
            String appData = Environment.GetEnvironmentVariable("appdata").ToString(); ;
            if (File.Exists(appData + SHA_CONF_FILEPATH))
            {
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
                                _DIK_Sha_Test = (HardwareScanCode)fileBytes[j];
                            } break;
                        case 0x02:
                            {
                                _DIK_Sha_Service = (HardwareScanCode)fileBytes[j];
                            } break;
                        case 0x10:
                            {
                                _DIK_Sha_P1_Start = (HardwareScanCode)fileBytes[j];
                            } break;
                        case 0x11:
                            {
                                _DIK_Sha_P1_Trigger = (HardwareScanCode)fileBytes[j];
                            } break;
                        case 0x20:
                            {
                                _DIK_Sha_P2_Start = (HardwareScanCode)fileBytes[j];
                            } break;
                        case 0x21:
                            {
                                _DIK_Sha_P2_Trigger = (HardwareScanCode)fileBytes[j];
                            } break;
                        case 0xFF:
                            {
                                _DIK_Sha_Exit = (HardwareScanCode)fileBytes[j];
                            } break;
                    }
                }
            }
            else
            {
                //MessageBox.Show("Silent Hill the Arcade : " + appData + @"\bemani_config\sha_v01.cfg not found", "DemulShooter Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Write Conf file
        /// </summary>
        public bool WriteConf(String ConfigFilePath)
        {
            try
            {
                using (StreamWriter sr = new StreamWriter(ConfigFilePath, false))
                {
                    foreach (PlayerSettings PlayerData in _PlayersSettings)
                    {
                        sr.WriteLine(";Player" + PlayerData.ID + " Device configuration");
                        sr.WriteLine("P" + PlayerData.ID + "Mode = " + PlayerData.Mode);
                        sr.WriteLine("P" + PlayerData.ID + "DeviceName = " + PlayerData.DeviceName);
                        if (PlayerData.RIController != null)
                        {
                            if (PlayerData.RIController.DeviceType == RawInput.RawInputDeviceType.RIM_TYPEHID)
                            {
                                sr.WriteLine("P" + PlayerData.ID + "HidAxisX = 0x" + PlayerData.RIController.Selected_AxisX.ToString("x2"));
                                sr.WriteLine("P" + PlayerData.ID + "InvertAxisX = " + PlayerData.InvertAxis_X);                                
                                sr.WriteLine("P" + PlayerData.ID + "HidAxisY = 0x" + PlayerData.RIController.Selected_AxisY.ToString("x2"));
                                sr.WriteLine("P" + PlayerData.ID + "InvertAxisY = " + PlayerData.InvertAxis_Y); 
                                sr.WriteLine("P" + PlayerData.ID + "HidBtnOnscreenTrigger = " + PlayerData.RIController.Selected_OnScreenTriggerButton.ToString());
                                sr.WriteLine("P" + PlayerData.ID + "HidBtnAction = " + PlayerData.RIController.Selected_ActionButton.ToString());
                                sr.WriteLine("P" + PlayerData.ID + "HidBtnOffscreenTrigger = " + PlayerData.RIController.Selected_OffScreenTriggerButton.ToString());
                            }
                        }
                        sr.WriteLine("");
                    }

                    sr.WriteLine(";VirtualGun Button keys for users who don't have more than a trigger with Aimtrak");
                    foreach (PlayerSettings PlayerData in _PlayersSettings)
                    {
                        sr.WriteLine("P" + PlayerData.ID + "VirtualMouseButtons_Enable = " + PlayerData.isVirtualMouseButtonsEnabled.ToString());
                        sr.WriteLine("P" + PlayerData.ID + "VirtualMouseButtonLeft_Key = " + PlayerData.DIK_VirtualMouseButton_Left.ToString());
                        sr.WriteLine("P" + PlayerData.ID + "VirtualMouseButtonMiddle_Key = " + PlayerData.DIK_VirtualMouseButton_Middle.ToString());
                        sr.WriteLine("P" + PlayerData.ID + "VirtualMouseButtonRight_Key = " + PlayerData.DIK_VirtualMouseButton_Right.ToString());
                    }
                    sr.WriteLine("");
                    sr.WriteLine(";Model2 emulator keyboard keys to change in-game crosshairs");
                    sr.WriteLine("M2_P1_CH = " + _DIK_M2_Crosshair_P1.ToString());
                    sr.WriteLine("M2_P2_CH = " + _DIK_M2_Crosshair_P2.ToString());
                    sr.WriteLine("M2_CH_VIS = " + _DIK_M2_Crosshair_Visibility.ToString());
                    sr.WriteLine("");
                    sr.WriteLine(";Enable Pedal-Mode for TTX Gundam Zeon, and set Keys");
                    sr.WriteLine("GSOZ_P1_PEDAL_ENABLE = " + _Gsoz_Pedal_P1_Enabled.ToString());
                    sr.WriteLine("GSOZ_P1_PEDAL_KEY = " + _DIK_Gsoz_Pedal_P1.ToString());
                    sr.WriteLine("GSOZ_P2_PEDAL_ENABLE = " + _Gsoz_Pedal_P2_Enabled.ToString());
                    sr.WriteLine("GSOZ_P2_PEDAL_KEY = " + _DIK_Gsoz_Pedal_P2.ToString());
                    sr.WriteLine("");
                    sr.WriteLine(";Dolphin Keyboard keys configuration for P2 buttons");
                    sr.WriteLine("DOLPHIN_P2_LCLICK = " + _DIK_Dolphin_P2_LClick.ToString());
                    sr.WriteLine("DOLPHIN_P2_MCLICK = " + _DIK_Dolphin_P2_MClick.ToString());
                    sr.WriteLine("DOLPHIN_P2_RCLICK = " + _DIK_Dolphin_P2_RClick.ToString());
                    sr.WriteLine("");
                    sr.WriteLine(";Offset for devices lacking calibration (Act Labs gun, etc...)");
                    sr.WriteLine("Act_Labs_Offset_Enable = " + _Act_Labs_Offset_Enable.ToString());
                    sr.WriteLine("Act_Labs_Display_Crosshair = " + _Act_Labs_Display_Crosshair.ToString());
                    foreach (PlayerSettings PlayerData in _PlayersSettings)
                    {
                        sr.WriteLine("P" + PlayerData.ID + "Act_Labs_Offset_X = " + PlayerData.Act_Labs_Offset_X.ToString());
                        sr.WriteLine("P" + PlayerData.ID + "Act_Labs_Offset_Y = " + PlayerData.Act_Labs_Offset_Y.ToString());
                    }
                    sr.WriteLine("");
                    sr.WriteLine(";Manual calibration for Analog devices");       
                    foreach (PlayerSettings PlayerData in _PlayersSettings)
                    {
                        sr.WriteLine("P" + PlayerData.ID + "Analog_Calibration_Override = " + PlayerData.AnalogAxisRangeOverride.ToString());
                        sr.WriteLine("P" + PlayerData.ID + "Analog_Manual_Xmin = " + PlayerData.AnalogManual_Xmin.ToString());
                        sr.WriteLine("P" + PlayerData.ID + "Analog_Manual_Xmax = " + PlayerData.AnalogManual_Xmax.ToString());
                        sr.WriteLine("P" + PlayerData.ID + "Analog_Manual_Ymin = " + PlayerData.AnalogManual_Ymin.ToString());
                        sr.WriteLine("P" + PlayerData.ID + "Analog_Manual_Ymax = " + PlayerData.AnalogManual_Ymax.ToString());
                    }
                    sr.WriteLine("");                    
                    sr.WriteLine(";Heavy Fire Afghanistan settings");
                    sr.WriteLine("HF3_Path = " + _HF3_Path);
                    sr.WriteLine("HF3_CoverSensibility = " + _HF3_CoverSensibility.ToString());
                    sr.WriteLine("");
                    sr.WriteLine(";Heavy Fire Shattered Spear settings");
                    sr.WriteLine("HF4_Path = " + _HF4_Path);
                    sr.WriteLine("HF4_CoverSensibility = " + _HF4_CoverSensibility.ToString());
                    sr.WriteLine("");
                    sr.WriteLine(";Wild West Shoutout settings");
                    sr.WriteLine("WWS_Path = " + _Wws_Path);
                    sr.WriteLine("WWS_P1_COIN_KEY = " + _DIK_Wws_P1Coin.ToString());
                    sr.WriteLine("WWS_P2_COIN_KEY = " + _DIK_Wws_P2Coin.ToString());
                    sr.WriteLine("WWS_TEST_KEY = " + _DIK_Wws_Test.ToString());
                    sr.WriteLine("");
                    sr.WriteLine(";Rabbids Hollywood settings");
                    sr.WriteLine("RHA_Path = " + _Rha_Path);
                    sr.WriteLine("");
                    sr.WriteLine(";Operation G.H.O.S.T credits settings");
                    sr.WriteLine("OpGhost_EnableFreeplay = " + _OpGHost_EnableFreeplay.ToString());
                    sr.WriteLine("OpGhost_CreditsToStart = " + _OpGhost_CreditsToStart.ToString());
                    sr.WriteLine("OpGhost_CreditsToContinue = " + _OpGhost_CreditsToContinue.ToString());
                    sr.WriteLine("OpGhost_CoinsByCredits = " + _OpGhost_CoinsByCredits.ToString());
                    sr.WriteLine("OpGhost_SeparateButtons = " + _OpGhost_SeparateButtons.ToString());
                    sr.WriteLine("OpGhost_P1_ACTION_KEY = " + _DIK_OpGhost_Action_P1.ToString());
                    sr.WriteLine("OpGhost_P2_ACTION_KEY = " + _DIK_OpGhost_Action_P2.ToString());
                    sr.WriteLine("");
                    sr.WriteLine(";RPCS3 Keys (System 357 only)");
                    sr.WriteLine("RPCS3_P1_START = " + _DIK_Rpcs3_P1_Start.ToString());
                    sr.WriteLine("RPCS3_P2_START = " + _DIK_Rpcs3_P2_Start.ToString());
                    sr.WriteLine("RPCS3_SERVICE = " + _DIK_Rpcs3_Service.ToString());
                    sr.WriteLine("RPCS3_UP = " + _DIK_Rpcs3_Up.ToString());
                    sr.WriteLine("RPCS3_DOWN = " + _DIK_Rpcs3_Down.ToString());
                    sr.WriteLine("RPCS3_ENTER = " + _DIK_Rpcs3_Enter.ToString());
                    sr.WriteLine("RPCS3_3D = " + _DIK_Rpcs3_3D_Switch.ToString());
                    sr.WriteLine("");
                    sr.WriteLine(";Enable Pedal-Mode for Lethal Enforcers 3, and set Keys");
                    sr.WriteLine("LE3_P1_PEDAL_ENABLE = " + _Le3_Pedal_P1_Enabled.ToString());
                    sr.WriteLine("LE3_P1_PEDAL_KEY = " + _DIK_Le3_Pedal_P1.ToString());
                    sr.WriteLine("LE3_P2_PEDAL_ENABLE = " + _Le3_Pedal_P2_Enabled.ToString());
                    sr.WriteLine("LE3_P2_PEDAL_KEY = " + _DIK_Le3_Pedal_P2.ToString());
                    sr.WriteLine("");
                    sr.WriteLine(";Output Settings");
                    sr.WriteLine("OutputEnabled = " + _OutputEnabled.ToString());
                    sr.WriteLine("OutputPollingDelay = " + _OutputPollingDelay.ToString());
                    sr.WriteLine("OutputCustomDamagedDelay = " + _OutputCustomDamagedDelay.ToString());
                    sr.WriteLine("OutputCustomRecoilOnDelay = " + _OutputCustomRecoilOnDelay.ToString());
                    sr.WriteLine("OutputCustomRecoilOffDelay = " + _OutputCustomRecoilOffDelay.ToString());
                    sr.WriteLine("");
                    sr.WriteLine(";DemulShooter Hooking procedure Timeout value");
                    sr.WriteLine("HookTimeout = " + _HookTimeout.ToString());
                    sr.Close();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Write Silent Hill the Arcade key mapping
        /// </summary>
        public bool Write_Sha_Config()
        {
            try
            {
                String appData = Environment.GetEnvironmentVariable("appdata").ToString();
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
                        if (_DIK_Sha_Exit != 0)
                        {
                            w.Write((short)0);
                            w.Write((byte)_DIK_Sha_Exit);
                            w.Write((byte)0xFF);
                            n++;
                        }
                        if (_DIK_Sha_Test != 0)
                        {
                            w.Write((short)0);
                            w.Write((byte)_DIK_Sha_Test);
                            w.Write((byte)0x01);
                            n++;
                        }
                        if (_DIK_Sha_Service != 0)
                        {
                            w.Write((short)0);
                            w.Write((byte)_DIK_Sha_Service);
                            w.Write((byte)0x02);
                            n++;
                        }
                        if (_DIK_Sha_P1_Start != 0)
                        {
                            w.Write((short)0);
                            w.Write((byte)_DIK_Sha_P1_Start);
                            w.Write((byte)0x10);
                            n++;
                        }
                        if (_DIK_Sha_P1_Trigger != 0)
                        {
                            w.Write((short)0);
                            w.Write((byte)_DIK_Sha_P1_Trigger);
                            w.Write((byte)0x11);
                            n++;
                        }
                        if (_DIK_Sha_P2_Start != 0)
                        {
                            w.Write((short)0);
                            w.Write((byte)_DIK_Sha_P2_Start);
                            w.Write((byte)0x20);
                            n++;
                        }
                        if (_DIK_Sha_P2_Trigger != 0)
                        {
                            w.Write((short)0);
                            w.Write((byte)_DIK_Sha_P2_Trigger);
                            w.Write((byte)0x21);
                            n++;
                        }

                        w.Seek(0, SeekOrigin.Begin);
                        w.Write(n);
                    }
                }                
                return true;
            }
            catch
            {                
                return false;
            }
        }     
    }
}
