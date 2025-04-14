using HarmonyLib;

namespace DCop_BepInEx_DemulShooter_Plugin
{
    /*class mU_hitboxHit
    {
        /// <summary>
        /// Trigger the yellow light
        /// </summary>
        [HarmonyPatch(typeof(U_hitboxHit), "Update")]
        class TurnOnPin
        {
            static bool Prefix(bool ___turnOnLed)
            {
                //Dcop_Plugin.MyLogger.LogMessage("mU_hitboxHit.Update()");
                if (___turnOnLed)
                    Dcop_Plugin.OutputData.P1_Damaged = 1;
                return true;
            }
        }
    }*/
}
