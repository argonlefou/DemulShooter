using HarmonyLib;
using UnityEngine;

namespace RabbidsHollywood_BepInEx_DemulShooter_Plugin
{
    class mLevelSelectorWindow
    {
        [HarmonyPatch(typeof(LevelSelectorWindow), "PlayerPressTrigger")]
        class PlayerPressTrigger
        {
            static bool Prefix(Vector3 i_Pos, ID i_Player)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("mLevelSelectorWindow.PlayerPressTrigger() : i_Player=" + i_Player.ToString() + ", v=" + i_Pos.ToString());  
                return true;
            }
        }

        [HarmonyPatch(typeof(LevelSelectorWindow), "InitPanels")]
        class InitPanels
        {
            static bool Prefix()
            {
                DemulShooter_Plugin.MyLogger.LogMessage("mLevelSelectorWindow.InitPanels()");  
                return true;
            }
        }
        
    }
}
