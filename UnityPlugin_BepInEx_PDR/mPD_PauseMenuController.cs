using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_PDR
{
    class mPD_PauseMenuController
    {
        [HarmonyPatch(typeof(PD_PauseMenuController), "ResumeGame")]
        class ResumeGame
        {
            static void Postfix()
            {
                Cursor.lockState = CursorLockMode.None;
                PD_GameManager.Instance.CursorLocked = false;
            }
        }
    }
}
