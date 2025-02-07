using System;

namespace DsCore.IPC
{
    public class DsTcp_OutputData_DinoInvasion
    {
        public const int MAX_PLAYER = 4;

        public byte[] IsPlaying = new byte[MAX_PLAYER];
        public byte[] Recoil = new byte[MAX_PLAYER];
        public byte[] Damaged = new byte[MAX_PLAYER];
        public UInt32[] Ammo = new UInt32[MAX_PLAYER];
        public UInt32[] Credits = new UInt32[MAX_PLAYER];

        public static readonly int DATA_LENGTH = (8 * MAX_PLAYER) + 3;

        public DsTcp_OutputData_DinoInvasion()
        {
            for (int i = 0; i < MAX_PLAYER; i++)
            {
                IsPlaying[i] = 0;
                Recoil[i] = 0;
                Damaged[i] = 0;
                Ammo[i] = 0;
                Credits[i] = 0;
            }
        }

        public void Update(byte[] ReceivedData)
        {
            for (int i = 0; i < 4; i++)
            {
                IsPlaying[i] = (byte)(ReceivedData[0] >> i & 0x01);
                Recoil[i] = (byte)(ReceivedData[1] >> i & 0x01);
                Damaged[i] = (byte)(ReceivedData[2] >> i & 0x01);
                Ammo[i] = BitConverter.ToUInt32(ReceivedData, 3 +(4 * i));
                Credits[i] = BitConverter.ToUInt32(ReceivedData, 19 + (4 * i));                
            }
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];

            for (int i = 0; i < 4; i++)
            {
                bArray[0] |= (byte)(IsPlaying[i] << i);
                bArray[1] |= (byte)(Recoil[i] << i);
                bArray[2] |= (byte)(Damaged[i] << i);
                Array.Copy(BitConverter.GetBytes(Ammo[i]), 0, bArray, 3 +(4 * i), 4);
                Array.Copy(BitConverter.GetBytes(Credits[i]), 0, bArray, 19 + (4 * i), 4);
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
