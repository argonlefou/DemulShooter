using HarmonyLib;
using UnityEngine;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mUartAD
    {
        /// <summary>
        /// Overwrite Axis data with our own, simulating UART data
        /// </summary>
        [HarmonyPatch(typeof(UartAD), "GetPercent_Uart")]
        class GetPercent_Uart
        {
            static void Postfix(Vector3 Point, ref Vector3 __result, int ___ID)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("UartAD.GetPercent_Uart(): ID= " + ___ID + ", Point=" + Point.ToString() + ", Result=" + __result.ToString());

                float fX = (DemulShooter_Plugin.PluginControllers[___ID].Axis_X / (float)Screen.width) * 2.0f - 1.0f;
                float fY = (DemulShooter_Plugin.PluginControllers[___ID].Axis_Y / (float)Screen.height) * 2.0f - 1.0f;
                fX = Mathf.Clamp(fX, -1.0f, 1.0f);
                fY = Mathf.Clamp(fY, -1.0f, 1.0f);
                __result = new Vector3 (fX, fY);
            }
        }
    }
}
