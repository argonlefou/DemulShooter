using HarmonyLib;

namespace UnityPlugin_BepInEx_PBX
{
    class mErrorMessageHandler
    {
        /// <summary>
        /// Remove the COIN ERROR message displayed when START button are pressed
        /// </summary>
        [HarmonyPatch(typeof(ErrorMessageHandler), "setCoinError")]
        class setCoinError
        {
            static bool Prefix(ref bool isError)
            {
                isError = false;
                return true;
            }
        }
        [HarmonyPatch(typeof(ErrorMessageHandler), "setServiceError")]
        class setServiceError
        {
            static bool Prefix(ref bool isError)
            {
                isError = false;
                return true;
            }
        }
    }
}
