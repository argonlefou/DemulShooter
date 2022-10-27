using System;
using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_RHA
{
    class mLevelSelectorWindow
    {
        [HarmonyPatch(typeof(LevelSelectorWindow), "PlayerPressTrigger")]
        class PlayerPressTrigger
        {
            static bool Prefix(Vector3 i_Pos, ID i_Player)
            {
                UnityEngine.Debug.LogError("mLevelSelectorWindow.PlayerPressTrigger() : i_Player=" + i_Player.ToString() + ", v=" + i_Pos.ToString());  
                return true;
            }
        }

        [HarmonyPatch(typeof(LevelSelectorWindow), "InitPanels")]
        class InitPanels
        {
            static bool Prefix()
            {
                UnityEngine.Debug.LogError("mLevelSelectorWindow.InitPanels()");  
                return true;
            }
        }
        
    }
}
