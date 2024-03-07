using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace DsCore.MameOutput
{
    public class Net_OutputClient
    {
        private TcpClient _TcpClient;
        private NetworkStream _Stream;

        public TcpClient Handler
        { get { return _TcpClient; } }

        public NetworkStream Stream
        { get { return _Stream; } }

        //This flag will tell if a client needs to be send the full list of outputs (usually at connection)
        //To ensure list of outputs don't arrive before MameStart
        private bool _ReadyToGetOutputs = false;
        public bool ReadyToGetOutputs
        { 
            get { return _ReadyToGetOutputs; }
            set { _ReadyToGetOutputs = value; }
        }

        //First outputs are client specific : need to send all of them at first when a client is connected
        //Then it will be filtered by change
        private bool _FirstOutputs = true;
        public bool FirstOutputs
        { 
            get { return _FirstOutputs; }
            set { _FirstOutputs = value; }
        }
 
        public Net_OutputClient(TcpClient Handler)
        {
            _TcpClient = Handler;
            _Stream = _TcpClient.GetStream();

            /*----------- GetStream() will return the same instance if it is already existing, so it can be reused anywhere in the code without memory leak --------------*/
        }

    }
}
