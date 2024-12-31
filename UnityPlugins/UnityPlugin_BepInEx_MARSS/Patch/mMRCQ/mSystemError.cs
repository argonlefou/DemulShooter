using HarmonyLib;

namespace UnityPlugin_BepInEx_MarsSortie.Patch
{
    class mSystemError
    {
        /// <summary>
        /// Removing upperleft red messages on screen
        /// </summary>
        [HarmonyPatch(typeof(MRCQ.SystemError), "SetIOBoardError")]
        class SetIOBoardError
        {
            static bool Prefix(string error)
            {
                //MarsSortie_Test_BepInEx_Plugin.MyLogger.LogMessage("MRCQ.SystemError.SetIOBoardError(): error=" + error);
                return false;
            }
        }

        [HarmonyPatch(typeof(MRCQ.SystemError), "SetLaserCamError")]
        class SetLaserCamError
        {
            static bool Prefix(string error)
            {
                //MarsSortie_Test_BepInEx_Plugin.MyLogger.LogMessage("MRCQ.SystemError.SetLaserCamError(): error=" + error);
                return false;
            }
        }
    }
}
