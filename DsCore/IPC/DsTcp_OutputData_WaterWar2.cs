using System;

namespace DsCore.IPC
{
    public class DsTcp_OutputData_WaterWar2
    {
        public const int MAX_PLAYER = 2;

        public byte[] WaterFire = new byte[MAX_PLAYER];
        public byte WaterPump = 0;
        public byte[] StartLed = new byte[MAX_PLAYER];
        public byte[] TicketFeeder = new byte[MAX_PLAYER];
        public byte[] BigGun = new byte[MAX_PLAYER];
        public UInt32[] Credits = new UInt32[MAX_PLAYER];

        public static readonly int DATA_LENGTH = (4 * MAX_PLAYER) + 3;

        public DsTcp_OutputData_WaterWar2()
        {
            for (int i = 0; i < MAX_PLAYER; i++)
            {
                WaterFire[i] = 0;
                WaterPump = 0;
                StartLed[i] = 0;
                TicketFeeder[i] = 0;
                BigGun[i] = 0;
                Credits[i] = 0;
            }
        }

        public void Update(byte[] ReceivedData)
        {
            for (int i = 0; i < MAX_PLAYER; i++)
            {
                WaterFire[i] = (byte)(ReceivedData[0] >> i & 0x01);
                WaterPump = (byte)(ReceivedData[0] >> 7 & 0x01);
                StartLed[i] = (byte)(ReceivedData[1] >> (i * 4) & 0x0F);
                BigGun[i] = (byte)(ReceivedData[2] >> i & 0x01);
                TicketFeeder[i] = (byte)(ReceivedData[2] >> (i + 4) & 0x01);                
                Credits[i] = BitConverter.ToUInt32(ReceivedData, 3 + (4 * i));
            }
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];

            for (int i = 0; i < MAX_PLAYER; i++)
            {
                bArray[0] |= (byte)(WaterFire[i] << i);
                bArray[0] |= (byte)(WaterPump << 7);
                bArray[1] |= (byte)(StartLed[i] << (4 * i));
                bArray[2] |= (byte)(BigGun[i] << i);
                bArray[2] |= (byte)(TicketFeeder[i] << (i + 4));
                Array.Copy(BitConverter.GetBytes(Credits[i]), 0, bArray, 3 + (4 * i), 4);
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
