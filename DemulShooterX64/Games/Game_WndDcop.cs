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
    class Game_WndDcop : Game
    {
        private DsTcp_Client _Tcpclient;
        private DsTcp_OutputData_Dcop _OutputData;

        //Thread-safe operation on input/output data
        //public static System.Object MutexLocker;

        private bool _HideCrosshair = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndDcop(String RomName, bool HideCrosshair, bool DisableInputHack, bool Verbose)
            : base(RomName, "DCOP", DisableInputHack, Verbose)
        {
            _HideCrosshair = HideCrosshair;

            _KnownMd5Prints.Add("DCOP - Tenoke ISO - Original", "3940b478b0069635b579c8bd2a6729c1");
            
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
                            if (FindGameWindow_Equals("DCOP"))
                            {
                                String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", @"DCOP_Data\Managed\Assembly-CSharp.dll");
                                CheckMd5(AssemblyDllPath);
                                
                                //Start TcpClient to dial with Unity Game
                                _OutputData = new DsTcp_OutputData_Dcop();
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

        ~Game_WndDcop()
        {
            if (_Tcpclient != null)
                _Tcpclient.Disconnect();
        }

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_DirectHit, OutputId.Lmp_DirectHit));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_PoliceBar, OutputId.Lmp_PoliceBar));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_GreenTestLight, OutputId.Lmp_GreenTestLight));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_RedLight, OutputId.Lmp_RedLight));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_WhiteStrobe, OutputId.Lmp_WhiteStrobe));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun, OutputId.P1_LmpGun));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));            
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
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

                SetOutputValue(OutputId.P1_GunRecoil, _OutputData.GunRecoil);
                SetOutputValue(OutputId.Lmp_DirectHit, _OutputData.DirectHit);
                SetOutputValue(OutputId.Lmp_PoliceBar, _OutputData.Police_LightBar);
                SetOutputValue(OutputId.Lmp_GreenTestLight, _OutputData.GreenTestLight);
                SetOutputValue(OutputId.Lmp_RedLight, _OutputData.RedLight);
                SetOutputValue(OutputId.Lmp_WhiteStrobe, _OutputData.WhiteStrobe);
                SetOutputValue(OutputId.P1_LmpGun, _OutputData.GunLight);
                SetOutputValue(OutputId.P1_Ammo, _OutputData.P1_Ammo);
                SetOutputValue(OutputId.P1_Life, _OutputData.P1_Life);

                SetOutputValue(OutputId.P1_CtmRecoil, _OutputData.GunRecoil);

                if (_P1_LastLife > _OutputData.P1_Life)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                _P1_LastLife = _OutputData.P1_Life;
            }
        }

        #endregion
    }
}
