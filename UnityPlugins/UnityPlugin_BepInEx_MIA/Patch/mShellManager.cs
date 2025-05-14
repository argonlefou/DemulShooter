using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UGGameShell;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    class mShellManager
    {
        /// <summary>
        /// Activating possibility to play without SHELL
        /// </summary>
        [HarmonyPatch(typeof(ShellManager), "Awake")]
        class Awake
        {
            static bool Prefix(ref bool ___SHELL_ONLY)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("ShellManager.Awake()");
                ___SHELL_ONLY = false;
                return true;
            }
        }

        /// <summary>
        /// Blocking Security counter increment
        /// </summary>
        [HarmonyPatch(typeof(ShellManager), "Update")]
        class Update
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                for (int i = 0; i < code.Count - 1; i++)
                {
                    if (code[i].opcode == OpCodes.Ldfld && code[i].operand.ToString().Contains("m_securityCounter"))
                    {
                        if (code[i + 1].opcode == OpCodes.Ldc_I4_1)
                            code[i + 1].opcode = OpCodes.Ldc_I4_0;
                    }
                }
                return code;
            }
        }

        /// <summary>
        /// Blocking Security counter increment
        /// </summary>
        [HarmonyPatch(typeof(ShellManager), "AnalyzeCommand")]
        class AnalyzeCommand
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                for (int i = 0; i < code.Count - 2; i++)
                {
                    if (code[i].opcode == OpCodes.Ldfld && code[i].operand.ToString().Contains("m_securityCounter"))
                    {
                        if (code[i + 1].opcode == OpCodes.Ldc_I4_1 && code[i+2].opcode == OpCodes.Add)
                            code[i + 1].opcode = OpCodes.Ldc_I4_0;
                    }
                }
                return code;
            }
        }

        /// <summary>
        /// Intercepting outputs command sent to Shell
        /// </summary>
        [HarmonyPatch(typeof(ShellManager), "SendCommand")]
        class SendCommand
        {
            static bool Prefix(CMD_ID cmdid, byte[] data)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("ShellManager.SendCommand() : cmdid=" + cmdid.ToString());                
                //Damage
                if (cmdid == CMD_ID.GAME_MSG_PLAYER_DAMAGE)
                {
                    if (data[0] == 1)
                        DemulShooter_Plugin.OutputData.Damaged[0] = 1;
                    else if (data[0] == 2)
                        DemulShooter_Plugin.OutputData.Damaged[1] = 1;
                    else if (data[0] == 0)
                    {
                        DemulShooter_Plugin.OutputData.Damaged[0] = 1;
                        DemulShooter_Plugin.OutputData.Damaged[1] = 1;
                    }
                }
                //Life
                /*else if (cmdid == CMD_ID.GAME_MSG_CURRENT_HEALTH)
                {
                    if (data[0] == 1)
                        DemulShooter_Plugin.OutputData.Life[0] = data[1];
                    else if (data[0] == 2)
                        DemulShooter_Plugin.OutputData.Life[1] = data[1];
                    else if (data[0] == 3)
                    {
                        DemulShooter_Plugin.OutputData.Life[0] = data[1];
                        DemulShooter_Plugin.OutputData.Life[1] = data[1];
                    }
                }*/

                return true;
            }
        }

        [HarmonyPatch(typeof(ShellManager), "GetCommandID")]
        class GetcommandId
        {
            static void Postfix(byte[] data, int length, CMD_ID __result)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("ShellManager.GetcommandId() : result=" + __result.ToString());                
            }
        }
        
    }
}
