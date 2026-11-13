using System;

namespace UnityPlugin_BepInEx_Core
{
    public class TcpOutputData : TcpData
    {
        public byte[] IsPlaying = null;
        public byte[] Recoil = null;
        public byte[] Damaged = null;
        public byte[] Life = null;
        public int[] Ammo = null;
        //Not in the home game : set by an arcade mod plugin, if any
        public byte[] Start_Led = null;
        public int Credits = 0;

        public TcpOutputData(int PlayerNumer) : base(PlayerNumer) { }
    }
}
