using HarmonyLib;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    class mBootSequence
    {
        /// <summary>
        /// Changing resolution
        /// </summary>
        [HarmonyPatch(typeof(BootSequence), "Start")]
        class Start
        {
            static void Postfix()
            {
                if (DemulShooter_Plugin.ChangeResolution)
                {
                    DemulShooter_Plugin.MyLogger.LogMessage("BootSequence.Start(): Changind Screen resolution to " + DemulShooter_Plugin.ScreenWidth + "x" + DemulShooter_Plugin.ScreenHeight + ", Fullscreen=" + DemulShooter_Plugin.Fullscreen);
                    UnityEngine.Screen.SetResolution(DemulShooter_Plugin.ScreenWidth, DemulShooter_Plugin.ScreenHeight, DemulShooter_Plugin.Fullscreen);
                }
            }
        }
    }
}
