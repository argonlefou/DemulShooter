using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DsCore.MameOutput
{
    public class Net_OutputHelper
    {
        private const string MAME_START_NAME = "mame_start";
        private const string MAME_STOP_NAME = "mame_stop";
        private const string MAME_START_EMPTY = "___empty";

        private Thread _TcpListenerThread;
        private TcpListener _TcpListener;
        private const int TCP_PORT = 8000;

        private List<Net_OutputClient> _ConnectedTcpClients;

        private List<GameOutput> _OutputsBefore;

        private string _RomName = string.Empty;
        private bool _IsGameHooked = false;

        private bool _ReadyToSendValues = false;
        public bool ReadyToSendValues
        { get { return _ReadyToSendValues; } }

        public Net_OutputHelper(string RomName)
        {
            _RomName = RomName;
            _ConnectedTcpClients = new List<Net_OutputClient>();

            try
            {
                // Create listener on localhost port 8000. 			
                _TcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), TCP_PORT);
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("Net_OutputHelper() => Error creating the Listener : " + Ex.Message.ToString());
            }
        }

        public void Start()
        {
            _TcpListenerThread = new Thread(new ThreadStart(ListenerThread));
            _TcpListenerThread.Start();
        }

        public void Stop()
        {
            try
            {  
                foreach (Net_OutputClient Client in _ConnectedTcpClients)
                {
                    SendStopMessage(Client);
                    if (Client.Handler.Connected && Client.Stream != null)
                    {
                        Client.Stream.Close();
                        Client.Handler.Close();
                    }
                }
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("Net_OutputHelper.Stop() => Exception: " + Ex);
            }
            finally
            {
                _TcpListener.Stop();
            }
        }

        /// <summary>
        /// Separate thread with infinite loop to get new TcpClients and store them for later use
        /// </summary>
        private void ListenerThread()
        {
            try
            {
                _TcpListener.Start();
                Logger.WriteLog("Net_OutputHelper.TcpClientThreadLoop() => TCP Server is listening on Port " + TCP_PORT);

                while (true) //Thread is blocked while waiting for a connection, can't be stopped by a flag
                {
                    Logger.WriteLog("Net_OutputHelper.TcpClientThreadLoop() => TCP Server is waiting for a client " + TCP_PORT);
                    
                    TcpClient client = _TcpListener.AcceptTcpClient();
                    Net_OutputClient NetClient = new Net_OutputClient(client);
                    Logger.WriteLog("Net_OutputHelper.TcpClientThreadLoop() => New client connected : IP=" + client.Client.RemoteEndPoint.ToString());

                    _ConnectedTcpClients.Add(NetClient);

                    //Sending the MameStart message with corresponding information
                    /************* CLosing the NetworkStrem close the TcpClient !!!!! *************/
                    string mStart = string.Empty;
                    if (!_IsGameHooked)
                    {
                        SendValue(NetClient, MAME_START_NAME, MAME_START_EMPTY);
                    }
                    else
                    {
                        SendStartMessage(NetClient);
                    }

                    byte[] data = Encoding.ASCII.GetBytes(mStart);
                    NetClient.Stream.Write(data, 0, data.Length);
                }
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("Net_OutputHelper.TcpClientThreadLoop() => ListenerThreadError " + Ex.Message.ToString());
            }
        }


        /// <summary>
        /// As opposed to WindowMessage outputs, where MameHooker has to be run _before_ DemulsHooter,
        /// Network cliant can be connected at everytime. So it may miss the 'GameHooked' event occuring before it's connection.
        /// We need that info to send a mame start '___empty' or '[GameRom]' accorting to it when connection is initiated
        /// </summary>
        /// <param name="HookedState"></param>
        public void SetGameHookedState(bool HookedState)
        {
            _IsGameHooked = HookedState;
        }

        /// <summary>
        /// Send updated values to all registered clients
        /// A small "filtering" method was added, to send output message to MameHooker only on changed state of a value
        /// </summary>
        /// <param name="Outputs">List of values to send</param>
        public void BroadcastValues(List<GameOutput> Outputs)
        {
            if (_OutputsBefore == null)

            {   //Cloning the output list without references to the GameOutput object
                _OutputsBefore = Outputs.ConvertAll(x => new GameOutput(x));
            }
                        


            foreach (Net_OutputClient Client in _ConnectedTcpClients)
            {
                if (Client.ReadyToGetOutputs)
                {
                    if (Client.FirstOutputs)
                    {
                        for (int i = 0; i < Outputs.Count; i++)
                        {
                            SendValue(Client, Outputs[i].Name, Outputs[i].OutputValue.ToString());
                        }
                        Client.FirstOutputs = false;
                    }
                    else
                    {
                        for (int i = 0; i < Outputs.Count; i++)
                        {
                            if (Outputs[i].OutputValue != _OutputsBefore[i].OutputValue)
                            {
                                SendValue(Client, Outputs[i].Name, Outputs[i].OutputValue.ToString());                                
                            }
                        }
                    }
                }
            }

            _OutputsBefore = Outputs.ConvertAll(x => new GameOutput(x));
        }


        /// <summary>
        /// Send a 'mamestart = [RomName'] to all connected clients
        /// </summary>
        /// <param name="RomName"></param>
        public void BroadcatStartMessage()
        {
            foreach (Net_OutputClient Client in _ConnectedTcpClients)
            {
                SendStartMessage(Client);
            }
        }

        /// <summary>
        /// Send a 'mamestart = ['RomName'] to a connected client
        /// </summary>
        /// <param name="RomName"></param>
        public void SendStartMessage(Net_OutputClient Client)
        {
            SendValue(Client, MAME_STOP_NAME, "1");
            SendValue(Client, MAME_START_NAME, _RomName);
            Client.ReadyToGetOutputs = true;
        }

        /// <summary>
        /// Send a 'mamestop = 1" message to a connected client
        /// </summary>
        /// <param name="RomName"></param>
        public void SendStopMessage(Net_OutputClient Client)
        {            
            SendValue(Client, MAME_STOP_NAME, "1");
            SendValue(Client, MAME_START_NAME, MAME_START_EMPTY);
            Client.ReadyToGetOutputs = false;
        }

        /// <summary>
        /// Send a specific updated value to all registered clients
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Value"></param>
        public void SendValue(Net_OutputClient Client, String Name, string Value)
        {
            try
            {
                if (Client.Stream != null && Client.Stream.CanWrite)
                {
                    byte[] Payload = Encoding.ASCII.GetBytes(Name + " = " + Value.ToString() + "\r");
                    Client.Stream.Write(Payload, 0, Payload.Length);
                }
            }
            catch (Exception Ex)
            {
                Logger.WriteLog("Net_OutputHelper.SendValue() => Error sending to " + Client.Handler.Client.RemoteEndPoint.ToString() + " : " + Ex.Message.ToString());
            }
        }
    }
}
