using System;
using System.IO;

namespace UnityPlugin_BepInEx_NHA2
{
    public class ConfManager
    {
        //Gameplay Settings
        private int _ResolutionWidth = 1920;
        private int _ResolutionHeight = 1080;
        private int _Fullscreen = 0;
        private int _InputMode = 0; //0=Mouse(1P), 1=DemulShooter(2P)
        private int _RemoveCrosshair = 0;
        private int _RemoveLaser = 0;
        private int _RemoveGun = 0;

        private string _ConfFilePath = "./InputPlugin_Config.ini";

        #region Accessors

        public bool Fullscreen
        { get { return _Fullscreen == 1 ? true : false; } }
        public float ResolutionWidth
        { get { return (float)_ResolutionWidth; } }
        public float ResolutionHeight
        { get { return (float)_ResolutionHeight; } }
        public NightHunterArcade2_Plugin.InputMode InputMode
        { get { return _InputMode == 1 ? NightHunterArcade2_Plugin.InputMode.DemulShooter : NightHunterArcade2_Plugin.InputMode.Mouse; } }
        public bool RemoveCrosshair
        { get { return _RemoveCrosshair == 1 ? true : false; } }
        public bool RemoveLaser
        { get { return _RemoveLaser == 1 ? true : false; } }
        public bool RemoveGuns
        { get { return _RemoveGun == 1 ? true : false; } }


        #endregion

        public ConfManager()
        {
            ReadConf(_ConfFilePath);
        }

        public void ReadConf(string ConfigFilePath)
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

                                if (StrKey.Equals("Fullscreen"))
                                {
                                    if (int.TryParse(StrValue, out _Fullscreen))
                                        NightHunterArcade2_Plugin.MyLogger.LogMessage("ConfManager.Readconf() => _" + StrKey + "=" + _Fullscreen.ToString());
                                    else
                                        NightHunterArcade2_Plugin.MyLogger.LogError("ConfManager.Readconf() => Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.Equals("ResolutionWidth"))
                                {
                                    if (int.TryParse(StrValue, out _ResolutionWidth))
                                        NightHunterArcade2_Plugin.MyLogger.LogMessage("ConfManager.Readconf() => _" + StrKey + "=" + _ResolutionWidth.ToString());
                                    else
                                        NightHunterArcade2_Plugin.MyLogger.LogError("ConfManager.Readconf() => Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.Equals("ResolutionHeight"))
                                {
                                    if (int.TryParse(StrValue, out _ResolutionHeight))
                                        NightHunterArcade2_Plugin.MyLogger.LogMessage("ConfManager.Readconf() => _" + StrKey + "=" + _ResolutionHeight.ToString());
                                    else
                                        NightHunterArcade2_Plugin.MyLogger.LogError("ConfManager.Readconf() => Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.Equals("InputMode"))
                                {
                                    if (int.TryParse(StrValue, out _InputMode))
                                        NightHunterArcade2_Plugin.MyLogger.LogMessage("ConfManager.Readconf() => _" + StrKey + "=" + _InputMode.ToString());
                                    else
                                        NightHunterArcade2_Plugin.MyLogger.LogError("ConfManager.Readconf() => Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.Equals("RemoveCrosshair"))
                                {
                                    if (int.TryParse(StrValue, out _RemoveCrosshair))
                                        NightHunterArcade2_Plugin.MyLogger.LogMessage("ConfManager.Readconf() => _" + StrKey + "=" + _RemoveCrosshair.ToString());
                                    else
                                        NightHunterArcade2_Plugin.MyLogger.LogError("ConfManager.Readconf() => Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.Equals("RemoveLaser"))
                                {
                                    if (int.TryParse(StrValue, out _RemoveLaser))
                                        NightHunterArcade2_Plugin.MyLogger.LogMessage("ConfManager.Readconf() => _" + StrKey + "=" + _RemoveLaser.ToString());
                                    else
                                        NightHunterArcade2_Plugin.MyLogger.LogError("ConfManager.Readconf() => Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                                else if (StrKey.Equals("RemoveGun"))
                                {
                                    if (int.TryParse(StrValue, out _RemoveGun))
                                        NightHunterArcade2_Plugin.MyLogger.LogMessage("ConfManager.Readconf() => _" + StrKey + "=" + _RemoveGun.ToString());
                                    else
                                        NightHunterArcade2_Plugin.MyLogger.LogError("ConfManager.Readconf() => Error parsing " + StrKey + " value in INI file : " + StrValue + " is not valid");
                                }
                            }
                        }
                        line = sr.ReadLine();
                    }
                    sr.Close();
                    NightHunterArcade2_Plugin.MyLogger.LogMessage("ConfManager.Readconf() => Configuration file succesfuly loaded");
                }
            }
            catch (Exception ex)
            {
                NightHunterArcade2_Plugin.MyLogger.LogError("ConfManager.Readconf() => Error reading " + ConfigFilePath + " : " + ex.Message);
            }
        }
    }
}
