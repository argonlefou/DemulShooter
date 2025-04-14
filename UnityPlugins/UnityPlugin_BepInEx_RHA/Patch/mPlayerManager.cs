using HarmonyLib;

namespace RabbidsHollywood_BepInEx_DemulShooter_Plugin
{
    class mPlayerManager
    {
        /// <summary>
        /// Getting Damaged flag
        /// </summary>
        [HarmonyPatch(typeof(PlayerManager), "GetDamage")]
        class GetDamage
        {
            static bool Prefix(BaseDamage.Info i_Info, ID i_ID)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("PlayerManager.GetDamage() : i_ID=" + i_ID.ToString());
                if ((int)i_ID < 4)
                {
                    DemulShooter_Plugin.OutputData.Damaged[(int)i_ID] = 1;
                }

                return true;
            }
        }
    }
}
