using HarmonyLib;

namespace UnityPlugin_BepInEx_MarsSortie.Patch
{
    class mSuperDogManager
    {
        /// <summary>
        /// Removing dongle check
        /// </summary>
        [HarmonyPatch(typeof(MRCQ.SuperDogManager), "DoCheckKey")]
        class Update
        {
            static bool Prefix(int featureId, MRCQ.SuperDogManager.EncryptionArray enArr, ref bool __result)
            {
                MarsSortie_BepInEx_Plugin.MyLogger.LogMessage("MRCQ.SuperDogManager.DoCheckKey()");
                __result = true;
                return false;
            }
        }
    }
}
