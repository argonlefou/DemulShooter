using HarmonyLib;

namespace UnityPlugin_BepInEx_WWS
{
    class mPlayerUIController
    {
        /// <summary>
        /// Send Ammo to DemulShooter
        /// </summary>
        [HarmonyPatch(typeof(PlayerUIController), "ReLoadBullets")]
        class ReLoadBullets
        {
            static bool Prefix(PlayerData ___playerData, int ___maxBullets)
            {
                int r = Demulshooter_Plugin.WWS_Mmf.ReadAll();
                if (r == 0)
                {
                    int iPlayerIndex = (int)___playerData.playerTpye;
                    Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_AMMO + iPlayerIndex] = (byte)___maxBullets;
                    Demulshooter_Plugin.WWS_Mmf.Writeall();
                }
                else
                    UnityEngine.Debug.LogError("mShootController.ReloadBullets() => DemulShooter MMF read error : " + r.ToString());            

                return true;
            }
        }

        /// <summary>
        /// Send Ammo to DemulShooter
        /// </summary>
        [HarmonyPatch(typeof(PlayerUIController), "ReLoadBulletsSlow")]
        class ReLoadBulletsSlow
        {
            static bool Prefix(PlayerData ___playerData, int ___maxBullets)
            {
                int r = Demulshooter_Plugin.WWS_Mmf.ReadAll();
                if (r == 0)
                {
                    int iPlayerIndex = (int)___playerData.playerTpye;
                    Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_AMMO + iPlayerIndex] = (byte)___maxBullets;
                    Demulshooter_Plugin.WWS_Mmf.Writeall();
                }
                else
                    UnityEngine.Debug.LogError("mShootController.ReloadBulletsSlow() => DemulShooter MMF read error : " + r.ToString());

                return true;
            }
        }

        /// <summary>
        /// Send Ammo to DemulShooter
        /// </summary>
        [HarmonyPatch(typeof(PlayerUIController), "DelBullets")]
        class DelBullets
        {
            static void Postfix(PlayerData ___playerData, int ___maxBullets, int ___bulletIndex)
            {
                int r = Demulshooter_Plugin.WWS_Mmf.ReadAll();
                if (r == 0)
                {
                    int iPlayerIndex = (int)___playerData.playerTpye;
                    //Option #1 => Adding a COM message for recoil
                    //The game is doing so in :
                    // - TEST mode recoil test
                    // - Level Select (onShoot)
                    // - Ingame ONLY with FireLoop enabled (in that case there will be a double COM message)
                    BaseCom.Instance.SendTestGun(iPlayerIndex, 1);        
                    //Option #2 => Using Shared Memory to compute recoil with each shoot
                    // -> Issue : no recoil during stage select
                    //Those values are still used to display Ammo count, though
                    Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_AMMO + iPlayerIndex] = (byte)(___maxBullets - ___bulletIndex);
                    Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_RECOIL + iPlayerIndex] = 1;
                    Demulshooter_Plugin.WWS_Mmf.Writeall();
                }
                else
                    UnityEngine.Debug.LogError("mShootController.DelBullets() => DemulShooter MMF read error : " + r.ToString());
            }
        }

        /// <summary>
        /// Send Ammo to DemulShooter
        /// </summary>
        [HarmonyPatch(typeof(PlayerUIController), "OnDestroy")]
        class OnDestroy
        {
            static bool Prefix(PlayerData ___playerData)
            {
                int r = Demulshooter_Plugin.WWS_Mmf.ReadAll();
                if (r == 0)
                {
                    int iPlayerIndex = (int)___playerData.playerTpye;
                    Demulshooter_Plugin.WWS_Mmf.Payload[WWS_MemoryMappedFile_Controller.INDEX_P1_AMMO + iPlayerIndex] = 0;
                    Demulshooter_Plugin.WWS_Mmf.Writeall();
                }
                else
                    UnityEngine.Debug.LogError("mShootController.OnDestroy() => DemulShooter MMF read error : " + r.ToString());

                return true;
            }
        }
    }
}
