using HarmonyLib;

namespace UnityPlugin_BepInEx_RTNA
{
    class mGame
    {
        /// <summary>
        /// Force periodic reboot to always OFF
        /// </summary>
        [HarmonyPatch(typeof(Game), "ShouldPeriodicReboot")]
        class ShouldPeriodicReboot
        {
            static bool Prefix(ref bool __result)
            {
                //NerfArcade_Plugin.MyLogger.LogMessage("Game.ShouldPeriodicReboot()");
                __result = false;
                return false;
            }
        }
    }
}
