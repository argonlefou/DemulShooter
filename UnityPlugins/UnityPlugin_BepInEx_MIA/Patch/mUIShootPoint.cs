using HarmonyLib;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    class mUIShootPoint
    {
        /// <summary>
        /// Removing Crosshair
        /// </summary>
        [HarmonyPatch(typeof(UIShootPoint), "Update")]
        class Update
        {
            static bool Prefix(UnityEngine.GameObject ___m_shootPointOn)
            {
                if ((PlayerManager.Instance.GetPlayer(PID.PID_ONE).PMode >= PlayerMode.PM_GAME && PlayerManager.Instance.GetPlayer(PID.PID_ONE).PMode <= PlayerMode.PM_GAME_OVER)
                    || (PlayerManager.Instance.GetPlayer(PID.PID_TWO).PMode >= PlayerMode.PM_GAME && PlayerManager.Instance.GetPlayer(PID.PID_TWO).PMode <= PlayerMode.PM_GAME_OVER))
                {
                    ___m_shootPointOn.SetActive(false);
                }
                return true;
            }
        }
    }
}
