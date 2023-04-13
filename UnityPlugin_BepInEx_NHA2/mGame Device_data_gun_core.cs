using HarmonyLib;
using UnityEngine;
using System;

namespace UnityPlugin_BepInEx_NHA2
{
    class mGame_Device_data_gun_core
    {
        /// <summary>
        /// Sometimes, activating P2 after P2 change the mouse from P1 to P2, thus disabing P1 (why ??)
        /// Easiest thing to do is to always return true here
        /// </summary>
        [HarmonyPatch(typeof(Game_Device_data_gun_core), "device_is_can_work")]
        class device_is_can_work
        {
            static bool Prefix(Game_Device_data_gun_core __instance, int device_num, ref bool __result)
            {
                //NightHunterArcade2_Plugin.MyLogger.LogMessage("mGame_Device_data_gun_core.device_is_can_work() => player: " + __instance.mygame_player_num + ", myis_use_mouse:  " + __instance.myis_use_mouse + ", result: " + __result);
                __result = true;
                return false;
            }
        }


        /// <summary>
        /// Called by the game to get mouse coordinates to handle Guns
        /// If plugin Input is set to MOUSE, we do nothing
        /// If plugin Input is set to DemulShooter, we will replace coordinates by our own calculated values
        /// </summary>
        [HarmonyPatch(typeof(Game_Device_data_gun_core), "get_gun_pos_by_mouse")]
        class get_gun_pos_by_mouse
        {
            static bool Prefix(Game_Device_data_gun_core __instance, ref Vector3 __result)
            {
                //NightHunterArcade2_Plugin.MyLogger.LogMessage("mGame_Device_data_gun_core.get_gun_pos_by_mouse() => player: " + __instance.mygame_player_num);                    
                if (NightHunterArcade2_Plugin.Configurator.InputMode == NightHunterArcade2_Plugin.InputMode.Mouse)
                {
                    return true;
                }
                else
                {
                    //DemulShooter axis value
                    Vector3 mouse_pos = new Vector3();
                    int PlayerNum = __instance.get_player_num() - 1;
                    mouse_pos.x = BitConverter.ToInt32(NightHunterArcade2_Plugin.NHA2_Mmf.Payload, NHA2_MemoryMappedFile_Controller.INDEX_P1_INGAME_X + 8 * PlayerNum);
                    mouse_pos.y = BitConverter.ToInt32(NightHunterArcade2_Plugin.NHA2_Mmf.Payload, NHA2_MemoryMappedFile_Controller.INDEX_P1_INGAME_Y + 8 * PlayerNum);
                    //NightHunterArcade2_Plugin.MyLogger.LogMessage("mGame_Device_data_gun_core.get_gun_pos_by_mouse() => player: " + __instance.mygame_player_num + ", Value: " + mouse_pos.ToString());
                    __result = zhichi_hanshu_pos.change_mouse_positon_to_gun_pos(mouse_pos);
                    return false;
                }
            }
        }

        /// <summary>
        /// Same thing for Fire triggering
        /// </summary>
        [HarmonyPatch(typeof(Game_Device_data_gun_core), "is_gun_fire")]
        class is_gun_fire
        {
            static bool Prefix(Game_Device_data_gun_core __instance, ref bool __result)
            {
                //NightHunterArcade2_Plugin.MyLogger.LogMessage("mGame_Device_data_gun_core.is_gun_fire() => player: " + __instance.mygame_player_num);
                if (NightHunterArcade2_Plugin.Configurator.InputMode == NightHunterArcade2_Plugin.InputMode.Mouse)
                {
                    return true;
                }
                else
                {
                    //DemulShooter button value
                    __result = false;
                    int PlayerNum = __instance.get_player_num() - 1;
                    if (__instance.is_single_fire())
                    {
                        if (NightHunterArcade2_Plugin.DemulShooter_Buttons[PlayerNum].IsTriggerPressed())
                        {
                            __result = true;
                        }
                    }
                    else
                    {
                        if (NightHunterArcade2_Plugin.DemulShooter_Buttons[PlayerNum].IsTriggerDown())
                        {
                            __result = true;
                        }
                    }
                    return false;
                }
            }
        }
    }
}
