using System;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;


namespace UnityPlugin_BepInEx_NHA2
{
    /// <summary>
    /// Change Keyboard KEYS
    /// </summary>
    class minput_manage
    {
        [HarmonyPatch(typeof(input_manage), "init_input_obj_list")]
        class init_input_obj_list
        {
            static bool Prefix(input_manage __instance)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("minput_manage.init_input_obj_list()");

                input_manage my_base = __instance;
                my_base.add_input_obj(new input_obj_start_game(KeyCode.Alpha1, 1));
                my_base.add_input_obj(new input_obj_start_game(KeyCode.Alpha2, 2));
                my_base.add_input_obj(new input_obj_pass_movie(KeyCode.Space));
                my_base.add_input_obj(new input_obj_pass_movie(KeyCode.Alpha1));
                my_base.add_input_obj(new input_obj_pass_movie(KeyCode.Alpha2));
                my_base.add_input_obj(new input_obj_goto_houtai(KeyCode.Alpha0));
                my_base.add_input_obj(new input_obj_insert_coin(KeyCode.Alpha5, 1));
                my_base.add_input_obj(new input_obj_insert_coin(KeyCode.Alpha6, 2));
                /*my_base.add_input_obj(new input_obj_change_gun(KeyCode.Keypad1, 1));                
                my_base.add_input_obj(new input_obj_change_gun(KeyCode.Keypad2, 2));
                my_base.add_input_obj(new input_obj_xiaoqiang(KeyCode.Keypad3, 1));
                my_base.add_input_obj(new input_obj_xiaoqiang(KeyCode.Keypad4, 2));
                 my_base.add_input_obj(new input_obj_change_input_way(KeyCode.Y));*/

                if (DemulShooter_Plugin.Configurator.InputMode == DemulShooter_Plugin.InputMode.Mouse)
                {
                    my_base.add_input_obj(new input_obj_change_bullet(KeyCode.Mouse1, 1));
                    my_base.add_input_obj(new input_obj_change_bullet(KeyCode.Mouse1, 2));
                    my_base.add_input_obj(new input_obj_big_power(KeyCode.Mouse2, 1));
                    my_base.add_input_obj(new input_obj_big_power(KeyCode.Mouse2, 2));
                }
                else
                {
                    //DemulShooter is handling
                }
                return false;                
            }
        }
    }
}
