using HarmonyLib;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    /*class mGame
    {
        /// <summary>
        /// Intercepting Damaged event
        /// </summary>
        [HarmonyPatch(typeof(UGGameShell.Game), "PlayerDamaged")]
        class PlayerDamaged
        {
            static bool Prefix(PID pid)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("UGGameShell.Game.PlayerDamaged() : pid=" + pid.ToString());
                switch (pid)
                {
                    case PID.PID_ONE:
                        DemulShooter_Plugin.OutputData.Damaged[0] = 1;
                        break;
                    case PID.PID_TWO:
                        DemulShooter_Plugin.OutputData.Damaged[1] = 1;
                        break;
                    case PID.PID_BOTH:
                        DemulShooter_Plugin.OutputData.Damaged[0] = 1;
                        DemulShooter_Plugin.OutputData.Damaged[1] = 1;
                        break;
                    default:
                        break;
                }
                
                return true;
            }
        }

        /// <summary>
        /// Intercepting HP refresh event
        /// </summary>
        [HarmonyPatch(typeof(UGGameShell.Game), "UpdateHP")]
        class UpdateHP
        {
            static bool Prefix(PID pid, int hp)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("UGGameShell.Game.UpdateHP() : pid=" + pid.ToString());
                switch (pid)
                {
                    case PID.PID_ONE:
                        DemulShooter_Plugin.OutputData.Life[0] = (uint)hp;
                        break;
                    case PID.PID_TWO:
                        DemulShooter_Plugin.OutputData.Life[1] = (uint)hp;
                        break;
                    default:
                        break;
                }

                return true;
            }
        }
    }*/
}
