using System;
using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_RHA
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
                if (Demulshooter_Plugin.IsDemoMode)
                    return true;

                //UnityEngine.Debug.Log("mCrosshairWindow.CrosshairMove(), i_PlayerID=" + i_PlayerID.ToString() + ", i_Viewport=" + i_Viewport.ToString());
                if (SBK.SceneSingleton<GameplayCamera>.Exists())
                {
                    for (int i = 0; i < __instance.m_Crosshairs.Length; i++)
                    {
                        if (__instance.m_Crosshairs[i].m_ID == i_PlayerID && SBK.Singleton<PlayerManager>.Instance.GetPlayerByID(i_PlayerID).Alive)
                        {
                            Vector3 v = new Vector3();
                            v.x = BitConverter.ToInt32(Demulshooter_Plugin.RHA_Mmf.Payload, RHA_MemoryMappedFile_Controller.INDEX_P1_UISCREEN_X + 16 * (int)i_PlayerID);
                            v.y = BitConverter.ToInt32(Demulshooter_Plugin.RHA_Mmf.Payload, RHA_MemoryMappedFile_Controller.INDEX_P1_UISCREEN_Y + 16 * (int)i_PlayerID);
                            //UnityEngine.Debug.LogError("mCrosshairWindow.CrosshairMove() => v = " + v.ToString());                        
                            __instance.m_Crosshairs[i].m_CrosshairTr.anchoredPosition = v;

                            break;
                        }
                    }
                    return false;
                }
                return false;
            }
        }
    }
}
