using System;
using System.Collections.Generic;
using System.Text;

namespace DsCore.IPC
{
    public class DsTcp_OutputData_MissionImpossible
    {
        public const int MAX_PLAYER = 2;

        public byte[] IsPlaying = new byte[MAX_PLAYER];
        public byte[] Recoil = new byte[MAX_PLAYER];
        public byte[] Damaged = new byte[MAX_PLAYER];
        public UInt32[] Life = new UInt32[MAX_PLAYER];
        public UInt32[] AmmoGunL = new UInt32[MAX_PLAYER];
        public UInt32[] AmmoGunR = new UInt32[MAX_PLAYER];
        public UInt32 Credits;

        public static readonly int DATA_LENGTH = (12 * MAX_PLAYER) + 7;

        public DsTcp_OutputData_MissionImpossible()
        {
            for (int i = 0; i < MAX_PLAYER; i++)
            {
                IsPlaying[i] = 0;
                Recoil[i] = 0;
                Damaged[i] = 0;
                Life[i] = 0;
                AmmoGunL[i] = 0;
                AmmoGunR[i] = 0;
            }
            Credits = 0;
        }

        public void Update(byte[] ReceivedData)
        {
            for (int i = 0; i < MAX_PLAYER; i++)
            {
                IsPlaying[i] = (byte)(ReceivedData[0] >> i & 0x01);
                Recoil[i] = (byte)(ReceivedData[1] >> i & 0x01);
                Damaged[i] = (byte)(ReceivedData[2] >> i & 0x01);
                Life[i] = BitConverter.ToUInt32(ReceivedData, 3 + (4 * i));
                AmmoGunL[i] = BitConverter.ToUInt32(ReceivedData, 11 + (4 * i));
                AmmoGunR[i] = BitConverter.ToUInt32(ReceivedData, 19 + (4 * i)); 
            }
            Credits = BitConverter.ToUInt32(ReceivedData, 27);
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];

            for (int i = 0; i < MAX_PLAYER; i++)
            {
                bArray[0] |= (byte)(IsPlaying[i] << i);
                bArray[1] |= (byte)(Recoil[i] << i);
                bArray[2] |= (byte)(Damaged[i] << i);
                Array.Copy(BitConverter.GetBytes(Life[i]), 0, bArray, 3 + (4 * i), 4);
                Array.Copy(BitConverter.GetBytes(AmmoGunL[i]), 0, bArray, 11 + (4 * i), 4);
                Array.Copy(BitConverter.GetBytes(AmmoGunR[i]), 0, bArray, 19 + (4 * i), 4); 
            }
            Array.Copy(BitConverter.GetBytes(Credits), 0, bArray, 27, 4);
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
