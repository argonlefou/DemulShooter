using System;

namespace DsCore.IPC
{
    public class DsTcp_InputData_Drakon
    {
        public float P1_X;
        public float P1_Y;
        public float P2_X;
        public float P2_Y;
        public byte P1_Trigger;        
        public byte P2_Trigger;
        public byte HideCrosshairs;
        public byte EnableInputsHack;

        public static readonly int DATA_LENGTH = 17;

        public DsTcp_InputData_Drakon()
        {
            P1_X = 0.0f;
            P1_Y = 0.0f;
            P2_X = 0.0f;
            P2_Y = 0.0f;
            P1_Trigger = 0;
            P2_Trigger = 0;
            HideCrosshairs = 0;
            EnableInputsHack = 0;
        }

        public void Update(byte[] ReceivedData)
        {
            P1_X = BitConverter.ToSingle(ReceivedData, 0);
            P1_Y = BitConverter.ToSingle(ReceivedData, 4);
            P2_X = BitConverter.ToSingle(ReceivedData, 8);
            P2_Y = BitConverter.ToSingle(ReceivedData, 12);
            P1_Trigger = (byte)(ReceivedData[16] & 0x01);
            P2_Trigger = (byte)(ReceivedData[16] >> 1 & 0x01);
            HideCrosshairs = (byte)(ReceivedData[16] >> 3 & 0x01);
            EnableInputsHack = (byte)(ReceivedData[16] >> 2 & 0x01);
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];
            Array.Copy(BitConverter.GetBytes(P1_X), 0, bArray, 0, 4);
            Array.Copy(BitConverter.GetBytes(P1_Y), 0, bArray, 4, 4);
            Array.Copy(BitConverter.GetBytes(P2_X), 0, bArray, 8, 4);
            Array.Copy(BitConverter.GetBytes(P2_Y), 0, bArray, 12, 4);
            bArray[16] |= P1_Trigger;
            bArray[16] |= (byte)(P2_Trigger << 1);
            bArray[16] |= (byte)(HideCrosshairs << 3);
            bArray[16] |= (byte)(EnableInputsHack << 2);
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
