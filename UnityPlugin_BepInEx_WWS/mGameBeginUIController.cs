using HarmonyLib;

namespace UnityPlugin_BepInEx_WWS
{
    class mGameBeginUIController
    {
        /// <summary>
        /// Removing mouse click on Title Screen to start game
        /// </summary>
        [HarmonyPatch(typeof(GameBeginUIController), "Update")]
        class Update
        {
            static bool Prefix()
            {                
                return false;
            }
        }
    }
}
