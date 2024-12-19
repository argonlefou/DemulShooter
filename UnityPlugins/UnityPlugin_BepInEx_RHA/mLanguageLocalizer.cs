using System;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace UnityPlugin_BepInEx_RHA
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
                UnityEngine.Debug.Log("mLanguageLocalizer.SwitchLanguage(), i_Code=" + i_Code);
                UnityEngine.Debug.Log("mLanguageLocalizer.SwitchLanguage(), Available Languages :");  
                foreach (String s in ___m_AvailableLanguages)
                {
                    UnityEngine.Debug.Log(s);
                }

                UnityEngine.Debug.Log("mLanguageLocalizer.SwitchLanguage() : Trying to read custom config file...");
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
                    UnityEngine.Debug.Log("mLanguageLocalizer.SwitchLanguage() : Succesfully sets Language to " + i_Code.ToString());
                }
                catch (Exception Ex)
                {
                    UnityEngine.Debug.Log("mLanguageLocalizer.SwitchLanguage() : Can't read config data, using custom default values.");
                    UnityEngine.Debug.Log("mLanguageLocalizer.SwitchLanguage() : " + Ex.Message.ToString());
                }
                
                return true;
            }
        }

    }
}
