﻿
namespace DsCore.MameOutput
{
    public enum OutputId : uint
    {
        P1_LmpStart = 1,
        P2_LmpStart,
        P3_LmpStart,
        P4_LmpStart,
        P1_LmpPanel,
        P2_LmpPanel,
        LmpPanel2,
        Lmp2D3D,
        LmpRoom,
        LmpCoin,
        LmpPanel,        
        LmpBillboard,
        LmpSpeaker,
        LmpDinoHead,
        LmpLogo,
        LmpDinoEyes,
        LmpRoof,
        LmpMarquee,
        LmpDash,
        LmpFoliage,
        LmpSeat_R,
        LmpSeat_G,
        LmpSeat_B,
        LmpBenchLogo,
        LmpSeatBase,
        LmpEstop,
        LmpCompressor,
        LmpRoof_R,
        LmpRoof_G,
        LmpRoof_B,
        LmpLgMarquee,
        LmpSqMarquee,
        LmpWalker_R,
        LmpWalker_G,
        LmpWalker_B,
        LmpWalkerEyes,
        LmpWalkerCeiling,
        LmpPosts,
        Lmp1,
        Lmp2,
        Lmp3,
        Lmp4,
        Lmp5,
        Lmp6,
        LmpLeft,
        LmpSide_R,
        LmpSide_G,
        LmpSide_B,
        LmpRight,
        LmpRear_R,
        LmpRear_G,
        LmpRear_B,
        LmpLBtn,
        LmpMBtn,
        LmpRBtn,
        LmpUpBtn,
        LmpDownBtn,
        LmpCloseBtn,
        LmpCannonBtn,
        LmpCannon_R,
        LmpCannon_G,
        LmpCannon_B,
        P1_LmpGun_R,
        P1_LmpGun_G,
        P1_LmpGun_B,
        P2_LmpGun_R,
        P2_LmpGun_G,
        P2_LmpGun_B,
        P1_LmpWindow_R,
        P1_LmpWindow_G,
        P1_LmpWindow_B,
        P2_LmpWindow_R,
        P2_LmpWindow_G,
        P2_LmpWindow_B,
        P1_LmpGun,
        P2_LmpGun,
        P3_LmpGun,
        P4_LmpGun,
        P1_LmpHolder,
        P2_LmpHolder,
        P3_LmpHolder,
        P4_LmpHolder,
        P1_LmpBreak,
        P2_LmpBreak,
        P1_Lmp_R,
        P1_Lmp_G,
        P1_Lmp_B,
        P2_Lmp_R,
        P2_Lmp_G,
        P2_Lmp_B,
        P1_LmpCard_R,
        P1_LmpCard_G,
        P2_LmpCard_R,
        P2_LmpCard_G,
        P1_LedAmmo1,
        P1_LedAmmo2,
        P2_LedAmmo1,
        P2_LedAmmo2,
        P1_LmpGunGrenadeBtn,
        P2_LmpGunGrenadeBtn,
        P1_LmpGunMolding,
        P2_LmpGunMolding,
        LmpMarqueeBacklight,
        LmpMarqueeUplight,
        LmpUpperCtrlPanel,
        LmpLowerCtrlPanel,
        P1_GunRecoil = 100,
        P2_GunRecoil,
        P3_GunRecoil,
        P4_GunRecoil,
        P1_GunMotor,
        P2_GunMotor,
        P3_GunMotor,
        P4_GunMotor,        
        P1_AirFront = 200,
        P2_AirFront,
        P1_AirRear,
        P2_AirRear,
        Blower_Level,
        P1_Fan,
        P2_Fan,
        VibrationSeat,
        DoorB,
        DoorA,
        ElevatorLedsStatus,
        LmpStop,
        LmpAction,
        LmpReset,
        LmpSpot,
        LmpFloor,
        LmpError,
        LmpSafety,
        P1_LedKills1,
        P1_LedKills2,
        P2_LedKills1,
        P2_LedKills2,
        P1_Whip_R,
        P1_Whip_G,
        P1_Whip_B,
        P2_Whip_R,
        P2_Whip_G,
        P2_Whip_B,
        P1_LmpHead,
        P2_LmpHead,
        P1_LmpFoot,
        P2_LmpFoot,
        P1_LmpFront,  
        P2_LmpFront,
        LmpLever,
        Lmp_Downlight,
        Lmp_DirectHit,
        Lmp_PoliceBar,
        Lmp_GreenTestLight,
        Lmp_RedLight,
        Lmp_WhiteStrobe,
        P1_LmpGunTip,
        P1_LmpGunBack,
        P2_LmpGunTip,
        P2_LmpGunBack,
        BallAgitator_State,
        BallAgitator_Direction,
        P1_BallShooter,
        P2_BallShooter,
        Lmp_Horn_R,
        Lmp_Horn_G,
        Lmp_Horn_B,
        Lmp_LeftBulletMark,
        Lmp_RightBulletMark,
        Lmp_W,
        Lmp_A,
        Lmp_N,
        Lmp_T,
        Lmp_E,
        Lmp_D,
        Lmp_LeftReload,
        Lmp_RightReload,
        Lmp_Payout,
        TicketDrive,
        TicketMeter,


        Credits = 1000,
        P1_CtmLmpStart,
        P2_CtmLmpStart,
        P3_CtmLmpStart,
        P4_CtmLmpStart,
        P1_Ammo,
        P2_Ammo,
        P3_Ammo,
        P4_Ammo,
        P1_Clip,
        P2_Clip,
        P3_Clip,
        P4_Clip,
        P1_CtmRecoil,
        P2_CtmRecoil,
        P3_CtmRecoil,
        P4_CtmRecoil,        
        P1_HealthBar,
        P2_HealthBar,
        P3_HealthBar,
        P4_HealthBar,
        P1_Life,
        P2_Life,
        P3_Life,
        P4_Life,
        P1_Damaged,
        P2_Damaged,
        P3_Damaged,
        P4_Damaged,
        P1_Credits,
        P2_Credits,
        P3_Credits,
        P4_Credits,
        MameOrientation = 12345,
        MamePause
    }
}
