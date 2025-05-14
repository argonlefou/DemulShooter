using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UGGameShell;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    class mGameSetting
    { 
        /// <summary>
        /// Using local file from folder
        /// </summary>
        [HarmonyPatch(typeof(GameSetting), "LoadFile")]
        class LoadFile
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "/../ShellData/GameSettings.ini")
                    {
                        code[i].operand = "/NVRAM/GameSettings.ini";
                    }
                }
                return code;
            }
        }
    }
}
