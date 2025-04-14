using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_NHA2
{
    class mtest_screen
    {
        /// <summary>
        /// Changing resolution when the game is opening it's window
        /// </summary>
        [HarmonyPatch(typeof(test_screen), "Start")]
        class start
        {
            static bool Prefix()
            {
                DemulShooter_Plugin.MyLogger.LogMessage("test_screen.start() => Changing resolution to " + DemulShooter_Plugin.Configurator.ResolutionWidth + "x" + DemulShooter_Plugin.Configurator.ResolutionHeight + " fullscreen: " + DemulShooter_Plugin.Configurator.Fullscreen);
                UnityEngine.Screen.SetResolution((int)DemulShooter_Plugin.Configurator.ResolutionWidth, (int)DemulShooter_Plugin.Configurator.ResolutionHeight, DemulShooter_Plugin.Configurator.Fullscreen);
                return false;
            }
        }
    }
}
