using HarmonyLib;

namespace WildWestShootout_BepInEx_DemulShooter_Plugin
{
    /// <summary>
    /// This class is using calls from a DLL not present in the game release, causing multiple ERROR
    /// "Blanking" calls from this class allow the Guns ti run "normally"
    /// </summary>
    class mBaseCamera
    {
        [HarmonyPatch(typeof(MVSDK.BaseCamera), "CaptureThreadProc")]
        class CaptureThreadProc
        {
            static bool Prefix()
            {
                UnityEngine.Debug.Log("mBaseCamera.CaptureThreadProc()");
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "Close")]
        class Close
        {
            static bool Prefix()
            {
                UnityEngine.Debug.Log("mBaseCamera.Close()");
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "GetFrameData")]
        class GetFrameData
        {
            static bool Prefix(ref bool __result)
            {
                UnityEngine.Debug.Log("mBaseCamera.GetFrameData()");
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "GetImageResolution")]
        class GetImageResolution
        {
            static bool Prefix()
            {
                UnityEngine.Debug.Log("mBaseCamera.GetImageResolution()");
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "InitCamera")]
        class InitCamera
        {
            static bool Prefix(ref bool __result)
            {
                UnityEngine.Debug.Log("mBaseCamera.InitCamera()");
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "LoadDefaultConfig")]
        class LoadDefaultConfig
        {
            static bool Prefix()
            {
                UnityEngine.Debug.Log("mBaseCamera.LoadDefaultConfig()");
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "Pause")]
        class Pause
        {
            static bool Prefix()
            {
                UnityEngine.Debug.Log("mBaseCamera.Pause()");
                return false;
            }

        }
        [HarmonyPatch(typeof(MVSDK.BaseCamera), "Play")]
        class Play
        {
            static bool Prefix(ref bool __result)
            {
                UnityEngine.Debug.Log("mBaseCamera.Play()");
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "SetExposureState")]
        class SetExposureState
        {
            static bool Prefix()
            {
                UnityEngine.Debug.Log("mBaseCamera.SetExposureState()");
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "SetImageResolution", new[] { typeof(int), typeof(int), typeof(int), typeof(int) })]
        class SetImageResolution
        {
            static bool Prefix(ref MVSDK.CameraSdkStatus __result)
            {
                UnityEngine.Debug.Log("mBaseCamera.SetImageResolution()");
                __result = MVSDK.CameraSdkStatus.CAMERA_STATUS_SUCCESS;
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "SetMonochrome")]
        class SetMonochrome
        {
            static bool Prefix()
            {
                UnityEngine.Debug.Log("mBaseCamera.SetMonochrome()");
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera), "UpdateExposure")]
        class UpdateExposure
        {
            static bool Prefix()
            {
                UnityEngine.Debug.Log("mBaseCamera.UpdateExposure()");
                return false;
            }
        }

        [HarmonyPatch(typeof(MVSDK.BaseCamera))]
        [HarmonyPatch("AnalogGain", MethodType.Setter)]
        class set_AnalogGain
        {
            static bool Prefix(float value, float ___m_fAnalogGain)
            {
                UnityEngine.Debug.Log("mBaseCamera.set_AnalogGain()");
                ___m_fAnalogGain = value;
                return false;
            }
        }
    }
}
