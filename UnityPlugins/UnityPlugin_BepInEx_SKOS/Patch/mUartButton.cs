using System;
using HarmonyLib;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mUartButton
    {
        [HarmonyPatch(typeof(UartButton), "OnEnable")]
        class OnEnable
        {
            static void Postfix(ref Func<bool> ___UartCB, BehaviourBase Data, UartValue Input, Func<bool> CB = null)
            {
                //DemulShooter_Plugin.MyLogger.LogMessage("UartButton.OnEnable(): " + Input.UartKey);
                ___UartCB = null;
            }
        }
    }
}
