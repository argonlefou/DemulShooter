using System;

namespace DsCore.IPC
{
    public class DsTcp_OutputData_OpWolfReturn
    {
        public byte P1_Recoil;
        public byte P2_Recoil;
        public byte P1_Damage;
        public byte P2_Damage;
        public byte P1_Life;
        public byte P2_Life;
        public UInt16 P1_Ammo;
        public UInt16 P2_Ammo;

        public static readonly int DATA_LENGTH = 7;

        public DsTcp_OutputData_OpWolfReturn()
        {
            P1_Recoil = 0;
            P2_Recoil = 0;
            P1_Damage = 0;
            P2_Damage = 0;
            P1_Ammo = 0;
            P2_Ammo = 0;
            P1_Life = 0;
            P2_Life = 0;
        }

        public void Update(byte[] ReceivedData)
        {
            P1_Recoil = (byte)(ReceivedData[0] & 0x01);
            P2_Recoil = (byte)(ReceivedData[0] >> 1 & 0x01);
            P1_Damage = (byte)(ReceivedData[0] >> 2 & 0x01);
            P2_Damage = (byte)(ReceivedData[0] >> 3 & 0x01);
            P1_Life = ReceivedData[1];
            P2_Life = ReceivedData[2];
            P1_Ammo = BitConverter.ToUInt16(ReceivedData, 3);
            P2_Ammo = BitConverter.ToUInt16(ReceivedData, 5);
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];
            bArray[0] |= P1_Recoil;
            bArray[0] |= (byte)(P2_Recoil << 1);
            bArray[0] |= (byte)(P1_Damage << 2);
            bArray[0] |= (byte)(P2_Damage << 3);
            bArray[1] = P1_Life;
            bArray[2] = P2_Life;
            Array.Copy(BitConverter.GetBytes(P1_Ammo), 0, bArray, 3, 2);
            Array.Copy(BitConverter.GetBytes(P2_Ammo), 0, bArray, 5, 2);

            return bArray;
        }
    }
}
