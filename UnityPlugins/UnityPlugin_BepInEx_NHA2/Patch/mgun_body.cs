using HarmonyLib;
using UnityEngine;
namespace UnityPlugin_BepInEx_NHA2
{
    class mgun_body
    {
        /// <summary>
        /// Removing 3D gun model on screen
        /// </summary>
        [HarmonyPatch(typeof(gun_body), "late_update_work_for_gun_num")]
        class late_update_work_for_gun_num
        {
            static bool Prefix(int num, gun_body __instance)
            {
                if (DemulShooter_Plugin.Configurator.RemoveGuns)
                {
                    foreach (Component c in __instance.gameObject.GetComponentsInChildren<Component>())
                    {
                        //NightHunterArcadePlugin.MyLogger.LogMessage("  +Component: " + c.ToString() + " [GameObject=" + c.gameObject.ToString() + "]");
                        if (/*c.name.StartsWith("Gun_A") /*|| */c.name.Equals("golden_eagle") /*|| c.name.StartsWith("FX_gun")*/)
                            c.gameObject.SetActive(false);
                    }
                }

                //Does not work for player 2 crosshair ???
                /*if (NightHunterArcadePlugin.Configurator.RemoveCrosshair)
                {
                    __instance.get_gun_body_target(1).SetActive(false);     //P1
                    __instance.get_gun_body_target(2).SetActive(false);     //P2
                }*/
                return true;
            }
        }
    }
}
