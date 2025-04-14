using HarmonyLib;
using Virtuallyz.VRShooter.IO;

namespace OperationWolf_BepInEx_DemulShooter_Plugin
{
    class mPlayerInOutModule
    {
        /// <summary>
        /// Disable the "Pause" popup when the 2nd controller is missing
        /// </summary>
        [HarmonyPatch(typeof(PlayerInOutModule), "OnMissingControllerTriggered")]
        class OnMissingControllerTriggered
        {
            static bool Prefix()
            {
                return false;
            }
        }
    }
}
