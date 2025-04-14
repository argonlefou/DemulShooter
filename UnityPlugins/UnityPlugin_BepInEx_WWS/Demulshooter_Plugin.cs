using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace WildWestShootout_BepInEx_DemulShooter_Plugin
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Demulshooter_Plugin : BaseUnityPlugin
    {
        public const String pluginGuid = "argonlefou.demulshooter.wws";
        public const String pluginName = "WildWestShootout_BepInEx_DemulShooter_Plugin";
        public const String pluginVersion = "2.0.0.0";

        static String MAPPED_FILE_NAME = "DemulShooter_MMF_Wws";
        static String MUTEX_NAME = "DemulShooter_Mutex_Wws";
        static long MAPPED_FILE_CAPACITY = 2048;
        public static WWS_MemoryMappedFile_Controller WWS_Mmf;

        public void Awake()
        {
            Logger.LogMessage("Plugin Loaded");
            Harmony harmony = new Harmony(pluginGuid);

            harmony.PatchAll();

            WWS_Mmf = new WWS_MemoryMappedFile_Controller(MAPPED_FILE_NAME, MUTEX_NAME, MAPPED_FILE_CAPACITY);
            int r = WWS_Mmf.MMFOpen();
            if (r == 0)
            {
                Logger.LogMessage("DemulShooter MMF opened succesfully");
            }
            else
            {
                Logger.LogError("DemulShooter MMF open error : " + r.ToString());
            }
        }
        
        private void HarmonyPatch(Harmony hHarmony, Type OriginalClass, String OriginalMethod, Type ReplacementClass, String ReplacementMethod)
        {
            MethodInfo original = AccessTools.Method(OriginalClass, OriginalMethod);
            MethodInfo patch = AccessTools.Method(ReplacementClass, ReplacementMethod);
            hHarmony.Patch(original, new HarmonyMethod(patch));
        }

        public void OnDestroy()
        {
            Logger.LogMessage("Closing Demulshooter MMF....");
            if (WWS_Mmf.IsOpened)
                WWS_Mmf.MMFClose();
        }

    }
}
