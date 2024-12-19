using System;

namespace UnityPlugin_BepInEx_DCOP
{
    public class TcpOutputData
    {
        public byte GunRecoil;
        public byte DirectHit;
        public byte Police_LightBar;
        public byte GreenTestLight;
        public byte RedLight;
        public byte WhiteStrobe;
        public byte GunLight;
        public byte P1_Life;
        public byte P1_Ammo;

        public static readonly int DATA_LENGTH = 9;

        public TcpOutputData()
        {
            GunRecoil = 0;
            DirectHit = 0;
            Police_LightBar = 0;
            GreenTestLight = 0;
            RedLight = 0;
            WhiteStrobe = 0;
            GunLight = 0;
            P1_Ammo = 0;
            P1_Life = 0;
        }

        public void Update(byte[] ReceivedData)
        {
            GunRecoil = (byte)(ReceivedData[0]);
            DirectHit = (byte)(ReceivedData[1]);
            Police_LightBar = (byte)(ReceivedData[2]);
            GreenTestLight = (byte)(ReceivedData[3]);
            RedLight = (byte)(ReceivedData[4]);
            WhiteStrobe = (byte)(ReceivedData[5]);
            GunLight = (byte)(ReceivedData[6]);
            P1_Ammo = (byte)(ReceivedData[7]);
            P1_Life = (byte)(ReceivedData[8]);
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];
            bArray[0] = GunRecoil;
            bArray[1] = DirectHit;
            bArray[2] = Police_LightBar;
            bArray[3] = GreenTestLight;
            bArray[4] = RedLight;
            bArray[5] = WhiteStrobe;
            bArray[6] = GunLight;
            bArray[7] = P1_Ammo;
            bArray[8] = P1_Life;
            return bArray;
        }

        public override string ToString()
        {
            string s = string.Empty;
            byte[] b = this.ToByteArray();
            for (int i = 0; i < b.Length; i++)
            {
                s += "0x" + b[i].ToString("X2") + " ";
            }
            return s;
        }
    }
}
