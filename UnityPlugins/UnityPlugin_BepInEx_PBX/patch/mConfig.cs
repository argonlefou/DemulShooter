using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace PointBlankX_BepInEx_DemulShooter_Plugin
{
    class mConfig
    {
        /// <summary>
        /// Transpiler to change the default SERIAL number from nothing (000000-000000/E) to something else
        /// </summary>
        [HarmonyPatch(typeof(Config))]
        [HarmonyPatch("loadConfig")]
        public static class loadConfig
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldstr && ((string)(codes[i].operand)).Equals(@"000000-000000/E"))
                    {
                        codes[i].operand = @"04R60N013F0U/A";
                        break;
                    }
                }
                return codes;
            }
        }
    }
}
