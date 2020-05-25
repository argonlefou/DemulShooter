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

        //Outputs settings
        private bool _OutputEnabled = false;
        private int _OutputPollingDelay = 20;
        private int _OutputCustomDamagedDelay = 200;
        private int _OutputCustomRecoilDelay = 200;
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
        public int OutputCustomRecoilDelay
        {
            get { return _OutputCustomRecoilDelay; }
            set { _OutputCustomRecoilDelay = value; }
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
                                else if (StrKey.ToLower().Equals("outputcustomrecoildelay"))
                                {
                                    if (!int.TryParse(StrValue, out _OutputCustomRecoilDelay))
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
                    sr.WriteLine(";Offset for devices lacking calibration (Act Labs gun, etc...)");
                    sr.WriteLine("Act_Labs_Offset_Enable = " + _Act_Labs_Offset_Enable.ToString());
                    sr.WriteLine("Act_Labs_Display_Crosshair = " + _Act_Labs_Display_Crosshair.ToString());
                    foreach (PlayerSettings PlayerData in _PlayersSettings)
                    {
                        sr.WriteLine("P" + PlayerData.ID + "Act_Labs_Offset_X = " + PlayerData.Act_Labs_Offset_X.ToString());
                        sr.WriteLine("P" + PlayerData.ID + "Act_Labs_Offset_Y = " + PlayerData.Act_Labs_Offset_Y.ToString());
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
                    sr.WriteLine(";Output Settings");
                    sr.WriteLine("OutputEnabled = " + _OutputEnabled.ToString());
                    sr.WriteLine("OutputPollingDelay = " + _OutputPollingDelay.ToString());
                    sr.WriteLine("OutputCustomDamagedDelay = " + _OutputCustomDamagedDelay.ToString());
                    sr.WriteLine("OutputCustomRecoilDelay = " + _OutputCustomRecoilDelay.ToString());
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
