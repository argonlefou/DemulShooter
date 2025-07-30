using System;

namespace DsCore.IPC
{
    public class DsTcp_OutputData_WisdomZombies
    {
        public const int MAX_PLAYER = 4;

        public byte[] IsPlaying = new byte[MAX_PLAYER];
        public byte[] StartLamp = new byte[MAX_PLAYER];
        public byte[] SmallWater = new byte[MAX_PLAYER];
        public byte[] BigWater = new byte[MAX_PLAYER];
        public byte[] GunMotor = new byte[MAX_PLAYER];
        public byte[] TicketFeeder = new byte[MAX_PLAYER];
        public byte BonusWeaponLamp;
        public byte SeatVibrationMotor;
        public byte WaterLevelLamp;
        public byte[] Life = new byte[MAX_PLAYER];
        public byte[] Damaged = new byte[MAX_PLAYER];
        public UInt32[] Credits = new UInt32[MAX_PLAYER];

        public static readonly int DATA_LENGTH = 24;

        public DsTcp_OutputData_WisdomZombies()
        {
            BonusWeaponLamp = 0;
            SeatVibrationMotor = 0;
            WaterLevelLamp = 0;
            for (int i = 0; i < MAX_PLAYER; i++)
            {
                IsPlaying[i] = 0;
                StartLamp[i] = 0;
                SmallWater[i] = 0;
                BigWater[i] = 0;
                GunMotor[i] = 0;
                TicketFeeder[i] = 0;
                BonusWeaponLamp = 0;
                Life[i] = 0;
                Damaged[i] = 0;
                Credits[i] = 0;
            }
        }

        public void Update(byte[] ReceivedData)
        {
            for (int i = 0; i < 4; i++)
            {
                IsPlaying[i] = (byte)(ReceivedData[0] >> i & 0x01);
                StartLamp[i] = (byte)(ReceivedData[0] >> (i + 4) & 0x01);
                SmallWater[i] = (byte)(ReceivedData[1] >> i & 0x01);
                BigWater[i] = (byte)(ReceivedData[1] >> (i + 4) & 0x01);
                GunMotor[i] = (byte)(ReceivedData[2] >> i & 0x01);
                Damaged[i] = (byte)(ReceivedData[2] >> (i + 4) & 0x01);
                Life[i] = ReceivedData[3 + i];
                TicketFeeder[i] = (byte)(ReceivedData[7] >> i & 0x01);
                Credits[i] = BitConverter.ToUInt32(ReceivedData, 8 + (4 * i));
            }
            BonusWeaponLamp = (byte)(ReceivedData[7] >> 4 & 0x01);
            SeatVibrationMotor = (byte)(ReceivedData[7] >> 6 & 0x01);
            WaterLevelLamp = (byte)(ReceivedData[7] >> 7 & 0x01);

        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];

            for (int i = 0; i < 4; i++)
            {
                bArray[0] |= (byte)(IsPlaying[i] << i);
                bArray[0] |= (byte)(StartLamp[i] << i + 4);
                bArray[1] |= (byte)(SmallWater[i] << i);
                bArray[1] |= (byte)(BigWater[i] << i + 4);
                bArray[2] |= (byte)(GunMotor[i] << i);
                bArray[2] |= (byte)(Damaged[i] << i + 4);
                bArray[3 + i] |= (byte)(Life[i]);
                bArray[7] |= (byte)(TicketFeeder[i] << i);
                Array.Copy(BitConverter.GetBytes(Credits[i]), 0, bArray, 8 + (4 * i), 4);
            }
            bArray[7] |= (byte)(BonusWeaponLamp << 4);
            bArray[7] |= (byte)(SeatVibrationMotor << 6);
            bArray[7] |= (byte)(WaterLevelLamp << 7);

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

