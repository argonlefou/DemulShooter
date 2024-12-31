using System;

namespace UnityPlugin_BepInEx_MarsSortie
{
    public class TcpInputData
    {
        public float P1_X;
        public float P1_Y;
        public float P2_X;
        public float P2_Y;
        public float P3_X;
        public float P3_Y;
        public float P4_X;
        public float P4_Y;
        public byte P1_Trigger;
        public byte P2_Trigger;
        public byte P3_Trigger;
        public byte P4_Trigger;
        public byte P1_ChangeWeapon;
        public byte P2_ChangeWeapon;
        public byte P3_ChangeWeapon;
        public byte P4_ChangeWeapon;
        public byte HideCrosshairs;
        public byte EnableInputsHack;

        public static readonly int DATA_LENGTH = 34;

        public TcpInputData()
        {
            P1_X = 0.0f;
            P1_Y = 0.0f;
            P2_X = 0.0f;
            P2_Y = 0.0f;
            P3_X = 0.0f;
            P3_Y = 0.0f; 
            P4_X = 0.0f;
            P4_Y = 0.0f;
            P1_Trigger = 0;
            P2_Trigger = 0;
            P3_Trigger = 0;
            P4_Trigger = 0;
            P1_ChangeWeapon = 0;
            P2_ChangeWeapon = 0;
            P3_ChangeWeapon = 0;
            P4_ChangeWeapon = 0;
            HideCrosshairs = 0;
            EnableInputsHack = 0;
        }

        public void Update(byte[] ReceivedData)
        {
            P1_X = BitConverter.ToSingle(ReceivedData, 0);
            P1_Y = BitConverter.ToSingle(ReceivedData, 4);
            P2_X = BitConverter.ToSingle(ReceivedData, 8);
            P2_Y = BitConverter.ToSingle(ReceivedData, 12);
            P3_X = BitConverter.ToSingle(ReceivedData, 16);
            P3_Y = BitConverter.ToSingle(ReceivedData, 20); 
            P4_X = BitConverter.ToSingle(ReceivedData, 24);
            P4_Y = BitConverter.ToSingle(ReceivedData, 28);
            P1_Trigger = (byte)(ReceivedData[32] & 0x01);
            P2_Trigger = (byte)(ReceivedData[32] >> 1 & 0x01);
            P3_Trigger = (byte)(ReceivedData[32] >> 2 & 0x01);
            P4_Trigger = (byte)(ReceivedData[32] >> 3 & 0x01);
            P1_ChangeWeapon = (byte)(ReceivedData[32] >> 4 & 0x01);
            P2_ChangeWeapon = (byte)(ReceivedData[32] >> 5 & 0x01);
            P3_ChangeWeapon = (byte)(ReceivedData[32] >> 6 & 0x01);
            P4_ChangeWeapon = (byte)(ReceivedData[32] >> 7 & 0x01);
            HideCrosshairs = (byte)(ReceivedData[33] & 0x01);
            EnableInputsHack = (byte)(ReceivedData[33] >> 1 & 0x01);
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];
            Array.Copy(BitConverter.GetBytes(P1_X), 0, bArray, 0, 4);
            Array.Copy(BitConverter.GetBytes(P1_Y), 0, bArray, 4, 4);
            Array.Copy(BitConverter.GetBytes(P2_X), 0, bArray, 8, 4);
            Array.Copy(BitConverter.GetBytes(P2_Y), 0, bArray, 12, 4);
            Array.Copy(BitConverter.GetBytes(P3_X), 0, bArray, 16, 4);
            Array.Copy(BitConverter.GetBytes(P3_Y), 0, bArray, 20, 4);
            Array.Copy(BitConverter.GetBytes(P4_X), 0, bArray, 24, 4);
            Array.Copy(BitConverter.GetBytes(P4_Y), 0, bArray, 28, 4);
            bArray[32] |= P1_Trigger;
            bArray[32] |= (byte)(P2_Trigger << 1);
            bArray[32] |= (byte)(P3_Trigger << 2);
            bArray[32] |= (byte)(P4_Trigger << 3);
            bArray[32] |= (byte)(P1_ChangeWeapon << 4);
            bArray[32] |= (byte)(P2_ChangeWeapon << 5);
            bArray[32] |= (byte)(P3_ChangeWeapon << 6);
            bArray[32] |= (byte)(P4_ChangeWeapon << 7);
            bArray[33] |= (byte)(HideCrosshairs);
            bArray[33] |= (byte)(EnableInputsHack << 1);
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
