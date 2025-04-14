using HarmonyLib;

namespace DCop_BepInEx_DemulShooter_Plugin
{
    /*class mU_policeLight
    {
        /// <summary>
        /// Trigger the siren and police light
        /// </summary>
        [HarmonyPatch(typeof(U_policeLight), "TurnOnPin")]
        class TurnOnPin
        {
            static bool Prefix()
            {
                Dcop_Plugin.MyLogger.LogMessage("mU_policeLight.TurnOnPin()");
                Dcop_Plugin.OutputData.Police_LightBar = 1;
                return true;
            }
        }
        [HarmonyPatch(typeof(U_policeLight), "TurnOffPin")]
        class TurnOffPin
        {
            static bool Prefix()
            {
                Dcop_Plugin.MyLogger.LogMessage("mU_policeLight.TurnOffPin()");
                Dcop_Plugin.OutputData.Police_LightBar = 0;
                return true;
            }
        }
    }*/
}
