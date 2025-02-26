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
    class Game_RtNerfArcade : Game
    {
        private DsTcp_Client _Tcpclient;
        private DsTcp_OutputData_Nerf _OutputData;
        private DsTcp_InputData _InputData;

        //Thread-safe operation on input/output data
        //public static System.Object MutexLocker;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_RtNerfArcade(String RomName)
            : base(RomName, "Nerf")
        {
            _KnownMd5Prints.Add("Nerf Arcade v1.55", "7f40b5a56501507b9e899f1d58401817");
            _tProcess.Start();
            Logger.WriteLog("Waiting for Raw Thrill game " + _RomName + " game to hook.....");
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
                            if (FindGameWindow_Equals("Nerf"))
                            {
                                String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", _Target_Process_Name + @"_Data\Managed\Assembly-CSharp.dll");
                                CheckMd5(AssemblyDllPath);

                                _InputData = new DsTcp_InputData();

                                //Start TcpClient to dial with Unity Game
                                _OutputData = new DsTcp_OutputData_Nerf();
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

        ~Game_RtNerfArcade()
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

                    double dRatio = TotalResX / TotalResY;
                    Logger.WriteLog("Game Window ratio = " + dRatio);

                    //Axis is between 0.0f and 1.0f
                    float fX = Convert.ToSingle(Math.Round(PlayerData.RIController.Computed_X / TotalResX, 2));
                    float fY = 1.0f - Convert.ToSingle(Math.Round(PlayerData.RIController.Computed_Y / TotalResY, 2));

                    if (fX < 0)
                        fX = 0;
                    if (fY < 0)
                        fY = 0;
                    if (fX > 1.0f)
                        fX = 1.0f;
                    if (fY > 1.0f)
                        fY = 1.0f;

                    PlayerData.RIController.Computed_X = (int)(fX * 1000);
                    PlayerData.RIController.Computed_Y = (int)(fY * 1000);

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
                float AxisX = (float)PlayerData.RIController.Computed_X / 1000.0f;
                float AxisY = (float)PlayerData.RIController.Computed_Y / 1000.0f;

                _InputData.Axis_X[PlayerData.ID - 1] = AxisX;
                _InputData.Axis_Y[PlayerData.ID - 1] = AxisY;

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    _InputData.Trigger[PlayerData.ID - 1] = 1;
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    _InputData.Trigger[PlayerData.ID - 1] = 0;

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    _InputData.Action[PlayerData.ID - 1] = 1;
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    _InputData.Action[PlayerData.ID - 1] = 0;

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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_SeatPuck, OutputId.P1_Lmp_SeatPuck));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_SeatMarquee, OutputId.P1_Lmp_SeatMarquee));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_SeatSpeaker_R, OutputId.P1_Lmp_SeatSpeaker_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_SeatSpeaker_O, OutputId.P1_Lmp_SeatSpeaker_O));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_SeatSpeaker_B, OutputId.P1_Lmp_SeatSpeaker_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_SeatPuck, OutputId.P2_Lmp_SeatPuck));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_SeatMarquee, OutputId.P2_Lmp_SeatMarquee));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_SeatSpeaker_R, OutputId.P2_Lmp_SeatSpeaker_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_SeatSpeaker_O, OutputId.P2_Lmp_SeatSpeaker_O));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_SeatSpeaker_B, OutputId.P2_Lmp_SeatSpeaker_B));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_TMolding_R, OutputId.Lmp_TMolding_R));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_TMolding_G, OutputId.Lmp_TMolding_G));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_TMolding_B, OutputId.Lmp_TMolding_B));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_SeatDownLight, OutputId.Lmp_SeatDownLight));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));            
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

                SetOutputValue(OutputId.P1_CtmRecoil, _OutputData.Recoil[0]);
                SetOutputValue(OutputId.P2_CtmRecoil, _OutputData.Recoil[1]);
                SetOutputValue(OutputId.P1_Credits, (int)_OutputData.Credits[0]);
                SetOutputValue(OutputId.P2_Credits, (int)_OutputData.Credits[1]);
                //Lamps
                SetOutputValue(OutputId.P1_LmpStart, _OutputData.P1_Lmp_Start);
                SetOutputValue(OutputId.P1_Lmp_SeatPuck, _OutputData.P1_Lmp_SeatPuck);
                SetOutputValue(OutputId.P1_Lmp_SeatMarquee, _OutputData.P1_Lmp_SeatMarquee);
                SetOutputValue(OutputId.P1_Lmp_SeatSpeaker_R, _OutputData.P1_Lmp_SeatRear_R);
                SetOutputValue(OutputId.P1_Lmp_SeatSpeaker_O, _OutputData.P1_Lmp_SeatRear_O);
                SetOutputValue(OutputId.P1_Lmp_SeatSpeaker_B, _OutputData.P1_Lmp_SeatRear_B);
                SetOutputValue(OutputId.P2_LmpStart, _OutputData.P2_Lmp_Start);
                SetOutputValue(OutputId.P2_Lmp_SeatPuck, _OutputData.P2_Lmp_SeatPuck);
                SetOutputValue(OutputId.P2_Lmp_SeatMarquee, _OutputData.P2_Lmp_SeatMarquee);
                SetOutputValue(OutputId.P2_Lmp_SeatSpeaker_R, _OutputData.P2_Lmp_SeatRear_R);
                SetOutputValue(OutputId.P2_Lmp_SeatSpeaker_O, _OutputData.P2_Lmp_SeatRear_O);
                SetOutputValue(OutputId.P2_Lmp_SeatSpeaker_B, _OutputData.P2_Lmp_SeatRear_B);
                SetOutputValue(OutputId.Lmp_TMolding_R, _OutputData.Cab_Lmp_R);
                SetOutputValue(OutputId.Lmp_TMolding_G, _OutputData.Cab_Lmp_G);
                SetOutputValue(OutputId.Lmp_TMolding_B, _OutputData.Cab_Lmp_B);
                SetOutputValue(OutputId.Lmp_SeatDownLight, _OutputData.Cab_Lmp_RearSeat);
            }
        }

        #endregion
    }
}
