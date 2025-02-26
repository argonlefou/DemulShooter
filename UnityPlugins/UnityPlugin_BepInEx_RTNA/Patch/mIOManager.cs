using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_RTNA
{
    class mIOManager
    {
        /// <summary>
        /// Force not using IO board
        /// </summary>
        [HarmonyPatch(typeof(IOManager), "set_m_UseRio")]
        class set_m_UseRio
        {
            static bool Prefix(ref bool value)
            {
                NerfArcade_Plugin.MyLogger.LogMessage("IOManager.set_m_UseRio() : value=" + value);
                value = false;
                return false;
            }
        }

        
        /// <summary>
        /// Update buttons based on Keyboard / Custom input data
        /// That function is called when m_UseRio is set to false
        /// </summary>
        [HarmonyPatch(typeof(IOManager), "UpdateInput_Unity")]
        class UpdateInput_Unity
        {
            static bool Prefix(DigitalInputData[] ___m_DigitalInputData, IOManager __instance)
            {
                //NerfArcade_Plugin.MyLogger.LogMessage("IOManager.ButtonNewlyPressed() : inputIn=" + inputIn.ToString());
                //Mouse + Keyboard controls   
                if (!NerfArcade_Plugin.EnableInputHack)
                {
                    for (int i = 0; i < ___m_DigitalInputData.Length; i++)
                    {
                        ___m_DigitalInputData[i].switchCntLast = ___m_DigitalInputData[i].switchCnt;

                        uint iKeyPressed = 0;
                        switch (i)
                        {
                            case (int)(int)DigitalGameInput.Coin1: iKeyPressed = Input.GetKey(KeyCode.Alpha5) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.Coin2: iKeyPressed = Input.GetKey(KeyCode.Alpha6) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.DBV: iKeyPressed = Input.GetKey(KeyCode.Alpha7) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.GunShoulder1: iKeyPressed = Input.GetMouseButton(2) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.GunShoulder2: iKeyPressed = Input.GetMouseButton(2) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.GunTrigger1: iKeyPressed = Input.GetMouseButton(0) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.GunTrigger2: iKeyPressed = Input.GetMouseButton(1) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.Service: iKeyPressed = Input.GetKey(KeyCode.Alpha0) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.Start1: iKeyPressed = Input.GetKey(KeyCode.Alpha1) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.Start2: iKeyPressed = Input.GetKey(KeyCode.Alpha2) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.Test: iKeyPressed = Input.GetKey(KeyCode.Alpha9) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.VolumeDown: iKeyPressed = Input.GetKey(KeyCode.DownArrow) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.VolumeUp: iKeyPressed = Input.GetKey(KeyCode.UpArrow) ? (uint)1 : (uint)0; break;
                            default: break;
                        }
                        ___m_DigitalInputData[i].switchCnt = iKeyPressed;

                        if (___m_DigitalInputData[i].switchCntLast == ___m_DigitalInputData[i].switchCnt)
                        {
                            ___m_DigitalInputData[i].time += Time.deltaTime;
                        }
                        else
                        {
                            ___m_DigitalInputData[i].time = 0f;
                        }
                    }

                    Vector3 mouse = Input.mousePosition;
                    float x = mouse.x / (float)Screen.width;
                    float y = mouse.y / (float)Screen.height;

                    MethodInfo mi = __instance.GetType().GetMethod("SetAnalogValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (mi != null)
                    {
                        mi.Invoke(__instance, new object[] { AnalogGameInput.Gun1X, x });
                        mi.Invoke(__instance, new object[] { AnalogGameInput.Gun1Y, y });
                        mi.Invoke(__instance, new object[] { AnalogGameInput.Gun2X, x });
                        mi.Invoke(__instance, new object[] { AnalogGameInput.Gun2Y, y });
                    }
                }
                //DemulShooter controls
                else
                {
                    for (int i = 0; i < ___m_DigitalInputData.Length; i++)
                    {
                        ___m_DigitalInputData[i].switchCntLast = ___m_DigitalInputData[i].switchCnt;

                        uint iKeyPressed = 0;
                        switch (i)
                        {
                            case (int)(int)DigitalGameInput.Coin1: iKeyPressed = Input.GetKey(KeyCode.Alpha5) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.Coin2: iKeyPressed = Input.GetKey(KeyCode.Alpha6) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.DBV: iKeyPressed = Input.GetKey(KeyCode.Alpha7) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.GunShoulder1: iKeyPressed = NerfArcade_Plugin.PluginControllers[0].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Action) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.GunShoulder2: iKeyPressed = NerfArcade_Plugin.PluginControllers[1].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Action) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.GunTrigger1: iKeyPressed = NerfArcade_Plugin.PluginControllers[0].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Trigger) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.GunTrigger2: iKeyPressed = NerfArcade_Plugin.PluginControllers[1].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Trigger) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.Service: iKeyPressed = Input.GetKey(KeyCode.Alpha0) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.Start1: iKeyPressed = Input.GetKey(KeyCode.Alpha1) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.Start2: iKeyPressed = Input.GetKey(KeyCode.Alpha2) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.Test: iKeyPressed = Input.GetKey(KeyCode.Alpha9) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.VolumeDown: iKeyPressed = Input.GetKey(KeyCode.DownArrow) ? (uint)1 : (uint)0; break;
                            case (int)DigitalGameInput.VolumeUp: iKeyPressed = Input.GetKey(KeyCode.UpArrow) ? (uint)1 : (uint)0; break;
                            default: break;
                        }
                        ___m_DigitalInputData[i].switchCnt = iKeyPressed;

                        if (___m_DigitalInputData[i].switchCntLast == ___m_DigitalInputData[i].switchCnt)
                        {
                            ___m_DigitalInputData[i].time += Time.deltaTime;
                        }
                        else
                        {
                            ___m_DigitalInputData[i].time = 0f;
                        }
                    }

                    MethodInfo mi = __instance.GetType().GetMethod("SetAnalogValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (mi != null)
                    {
                        mi.Invoke(__instance, new object[] { AnalogGameInput.Gun1X, NerfArcade_Plugin.PluginControllers[0].Axis_X });
                        mi.Invoke(__instance, new object[] { AnalogGameInput.Gun1Y, NerfArcade_Plugin.PluginControllers[0].Axis_Y });
                        mi.Invoke(__instance, new object[] { AnalogGameInput.Gun2X, NerfArcade_Plugin.PluginControllers[1].Axis_X });
                        mi.Invoke(__instance, new object[] { AnalogGameInput.Gun2Y, NerfArcade_Plugin.PluginControllers[1].Axis_Y });
                    }
                }

                return false;                
            }
        }
    }
}
