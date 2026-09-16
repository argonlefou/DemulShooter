using HarmonyLib;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mConfigMenu
    {
        /// <summary>
        /// Is This to force a player to shoot/press a button to continue even if crdtis are on ?
        /// Anyway, it still autostart on ...start
        /// </summary>
        [HarmonyPatch(typeof(ConfigMenu), "Value_ForceStartToReady")]
        class Value_ForceStartToReady
        {
            static bool Prefix(ref bool __result)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("ConfigMenu.Value_ForceStartToReady()");
                __result = false;
                return false;
            }
        }
    }
}
