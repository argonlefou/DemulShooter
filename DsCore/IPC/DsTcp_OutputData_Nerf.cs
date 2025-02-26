using System;
using System.Collections.Generic;
using System.Text;

namespace DsCore.IPC
{
    public class DsTcp_OutputData_Nerf
    {
        public const int MAX_PLAYER = 4;

        public byte[] IsPlaying = new byte[MAX_PLAYER];
        public byte[] Recoil = new byte[MAX_PLAYER];
        public UInt32[] Credits = new UInt32[MAX_PLAYER];
        public UInt16 P1_Lmp_Start = 0;        
        public UInt16 P1_Lmp_SeatPuck = 0;        
        public UInt16 P1_Lmp_SeatMarquee = 0;       
        public UInt16 P1_Lmp_SeatRear_R = 0;
        public UInt16 P1_Lmp_SeatRear_O = 0;
        public UInt16 P1_Lmp_SeatRear_B = 0;
        public UInt16 P2_Lmp_Start = 0;
        public UInt16 P2_Lmp_SeatPuck = 0;
        public UInt16 P2_Lmp_SeatMarquee = 0;
        public UInt16 P2_Lmp_SeatRear_R = 0;
        public UInt16 P2_Lmp_SeatRear_O = 0;
        public UInt16 P2_Lmp_SeatRear_B = 0;
        public UInt16 Cab_Lmp_R = 0;
        public UInt16 Cab_Lmp_G = 0;
        public UInt16 Cab_Lmp_B = 0;
        public UInt16 Cab_Lmp_RearSeat = 0;

        public static readonly int DATA_LENGTH = 2 + (4 * MAX_PLAYER) + 32;

        public DsTcp_OutputData_Nerf()
        {
            for (int i = 0; i < MAX_PLAYER; i++)
            {
                IsPlaying[i] = 0;
                Recoil[i] = 0;
                Credits[i] = 0;
            }
        }

        public void Update(byte[] ReceivedData)
        {
            for (int i = 0; i < 4; i++)
            {
                IsPlaying[i] = (byte)(ReceivedData[0] >> i & 0x01);
                Recoil[i] = (byte)(ReceivedData[1] >> i & 0x01);
                Credits[i] = BitConverter.ToUInt32(ReceivedData, 2 + (4 * i));
            }
            P1_Lmp_Start = BitConverter.ToUInt16(ReceivedData, 18);
            P1_Lmp_SeatPuck = BitConverter.ToUInt16(ReceivedData, 20);
            P1_Lmp_SeatMarquee = BitConverter.ToUInt16(ReceivedData, 22);
            P1_Lmp_SeatRear_R = BitConverter.ToUInt16(ReceivedData, 24);
            P1_Lmp_SeatRear_O = BitConverter.ToUInt16(ReceivedData, 26);
            P1_Lmp_SeatRear_B = BitConverter.ToUInt16(ReceivedData, 28);
            P2_Lmp_Start = BitConverter.ToUInt16(ReceivedData, 30);
            P2_Lmp_SeatPuck = BitConverter.ToUInt16(ReceivedData, 32);
            P2_Lmp_SeatMarquee = BitConverter.ToUInt16(ReceivedData, 34);
            P2_Lmp_SeatRear_R = BitConverter.ToUInt16(ReceivedData, 36);
            P2_Lmp_SeatRear_O = BitConverter.ToUInt16(ReceivedData, 38);
            P2_Lmp_SeatRear_B = BitConverter.ToUInt16(ReceivedData, 40);
            Cab_Lmp_R = BitConverter.ToUInt16(ReceivedData, 42);
            Cab_Lmp_G = BitConverter.ToUInt16(ReceivedData, 44);
            Cab_Lmp_B = BitConverter.ToUInt16(ReceivedData, 46);
            Cab_Lmp_RearSeat = BitConverter.ToUInt16(ReceivedData, 48);
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];

            for (int i = 0; i < 4; i++)
            {
                bArray[0] |= (byte)(IsPlaying[i] << i);
                bArray[1] |= (byte)(Recoil[i] << i);
                Array.Copy(BitConverter.GetBytes(Credits[i]), 0, bArray, 2 + (4 * i), 4);
            }            
            Array.Copy(BitConverter.GetBytes(P1_Lmp_Start), 0, bArray, 18, 2);
            Array.Copy(BitConverter.GetBytes(P1_Lmp_SeatPuck), 0, bArray, 20, 2);
            Array.Copy(BitConverter.GetBytes(P1_Lmp_SeatMarquee), 0, bArray, 22, 2);
            Array.Copy(BitConverter.GetBytes(P1_Lmp_SeatRear_R), 0, bArray, 24, 2);
            Array.Copy(BitConverter.GetBytes(P1_Lmp_SeatRear_O), 0, bArray, 26, 2);
            Array.Copy(BitConverter.GetBytes(P1_Lmp_SeatRear_B), 0, bArray, 28, 2);
            Array.Copy(BitConverter.GetBytes(P2_Lmp_Start), 0, bArray, 30, 2);
            Array.Copy(BitConverter.GetBytes(P2_Lmp_SeatPuck), 0, bArray, 32, 2);
            Array.Copy(BitConverter.GetBytes(P2_Lmp_SeatMarquee), 0, bArray, 34, 2);
            Array.Copy(BitConverter.GetBytes(P2_Lmp_SeatRear_R), 0, bArray, 36, 2);
            Array.Copy(BitConverter.GetBytes(P2_Lmp_SeatRear_O), 0, bArray, 38, 2);
            Array.Copy(BitConverter.GetBytes(P2_Lmp_SeatRear_B), 0, bArray, 40, 2);
            Array.Copy(BitConverter.GetBytes(Cab_Lmp_R), 0, bArray, 42, 2);
            Array.Copy(BitConverter.GetBytes(Cab_Lmp_G), 0, bArray, 44, 2);
            Array.Copy(BitConverter.GetBytes(Cab_Lmp_B), 0, bArray, 46, 2);
            Array.Copy(BitConverter.GetBytes(Cab_Lmp_RearSeat), 0, bArray, 48, 2); 
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
