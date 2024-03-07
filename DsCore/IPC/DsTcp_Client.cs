using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace DsCore.IPC
{
    public class DsTcp_Client
    {
        public static readonly int DS_TCP_CLIENT_PORT = 33610;

        bool TerminateListenerThread = false;
        bool ListenerThreadActive = false;        

        private TcpClient _TcpClient;
        public TcpClient Client
        {
            get { return _TcpClient; }
        }

        private NetworkStream _ClientNetworkStream;
        private string _ServerIpAddress;
        private int _ServerTcpPort;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Address">IP Adress of the server to connect to</param>
        /// <param name="Port">TCP Port the server is listening at</param>
        public DsTcp_Client(string Address, int Port)
        {
            _ServerIpAddress = Address;
            _ServerTcpPort = Port;
            _TcpClient = new TcpClient();
        }

        public void Connect()
        {
            try
            {               
                StartListenerThread();
            }
            catch (Exception ex)
            {
                Logger.WriteLog("Ds_TcpClient.Connect(): Error trying to connect to " + _ServerIpAddress + ":" + _ServerTcpPort + "=>" +  ex.Message);
            }
        }


        public void Disconnect()
        {
            //Stop the packet listener thread.
            TerminateListenerThread = true;
            ListenerThreadActive = false;

            //Handle the disconnection in a separate thread.
            Thread DThread = new Thread(DisconnectThread);
            DThread.IsBackground = true;
            DThread.Start();

            _ClientNetworkStream = null;
        }
        private void DisconnectThread()
        {
            //Wait just a little moment so the packet listener thread can terminate.
            System.Threading.Thread.Sleep(25);
            try 
            { 
                _TcpClient.GetStream().Close(5000); 
            }
            catch { }

            try 
            {
                _TcpClient.Close(); 
            }
            catch { }
        }

        public bool IsConnected()
        {
            return _TcpClient != null && _TcpClient.Connected;
        }

        /// <summary> 	
        /// Send message to server using socket connection. 	
        /// </summary> 	
        public void SendMessage(byte[] DataToSend)
        {            
            try
            {
                if (_TcpClient == null)
                {
                    Logger.WriteLog("DspTcp_Client.SendMessage() => TcpClient is NULL, skipping.");
                    return;
                }
                if (_TcpClient.Connected)
                {
                    // Get a stream object for writing. 			
                    NetworkStream stream = _TcpClient.GetStream();
                    if (stream.CanWrite)
                    {
                        stream.Write(DataToSend, 0, DataToSend.Length);
                        Logger.WriteLog("DspTcp_Client.SendMessage() => Succesfully sent data (size=" + DataToSend.Length + " bytes) to TCP server");
                    }
                }
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("DspTcp_Client.SendMessage() =>  Socket exception: " + Ex.Message.ToString());
            }
        } 

        /// <summary>
        /// Starts the thread that listens for packets (automatically started by Connect()).
        /// </summary>
        /// <remarks></remarks>
        private void StartListenerThread()
        {
            if (ListenerThreadActive == true) 
            {
                Logger.WriteLog("Ds_TcpClient.StartListenerThread(): Error -> Listener already running !!");
                return;
            }
            Thread ListenerThread = new Thread(Listener);
            ListenerThread.IsBackground = true;
            ListenerThread.Start();
            ListenerThreadActive = true;
        }

        private void Listener()
        {
            Logger.WriteLog("Successfully Started DsTcp_Client Listener() Thread");            
            while (true)
            {
                try
                {
                    _TcpClient.Connect(_ServerIpAddress, _ServerTcpPort);
                    Logger.WriteLog("Ds_TcpClient.Listener(): Successfully connected to " + _ServerIpAddress + ":" + _ServerTcpPort);
                    _ClientNetworkStream = _TcpClient.GetStream();

                    int PacketLengthInfo = -1;
                    int BytesRead = 0;
                    byte[] InputBuffer = new byte[0];

                    while (_TcpClient != null && IsConnected() == true && TerminateListenerThread == false)
                    {
                        while (_ClientNetworkStream != null && _ClientNetworkStream.DataAvailable == true)
                        {
                            //If Lenght Information is not existing, we need first to get it in order to knwow the full message length
                            //For that, we need to get the first 4 bytes of the frame
                            if (PacketLengthInfo < 0)
                            {
                                if (BytesRead == 0)
                                    InputBuffer = new byte[4];

                                BytesRead += _ClientNetworkStream.Read(InputBuffer, BytesRead, (InputBuffer.Length - BytesRead));

                                //Getting the Length of the pakcet to follow using the first 4 bytes read
                                if (BytesRead == 4)
                                {
                                    PacketLengthInfo = BitConverter.ToInt32(InputBuffer, 0);
                                    BytesRead = 0;
                                    Array.Clear(InputBuffer, 0, InputBuffer.Length);
                                }
                            }
                            //If the Packet lenght is known, we can know read data correctly and safely
                            else
                            {
                                if (BytesRead == 0)
                                    InputBuffer = new byte[PacketLengthInfo];

                                BytesRead += _ClientNetworkStream.Read(InputBuffer, BytesRead, (InputBuffer.Length - BytesRead));

                                if (BytesRead >= PacketLengthInfo) //Check so that we've received the whole packet.
                                {
                                    OnPacketReceived(this, new PacketReceivedEventArgs(new DsTcp_TcpPacket(InputBuffer)));

                                    //Reset
                                    BytesRead = 0;
                                    PacketLengthInfo = -1;
                                    Array.Clear(InputBuffer, 0, InputBuffer.Length);
                                }
                            }
                        }

                        System.Threading.Thread.Sleep(1);
                    }
                }
                catch (SocketException Ex)
                {
                    Logger.WriteLog("Ds_TcpClient.Listener(): Error trying to connect to " + _ServerIpAddress + ":" + _ServerTcpPort);
                    Logger.WriteLog(Ex.Message.ToString());
                }
            }            
        }

        #region Custom Event

        public delegate void PacketReceivedEventHandler(object sender, PacketReceivedEventArgs e);
        public event PacketReceivedEventHandler PacketReceived;
        private void OnPacketReceived(object sender, PacketReceivedEventArgs e)
        {
            if (PacketReceived != null)
                PacketReceived(sender, e);
        }

        public class PacketReceivedEventArgs : EventArgs        
        {
            private DsTcp_TcpPacket _TcpPacket;

            public DsTcp_TcpPacket Packet
            {
                get { return _TcpPacket; }
            }

            public PacketReceivedEventArgs(DsTcp_TcpPacket Packet)
            {
                _TcpPacket = Packet;
            }
        }

        #endregion
    }
}
