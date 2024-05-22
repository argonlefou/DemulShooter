using HarmonyLib;
using Uduino;

namespace UnityPlugin_BepInEx_DCOP
{
    class mUduinoManager
    {
        /// <summary>
        /// Trigger the siren and police light
        /// </summary>
        [HarmonyPatch(typeof(UduinoManager), "arduinoWrite")]
        class arduinoWrite
        {
            static bool Prefix(UduinoDevice target, int pin, int value, string typeOfPin, string bundle=null)
            {
                /*if (bundle != null)                    
                    Dcop_Plugin.MyLogger.LogMessage("mUduinoManager.arduinoWrite() => target=" + target.ToString() + ", pin=" + pin.ToString() + ", value=" + value.ToString() + ", typeOfPin=" + typeOfPin.ToString() + ", bundle=" + bundle.ToString());
                else      
                    Dcop_Plugin.MyLogger.LogMessage("mUduinoManager.arduinoWrite() => target=" + target.ToString() + ", pin=" + pin.ToString() + ", value=" + value.ToString() + ", typeOfPin=" + typeOfPin.ToString() + ", bundle=null");
                */
                //Dcop_Plugin.MyLogger.LogMessage("mUduinoManager.arduinoWrite() => pin=" + pin.ToString() + ", value=" + value.ToString() + ", typeOfPin=" + typeOfPin.ToString());

                if (value != 0)
                {
                    if (pin == (int)Dcop_Plugin.ArduinoPin.PlayerGun_Solenoid)
                        Dcop_Plugin.OutputData.GunRecoil = 1;

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.DirectHit_Light)
                        Dcop_Plugin.OutputData.DirectHit = 1;

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.Police_LightBar)
                        Dcop_Plugin.OutputData.Police_LightBar = 1;

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.ArduinoReady_Light)
                        Dcop_Plugin.OutputData.GreenTestLight = 1;

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.RedLightContinue)
                        Dcop_Plugin.OutputData.RedLight = 1;

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.WhiteStrobe_Flasher)
                        Dcop_Plugin.OutputData.WhiteStrobe = 1;

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.GameGun_Light)
                        Dcop_Plugin.OutputData.GunLight = 1;

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.DirectHit_Light2)
                        Dcop_Plugin.MyLogger.LogWarning("mUduinoManager.arduinoWrite() => DirectHit_Light2 ON");

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.EnemyGun_Solenoid)
                        Dcop_Plugin.MyLogger.LogWarning("mUduinoManager.arduinoWrite() => Enemy Gun Solenoid ON");

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.EnemyGun2_Solenoid)
                        Dcop_Plugin.MyLogger.LogWarning("mUduinoManager.arduinoWrite() => Enemy Gun Solenoid 2 ON");
                }
                else
                {
                    if (pin == (int)Dcop_Plugin.ArduinoPin.PlayerGun_Solenoid)
                        Dcop_Plugin.OutputData.GunRecoil = 0;

                    if (pin == (int)Dcop_Plugin.ArduinoPin.DirectHit_Light)
                        Dcop_Plugin.OutputData.DirectHit = 0;

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.Police_LightBar)
                        Dcop_Plugin.OutputData.Police_LightBar = 0;

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.ArduinoReady_Light)
                        Dcop_Plugin.OutputData.GreenTestLight = 0;

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.RedLightContinue)
                        Dcop_Plugin.OutputData.RedLight = 0;

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.WhiteStrobe_Flasher)
                        Dcop_Plugin.OutputData.WhiteStrobe = 0;

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.GameGun_Light)
                        Dcop_Plugin.OutputData.GunLight = 0;

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.DirectHit_Light2)
                        Dcop_Plugin.MyLogger.LogWarning("mUduinoManager.arduinoWrite() => DirectHit_Light2 OFF");

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.EnemyGun_Solenoid)
                        Dcop_Plugin.MyLogger.LogWarning("mUduinoManager.arduinoWrite() => Enemy Gun Solenoid OFF");

                    else if (pin == (int)Dcop_Plugin.ArduinoPin.EnemyGun2_Solenoid)
                        Dcop_Plugin.MyLogger.LogWarning("mUduinoManager.arduinoWrite() => Enemy Gun Solenoid 2 OFF");
                }

                return true;
            }
        }
    }
}
