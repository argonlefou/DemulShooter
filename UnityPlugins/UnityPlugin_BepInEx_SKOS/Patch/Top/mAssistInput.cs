using HarmonyLib;
using UnityEngine;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mAssistInput
    {
        /// <summary>
        /// Force the game to use SHIFT + any other key to use DebugKeyboard keys hardcoded
        /// </summary>
        [HarmonyPatch(typeof(Top.AssistInput), "GetKeyDown")]
        class GetKeyDown
        {
            static bool Prefix(ref KeyCode ___Enable, KeyCode Key1, string Key2 = null)
            {
                ___Enable = KeyCode.LeftShift;
                return true;
            }
        }
    }
}
