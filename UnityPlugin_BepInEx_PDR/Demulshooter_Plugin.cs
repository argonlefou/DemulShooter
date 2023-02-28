using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace UnityPlugin_BepInEx_PDR
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Demulshooter_Plugin : BaseUnityPlugin
    {
        public const String pluginGuid = "argonlefou.demulshooter.pdr";
        public const String pluginName = "PDR DemulShooter Plugin";
        public const String pluginVersion = "1.0.0.0";

        public void Awake()
        {
            Logger.LogMessage("Plugin Loaded");
            Harmony harmony = new Harmony(pluginGuid);

            harmony.PatchAll();
        }
        
        private void HarmonyPatch(Harmony hHarmony, Type OriginalClass, String OriginalMethod, Type ReplacementClass, String ReplacementMethod)
        {
            MethodInfo original = AccessTools.Method(OriginalClass, OriginalMethod);
            MethodInfo patch = AccessTools.Method(ReplacementClass, ReplacementMethod);
            hHarmony.Patch(original, new HarmonyMethod(patch));
        }
    }
}
