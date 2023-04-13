using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_NHA2
{
    class mzzp_houtai_manage_check_control
    {
        /// <summary>
        /// Replacing Keyboard keys used in SERVICE menu
        /// </summary>
        [HarmonyPatch(typeof(zzp_houtai_manage_check_control), "check_for_zzp_houtai_control")]
        class check_for_zzp_houtai_control
        {
            static bool Prefix(zzp_houtai_manage_check_control __instance)
            {
                //NightHunterArcade2_Plugin.MyLogger.LogMessage("mzzp_houtai_manage_check_control.check_for_zzp_houtai_control()");
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    __instance.add_key_work();
                }
                if (Input.GetKeyDown(KeyCode.UpArrow))  // <---Not working, funtion null
                {
                    __instance.del_key_work();
                }
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    __instance.queding_work();
                }

                return false;
            }
        }        
    }
}
