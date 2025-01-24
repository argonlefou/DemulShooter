using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.IPC;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_SegaPvzLastStand : Game
    {
        private DsTcp_Client _Tcpclient;
        private DsTcp_InputData_Pvz _InputData;

        //Inputs
        private UInt32 _SystemButtons_Offset = 0x00119A41;
        private UInt32 _TriggerButton_Offset = 0x001120CA;

        //Outputs
        private UInt32 _Outputs_Offset = 0x00118AC0;
        private UInt32 _Credits_Offset = 0x0010AF80;
        private InjectionStruct _CustomDamage_InjectionStruct = new InjectionStruct(0x000091BD, 5);
        private UInt32 _CustomDamageOriginalDword_Offset = 0x00072080;
        private UInt32 _P1_DamageStatus_CaveAddress = 0;

        //
        private HardwareScanCode _DIK_Coin = HardwareScanCode.DIK_5;
        private HardwareScanCode _DIK_Trigger = HardwareScanCode.DIK_1;

        //This game needs to hook to 2 different processes :
        //shell.exe for outputs hack
        //Pvz.exe for Inputs and WindowSize
        private Process _PvzWindowProcess;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_SegaPvzLastStand(String RomName)
            : base(RomName, "shell")
        {
            _KnownMd5Prints.Add("Shell.exe v1.0.0 - Original", "c875cf47549a70f2e94c4059b52be2eb");
            _KnownMd5Prints.Add("Shell.exe v1.0.0 - Patch_v2 by Ducon", "48fdf6363dde5fac142864689d5757c8");
            _KnownMd5Prints.Add("Shell.exe v1.0.0 - Patch_vArgon by Ducon", "878736c94d934bc105d15a2c19192889");

            _tProcess.Start();
            Logger.WriteLog("Waiting for TTX " + _RomName + " game to hook.....");
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
                    Process[] PvzProcesses = Process.GetProcessesByName("pvz");
                    if (processes.Length > 0 && PvzProcesses.Length > 0)
                    {
                        _TargetProcess = PvzProcesses[0];
                        _PvzWindowProcess = PvzProcesses[0];
                        // The game may start with other Windows than the main one (BepInEx console, other stuff.....) so we need to filter
                        // the displayed window according to the Title, if DemulShooter is started before the game,  to hook the correct one
                        if (FindGameWindow_Equals("PvzCore"))
                        {
                            _TargetProcess = processes[0];
                            _ProcessHandle = _TargetProcess.Handle;
                            _TargetProcess_MemoryBaseAddress = _TargetProcess.MainModule.BaseAddress;
                            if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                            {
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                CheckExeMd5();
                                Apply_MemoryHacks();

                                _InputData = new DsTcp_InputData_Pvz();

                                //Start TcpClient to dial with Unity Game
                                _Tcpclient = new DsTcp_Client("127.0.0.1", DsTcp_Client.DS_TCP_CLIENT_PORT);
                                _Tcpclient.TcpConnected += DsTcp_client_TcpConnected;
                                _Tcpclient.Connect();

                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
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

        ~Game_SegaPvzLastStand()
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
                    
                    if (PlayerData.RIController.Computed_X < 0)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 0)
                        PlayerData.RIController.Computed_Y = 0;
                    if (PlayerData.RIController.Computed_X > (int)TotalResX)
                        PlayerData.RIController.Computed_X = (int)TotalResX;
                    if (PlayerData.RIController.Computed_Y > (int)TotalResY)
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

        #region Memory Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_DamageStatus_CaveAddress = _OutputsDatabank_Address + 0x00;

            SetHack_Damage();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Code injection where the game is calling for rumble because of damage.
        /// That way we can known when a player is damaged and make our own output.
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov _P1_DamageStatus_CaveAddress, 1
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_DamageStatus_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //push Shell.exe+OFFSET
            CaveMemory.Write_StrBytes("68");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress +_CustomDamageOriginalDword_Offset));
            
            //Inject it
            CaveMemory.InjectToOffset(_CustomDamage_InjectionStruct, "Dammage");
        }

        #endregion

        
        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>
        public override void SendInput(PlayerSettings PlayerData)
        { 
            if (PlayerData.ID == 1)
            {
                _InputData.P1_X = (float)PlayerData.RIController.Computed_X;
                _InputData.P1_Y = (float)PlayerData.RIController.Computed_Y;

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _TriggerButton_Offset, 0x01);
                    _InputData.P1_Trigger = 1;
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _TriggerButton_Offset, 0x00);
                    _InputData.P1_Trigger = 0;
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _TriggerButton_Offset, 0x01);
                    _InputData.P1_Trigger = 1;
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _TriggerButton_Offset, 0x00);
                    _InputData.P1_Trigger = 0;
                }
            }

            if (_HideCrosshair)
                _InputData.HideCrosshairs = 1;
            else
                _InputData.HideCrosshairs = 0;

            _Tcpclient.SendMessage(_InputData.ToByteArray());
        }

        /// <summary>
        /// Low-level Keyboard hook callback.
        /// Shell.exe window need to have focus to register system button or Fire button 
        /// Using this hook we can modify the Shell memory directly when it's not focused
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    if (s.scanCode == _DIK_Coin)
                    {
                        Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _SystemButtons_Offset, 0x40);
                    }
                    if (s.scanCode == _DIK_Trigger)
                    {
                        WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _TriggerButton_Offset, 0x01);
                    }
                }
                else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                {
                    if (s.scanCode == _DIK_Coin)
                    {
                        Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _SystemButtons_Offset, 0xBF);
                    }
                    if (s.scanCode == _DIK_Trigger)
                    {
                        WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _TriggerButton_Offset, 0x00);
                    }
                }
            }
            return Win32API.CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
        }

        #endregion
        

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : Is activated for every bullet fired AND when player gets
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun_R, OutputId.P1_LmpGun_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun_G, OutputId.P1_LmpGun_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun_B, OutputId.P1_LmpGun_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_R, OutputId.P1_Lmp_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_G, OutputId.P1_Lmp_G));
            _Outputs.Add(new GameOutput(OutputDesciption.TicketDrive, OutputId.TicketDrive));
            _Outputs.Add(new GameOutput(OutputDesciption.TicketMeter, OutputId.TicketMeter));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            byte OutputData = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset);
            SetOutputValue(OutputId.P1_LmpGun_R, OutputData & 0x01);
            SetOutputValue(OutputId.P1_LmpGun_G, OutputData >> 1 & 0x01);
            SetOutputValue(OutputId.P1_LmpGun_B, OutputData >> 2 & 0x01);            
            SetOutputValue(OutputId.P1_GunRecoil, OutputData >> 3 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_R, OutputData >> 4 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_G, OutputData >> 5 & 0x01);
            SetOutputValue(OutputId.TicketDrive, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset +1) >> 1 & 0x01);

            //[Recoil] custom Output
            SetOutputValue(OutputId.P1_CtmRecoil, OutputData >> 3 & 0x01);

            //Custom Outputs:
            //[Damaged] custom Output
            if (ReadByte(_P1_DamageStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P1_Damaged, 1);
                WriteByte(_P1_DamageStatus_CaveAddress, 0x00);
            }           

            //Credits
            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
