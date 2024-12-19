using HarmonyLib;

namespace UnityPlugin_BepInEx_NHA2
{
    class mconfig_gun
    {
       
        /// <summary>
        /// Force MOUSE use all the time
        /// </summary>
        [HarmonyPatch(typeof(config_gun), "get_gun_input_way")]
        class get_gun_input_way
        {
            static bool Prefix(ref config_gun.GUN_INPUT_WAY __result)
            {
                //NightHunterArcadePlugin.MyLogger.LogMessage("mconfig_gun.get_gun_input_way()");
                __result = config_gun.GUN_INPUT_WAY.MOUSE;
                return false;
            }
        } 
    }
}
