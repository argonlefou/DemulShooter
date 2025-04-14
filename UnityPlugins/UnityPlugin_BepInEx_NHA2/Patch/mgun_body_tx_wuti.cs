using HarmonyLib;

namespace UnityPlugin_BepInEx_NHA2
{
    class mgun_body_tx_wuti
    {
        /// <summary>
        /// Remove laser sight
        /// </summary>
        [HarmonyPatch(typeof(gun_body_tx_wuti), "update_linerenderer")]
        class update_linerenderer
        {
            static bool Prefix(gun_body_tx_wuti __instance, int num, gun_body gun_body1)
            {
                //NightHunterArcadePlugin.MyLogger.LogMessage("gun_fire_work.update_linerenderer()");
                if (DemulShooter_Plugin.Configurator.RemoveLaser)
                {
                    if (__instance.mylinerenderer != null)
                    {
                        __instance.mylinerenderer.enabled = false; // <------------------------- Remove sight line
                    }
                    return false;
                }

                return true;
            }
        }
    }
}
