using System;
using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_RHA
{
    class mPlayerPrefs
    {
        [HarmonyPatch(typeof(PlayerPrefs), "DeleteKey")]
        class DeleteKey
        {
            /// <summary>
            /// Filter Registry Key removing to not remove FullScreen option
            /// </summary>
            /// <returns></returns>
            static bool Prefix(String key)
            {
                UnityEngine.Debug.Log("mPlayerPrefs.DeleteKey(), key=" + key);
                
                if (key.Contains("Screenmanager Is Fullscreen"))
                    return false;
                return true;
            }
        }
    }
}
