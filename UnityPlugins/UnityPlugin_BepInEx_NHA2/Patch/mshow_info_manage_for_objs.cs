using HarmonyLib;

namespace UnityPlugin_BepInEx_NHA2
{
    class mshow_info_manage_for_objs
    {
        /// <summary>
        /// Removing those Keyboard shortcut that are used to display various Debug string on screen
        /// </summary>
        [HarmonyPatch(typeof(show_info_manage_for_objs), "init_show_info_obj_list")]
        class init_show_info_obj_list
        {
            static bool Prefix()
            {
                DemulShooter_Plugin.MyLogger.LogMessage("mshow_info_manage_for_objs.init_show_info_obj_list()");
                /*base.init_show_info_obj_list();
                base.add_show_info_obj(this.myshow_info_obj_wheel_value_R, KeyCode.R);
                base.add_show_info_obj(this.myshow_info_obj_reload_config, KeyCode.C);
                base.add_show_info_obj(this.myshow_info_obj_movie_and_movie_time, KeyCode.S);
                base.add_show_info_obj(this.myshow_info_obj_to_save_to_file_last_time, KeyCode.D);
                base.add_show_info_obj(this.myshow_info_obj_gun_shank, KeyCode.F);
                base.add_show_info_obj(this.myshow_info_obj_zhen, KeyCode.P);
                base.add_show_info_obj(this.myshow_info_obj_show_gun_color_light, KeyCode.L);
                base.add_show_info_obj(this.myshow_info_obj_gun_audio, KeyCode.W);
                base.add_show_info_obj(this.myshow_info_obj_debug_mem_begin, KeyCode.E);*/
                return false;
            }
        }
    }
}
