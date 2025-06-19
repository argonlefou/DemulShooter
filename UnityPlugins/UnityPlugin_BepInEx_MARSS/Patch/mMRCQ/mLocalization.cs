using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;

namespace MarsSortie_BepInEx_DemulShooter_Plugin.Patch.mMRCQ
{
     class mLocalization
    {
        /// <summary>
        /// List available Languages
        /// </summary>
        [HarmonyPatch]
        class LoadMask
        {
            static MethodBase TargetMethod()
            {
                MethodInfo[] mis = AccessTools.TypeByName("MRCQ.Localization").GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (MethodInfo mi in mis)
                {
                    if (mi.Name.Equals("LoadMask"))
                        return mi;
                }
                return null;
            }
            static bool Prefix(List<string> ___languages)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("MRCQ.Localization.LoadMask(): Available languages:");
                for (int i = 0; i < ___languages.Count; i++)
                {
                    DemulShooter_Plugin.MyLogger.LogMessage(___languages[i]);
                }
                return true;
            }
        }

        /// <summary>
        /// Force SetLanguage as English
        /// </summary>
        [HarmonyPatch]
        class SetLanguage
        {
            static MethodBase TargetMethod()
            {
                MethodInfo[] mis = AccessTools.TypeByName("MRCQ.Localization").GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
                foreach (MethodInfo mi in mis)
                {
                    if (mi.Name.Equals("SetLanguage"))
                        return mi;
                }
                return null;
            }
            static bool Prefix(ref string language)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("MRCQ.Localization.SetLanguage(): language=" + language);
                language = "English";
                return true;
            }
        }

        /// <summary>
        /// Force word translation to be used in English
        /// Degaut call has language empty string, even if English is set above as a Language
        /// </summary>
        [HarmonyPatch]
        class Tran
        {
            static MethodBase TargetMethod()
            {
                MethodInfo[] mis = AccessTools.TypeByName("MRCQ.Localization").GetMethods(BindingFlags.Static | BindingFlags.Public);
                foreach (MethodInfo mi in mis)
                {
                    if (mi.Name.Equals("Tran"))
                        return mi;
                }
                return null;
            }
            static bool Prefix(string str, ref string language)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("MRCQ.Localization.Tran(): str=" + str + ", language=" + language);
                language = "English";
                return true;
            }
            static void Postfix(string str, string language, string __result)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("MRCQ.Localization.Tran(): __result=" + __result);
            }
        }
    }
}
