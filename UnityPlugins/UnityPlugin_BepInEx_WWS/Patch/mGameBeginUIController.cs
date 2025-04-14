using HarmonyLib;

namespace WildWestShootout_BepInEx_DemulShooter_Plugin
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
