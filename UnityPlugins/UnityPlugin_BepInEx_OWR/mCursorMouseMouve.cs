using HarmonyLib;
using UnityEngine;
using Virtuallyz.VRShooter.IO;

namespace UnityPlugin_BepInEx_OWR
{
    class mCursorMouseMouve
    {
        [HarmonyPatch(typeof(CursorMouseMove), "MouseMove")]
        class MouseMove
        {
            static bool Prefix(CursorMouseMove __instance, ref Vector3 __result)
            {
                __result = Vector3.zero;
                return false;
            }
        }

        /// <summary>
        /// Replacing the original function where the procedure is different before Cursor Locked and not Locked
        /// Here it is replaced by our own Lock flag ans changed the relative input reading by mouse position
        /// </summary>
        [HarmonyPatch(typeof(CursorMouseMove), "Mouse2DUpdate")]
        class Mouse2DUpdate
        {
            static bool Prefix(CursorMouseMove __instance, Camera ___PCCamera, ref Vector3 ___Cursor2DPos, float ___LastDistanceFromScreen, Animator ___viewportController)
            {
                if (!___PCCamera)
                {
                    return false;
                }

                Vector3 vector6;
                if (OpWolf_Plugin.IsMouseLockedRequired)
                {
                    //Custom option to hide Crosshairs
                    if (OpWolf_Plugin.DisableCrosshair == 0)
                        __instance.Target.SetActive(true);
                    else
                        __instance.Target.SetActive(false);

                    //Vector3 MouseAxis = Input.mousePosition;
                    Vector3 MouseAxis = Vector3.zero;
                    if (__instance.name.Equals("MainController"))
                    {
                        lock (OpWolf_Plugin.MutexLocker_Inputs)
                        {
                            MouseAxis = new Vector3(OpWolf_Plugin.PluginControllers[(int)OpWolf_Plugin.PlayerType.Player1].GetAxisX(), OpWolf_Plugin.PluginControllers[0].GetAxisY(), 0);                            
                        }
                    }
                    else if (__instance.name.Equals("PlayerTwoController"))
                    {
                        lock (OpWolf_Plugin.MutexLocker_Inputs)
                        {
                            MouseAxis = new Vector3(OpWolf_Plugin.PluginControllers[(int)OpWolf_Plugin.PlayerType.Player2].GetAxisX(), OpWolf_Plugin.PluginControllers[1].GetAxisY(), 0);
                        }
                    }
                    //OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.VRShooter.IO.Mouse2DUpdate() => " + __instance.name + " data : " + MouseAxis.ToString());
                    ___Cursor2DPos.x = MouseAxis.x - (Screen.width / 2.0f);
                    ___Cursor2DPos.y = MouseAxis.y - (Screen.height / 2.0f);
                    ___Cursor2DPos.z = 1.0f;


                    Vector3 vector = new Vector3((float)Screen.width * 0.5f, (float)Screen.height * 0.5f, 0f);
                    float num = 1f;
                    float num2 = 1f - num;
                    Vector3 vector2 = vector * num;
                    Vector3 zero = Vector3.zero;
                    ___Cursor2DPos.x = Mathf.Clamp(___Cursor2DPos.x, -vector.x, vector.x);
                    ___Cursor2DPos.y = Mathf.Clamp(___Cursor2DPos.y, -vector.y, vector.y);
                    Vector3 vector4 = ___Cursor2DPos;
                    vector4.x = Mathf.Abs(vector4.x);
                    vector4.y = Mathf.Abs(vector4.y);
                    vector4 -= vector2;
                    vector4.x /= (float)Screen.width * 0.5f * num2;
                    vector4.y /= (float)Screen.height * 0.5f * num2;
                    Vector2 vector5 = new Vector2(10f, 10f);
                    if (___Cursor2DPos.x < -vector2.x)
                    {
                        zero.y = -Mathf.Lerp(0f, vector5.y, vector4.x);
                    }
                    if (___Cursor2DPos.x > vector2.x)
                    {
                        zero.y = Mathf.Lerp(0f, vector5.y, vector4.x);
                    }
                    if (___Cursor2DPos.y < -vector2.y)
                    {
                        zero.x = Mathf.Lerp(0f, vector5.x, vector4.y);
                    }
                    if (___Cursor2DPos.y > vector2.y)
                    {
                        zero.x = -Mathf.Lerp(0f, vector5.x, vector4.y);
                    }
                    vector6 = ___Cursor2DPos + vector;
                    __instance.Target.transform.position = ___PCCamera.ScreenToWorldPoint(vector6);
                    Quaternion localRotation = Quaternion.Euler(zero.x, zero.y, 0f);
                    __instance.transform.localRotation = localRotation;
                }
                else
                {
                    vector6 = Input.mousePosition;
                    if (__instance.depthPositionFixed)
                    {
                        vector6 = new Vector3(vector6.x, vector6.y, 0f);
                    }
                    __instance.Target.transform.position = ___PCCamera.ScreenToWorldPoint(vector6);
                }
                ___PCCamera.ScreenPointToRay(vector6);

                if (___viewportController)
                {
                    Vector2 vector7 = ___PCCamera.ScreenToViewportPoint(vector6);
                    ___viewportController.SetFloat("Viewport_X", vector7.x);
                    ___viewportController.SetFloat("Viewport_Y", vector7.y);
                    ___viewportController.SetBool("isRight", __instance.isRight);
                    ___viewportController.SetBool("isLongWeapon", __instance.isLongWeapon);
                }

                vector6.z = ___LastDistanceFromScreen;
                __instance.GetForceTarget().transform.position = ___PCCamera.ScreenToWorldPoint(vector6);

                return false;
            }
        }
    }
}
