using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mAssistWindow
    {

        /// <summary>
        /// Change game resolution and remove some of Cursor/Resolution dynamic setting by not running the IEnumerator
        /// </summary>
        [HarmonyPatch(typeof(Top.AssistWindow), "OnEnable")]
        class OnEnable
        {
            static bool Prefix(Top.AssistWindow __instance)
            {
                Cursor.visible = false;
                if (DemulShooter_Plugin.ForceResolution)
                {
                    Screen.SetResolution(DemulShooter_Plugin.ScreenWidth, DemulShooter_Plugin.ScreenHeight, DemulShooter_Plugin.Fullscreen);
                    return false;
                }

                //Original procedure:
                return true;
            }
        }

        /// <summary>
        /// If ppligin does not force resolution, that IEnumerator is called and run in a loop
        /// Modding it so that it does not make cursor visible, or change resolution
        /// </summary>
        [HarmonyPatch(typeof(Top.AssistWindow), "Handle", MethodType.Enumerator)]
        class Handle
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                if (!DemulShooter_Plugin.ForceResolution)
                {
                    for (int i = 0; i < code.Count; i++)
                    {                       
                        //Removing the Scale Change (-0.1) if "Backslash" Key is pressed
                        if (code[i].opcode == OpCodes.Sub)
                        {
                            if (code[i - 1].opcode == OpCodes.Ldc_R4 && (float)code[i - 1].operand == 0.1f)
                            {
                                code[i - 1].operand = 0.0f;
                            }
                        }

                        //Forcing Cursur.SetVisible to FALSE if mouse button is pressed
                        if (code[i].opcode == OpCodes.Call && code[i].operand.ToString().Contains("set_visible"))
                        {
                            if (code[i - 1].opcode == OpCodes.Ldc_I4_1)
                                code[i - 1].opcode = OpCodes.Ldc_I4_0;
                        }
                    }
                }
                return code;
            }


            
        }
    }
}
