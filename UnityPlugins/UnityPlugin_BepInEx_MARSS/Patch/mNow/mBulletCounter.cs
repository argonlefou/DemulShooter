using HarmonyLib;
using Now;

namespace UnityPlugin_BepInEx_MarsSortie.Patch.mNow
{
    class mBulletCounter
    {
        [HarmonyPatch(typeof(BulletCounter), "SetBulletCount")]
        class SetBulletCount
        {
            static bool Prefix(PlayerPanel ___playerPanel, int count)
            {
                MarsSortie_BepInEx_Plugin.MyLogger.LogMessage("SetBulletCount: " + ___playerPanel.playerIndex + ", count=" + count);
                return true;
            }
        }
    }
}
