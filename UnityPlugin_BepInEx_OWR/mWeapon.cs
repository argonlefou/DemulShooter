using HarmonyLib;
using Virtuallyz.VRShooter.Characters;
using Virtuallyz.VRShooter.Weapons;

namespace UnityPlugin_BepInEx_OWR
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
                    lock (OpWolf_Plugin.MutexLocker_Outputs)
                    {
                        if (__instance.ControllerId == 0)
                            OpWolf_Plugin.OutputData.P1_Recoil = 1;   
                        else if (__instance.ControllerId == 1)
                            OpWolf_Plugin.OutputData.P2_Recoil = 1;
                    }
                    OpWolf_Plugin.SendOutputs();
                }
                return true;
            }
        }
    }
}
