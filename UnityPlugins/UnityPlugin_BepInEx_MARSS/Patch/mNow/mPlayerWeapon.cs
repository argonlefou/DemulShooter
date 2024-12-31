using System;
using HarmonyLib;
using Now;

namespace UnityPlugin_BepInEx_MarsSortie.Patch.mNow
{
    class mPlayerWeapon
    {
        /*[HarmonyPatch(typeof(Now.PlayerWeapon), "Fire")]
        class Fire
        {   static void Postfix(bool __result, PlayerWeapon __instance)
            {
                byte bRecoil = 0;
                if (__result)
                    bRecoil = 1;

                switch (__instance.PlayerIndex)
                {
                    case 0:
                        MarsSortie_BepInEx_Plugin.OutputData.P1_Recoil = bRecoil; break;
                    case 1:
                        MarsSortie_BepInEx_Plugin.OutputData.P2_Recoil = bRecoil; break;
                    case 2:
                        MarsSortie_BepInEx_Plugin.OutputData.P3_Recoil = bRecoil; break;
                    case 3:
                        MarsSortie_BepInEx_Plugin.OutputData.P3_Recoil = bRecoil; break;

                    default: break;
                }
            }
        }

        [HarmonyPatch(typeof(Now.PlayerWeapon), "Update")]
        class Update
        {
            static void Postfix(PlayerWeapon __instance)
            {
                switch (__instance.PlayerIndex)
                {
                    case 0:
                        MarsSortie_BepInEx_Plugin.OutputData.P1_Ammo = (UInt16)__instance.ammo; break;
                    case 1:
                        MarsSortie_BepInEx_Plugin.OutputData.P2_Ammo = (UInt16)__instance.ammo; break;
                    case 2:
                        MarsSortie_BepInEx_Plugin.OutputData.P3_Ammo = (UInt16)__instance.ammo; break;
                    case 3:
                        MarsSortie_BepInEx_Plugin.OutputData.P4_Ammo = (UInt16)__instance.ammo; break;

                    default: break;
                }
            }
        }*/
    }
}
