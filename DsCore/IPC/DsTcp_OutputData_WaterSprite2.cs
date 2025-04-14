using System;

namespace DsCore.IPC
{
    public class DsTcp_OutputData_WaterSprite2
    {
        public const int MAX_PLAYER = 2;

        public byte[] IsPlaying = new byte[MAX_PLAYER];
        public byte[] WaterFire = new byte[MAX_PLAYER];
        public byte WaterPump = 0;
        public byte GameLed = 0;
        public byte[] StartLed = new byte[MAX_PLAYER];
        public byte[] TicketFeeder = new byte[MAX_PLAYER];
        public byte[] BigGun = new byte[MAX_PLAYER];
        public byte[] Damaged = new byte[MAX_PLAYER];
        public UInt32[] Credits = new UInt32[MAX_PLAYER];

        public static readonly int DATA_LENGTH = 13;

        public DsTcp_OutputData_WaterSprite2()
        {
            WaterPump = 0;
            GameLed = 0;
            for (int i = 0; i < MAX_PLAYER; i++)
            {
                IsPlaying[i] = 0;
                WaterFire[i] = 0;                
                StartLed[i] = 0;
                TicketFeeder[i] = 0;
                BigGun[i] = 0;
                Damaged[i] = 0;
                Credits[i] = 0;
            }
        }

        public void Update(byte[] ReceivedData)
        {
            WaterPump = (byte)(ReceivedData[0] >> 7 & 0x01);
            GameLed = (byte)(ReceivedData[0] & 0x0F);

            for (int i = 0; i < MAX_PLAYER; i++)
            {
                IsPlaying[i] = (byte)(ReceivedData[1] >> i & 0x01);
                WaterFire[i] = (byte)(ReceivedData[1] >> (i + 4) & 0x01);                
                StartLed[i] = (byte)(ReceivedData[2] >> (i * 4) & 0x0F);
                BigGun[i] = (byte)(ReceivedData[3] >> i & 0x01);
                TicketFeeder[i] = (byte)(ReceivedData[3] >> (i + 4) & 0x01);
                Damaged[i] = (byte)(ReceivedData[4] >> i & 0x01);
                Credits[i] = BitConverter.ToUInt32(ReceivedData, 5 + (4 * i));
            }
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];

            bArray[0] |= (byte)(WaterPump << 7);
            bArray[0] |= (byte)GameLed;

            for (int i = 0; i < MAX_PLAYER; i++)
            {
                bArray[1] |= (byte)(IsPlaying[i] << i);
                bArray[1] |= (byte)(WaterFire[i] << (i + 4));
                bArray[2] |= (byte)(StartLed[i] << (4 * i));
                bArray[3] |= (byte)(BigGun[i] << i);
                bArray[3] |= (byte)(TicketFeeder[i] << (i + 4));
                bArray[4] |= (byte)(Damaged[i] << i);
                Array.Copy(BitConverter.GetBytes(Credits[i]), 0, bArray, 5 + (4 * i), 4);
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
