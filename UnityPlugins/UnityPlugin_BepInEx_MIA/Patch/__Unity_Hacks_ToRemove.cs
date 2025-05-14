using HarmonyLib;

namespace DemulShooter_BepInEx_Plugin_DinoInvasion.Patch
{
    class __Unity_Hacks_ToRemove
    {
        [HarmonyPatch(typeof(UnityEngine.Screen), "SetResolution", new System.Type[] { typeof(int), typeof(int), typeof(bool) })]
        class SetResolution1
        {
            static bool Prefix(ref int width, ref int height, ref bool fullscreen)
            {
                My_DemulShooter_BepInEx_Plugin.MyLogger.LogWarning("UnityEngine.Screen.SetResolution(" + width + ", " + height + ", " + fullscreen + ")");
                My_DemulShooter_BepInEx_Plugin.MyLogger.LogWarning(System.Environment.StackTrace);
                /*width = 800;
                height = 600;
                fullscreen = false;*/
                return true;
            }
        }
        [HarmonyPatch(typeof(UnityEngine.Screen), "SetResolution", new System.Type[] { typeof(int), typeof(int), typeof(bool), typeof(int) })]
        class SetResolution2
        {
            static bool Prefix(ref int width, ref int height, ref bool fullscreen, ref int preferredRefreshRate)
            {
                My_DemulShooter_BepInEx_Plugin.MyLogger.LogWarning("UnityEngine.Screen.SetResolution(" + width + ", " + height + ", " + fullscreen + ", " + preferredRefreshRate + ")");
                My_DemulShooter_BepInEx_Plugin.MyLogger.LogWarning(System.Environment.StackTrace);
                /*width = 800;
                height = 600;
                fullscreen = false;*/
                //preferredRefreshRate = 30;
                return true;
            }
        }
        [HarmonyPatch(typeof(UnityEngine.Cursor), "set_visible")]
        class Cursor_SetVisible
        {
            static bool Prefix(ref bool value)
            {
                My_DemulShooter_BepInEx_Plugin.MyLogger.LogWarning("UnityEngine.Cursor.set_visible(" + value + ")");
                My_DemulShooter_BepInEx_Plugin.MyLogger.LogWarning(System.Environment.StackTrace);
                value = false;
                return true;
            }
        }
    }
}
