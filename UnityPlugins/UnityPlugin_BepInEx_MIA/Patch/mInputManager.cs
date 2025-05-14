using HarmonyLib;
using UnityEngine;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    class mInputManager
    {
        [HarmonyPatch(typeof(InputManager), "Start")]
        class Start
        {
            static void Postfix(InputManager __instance, InputMode[] ___m_inputMode)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("InputManager.Start(): " + ___m_inputMode[0].ToString() + ", " + ___m_inputMode[1].ToString());
            }
        }

        /// <summary>
        /// Set both player to mouse mode
        /// By default, without shell, it would be AUTOPLAY mode
        /// </summary>
        [HarmonyPatch(typeof(InputManager), "ChangeInputMode")]
        class ChangeInputMode
        {
            static bool Prefix(InputManager __instance, ref InputMode[] ___m_inputMode)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("InputManager.ChangeInputMode(): " + ___m_inputMode[0].ToString() + ", " + ___m_inputMode[1].ToString());
                ___m_inputMode[0] = InputMode.MOUSE;
                ___m_inputMode[1] = InputMode.MOUSE;                
                return true;
            }
        }

        /// <summary>
        /// Replacing BUTTON data
        /// </summary>
        [HarmonyPatch(typeof(InputManager), "GetGameButtonOn")]
        class GetGameButtonOn
        {
            static bool Prefix(InputType it, ref bool __result)
            {
                switch (it)
                {
                    case InputType.P1L:
                        __result = DemulShooter_Plugin.PluginControllers[0].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.TriggerLeft);
                        break;
                    case InputType.P1R:
                        __result = DemulShooter_Plugin.PluginControllers[0].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.TriggerRight);
                        break;
                    case InputType.P2L:
                        __result = DemulShooter_Plugin.PluginControllers[1].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.TriggerLeft);
                        break;
                    case InputType.P2R:
                        __result = DemulShooter_Plugin.PluginControllers[1].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.TriggerRight);
                        break;
                    case InputType.P1START:
                        __result = DemulShooter_Plugin.PluginControllers[0].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Start);
                        break;
                    case InputType.P2START:
                        __result = DemulShooter_Plugin.PluginControllers[1].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Start);
                        break;
                    default:
                        __result = false;
                        break;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(InputManager), "GetGameButtonPress")]
        class GetGameButtonPress
        {
            static bool Prefix(InputType it, ref bool __result)
            {
                switch (it)
                {
                    case InputType.P1L:
                        __result = DemulShooter_Plugin.PluginControllers[0].GetButtonDown(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.TriggerLeft);
                        break;
                    case InputType.P1R:
                        __result = DemulShooter_Plugin.PluginControllers[0].GetButtonDown(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.TriggerRight);
                        break;
                    case InputType.P2L:
                        __result = DemulShooter_Plugin.PluginControllers[1].GetButtonDown(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.TriggerLeft);
                        break;
                    case InputType.P2R:
                        __result = DemulShooter_Plugin.PluginControllers[1].GetButtonDown(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.TriggerRight);
                        break;
                    case InputType.P1START:
                        __result = DemulShooter_Plugin.PluginControllers[0].GetButtonDown(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Start);
                        break;
                    case InputType.P2START:
                        __result = DemulShooter_Plugin.PluginControllers[1].GetButtonDown(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Start);
                        break;
                    default:
                        __result = false;
                        break;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(InputManager), "GetGameButtonRelease")]
        class GetGameButtonRelease
        {
            static bool Prefix(InputType it, ref bool __result)
            {
                switch (it)
                {
                    case InputType.P1L:
                        __result = DemulShooter_Plugin.PluginControllers[0].GetButtonUp(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.TriggerLeft);
                        break;
                    case InputType.P1R:
                        __result = DemulShooter_Plugin.PluginControllers[0].GetButtonUp(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.TriggerRight);
                        break;
                    case InputType.P2L:
                        __result = DemulShooter_Plugin.PluginControllers[1].GetButtonUp(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.TriggerLeft);
                        break;
                    case InputType.P2R:
                        __result = DemulShooter_Plugin.PluginControllers[1].GetButtonUp(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.TriggerRight);
                        break;
                    case InputType.P1START:
                        __result = DemulShooter_Plugin.PluginControllers[0].GetButtonUp(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Start);
                        break;
                    case InputType.P2START:
                        __result = DemulShooter_Plugin.PluginControllers[1].GetButtonUp(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Start);
                        break;
                    default:
                        __result = false;
                        break;
                }
                return false;
            }
        }

        /// <summary>
        /// Replacing Axis data
        /// </summary>
        [HarmonyPatch(typeof(InputManager), "GetPosition")]
        class GetPosition
        {
            static bool Prefix(PID pid, ref Vector3 __result)
            {
                if (pid == PID.PID_ONE)
                {
                    __result = DemulShooter_Plugin.PluginControllers[0].GetAimingPosition();
                }
                else if (pid == PID.PID_TWO)
                {
                    __result = DemulShooter_Plugin.PluginControllers[1].GetAimingPosition();
                }
                return false;
            }
        }
         
        
    }
}
