using System.Globalization;
using HarmonyLib;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mConfigLevel
    {
        /// <summary>
        /// Add CultureInfo.InvariantCulture to enable parsing float with decimal point in config file
        /// </summary>
        [HarmonyPatch(typeof(ConfigLevel), "GetValue")]
        class GetValue
        {
            static bool Prefix(ref float[] __result, string Config, string ___Path)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("ConfigLevel.GetValue(): Config=" + Config);
                __result = ConfigKind.FileMap.GetConfig<string>(___Path, Config, null).Split(',').Convert((string Item) => float.Parse(Item, CultureInfo.InvariantCulture));
                return false;
            }
        }
    }
}
