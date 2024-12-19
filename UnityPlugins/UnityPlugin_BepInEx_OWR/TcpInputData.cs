﻿using System;

namespace UnityPlugin_BepInEx_OWR
{
    public class TcpInputData
    {
        public UInt16 P1_X;
        public UInt16 P1_Y;
        public byte P1_Trigger;
        public byte P1_Reload;
        public byte P1_Grenade;
        public byte P1_ChangeWeapon;
        public UInt16 P2_X;
        public UInt16 P2_Y;
        public byte P2_Trigger;
        public byte P2_Reload;
        public byte P2_Grenade;
        public byte P2_ChangeWeapon;
        public byte HideCrosshair = 0;

        public static readonly int DATA_LENGTH = 10;

        public TcpInputData()
        {
            P1_X = 0;
            P1_Y = 0;
            P1_Trigger = 0;
            P1_Reload = 0;
            P1_Grenade = 0;
            P1_ChangeWeapon = 0;
            P2_X = 0;
            P2_Y = 0;
            P2_Trigger = 0;
            P2_Reload = 0;
            P2_Grenade = 0;
            P2_ChangeWeapon = 0;
            HideCrosshair = 0;
        }

        public void Update(byte[] ReceivedData)
        {
            P1_X = BitConverter.ToUInt16(ReceivedData, 0);
            P1_Y = BitConverter.ToUInt16(ReceivedData, 2);
            P2_X = BitConverter.ToUInt16(ReceivedData, 4);
            P2_Y = BitConverter.ToUInt16(ReceivedData, 6);
            P1_Trigger = (byte)(ReceivedData[8] & 0x01);
            P1_Reload = (byte)(ReceivedData[8] >> 1 & 0x01);
            P1_Grenade = (byte)(ReceivedData[8] >> 2 & 0x01);
            P1_ChangeWeapon = (byte)(ReceivedData[8] >> 3 & 0x01);
            P2_Trigger = (byte)(ReceivedData[8] >> 4 & 0x01);
            P2_Reload = (byte)(ReceivedData[8] >> 5 & 0x01);
            P2_Grenade = (byte)(ReceivedData[8] >> 6 & 0x01);
            P2_ChangeWeapon = (byte)(ReceivedData[8] >> 7 & 0x01);
            HideCrosshair = ReceivedData[9];
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];
            Array.Copy(BitConverter.GetBytes(P1_X), 0, bArray, 0, 2);
            Array.Copy(BitConverter.GetBytes(P1_Y), 0, bArray, 2, 2);
            Array.Copy(BitConverter.GetBytes(P2_X), 0, bArray, 4, 2);
            Array.Copy(BitConverter.GetBytes(P2_Y), 0, bArray, 6, 2);
            bArray[8] |= P1_Trigger;
            bArray[8] |= (byte)(P1_Reload << 1);
            bArray[8] |= (byte)(P1_Grenade << 2);
            bArray[8] |= (byte)(P1_ChangeWeapon << 3);
            bArray[8] |= (byte)(P2_Trigger << 4);
            bArray[8] |= (byte)(P2_Reload << 5);
            bArray[8] |= (byte)(P2_Grenade << 6);
            bArray[8] |= (byte)(P2_ChangeWeapon << 7);
            bArray[9] = HideCrosshair;

            return bArray;
        }

        public override string ToString()
        {
            string s = string.Empty;
            byte[] buffer = this.ToByteArray();
            for (int i = 0; i < buffer.Length; i++)
            {
                s += "0x" + buffer[i].ToString("X2") + " ";
            }
            return s;
        }
    }
}
