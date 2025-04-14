using HarmonyLib;

namespace NerfArcade_BepInEx_DemulShooter_Plugin
{
    class mSys_Linux
    {
        /// <summary>
        /// Deactivating linux kernel operation
        /// </summary>
        [HarmonyPatch(typeof(Sys_Linux), "CreateUSBMountRules")]
        class CreateUSBMountRules
        {
            static bool Prefix()
            {
                DemulShooter_Plugin.MyLogger.LogMessage("Sys_Linux.CreateUSBMountRules()");
                return false;
            }
        }
        [HarmonyPatch(typeof(Sys_Linux), "CreateFlushBufferScript")]
        class CreateFlushBufferScript
        {
            static bool Prefix()
            {
                DemulShooter_Plugin.MyLogger.LogMessage("Sys_Linux.CreateFlushBufferScript()");
                return false;
            }
        }
        
    }
}
