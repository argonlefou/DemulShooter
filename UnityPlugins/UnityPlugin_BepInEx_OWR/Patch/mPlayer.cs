using HarmonyLib;
using Virtuallyz.VRShooter.Characters.Players;

namespace OperationWolf_BepInEx_DemulShooter_Plugin
{
    class mPlayer
    {
        /// <summary>
        /// Intercept dammage event to send output to Demulshooter
        /// </summary>
        [HarmonyPatch(typeof(Player), "OnDamageTaken")]
        class OnDamageTaken
        {
            static bool Prefix(Player __instance)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("Virtuallyz.VRShooter.Characters.Players.OnDamageTaken()");
                lock (DemulShooter_Plugin.MutexLocker_Outputs)
                {
                    DemulShooter_Plugin.OutputData.P1_Damage = 1;
                    DemulShooter_Plugin.OutputData.P2_Damage = 1;
                    DemulShooter_Plugin.SendOutputs(); 
                }
                DemulShooter_Plugin.SendOutputs();
                return true;
            }
        }
    }
}
