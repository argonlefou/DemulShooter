using HarmonyLib;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mIGun
    {
        /// <summary>
        /// Remove CAMERA ERROR message from main game
        /// </summary>
        [HarmonyPatch(typeof(Boys.CamGun.IGun), "CamError")]
        class CamError
        {
            static bool Prefix(ref bool __result)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("Boys.CamGun.IGun : " );
                __result = false;
                return false;
            }
        }

        /// <summary>
        /// Force it to be good, to make Aim work
        /// </summary>
        [HarmonyPatch(typeof(Boys.CamGun.IGun), "LossPoint")]
        class LossPoint
        {
            static bool Prefix(ref bool __result)
            {
                __result = false;
                return false;
            }
        }
    }
}
