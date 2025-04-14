using System;
using HarmonyLib;
using UnityEngine;

namespace RabbidsHollywood_BepInEx_DemulShooter_Plugin
{
    class mCrosshairWindow
    {
        [HarmonyPatch(typeof(CrosshairWindow), "CrosshairMove")]
        class CrosshairMove
        {
            /// <summary>
            /// Original function is calculating the pixel position of the Crosshair with what seems to be a fixed range [1920x1080]
            /// This is creating an offset between the float real position of the aim [0, 1] and the crosshair drawing
            /// </summary>
            /// <returns></returns>
            static bool Prefix(Vector3 i_Viewport, ID i_PlayerID, CrosshairWindow __instance)
            {
                //If in Attractmode, we let the game handle the crosshair without messing with it
                if (DemulShooter_Plugin.IsDemoMode)
                    return true;

                if (DemulShooter_Plugin.EnableInputHack)
                {
                    //UnityEngine.Debug.Log("mCrosshairWindow.CrosshairMove(), i_PlayerID=" + i_PlayerID.ToString() + ", i_Viewport=" + i_Viewport.ToString());
                    if (SBK.SceneSingleton<GameplayCamera>.Exists())
                    {
                        for (int i = 0; i < __instance.m_Crosshairs.Length; i++)
                        {
                            if (__instance.m_Crosshairs[i].m_ID == i_PlayerID && SBK.Singleton<PlayerManager>.Instance.GetPlayerByID(i_PlayerID).Alive)
                            {
                                float fRatio = (float)Screen.width / (float)Screen.height;
                                float fMaxY = 1080.0f;
                                float fMaxX = fRatio * fMaxY;
                                Vector3 v = new Vector3();
                                v.x = DemulShooter_Plugin.PluginControllers[(int)i_PlayerID].Axis_X * fMaxX;
                                v.y = DemulShooter_Plugin.PluginControllers[(int)i_PlayerID].Axis_Y * fMaxY;
                                //UnityEngine.Debug.LogError("mCrosshairWindow.CrosshairMove() => v = " + v.ToString());                        
                                __instance.m_Crosshairs[i].m_CrosshairTr.anchoredPosition = v;

                                break;
                            }
                        }
                        return false;
                    }
                    return false;
                }
                return true;
            }

            /// <summary>
            /// Using the postfix to hide crosshair from screen if option is activated
            /// </summary>
            /// <returns></returns>
            static void Postfix(Vector3 i_Viewport, ID i_PlayerID, CrosshairWindow __instance)
            {
                //If in Attractmode, we let the game handle the crosshair without messing with it
                if (DemulShooter_Plugin.IsDemoMode || SBK.Singleton<ArcadeGameplayManager>.Instance.CurrentState == ArcadeGameplayManager.State.ChooseWorld)
                    return;

                if (!DemulShooter_Plugin.CrossHairVisibility)
                {
                    for (int i = 0; i < __instance.m_Crosshairs.Length; i++)
                    {
                        __instance.m_Crosshairs[i].m_CrosshairTr.anchoredPosition = new Vector2(-100.0f, -100.0f);
                    }
                }
            }
        }
    }
}
