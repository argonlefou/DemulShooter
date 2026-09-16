using HarmonyLib;
using UnityEngine;

namespace BepInEx_DemulShooter_Plugin.Patch
{
    internal class mDebugKeyboard
    {
        /// <summary>
        /// Remove all keyboard original keys for coin, menu, etc...
        /// </summary>
        [HarmonyPatch(typeof(DebugKeyBoard), "OnEnable")]
        class OnEnable
        {
            static bool Prefix(BehaviourBase Target)
            {
                DemulShooter_Plugin.MyLogger.LogMessage("DebugKeyBoard.OnEnable()");
                return false;
            }
        }

        [HarmonyPatch(typeof(DebugKeyBoard), "GetKey")]
        class GetKey
        {
            static bool Prefix(KeyCode Key)
            {
               return false;
            }
        }

        [HarmonyPatch(typeof(DebugKeyBoard), "GetKeyDown")]
        class GetKeyDown
        {
            static bool Prefix(KeyCode Key)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(DebugKeyBoard), "OnDisable")]
        class OnDisable
        {
            static bool Prefix()
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(DebugKeyBoard), "SendKey")]
        class SendKey
        {
            static bool Prefix(KeyCode Key)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(DebugKeyBoard), "SendKeyDown")]
        class SendKeyDown
        {
            static bool Prefix(KeyCode Key)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(DebugKeyBoard), "SendKeyUp")]
        class SendKeyUp
        {
            static bool Prefix(KeyCode Key)
            {
                return false;
            }
        }
    }
}
