using HarmonyLib;

namespace UnityPlugin_BepInEx_NHA2
{
    class minput_obj_change_bullet
    {
        /// <summary>
        /// Used to map Window coordinates to UI coordinates : UI is fixed on [-960, 960] for X and [-540, 540] for Y (1920p resolution)
        /// We need to scale down the coordinates before putting it in the good range
        /// </summary>
        [HarmonyPatch(typeof(input_obj_change_bullet), "change_weapon")]
        class mchange_weapon
        {
            static bool Prefix(int player_num)
            {
                NightHunterArcade2_Plugin.MyLogger.LogMessage("minput_obj_change_bullet.change_weapon() => player_num: " + player_num.ToString());
                if (!zhichi_hanshu_gun_wheel_mark_manage.gun_is_can_fire())
                {
                    return false;
                }
                game_player game_player = zhichi_hanshu_game_player.get_game_player(player_num);
                if (!game_player.is_living())
                {
                    return false;
                }
                                
                game_player.change_weapon();

                return false;
            }
        }
    }
}
