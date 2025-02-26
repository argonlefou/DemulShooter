using HarmonyLib;

namespace UnityPlugin_BepInEx_RTNA
{
    class mPlayerInfo
    {
        /// <summary>
        /// Creating recoil data
        /// </summary>
        [HarmonyPatch(typeof(PlayerInfo), "ShotFired")]
        class ShotFired
        {
            static bool Prefix(int ___m_playerIndex)
            {
                NerfArcade_Plugin.OutputData.Recoil[___m_playerIndex] = 1;
                return true;
            }
        }
    }
}
