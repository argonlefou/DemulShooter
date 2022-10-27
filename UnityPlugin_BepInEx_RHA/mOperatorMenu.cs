using System;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace UnityPlugin_BepInEx_RHA
{
    class mOperatorMenu
    {
        [HarmonyPatch(typeof(OperatorMenu.OperatorMenuSaveInfo), "ResetData")]
        class ResetData
        {
            static void Postfix(OperatorMenu.OperatorMenuSaveInfo __instance)
            {
                UnityEngine.Debug.Log("mOperatorMenu.ResetData() : Trying to read custom config file...");
                try
                {
                    String[] lines = System.IO.File.ReadAllLines("RabbidsHollywood_Operator.conf");
                    foreach (String line in lines)
                    {
                        String[] buffer = line.Split(':');
                        String Key = buffer[0];
                        String Value = buffer[1];

                        switch (Key)
                        {
                            case "CreditsPerGame":
                                {
                                    __instance.m_CreditsPerGame = int.Parse(Value);
                                } break;
                            case "GameAudioVolume":
                                {
                                    __instance.m_GameAudioVolume = int.Parse(Value);
                                } break;
                            case "AttractAudioVolume":
                                {
                                    __instance.m_AttractAudioVolume = int.Parse(Value);
                                } break;
                            case "PaymentType":
                                {
                                    __instance.m_PaymentType = int.Parse(Value);
                                } break;
                            case "ElectronicTicket":
                                {
                                    __instance.m_ElectronicTicket = int.Parse(Value);
                                } break;
                            case "CredtitsSharing":
                                {
                                    __instance.m_CanShareCredits = int.Parse(Value);
                                } break;
                            case "Difficulty":
                                {
                                    __instance.m_Difficulty = int.Parse(Value);
                                } break;
                            case "RedemptionMode":
                                {
                                    __instance.m_RedemptionMode = int.Parse(Value);
                                } break;
                            case "MinimumTicket":
                                {
                                    __instance.m_MinimumTicket = int.Parse(Value);
                                } break;
                            case "TicketValue":
                                {
                                    __instance.m_TicketValue = int.Parse(Value);
                                } break;
                            case "PointPerTicket":
                                {
                                    __instance.m_PointPerTicket = int.Parse(Value);
                                } break;
                            case "GunFrequency":
                                {
                                    __instance.m_GunFrequence = int.Parse(Value);
                                } break;
                            case "GunP1":
                                {
                                    if (int.Parse(Value) == 0)
                                        __instance.m_GunEnable[0] = false;
                                    else
                                        __instance.m_GunEnable[0] = true;
                                } break;
                            case "GunP2":
                                {
                                    if (int.Parse(Value) == 0)
                                        __instance.m_GunEnable[1] = false;
                                    else
                                        __instance.m_GunEnable[1] = true;
                                } break;
                            case "GunP3":
                                {
                                    if (int.Parse(Value) == 0)
                                        __instance.m_GunEnable[2] = false;
                                    else
                                        __instance.m_GunEnable[2] = true;
                                } break;
                            case "GunP4":
                                {
                                    if (int.Parse(Value) == 0)
                                        __instance.m_GunEnable[3] = false;
                                    else
                                        __instance.m_GunEnable[3] = true;
                                } break;
                            default: break;
                        }
                    }
                    UnityEngine.Debug.Log("mOperatorMenu.ResetData() : Succes !");                
                }
                catch (Exception Ex)
                {
                    UnityEngine.Debug.Log("mOperatorMenu.ResetData() : Can't read config data, using custom default values.");
                    UnityEngine.Debug.Log("mOperatorMenu.ResetData() : " + Ex.Message.ToString());
                    __instance.m_CreditsPerGame = 0;            //0 to 20
                    __instance.m_GameAudioVolume = 20;          //0 to 20
                    __instance.m_AttractAudioVolume = 20;       //0 to 20
                    __instance.m_PaymentType = 0;               //0 = Credits, 1=Card
                    __instance.m_ElectronicTicket = 0;          //0 = OFF, 1 = ON
                    __instance.m_BossValue = 10;
                    __instance.m_Difficulty = 1;                //1 to 5
                    __instance.m_RedemptionMode = 0;            //0 = OFF, 1 = ON
                    __instance.m_MinimumTicket = 5;             //0 to 10
                    __instance.m_TicketValue = 2;               //1 or 2
                    __instance.m_PointPerTicket = 1000;         //500 to 5000
                    __instance.m_NewJersey = 0;
                    __instance.m_ContinueCost = 4;
                    __instance.m_ContinueAmount = 2;
                    __instance.m_WorldSelection = 1;
                    __instance.m_WorldTimer = 15;
                    //__instance.m_StartGameFailSafeTimer = 15;
                    __instance.m_CanShareCredits = 0;           //0 = OFF, 1 = ON
                    /*__instance.m_LT_Games = 0;
                    __instance.m_LT_Credits = 0;
                    __instance.m_LR_Games = 0;
                    __instance.m_LR_Credits = 0;
                    __instance.m_LR_Day = DateTime.Now.Day;
                    __instance.m_LR_Month = DateTime.Now.Month;
                    __instance.m_LR_Year = DateTime.Now.Year;
                    __instance.m_LT_ContinuesTakenTotal = 0;
                    __instance.m_LR_ContinuesTakenTotal = 0;
                    __instance.m_LT_ContinuesOfferedTotal = 0;
                    __instance.m_LR_ContinuesOfferedTotal = 0;
                    __instance.m_LT_ContinuesTakenSingle = 0;
                    __instance.m_LR_ContinuesTakenSingle = 0;
                    __instance.m_LT_ContinuesOfferedSingle = 0;
                    __instance.m_LR_ContinuesOfferedSingle = 0;
                    __instance.m_LT_ContinuesTakenMulti = 0;
                    __instance.m_LR_ContinuesTakenMulti = 0;
                    __instance.m_LT_ContinuesOfferedMulti = 0;
                    __instance.m_LR_ContinuesOfferedMulti = 0;
                    __instance.m_LT_Bonus = 0;
                    __instance.m_FailCounterPerLevel = new Dictionary<int, int>();
                    __instance.m_NbOfGamesPlayedPerLevel = new Dictionary<int, int>();
                    __instance.m_WinCounterPerLevel = new Dictionary<int, int>();
                    __instance.CheckDictionaries();*/
                    __instance.m_GunEnable = new List<bool>(4)
			        {
				        true,
				        true,
				        true,
				        true
			        };
                    __instance.m_GunFrequence = 1;
                    //__instance.m_LastLogs = new List<string>();
                }


                
            }
        }
    }
}
