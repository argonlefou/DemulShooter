using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.IPC;
using DsCore.MameOutput;

namespace DemulShooter
{
    class Game_ArcadepcWaterWar2 : Game
    {
        private DsTcp_Client _Tcpclient;
        private DsTcp_OutputData_WaterWar2 _OutputData;
        private DsTcp_InputData _InputData;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_ArcadepcWaterWar2(String RomName)
            : base(RomName, "Water2_EN")
        {
            _KnownMd5Prints.Add("Happy Water War 2 - v1.1", "f3fb78a0aa85562caba6ffbc6f295e13");

            _tProcess.Start();
            Logger.WriteLog("Waiting for " + _RomName + " game to hook.....");
        }

        /// <summary>
        /// Timer event when looking for Process (auto-Hook and auto-close)
        /// </summary>
        protected override void tProcess_Elapsed(Object Sender, EventArgs e)
        {
            if (!_ProcessHooked)
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName(_Target_Process_Name);
                    if (processes.Length > 0)
                    {
                        _TargetProcess = processes[0];
                        _ProcessHandle = _TargetProcess.Handle;
                        _TargetProcess_MemoryBaseAddress = _TargetProcess.MainModule.BaseAddress;

                        //Looking for the game's window based on it's Title
                        _GameWindowHandle = IntPtr.Zero;
                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            // The game may start with other Windows than the main one (BepInEx console, other stuff.....) so we need to filter
                            // the displayed window according to the Title, if DemulShooter is started before the game,  to hook the correct one
                            if (FindGameWindow_Equals("Water2_EN"))
                            {
                                String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", _Target_Process_Name + @"_Data\Managed\Assembly-CSharp.dll");
                                CheckMd5(AssemblyDllPath); 
                              
                                _InputData = new DsTcp_InputData();

                                //Start TcpClient to dial with Unity Game
                                _OutputData = new DsTcp_OutputData_WaterWar2();
                                _Tcpclient = new DsTcp_Client("127.0.0.1", DsTcp_Client.DS_TCP_CLIENT_PORT);
                                _Tcpclient.PacketReceived += DsTcp_Client_PacketReceived;
                                _Tcpclient.TcpConnected += DsTcp_client_TcpConnected;
                                _Tcpclient.Connect();

                                if (_DisableInputHack)
                                    Logger.WriteLog("Input Hack disabled");

                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
                            }
                            else
                            {
                                Logger.WriteLog("Game Window not found");
                                return;
                            }
                        }
                    }
                }
                catch
                {
                    Logger.WriteLog("Error trying to hook " + _Target_Process_Name + ".exe");
                }
            }
            else
            {
                Process[] processes = Process.GetProcessesByName(_Target_Process_Name);
                if (processes.Length <= 0)
                {
                    _ProcessHooked = false;
                    _TargetProcess = null;
                    _ProcessHandle = IntPtr.Zero;
                    _TargetProcess_MemoryBaseAddress = IntPtr.Zero;
                    Logger.WriteLog(_Target_Process_Name + ".exe closed");
                    Application.Exit();
                }
            }
        }

        ~Game_ArcadepcWaterWar2()
        {
            if (_Tcpclient != null)
                _Tcpclient.Disconnect();
        }

        /// <summary>
        /// Sending a TCP message on connection
        /// </summary>
        private void DsTcp_client_TcpConnected(Object Sender, EventArgs e)
        {
            if (_HideCrosshair)
                _InputData.HideCrosshairs = 1;
            else
                _InputData.HideCrosshairs = 0;

            if (_DisableInputHack)
                _InputData.EnableInputsHack = 0;
            else
                _InputData.EnableInputsHack = 1;

            _Tcpclient.SendMessage(_InputData.ToByteArray());
        }

        #region Screen

        public override bool GameScale(PlayerSettings PlayerData)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //Coordinates are Window size range, but inverted Y
                    int X_Value = PlayerData.RIController.Computed_X;
                    int Y_Value = (int)TotalResY - PlayerData.RIController.Computed_Y;

                    if (X_Value < 0)
                        X_Value = 0;
                    if (Y_Value < 0)
                        Y_Value = 0;
                    if (X_Value > (int)TotalResX)
                        X_Value = (int)TotalResX;
                    if (Y_Value > (int)TotalResY)
                        Y_Value = (int)TotalResY;

                    PlayerData.RIController.Computed_X = X_Value;
                    PlayerData.RIController.Computed_Y = Y_Value;

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Error scaling mouse coordonates to GameFormat : " + ex.Message.ToString());
                }
            }
            return false;
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>
        public override void SendInput(PlayerSettings PlayerData)
        {
            if (!_DisableInputHack)
            {
                float AxisX = PlayerData.RIController.Computed_X;
                float AxisY = PlayerData.RIController.Computed_Y;

                _InputData.Axis_X[PlayerData.ID - 1] = AxisX;
                _InputData.Axis_Y[PlayerData.ID - 1] = AxisY;

                if (_HideCrosshair)
                    _InputData.HideCrosshairs = 1;
                else
                    _InputData.HideCrosshairs = 0;

                _Tcpclient.SendMessage(_InputData.ToByteArray());
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_WaterFire, OutputId.P1_WaterFire));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_WaterFire, OutputId.P2_WaterFire));
            _Outputs.Add(new GameOutput(OutputDesciption.WaterPump, OutputId.WaterPump));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_BigGun, OutputId.P1_BigGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_BigGun, OutputId.P2_BigGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_TicketFeeder, OutputId.P1_TicketFeeder));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_TicketFeeder, OutputId.P2_TicketFeeder));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Credits, OutputId.P1_Credits));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Credits, OutputId.P2_Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Nothing to do here, update will be done by the Tcp packet received event
        }

        private void DsTcp_Client_PacketReceived(Object Sender, DsTcp_Client.PacketReceivedEventArgs e)
        {
            if (e.Packet.GetHeader() == DsTcp_TcpPacket.PacketHeader.Outputs)
            {
                _OutputData.Update(e.Packet.GetPayload());

                SetOutputValue(OutputId.P1_LmpStart, _OutputData.StartLed[0]);
                SetOutputValue(OutputId.P2_LmpStart, _OutputData.StartLed[1]);

                SetOutputValue(OutputId.P1_WaterFire, _OutputData.WaterFire[0]);
                SetOutputValue(OutputId.P2_WaterFire, _OutputData.WaterFire[1]);
                SetOutputValue(OutputId.WaterPump, _OutputData.WaterPump);

                SetOutputValue(OutputId.P1_BigGun, _OutputData.BigGun[0]);
                SetOutputValue(OutputId.P2_BigGun, _OutputData.BigGun[1]);

                SetOutputValue(OutputId.P1_TicketFeeder, _OutputData.TicketFeeder[0]);
                SetOutputValue(OutputId.P2_TicketFeeder, _OutputData.TicketFeeder[1]);

                SetOutputValue(OutputId.P1_Credits, (int)_OutputData.Credits[0]);
                SetOutputValue(OutputId.P2_Credits, (int)_OutputData.Credits[1]);
            }
        }

        #endregion

    }
}
