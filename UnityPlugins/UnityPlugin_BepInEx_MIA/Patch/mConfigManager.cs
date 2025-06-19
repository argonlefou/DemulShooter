using HarmonyLib;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    class mConfigManager
    {
        /// <summary>
        /// Unlock Mission 3
        /// </summary>
        [HarmonyPatch(typeof(ConfigManager), MethodType.Constructor)]
        class CCtor
        {
            static void Postfix(ref int ___m_mission1Progress, ref int ___m_mission2Progress, ref int ___m_mission3Progress, ref bool ___m_mission3Unlocked)
            {
                //___m_mission3Unlocked = true;
            }
        }

        [HarmonyPatch(typeof(ConfigManager), "GetAssetBundleCacheDirOnUnityEditor")]
        class GetAssetBundleCacheDirOnUnityEditor
        {
            static void Postfix(ref string __result)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("ConfigManager.GetAssetBundleCacheDirOnUnityEditor() -> result=" + __result);
                __result = "../NVRAM/GameData/MI_Cache/";
            }
        }

        /// <summary>
        /// This one is used in the current version, fixing PAth to local folder
        /// </summary>
        [HarmonyPatch(typeof(ConfigManager), "GetAssetBundleCacheDirOnExe")]
        class GetAssetBundleCacheDirOnExe
        {
            static void Postfix(ref string __result)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("ConfigManager.GetAssetBundleCacheDirOnExe() -> result=" + __result);
                __result = "../NVRAM/GameData/MI_Cache/";
            }
        }

       /* [HarmonyPatch(typeof(ConfigManager), "Awake")]
        class Awake
        {
            static void Postfix(ref int ___SCREEN_WIDTH, ref int ___SCREEN_HEIGHT)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("ConfigManager.Awake() -> Screen=" + ___SCREEN_WIDTH + "x" + ___SCREEN_HEIGHT);
            }
        }*/
    }
}
