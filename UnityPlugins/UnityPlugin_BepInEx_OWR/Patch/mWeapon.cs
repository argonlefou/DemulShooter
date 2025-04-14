using HarmonyLib;
using Virtuallyz.VRShooter.Characters;
using Virtuallyz.VRShooter.Weapons;

namespace OperationWolf_BepInEx_DemulShooter_Plugin
{
    class mWeapon
    {
        /// <summary>
        /// Intercept the shoot event to create Output to DemulShooter
        /// </summary>
        [HarmonyPatch(typeof(Weapon), "Shoot")]
        class Shoot
        {
            static bool Prefix(Character ___owner, Weapon __instance)
            {
                //OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.VRShooter.Weapons.Weapon.Shoot() => " + ___owner.ToString());
                //Owner of the Weapon is a character, and the Character class is overriding the ToString() method :
                //If the owner if a Player, it will return "Player"
                //Else, a NPC name
                if (___owner.ToString().Equals("Player"))
                {
                    //OpWolf_Plugin.MyLogger.LogMessage("Virtuallyz.VRShooter.Weapons.Weapon.Shoot() => ControllerId: " + __instance.ControllerId);
                    lock (DemulShooter_Plugin.MutexLocker_Outputs)
                    {
                        if (__instance.ControllerId == 0)
                            DemulShooter_Plugin.OutputData.P1_Recoil = 1;   
                        else if (__instance.ControllerId == 1)
                            DemulShooter_Plugin.OutputData.P2_Recoil = 1;
                    }
                    DemulShooter_Plugin.SendOutputs();
                }
                return true;
            }
        }
    }
}
