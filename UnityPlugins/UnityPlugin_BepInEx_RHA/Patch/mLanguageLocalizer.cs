using System;
using System.Collections.Generic;
using HarmonyLib;

namespace RabbidsHollywood_BepInEx_DemulShooter_Plugin
{
    class mLanguageLocalizer
    {
        /// <summary>
        /// Available Languages : EN / FR / JA / ZH
        /// </summary>
        [HarmonyPatch(typeof(SBK.Localization.LanguageLocalizer), "SwitchLanguage")]
        class SwitchLanguage
        {
            static bool Prefix(ref SBK.Localization.LanguageCode i_Code, List<String> ___m_AvailableLanguages)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("mLanguageLocalizer.SwitchLanguage(), i_Code=" + i_Code);
                DemulShooter_Plugin.MyLogger.LogMessage("mLanguageLocalizer.SwitchLanguage(), Available Languages :");  
                foreach (String s in ___m_AvailableLanguages)
                {
                    DemulShooter_Plugin.MyLogger.LogMessage(s);
                }

                DemulShooter_Plugin.MyLogger.LogMessage("mLanguageLocalizer.SwitchLanguage() : Trying to read custom config file...");
                try
                {
                    String[] lines = System.IO.File.ReadAllLines("RabbidsHollywood_Operator.conf");
                    foreach (String line in lines)
                    {
                        String[] buffer = line.Split(':');
                        String Key = buffer[0];
                        String Value = buffer[1];

                        if (Key.Equals("Language"))
                        {
                            switch (Value)
                            {
                                case "EN":
                                    {
                                        i_Code = SBK.Localization.LanguageCode.EN;
                                    } break;
                                case "FR":
                                    {
                                        i_Code = SBK.Localization.LanguageCode.FR;
                                    } break;
                                case "JA":
                                    {
                                        i_Code = SBK.Localization.LanguageCode.JA;
                                    } break;
                                case "ZH":
                                    {
                                        i_Code = SBK.Localization.LanguageCode.ZH;
                                    } break;
                                default:
                                    {
                                        i_Code = SBK.Localization.LanguageCode.EN;
                                    } break;
                            }
                            break;
                        }
                    }
                    DemulShooter_Plugin.MyLogger.LogMessage("mLanguageLocalizer.SwitchLanguage() : Succesfully sets Language to " + i_Code.ToString());
                }
                catch (Exception Ex)
                {
                    DemulShooter_Plugin.MyLogger.LogMessage("mLanguageLocalizer.SwitchLanguage() : Can't read config data, using custom default values.");
                    DemulShooter_Plugin.MyLogger.LogMessage("mLanguageLocalizer.SwitchLanguage() : " + Ex.Message.ToString());
                }
                
                return true;
            }
        }

    }
}
