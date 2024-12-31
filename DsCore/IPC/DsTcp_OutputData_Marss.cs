using System;

namespace DsCore.IPC
{
    public class DsTcp_OutputData_Marss
    {
        public byte Flashlight;
        public byte P1_Playing;
        public byte P2_Playing;
        public byte P3_Playing;
        public byte P4_Playing;
        public byte P1_Recoil;
        public byte P2_Recoil;
        public byte P3_Recoil;
        public byte P4_Recoil;
        public byte P1_WeaponId;
        public byte P2_WeaponId;
        public byte P3_WeaponId;
        public byte P4_WeaponId;
        public UInt16 P1_Ammo;
        public UInt16 P2_Ammo;
        public UInt16 P3_Ammo;
        public UInt16 P4_Ammo;
        public UInt16 P1_Credits;
        public UInt16 P2_Credits;
        public UInt16 P3_Credits;
        public UInt16 P4_Credits;

        public static readonly int DATA_LENGTH = 18;

        public DsTcp_OutputData_Marss()
        {
            Flashlight = 0;
            P1_Playing = 0;
            P2_Playing = 0;
            P3_Playing = 0;
            P4_Playing = 0;
            P1_Recoil = 0;
            P2_Recoil = 0;
            P3_Recoil = 0;
            P4_Recoil = 0;            
            P1_WeaponId = 0;
            P2_WeaponId = 0;
            P3_WeaponId = 0;
            P4_WeaponId = 0;
            P1_Ammo = 0;
            P2_Ammo = 0;
            P3_Ammo = 0;
            P4_Ammo = 0;           
            P1_Credits = 0;
            P2_Credits = 0;
            P3_Credits = 0;
            P4_Credits = 0;
        }

        public void Update(byte[] ReceivedData)
        {
            Flashlight = (byte)(ReceivedData[0] >> 7 & 0x01);

            P1_Playing = (byte)(ReceivedData[0] & 0x01);
            P2_Playing = (byte)(ReceivedData[0] >> 1 & 0x01);
            P3_Playing = (byte)(ReceivedData[0] >> 2 & 0x01);
            P4_Playing = (byte)(ReceivedData[0] >> 3 & 0x01);

            P1_Recoil = (byte)(ReceivedData[1] & 0x01);
            P2_Recoil = (byte)(ReceivedData[1] >> 1 & 0x01);
            P3_Recoil = (byte)(ReceivedData[1] >> 2 & 0x01);
            P4_Recoil = (byte)(ReceivedData[1] >> 3 & 0x01);
            P1_WeaponId = (byte)(ReceivedData[1] >> 4 & 0x01);
            P2_WeaponId = (byte)(ReceivedData[1] >> 5 & 0x01);
            P3_WeaponId = (byte)(ReceivedData[1] >> 6 & 0x01);
            P4_WeaponId = (byte)(ReceivedData[1] >> 7 & 0x01);

            P1_Ammo = BitConverter.ToUInt16(ReceivedData, 2);
            P2_Ammo = BitConverter.ToUInt16(ReceivedData, 4);
            P3_Ammo = BitConverter.ToUInt16(ReceivedData, 6);
            P4_Ammo = BitConverter.ToUInt16(ReceivedData, 8);

            P1_Credits = BitConverter.ToUInt16(ReceivedData, 10);
            P2_Credits = BitConverter.ToUInt16(ReceivedData, 12);
            P3_Credits = BitConverter.ToUInt16(ReceivedData, 14);
            P4_Credits = BitConverter.ToUInt16(ReceivedData, 16);
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];
            bArray[0] |= P1_Playing;
            bArray[0] |= (byte)(P2_Playing << 1);
            bArray[0] |= (byte)(P3_Playing << 2);
            bArray[0] |= (byte)(P4_Playing << 3);
            bArray[0] |= (byte)(Flashlight << 7);

            bArray[1] |= P1_Recoil;
            bArray[1] |= (byte)(P2_Recoil << 1);
            bArray[1] |= (byte)(P3_Recoil << 2);
            bArray[1] |= (byte)(P4_Recoil << 3);
            bArray[1] |= (byte)(P2_WeaponId << 4);
            bArray[1] |= (byte)(P2_WeaponId << 5);
            bArray[1] |= (byte)(P3_WeaponId << 6);
            bArray[1] |= (byte)(P4_WeaponId << 7);

            Array.Copy(BitConverter.GetBytes(P1_Ammo), 0, bArray, 2, 2);
            Array.Copy(BitConverter.GetBytes(P2_Ammo), 0, bArray, 4, 2);
            Array.Copy(BitConverter.GetBytes(P3_Ammo), 0, bArray, 6, 2);
            Array.Copy(BitConverter.GetBytes(P4_Ammo), 0, bArray, 8, 2);

            Array.Copy(BitConverter.GetBytes(P1_Credits), 0, bArray, 10, 2);
            Array.Copy(BitConverter.GetBytes(P2_Credits), 0, bArray, 12, 2);
            Array.Copy(BitConverter.GetBytes(P3_Credits), 0, bArray, 14, 2);
            Array.Copy(BitConverter.GetBytes(P4_Credits), 0, bArray, 16, 2);

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
