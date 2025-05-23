﻿using System;
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
    class Game_ArcadePcMechaDino : Game
    {
        private DsTcp_Client _Tcpclient;
        private DsTcp_OutputData_MechaDino _OutputData;
        private DsTcp_InputData _InputData;

        //Thread-safe operation on input/output data
        //public static System.Object MutexLocker;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_ArcadePcMechaDino(String RomName)
            : base(RomName, "game")
        {
            _KnownMd5Prints.Add("Mechanical Dinosaur v6.5 - Original", "25dd74d2eeef2f61ed32ce439bcd264a");

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
                            if (FindGameWindow_Equals("RobotDragon"))
                            {
                                String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", _Target_Process_Name + @"_Data\Managed\Assembly-CSharp.dll");
                                CheckMd5(AssemblyDllPath);

                                _InputData = new DsTcp_InputData();

                                //Start TcpClient to dial with Unity Game
                                _OutputData = new DsTcp_OutputData_MechaDino();
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

        ~Game_ArcadePcMechaDino()
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun, OutputId.P1_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun, OutputId.P2_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_LmpGun, OutputId.P3_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_LmpGun, OutputId.P4_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_GunMotor, OutputId.P3_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_GunMotor, OutputId.P4_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpBonusWeapon, OutputId.P1_LmpBonusWeapon));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpBonusWeapon, OutputId.P2_LmpBonusWeapon));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_LmpBonusWeapon, OutputId.P3_LmpBonusWeapon));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_LmpBonusWeapon, OutputId.P4_LmpBonusWeapon));
            _Outputs.Add(new GameOutput(OutputDesciption.BonusWeaponLamp, OutputId.BonusWeaponLamp));
            _Outputs.Add(new GameOutput(OutputDesciption.SeatVibrationLamp, OutputId.SeatVibrationLamp));
            _Outputs.Add(new GameOutput(OutputDesciption.WaterFallLamp, OutputId.WaterFallLamp));
            _Outputs.Add(new GameOutput(OutputDesciption.WaterLevelLamp, OutputId.WaterLevelLamp));
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

                //Handling Start Lamps based on player status
                if (_OutputData.StartLamp[0] == 1)
                    SetOutputValue(OutputId.P1_CtmLmpStart, -1);
                else
                    SetOutputValue(OutputId.P1_CtmLmpStart, 0);

                if (_OutputData.StartLamp[1] == 1)
                    SetOutputValue(OutputId.P2_CtmLmpStart, -1);
                else
                    SetOutputValue(OutputId.P2_CtmLmpStart, 0);

                if (_OutputData.StartLamp[2] == 1)
                    SetOutputValue(OutputId.P3_CtmLmpStart, -1);
                else
                    SetOutputValue(OutputId.P3_CtmLmpStart, 0);

                if (_OutputData.StartLamp[3] == 1)
                    SetOutputValue(OutputId.P4_CtmLmpStart, -1);
                else
                    SetOutputValue(OutputId.P4_CtmLmpStart, 0);

                SetOutputValue(OutputId.P1_LmpGun, _OutputData.BallMotor[0]);
                SetOutputValue(OutputId.P2_LmpGun, _OutputData.BallMotor[1]);
                SetOutputValue(OutputId.P3_LmpGun, _OutputData.BallMotor[2]);
                SetOutputValue(OutputId.P4_LmpGun, _OutputData.BallMotor[3]);

                SetOutputValue(OutputId.P1_GunMotor, (int)(sbyte)_OutputData.SpineMotor[0]);
                SetOutputValue(OutputId.P2_GunMotor, (int)(sbyte)_OutputData.SpineMotor[1]);
                SetOutputValue(OutputId.P3_GunMotor, (int)(sbyte)_OutputData.SpineMotor[2]);
                SetOutputValue(OutputId.P4_GunMotor, (int)(sbyte)_OutputData.SpineMotor[3]);

                SetOutputValue(OutputId.P1_LmpBonusWeapon, _OutputData.PlayerBonusWeaponLamp[0]);
                SetOutputValue(OutputId.P2_LmpBonusWeapon, _OutputData.PlayerBonusWeaponLamp[1]);
                SetOutputValue(OutputId.P3_LmpBonusWeapon, _OutputData.PlayerBonusWeaponLamp[2]);
                SetOutputValue(OutputId.P4_LmpBonusWeapon, _OutputData.PlayerBonusWeaponLamp[3]);

                SetOutputValue(OutputId.BonusWeaponLamp, _OutputData.BonusWeaponLamp);
                SetOutputValue(OutputId.SeatVibrationLamp, _OutputData.SeatVibrationLamp);
                SetOutputValue(OutputId.WaterFallLamp, _OutputData.WaterLevelLamp);
                SetOutputValue(OutputId.WaterLevelLamp, _OutputData.WaterLevelLamp);

                SetOutputValue(OutputId.P1_Damaged, _OutputData.Damaged[0]);
                SetOutputValue(OutputId.P2_Damaged, _OutputData.Damaged[1]);
                SetOutputValue(OutputId.P3_Damaged, _OutputData.Damaged[2]);
                SetOutputValue(OutputId.P4_Damaged, _OutputData.Damaged[3]);

                if (_OutputData.IsPlaying[0] == 1)
                    SetOutputValue(OutputId.P1_Life, (int)_OutputData.Life[0]);
                else
                    SetOutputValue(OutputId.P1_Life, 0);
                if (_OutputData.IsPlaying[1] == 1)
                    SetOutputValue(OutputId.P2_Life, (int)_OutputData.Life[1]);
                else
                    SetOutputValue(OutputId.P2_Life, 0);
                if (_OutputData.IsPlaying[2] == 1)
                    SetOutputValue(OutputId.P3_Life, (int)_OutputData.Life[2]);
                else
                    SetOutputValue(OutputId.P4_Life, 0);
                if (_OutputData.IsPlaying[3] == 1)
                    SetOutputValue(OutputId.P1_Life, (int)_OutputData.Life[3]);
                else
                    SetOutputValue(OutputId.P4_Life, 0);

                SetOutputValue(OutputId.P1_Credits, (int)_OutputData.Credits[0]);
                SetOutputValue(OutputId.P2_Credits, (int)_OutputData.Credits[1]);
                SetOutputValue(OutputId.P3_Credits, (int)_OutputData.Credits[2]);
                SetOutputValue(OutputId.P4_Credits, (int)_OutputData.Credits[3]);
            }
        }

        #endregion
    }
}
