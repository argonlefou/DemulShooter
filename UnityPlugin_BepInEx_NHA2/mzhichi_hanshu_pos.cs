using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_NHA2
{
    class mzhichi_hanshu_pos
    {
        /// <summary>
        /// Used to map Window coordinates to UI coordinates : UI is fixed on [-960, 960] for X and [-540, 540] for Y (1920p resolution)
        /// We need to scale down the coordinates before putting it in the good range
        /// </summary>
        [HarmonyPatch(typeof(zhichi_hanshu_pos), "change_to_min")]
        class change_to_min
        {
            static bool Prefix(Vector3 vec_input, ref Vector3 __result)
            {
                //NightHunterArcadePlugin.MyLogger.LogMessage("mzhichi_hanshu_pos.change_to_min() => mouse_pos: " + vec_input.ToString());
                __result = vec_input;
                __result.x = (vec_input.x * zhichi_hanshu_pos.PIXEL_WIDTH / NightHunterArcade2_Plugin.Configurator.ResolutionWidth) - (zhichi_hanshu_pos.PIXEL_WIDTH / 2);
                __result.y = (vec_input.y * zhichi_hanshu_pos.PIXEL_HEIGHT / NightHunterArcade2_Plugin.Configurator.ResolutionHeight) - (zhichi_hanshu_pos.PIXEL_HEIGHT / 2);
                return false;
            }
        }
        
    }
}
