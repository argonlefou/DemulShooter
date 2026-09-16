using HarmonyLib;
using HASP;
using UnityEngine;

namespace BepInEx_DemulShooter_Plugin.Patch.HASP
{
    //[HarmonyPatch(typeof(RemainRequest), "OnEnable")]
    //class OnEnable
    //{
    //    static bool Prefix(RemainRequest __instance, RemainRequest.ExternParm ___Parm)
    //    {
    //        DemulShooter_Plugin.MyLogger.LogMessage("HASP.RemainRequest.OnEnable()");
    //        ___Parm.ExitCB();
    //        UnityEngine.Object.Destroy(__instance.gameObject);
    //        return false;
    //    }
    //}

    //[HarmonyPatch(typeof(RemainRequest), "CreateUI")]
    //class CreateUI
    //{
    //    static bool Prefix(RemainRequest.ExternParm parm)
    //    {
    //        DemulShooter_Plugin.MyLogger.LogMessage("HASP.RemainRequest.CreateUI()");
    //        DemulShooter_Plugin.PrintStackTrace();
    //        return true;
    //    }
    //}
}
