using HarmonyLib;
using UnityEngine;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    class mPlayer
    {
        /// <summary>
        /// Removing crosshair (old, replaced)
        /// </summary>
        /*[HarmonyPatch(typeof(Player), "UpdatePlayerUI")]
        class UpdatePlayerUI
        {
            static bool Prefix(Player __instance, PlayerMode ___m_playerMode)
            {
                if (UIManager.Instance != null)
                {
                    if (___m_playerMode >= PlayerMode.PM_GAME && ___m_playerMode <= PlayerMode.PM_GAME_OVER)
                    {
                        if (__instance.TwoHandGun != null)
                        {
                            if (DemulShooter_Plugin.CrossHairVisibility)
                                EventCenter.GenerateEvent<PlayerGunID, Vector3>(127, __instance.TwoHandGun.PGid, __instance.TwoHandGun.GetForesightPos());
                            else
                            {
                                if (___m_playerMode == PlayerMode.PM_GAME)
                                    EventCenter.GenerateEvent<PlayerGunID, Vector3>(127, __instance.TwoHandGun.PGid, new Vector3(-1.0f, -1.0f));
                                else
                                    EventCenter.GenerateEvent<PlayerGunID, Vector3>(127, __instance.TwoHandGun.PGid, __instance.TwoHandGun.GetForesightPos());
                            }
                        }
                        else if (__instance.PlayerGunL != null && __instance.PlayerGunR != null)
                        {
                            if (DemulShooter_Plugin.CrossHairVisibility)
                            {
                                EventCenter.GenerateEvent<PlayerGunID, Vector3>(127, __instance.PlayerGunL.PGid, __instance.PlayerGunL.GetForesightPos());
                                EventCenter.GenerateEvent<PlayerGunID, Vector3>(127, __instance.PlayerGunR.PGid, __instance.PlayerGunR.GetForesightPos());
                            }
                            else
                            {
                                if (___m_playerMode == PlayerMode.PM_GAME)
                                {
                                    EventCenter.GenerateEvent<PlayerGunID, Vector3>(127, __instance.PlayerGunL.PGid, new Vector3(-1.0f, -1.0f));
                                    EventCenter.GenerateEvent<PlayerGunID, Vector3>(127, __instance.PlayerGunR.PGid, new Vector3(-1.0f, -1.0f));
                                }
                                else
                                {
                                    EventCenter.GenerateEvent<PlayerGunID, Vector3>(127, __instance.PlayerGunL.PGid, __instance.PlayerGunL.GetForesightPos());
                                    EventCenter.GenerateEvent<PlayerGunID, Vector3>(127, __instance.PlayerGunR.PGid, __instance.PlayerGunR.GetForesightPos());
                                }
                            }
                        }
                    }
                    if (__instance.TwoHandGun != null)
                    {
                        EventCenter.GenerateEvent<PID, int, int, int>(23, __instance.m_pid, __instance.TwoHandGun.SurplusBulletNum, __instance.TwoHandGun.SurplusBulletNum, __instance.TwoHandGun.MaxBulletNum);
                    }
                    else if (__instance.PlayerGunL != null && __instance.PlayerGunR != null)
                    {
                        EventCenter.GenerateEvent<PID, int, int, int>(23, __instance.m_pid, __instance.PlayerGunL.SurplusBulletNum, __instance.PlayerGunR.SurplusBulletNum, __instance.PlayerGunL.MaxBulletNum);
                    }
                }
                return false;
            }
        }*/
    }
}
