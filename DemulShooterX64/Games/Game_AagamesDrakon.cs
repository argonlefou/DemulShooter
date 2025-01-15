using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.IPC;
using DsCore.MameOutput;
using DsCore.RawInput;

namespace DemulShooterX64
{
    class Game_AagamesDrakon : Game
    {
        private DsTcp_Client _Tcpclient;
        private DsTcp_OutputData_Drakon _OutputData;
        private DsTcp_InputData_Drakon _InputData;

        protected float _P1_X_Value;
        protected float _P1_Y_Value;
        protected float _P2_X_Value;
        protected float _P2_Y_Value;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_AagamesDrakon(String RomName)
            : base(RomName, "Game")
        {
            _KnownMd5Prints.Add("Drakon Realm Keepers - Development Build v227996", "783a592917167b3a3a3e42f9f0717a06");
            _KnownMd5Prints.Add("Drakon Realm Keepers - Release Build v223011", "b9eaa606548f04d684876c17f48deaa3");
            
            _tProcess.Start();
            Logger.WriteLog("Waiting for Adrenaline Amusements game " + _RomName + " game to hook.....");
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
                            if (FindGameWindow_Equals("09038_Adrenaline_Skyride_Turret"))
                            {
                                String GameAssembly = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", @"GameAssembly.dll");
                                CheckMd5(GameAssembly); 
                              
                                _InputData = new DsTcp_InputData_Drakon();

                                //Start TcpClient to dial with Unity Game
                                _OutputData = new DsTcp_OutputData_Drakon();
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

        ~Game_AagamesDrakon()
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

                    //Coordinates goes from [-1.0, -1.0] in bottom left corner to [1.0, 1.0] in upper right, no matter the display ratio
                    float X_Value = (2.0f * (float)PlayerData.RIController.Computed_X / (float)TotalResX) - 1.0f;
                    float Y_Value = (1.0f - (2.0f * (float)PlayerData.RIController.Computed_Y / (float)TotalResY));

                    if (X_Value < -1.0f)
                        X_Value = -1.0f;
                    if (Y_Value < -1.0f)
                        Y_Value = 1.0f;
                    if (X_Value > (float)1.0f)
                        X_Value = (float)1.0f;
                    if (Y_Value > (float)1.0f)
                        Y_Value = (float)1.0f;

                    Logger.WriteLog("Computed float values = [ " + X_Value + "x" + Y_Value + " ]");

                    if (PlayerData.ID == 1)
                    {
                        _P1_X_Value = X_Value;
                        _P1_Y_Value = Y_Value;
                    }
                    else if (PlayerData.ID == 2)
                    {
                        _P2_X_Value = X_Value;
                        _P2_Y_Value = Y_Value;
                    }

                    PlayerData.RIController.Computed_X = Convert.ToInt16(X_Value * 100);
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(Y_Value * 100);
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
                if (PlayerData.ID == 1)
                {
                    _InputData.P1_X = _P1_X_Value;
                    _InputData.P1_Y = _P1_Y_Value;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        _InputData.P1_Trigger = 1;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        _InputData.P1_Trigger = 0;
                }
                else if (PlayerData.ID == 2)
                {
                    _InputData.P2_X = _P2_X_Value;
                    _InputData.P2_Y = _P2_Y_Value;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        _InputData.P2_Trigger = 1;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        _InputData.P2_Trigger = 0;
                }

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
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));            
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            /*_Outputs.Add(new GameOutput(OutputDesciption.P1_Credits, OutputId.P1_Credits));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Credits, OutputId.P2_Credits));*/
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

                SetOutputValue(OutputId.P1_CtmRecoil, _OutputData.P1_Recoil);
                SetOutputValue(OutputId.P2_CtmRecoil, _OutputData.P2_Recoil);
                SetOutputValue(OutputId.P1_GunMotor, _OutputData.P1_Rumble);
                SetOutputValue(OutputId.P2_GunMotor, _OutputData.P2_Rumble);
                SetOutputValue(OutputId.P1_Damaged, _OutputData.P1_Damage);
                SetOutputValue(OutputId.P2_Damaged, _OutputData.P2_Damage);
                SetOutputValue(OutputId.P1_LmpStart, _OutputData.P1_StartLED);
                SetOutputValue(OutputId.P2_LmpStart, _OutputData.P2_StartLED);
                /*SetOutputValue(OutputId.P1_Credits, _OutputData.P1_Credits);
                SetOutputValue(OutputId.P2_Credits, _OutputData.P2_Credits);*/
            }
        }

        #endregion
    }
}
