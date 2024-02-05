using HarmonyLib;

namespace UnityPlugin_BepInEx_PBX
{
    class mBNUsioController
    {  
        /// <summary>
        /// Intercepting START Led conmmands to send to DemulShooter
        /// </summary>
        [HarmonyPatch(typeof(BNUsioController), "setStartLED")]
        class setStartLED
        {
            static void Postfix(int idx, bool isOn)
            {
                //PointBlankX_Plugin.MyLogger.LogMessage("mBNUsioController.setStartLED() => idx : " + idx.ToString() + ", isOn: " + isOn.ToString());
                if (idx == 1)
                    PointBlankX_Plugin.OutputData.P1_StartLED = isOn == true ? (byte)1 : (byte)0;
                else if (idx == 2)
                    PointBlankX_Plugin.OutputData.P2_StartLED = isOn == true ? (byte)1 : (byte)0;                
            }
        }

        /// <summary>
        /// Intercepting Cabinet Led conmmands to send to DemulShooter
        /// </summary>
        [HarmonyPatch(typeof(BNUsioController), "setLED")]
        class setLED
        {
            static void Postfix(int idx, bool isOn)
            {
                //PointBlankX_Plugin.MyLogger.LogMessage("mBNUsioController.setLED() => idx : " + idx.ToString() + ", isOn: " + isOn.ToString());
                if (idx == 1)
                    PointBlankX_Plugin.OutputData.P1_LED = isOn == true ? (byte)1 : (byte)0;
                else if (idx == 2)
                    PointBlankX_Plugin.OutputData.P2_LED = isOn == true ? (byte)1 : (byte)0;
            }
        }

        /// <summary>
        /// Check here if a patch is needed on computer where controls are not responding at all ??? (TEST, SERVICE, etc....)
        /// </summary>
        /*[HarmonyPatch(typeof(BNUsioController), "Update")]
        class Update
        {
            static bool Prefix()
            {                
                // Nothing ?
                return true;
            }
        }*/
        


    }
}
