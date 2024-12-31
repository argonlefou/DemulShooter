using System;
using HarmonyLib;
using MRCQ;

namespace UnityPlugin_BepInEx_MarsSortie.Patch
{
    class mHardware
    {
        /// <summary>
        /// axisName is "x01" ... "x06" and "y01" ... "y06"
        /// X value is 0.0f (left) to 1.0f (right)
        /// Y value is 0.0f (Bottom) to 1.0f (top)
        /// </summary>
        [HarmonyPatch(typeof(Hardware), "GetInputAxis", new Type[] { typeof(string) })]
        class GetInputAxis
        {
            static void Postfix(string axisName, ref float __result)
            {
                if (MarsSortie_BepInEx_Plugin.EnableInputHack)
                {
                    switch (axisName)
                    {
                        case "x01":
                            __result = MarsSortie_BepInEx_Plugin.PluginControllers[0].Axis_X; break;
                        case "x02":
                            __result = MarsSortie_BepInEx_Plugin.PluginControllers[1].Axis_X; break;
                        case "x03":
                            __result = MarsSortie_BepInEx_Plugin.PluginControllers[2].Axis_X; break;
                        case "x04":
                            __result = MarsSortie_BepInEx_Plugin.PluginControllers[3].Axis_X; break;

                        case "y01":
                            __result = MarsSortie_BepInEx_Plugin.PluginControllers[0].Axis_Y; break;
                        case "y02":
                            __result = MarsSortie_BepInEx_Plugin.PluginControllers[1].Axis_Y; break;
                        case "y03":
                            __result = MarsSortie_BepInEx_Plugin.PluginControllers[2].Axis_Y; break;
                        case "y04":
                            __result = MarsSortie_BepInEx_Plugin.PluginControllers[3].Axis_Y; break;

                        default:
                            __result = 0.0f;
                            break;
                    }
                }
            }
        }


        /// <summary>
        /// Update buttons with our own values by sending signals
        /// </summary>
        [HarmonyPatch(typeof(Hardware), "Update")]
        class Update
        {
            static bool Prefix()
            {
                if (MarsSortie_BepInEx_Plugin.EnableInputHack)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        //Trigger input
                        bool flag = false;
                        if (MarsSortie_BepInEx_Plugin.PluginControllers[i].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Trigger))
                            flag = true;

                        MRCQ.Hardware.SetInputKey(i, "startGameKey", flag);
                        MRCQ.Hardware.SetInputKey(i, "fire", flag);

                        //Switch Weapon
                        flag = false;
                        if (MarsSortie_BepInEx_Plugin.PluginControllers[i].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Action))
                            flag = true;
                        MRCQ.Hardware.SetInputKey("switchWeapon0" + ((i+1).ToString()), flag);
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Hardware), "SetOutputKey", new Type[] { typeof(string), typeof(bool) })]
        class SetOutputKey
        {
            static void Prefix(string keyName, bool keyStatus)
            {
                //MarsSortie_BepInEx_Plugin.MyLogger.LogMessage("MRCQ.Hardware.SetOutputKey(): keyName=" + keyName + " ,keyS=" + keyStatus);
                switch (keyName)
                {
                    case "flashlight": MarsSortie_BepInEx_Plugin.OutputData.Flashlight = keyStatus ? (byte)1 : (byte)0; break;
                    case "start01": MarsSortie_BepInEx_Plugin.OutputData.P1_Playing = keyStatus ? (byte)1 : (byte)0; break;
                    case "start02": MarsSortie_BepInEx_Plugin.OutputData.P2_Playing = keyStatus ? (byte)1 : (byte)0; break;
                    case "start03": MarsSortie_BepInEx_Plugin.OutputData.P3_Playing = keyStatus ? (byte)1 : (byte)0; break;
                    case "start04": MarsSortie_BepInEx_Plugin.OutputData.P4_Playing = keyStatus ? (byte)1 : (byte)0; break;
                    case "fire01": MarsSortie_BepInEx_Plugin.OutputData.P1_Recoil = keyStatus ? (byte)1 : (byte)0; break;
                    case "fire02": MarsSortie_BepInEx_Plugin.OutputData.P2_Recoil = keyStatus ? (byte)1 : (byte)0; break;
                    case "fire03": MarsSortie_BepInEx_Plugin.OutputData.P3_Recoil = keyStatus ? (byte)1 : (byte)0; break;
                    case "fire04": MarsSortie_BepInEx_Plugin.OutputData.P4_Recoil = keyStatus ? (byte)1 : (byte)0; break;
                    default: break;
                }

            }
        }
    }
}
