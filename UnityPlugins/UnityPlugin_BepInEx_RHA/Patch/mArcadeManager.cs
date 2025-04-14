using HarmonyLib;

namespace RabbidsHollywood_BepInEx_DemulShooter_Plugin
{
    class mArcadeManager
    {
        [HarmonyPatch(typeof(ArcadeManager), "CheckIOBoard")]
        class CheckIOBoard
        {
            static bool Prefix()
            {
                DemulShooter_Plugin.MyLogger.LogMessage("mArcadeManager.CheckIOBoard()");
                return false;         
            }
        }

        [HarmonyPatch(typeof(ArcadeManager), "GunMalfunctionCheck")]
        class GunMalfunctionCheck
        {
            static bool Prefix()
            {
                DemulShooter_Plugin.MyLogger.LogMessage("mArcadeManager.GunMalfunctionCheck()");
                return false;
            }
        }

        //Removes the error screen
        [HarmonyPatch(typeof(ArcadeManager), "PushErrorPopup")]
        class PushErrorPopup
        {
            static bool Prefix()
            {
                DemulShooter_Plugin.MyLogger.LogMessage("mArcadeManager.PushErrorPopup()");
                return false;
            }
        }
    }
}
