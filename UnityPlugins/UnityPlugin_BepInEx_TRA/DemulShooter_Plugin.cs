using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using SBK;

namespace TombRaider_BepInEx_DemulShooter_Plugin
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Demulshooter_Plugin : BaseUnityPlugin
    {
        public const String pluginGuid = "argonlefou.demulshooter.tra";
        public const String pluginName = "TombRaider_BepInEx_DemulShooter_Plugin";
        public const String pluginVersion = "3.0.0.0";

        static String MAPPED_FILE_NAME = "DemulShooter_MMF_Tra";
        static String MUTEX_NAME = "DemulShooter_Mutex_Tra";
        static long MAPPED_FILE_CAPACITY = 2048;
        public static TRA_MemoryMappedFile_Controller TRA_Mmf;

        private float[] _fLifebarArray;
        private int[] _iAmmoArray;
        private int[] _iCreditsArray;

        private int _skipFrame = 0;

        public void Awake()
        {
            Logger.LogMessage("Plugin Loaded");
            Harmony harmony = new Harmony(pluginGuid);

            harmony.PatchAll();

            TRA_Mmf = new TRA_MemoryMappedFile_Controller(MAPPED_FILE_NAME, MUTEX_NAME, MAPPED_FILE_CAPACITY);
            int r = TRA_Mmf.MMFOpen();
            if (r == 0)
            {
                Logger.LogMessage("DemulShooter MMF opened succesfully");
                r = Demulshooter_Plugin.TRA_Mmf.ReadAll();
                if (r != 0)
                    Logger.LogError("DemulShooter MMF initial read error : " + r.ToString());
                else
                    Logger.LogError("DemulShooter MMF initial read success)");
            }
            else
            {
                Logger.LogError("DemulShooter MMF open error : " + r.ToString());
            }

            _fLifebarArray = new float[4];
            _iAmmoArray = new int[4];
            _iCreditsArray = new int[4];
        }

        // Update() called every Frame, FixedUpdate() called every 0.02sec.
        // Loop function to get data from the Game and pass them to Demulshooter:
        // - Life
        // - Ammo
        // - Credits
        // Crosshair handling is done in PlayerManager.EnableAllCrossHairs(). This or put them out of boundaries with demulshooter
        // Keyboard keys are handled in SBKInputManager
        // Coins keys are handled in KaboomManager.Update() - All other keys too (?) test rumble too
        public void Update()
        {
            //If need to skip some frame to acces Memory and Mutex less often
            if (_skipFrame == 0)
            {
                int r = Demulshooter_Plugin.TRA_Mmf.ReadAll();
                if (r != 0)
                    Logger.LogError("DemulShooter_Plugin.Update() => DemulShooter MMF read error : " + r.ToString());

                for (int i = 0; i < 4; i++)
                {
                    _fLifebarArray[i] = 0.0f;
                    _iAmmoArray[i] = 0;
                    _iCreditsArray[i] = 0;
                }

                Player[] playerList = Singleton<PlayerManager>.Instance.m_PlayerList;

                for (int i = 0; i < playerList.Length; i++)
                {
                    if (!Singleton<GameplayManager>.Instance.IsDemo)
                    {
                        if (playerList[i].IsPlaying())
                        {
                            if (playerList[i].Health > 0)
                                _fLifebarArray[i] = playerList[i].Health;
                            _iAmmoArray[i] = playerList[i].CurrentGun.ClipRemaining;
                        }
                    }
                    _iCreditsArray[i] = Singleton<KaboomManager>.Instance.GetCoinCount((ID)i);
                }

                for (int i = 0; i < 4; i++)
                {
                    
                    Array.Copy(BitConverter.GetBytes(_fLifebarArray[i]), 0, Demulshooter_Plugin.TRA_Mmf.Payload, TRA_MemoryMappedFile_Controller.INDEX_P1_LIFE + i * 4, 4);
                    Array.Copy(BitConverter.GetBytes(_iAmmoArray[i]), 0, Demulshooter_Plugin.TRA_Mmf.Payload, TRA_MemoryMappedFile_Controller.INDEX_P1_AMMO + i * 4, 4);
                    Array.Copy(BitConverter.GetBytes(_iCreditsArray[i]), 0, Demulshooter_Plugin.TRA_Mmf.Payload, TRA_MemoryMappedFile_Controller.INDEX_P1_CREDITS + i * 4, 4);

                }
                Demulshooter_Plugin.TRA_Mmf.Writeall();

                _skipFrame = 0;
            }
            else
                _skipFrame++;
        }

        private void HarmonyPatch(Harmony hHarmony, Type OriginalClass, String OriginalMethod, Type ReplacementClass, String ReplacementMethod)
        {
            MethodInfo original = AccessTools.Method(OriginalClass, OriginalMethod);
            MethodInfo patch = AccessTools.Method(ReplacementClass, ReplacementMethod);
            hHarmony.Patch(original, new HarmonyMethod(patch));
        }

        public void OnDestroy()
        {
            Logger.LogMessage("Closing Demulshooter MMF....");
            if (TRA_Mmf.IsOpened)
                TRA_Mmf.MMFClose();
        }

    }
}
