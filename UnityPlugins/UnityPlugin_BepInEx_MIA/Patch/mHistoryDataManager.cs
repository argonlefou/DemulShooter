using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System;

namespace MissionImpossible_BepInEx_DemulShooter_Plugin.Patch
{
    class mHistoryDataManager
    {
        //--------------------------------------------//
        //Use a lot of Hardcoded patch to store file  //
        //Changing them to local folder               //
        //--------------------------------------------//

        [HarmonyPatch(typeof(HistoryDataManager), "Start")]
        class Start
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldstr && ((string)code[i].operand).StartsWith("../"))
                    {
                        string s = (string)code[i].operand;
                        s = s.Replace("../", BepInEx.Paths.GameRootPath + "/NVRAM/");
                        code[i].operand = s;
                    }
                }
                return code;
            }
        }

        [HarmonyPatch(typeof(HistoryDataManager), "GenerateHistoryData", new Type[]{typeof(int), typeof(string)})]
        class GenerateHistoryData
        {
            static bool Prefix(int stage, ref string folderPath)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("HistoryDataManager.GenerateHistoryData() : folderPath=" + folderPath);
                if (folderPath.StartsWith("../"))
                    folderPath = folderPath.Replace("../", BepInEx.Paths.GameRootPath + "/NVRAM/");
                DemulShooter_Plugin.MyLogger.LogMessage("--> HistoryDataManager.GenerateHistoryData() : folderPath=" + folderPath);
                return true;
            }
        }

        [HarmonyPatch(typeof(HistoryDataManager), "GenerateHistoryDataFromFile")]
        class GenerateHistoryDataFromFile
        {
            static bool Prefix(ref string folderPath)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("HistoryDataManager.GenerateHistoryDataFromFile() : folderPath=" + folderPath);
                if (folderPath.StartsWith("../"))
                    folderPath = folderPath.Replace("../", BepInEx.Paths.GameRootPath + "/NVRAM/");
                DemulShooter_Plugin.MyLogger.LogMessage("--> HistoryDataManager.GenerateHistoryDataFromFile() : folderPath=" + folderPath);
                return true;
            }
        }

        [HarmonyPatch(typeof(HistoryDataManager), "ReadHistoryDataFromFile")]
        class ReadHistoryDataFromFile
        {
            static bool Prefix(ref string folderPath)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("HistoryDataManager.ReadHistoryDataFromFile() : folderPath=" + folderPath);
                if (folderPath.StartsWith("../"))
                    folderPath = folderPath.Replace("../", BepInEx.Paths.GameRootPath + "/NVRAM/");
                DemulShooter_Plugin.MyLogger.LogMessage("--> HistoryDataManager.ReadHistoryDataFromFile() : folderPath=" + folderPath);
                return true;
            }
        }
        
        [HarmonyPatch(typeof(HistoryDataManager), "ReadPlayerGameDataFromFile")]
        class ReadPlayerGameDataFromFile
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "../GameData/MI_PlayerData/PlayerGameData.txt")
                    {
                        code[i].operand = BepInEx.Paths.GameRootPath + "/NVRAM/GameData/MI_PlayerData/PlayerGameData.txt";
                    }
                }
                return code;
            }
        }

        [HarmonyPatch(typeof(HistoryDataManager), "ReadPlayerMADataFromFile")]
        class ReadPlayerMADataFromFile
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "../GameData/MI_PlayerData/PlayerMAData.txt")
                    {
                        code[i].operand = BepInEx.Paths.GameRootPath + "/NVRAM/GameData/MI_PlayerData/PlayerMAData.txt";
                    }
                }
                return code;
            }
        }

        [HarmonyPatch(typeof(HistoryDataManager), "SavePlayerGameDataToFlie")]
        class SavePlayerGameDataDataToFlie
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "../GameData/MI_PlayerData/PlayerGameData.txt")
                    {
                        code[i].operand = BepInEx.Paths.GameRootPath + "/NVRAM/GameData/MI_PlayerData/PlayerGameData.txt";
                    }
                }
                return code;
            }
        }

        [HarmonyPatch(typeof(HistoryDataManager), "SavePlayerMADataDataToFlie")]
        class SavePlayerMADataDataToFlie
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var code = new List<CodeInstruction>(instructions);
                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].opcode == OpCodes.Ldstr && (string)code[i].operand == "../GameData/MI_PlayerData/PlayerMAData.txt")
                    {
                        code[i].operand = BepInEx.Paths.GameRootPath + "/NVRAM/GameData/MI_PlayerData/PlayerMAData.txt";
                    }
                }
                return code;
            }
        }

        [HarmonyPatch(typeof(HistoryDataManager), "SaveProgressDataToFile", new Type[] { typeof(StageName), typeof(string) })]
        class SaveProgressDataToFile
        {
            static bool Prefix(StageName stage, ref string folderPath)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("HistoryDataManager.SaveProgressDataToFile() : folderPath=" + folderPath);
                if (folderPath.StartsWith("../"))
                    folderPath = folderPath.Replace("../", BepInEx.Paths.GameRootPath + "/NVRAM/");
                DemulShooter_Plugin.MyLogger.LogMessage("--> HistoryDataManager.SaveProgressDataToFile() : folderPath=" + folderPath);
                return true;
            }
        }
        
    }
}
