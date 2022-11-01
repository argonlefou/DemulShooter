using System;
using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_RHA
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
                //UnityEngine.Debug.Log("mNewInputManager.MouseControl()");     
                if (___m_AllInputEnable)
                {
                    Vector3 i_Arg = new Vector3();

                    for (int i = 0; i < 4; i++)
                    {
                        if (SBK.Singleton<ArcadeManager>.Instance.GunEnabled(i))
                        {
                            i_Arg.x = BitConverter.ToSingle(Demulshooter_Plugin.RHA_Mmf.Payload, RHA_MemoryMappedFile_Controller.INDEX_P1_INGAME_X + 16 * i);
                            i_Arg.y = BitConverter.ToSingle(Demulshooter_Plugin.RHA_Mmf.Payload, RHA_MemoryMappedFile_Controller.INDEX_P1_INGAME_Y + 16 * i);
                            EventManager<Vector3, ID>.TriggerEvent(NewInputManager.BaseMessage.PlayerCrosshairMove.ToString(), i_Arg, (ID)i /*ID.One*/);
                            //UnityEngine.Debug.LogError("mNewInputManager.MouseControl(" + i + ") => i_Arg = " + i_Arg.ToString());
                        }

                        //For Triggers, DemulShooter will output "1" for ButtonUp and "2" for ButtonDown. 0 When no inputs
                        byte TriggerState = Demulshooter_Plugin.RHA_Mmf.Payload[RHA_MemoryMappedFile_Controller.INDEX_P1_TRIGGER + 4 * i];
                        if (TriggerState == 2)
                        {
                            EventManager<Vector3, ID>.TriggerEvent(NewInputManager.BaseMessage.PlayerPressTrigger.ToString(), i_Arg, (ID)i);
                            if (___m_AreCrosshairsEnabled)
                            {
                                EventManager<Vector3, ID>.TriggerEvent(NewInputManager.BaseMessage.PlayerShoot.ToString(), i_Arg, (ID)i);
                                //UnityEngine.Debug.LogError("mNewInputManager.MouseControl(" + i + ") => Trigger Pushed");
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
                        else if (TriggerState == 1)
                        {
                            if (___m_AreCrosshairsEnabled)
                            {
                                EventManager<ID>.TriggerEvent(NewInputManager.BaseMessage.PlayerTriggerRelease.ToString(), (ID)i);
                                //UnityEngine.Debug.LogError("mNewInputManager.MouseControl(" + i + ") => Trigger Released");
                            }
                            ___m_HoldTrigger[i] = false;
                        }
                    }                    
                }               
                return false;
            }
        }        
    }
}
