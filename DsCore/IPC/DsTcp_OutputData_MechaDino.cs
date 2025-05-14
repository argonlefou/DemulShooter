using System;

namespace DsCore.IPC
{
    public class DsTcp_OutputData_MechaDino
    {
        public const int MAX_PLAYER = 4;

        public byte[] IsPlaying = new byte[MAX_PLAYER];
        public byte[] BallMotor = new byte[MAX_PLAYER];
        public byte[] StartLamp = new byte[MAX_PLAYER];
        public byte[] PlayerBonusWeaponLamp = new byte[MAX_PLAYER];
        public byte[] SpineMotor = new byte[MAX_PLAYER];
        public byte BonusWeaponLamp;
        public byte SeatVibrationLamp;
        public byte WaterFallLamp;
        public byte WaterLevelLamp;
        public float[] Life = new float[MAX_PLAYER];
        public byte[] Damaged = new byte[MAX_PLAYER];
        public UInt32[] Credits = new UInt32[MAX_PLAYER];

        public static readonly int DATA_LENGTH = 39;

        public DsTcp_OutputData_MechaDino()
        {
            BonusWeaponLamp = 0;
            SeatVibrationLamp = 0;
            WaterFallLamp = 0;
            WaterLevelLamp = 0;
            for (int i = 0; i < MAX_PLAYER; i++)
            {
                IsPlaying[i] = 0;
                BallMotor[i] = 0;
                StartLamp[i] = 0;
                PlayerBonusWeaponLamp[i] = 0;
                SpineMotor[i] = 0;
                Life[i] = 0.0f;
                Damaged[i] = 0;
                Credits[i] = 0;
            }
        }

        public void Update(byte[] ReceivedData)
        {
            for (int i = 0; i < 4; i++)
            {
                IsPlaying[i] = (byte)(ReceivedData[0] >> i & 0x01);
                BallMotor[i] = (byte)(ReceivedData[0] >> (i + 4) & 0x01);
                StartLamp[i] = (byte)(ReceivedData[1] >> i & 0x01);
                PlayerBonusWeaponLamp[i] = (byte)(ReceivedData[1] >> (i + 4) & 0x01);
                Damaged[i] = (byte)(ReceivedData[2] >> i & 0x01);
                SpineMotor[i] = ReceivedData[3 + i];
                Life[i] = BitConverter.ToSingle(ReceivedData, 7 + (4 * i));
                Credits[i] = BitConverter.ToUInt32(ReceivedData, 23 + (4 * i));
            }
            BonusWeaponLamp = (byte)(ReceivedData[2] >> 4 & 0x01);
            SeatVibrationLamp = (byte)(ReceivedData[2] >> 5 & 0x01);
            WaterLevelLamp = (byte)(ReceivedData[2] >> 6 & 0x01);
            WaterLevelLamp = (byte)(ReceivedData[2] >> 7 & 0x01);
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];

            for (int i = 0; i < 4; i++)
            {
                bArray[0] |= (byte)(IsPlaying[i] << i);
                bArray[0] |= (byte)(BallMotor[i] << i + 4);
                bArray[1] |= (byte)(StartLamp[i] << i);
                bArray[1] |= (byte)(PlayerBonusWeaponLamp[i] << i + 4);
                bArray[2] |= (byte)(Damaged[i] << i);   
                bArray[3 + i] = SpineMotor[i];
                Array.Copy(BitConverter.GetBytes(Life[i]), 0, bArray, 7 + (4 * i), 4);
                Array.Copy(BitConverter.GetBytes(Credits[i]), 0, bArray, 23 + (4 * i), 4);
            }

            bArray[2] |= (byte)(BonusWeaponLamp << 4);
            bArray[2] |= (byte)(SeatVibrationLamp << 5);
            bArray[2] |= (byte)(WaterFallLamp << 6);
            bArray[2] |= (byte)(WaterLevelLamp << 7);

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
