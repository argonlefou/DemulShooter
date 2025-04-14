using HarmonyLib;

namespace NerfArcade_BepInEx_DemulShooter_Plugin
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
                DemulShooter_Plugin.OutputData.Recoil[___m_playerIndex] = 1;
                return true;
            }
        }
    }
}
