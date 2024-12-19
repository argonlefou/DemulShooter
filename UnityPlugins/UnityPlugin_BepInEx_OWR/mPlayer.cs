using HarmonyLib;
using Virtuallyz.VRShooter.Characters.Players;

namespace UnityPlugin_BepInEx_OWR
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
                OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.VRShooter.Characters.Players.OnDamageTaken()");
                lock (OpWolf_Plugin.MutexLocker_Outputs)
                {
                    OpWolf_Plugin.OutputData.P1_Damage = 1;
                    OpWolf_Plugin.OutputData.P2_Damage = 1;
                    OpWolf_Plugin.SendOutputs(); 
                }
                OpWolf_Plugin.SendOutputs();
                return true;
            }
        }
    }
}
