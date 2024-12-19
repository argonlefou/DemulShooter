using HarmonyLib;
using Virtuallyz.VRShooter.IO;

namespace UnityPlugin_BepInEx_OWR
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
