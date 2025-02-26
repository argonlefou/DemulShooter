using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace UnityPlugin_BepInEx_PBX.patch
{
    class mTitleController
    {
        /// <summary>
        /// Transpiler remove the "PLEASE INSERT COIN" nag when a player is not playing
        /// The game is detecting Gun at screen (of course, with the plugin) and displays the message after 3 seconds
        /// To remove the message, either set the gun "OffScreen" (T/Y keys)
        /// Or like here, remove the Time variable to be inscreased by the timer so that the 3 seconds will never be reached
        /// </summary>
        [HarmonyPatch(typeof(TitleController))]
        [HarmonyPatch("Update")]
        public static class Start
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Call && codes[i].operand.ToString().Contains("get_deltaTime"))
                    {
                        codes[i].opcode = OpCodes.Ldc_R4;
                        codes[i].operand = 0.0f;
                        PointBlankX_Plugin.MyLogger.LogMessage("UIPlayerPanel.Update(): Patched INSERT COIN Title/Demo panel");
                        break;
                    }
                }
                return codes;
            }
        }
    }
}
