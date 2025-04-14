using System;

namespace RabbidsHollywood_BepInEx_DemulShooter_Plugin
{
    public class TcpOutputData
    {
        public const int MAX_PLAYER = 4;

        public byte[] Recoil = new byte[MAX_PLAYER];
        public byte[] Damaged = new byte[MAX_PLAYER];
        public float[] Life = new float[MAX_PLAYER];
        public UInt32[] Ammo = new UInt32[MAX_PLAYER];
        public UInt32[] Credits = new UInt32[MAX_PLAYER];

        //Each player has 12 Bytes for Ammo/Credit/Life, and the 2 bytes left handles all players
        public static readonly int DATA_LENGTH = (12 * MAX_PLAYER) + 2;

        public TcpOutputData()
        {
            for (int i = 0; i < MAX_PLAYER; i++)
            {
                Recoil[i] = 0;
                Damaged[i] = 0;
                Life[i] = 0;
                Ammo[i] = 0;
                Credits[i] = 0;
            }
        }

        public void Update(byte[] ReceivedData)
        {
            for (int i = 0; i < 4; i++)
            {
                Recoil[i] = (byte)(ReceivedData[0] >> i & 0x01);
                Damaged[i] = (byte)(ReceivedData[1] >> i & 0x01);
                Life[i] = BitConverter.ToSingle(ReceivedData, 2 + (4 * i));
                Ammo[i] = BitConverter.ToUInt32(ReceivedData, 18 + (4 * i));
                Credits[i] = BitConverter.ToUInt32(ReceivedData, 34 + (4 * i));                
            }
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];

            for (int i = 0; i < 4; i++)
            {
                bArray[0] |= (byte)(Recoil[i] << i);
                bArray[1] |= (byte)(Damaged[i] << i);
                Array.Copy(BitConverter.GetBytes(Life[i]), 0, bArray, 2 + (4 * i), 4);
                Array.Copy(BitConverter.GetBytes(Ammo[i]), 0, bArray, 18 +(4 * i), 4);
                Array.Copy(BitConverter.GetBytes(Credits[i]), 0, bArray, 34 + (4 * i), 4);
            }
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
