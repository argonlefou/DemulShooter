using HarmonyLib;
using UnityEngine;

namespace RabbidsHollywood_BepInEx_DemulShooter_Plugin
{
    class mWorldSelectorWindow
    {
        [HarmonyPatch(typeof(WorldSelectorWindow), "ManageMouseOver")]
        class ManageMouseOver
        {
            static bool Prefix()
            {
                //UnityEngine.Debug.LogError("mWorldSelectorWindow.ManageMouseOver()");
                return false;
            }            
        }

        /// <summary>
        /// Function called at the level select
        /// Keep it in case of need to change values for crosshair/selection offset
        /// </summary>
        [HarmonyPatch(typeof(WorldSelectorWindow), "PlayerPressTrigger")]
        class PlayerPressTrigger
        {
            static bool Prefix(Vector3 i_Pos, ID i_Player)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("mWorldSelectorWindow.PlayerPressTrigger() : i_Player=" + i_Player.ToString() + ", v=" + i_Pos.ToString());                  
                return true;                
            }
        }
    }
}
