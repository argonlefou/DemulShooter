using System;
using HarmonyLib;
using UnityEngine;

namespace RabbidsHollywood_BepInEx_DemulShooter_Plugin
{
    class mKaboomManager
    {
        /// <summary>
        /// Use this funtion to generate DemulShooter custom recoil output
        /// </summary>
        [HarmonyPatch(typeof(KaboomManager), "StartMotorPwm")]
        class StartMotorPwm
        {
            static bool Prefix(KaboomOutput.MotorPwmId MotorId, byte un8PwmDutyCycle, ushort iMsDuration = 144)
            {
                //UnityEngine.Debug.Log("mKaboomManager.StartMotorPwm() : MotorId=" + MotorId.ToString() + ", un8PwmDutyCycle=" + un8PwmDutyCycle + ", iMsDuration=" + iMsDuration);         
                if (MotorId.ToString().Contains("P1"))
                    DemulShooter_Plugin.OutputData.Recoil[0] = 1;
                else if (MotorId.ToString().Contains("P2"))
                    DemulShooter_Plugin.OutputData.Recoil[1] = 1;
                else if (MotorId.ToString().Contains("P3"))
                    DemulShooter_Plugin.OutputData.Recoil[2] = 1;
                else if (MotorId.ToString().Contains("P4"))
                    DemulShooter_Plugin.OutputData.Recoil[3] = 1;
                
                return true;
            }
        }
    }
}
