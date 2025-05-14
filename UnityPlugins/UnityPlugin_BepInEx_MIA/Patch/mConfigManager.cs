using HarmonyLib;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    class mConfigManager
    {
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

        /// <summary>
        /// Changing Unity ScreenResolution will only impact 3D rendering
        /// 2D sprites will stay on original resolution rendering, but there's a general 2DCanvas here to be able to change that
        /// </summary>
        [HarmonyPatch(typeof(UIManager), "Update")]
        class Update
        {
            static void Postfix(UIManager __instance)
            {
                if (DemulShooter_Plugin.ChangeResolution)
                {
                    foreach (UnityEngine.UI.CanvasScaler cs in __instance.MainCanvas2D.GetComponents<UnityEngine.UI.CanvasScaler>())
                    {
                        cs.scaleFactor = (float)DemulShooter_Plugin.ScreenWidth / DemulShooter_Plugin.ORIGINAL_WIDTH;
                    }
                }
            }
        }

    }
}
