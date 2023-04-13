using HarmonyLib;

namespace UnityPlugin_BepInEx_NHA2
{
	class mgame_device_sub_gun
	{
        /// <summary>
        /// Called for each shoot, for recoil)
        /// </summary>
        [HarmonyPatch(typeof(game_device_sub_gun), "fire_obj_list")]
        class fire_obj_list
        {
            static bool Prefix(game_device_sub_gun __instance)
            {
                int PlayerNum = __instance.get_player_num();
                //NightHunterArcade2_Plugin.MyLogger.LogMessage("mgame_device_sub_gun.fire_obj_list() => Player: " + PlayerNum);
                NightHunterArcade2_Plugin.Players_RecoilEnabled[PlayerNum - 1] = true;                
                return true;
            }
        }
	}
}
