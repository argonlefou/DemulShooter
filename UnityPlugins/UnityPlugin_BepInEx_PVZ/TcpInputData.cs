using System;

namespace PvZ_BepInEx_DemulShooter_Plugin
{
    public class TcpInputData
    {
        public float P1_X;
        public float P1_Y;
        public byte P1_Trigger;
        public byte HideCrosshairs;
        public byte EnableInputsHack;

        public static readonly int DATA_LENGTH = 11;

        public TcpInputData()
        {
            P1_X = 0.0f;
            P1_Y = 0.0f;
            P1_Trigger = 0;
            HideCrosshairs = 0;
            EnableInputsHack = 0;
        }

        public void Update(byte[] ReceivedData)
        {
            P1_X = BitConverter.ToSingle(ReceivedData, 0);
            P1_Y = BitConverter.ToSingle(ReceivedData, 4);
            P1_Trigger = (byte)(ReceivedData[8] & 0x01);
            HideCrosshairs = (byte)(ReceivedData[9] & 0x01);
            EnableInputsHack = (byte)(ReceivedData[9] >> 1 & 0x01);
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];
            Array.Copy(BitConverter.GetBytes(P1_X), 0, bArray, 0, 4);
            Array.Copy(BitConverter.GetBytes(P1_Y), 0, bArray, 4, 4);
            bArray[8] |= P1_Trigger;
            bArray[9] |= (byte)(HideCrosshairs);
            bArray[9] |= (byte)(EnableInputsHack << 1);
            return bArray;
        }

        public override string ToString()
        {
            string r = string.Empty;
            byte[] b = this.ToByteArray();
            for (int i = 0; i < b.Length; i++)
            {
                r += "0x" + b[i].ToString("X2") + " ";
            }
            return r;
        }
    }
}
