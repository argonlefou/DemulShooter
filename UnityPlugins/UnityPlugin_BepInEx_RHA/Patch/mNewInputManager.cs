using System;
using HarmonyLib;
using UnityEngine;

namespace RabbidsHollywood_BepInEx_DemulShooter_Plugin
{
    class mNewInputManager
    {
        /// <summary>
        /// The original function is lopp-called when gun are not detected to mouse-control P1 and P2
        /// By overriding it, we can inject data for all 4 players inside
        /// </summary>
        [HarmonyPatch(typeof(NewInputManager), "MouseControl")]
        class MouseControl
        {
            static bool Prefix (bool ___m_AllInputEnable, bool ___m_AreCrosshairsEnabled, bool[] ___m_HoldTrigger, NewInputManager __instance)
            {
                if (DemulShooter_Plugin.EnableInputHack)
                {
                    //UnityEngine.Debug.Log("mNewInputManager.MouseControl()");     
                    if (___m_AllInputEnable)
                    {
                        Vector3 i_Arg = new Vector3();

                        for (int i = 0; i < 4; i++)
                        {
                            if (SBK.Singleton<ArcadeManager>.Instance.GunEnabled(i))
                            {
                                i_Arg.x = DemulShooter_Plugin.PluginControllers[i].Axis_X;
                                i_Arg.y = DemulShooter_Plugin.PluginControllers[i].Axis_Y;
                                EventManager<Vector3, ID>.TriggerEvent(NewInputManager.BaseMessage.PlayerCrosshairMove.ToString(), i_Arg, (ID)i);
                            }

                            if (DemulShooter_Plugin.PluginControllers[i].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Trigger))
                            {
                                //Rha_Plugin.MyLogger.LogMessage("mNewInputManager.MouseControl(" + i + ") => Trigger Pushed");
                                EventManager<Vector3, ID>.TriggerEvent(NewInputManager.BaseMessage.PlayerPressTrigger.ToString(), i_Arg, (ID)i);
                                if (___m_AreCrosshairsEnabled)
                                {
                                    EventManager<Vector3, ID>.TriggerEvent(NewInputManager.BaseMessage.PlayerShoot.ToString(), i_Arg, (ID)i);
                                }
                                else if (!___m_HoldTrigger[i])
                                {
                                    ___m_HoldTrigger[i] = true;
                                    if (SBK.Singleton<ArcadeGameplayManager>.Instance.CurrentState == ArcadeGameplayManager.State.PlayState)
                                    {
                                        SBK.Singleton<SBK.Audio.AudioManager>.Instance.Play(SBK.Audio.AudioID_Global.ui_cantshoot, null);
                                    }
                                }
                            }
                            else if (DemulShooter_Plugin.PluginControllers[i].GetButtonUp(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Trigger))
                            {
                                //Rha_Plugin.MyLogger.LogMessage("mNewInputManager.MouseControl(" + i + ") => Trigger Released");
                                if (___m_AreCrosshairsEnabled)
                                {
                                    EventManager<ID>.TriggerEvent(NewInputManager.BaseMessage.PlayerTriggerRelease.ToString(), (ID)i);                                    
                                }
                                ___m_HoldTrigger[i] = false;
                            }
                        }
                    }
                    return false;
                }
                return true;
            }
        }        
    }
}
