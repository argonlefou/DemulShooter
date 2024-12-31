using HarmonyLib;
using MRCQ;
using System;
using System.Xml;

namespace UnityPlugin_BepInEx_MarsSortie.Patch.mMRCQ
{
    class mSignalKey
    {
        /// <summary>
        /// Replacing hardware encoded keyboard keys to MAME-compatible map
        /// </summary>
        [HarmonyPatch(typeof(SignalKey), MethodType.Constructor, new System.Type[] { typeof(XmlNode), typeof (bool) })]
        class Ctor
        {
            static void Postfix(XmlNode signal, bool isInputSignal, ref string ___simulator, string ___device, string ___name)
            {
                MarsSortie_BepInEx_Plugin.MyLogger.LogMessage("---------- SIGNAL-----------");
                MarsSortie_BepInEx_Plugin.MyLogger.LogMessage("IsInput=" + isInputSignal);
                MarsSortie_BepInEx_Plugin.MyLogger.LogMessage("name=" + ___name);
                MarsSortie_BepInEx_Plugin.MyLogger.LogMessage("device=" + ___device);
                MarsSortie_BepInEx_Plugin.MyLogger.LogMessage("key='" + ___simulator + "'");
                
                bool flag = false;
                string oldsim = ___simulator;
                switch (___simulator)
                {
                    case "1": ___simulator = "5"; flag = true; break;
                    case "2": ___simulator = "6"; flag = true; break;
                    case "3": ___simulator = "7"; flag = true; break;
                    case "4": ___simulator = "8"; flag = true; break;

                    /*case "q": ___simulator = "1"; flag = true; break;
                    case "w": ___simulator = "2"; flag = true; break;
                    case "e": ___simulator = "3"; flag = true; break;
                    case "r": ___simulator = "4"; flag = true; break;*/

                    default: break;
                }
                if (flag)
                    MarsSortie_BepInEx_Plugin.MyLogger.LogMessage("MRCQ.SignalKey.Ctor(): changed Key for " + ___name + " Signal from [" + oldsim + "] to [" + ___simulator + "]" );
            }
        }

        /// <summary>
        /// Blocking Keyboard/mouse inputs for gameplay
        /// Only keeping TEST menu controls
        /// </summary>
        [HarmonyPatch(typeof(SignalKey), "Update")]
        class Update
        {
            static bool Prefix(ref string ___simulator, string ___outputKeyName, bool ___isVKey, ref int ___mouse)
            {
                if (MarsSortie_BepInEx_Plugin.EnableInputHack)
                {
                    //Remove mouse buttons
                    if (___mouse != -1)
                        ___mouse = -1;

                    //Remove unwanted keyboard buttons
                    if (___simulator != null)
                    {
                        if (___simulator.Equals("5") || ___simulator.Equals("6") || ___simulator.Equals("7") || ___simulator.Equals("8") ||
                            ___simulator.Equals("space") || ___simulator.Equals("up") || ___simulator.Equals("down") || ___simulator.Equals("left") || ___simulator.Equals("right"))
                            return true;
                        else
                            ___simulator = null;
                    }
                }
                return true;
            }
        }
    }
}
