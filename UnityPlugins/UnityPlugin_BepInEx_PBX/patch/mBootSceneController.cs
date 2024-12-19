using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace UnityPlugin_BepInEx_PBX
{
    class mBootSceneController
    {
        /// <summary>
        /// Transpiler to change the "true" parameter for fullscreen to "false"
        /// </summary>
        [HarmonyPatch(typeof(BootSceneController))]
        [HarmonyPatch("Start")]
        public static class Start
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_I4_1)
                    {
                        codes[i].opcode = OpCodes.Ldc_I4_0;
                        break;
                    }
                }
                return codes;
            }
        }
    }
}
