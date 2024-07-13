using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_PBX
{
    class mInputWrapper
    {
        /// <summary>
        /// Force Credits key Handling
        /// </summary>
        [HarmonyPatch(typeof(InputWrapper), "isCreditAdded")]
        class isCreditAdded
        {
            static bool Prefix(ref bool __result)
            {
                //PointBlankX_Plugin.MyLogger.LogMessage("InputWrapper.isCreditAdded()");
                __result = UnityEngine.Input.GetKeyDown(PointBlankX_Plugin.Credits_KeyCode);
                return false;
            }
        }

        /// <summary>
        /// Force Start Keys handling
        /// </summary>
        [HarmonyPatch(typeof(InputWrapper), "isStartPressed")]
        class isStartPressed
        {
            static bool Prefix(string playerName, ref bool __result)
            {
                __result = false;
                //PointBlankX_Plugin.MyLogger.LogWarning("InputWrapper.isStartPressed() => " + playerName);
                if (playerName.Equals("P1") && Input.GetKeyDown(PointBlankX_Plugin.P1_Start_KeyCode))
                {
                    PointBlankX_Plugin.MyLogger.LogWarning("InputWrapper.isStartPressed() => P1 Start Detected");
                    __result = true;
                    return false;
                }

                if (playerName.Equals("P2") && Input.GetKeyDown(PointBlankX_Plugin.P2_Start_KeyCode))
                {
                    PointBlankX_Plugin.MyLogger.LogWarning("InputWrapper.isStartPressed() => P2 start Detected");
                    __result = true;
                    return false;
                }

                return false;
            }
        }

        /// <summary>
        /// Feeding gun trigger with DemulShooter
        /// </summary>
        [HarmonyPatch(typeof(InputWrapper), "isShooting")]
        class isShooting
        {
            static bool Prefix(string playerName, ref int ___p1ShootFrameCount, ref int ___p2ShootFrameCount, ref float ___p1ShootHoldTime, ref float ___p2ShootHoldTime, float ___rapidFireShootInterval, ref bool __result, bool allowRapidFire = false)
            {
                if (PointBlankX_Plugin.EnableInputHack)
                {
                    //PointBlankX_Plugin.MyLogger.LogWarning("InputWrapper.isShooting() => Player: " + playerName + "allowRapidFire: " + allowRapidFire);
                    if (playerName == "P1")
                    {
                        if (PointBlankX_Plugin.P1_Trigger_ButtonState == 0)
                        {
                            ___p1ShootFrameCount = 0;
                            ___p1ShootHoldTime = 0f;
                            __result = false;
                            return false;
                        }
                        ___p1ShootFrameCount++;
                        ___p1ShootHoldTime += Time.deltaTime;
                        if (allowRapidFire)
                        {
                            bool flag3 = false;
                            if (___p1ShootHoldTime > ___rapidFireShootInterval)
                            {
                                flag3 = true;
                                ___p1ShootHoldTime -= ___rapidFireShootInterval;
                            }
                            __result = ___p1ShootFrameCount == 1 || flag3;
                            return false;
                        }
                        __result = ___p1ShootFrameCount == 1;
                        return false;
                    }
                    else
                    {
                        if (PointBlankX_Plugin.P2_Trigger_ButtonState == 0)
                        {
                            ___p2ShootFrameCount = 0;
                            ___p2ShootHoldTime = 0f;
                            __result = false;
                            return false;
                        }
                        ___p2ShootFrameCount++;
                        ___p2ShootHoldTime += Time.deltaTime;
                        if (allowRapidFire)
                        {
                            bool flag4 = false;
                            if (___p2ShootHoldTime > ___rapidFireShootInterval)
                            {
                                flag4 = true;
                                ___p2ShootHoldTime -= ___rapidFireShootInterval;
                            }
                            __result = ___p2ShootFrameCount == 1 || flag4;
                            return false;
                        }
                        __result = ___p2ShootFrameCount == 1;
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Feeding gun position with DemulShooter
        /// </summary>
        [HarmonyPatch(typeof(InputWrapper), "getPositionOnScreen")]
        class getPositionOnScreen
        {
            static bool Prefix(string playerName, ref Vector2 __result)
            {
                if (PointBlankX_Plugin.EnableInputHack)
                {
                    //PointBlankX_Plugin.MyLogger.LogWarning("InputWrapper.getPositionOnScreen() => " + playerName + " : " + playerName.ToString());
                    if (playerName.Equals("P1"))
                    {
                        __result = PointBlankX_Plugin.P1_Axis;
                    }
                    else if (playerName.Equals("P2"))
                    {
                        __result = PointBlankX_Plugin.P2_Axis;
                    }
                    return false;
                }
                else
                {
                    return true;
                }

            }
        }
        
    }
}
