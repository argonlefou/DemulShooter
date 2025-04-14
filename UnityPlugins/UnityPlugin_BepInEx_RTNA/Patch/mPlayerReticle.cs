using HarmonyLib;
using UnityEngine;

namespace NerfArcade_BepInEx_DemulShooter_Plugin
{
    class mPlayerReticle
    {
        /// <summary>
        /// Remove laser and reticle ??
        /// </summary>
        [HarmonyPatch(typeof(PlayerReticle), "LateUpdate")]
        class LateUpdate
        {
            static bool Prefix(ref float ___m_LaserStartWidth, Animator[] ___m_ReticleAnimators, GunState ___m_CurrentGunState, PlayerInfo ___m_PlayerInfo, int ___m_PlayerIndex)
            {
                if (!DemulShooter_Plugin.CrossHairVisibility)
                {
                    //Removing reticle by changing the animator scale to 0
                    if (___m_PlayerInfo.m_playerState == PlayerState.NameEntry && SingletonMonoBehaviour<UI_Manager>.Instance.NameEntry_ShouldShowReticle(___m_PlayerIndex))
                    {
                        ___m_ReticleAnimators[(int)___m_CurrentGunState].transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                    }
                    else
                    {
                        ___m_ReticleAnimators[(int)___m_CurrentGunState].transform.localScale = new Vector3();
                    }

                    //Removing laser by changing it's width to 0
                    ___m_LaserStartWidth = 0.0f;
                }
                return true;
            }
        }        
    }
}
