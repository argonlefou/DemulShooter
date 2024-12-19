using HarmonyLib;
using Uduino;

namespace UnityPlugin_BepInEx_DCOP
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
                Dcop_Plugin.MyLogger.LogMessage("mUduinoConnection_DesktopSerial.Discover()");
                portNames = new string[0];
                return true;
            }
        }
    }
}
