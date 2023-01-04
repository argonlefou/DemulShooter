using System;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace UnityPlugin_BepInEx_RHA
{
    class mArcadeGamePlayManager
    {
        [HarmonyPatch(typeof(ArcadeGameplayManager), "LoadDemoMode")]
        class LoadDemoMode
        {
            static bool Prefix(WorldManager.WorldIndex i_World)
            {
                UnityEngine.Debug.Log("mArcadeGameplayManager.LoadDemoMode()");
                Demulshooter_Plugin.IsDemoMode = true;
                return true;
            }
        }

        [HarmonyPatch(typeof(ArcadeGameplayManager), "OnDemoStateExit")]
        class OnDemoStateExit
        {
            static bool Prefix()
            {
                UnityEngine.Debug.Log("mArcadeGameplayManager.OnDemoStateExit()");
                Demulshooter_Plugin.IsDemoMode = false;
                return true;
            }
        }

    }
}
