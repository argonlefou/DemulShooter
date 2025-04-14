using HarmonyLib;

namespace UnityPlugin_BepInEx_NHA2
{
    class mdog_check_new
    {

        /// <summary>
        /// Dongle bypass
        /// </summary>
        [HarmonyPatch(typeof(dog_check_new), "is_dog_ok_in_start")]
        class is_dog_ok_in_start
        {
            static bool Prefix(ref bool __result)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("mdog_check_new.is_dog_ok_in_start()");
                dog_check_new.myis_has_dog_in_update = true;
                __result = dog_check_new.is_dog_ok_in_update();
                return false;
            }
        }

        /// <summary>
        /// Dongle bypass
        /// </summary>
        [HarmonyPatch(typeof(dog_check_new), "is_dog_ok_in_update")]
        class is_dog_ok_in_update
        {
            static bool Prefix(ref bool __result)
            {
                //NightHunterArcadePlugin.MyLogger.LogMessage("mdog_check_new.is_dog_ok_in_update()");
                __result = true;
                return false;
            }
        }
    }
}
