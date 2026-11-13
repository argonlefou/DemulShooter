using HarmonyLib;

namespace BepInEx_DemulShooter_Plugin
{
    class mMP_ControllerDisconnectedPopup
    {
        /// <summary>
        /// No controller assignment popup when starting/continuing a multiplayer campaign, guns are handled by DemulShooter
        /// P2 can join at any time with his START button
        /// </summary>
        [HarmonyPatch(typeof(MP_ControllerDisconnectedPopup), "ShouldShowMultiplayerAssignmentPopup")]
        class ShouldShowMultiplayerAssignmentPopup
        {
            static bool Prefix(ref bool __result)
            {
                if (DemulShooter_Plugin.EnableInputHack)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
        }
    }
}
