using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_NHA2
{
    class mgame_mark_3d_obj
    {
        /// <summary>
        /// Remove Crosshair
        /// </summary>
        [HarmonyPatch(typeof(game_mark_3d_obj), "set_mark_obj")]
        class set_mark_obj
        {
            static bool Prefix(int num, game_mark_3d_obj __instance)
            {
                if (DemulShooter_Plugin.Configurator.RemoveCrosshair)
                {
                    //NightHunterArcadePlugin.MyLogger.LogMessage("mgame_mark_3d_obj.set_mark_obj() => num: " + num);
                    __instance.mymark_num = num;
                    for (int i = 0; i < __instance.mymark_obj_list.Count; i++)
                    {
                        GameObject gameObject = __instance.mymark_obj_list[i];
                        gameObject.SetActive(false);
                    }
                    return false;
                }

                return true;
            }
        }
    }
}
