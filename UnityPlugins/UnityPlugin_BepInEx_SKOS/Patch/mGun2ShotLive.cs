using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mGun2ShotLive
    {
        /// <summary>
        /// In the IEnumerator, game has hardcoded check for Input.GetMouseButton(0) on top of UART Trigger data
        /// This is causing the mouse click to trigger all guns, even with demulshooter
        /// To remove this, using Transpiler, we can either:
        /// - Change the ldc.i4.0 instruction before the call to change the GetMouseButton() parameter to something physically non existent instead of '0' (= Left Buttton)
        /// - Or just remove the Call to GetMouseButton(), the prior ldc.i4.0 will then be used on top of the stack (instead of the return value of the called function) to set IsShot to "0" (which is what we need)
        /// </summary>
        [HarmonyPatch(typeof(Gun2ShotLive), "Handle", MethodType.Enumerator)]
        class ShotOnce
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Call && code[i].operand.ToString().Contains("GetMouseButton"))
                    {
                        if (code[i - 1].opcode == OpCodes.Ldc_I4_0)
                        {
                            code.RemoveAt(i);
                        }
                    }
                }
                return code;
            }
        }
    }
}
