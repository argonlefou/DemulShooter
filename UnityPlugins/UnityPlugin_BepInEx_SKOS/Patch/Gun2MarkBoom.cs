using HarmonyLib;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class Gun2MarkBoom
    {
        /// <summary>
        /// Don't know when this one is used ???
        /// </summary>
        //[HarmonyPatch(typeof(Gun2MarkBoom), "OnEnable")]
        //class OnEnable
        //{
        //    static bool Prefix(/*EffectPlayer ___Mark*/)
        //    {
        //        DemulShooter_Plugin.MyLogger.LogWarning("Gun2MarkBoom.OnEnable()");
        //        //if (!DemulShooter_Plugin.CrossHairVisibility)
        //        //{
        //        //    ___Mark.transform.localScale = new UnityEngine.Vector3();
        //        //    return false;
        //        //}
        //        return true;
        //    }
        //}
    }
}
