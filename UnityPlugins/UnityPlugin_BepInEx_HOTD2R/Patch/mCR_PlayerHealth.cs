using HarmonyLib;

namespace BepInEx_DemulShooter_Plugin
{
    class mCR_PlayerHealth
    {
        /// <summary>
        /// Result is true only if the damage is really applied (no invulnerability, cheat...). That's also where the game is playing its own gamepad vibration
        /// Used to generate our own "Damaged" output
        /// </summary>
        [HarmonyPatch(typeof(CR_PlayerHealth), "TakeDamage", new System.Type[] { typeof(CR_IDamage) })]
        class TakeDamage
        {
            static void Postfix(CR_PlayerHealth __instance, bool __result)
            {
                if (__result && __instance.Owner != null)
                {
                    DemulShooter_Plugin.MyLogger.LogMessage("CR_PlayerHealth.TakeDamage() => " + __instance.Owner.Type.ToString() + ", HP left : " + __instance.CurrentHP.ToString());
                    DemulShooter_Plugin.OutputData.Damaged[__instance.Owner.Id] = 1;
                }
            }
        }
    }
}
