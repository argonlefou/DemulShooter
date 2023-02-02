using System;
using HarmonyLib;
using UnityEngine;
using SBK;

namespace UnityPlugin_BepInEx_TRA
{
    class mInputManager
    {
        [HarmonyPatch(typeof(InputManager), "MouseControl")]
        class MouseControl
        {
            static bool Prefix(bool ___m_AreCrosshairsEnabled, bool[] ___m_HoldTrigger, ID[] ___m_IDResolution)
            {
                Vector3 i_Arg = new Vector3();
                Player[] playerList = Singleton<PlayerManager>.Instance.m_PlayerList;

                if (!Singleton<GameplayManager>.Instance.IsDemo)
                {
                    for (int i = 0; i < playerList.Length; i++)
                    {
                        if (i == 0)
                        {
                            if ((Singleton<ArcadeManager>.Instance.GunEnabled(i) && playerList[i].IsPlaying()) || Singleton<WindowManager>.Instance.GetWindow(WindowID.ID.MainMenuWindow))
                            {
                                i_Arg.x = BitConverter.ToSingle(Demulshooter_Plugin.TRA_Mmf.Payload, TRA_MemoryMappedFile_Controller.INDEX_P1_INGAME_X + 16 * i);
                                i_Arg.y = BitConverter.ToSingle(Demulshooter_Plugin.TRA_Mmf.Payload, TRA_MemoryMappedFile_Controller.INDEX_P1_INGAME_Y + 16 * i);
                                Singleton<PlayerManager>.Instance.PlayerMoveCrosshair(i_Arg, ID.One);
                            }
                        }
                        else
                        {
                            if (Singleton<ArcadeManager>.Instance.GunEnabled(i) && playerList[i].IsPlaying())
                            {
                                i_Arg.x = BitConverter.ToSingle(Demulshooter_Plugin.TRA_Mmf.Payload, TRA_MemoryMappedFile_Controller.INDEX_P1_INGAME_X + 16 * i);
                                i_Arg.y = BitConverter.ToSingle(Demulshooter_Plugin.TRA_Mmf.Payload, TRA_MemoryMappedFile_Controller.INDEX_P1_INGAME_Y + 16 * i);
                                Singleton<PlayerManager>.Instance.PlayerMoveCrosshair(i_Arg, (ID)i);
                            }
                        }
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    if (Singleton<ArcadeManager>.Instance.GunEnabled(i))
                    {
                        i_Arg.x = BitConverter.ToSingle(Demulshooter_Plugin.TRA_Mmf.Payload, TRA_MemoryMappedFile_Controller.INDEX_P1_INGAME_X + 16 * i);
                        i_Arg.y = BitConverter.ToSingle(Demulshooter_Plugin.TRA_Mmf.Payload, TRA_MemoryMappedFile_Controller.INDEX_P1_INGAME_Y + 16 * i);

                        //For Triggers, DemulShooter will output "1" for ButtonUp and "2" for ButtonDown. 0 When no inputs
                        byte TriggerState = Demulshooter_Plugin.TRA_Mmf.Payload[TRA_MemoryMappedFile_Controller.INDEX_P1_TRIGGER + 4 * i];
                        byte ReloadState = Demulshooter_Plugin.TRA_Mmf.Payload[TRA_MemoryMappedFile_Controller.INDEX_P1_RELOAD + 4 * i];
                        if (TriggerState == 2 && Singleton<PlayerManager>.Instance.GetPlayerByID((ID)i).Alive)
                        {
                            Singleton<PlayerManager>.Instance.PlayerPressTrigger(i_Arg, (ID)i);
                            if (___m_AreCrosshairsEnabled)
                            {
                                Singleton<PlayerManager>.Instance.PlayerShoot(i_Arg, (ID)i, ___m_HoldTrigger[i], true);
                                if (!___m_HoldTrigger[i])
                                {
                                    ___m_HoldTrigger[i] = true;
                                }
                            }
                            else if (!___m_HoldTrigger[i])
                            {
                                ___m_HoldTrigger[i] = true;
                                if (Singleton<GameplayManager>.Instance.CurrentState == GameplayManager.State.PlayState && !Singleton<TRA.Arcade.Manager.VideoPlaybackManager>.Instance.IsPlaying)
                                {
                                    Singleton<PlayerManager>.Instance.PlayPlayerSound(SBK.Audio.AudioID_Global.player_cantshoot, ___m_IDResolution[i]);
                                }
                            }
                        }
                        if (TriggerState == 1)
                        {
                            if (___m_AreCrosshairsEnabled)
                            {
                                Singleton<PlayerManager>.Instance.PlayerTriggerRelease((ID)i);
                            }
                            ___m_HoldTrigger[i] = false;
                        }
                        if (ReloadState == 1)
                        {
                            Singleton<PlayerManager>.Instance.PlayerReload((ID)i);
                        }
                        if (!___m_HoldTrigger[i])
                        {
                            Singleton<PlayerManager>.Instance.StopHoldingFire(ID.One);
                        }
                    }
                }
       
                return false;
            }
        } 
    }
}
