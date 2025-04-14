using HarmonyLib;
using Pvz;

namespace PvZ_BepInEx_DemulShooter_Plugin.Patch
{
    class mPvzCrosshair
    {
        /// <summary>
        /// Hiding crosshair if needed
        /// </summary>
        [HarmonyPatch(typeof(PvzCrosshair), "OnGUI")]
        class OnGUI
        {
            static bool Prefix(ref bool ___show)
            {
                if (!DemulShooter_Plugin.CrossHairVisibility)
                    ___show = false;

                return true;
            }
        }
    }
}
