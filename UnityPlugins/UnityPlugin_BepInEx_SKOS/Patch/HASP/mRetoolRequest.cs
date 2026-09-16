using HarmonyLib;
using HASP;
using UnityEngine;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mRetoolRequest
    {
        /// <summary>
        /// Remove initial Identifier CODE protect window
        /// </summary>
        [HarmonyPatch(typeof(RetoolRequest), "OnEnable")]
        class OnEnable
        {
            static bool Prefix(RetoolRequest __instance, Camera ___Camera, RectTransform ___Canvas, RetoolRequest.ExternParm ___Parm)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("HASP.RetoolRequest.OnEnable()");
                ___Camera.gameObject.SetActive(false);
                ___Canvas.gameObject.SetActive(false);
                ___Parm.ExitCB();
                UnityEngine.Object.Destroy(__instance.gameObject);
                return false;
            }
        }
    }
}
