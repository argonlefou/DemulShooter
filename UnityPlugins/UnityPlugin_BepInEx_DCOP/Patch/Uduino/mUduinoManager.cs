using HarmonyLib;
using Uduino;

namespace DCop_BepInEx_DemulShooter_Plugin
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
                    if (pin == (int)DemulShooter_Plugin.ArduinoPin.PlayerGun_Solenoid)
                        DemulShooter_Plugin.OutputData.GunRecoil = 1;

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.DirectHit_Light)
                        DemulShooter_Plugin.OutputData.DirectHit = 1;

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.Police_LightBar)
                        DemulShooter_Plugin.OutputData.Police_LightBar = 1;

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.ArduinoReady_Light)
                        DemulShooter_Plugin.OutputData.GreenTestLight = 1;

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.RedLightContinue)
                        DemulShooter_Plugin.OutputData.RedLight = 1;

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.WhiteStrobe_Flasher)
                        DemulShooter_Plugin.OutputData.WhiteStrobe = 1;

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.GameGun_Light)
                        DemulShooter_Plugin.OutputData.GunLight = 1;

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.DirectHit_Light2)
                        DemulShooter_Plugin.MyLogger.LogWarning("mUduinoManager.arduinoWrite() => DirectHit_Light2 ON");

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.EnemyGun_Solenoid)
                        DemulShooter_Plugin.MyLogger.LogWarning("mUduinoManager.arduinoWrite() => Enemy Gun Solenoid ON");

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.EnemyGun2_Solenoid)
                        DemulShooter_Plugin.MyLogger.LogWarning("mUduinoManager.arduinoWrite() => Enemy Gun Solenoid 2 ON");
                }
                else
                {
                    if (pin == (int)DemulShooter_Plugin.ArduinoPin.PlayerGun_Solenoid)
                        DemulShooter_Plugin.OutputData.GunRecoil = 0;

                    if (pin == (int)DemulShooter_Plugin.ArduinoPin.DirectHit_Light)
                        DemulShooter_Plugin.OutputData.DirectHit = 0;

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.Police_LightBar)
                        DemulShooter_Plugin.OutputData.Police_LightBar = 0;

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.ArduinoReady_Light)
                        DemulShooter_Plugin.OutputData.GreenTestLight = 0;

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.RedLightContinue)
                        DemulShooter_Plugin.OutputData.RedLight = 0;

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.WhiteStrobe_Flasher)
                        DemulShooter_Plugin.OutputData.WhiteStrobe = 0;

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.GameGun_Light)
                        DemulShooter_Plugin.OutputData.GunLight = 0;

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.DirectHit_Light2)
                        DemulShooter_Plugin.MyLogger.LogWarning("mUduinoManager.arduinoWrite() => DirectHit_Light2 OFF");

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.EnemyGun_Solenoid)
                        DemulShooter_Plugin.MyLogger.LogWarning("mUduinoManager.arduinoWrite() => Enemy Gun Solenoid OFF");

                    else if (pin == (int)DemulShooter_Plugin.ArduinoPin.EnemyGun2_Solenoid)
                        DemulShooter_Plugin.MyLogger.LogWarning("mUduinoManager.arduinoWrite() => Enemy Gun Solenoid 2 OFF");
                }

                return true;
            }
        }
    }
}
