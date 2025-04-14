using HarmonyLib;
using UnityEngine.UI;
using UnityEngine;

namespace UnityPlugin_BepInEx_NHA2
{
    class mnew_game_gui_connect_player_slider
    {
        /// <summary>
        /// Changing the GUI image of the lifebar holder :
        /// Without laser, crosshair and gun, there is no more clue to know which is the selected weapon (blue or red)
        /// Changing the life/energy holder with a red/blue indicator will fix that
        /// </summary>
        [HarmonyPatch(typeof(new_game_gui_connect_player_slider), "update_work_data")]
        class update_work_data
        {
            static void Postfix(new_game_gui_connect_player_slider __instance)
            {
                if (DemulShooter_Plugin.Configurator.RemoveCrosshair && DemulShooter_Plugin.Configurator.RemoveLaser && DemulShooter_Plugin.Configurator.RemoveGuns)
                {
                    game_player game_player = (game_player)__instance.myreal_obj;
                    int PlayerNum = game_player.get_user_num(); // 1 or 2
                    string PlayerGunColor = zhichi_hanshu_gun_wheel_mark_manage.get_gun_wheel_mark_manage().get_gun_type(PlayerNum).ToString();

                    Image component = __instance.get_show_obj().GetComponent<Image>();
                    foreach (Component c in component.transform.parent.gameObject.GetComponentsInChildren<Component>())
                    {
                        if (c.name == "di")
                        {
                            Image img = c.GetComponent<Image>();
                            if (PlayerNum == 1)
                            {
                                if (PlayerGunColor.Contains("RED"))
                                    img.sprite = DemulShooter_Plugin.Sprite_1P_Red;
                                else if (PlayerGunColor.Contains("BLUE"))
                                    img.sprite = DemulShooter_Plugin.Sprite_1P_Blue;
                            }
                            if (PlayerNum == 2)
                            {
                                if (PlayerGunColor.Contains("RED"))
                                    img.sprite = DemulShooter_Plugin.Sprite_2P_Red;
                                else if (PlayerGunColor.Contains("BLUE"))
                                    img.sprite = DemulShooter_Plugin.Sprite_2P_Blue;
                            }
                            break;
                        }
                    }
                    //NightHunterArcade2_Plugin.MyLogger.LogWarning("mnew_game_gui_connect_player_slider.update_work_data() => game_player.get_user_num: " + game_player.get_user_num() + " - color: " + game_player.get_fire_weapon().mybullet_color.ToString() + "config: " + zhichi_hanshu_gun_wheel_mark_manage.get_gun_wheel_mark_manage().get_gun_type(PlayerNum).ToString());
                }
            }
        }
    }
}
