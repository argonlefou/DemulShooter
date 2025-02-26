using System;

namespace UnityPlugin_BepInEx_Core
{
    class TcpInputData
    {
        public const int MAX_PLAYER = 4;

        public float[] Axis_X = new float[MAX_PLAYER];
        public float[] Axis_Y = new float[MAX_PLAYER];
        public byte[] Trigger = new byte[MAX_PLAYER];
        public byte[] Reload = new byte[MAX_PLAYER];
        public byte[] Action = new byte[MAX_PLAYER];
        public byte[] ChangeWeapon = new byte[MAX_PLAYER];
        public byte HideCrosshairs;
        public byte Hideguns;
        public byte EnableInputsHack;

        public static readonly int DATA_LENGTH = (8 * MAX_PLAYER) + 5;

        public TcpInputData()
        {
            for (int i = 0; i < 4; i++)
            {
                Axis_X[i] = 0.0f;
                Axis_X[i] = 0.0f;
                Trigger[i] = 0;
                Reload[i] = 0;
                Action[i] = 0;
                ChangeWeapon[i] = 0;
            }
            HideCrosshairs = 0;
            Hideguns = 0;
            EnableInputsHack = 0;
        }

        public byte[] ToByteArray()
        {
            byte[] bArray = new byte[DATA_LENGTH];
            for (int i = 0; i < 4; i++)
            {
                Array.Copy(BitConverter.GetBytes(Axis_X[i]), 0, bArray, (8 * i), 4);
                Array.Copy(BitConverter.GetBytes(Axis_Y[i]), 0, bArray, 4 + (8 * i), 4);
                bArray[32] |= (byte)(Trigger[i] << i);
                bArray[33] |= (byte)(Reload[i] << i);
                bArray[34] |= (byte)(Action[i] << i);
                bArray[35] |= (byte)(ChangeWeapon[i] << i);
            }
            bArray[36] |= HideCrosshairs;
            bArray[36] |= (byte)(Hideguns << 1);
            bArray[36] |= (byte)(EnableInputsHack << 7);

            return bArray;
        }

        public void Update(byte[] ReceivedData)
        {
            for (int i = 0; i < 4; i++)
            {
                Axis_X[i] = BitConverter.ToSingle(ReceivedData, 8 * i);
                Axis_Y[i] = BitConverter.ToSingle(ReceivedData, (8 * i) + 4);
                Trigger[i] = (byte)(ReceivedData[32] >> i & 0x01);
                Reload[i] = (byte)(ReceivedData[33] >> i & 0x01);
                Action[i] = (byte)(ReceivedData[34] >> i & 0x01);
                ChangeWeapon[i] = (byte)(ReceivedData[35] >> i & 0x01);
            }
            HideCrosshairs = (byte)(ReceivedData[36] & 0x01);
            Hideguns = (byte)(ReceivedData[36] >> 1 & 0x01);
            EnableInputsHack = (byte)(ReceivedData[36] >> 7 & 0x01);
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
