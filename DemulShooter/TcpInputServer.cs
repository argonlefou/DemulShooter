using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DsCore;

namespace DemulShooter
{
    /// <summary>
    /// TCP Input Server for simplified mouse protocol supporting 4 players.
    /// Compatible with batocera-wine-guns for BTN_LEFT (trigger), BTN_RIGHT (reload), and BTN_MIDDLE (action).
    /// Packet format: X[4](float) Y[4](float) EnableInputsHack(byte) HideCrosshairs(byte) Trigger[4](byte) Reload[4](byte) Action[4](byte)
    /// Total: 46 bytes
    /// </summary>
    internal delegate void TcpInputDataHandler(float[] axisX, float[] axisY, bool[] trigger, bool[] reload, bool[] action);

    internal class TcpInputServer
    {
        private const int MAX_PLAYERS = 4;

        private readonly TcpInputDataHandler _dataHandler;
        private readonly int _port;
        private TcpListener _listener;
        private Thread _listenerThread;
        private volatile bool _running;

        public TcpInputServer(TcpInputDataHandler dataHandler, int port)
        {
            _dataHandler = dataHandler;
            _port = port;
        }

        public void Start()
        {
            if (_running)
                return;

            _running = true;
            _listenerThread = new Thread(new ThreadStart(ListenerThreadLoop));
            _listenerThread.IsBackground = true;
            _listenerThread.Start();
        }

        public void Stop()
        {
            _running = false;

            try
            {
                _listener?.Stop();
            }
            catch { }

            try
            {
                if (_listenerThread != null && _listenerThread.IsAlive)
                    _listenerThread.Join(1000);
            }
            catch { }
        }

        private void ListenerThreadLoop()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Loopback, _port);
                _listener.Server.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);
                _listener.Start();
                Logger.WriteLog("TcpInputServer listening on 127.0.0.1:" + _port);

                while (_running)
                {
                    TcpClient client = null;
                    try
                    {
                        client = _listener.AcceptTcpClient();
                        Logger.WriteLog("TcpInputServer: client connected " + client.Client.RemoteEndPoint);
                        HandleClient(client);
                    }
                    catch (SocketException ex)
                    {
                        if (_running)
                            Logger.WriteLog("TcpInputServer socket error: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog("TcpInputServer error: " + ex.Message);
                    }
                    finally
                    {
                        if (client != null)
                        {
                            try { client.Close(); } catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog("TcpInputServer failed to start: " + ex.Message);
            }
        }

        private void HandleClient(TcpClient client)
        {
            // Packet format: X[4](float) Y[4](float) EnableInputsHack(byte) HideCrosshairs(byte) Trigger[4](byte) Reload[4](byte) Action[4](byte) = 46 bytes
            const int PACKET_SIZE = 46;
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[PACKET_SIZE * 16];
                int buffered = 0;

                while (_running && client.Connected)
                {
                    int bytesRead = stream.Read(buffer, buffered, buffer.Length - buffered);
                    if (bytesRead == 0)
                        break;

                    buffered += bytesRead;

                    int processed = 0;
                    while (buffered - processed >= PACKET_SIZE)
                    {
                        TryParsePacket(buffer, processed);
                        processed += PACKET_SIZE;
                    }

                    if (processed > 0)
                    {
                        int remaining = buffered - processed;
                        if (remaining > 0)
                            Buffer.BlockCopy(buffer, processed, buffer, 0, remaining);
                        buffered = remaining;
                    }
                }
            }
        }

        private void TryParsePacket(byte[] buffer, int offset)
        {
            try
            {
                float[] axisX = new float[4];
                float[] axisY = new float[4];
                bool[] trigger = new bool[4];
                bool[] reload = new bool[4];
                bool[] action = new bool[4];

                for (int i = 0; i < 4; i++)
                {
                    axisX[i] = BitConverter.ToSingle(buffer, offset);
                    offset += 4;
                }

                for (int i = 0; i < 4; i++)
                {
                    axisY[i] = BitConverter.ToSingle(buffer, offset);
                    offset += 4;
                }

                offset += 2; // EnableInputsHack, HideCrosshairs

                for (int i = 0; i < 4; i++)
                    trigger[i] = buffer[offset++] != 0;

                for (int i = 0; i < 4; i++)
                    reload[i] = buffer[offset++] != 0;

                for (int i = 0; i < 4; i++)
                    action[i] = buffer[offset++] != 0;

                _dataHandler?.Invoke(axisX, axisY, trigger, reload, action);
            }
            catch (Exception ex)
            {
                Logger.WriteLog("TcpInputServer packet parse error: " + ex.Message);
            }
        }
    }
}
