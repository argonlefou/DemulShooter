using Boys.Hasp;
using HarmonyLib;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mUIIdleDemo
    {
        /// <summary>
        /// Force LOCK screen disable ? Not sure to be working...
        /// </summary>
        [HarmonyPatch(typeof(HaspTimerBuilder), "EnableLock", MethodType.Getter)]
        class Get_EnableLock
        {
            static bool Prefix(ref bool __result)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("HaspTimerBuilder.EnableLock");
                __result = false;
                return false;
            }
        }
    }
}
