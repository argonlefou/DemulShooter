using HarmonyLib;

namespace UnityPlugin_BepInEx_NHA2
{
    /// <summary>
    /// Find when a player is hit for custom outputs
    /// </summary>
    class mgame_player_is_hit
    {
        [HarmonyPatch(typeof(game_player_is_hit), "player_is_hit")]
        class player_is_hit
        {
            static bool Prefix(game_base game_hit_obj1, game_player_is_hit __instance)
            {
                /*NightHunterArcade2_Plugin.MyLogger.LogWarning("mgame_player_is_hit.player_is_hit() => userNum: " + __instance.get_user_num()  + ", life: " + __instance.get_life());
                if (!__instance.player_status_is_can_be_hit())
                {
                    return true;
                }
                else
                {
                    NightHunterArcade2_Plugin.MyLogger.LogWarning("mgame_player_is_hit.player_is_hit() => Dammage ! Old Life: " + __instance.get_curr_blood());
                    NightHunterArcade2_Plugin.MyLogger.LogWarning("mgame_player_is_hit.player_is_hit() => bulletCount: " + __instance.get_bullet_count());
                }*/
                return true;
            }
        }

    }
}
