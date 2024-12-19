using System;
using HarmonyLib;
using UnityEngine;

namespace UnityPlugin_BepInEx_WWS
{
    class mGameUIController
    {
        /// <summary>
        /// Displaying cursors InGame
        /// </summary>
        [HarmonyPatch(typeof(GameUIController), "Update")]
        class Update
        {
            static bool Prefix(GameUIController __instance, bool ___isLoadEnd)
            {
                for (int i = 0; i < __instance.gunPoint.Length; ++i)
                {
                    if (GameData.GetPlayerData(i).CanShoot() && !___isLoadEnd)
                    {
                        int r = Demulshooter_Plugin.WWS_Mmf.ReadAll();
                        if (r == 0)
                        {
                            __instance.gunPoint[i].mRectTF.parent.GetComponent<RectTransform>();
                            Vector3 vAxis = new Vector3();
                            vAxis.x = BitConverter.ToSingle(Demulshooter_Plugin.WWS_Mmf.Payload, WWS_MemoryMappedFile_Controller.INDEX_P1_UISCREEN_X + (16 * i));
                            vAxis.y = BitConverter.ToSingle(Demulshooter_Plugin.WWS_Mmf.Payload, WWS_MemoryMappedFile_Controller.INDEX_P1_UISCREEN_Y + (16 * i));
                            __instance.gunPoint[i].mRectTF.anchoredPosition = vAxis;
                        }
                        else
                        {
                            UnityEngine.Debug.LogError("mGameUIController.Update() => DemulShooter MMF read error : " + r.ToString());
                            __instance.gunPoint[i].transform.localPosition = new Vector3(-1000f, -600f);
                        }
                    }
                    else
                        __instance.gunPoint[i].transform.localPosition = new Vector3(-1000f, -600f);
                }
                return false;
            }
        }
    }
}
