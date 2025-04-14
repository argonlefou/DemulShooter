using HarmonyLib;
using UnityEngine;

namespace PvZ_BepInEx_DemulShooter_Plugin.Patch
{
    class mInput
    {
        /// <summary>
        /// Overriding original button detection by the game with our own controls
        /// </summary>
        [HarmonyPatch(typeof(Input), "GetButton")]
        class GzetButton
        {
            static bool Prefix(string buttonName, ref bool __result)
            {
                if (buttonName.Equals("Fire1") && DemulShooter_Plugin.EnableInputHack)
                {
                    __result = DemulShooter_Plugin.PluginPlayerController.GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Trigger);
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Overriding original button detection by the game with our own controls
        /// </summary>        
        [HarmonyPatch(typeof(Input), "GetButtonDown")]
        class GetButtonDown
        {
            static bool Prefix(string buttonName, ref bool __result)
            {
                if (buttonName.Equals("Fire1") && DemulShooter_Plugin.EnableInputHack)
                {
                    __result = DemulShooter_Plugin.PluginPlayerController.GetButtonDown(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Trigger);
                    if (__result)
                        DemulShooter_Plugin.MyLogger.LogWarning("Trigger down");
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Overriding original button detection by the game with our own controls
        /// </summary>        
        [HarmonyPatch(typeof(Input), "GetButtonUp")]
        class GetButtonUp
        {
            static bool Prefix(string buttonName, ref bool __result)
            {
                if (buttonName.Equals("Fire1") && DemulShooter_Plugin.EnableInputHack)
                {
                    __result = DemulShooter_Plugin.PluginPlayerController.GetButtonUp(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Trigger);
                    if (__result)
                        DemulShooter_Plugin.MyLogger.LogWarning("Trigger up");
                    return false;
                }
                return true;
            }
        }
    }
}
