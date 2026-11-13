using HarmonyLib;

namespace BepInEx_DemulShooter_Plugin
{
    class mCR_WeaponHolder
    {
        /// <summary>
        /// Called by the weapon's OnFired event, only when a bullet is really shot (ammo is used)
        /// Used to generate our own "Recoil" output
        /// </summary>
        [HarmonyPatch(typeof(CR_WeaponHolder), "onWeaponFired")]
        class onWeaponFired
        {
            static void Postfix(CR_WeaponHolder __instance)
            {
                if (__instance.Owner != null)
                {
                    //DemulShooter_Plugin.MyLogger.LogMessage("CR_WeaponHolder.onWeaponFired() => " + __instance.Owner.Type.ToString());
                    DemulShooter_Plugin.OutputData.Recoil[__instance.Owner.Id] = 1;
                }
            }
        }
    }
}
