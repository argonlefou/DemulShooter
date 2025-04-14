using HarmonyLib;
using Virtuallyz.VRShooter.IO;

namespace OperationWolf_BepInEx_DemulShooter_Plugin
{
    /// <summary>
    /// Replacing the Cursor Locked command by a flag of our own
    /// Cursor will stay unlocked and info is stored for the mouse update function
    /// </summary>
    class mScreenMouseLook_InOutSystem
    {
        [HarmonyPatch(typeof(ScreenMouseLook_InOutSystem), "ResetCursorNormalState")]
        class ResetCursorNormalState
        {
            static bool Prefix(ref ScreenView ___currentView, ref bool ___UIMode)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("mScreenMouseLook_InOutSystem.ResetCursorNormalState()");
                ___currentView.SetMouseLook(true);
                ___UIMode = false;
                //Cursor.lockState = CursorLockMode.Locked;
                DemulShooter_Plugin.IsMouseLockedRequired = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(ScreenMouseLook_InOutSystem), "UnlockCursor")]
        class UnlockCursor
        {
            static bool Prefix(ref ScreenView ___currentView, ref bool ___UIMode, bool keepWeapon, bool withPointers = true)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("mScreenDuckHunt_InOutSystem.UnlockCursor()");
                ___currentView.SetMouseLook(false);
                ___UIMode = true;
                //Cursor.lockState = CursorLockMode.None;
                DemulShooter_Plugin.IsMouseLockedRequired = false;
                return false;
            }
        }
    }
}
