using HarmonyLib;

namespace DCop_BepInEx_DemulShooter_Plugin
{
    /*class mU_onPlayerShoot
    {
        /// <summary>
        /// Trigger the gun recoil and gun light
        /// </summary>
        [HarmonyPatch(typeof(U_onPlayerShoot), "TurnOnPin")]
        class TurnOnPin
        {
            static bool Prefix()
            {
                Dcop_Plugin.MyLogger.LogMessage("mU_onPlayerShoot.TurnOnPin()");
                Dcop_Plugin.OutputData.P1_Recoil = 1;
                return true;
            }
        }
    }*/
}