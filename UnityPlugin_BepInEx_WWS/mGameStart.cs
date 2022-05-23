using System.Runtime.CompilerServices;
using HarmonyLib;

namespace UnityPlugin_BepInEx_WWS
{
    class mGameStart
    {
        /// <summary>
        /// Replacing this method force the game to start without waiting for the "C" button
        /// </summary>
        [HarmonyPatch(typeof(GameStart), "FixedUpdate")]
        class FixedUpdate
        {
            static bool Prefix(GameStart __instance)
            {
                mEnterGame.EnterGame(__instance);
                return false;
            }
        }
        
        [HarmonyPatch(typeof(GameStart), "EnterGame")]
        class mEnterGame
        {
            [HarmonyReversePatch]
	        [MethodImpl(MethodImplOptions.NoInlining)]
	        public static void EnterGame(object instance)
	        {
                //Used to call the private method GameStart.EnterGame()      
	        }
        }        
    }
}
