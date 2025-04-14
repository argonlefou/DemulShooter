using HarmonyLib;
using MRCQ;
using System;

namespace MarsSortie_BepInEx_DemulShooter_Plugin.Patch
{
    class mGameDatabase
    {
        /// <summary>
        /// Called in a loop
        /// Station is player : 0 to 5
        /// We can get credits value for outputs
        /// 
        /// Also check GetcoinsIn() ???
        /// </summary>
        [HarmonyPatch(typeof(MRCQ.GameDataBase), "GetCredit")]
        class GetCredits
        {
            static bool Prefix(int station = -1, int record = -1, bool total = false)
            {
                //MarsSortie_Test_BepInEx_Plugin.MyLogger.LogMessage("MRCQ.GameDataBase.GetCredits(): station=" + station + ", record=" + record + ",total=" + total );
                return true;
            }
            static void Postfix(ref int __result, int station = -1, int record = -1, bool total = false)
            {
                switch (station)
                {
                    case 0:
                        DemulShooter_Plugin.OutputData.P1_Credits = (UInt16)__result; break;
                    case 1:
                        DemulShooter_Plugin.OutputData.P2_Credits = (UInt16)__result; break;
                    case 2:
                        DemulShooter_Plugin.OutputData.P3_Credits = (UInt16)__result; break;
                    case 3:
                        DemulShooter_Plugin.OutputData.P4_Credits = (UInt16)__result; break;

                    default: break;         
                }
            }
        }
    }
}
