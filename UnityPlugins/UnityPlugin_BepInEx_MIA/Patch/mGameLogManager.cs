using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    class mGameLogManager
    {
        /// <summary>
        /// Using local file from folder
        /// </summary>
        [HarmonyPatch(typeof(GameLogManager), "Start")]
        class Start
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "../GameData/MI_Log/")
                    {
                        code[i].operand = BepInEx.Paths.GameRootPath + "/NVRAM/GameData/MI_Log/";
                    }
                }
                return code;
            }
        }
    }
}
