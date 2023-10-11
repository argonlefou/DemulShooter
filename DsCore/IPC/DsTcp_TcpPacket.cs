using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DsCore.IPC
{
    public class DsTcp_TcpPacket
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
            /// Packets sent from Demulshooteer with Input data to control the Game
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

        public DsTcp_TcpPacket(PacketHeader Header)
            : this(new byte[0], Header)
        { }

        public DsTcp_TcpPacket(byte[] Payload, PacketHeader Header)
        {
            _Payload = Payload;
            _Header = Header;
            _PayloadLenght = _Payload.Length;
        }

        public DsTcp_TcpPacket(byte[] PacketAndHeader)
        {
            _Header = (PacketHeader)PacketAndHeader[0];
            _Payload = new byte[PacketAndHeader.Length - 1];
            Array.Copy(PacketAndHeader, 1, _Payload, 0, _Payload.Length);
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
    }
}
