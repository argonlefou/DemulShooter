using System;
using System.Collections.Generic;
using System.Text;
using DsCore.IPC;
using DsCore;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore.MameOutput;
using DsCore.Config;
using DsCore.RawInput;

namespace DemulShooterX64
{
    class Game_UnisDinoInvasion : Game
    {
        private DsTcp_Client _Tcpclient;
        private DsTcp_OutputData_DinoInvasion _OutputData;
        private DsTcp_InputData _InputData;

        //Thread-safe operation on input/output data
        //public static System.Object MutexLocker;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <summary>
        /// Constructor
        /// </summary>
        public Game_UnisDinoInvasion(String RomName)
            : base(RomName, "konglong")
        {
            _KnownMd5Prints.Add("DinoInvasion EN v1.2.8 - Original", "30aacfb5fb65d409e7bd6baee679ba2d");

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
                            if (FindGameWindow_Equals("shoulie_konglong"))
                            {
                                String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", _Target_Process_Name + @"_Data\Managed\Assembly-CSharp.dll");
                                CheckMd5(AssemblyDllPath);

                                _InputData = new DsTcp_InputData();

                                //Start TcpClient to dial with Unity Game
                                _OutputData = new DsTcp_OutputData_DinoInvasion();
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

        ~Game_UnisDinoInvasion()
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

                _InputData.Axis_X[PlayerData.ID -1] = AxisX;
                _InputData.Axis_Y[PlayerData.ID -1] = AxisY;

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    _InputData.Trigger[PlayerData.ID -1] = 1;
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    _InputData.Trigger[PlayerData.ID -1] = 0;

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
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P1_CtmLmpStart, OutputId.P1_CtmLmpStart, 500));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P2_CtmLmpStart, OutputId.P2_CtmLmpStart, 500));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P3_CtmLmpStart, OutputId.P3_CtmLmpStart, 500));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P4_CtmLmpStart, OutputId.P4_CtmLmpStart, 500));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P3_CtmRecoil, OutputId.P3_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P4_CtmRecoil, OutputId.P4_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Ammo, OutputId.P3_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Ammo, OutputId.P4_Ammo));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P3_Damaged, OutputId.P3_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P4_Damaged, OutputId.P4_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Credits, OutputId.P1_Credits));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Credits, OutputId.P2_Credits));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Credits, OutputId.P3_Credits));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Credits, OutputId.P4_Credits));
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

                //Handling Start Lamps based on player status
                if (_OutputData.IsPlaying[0] == 0)
                    SetOutputValue(OutputId.P1_CtmLmpStart, -1);
                else
                    SetOutputValue(OutputId.P1_CtmLmpStart, 0);

                if (_OutputData.IsPlaying[1] == 0)
                    SetOutputValue(OutputId.P2_CtmLmpStart, -1);
                else
                    SetOutputValue(OutputId.P2_CtmLmpStart, 0);

                if (_OutputData.IsPlaying[2] == 0)
                    SetOutputValue(OutputId.P3_CtmLmpStart, -1);
                else
                    SetOutputValue(OutputId.P3_CtmLmpStart, 0);

                if (_OutputData.IsPlaying[3] == 0)
                    SetOutputValue(OutputId.P4_CtmLmpStart, -1);
                else
                    SetOutputValue(OutputId.P4_CtmLmpStart, 0);

                SetOutputValue(OutputId.P1_CtmRecoil, _OutputData.Recoil[0]);
                SetOutputValue(OutputId.P2_CtmRecoil, _OutputData.Recoil[1]);
                SetOutputValue(OutputId.P3_CtmRecoil, _OutputData.Recoil[2]);
                SetOutputValue(OutputId.P4_CtmRecoil, _OutputData.Recoil[3]);

                SetOutputValue(OutputId.P1_Ammo, (int)_OutputData.Ammo[0]);
                SetOutputValue(OutputId.P2_Ammo, (int)_OutputData.Ammo[1]);
                SetOutputValue(OutputId.P3_Ammo, (int)_OutputData.Ammo[2]);
                SetOutputValue(OutputId.P4_Ammo, (int)_OutputData.Ammo[3]);

                SetOutputValue(OutputId.P1_Damaged, _OutputData.Damaged[0]);
                SetOutputValue(OutputId.P2_Damaged, _OutputData.Damaged[1]);
                SetOutputValue(OutputId.P3_Damaged, _OutputData.Damaged[2]);
                SetOutputValue(OutputId.P4_Damaged, _OutputData.Damaged[3]);

                SetOutputValue(OutputId.P1_Credits, (int)_OutputData.Credits[0]);
                SetOutputValue(OutputId.P2_Credits, (int)_OutputData.Credits[1]);
                SetOutputValue(OutputId.P3_Credits, (int)_OutputData.Credits[2]);
                SetOutputValue(OutputId.P4_Credits, (int)_OutputData.Credits[3]);
            }
        }

        #endregion

    }
}
