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
    class Game_WndPointBlankX : Game
    {
        private DsTcp_Client _Tcpclient;
        private DsTcp_OutputData_PointBlankX _OutputData;
        private DsTcp_InputData_PointBlankX _InputData;

        //Thread-safe operation on input/output data
        //public static System.Object MutexLocker;

        protected float _P1_X_Value;
        protected float _P1_Y_Value;
        protected float _P2_X_Value;
        protected float _P2_Y_Value;

        private bool _HideCrosshair = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndPointBlankX(String RomName, bool HideCrosshair, bool DisableInputHack, bool Verbose)
            : base(RomName, "PBX100-2-NA-MPR0-A63", DisableInputHack, Verbose)
        {
            _HideCrosshair = HideCrosshair;
            _KnownMd5Prints.Add("Point Blank X - ROM PBX100-2-NA-MPR0-A63 - Original", "9aea1303f133b424c661ec897c67bf9e");
            _KnownMd5Prints.Add("Point Blank X - ROM PBX100-2-NA-MPR0-A63 - Patched", "70432507a3a9b66592d561259a9741ed");
            
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
                            if (FindGameWindow_Equals("PointBlankRevival"))
                            {
                                String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", @"PBX100-2-NA-MPR0-A63_Data\Managed\Assembly-CSharp.dll");
                                CheckMd5(AssemblyDllPath);

                                _InputData = new DsTcp_InputData_PointBlankX();

                                //Start TcpClient to dial with Unity Game
                                _OutputData = new DsTcp_OutputData_PointBlankX();
                                _Tcpclient = new DsTcp_Client("127.0.0.1", DsTcp_Client.DS_TCP_CLIENT_PORT);
                                _Tcpclient.PacketReceived += DsTcp_Client_PacketReceived;
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

        ~Game_WndPointBlankX()
        {
            if (_Tcpclient != null)
                _Tcpclient.Disconnect();
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

                    //Coordinates goes from [0.0, 0.0] in bottom left corner to [1.0, 1.0] in upper right 
                    float X_Value = (float)PlayerData.RIController.Computed_X / (float)TotalResX;
                    float Y_Value = 1.0f - ((float)PlayerData.RIController.Computed_Y / (float)TotalResY);

                    if (X_Value < 0.0f)
                        X_Value = 0.0f;
                    if (Y_Value < 0.0f)
                        Y_Value = 0.0f;
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpPanel, OutputId.P1_LmpPanel));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpPanel, OutputId.P2_LmpPanel));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
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

                SetOutputValue(OutputId.P1_LmpStart, _OutputData.P1_StartLED);
                SetOutputValue(OutputId.P2_LmpStart, _OutputData.P2_StartLED);

                SetOutputValue(OutputId.P1_LmpPanel, _OutputData.P1_LED);
                SetOutputValue(OutputId.P2_LmpPanel, _OutputData.P2_LED);

                SetOutputValue(OutputId.P1_Ammo, _OutputData.P1_Ammo);
                SetOutputValue(OutputId.P2_Ammo, _OutputData.P2_Ammo);

                SetOutputValue(OutputId.P1_Life, _OutputData.P1_Life);
                SetOutputValue(OutputId.P2_Life, _OutputData.P2_Life);

                SetOutputValue(OutputId.P1_CtmRecoil, _OutputData.P1_Recoil);
                SetOutputValue(OutputId.P2_CtmRecoil, _OutputData.P2_Recoil);

                SetOutputValue(OutputId.Credits, _OutputData.Credits);
            }
        }

        #endregion
    }
}
