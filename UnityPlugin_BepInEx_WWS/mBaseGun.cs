using System;
using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_WWS
{
    class mBaseGun
    {
        /// <summary>
        /// Returning shoot point coordinates for InGame
        /// Coordinates are in range [-1.0f ; 1.0f];
        /// </summary>
        [HarmonyPatch(typeof(MVSDK.BaseGun), "GetGunPos")]
        class GetGunPos
        {
            static bool Prefix(MVSDK.BaseGun __instance, ref Vector2 __result, int gun_id)
            {
                if (gun_id >= 0 && gun_id < __instance.MaxGun)
                {
                    int r = Demulshooter_Plugin.WWS_Mmf.ReadAll();
                    if (r == 0)
                    {
                        Vector2 vAxis = new Vector2();
                        vAxis.x = BitConverter.ToSingle(Demulshooter_Plugin.WWS_Mmf.Payload, WWS_MemoryMappedFile_Controller.INDEX_P1_INGAME_X + (16 * gun_id));
                        vAxis.y = BitConverter.ToSingle(Demulshooter_Plugin.WWS_Mmf.Payload, WWS_MemoryMappedFile_Controller.INDEX_P1_INGAME_Y + (16 * gun_id)); 
                        __result = vAxis;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("mBaseGun.GetgunPos() => DemulShooter MMF read error : " + r.ToString());
                        __result = MVSDK.BaseGun.InvalidPoint;
                    }
                }
                else
                     __result = MVSDK.BaseGun.InvalidPoint;

                return false;
            }
        }

        /// <summary>
        /// Returning shoot point coordinates for InGame
        /// Coordinates are in range [WindowWidth ; WindowHeight]
        /// UnityEngine.Screen is returning Window size
        /// </summary>
        [HarmonyPatch(typeof(MVSDK.BaseGun), "GetGunRealPixPos")]
        class GetGunRealPixPos
        {
            static bool Prefix(MVSDK.BaseGun __instance, ref Vector2 __result, int gun_id)
            {
                if (gun_id >= 0 && gun_id < __instance.MaxGun)
                {
                    int r = Demulshooter_Plugin.WWS_Mmf.ReadAll();
                    if (r == 0)
                    {
                        Vector2 vAxis = new Vector2();
                        vAxis.x = BitConverter.ToSingle(Demulshooter_Plugin.WWS_Mmf.Payload, WWS_MemoryMappedFile_Controller.INDEX_P1_INGAME_X + (16 * gun_id)) * (float)UnityEngine.Screen.width;
                        vAxis.y = BitConverter.ToSingle(Demulshooter_Plugin.WWS_Mmf.Payload, WWS_MemoryMappedFile_Controller.INDEX_P1_INGAME_Y + (16 * gun_id)) * (float)UnityEngine.Screen.height;                      
                        __result = vAxis;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("mBaseGun.GetGunRealPixPos() => DemulShooter MMF read error : " + r.ToString());
                        __result = MVSDK.BaseGun.InvalidPoint;
                    }
                }
                else
                    __result = MVSDK.BaseGun.InvalidPoint;

                return false;
            }
        }
    }
}
