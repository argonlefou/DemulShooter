using System;

namespace UnityPlugin_BepInEx_Core
{
    public class TcpOutputData : TcpData
    {
        public byte[] IsPlaying = null;
        public byte[] Light = null;
        public byte[] SelectedWeapon = null;
        public byte[] GunMotor = null;
        public byte[] Recoil = null;
        public byte[] Damaged = null;
        public byte[] Life = null;
        public int[] Ammo = null;
        public int[] Credits = null;

        public TcpOutputData(int PlayerNumer) : base(PlayerNumer) { }
    }
}
