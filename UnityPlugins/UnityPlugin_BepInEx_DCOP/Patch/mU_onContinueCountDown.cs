using HarmonyLib;

namespace DCop_BepInEx_DemulShooter_Plugin
{
    /*class mU_onContinueCountDown
    {
        /// <summary>
        /// Trigger the RED light
        /// </summary>
        [HarmonyPatch(typeof(U_onContinueCountdown), "TurnOnPin")]
        class TurnOnPin
        {
            static bool Prefix()
            {
                Dcop_Plugin.MyLogger.LogMessage("mU_onContinueCountdown.TurnOnPin()");
                Dcop_Plugin.OutputData.RedLight = 1;
                return true;
            }
        }
        [HarmonyPatch(typeof(U_onContinueCountdown), "TurnOffPin")]
        class TurnOffPin
        {
            static bool Prefix()
            {
                Dcop_Plugin.MyLogger.LogMessage("mU_onContinueCountdown.TurnOffPin()");
                Dcop_Plugin.OutputData.RedLight = 0;
                return true;
            }
        }
    }*/
}
