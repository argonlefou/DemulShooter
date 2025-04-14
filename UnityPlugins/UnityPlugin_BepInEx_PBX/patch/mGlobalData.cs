using HarmonyLib;

namespace PointBlankX_BepInEx_DemulShooter_Plugin
{
    class mGlobalData
    {
        /// <summary>
        /// Set the "crosshairvisible" flag as we want
        /// </summary>
        [HarmonyPatch(typeof(GlobalData), MethodType.Constructor)]
        class Ctor
        {
            static void Postfix(ref bool ___isCrosshairVisible)
            {
                ___isCrosshairVisible = DemulShooter_Plugin.CrossHairVisibility;
                DemulShooter_Plugin.MyLogger.LogMessage("mGlobalData() => isCrosshairVisible: " + ___isCrosshairVisible);
            }
        }
    }
}
