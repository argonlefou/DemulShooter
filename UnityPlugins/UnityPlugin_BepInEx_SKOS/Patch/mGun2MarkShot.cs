using HarmonyLib;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mGun2MarkShot
    {
        /// <summary>
        /// Remove crosshair when Gun is displaying "shooting" crosshair
        /// </summary>
        [HarmonyPatch(typeof(Gun2MarkShot), "OnEnable")]
        class OnEnable
        {
            static bool Prefix(Gun2MarkShot __instance)
            {
                DemulShooter_Plugin.MyLogger.LogWarning("Gun2MarkShot.OnEnable()");
                if (!DemulShooter_Plugin.CrossHairVisibility)
                {
                    __instance.transform.localScale = new UnityEngine.Vector3();
                    return false;
                }
                return true;
            }
        }
    }
}
