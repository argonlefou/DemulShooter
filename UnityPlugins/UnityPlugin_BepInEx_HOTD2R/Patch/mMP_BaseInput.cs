using HarmonyLib;
using MegaPixel.Input;

namespace BepInEx_DemulShooter_Plugin
{
    class mMP_BaseInput
    {
        /// <summary>
        /// P2 is controlled by DemulShooter and has no Unity controller assigned
        /// Without this, the game is pausing a multiplayer game with a "controller disconnected / disable multiplayer ?" popup
        /// </summary>
        [HarmonyPatch(typeof(MP_BaseInput), "HasAnyInputDevice")]
        class HasAnyInputDevice
        {
            static void Postfix(MP_BaseInput __instance, ref bool __result)
            {
                if (!__result && DemulShooter_Plugin.EnableInputHack)
                {
                    MP_PlayerInput PlayerInput = __instance as MP_PlayerInput;
                    if (PlayerInput != null && PlayerInput.Id == (int)PlayerType.Player2)
                        __result = true;
                }
            }
        }
    }
}
