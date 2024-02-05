using HarmonyLib;

namespace UnityPlugin_BepInEx_PBX
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
                ___isCrosshairVisible = PointBlankX_Plugin.CrossHairVisibility;
                PointBlankX_Plugin.MyLogger.LogMessage("mGlobalData() => isCrosshairVisible: " + ___isCrosshairVisible);
            }
        }
    }
}
