using System;

namespace DsCore.IPC
{
    public class DsTcp_InputData_Dcop
    {
        public float P1_X;
        public float P1_Y;
        public byte P1_Trigger;
        public byte HideCrosshairs;

        public static readonly int DATA_LENGTH = 10;

        public DsTcp_InputData_Dcop()
        {
            P1_X = 0.0f;
            P1_Y = 0.0f;
            P1_Trigger = 0;
            HideCrosshairs = 0;
        }

        public void Update(byte[] ReceivedData)
        {
            P1_X = BitConverter.ToSingle(ReceivedData, 0);
            P1_Y = BitConverter.ToSingle(ReceivedData, 4);
            P1_Trigger = ReceivedData[8];
            HideCrosshairs = ReceivedData[9];
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];
            Array.Copy(BitConverter.GetBytes(P1_X), 0, bArray, 0, 4);
            Array.Copy(BitConverter.GetBytes(P1_Y), 0, bArray, 4, 4);
            bArray[8] = P1_Trigger;
            bArray[9] = HideCrosshairs;
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
