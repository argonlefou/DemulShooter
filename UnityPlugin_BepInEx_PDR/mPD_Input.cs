using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_PDR
{
    class mPD_Input
    {
        [HarmonyPatch(typeof(PD_Input), "GetAxis")]
        class GetAxis
        {
            static float _lastX = 0f;
            static float _lastY = 0f;

            static void Postfix(object[] __args, ref float __result)
            {
                AxisType _axisType = (AxisType)__args[0];

                if (PD_SaveLoad.GameSave.PlayerMovementMode == MechanicMode.classic)
                {
                    switch (_axisType)
                    {
                        case AxisType.pointerMovementHorizontal:
                            {
                                var x = Input.mousePosition.x;
                                if (x > Screen.width * 0.95f)
                                    __result = 1.4f;
                                else if (x > Screen.width * 0.90f)
                                    __result = 0.8f;
                                else if (x > Screen.width * 0.80f)
                                    __result = 0.7f;
                                else if (x < Screen.width * 0.05f)
                                    __result = -1.4f;
                                else if (x < Screen.width * 0.10f)
                                    __result = -0.8f;
                                else if (x < Screen.width * 0.20f)
                                    __result = -0.7f;
                                else if (x > _lastX)
                                    __result = 0.6f;
                                else if (x < _lastX)
                                    __result = -0.6f;
                                else
                                    __result = 0f;
                                _lastX = x;
                                break;
                            }
                        case AxisType.pointerMovementVertical:
                            {
                                var y = Input.mousePosition.y;
                                if (y > Screen.height * 0.9f)
                                    __result = 1f;
                                else if (y > Screen.height * 0.8f)
                                    __result = 0.6f;
                                else if (y < Screen.height * 0.1f)
                                    __result = -1f;
                                else if (y < Screen.height * 0.2f)
                                    __result = -0.6f;
                                else if (y > _lastY)
                                    __result = 0.4f;
                                else if (y < _lastY)
                                    __result = -0.4f;
                                else
                                    __result = 0f;
                                _lastY = y;
                                break;
                            }
                    }
                }
            }
        }
    }
}
