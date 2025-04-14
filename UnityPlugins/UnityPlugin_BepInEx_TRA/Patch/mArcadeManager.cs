using System;
using HarmonyLib;
using UnityEngine;

namespace TombRaider_BepInEx_DemulShooter_Plugin
{
    class mArcadeManager
    {
        [HarmonyPatch(typeof(ArcadeManager), "CheckIOBoard")]
        class CheckIOBoard
        {
            static bool Prefix()
            {
                UnityEngine.Debug.Log("mArcadeManager.CheckIOBoard()");
                return false;
            }
        }

        [HarmonyPatch(typeof(ArcadeManager), "GunMalfunctionCheck")]
        class GunMalfunctionCheck
        {
            static bool Prefix()
            {
                UnityEngine.Debug.Log("mArcadeManager.GunMalfunctionCheck()");
                return false;
            }
        }

        //Removes the error screen
        [HarmonyPatch(typeof(ArcadeManager), "PushErrorPopup")]
        class PushErrorPopup
        {
            static bool Prefix()
            {
                UnityEngine.Debug.Log("mArcadeManager.PushErrorPopup()");
                return false;
            }
        }
    }
}
