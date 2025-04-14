using HarmonyLib;

namespace DCop_BepInEx_DemulShooter_Plugin
{
    class mLifeTracker
    {
        /// <summary>
        /// Get life info
        /// </summary>
        [HarmonyPatch(typeof(LifeTracker), "Update")]
        class Update
        {
            static bool Prefix(int ___lifes)
            {
                //Dcop_Plugin.MyLogger.LogMessage("mLifeTracker.Update()");
                DemulShooter_Plugin.OutputData.P1_Life = (byte)___lifes;                    
                return true;
            }
        }
    }
}
