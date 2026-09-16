using HarmonyLib;
using Top.Modul.Uart;
using UnityEngine;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mUartChatA
    {
        /// <summary>
        /// Clean COM init to remove errors
        /// </summary>
        [HarmonyPatch(typeof(Top.Modul.Uart.A.UartChatA), "InitUart")]
        class InitUart
        {
            static bool Prefix(MonoBehaviour Loader, UartConfig Config)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("Top.Modul.Uart.A.UartChatA.InitUart()");
                return false;
            }
        }

        /// <summary>
        /// Clean COM destroy to remove errors
        /// </summary>
        [HarmonyPatch(typeof(Top.Modul.Uart.A.UartChatA), "DestroyUart")]
        class DestroyUart
        {
            static bool Prefix()
            {
                DemulShooter_Plugin.MyLogger.LogMessage("Top.Modul.Uart.A.UartChatA.DestroyUart()");
                return false;
            }
        }

        /// <summary>
        /// Insert our own data instead of the original ones given by COM
        /// </summary>
        [HarmonyPatch(typeof(Top.Modul.Uart.A.UartChatA), "GetData")]
        class GetData
        {
            static bool Prefix(string Key, ref int __result)
            {
                //DemulShooter_Plugin.MyLogger.LogWarning("Top.Modul.Uart.A.UartChatA.GetData(): Key=" + Key);
                //DemulShooter_Plugin.PrintStackTrace();

                switch (Key)
                {
                    case "ButtonMenu": __result = DemulShooter_Plugin.Test_Key.GetButton() ? 1 : 0; break;
                    case "ButtonUp": __result = DemulShooter_Plugin.MenuUp_Key.GetButton() ? 1 : 0; break;
                    case "ButtonDown": __result = DemulShooter_Plugin.MenuDown_Key.GetButton() ? 1 : 0; break;
                    case "ButtonLeft": __result = DemulShooter_Plugin.MenuLeft_Key.GetButton() ? 1 : 0; break;
                    case "ButtonRight": __result = DemulShooter_Plugin.MenuRight_Key.GetButton() ? 1 : 0; break;

                    case "ButtonStart0":
                        {
                            if (DemulShooter_Plugin.PluginControllers[0].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Action) || DemulShooter_Plugin.PluginControllers[0].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Start))
                                __result = 1;
                            else
                                __result = 0;
                        }break;
                    case "ButtonStart1":
                        {
                            if (DemulShooter_Plugin.PluginControllers[1].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Action) || DemulShooter_Plugin.PluginControllers[1].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Start))
                                __result = 1;
                            else
                                __result = 0;
                        }
                        break;
                    case "ButtonStart2":
                        {
                            if (DemulShooter_Plugin.PluginControllers[2].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Action) || DemulShooter_Plugin.PluginControllers[2].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Start))
                                __result = 1;
                            else
                                __result = 0;
                        }
                        break;
                    case "ButtonStart3":
                        {
                            if (DemulShooter_Plugin.PluginControllers[3].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Action) || DemulShooter_Plugin.PluginControllers[3].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Start))
                                __result = 1;
                            else
                                __result = 0;
                        }
                        break;

                    //case "GunShot0": break;
                    //case "GunShot1": break;
                    //case "GunShot2": break;
                    //case "GunShot3": break;

                    case "ButtonShot0": __result = DemulShooter_Plugin.PluginControllers[0].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Trigger) ? 1 : 0; break;
                    case "ButtonShot1": __result = DemulShooter_Plugin.PluginControllers[1].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Trigger) ? 1 : 0; break;
                    case "ButtonShot2": __result = DemulShooter_Plugin.PluginControllers[2].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Trigger) ? 1 : 0; break;
                    case "ButtonShot3": __result = DemulShooter_Plugin.PluginControllers[3].GetButton(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Trigger) ? 1 : 0; break;

                    default: __result = 0;break;
                }
                return false;
            }
        }

        /// <summary>
        /// /// <summary>
        /// Insert our own data instead of the original ones given by COM
        /// </summary>
        [HarmonyPatch(typeof(Top.Modul.Uart.A.UartChatA), "GetSignal")]
        class GetSignal
        {
            static bool Prefix(string Key, ref bool __result)
            {
               //DemulShooter_Plugin.MyLogger.LogWarning("Top.Modul.Uart.A.UartChatA.GetSignal(): Key=" + Key + ", Value=" + __result);
                switch (Key)
                {
                    case "Coin0": __result = DemulShooter_Plugin.PluginControllers[0].GetButtonDown(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Coin);break;
                    case "Coin1": __result = DemulShooter_Plugin.PluginControllers[1].GetButtonDown(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Coin);break;
                    case "Coin2": __result = DemulShooter_Plugin.PluginControllers[2].GetButtonDown(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Coin);break;
                    case "Coin3": __result = DemulShooter_Plugin.PluginControllers[3].GetButtonDown(UnityPlugin_BepInEx_Core.PluginController.MyInputButtons.Coin);break;

                    default: __result = false; break;
                }                
                return false;
            }
        }

        [HarmonyPatch(typeof(Top.Modul.Uart.A.UartChatA), "SetData")]
        class SetData
        {
            static bool Prefix(string Key, int Data)
            {
                if (Key.Equals("MotorShakeGun0"))
                    DemulShooter_Plugin.OutputData.GunMotor[0] = (byte)Data;
                else if (Key.Equals("MotorShakeGun1"))
                    DemulShooter_Plugin.OutputData.GunMotor[1] = (byte)Data;
                else if (Key.Equals("MotorShakeGun2"))
                    DemulShooter_Plugin.OutputData.GunMotor[2] = (byte)Data;
                else if (Key.Equals("MotorShakeGun3"))
                    DemulShooter_Plugin.OutputData.GunMotor[3] = (byte)Data;

                else if (Key.Equals("GameState0"))
                    DemulShooter_Plugin.OutputData.Light[0] = (byte)Data;
                else if (Key.Equals("GameState1"))
                    DemulShooter_Plugin.OutputData.Light[1] = (byte)Data;
                else if (Key.Equals("GameState2"))
                    DemulShooter_Plugin.OutputData.Light[2] = (byte)Data;
                else if (Key.Equals("GameState3"))
                    DemulShooter_Plugin.OutputData.Light[3] = (byte)Data;

                else
                    DemulShooter_Plugin.MyLogger.LogWarning("Top.Modul.Uart.A.UartChatA.SetData(): Key=" + Key + ", Data=" + Data);
                //DemulShooter_Plugin.PrintStackTrace();

                return false;
            }
        }

        [HarmonyPatch(typeof(Top.Modul.Uart.A.UartChatA), "SetSignal")]
        class SetSignal
        {
            static bool Prefix(string Key, int Data)
            {
                DemulShooter_Plugin.MyLogger.LogWarning("Top.Modul.Uart.A.UartChatA.SetSignal(): Key=" + Key + ", Data=" + Data);
                return false;
            }
        }

        /// <summary>
        /// Force return true to remove error messages
        /// </summary>
        [HarmonyPatch(typeof(Top.Modul.Uart.A.UartChatA), "IsConnect")]
        class IsConnect
        {
            static bool Prefix(ref bool __result)
            {
                __result = true;
                return false;
            }
        }
    }
}
