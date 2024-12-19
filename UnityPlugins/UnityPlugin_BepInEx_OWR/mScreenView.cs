using HarmonyLib;
using System.Collections.Generic;
using Virtuallyz.VRShooter.IO;
using System.Runtime.CompilerServices;
using Virtuallyz.VRShooter.Characters.Players;

namespace UnityPlugin_BepInEx_OWR
{
    class _mScreenView
    {
        /// <summary>
        /// Force non-detection of Gamepad to not mess with the rest of the game
        /// Game will only display keyboard keys on screen, and Gamepads button will not be used out of DS controls
        /// </summary>
        [HarmonyPatch(typeof(ScreenView), "CheckInputDevicesSolo")]
        class CheckInputDevicesSolo
        {
            static bool Prefix(ScreenView __instance, /*bool ___UseKeyBoard, */ ref PlayerController ___keyboardController, Dictionary<PlayerController, UnityEngine.InputSystem.Gamepad> ___associatedGamepads, PlayerController[] ___controllers)
            {
                if (___keyboardController == null)
                {
                    OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.VRShooter.IO.CheckInputDevicesSolo() => Associate controller 0 to Keyboard");
                }
                ___associatedGamepads.Clear();
                ___keyboardController = ___controllers[0];
                ___controllers[0].SetDevice(UnityEngine.InputSystem.Keyboard.current);

                mSetUseKeyboard.SetUseKeyboard(__instance, true);
                return false;
            }
        }
        [HarmonyPatch(typeof(ScreenView), "SetUseKeyboard")]
        class mSetUseKeyboard
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void SetUseKeyboard(object instance, bool useKeyboard)
            {
                //Used to call the private method    
            }
        }

        /// <summary>
        /// Called for multiplayer Gamepad assignment, this way no Gamepad is found
        /// </summary>
        [HarmonyPatch(typeof(ScreenView), "GetFreeGamepad")]
        class GetFreeGamepad
        {
            static bool Prefix(ref UnityEngine.InputSystem.Gamepad __result)
            {
                __result = null;
                return false;
            }
        }
    }


}


