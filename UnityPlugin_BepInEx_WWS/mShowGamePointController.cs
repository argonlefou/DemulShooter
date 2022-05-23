using System;
using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_WWS
{
    class mShowGamePointController
    {
        /// <summary>
        /// Displaying cursors on Level Select screen
        /// </summary>
        [HarmonyPatch(typeof(ShowGamePointController), "Update")]
        class Update
        {
            static bool Prefix(ShowGamePointController __instance)
            {
                for (int index = 0; index < __instance.gunPoint.Length; ++index)
                { 
                    if (GameData.GetPlayerData(index).CanShoot())
                    {
                        int r = Demulshooter_Plugin.WWS_Mmf.ReadAll();
                        if (r == 0)
                        {
                            Vector3 vAxis = new Vector3();
                            vAxis.x = BitConverter.ToSingle(Demulshooter_Plugin.WWS_Mmf.Payload, WWS_MemoryMappedFile_Controller.INDEX_P1_UISCREEN_X + (16 * index));
                            vAxis.y = BitConverter.ToSingle(Demulshooter_Plugin.WWS_Mmf.Payload, WWS_MemoryMappedFile_Controller.INDEX_P1_UISCREEN_Y + (16 * index));
                            __instance.gunPoint[index].mRectTF.anchoredPosition = vAxis;
                        }
                        else
                        {
                            UnityEngine.Debug.LogError("mShowGamePointController.Update() => DemulShooter MMF read error : " + r.ToString());
                            __instance.gunPoint[index].transform.localPosition = new Vector3(-1000f, -1000f);
                        }
                    }
                    else
                        __instance.gunPoint[index].transform.localPosition = new Vector3(-1000f, -1000f);
                }
                return false;
            }
        }
    }
}
