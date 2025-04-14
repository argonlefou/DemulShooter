using System.Reflection;
using HarmonyLib;
using Pvz;
using UnityEngine;

namespace PvZ_BepInEx_DemulShooter_Plugin
{
    class mPvzCore
    {
        /// <summary>
        /// Force the game position data to be filled with our own
        /// </summary>
        [HarmonyPatch(typeof(PvzCore), "UpdateInputPosition")]
        class UpdateInputPosition
        {
            static void Postfix(Pvz.PvzCore __instance)
            {
                if (DemulShooter_Plugin.EnableInputHack)
                {
                    PropertyInfo[] Pis = __instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    foreach (PropertyInfo Pi in Pis)
                    {
                        if (Pi.Name.Equals("inputPosition"))
                        {
                            Pi.SetValue(__instance, new Vector3(DemulShooter_Plugin.PluginPlayerController.Axis_X, DemulShooter_Plugin.PluginPlayerController.Axis_Y), null);
                            break;
                        }
                    }
                    foreach (PropertyInfo Pi in Pis)
                    {
                        if (Pi.Name.Equals("normalizedInputPosition"))
                        {
                            Pi.SetValue(__instance, new Vector3(__instance.inputPosition.x / __instance.screenSize.x, __instance.inputPosition.y / __instance.screenSize.y), null);
                            break;
                        }
                    }
                }
            }
        }
    }
}
