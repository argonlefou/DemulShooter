using System;
using HarmonyLib;
using UnityEngine;

namespace RabbidsHollywood_BepInEx_DemulShooter_Plugin
{
    class mGunDetection
    {
        /// <summary>
        /// This funtion is used to run system command, mostly for gun flashing BUT ALSO TO REBOOT COMPUTER (!!) if the fullscreen is disabled
        /// So...Nope !
        /// </summary>
        [HarmonyPatch(typeof(GunDetection), "ExternCall")]
        class ExternCall
        {
            static bool Prefix(string ao_Command, string ao_Arguments, string[] ao_Outputs = null)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("mGunDetection.ExternCall() : ao_Command=" + ao_Command + ", ao_Arguments=" + ao_Arguments + ", ao_Outputs=" + ao_Outputs);
                return false;
            }
        }
    }
}
