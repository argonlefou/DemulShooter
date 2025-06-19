using HarmonyLib;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    class mUIManager
    {
        /// <summary>
        /// Changing Unity ScreenResolution will only impact 3D rendering
        /// 2D sprites will stay on original resolution rendering, but there's a general 2DCanvas here to be able to change that
        /// </summary>
        [HarmonyPatch(typeof(UIManager), "Update")]
        class Update
        {
            static void Postfix(UIManager __instance)
            {
                if (DemulShooter_Plugin.ChangeResolution)
                {
                    foreach (UnityEngine.UI.CanvasScaler cs in __instance.MainCanvas2D.GetComponents<UnityEngine.UI.CanvasScaler>())
                    {
                        cs.scaleFactor = (float)DemulShooter_Plugin.ScreenWidth / DemulShooter_Plugin.ORIGINAL_WIDTH;
                    }
                }
            }
        }
    }
}
