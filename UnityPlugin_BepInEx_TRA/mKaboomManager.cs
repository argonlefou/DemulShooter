using System;
using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_TRA
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
                UnityEngine.Debug.Log("mKaboomManager.StartMotorPwm() : MotorId=" + MotorId.ToString() + ", un8PwmDutyCycle=" + un8PwmDutyCycle + ", iMsDuration=" + iMsDuration);
                if (MotorId.ToString().Contains("P1"))
                    Demulshooter_Plugin.TRA_Mmf.Payload[TRA_MemoryMappedFile_Controller.INDEX_P1_MOTOR] = 1;
                else if (MotorId.ToString().Contains("P2"))
                    Demulshooter_Plugin.TRA_Mmf.Payload[TRA_MemoryMappedFile_Controller.INDEX_P2_MOTOR] = 1;
                else if (MotorId.ToString().Contains("P3"))
                    Demulshooter_Plugin.TRA_Mmf.Payload[TRA_MemoryMappedFile_Controller.INDEX_P3_MOTOR] = 1;
                else if (MotorId.ToString().Contains("P4"))
                    Demulshooter_Plugin.TRA_Mmf.Payload[TRA_MemoryMappedFile_Controller.INDEX_P4_MOTOR] = 1;

                Demulshooter_Plugin.TRA_Mmf.Writeall();

                return true;
            }
        }

        /// <summary>
        /// Use this funtion to generate DemulShooter custom recoil output
        /// </summary>
        [HarmonyPatch(typeof(KaboomManager), "StopMotorPwm")]
        class StopMotorPwm
        {
            static bool Prefix(KaboomOutput.MotorPwmId MotorId)
            {
                //UnityEngine.Debug.Log("mKaboomManager.StartMotorPwm() : MotorId=" + MotorId.ToString());
                if (MotorId.ToString().Contains("P1"))
                    Demulshooter_Plugin.TRA_Mmf.Payload[TRA_MemoryMappedFile_Controller.INDEX_P1_MOTOR] = 0;
                else if (MotorId.ToString().Contains("P2"))
                    Demulshooter_Plugin.TRA_Mmf.Payload[TRA_MemoryMappedFile_Controller.INDEX_P2_MOTOR] = 0;
                else if (MotorId.ToString().Contains("P3"))
                    Demulshooter_Plugin.TRA_Mmf.Payload[TRA_MemoryMappedFile_Controller.INDEX_P3_MOTOR] = 0;
                else if (MotorId.ToString().Contains("P4"))
                    Demulshooter_Plugin.TRA_Mmf.Payload[TRA_MemoryMappedFile_Controller.INDEX_P4_MOTOR] = 0;

                Demulshooter_Plugin.TRA_Mmf.Writeall();

                return true;
            }
        }
    }
}
