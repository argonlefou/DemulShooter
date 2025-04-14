using HarmonyLib;

namespace RabbidsHollywood_BepInEx_DemulShooter_Plugin
{
    class mArcadeGamePlayManager
    {
        [HarmonyPatch(typeof(ArcadeGameplayManager), "LoadDemoMode")]
        class LoadDemoMode
        {
            static bool Prefix(WorldManager.WorldIndex i_World)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("mArcadeGameplayManager.LoadDemoMode()");
                DemulShooter_Plugin.IsDemoMode = true;
                return true;
            }
        }

        [HarmonyPatch(typeof(ArcadeGameplayManager), "OnDemoStateExit")]
        class OnDemoStateExit
        {
            static bool Prefix()
            {
                DemulShooter_Plugin.MyLogger.LogMessage("mArcadeGameplayManager.OnDemoStateExit()");
                DemulShooter_Plugin.IsDemoMode = false;
                return true;
            }
        }

    }
}
