using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using SBK;

namespace UnityPlugin_BepInEx_RHA
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Demulshooter_Plugin : BaseUnityPlugin
    {
        public const String pluginGuid = "argonlefou.demulshooter.rha";
        public const String pluginName = "RHA DemulShooter Plugin";
        public const String pluginVersion = "2.0.0.0";

        static String MAPPED_FILE_NAME = "DemulShooter_MMF_Rha";
        static String MUTEX_NAME = "DemulShooter_Mutex_Rha";
        static long MAPPED_FILE_CAPACITY = 2048;
        public static RHA_MemoryMappedFile_Controller RHA_Mmf;

        private float[] _fLifebarArray;
        private int[] _iCreditsArray;

        private int _skipFrame = 0;

        public void Awake()
        {
            Logger.LogMessage("Plugin Loaded");
            Harmony harmony = new Harmony(pluginGuid);

            harmony.PatchAll();

            RHA_Mmf = new RHA_MemoryMappedFile_Controller(MAPPED_FILE_NAME, MUTEX_NAME, MAPPED_FILE_CAPACITY);
            int r = RHA_Mmf.MMFOpen();
            if (r == 0)
            {
                Logger.LogMessage("DemulShooter MMF opened succesfully");
                r = Demulshooter_Plugin.RHA_Mmf.ReadAll();
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
            _iCreditsArray = new int[4];
        }

        // Update() called every Frame, FixedUpdate() called every 0.02sec.
        // Loop function to get data from the Game and pass them to Demulshooter:
        // - Life
        // - Credits
        public void Update()
        {
            //If need to skip some frame to acces Memory and Mutex less often
            if (_skipFrame == 0)
            {
                int r = Demulshooter_Plugin.RHA_Mmf.ReadAll();
                if (r != 0)
                    Logger.LogError("DemulSHooter_Plugin.Update() => DemulShooter MMF read error : " + r.ToString());

                for (int i = 0; i < 4; i++)
                {
                    _fLifebarArray[i] = 0.0f;
                    _iCreditsArray[i] = 0;
                }

                Player[] playerList = Singleton<PlayerManager>.Instance.m_PlayerList;

                for (int i = 0; i < playerList.Length; i++)
                {
                    if (playerList[i].m_IsPlaying && playerList[i].Alive)
                    {
                        if (_fLifebarArray[i] > 0)
                            _fLifebarArray[i] = playerList[i].m_Health;
                    }
                    _iCreditsArray[i] = Singleton<KaboomManager>.Instance.GetCoinCount((ID)i);
                }

                for (int i = 0; i < 4; i++)
                {
                    Array.Copy(BitConverter.GetBytes(_fLifebarArray[i]), 0, Demulshooter_Plugin.RHA_Mmf.Payload, RHA_MemoryMappedFile_Controller.INDEX_P1_LIFE + i * 4, 4);
                    Array.Copy(BitConverter.GetBytes(_iCreditsArray[i]), 0, Demulshooter_Plugin.RHA_Mmf.Payload, RHA_MemoryMappedFile_Controller.INDEX_P1_CREDITS + i * 4, 4);
                }
                Demulshooter_Plugin.RHA_Mmf.Writeall();

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
            if (RHA_Mmf.IsOpened)
                RHA_Mmf.MMFClose();
        }

    }
}
