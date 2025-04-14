using System;
using System.Collections.Generic;
using System.Text;

namespace DsCore.IPC
{
    public class DsTcp_OutputData_MechaTornado
    {
        public const int MAX_PLAYER = 4;

        public byte Atomizer = 0;
        public byte[] IsPlaying = new byte[MAX_PLAYER];
        public byte[] Shake = new byte[MAX_PLAYER];
        public byte[] RotatingMotor = new byte[MAX_PLAYER];
        public byte[] WaterPower = new byte[MAX_PLAYER];
        public byte[] StartLight = new byte[MAX_PLAYER];
        public byte[] PlayerLight = new byte[MAX_PLAYER];
        public byte[] LightBelt = new byte[MAX_PLAYER];
        public byte[] Damaged = new byte[MAX_PLAYER];
        public byte[] Recoil = new byte[MAX_PLAYER];   
        public float[] Life = new float[MAX_PLAYER];     
        public UInt32[] Credits = new UInt32[MAX_PLAYER];

        public static readonly int DATA_LENGTH = 46;

        public DsTcp_OutputData_MechaTornado()
        {
            for (int i = 0; i < MAX_PLAYER; i++)
            {
                IsPlaying[i] = 0;
                Shake[i] = 0;
                Damaged[i] = 0;
                RotatingMotor[i] = 0;
                WaterPower[i] = 0;
                StartLight[i] = 0;
                PlayerLight[i] = 0;
                LightBelt[i] = 0;
                Damaged[i] = 0;
                Recoil[i] = 0;
                Life[i] = 0.0f;
                Credits[i] = 0;
            }
        }

        public void Update(byte[] ReceivedData)
        {
            Atomizer = (byte)(ReceivedData[0] >> 7 & 0x01);
            for (int i = 0; i < MAX_PLAYER; i++)
            {
                IsPlaying[i] = (byte)(ReceivedData[0] >> i & 0x01);
                Damaged[i] = (byte)(ReceivedData[1] >> i & 0x01);
                Recoil[i] = (byte)(ReceivedData[1] >> (i + 4) & 0x01);
                Shake[i] = (byte)(ReceivedData[(3 * i) + 2] & 0x0F);
                RotatingMotor[i] = (byte)(ReceivedData[(3 * i) + 2] >> 4 & 0x0F);
                WaterPower[i] = (byte)(ReceivedData[(3 * i) + 3] & 0x0F);
                StartLight[i] = (byte)(ReceivedData[(3 * i) + 3] >> 4 & 0x0F);
                PlayerLight[i] = (byte)(ReceivedData[(3 * i) + 4] & 0x0F);
                LightBelt[i] = (byte)(ReceivedData[(3 * i) + 4] >> 4 & 0x0F); 
                Life[i] = BitConverter.ToSingle(ReceivedData, 14 + (4 * i));
                Credits[i] = BitConverter.ToUInt32(ReceivedData, 30 + (4 * i));
            }
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];

            bArray[0] |= (byte)(Atomizer << 7);
            for (int i = 0; i < MAX_PLAYER; i++)
            {
                bArray[0] |= (byte)(IsPlaying[i] << i);
                bArray[1] |= (byte)(Damaged[i] << i);
                bArray[1] |= (byte)(Recoil[i] << (i + 4));
                bArray[(3 * i) + 2] |= (byte)(Shake[i]);                
                bArray[(3 * i) + 2] |= (byte)(RotatingMotor[i] << 4);
                bArray[(3 * i) + 3] |= (byte)(WaterPower[i]);
                bArray[(3 * i) + 3] |= (byte)(StartLight[i] << 4);
                bArray[(3 * i) + 4] |= (byte)(PlayerLight[i]);
                bArray[(3 * i) + 4] |= (byte)(LightBelt[i] << 4);                
                Array.Copy(BitConverter.GetBytes(Life[i]), 0, bArray, 14 + (4 * i), 4);
                Array.Copy(BitConverter.GetBytes(Credits[i]), 0, bArray, 30 + (4 * i), 4);
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
