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
    class Game_ArcadePcMissionImpossible : Game
    {
        private DsTcp_Client _Tcpclient;
        private DsTcp_OutputData_MissionImpossible _OutputData;
        private DsTcp_InputData _InputData;

        //Thread-safe operation on input/output data
        //public static System.Object MutexLocker;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_ArcadePcMissionImpossible(String RomName)
            : base(RomName, "MissionImpossible")
        {
            _KnownMd5Prints.Add("Mission Impossible Arcade v201123 - Original", "aa47159c7b394366da6e7b8f402c52ef");

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
                            if (FindGameWindow_Equals("MissionImpossible"))
                            {
                                String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", _Target_Process_Name + @"_Data\Managed\Assembly-CSharp.dll");
                                CheckMd5(AssemblyDllPath);

                                _InputData = new DsTcp_InputData();

                                //Start TcpClient to dial with Unity Game
                                _OutputData = new DsTcp_OutputData_MissionImpossible();
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

        ~Game_ArcadePcMissionImpossible()
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
                {
                    if (Configurator.GetInstance().MissionImpossible_MergeTriggers)
                    {
                        _InputData.Trigger[PlayerData.ID - 1] = 1;
                        _InputData.Action[PlayerData.ID - 1] = 1;
                    }
                    else
                    {
                        _InputData.Trigger[PlayerData.ID - 1] = 1;
                    }
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    if (Configurator.GetInstance().MissionImpossible_MergeTriggers)
                    {
                        _InputData.Trigger[PlayerData.ID - 1] = 0;
                        _InputData.Action[PlayerData.ID - 1] = 0;
                    }
                    else
                    {
                        _InputData.Trigger[PlayerData.ID - 1] = 0;
                    }
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    if (!Configurator.GetInstance().MissionImpossible_MergeTriggers)
                    {
                        _InputData.Action[PlayerData.ID - 1] = 1;
                    }
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    if (!Configurator.GetInstance().MissionImpossible_MergeTriggers)
                    {
                        _InputData.Action[PlayerData.ID - 1] = 0;
                    }
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
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P1_CtmLmpStart, OutputId.P1_CtmLmpStart, 500));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P2_CtmLmpStart, OutputId.P2_CtmLmpStart, 500));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
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

                if (_OutputData.IsPlaying[0] == 1)
                {
                    SetOutputValue(OutputId.P1_CtmLmpStart, 0);
                    SetOutputValue(OutputId.P1_Ammo, (int)(_OutputData.AmmoGunL[0] + _OutputData.AmmoGunR[0]));  
                    SetOutputValue(OutputId.P1_CtmRecoil, _OutputData.Recoil[0]);
                    SetOutputValue(OutputId.P1_Life, (int)_OutputData.Life[0]);                    
                    SetOutputValue(OutputId.P1_Damaged, _OutputData.Damaged[0]);
                }
                else
                {
                    SetOutputValue(OutputId.P1_CtmLmpStart, -1);
                    SetOutputValue(OutputId.P1_Ammo, 0); 
                    SetOutputValue(OutputId.P1_Life, 0);
                }

                if (_OutputData.IsPlaying[1] == 1)
                {
                    SetOutputValue(OutputId.P2_CtmLmpStart, 0);
                    SetOutputValue(OutputId.P2_Ammo, (int)(_OutputData.AmmoGunL[1] + _OutputData.AmmoGunR[1])); 
                    SetOutputValue(OutputId.P2_CtmRecoil, _OutputData.Recoil[1]);
                    SetOutputValue(OutputId.P2_Life, (int)_OutputData.Life[1]);
                    SetOutputValue(OutputId.P2_Damaged, _OutputData.Damaged[1]);
                }
                else
                {
                    SetOutputValue(OutputId.P2_CtmLmpStart, -1);
                    SetOutputValue(OutputId.P2_Ammo, 0); 
                    SetOutputValue(OutputId.P2_Life, 0);
                }


                SetOutputValue(OutputId.Credits, (int)_OutputData.Credits);
            }
        }

        #endregion
    }
}
