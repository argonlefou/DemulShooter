using HarmonyLib;

namespace UnityPlugin_BepInEx_WWS
{
    class mGameChooseUI2
    {
        /// <summary>
        /// Disabling mouse input on Level Select Screen
        /// </summary>
        [HarmonyPatch(typeof(GameChooseUI2), "Update")]
        class Update
        {
            static bool Prefix()
            { 
                return false;
            }
        }
    }
}
