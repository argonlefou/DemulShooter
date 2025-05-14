using HarmonyLib;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    class mPlayerGunBase
    {
        /// <summary>
        /// Intercept event to create Recoil
        /// </summary>
        [HarmonyPatch(typeof(PlayerGunBase), "Shoot")]
        class Shoot
        {
            static bool Prefix(PlayerGunBase __instance)
            {
                if (__instance.Pid == PID.PID_ONE)
                    DemulShooter_Plugin.OutputData.Recoil[0] = 1;
                else if (__instance.Pid == PID.PID_TWO)
                    DemulShooter_Plugin.OutputData.Recoil[1] = 1;

                return true;
            }
        }

        /// <summary>
        /// Get ammo count
        /// </summary>
        [HarmonyPatch(typeof(PlayerGunBase), "Update")]
        class Update
        {
            static bool Prefix(PlayerGunBase __instance)
            {
                if (__instance.Pid == PID.PID_ONE)
                {
                    if (__instance.PGid == PlayerGunID.PGID_P1GL)
                        DemulShooter_Plugin.OutputData.AmmoGunL[0] = (uint)__instance.Magazine.BulletNum;
                    else if (__instance.PGid == PlayerGunID.PGID_P1GR)
                        DemulShooter_Plugin.OutputData.AmmoGunR[0] = (uint)__instance.Magazine.BulletNum;

                }
                else if (__instance.Pid == PID.PID_TWO)
                {
                    if (__instance.PGid == PlayerGunID.PGID_P2GL)
                        DemulShooter_Plugin.OutputData.AmmoGunL[1] = (uint)__instance.Magazine.BulletNum;
                    else if (__instance.PGid == PlayerGunID.PGID_P2GR)
                        DemulShooter_Plugin.OutputData.AmmoGunR[1] = (uint)__instance.Magazine.BulletNum;
                }

                return true;
            }
        }

        /// <summary>
        /// If Screen res has changed, there's an offset between the 2D reticle position (matching the screen size) and the real bullet position (matching original size)
        /// We can fix it here
        /// </summary>
        [HarmonyPatch(typeof(PlayerGunBase), "CalcShotSight")]
        class CalcShotSight
        {
            static bool Prefix(PlayerGunBase __instance, ref UnityEngine.Vector2 pos)
            {
                if (DemulShooter_Plugin.ChangeResolution)
                {
                    pos.x = pos.x * (DemulShooter_Plugin.ORIGINAL_WIDTH / (float)DemulShooter_Plugin.ScreenWidth);
                    pos.y = pos.y * (DemulShooter_Plugin.ORIGINAL_WIDTH / (float)DemulShooter_Plugin.ScreenWidth);
                }
                return true;
            }
        }
        
    }
}
