using HarmonyLib;
using Pvz;

namespace UnityPlugin_BepInEx_PVZ.Patch
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
                if (!PvZ_BepInEx_Plugin.CrossHairVisibility)
                    ___show = false;

                return true;
            }
        }
    }
}
