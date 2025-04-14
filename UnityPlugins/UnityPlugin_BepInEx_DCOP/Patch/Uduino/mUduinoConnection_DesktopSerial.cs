using HarmonyLib;
using Uduino;

namespace DCop_BepInEx_DemulShooter_Plugin
{
    class mUduinoConnection_DesktopSerial
    {
        /// <summary>
        /// disable Arduino lookups and hangs for connections failed
        /// </summary>
        [HarmonyPatch(typeof(UduinoConnection_DesktopSerial), "Discover")]
        class TurnOnPin
        {
            static bool Prefix(ref string[] portNames)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("mUduinoConnection_DesktopSerial.Discover()");
                portNames = new string[0];
                return true;
            }
        }
    }
}
