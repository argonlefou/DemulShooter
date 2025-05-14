using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    class mGameObjPoolManager
    {
        /// <summary>
        /// Change folder to local one
        /// </summary>
        [HarmonyPatch(typeof(GameObjPoolManager), "DebugListenPoolRecord")]
        class DebugListenPoolRecord
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == ("../GameData/MI_EffectPoolLog/"))
                    {
                        code[i].operand = BepInEx.Paths.GameRootPath + "/NVRAM/GameData/MI_EffectPoolLog/";
                    }
                }
                return code;
            }
        }
    }
}
