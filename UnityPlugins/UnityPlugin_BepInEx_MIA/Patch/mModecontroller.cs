using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    class mModecontroller
    {
        /// <summary>
        /// Disabling Always-On-Top window mode
        /// </summary>
        [HarmonyPatch(typeof(ModeController), "FixedUpdate")]
        class FixedUpdate
        {
            static bool Prefix()
            {
                return false;
            }
        }

        /// <summary>
        /// Forcing cursor ON
        /// </summary>
        [HarmonyPatch(typeof(ModeController), "Update")]
        class Update
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                /*for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldc_I4_0)
                    {
                        code[i].opcode = OpCodes.Ldc_I4_1;
                    }
                }*/
                return code;
            }
        }
    }
}
