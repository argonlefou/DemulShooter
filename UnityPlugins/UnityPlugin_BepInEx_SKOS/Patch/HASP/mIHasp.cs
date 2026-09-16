using HarmonyLib;
using HASP;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mIHasp
    {
        /// <summary>
        /// Force HASP code to be valid long enough at every moment to not display the protect code window
        /// </summary>
        [HarmonyPatch(typeof(IHasp), "GetRemainTime")]
        class GetRemainTime
        {
            static bool Prefix(bool IsDay, ref int __result)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("HASP.IHasp.GetRemainTime() : IsDay=" + IsDay);
                if (IsDay)
                    __result = 365;
                else
                    __result = 15768000; //365 days * 12h *60mn * 60sec
               
                return false;
            }
        }
    }
}
