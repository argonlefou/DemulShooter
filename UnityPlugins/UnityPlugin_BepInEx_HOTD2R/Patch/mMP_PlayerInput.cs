using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaPixel.Input;
using UnityEngine.InputSystem;

namespace BepInEx_DemulShooter_Plugin
{
    class mMP_PlayerInput
    {
        /// <summary>
        /// Unity only sees one Mouse for all lightguns, so P1 native mouse events would be triggered by any gun (and START keys are also bound to weapon selection)
        /// Blocking the native gameplay actions when DemulShooter is handling the players, they are replaced by our own calls in mCR_Player and DemulShooter_Plugin
        /// Menus, Ready and Skip actions are left untouched
        /// </summary>
        [HarmonyPatch]
        class BlockGameplayActions
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                string[] Actions = new string[]
                {
                    "Fire",
                    "Reload",
                    "CenterAim",
                    "ChangeWeaponInDirection",
                    "ChangeWeaponToNext",
                    "ChangeWeaponToPrev",
                    "SelectWeapon_1",
                    "SelectWeapon_2",
                    "SelectWeapon_3",
                    "SelectWeapon_4",
                    "SelectWeapon_5",
                    "Revive",
                    "BuyContinue"
                };
                foreach (string Action in Actions)
                {
                    yield return AccessTools.Method(typeof(MP_PlayerInput), Action, new Type[] { typeof(InputAction.CallbackContext) });
                }
            }

            static bool Prefix()
            {
                return !DemulShooter_Plugin.EnableInputHack;
            }
        }
    }
}
