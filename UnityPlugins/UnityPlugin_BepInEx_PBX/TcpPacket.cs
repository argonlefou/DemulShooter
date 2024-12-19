using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace UnityPlugin_BepInEx_Core
{
    class TcpPacket
    {
        #region Variables 

        private PacketHeader _Header;
        private byte[] _Payload;
        private int _PayloadLenght;

        /// <summary>
        /// Packet type
        /// </summary>
        /// <remarks></remarks>
        public enum PacketHeader : byte
        {
            /// <summary>
            /// Packets received from Demulshooteer with Input data to control the Game
            /// </summary>
            /// <remarks></remarks>
            Inputs = 1,
            /// <summary>
            /// Packets sent to Demulshooter with Outputs status to control LEDs and Recoils
            /// </summary>
            /// <remarks></remarks>
            Outputs
        }

        #endregion

        #region Constructors
        
        public TcpPacket(PacketHeader Header)
            : this(new byte[0], Header)
        { }

        public TcpPacket(byte[] Payload, PacketHeader Header)
        {
            _Payload = Payload;
            _Header = Header;
            _PayloadLenght = _Payload.Length;
        }

        #endregion

        /// <summary>
        /// Return only the Header
        /// </summary>
        /// <returns></returns>
        public PacketHeader GetHeader()
        {
            return _Header;
        }

        /// <summary>
        /// Return only the Payload
        /// </summary>
        /// <returns></returns>
        public byte[] GetPayload()
        {
            return _Payload;
        }

        /// <summary>
        /// Return the full message Packets containier Lenght + Header + Payload
        /// </summary>
        /// <returns></returns>
        public byte[] GetFullPacket()
        {
            List<byte> FormatedPacket = new List<byte>();
            FormatedPacket.AddRange(BitConverter.GetBytes(_PayloadLenght + 1));
            FormatedPacket.Add((byte)_Header);
            FormatedPacket.AddRange(_Payload);
            return FormatedPacket.ToArray();
        }

        public override string ToString()
        {
            string s = string.Empty;
            for (int i = 0; i < _PayloadLenght; i++)
            {
                s += "0x" + _Payload[i].ToString("X2") + " ";
            }
            return s;
        }
    }
}
