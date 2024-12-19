﻿using HarmonyLib;
using Virtuallyz.VRShooter.Characters;
using Virtuallyz.VRShooter.Modules;
using Virtuallyz.VRShooter.Weapons;


namespace UnityPlugin_BepInEx_OWR
{
    class mInterfaceModule
    {
        /// <summary>
        /// Intercapting call to LifeBar update to get Life value of player
        /// </summary>
        [HarmonyPatch(typeof(InterfaceLifeModule), "UpdateLifebars")]
        class UpdateLifebars
        {
            static bool Prefix(Character character)
            {
                //OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.Modules.InterfaceLifeModule.UpdateLifebars() => " + character.ToString());
                //OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.Modules.InterfaceLifeModule.UpdateLifebars() => currentHP: " + character.CurrentHP + ", isFine: " + character.IsFine + ", is BadlyHurt: " + character.IsBadlyHurt + ", isDead: " + character.IsDead);

                if (character.ToString().Equals("Player"))
                {
                    if ((byte)character.CurrentHP != OpWolf_Plugin.P1_LastLife)
                    {
                        lock (OpWolf_Plugin.MutexLocker_Outputs)
                        {
                            OpWolf_Plugin.OutputData.P1_Life = (byte)character.CurrentHP;
                            OpWolf_Plugin.OutputData.P2_Life = (byte)character.CurrentHP;
                        }
                        OpWolf_Plugin.P1_LastLife = (byte)character.CurrentHP;
                        OpWolf_Plugin.SendOutputs();
                    }             
                }
                return true;
            }
        }

        /// <summary>
        /// Intercapting call to Weapon UI update to get Ammo value of player weapon
        /// </summary>
        [HarmonyPatch(typeof(InterfaceWeaponModule), "UpdateWeaponUIInformation")]
        class UpdateWeaponUIInformation
        {
            static bool Prefix(Weapon weapon)
            {
                if (weapon != null && weapon.Owner.ToString().Equals("Player"))
                {
                    ushort Ammo = 0;
                    if (weapon is ProjectileWeapon )
                    {
                        //OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.Modules.InterfaceWeaponModule.UpdateWeaponUIInformation(): weapon.Name: " + weapon.name + ", weapon.ControllerId: " + weapon.ControllerId);                
                        //OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.Modules.InterfaceLifeModule.UpdateWeaponUIInformation() => Projectile");
                        ProjectileWeapon w = weapon as ProjectileWeapon;
                        Ammo = (ushort)w.CurrentChargerAmmosInt;
                        //OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.Modules.InterfaceLifeModule.UpdateWeaponUIInformation() => CurrentChargerAmmosInt: " + w.CurrentChargerAmmosInt);                        
                    }

                    if (weapon.ControllerId == 0)
                    {
                        if (OpWolf_Plugin.P1_LastAmmo != Ammo)
                        {
                            lock (OpWolf_Plugin.MutexLocker_Outputs)
                            {
                                OpWolf_Plugin.OutputData.P1_Ammo = Ammo;
                            }
                            OpWolf_Plugin.P1_LastAmmo = Ammo;
                            OpWolf_Plugin.SendOutputs();
                        }
                    }
                    else if (weapon.ControllerId == 1)
                    {
                        if (OpWolf_Plugin.P2_LastAmmo != Ammo)
                        {
                            lock (OpWolf_Plugin.MutexLocker_Outputs)
                            {
                                OpWolf_Plugin.OutputData.P2_Ammo = Ammo;
                            }
                            OpWolf_Plugin.P2_LastAmmo = Ammo;
                            OpWolf_Plugin.SendOutputs();
                        }
                    }
                }
                return true;
            }
        }
    }
}
