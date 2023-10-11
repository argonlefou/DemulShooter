using HarmonyLib;
using Virtuallyz.VRShooter;

namespace UnityPlugin_BepInEx_OWR
{
    class mGameManager
    {
        /// <summary>
        /// On LevelStopped (quit or died) => set Ammo and Life to 0
        /// </summary>
        [HarmonyPatch(typeof(GameManager), "OnLevelActStopped")]
        class Update
        {
            static bool Prefix()
            {
                OpWolf_Plugin.MyLogger.LogWarning("Virtuallyz.VRShooter.GameManager.OnLevelActStopped()");
                lock (OpWolf_Plugin.MutexLocker_Outputs)
                {
                    OpWolf_Plugin.OutputData.P1_Life = 0;
                    OpWolf_Plugin.OutputData.P1_Ammo = 0;
                    OpWolf_Plugin.OutputData.P2_Life = 0;
                    OpWolf_Plugin.OutputData.P2_Ammo = 0;
                }
                OpWolf_Plugin.SendOutputs();
                return true;
            }

        }
    }
}
