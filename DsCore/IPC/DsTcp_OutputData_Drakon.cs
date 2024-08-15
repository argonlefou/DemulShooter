using System;

namespace DsCore.IPC
{
    public class DsTcp_OutputData_Drakon
    {
        public byte P1_Recoil;
        public byte P2_Recoil;
        public byte P1_Rumble;
        public byte P2_Rumble;
        public byte P1_Damage;
        public byte P2_Damage;
        public byte P1_StartLED;
        public byte P2_StartLED;
        public byte P1_Credits;
        public byte P2_Credits;

        public static readonly int DATA_LENGTH = 4;

        public DsTcp_OutputData_Drakon()
        {
            P1_Recoil = 0;
            P2_Recoil = 0;
            P1_Rumble = 0;
            P2_Rumble = 0;
            P1_Damage = 0;
            P2_Damage = 0;
            P1_StartLED = 0;
            P2_StartLED = 0;
            P1_Credits = 0;
        }

        public void Update(byte[] ReceivedData)
        {
            P1_Recoil = (byte)(ReceivedData[0] & 0x01);
            P2_Recoil = (byte)(ReceivedData[0] >> 1 & 0x01);
            P1_Rumble = (byte)(ReceivedData[0] >> 2 & 0x01);
            P2_Rumble = (byte)(ReceivedData[0] >> 3 & 0x01);
            P1_Damage = (byte)(ReceivedData[1] >> 2 & 0x01);
            P2_Damage = (byte)(ReceivedData[1] >> 3 & 0x01);
            P1_StartLED = (byte)(ReceivedData[1] & 0x01);
            P2_StartLED = (byte)(ReceivedData[1] >> 1 & 0x01);
            P1_Credits = ReceivedData[2];
            P2_Credits = ReceivedData[3];
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];
            bArray[0] |= P1_Recoil;
            bArray[0] |= (byte)(P2_Recoil << 1);
            bArray[0] |= (byte)(P1_Rumble << 2);
            bArray[0] |= (byte)(P2_Rumble << 3);
            bArray[1] |= (byte)(P1_Damage << 2);
            bArray[1] |= (byte)(P2_Damage << 3);
            bArray[1] |= P1_StartLED;
            bArray[1] |= (byte)(P2_StartLED << 1);
            bArray[2] = P1_Credits;
            bArray[3] = P2_Credits;

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
