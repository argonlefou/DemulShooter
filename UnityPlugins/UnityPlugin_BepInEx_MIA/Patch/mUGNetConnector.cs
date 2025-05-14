using HarmonyLib;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    class mUGNetConnector
    {
        /// <summary>
        /// Force SinglePlay
        /// </summary>
        [HarmonyPatch(typeof(UGNetConnector), "Awake")]
        class Awake
        {
            static void Postfix(ref bool ___m_isSinglePlay, bool ___m_isHost)
            {
                ___m_isSinglePlay = true;
                DemulShooter_Plugin.MyLogger.LogMessage("UGNetConnector.Awake() : m_isSinglePlay=" + ___m_isSinglePlay + ", m_isHost=" + ___m_isHost);
            }
        }
    }
}
