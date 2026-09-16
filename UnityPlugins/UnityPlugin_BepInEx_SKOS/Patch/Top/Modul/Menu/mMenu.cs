using HarmonyLib;
using Top.Modul.Menu;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mMenu
    {
        /// <summary>
        /// Forcing English language
        /// </summary>
        [HarmonyPatch(typeof(Menu), "GetLanguage")]
        class GetLanguage
        {
            static void Postfix(ref int __result)
            {
                if (DemulShooter_Plugin.Language.Equals("EN"))
                    __result = 0;
                else if (DemulShooter_Plugin.Language.Equals("CH"))
                    __result = 1;
            }
        }
    }
}
