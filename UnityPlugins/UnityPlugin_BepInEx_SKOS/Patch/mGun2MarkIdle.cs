using HarmonyLib;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mGun2MarkIdle
    {
        /// <summary>
        /// Keeping the crossahir when shot is forbidden ('X' crosshair)
        /// </summary>
        [HarmonyPatch(typeof(Gun2MarkIdle), "OnEnable")]
        class OnEnable
        {
            static bool Prefix(Gun2MarkIdle __instance)
            {
                DemulShooter_Plugin.MyLogger.LogWarning("Gun2MarkIdle.OnEnable()");
                //if (!DemulShooter_Plugin.CrossHairVisibility)
                //{
                //    __instance.transform.localScale = new UnityEngine.Vector3();
                //    return false;
                //}
                return true;
            }
        }
    }
}
