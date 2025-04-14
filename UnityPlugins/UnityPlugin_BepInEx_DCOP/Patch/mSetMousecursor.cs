using HarmonyLib;

namespace DCop_BepInEx_DemulShooter_Plugin.Patch
{
    class mSetMousecursor
    {
        /// <summary>
        /// Remove crosshair
        /// </summary>
        [HarmonyPatch(typeof(SetMouseCursor), "ShowCursor")]
        class ShowCursor
        {
            static bool Prefix()
            {
                DemulShooter_Plugin.MyLogger.LogWarning("mSetMousecursor.ShowCursor()");
                if (DemulShooter_Plugin.CrossHairVisibility == false)
                    UnityEngine.Cursor.visible = false;
                else
                    UnityEngine.Cursor.visible = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(SetMouseCursor), "SetCursor")]
        class SetCursor
        {
            static bool Prefix(ref int cursorNumber)
            {
                DemulShooter_Plugin.MyLogger.LogWarning("mSetMousecursor.SetCursor() => cursorNumber=" + cursorNumber);
                if (DemulShooter_Plugin.CrossHairVisibility == false)
                    cursorNumber = -1;  //-1 makes the original function call HideCursor()
                return true;
            }
        }
    }
}
