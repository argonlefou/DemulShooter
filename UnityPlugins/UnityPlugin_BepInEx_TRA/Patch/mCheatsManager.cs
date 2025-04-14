using HarmonyLib;

namespace TombRaider_BepInEx_DemulShooter_Plugin
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
