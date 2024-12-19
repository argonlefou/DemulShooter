using HarmonyLib;

namespace UnityPlugin_BepInEx_TRA
{
    class mCheatsManager
    {
        [HarmonyPatch(typeof(SBK.CheatsManager), "Update")]
        class Update
        {
            static bool Prefix()
            {
                return false;
            }
        }
    }
}
