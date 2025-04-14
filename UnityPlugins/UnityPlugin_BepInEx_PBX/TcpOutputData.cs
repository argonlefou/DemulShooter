using System;

namespace PointBlankX_BepInEx_DemulShooter_Plugin
{
    public class TcpOutputData
    {
        public byte P1_Recoil;
        public byte P2_Recoil;
        public byte P1_StartLED;
        public byte P2_StartLED;
        public byte P1_LED;
        public byte P2_LED;
        public byte P1_Life;
        public byte P2_Life;
        public UInt16 P1_Ammo;
        public UInt16 P2_Ammo;
        public byte Credits;

        public static readonly int DATA_LENGTH = 9;

        public TcpOutputData()
        {
            P1_Recoil = 0;
            P2_Recoil = 0;
            P1_StartLED = 0;
            P2_StartLED = 0;
            P1_LED = 0;
            P2_LED = 0;
            P1_Ammo = 0;
            P2_Ammo = 0;
            P1_Life = 0;
            P2_Life = 0;
            Credits = 0;
        }

        public void Update(byte[] ReceivedData)
        {
            P1_Recoil = (byte)(ReceivedData[0] & 0x01);
            P2_Recoil = (byte)(ReceivedData[0] >> 1 & 0x01);
            P1_StartLED = (byte)(ReceivedData[0] >> 2 & 0x01);
            P2_StartLED = (byte)(ReceivedData[0] >> 3 & 0x01);
            P1_LED = (byte)(ReceivedData[1] & 0x01);
            P2_LED = (byte)(ReceivedData[1] >> 1 & 0x01);
            P1_Life = ReceivedData[2];
            P2_Life = ReceivedData[3];
            P1_Ammo = BitConverter.ToUInt16(ReceivedData, 4);
            P2_Ammo = BitConverter.ToUInt16(ReceivedData, 6);
            Credits = ReceivedData[8];
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];
            bArray[0] |= P1_Recoil;
            bArray[0] |= (byte)(P2_Recoil << 1);
            bArray[0] |= (byte)(P1_StartLED << 2);
            bArray[0] |= (byte)(P2_StartLED << 3);
            bArray[1] |= P1_LED;
            bArray[1] |= (byte)(P2_LED << 1);
            bArray[2] = P1_Life;
            bArray[3] = P2_Life;
            Array.Copy(BitConverter.GetBytes(P1_Ammo), 0, bArray, 4, 2);
            Array.Copy(BitConverter.GetBytes(P2_Ammo), 0, bArray, 6, 2);
            bArray[8] = Credits;

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
