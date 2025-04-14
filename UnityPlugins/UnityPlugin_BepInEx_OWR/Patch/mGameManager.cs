using HarmonyLib;
using Virtuallyz.VRShooter;

namespace OperationWolf_BepInEx_DemulShooter_Plugin
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
                DemulShooter_Plugin.MyLogger.LogWarning("Virtuallyz.VRShooter.GameManager.OnLevelActStopped()");
                lock (DemulShooter_Plugin.MutexLocker_Outputs)
                {
                    DemulShooter_Plugin.OutputData.P1_Life = 0;
                    DemulShooter_Plugin.OutputData.P1_Ammo = 0;
                    DemulShooter_Plugin.OutputData.P2_Life = 0;
                    DemulShooter_Plugin.OutputData.P2_Ammo = 0;
                }
                DemulShooter_Plugin.SendOutputs();
                return true;
            }

        }
    }
}
