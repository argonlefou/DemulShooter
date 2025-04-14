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
    class Game_ArcadepcMarsSortie : Game
    {
        private DsTcp_Client _Tcpclient;
        private DsTcp_OutputData_Marss _OutputData;
        private DsTcp_InputData_Marss _InputData;

        //Thread-safe operation on input/output data
        //public static System.Object MutexLocker;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_ArcadepcMarsSortie(String RomName)
            : base(RomName, "Shooter")
        {
            _KnownMd5Prints.Add("Mars Sortie v1.46.9 - Original", "01a643a3f615a22338c2505bcb1b9609");
            
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
                            if (FindGameWindow_Equals("Shooter"))
                            {
                                String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", _Target_Process_Name + @"_Data\Managed\Assembly-CSharp.dll");
                                CheckMd5(AssemblyDllPath); 
                              
                                _InputData = new DsTcp_InputData_Marss();

                                //Start TcpClient to dial with Unity Game
                                _OutputData = new DsTcp_OutputData_Marss();
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

        ~Game_ArcadepcMarsSortie()
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

                    //Coordinates goes from [0.0, 0.0] in bottom left corner to [1.0, 1.0] in upper right, no matter the display ratio
                    float X_Value = ((float)PlayerData.RIController.Computed_X / (float)TotalResX);
                    float Y_Value = (1.0f - ((float)PlayerData.RIController.Computed_Y / (float)TotalResY));

                    if (X_Value < 0.0f)
                        X_Value = 0.0f;
                    if (Y_Value < 0.0f)
                        Y_Value = 0.0f;
                    if (X_Value > (float)1.0f)
                        X_Value = (float)1.0f;
                    if (Y_Value > (float)1.0f)
                        Y_Value = (float)1.0f;

                    Logger.WriteLog("Computed float values = [ " + X_Value + "x" + Y_Value + " ]");

                    //Store data in [0-1000] range to store as Int and divise later and get float value
                    PlayerData.RIController.Computed_X = Convert.ToInt16(X_Value * 1000);
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(Y_Value * 1000);
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
                float fX = (float)PlayerData.RIController.Computed_X / 1000.0f;
                float fY = (float)PlayerData.RIController.Computed_Y / 1000.0f;

                if (PlayerData.ID == 1)
                {
                    _InputData.P1_X = fX;
                    _InputData.P1_Y = fY;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        _InputData.P1_Trigger = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        _InputData.P1_Trigger = 0;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        _InputData.P1_ChangeWeapon = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        _InputData.P1_ChangeWeapon = 0;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                        _InputData.P1_ChangeWeapon = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                        _InputData.P1_ChangeWeapon = 0;
                }
                else if (PlayerData.ID == 2)
                {
                    _InputData.P2_X = fX;
                    _InputData.P2_Y = fY;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        _InputData.P2_Trigger = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        _InputData.P2_Trigger = 0;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        _InputData.P2_ChangeWeapon = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        _InputData.P2_ChangeWeapon = 0;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                        _InputData.P2_ChangeWeapon = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                        _InputData.P2_ChangeWeapon = 0;
                }
                else if (PlayerData.ID == 3)
                {
                    _InputData.P3_X = fX;
                    _InputData.P3_Y = fY;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        _InputData.P3_Trigger = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        _InputData.P3_Trigger = 0;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        _InputData.P3_ChangeWeapon = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        _InputData.P3_ChangeWeapon = 0;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                        _InputData.P3_ChangeWeapon = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                        _InputData.P3_ChangeWeapon = 0;
                }
                else if (PlayerData.ID == 4)
                {
                    _InputData.P4_X = fX;
                    _InputData.P4_Y = fY;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        _InputData.P4_Trigger = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        _InputData.P4_Trigger = 0;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        _InputData.P4_ChangeWeapon = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        _InputData.P4_ChangeWeapon = 0;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                        _InputData.P4_ChangeWeapon = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                        _InputData.P4_ChangeWeapon = 0;
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
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_WhiteStrobe, OutputId.Lmp_WhiteStrobe));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P1_CtmLmpStart, OutputId.P1_CtmLmpStart, 500));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P2_CtmLmpStart, OutputId.P2_CtmLmpStart, 500));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P3_CtmLmpStart, OutputId.P3_CtmLmpStart, 500));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P4_CtmLmpStart, OutputId.P4_CtmLmpStart, 500));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P3_CtmRecoil, OutputId.P3_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P4_CtmRecoil, OutputId.P4_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));            
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
                SetOutputValue(OutputId.Lmp_WhiteStrobe, _OutputData.Flashlight);

                //Handling Start Lamps based on player status
                if (_OutputData.P1_Playing == 0)
                    SetOutputValue(OutputId.P1_CtmLmpStart, -1);
                else
                    SetOutputValue(OutputId.P1_CtmLmpStart, 0);

                if (_OutputData.P2_Playing == 0)
                    SetOutputValue(OutputId.P2_CtmLmpStart, -1);
                else
                    SetOutputValue(OutputId.P2_CtmLmpStart, 0);

                if (_OutputData.P3_Playing == 0)
                    SetOutputValue(OutputId.P3_CtmLmpStart, -1);
                else
                    SetOutputValue(OutputId.P3_CtmLmpStart, 0);

                if (_OutputData.P4_Playing == 0)
                    SetOutputValue(OutputId.P4_CtmLmpStart, -1);
                else
                    SetOutputValue(OutputId.P4_CtmLmpStart, 0);

                SetOutputValue(OutputId.P1_CtmRecoil, _OutputData.P1_Recoil);
                SetOutputValue(OutputId.P2_CtmRecoil, _OutputData.P2_Recoil);
                SetOutputValue(OutputId.P3_CtmRecoil, _OutputData.P3_Recoil);
                SetOutputValue(OutputId.P4_CtmRecoil, _OutputData.P4_Recoil);
                SetOutputValue(OutputId.P1_Credits, _OutputData.P1_Credits);
                SetOutputValue(OutputId.P2_Credits, _OutputData.P2_Credits);
                SetOutputValue(OutputId.P3_Credits, _OutputData.P3_Credits);
                SetOutputValue(OutputId.P4_Credits, _OutputData.P4_Credits);
            }
        }

        #endregion
    }
}
