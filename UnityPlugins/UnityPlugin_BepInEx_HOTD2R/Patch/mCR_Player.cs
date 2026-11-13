using HarmonyLib;
using UnityEngine;
using UnityPlugin_BepInEx_Core;

namespace BepInEx_DemulShooter_Plugin
{
    class mCR_Player
    {
        //Like the game's Fire action : only true after a trigger press made during the gameplay, until the trigger is released
        //(a trigger still held at the end of a cutscene must not start the auto-fire)
        internal static readonly bool[] IsHoldingFire = new bool[DemulShooter_Plugin.MAX_PLAYERS];

        private static readonly AccessTools.FieldRef<CR_Crosshair, int> _CrosshairLastRayUpdateFrame = AccessTools.FieldRefAccess<CR_Crosshair, int>("lastRayUpdateFrame");

        /// <summary>
        /// Moving the player's crosshair to DemulShooter's position
        /// The crosshair is on a Screen Space Overlay canvas, and the game is using its position as screen point for the shooting raycast
        /// </summary>
        private static void setCrosshairPosition(CR_Player Player)
        {
            if (CR_ReadyCheckPanel.ArePlayersReady && !Player.HasDied && Player.Crosshair != null)
            {
                Vector3 vAim = DemulShooter_Plugin.PluginControllers[Player.Id].GetAimingPosition();
                Transform CrosshairTransform = Player.Crosshair.transform;
                CrosshairTransform.position = new Vector3(vAim.x, vAim.y, CrosshairTransform.position.z);
                //The game is computing the shooting ray once per frame : computing it again from the new position
                _CrosshairLastRayUpdateFrame(Player.Crosshair) = -1;
            }
        }

        /// <summary>
        /// Original game is only using relative mouse/gamepad movement for the crosshair
        /// Replacing it by DemulShooter absolute screen position for each player
        /// </summary>
        [HarmonyPatch(typeof(CR_Player), "handleMovingCrosshair")]
        class handleMovingCrosshair
        {
            static bool Prefix(CR_Player __instance)
            {
                if (!DemulShooter_Plugin.EnableInputHack)
                    return true;

                setCrosshairPosition(__instance);
                return false;
            }
        }

        /// <summary>
        /// Same as original, but reading the trigger state from DemulShooter instead of the player's controller
        /// </summary>
        [HarmonyPatch(typeof(CR_Player), "handleAutoFire")]
        class handleAutoFire
        {
            static bool Prefix(CR_Player __instance, ref bool ___isAutoFireBlocked)
            {
                if (!DemulShooter_Plugin.EnableInputHack)
                    return true;

                CR_WeaponHolder WeaponHolder = __instance.WeaponHolder;
                if (WeaponHolder == null)
                    return false;

                if (IsHoldingFire[__instance.Id] && WeaponHolder.CurrentWeapon != null && WeaponHolder.CurrentWeapon.ShootMode == CR_Weapon.FireMode.AutoFire)
                {
                    if (___isAutoFireBlocked)
                    {
                        ___isAutoFireBlocked = false;
                        if (__instance.OnAutoFireBlockChanged != null)
                            __instance.OnAutoFireBlockChanged(false);
                    }
                    else
                    {
                        WeaponHolder.FireWeapon();
                    }
                }
                else
                {
                    WeaponHolder.ResetFiredState();
                }
                return false;
            }
        }

        /// <summary>
        /// Single shots, reload and weapon change with DemulShooter buttons
        /// Native events for these actions are blocked in mMP_PlayerInput
        /// </summary>
        [HarmonyPatch(typeof(CR_Player), "Update")]
        class Update
        {
            static void Prefix(CR_Player __instance)
            {
                if (!DemulShooter_Plugin.EnableInputHack)
                    return;

                PluginController Controller = DemulShooter_Plugin.PluginControllers[__instance.Id];
                bool bTrigger = Controller.GetButtonDown(PluginController.MyInputButtons.Trigger);
                bool bReload = Controller.GetButtonDown(PluginController.MyInputButtons.Reload);
                bool bAction = Controller.GetButtonDown(PluginController.MyInputButtons.Action);

                //Cutscenes, pause menu, etc... are disabling the Gameplay input map
                if (!DemulShooter_Plugin.IsGameplayInputActive() || !Controller.GetButton(PluginController.MyInputButtons.Trigger))
                    IsHoldingFire[__instance.Id] = false;
                if (!DemulShooter_Plugin.IsGameplayInputActive())
                    return;

                if (bTrigger)
                {
                    IsHoldingFire[__instance.Id] = true;
                    //The original Update is moving the crosshair after this prefix : the shot must use the current gun position
                    setCrosshairPosition(__instance);
                    __instance.Fire();
                }
                if (bReload)
                    __instance.Reload();
                if (bAction)
                    __instance.SwitchWeaponToNext();
            }
        }
    }
}
