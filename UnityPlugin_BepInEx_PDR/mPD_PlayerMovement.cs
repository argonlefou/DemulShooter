using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_PDR
{
    class mPD_PlayerMovement
    {
        [HarmonyPatch(typeof(PD_PlayerMovement), "Start")]
        class Start
        {
            static void Postfix()
            {
                Cursor.lockState = CursorLockMode.None;
                PD_GameManager.Instance.CursorLocked = false;
            }
        }
    }
}
