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
                NightHunterArcade2_Plugin.MyLogger.LogMessage("test_screen.start() => Changing resolution to " + NightHunterArcade2_Plugin.Configurator.ResolutionWidth + "x" + NightHunterArcade2_Plugin.Configurator.ResolutionHeight + " fullscreen: " + NightHunterArcade2_Plugin.Configurator.Fullscreen);
                UnityEngine.Screen.SetResolution((int)NightHunterArcade2_Plugin.Configurator.ResolutionWidth, (int)NightHunterArcade2_Plugin.Configurator.ResolutionHeight, NightHunterArcade2_Plugin.Configurator.Fullscreen);
                return false;
            }
        }
    }
}
