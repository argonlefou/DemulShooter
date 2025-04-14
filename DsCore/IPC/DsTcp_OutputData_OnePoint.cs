using System;

namespace DsCore.IPC
{
    public class DsTcp_OutputData_OnePoint
    {
        public byte IsPlaying = 0;
        public byte Recoil = 0;
        public byte LED_LeftButton = 0;
        public byte LED_RightButton = 0;
        public byte LED_StartButton = 0;
        public byte LED_Safety = 0;
        public byte LED_TapeLed = 0;
        public UInt32 Ammo = 0;
        public UInt32 Credits = 0;

        public static readonly int DATA_LENGTH = 15;

        public DsTcp_OutputData_OnePoint()
        {
        }

        public void Update(byte[] ReceivedData)
        {
            IsPlaying = ReceivedData[0];
            Recoil = ReceivedData[1];
            LED_LeftButton = ReceivedData[2];
            LED_StartButton = ReceivedData[3];
            LED_RightButton = ReceivedData[4];
            LED_Safety = ReceivedData[5];
            LED_TapeLed = ReceivedData[6];
            Ammo = BitConverter.ToUInt32(ReceivedData, 7);
            Credits = BitConverter.ToUInt32(ReceivedData, 11);
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];

            bArray[0] = IsPlaying;
            bArray[1] = Recoil;
            bArray[2] = LED_LeftButton;
            bArray[3] = LED_StartButton;
            bArray[4] = LED_RightButton;
            bArray[5] = LED_Safety;
            bArray[6] = LED_TapeLed;
            Array.Copy(BitConverter.GetBytes(Ammo), 0, bArray, 7, 4);
            Array.Copy(BitConverter.GetBytes(Credits), 0, bArray, 11, 4);

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