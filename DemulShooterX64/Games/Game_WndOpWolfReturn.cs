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
    public class Game_WndOpWolfReturn : Game
    {
        private DsTcp_Client _Tcpclient;
        private DsTcp_OutputData_OpWolfReturn _OutputData;
        private DsTcp_InputData_OpwolfReturn _InputData;

        //Thread-safe operation on input/output data
        public static System.Object MutexLocker;

        private byte _HideCrosshair = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndOpWolfReturn(String RomName, bool HideCrosshair, bool DisableInputHack, bool Verbose)
            : base(RomName, "OperationWolf", DisableInputHack, Verbose)
        {
            if (HideCrosshair)
                _HideCrosshair = 1;
            _KnownMd5Prints.Add("Operation Wolf Returns - COG", "6c32e74cda2fd1953245158382cf188a");
            _tProcess.Start();
            Logger.WriteLog("Waiting for Coastal " + _RomName + " game to hook.....");
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
                            if (FindGameWindow_Equals("Operation Wolf Returns: First Mission"))
                            {
                                String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", @"OperationWolf_Data\Managed\Assembly-CSharp.dll");
                                CheckMd5(AssemblyDllPath);

                                _InputData = new DsTcp_InputData_OpwolfReturn();

                                //Start TcpClient to dial with Unity Game
                                _OutputData = new DsTcp_OutputData_OpWolfReturn();
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

        ~Game_WndOpWolfReturn()
        {
            if (_Tcpclient != null)
                _Tcpclient.Disconnect();
        }

        #region Screen

        /// <summary>
        /// Inverted Y axis, 0 is on top
        /// </summary>
        public override bool GameScale(PlayerSettings PlayerData)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    PlayerData.RIController.Computed_Y = Convert.ToInt16((int)TotalResY - PlayerData.RIController.Computed_Y);
                    if (PlayerData.RIController.Computed_X < 0)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 0)
                        PlayerData.RIController.Computed_Y = 0;
                    if (PlayerData.RIController.Computed_X > TotalResX)
                        PlayerData.RIController.Computed_X = (int)TotalResX;
                    if (PlayerData.RIController.Computed_Y > TotalResX)
                        PlayerData.RIController.Computed_Y = (int)TotalResY;

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
                    _InputData.P1_X = (UInt16)PlayerData.RIController.Computed_X;
                    _InputData.P1_Y = (UInt16)PlayerData.RIController.Computed_Y;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        _InputData.P1_Trigger = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                        _InputData.P1_Reload = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        _InputData.P1_ChangeWeapon = 1;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        _InputData.P1_Trigger = 0;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                        _InputData.P1_Reload = 0;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        _InputData.P1_ChangeWeapon = 0;
                }
                else if (PlayerData.ID == 2)
                {
                    _InputData.P2_X = (UInt16)PlayerData.RIController.Computed_X;
                    _InputData.P2_Y = (UInt16)PlayerData.RIController.Computed_Y;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        _InputData.P2_Trigger = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                        _InputData.P2_Reload = 1;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        _InputData.P2_ChangeWeapon = 1;

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        _InputData.P2_Trigger = 0;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                        _InputData.P2_Reload = 0;
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        _InputData.P2_ChangeWeapon = 0;
                }

                //Small hack to send a DS param to Unity plugin.....not really an Input variable but eh....
                _InputData.HideCrosshair = _HideCrosshair;

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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
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

                SetOutputValue(OutputId.P1_Ammo, _OutputData.P1_Ammo);
                SetOutputValue(OutputId.P2_Ammo, _OutputData.P2_Ammo);

                SetOutputValue(OutputId.P1_Life, _OutputData.P1_Life);
                SetOutputValue(OutputId.P2_Life, _OutputData.P2_Life);

                SetOutputValue(OutputId.P1_CtmRecoil, _OutputData.P1_Recoil);
                SetOutputValue(OutputId.P2_CtmRecoil, _OutputData.P2_Recoil);

                SetOutputValue(OutputId.P1_Damaged, _OutputData.P1_Damage);
                SetOutputValue(OutputId.P2_Damaged, _OutputData.P2_Damage);
            }
        }

        #endregion
    }


}
