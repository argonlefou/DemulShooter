using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.IPC;
using DsCore.MameOutput;
using DsCore.RawInput;

namespace DemulShooter
{
    public class Game_ArcadepcMechaTornado : Game
    {
        private DsTcp_Client _Tcpclient;
        private DsTcp_OutputData_MechaTornado _OutputData;
        private DsTcp_InputData _InputData;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_ArcadepcMechaTornado(String RomName)
            : base(RomName, "mecha")
        {
            _KnownMd5Prints.Add("Mecha Tornado Arcade - v1.5", "32458270101d83dd6e0f08d0c617bf7e");

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
                            if (FindGameWindow_Equals("jixiesheqiu"))
                            {
                                String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", _Target_Process_Name + @"_Data\Managed\Assembly-CSharp.dll");
                                CheckMd5(AssemblyDllPath); 
                              
                                _InputData = new DsTcp_InputData();

                                //Start TcpClient to dial with Unity Game
                                _OutputData = new DsTcp_OutputData_MechaTornado();
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

        ~Game_ArcadepcMechaTornado()
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
            _Outputs.Add(new GameOutput(OutputDesciption.P3_LmpStart, OutputId.P3_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_LmpStart, OutputId.P4_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpFront, OutputId.P1_LmpFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpFront, OutputId.P2_LmpFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_LmpFront, OutputId.P3_LmpFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_LmpFront, OutputId.P4_LmpFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_GunMotor, OutputId.P3_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_GunMotor, OutputId.P4_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Shaker, OutputId.P1_Shaker));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Shaker, OutputId.P2_Shaker));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Shaker, OutputId.P3_Shaker));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Shaker, OutputId.P4_Shaker));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_WaterFire, OutputId.P1_WaterFire));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_WaterFire, OutputId.P2_WaterFire));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_WaterFire, OutputId.P3_WaterFire));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_WaterFire, OutputId.P4_WaterFire));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_BigGun, OutputId.P1_BigGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_BigGun, OutputId.P2_BigGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_BigGun, OutputId.P3_BigGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_BigGun, OutputId.P4_BigGun));
            //_Outputs.Add(new GameOutput(OutputDesciption.P1_TicketFeeder, OutputId.P1_TicketFeeder));
            //_Outputs.Add(new GameOutput(OutputDesciption.P2_TicketFeeder, OutputId.P2_TicketFeeder));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P3_CtmRecoil, OutputId.P3_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P4_CtmRecoil, OutputId.P4_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));       
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Life, OutputId.P3_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Life, OutputId.P4_Life));
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

                SetOutputValue(OutputId.P1_LmpStart, _OutputData.StartLight[0]);
                SetOutputValue(OutputId.P2_LmpStart, _OutputData.StartLight[1]);
                SetOutputValue(OutputId.P3_LmpStart, _OutputData.StartLight[2]);
                SetOutputValue(OutputId.P4_LmpStart, _OutputData.StartLight[3]);

                SetOutputValue(OutputId.P1_LmpFront, _OutputData.PlayerLight[0]);
                SetOutputValue(OutputId.P2_LmpFront, _OutputData.PlayerLight[1]);
                SetOutputValue(OutputId.P3_LmpFront, _OutputData.PlayerLight[2]);
                SetOutputValue(OutputId.P4_LmpFront, _OutputData.PlayerLight[3]);

                SetOutputValue(OutputId.P1_GunMotor, _OutputData.RotatingMotor[0]);
                SetOutputValue(OutputId.P2_GunMotor, _OutputData.RotatingMotor[1]);
                SetOutputValue(OutputId.P3_GunMotor, _OutputData.RotatingMotor[2]);
                SetOutputValue(OutputId.P4_GunMotor, _OutputData.RotatingMotor[3]);

                SetOutputValue(OutputId.P1_Shaker, _OutputData.Shake[0]);
                SetOutputValue(OutputId.P2_Shaker, _OutputData.Shake[1]);
                SetOutputValue(OutputId.P3_Shaker, _OutputData.Shake[2]);
                SetOutputValue(OutputId.P4_Shaker, _OutputData.Shake[3]);

                SetOutputValue(OutputId.P1_WaterFire, _OutputData.WaterPower[0]);
                SetOutputValue(OutputId.P2_WaterFire, _OutputData.WaterPower[1]);
                SetOutputValue(OutputId.P3_WaterFire, _OutputData.WaterPower[2]);
                SetOutputValue(OutputId.P4_WaterFire, _OutputData.WaterPower[3]);

                //SetOutputValue(OutputId.WaterPump, _OutputData.WaterPump);

                if (_OutputData.IsPlaying[0] == 1)
                {
                    SetOutputValue(OutputId.P1_Life, (int)_OutputData.Life[0]);
                    SetOutputValue(OutputId.P1_Damaged, (int)_OutputData.Damaged[0]);
                }
                else
                {
                    SetOutputValue(OutputId.P1_Life, 0);
                }

                if (_OutputData.IsPlaying[1] == 1)
                {
                    SetOutputValue(OutputId.P2_Life, (int)_OutputData.Life[0]);
                    SetOutputValue(OutputId.P2_Damaged, (int)_OutputData.Damaged[1]);
                }
                else
                {
                    SetOutputValue(OutputId.P2_Life, 0);
                }

                if (_OutputData.IsPlaying[2] == 1)
                {
                    SetOutputValue(OutputId.P3_Life, (int)_OutputData.Life[0]);
                    SetOutputValue(OutputId.P3_Damaged, (int)_OutputData.Damaged[2]);
                }
                else
                {
                    SetOutputValue(OutputId.P3_Life, 0);
                }

                if (_OutputData.IsPlaying[3] == 1)
                {
                    SetOutputValue(OutputId.P4_Life, (int)_OutputData.Life[0]);
                    SetOutputValue(OutputId.P4_Damaged, (int)_OutputData.Damaged[3]);
                }
                else
                {
                    SetOutputValue(OutputId.P4_Life, 0);
                }
            
                SetOutputValue(OutputId.P1_Credits, (int)_OutputData.Credits[0]);
                SetOutputValue(OutputId.P2_Credits, (int)_OutputData.Credits[1]);
                SetOutputValue(OutputId.P3_Credits, (int)_OutputData.Credits[2]);
                SetOutputValue(OutputId.P4_Credits, (int)_OutputData.Credits[3]);
            }
        }

        #endregion
    }
}
