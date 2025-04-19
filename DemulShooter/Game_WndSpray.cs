using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;
namespace DemulShooter
{
    class Game_WndSpray : Game
    {
        //Memory values
        private InjectionStruct _CoinKeyCode_InjectionStruct = new InjectionStruct(0x0000A45A, 5);
        private NopStruct _Nop_P1IoControls = new NopStruct(0x0000AAA4, 5);
        private NopStruct _Nop_P2IoControls = new NopStruct(0x0000AA95, 5);
        private NopStruct _Nop_MouseAxis = new NopStruct(0x0000A401, 6);
        private UInt32 _Patch_MouseButtons_Offset = 0x0000A3B9;
        private UInt32 _P1_X_Offset = 0x0005AC8C;
        private UInt32 _P1_Y_Offset = 0x0005AC90;
        private UInt32 _P1_Trigger_Offset = 0x0005AC94;
        private UInt32 _P2_X_Offset = 0x0005AC70;
        private UInt32 _P2_Y_Offset = 0x0005AC74;
        private UInt32 _P2_Trigger_Offset = 0x0005AC78;
        private UInt32 _P1_Life_Offset = 0x00055094;
        private UInt32 _P2_Life_Offset = 0x00055098;
        private UInt32 _P1_Ammo_Offset = 0x000520A0;
        //private UInt32 _P2_Ammo_Offset = 0x000520A0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndSpray(String RomName)
            : base(RomName, "SprayGun")
        {
            _KnownMd5Prints.Add("Spray v1.0.0.1 - Original exe", "3f73bd67f4247900c12eea7932ea1517");
            _tProcess.Start();

            Logger.WriteLog("Waiting for Windows " + _RomName + " game to hook.....");
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

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            _GameWindowHandle = _TargetProcess.MainWindowHandle;
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();
                            Apply_MemoryHacks();
                            _ProcessHooked = true;
                            RaiseGameHookedEvent();
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

        #region Screen

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
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

                    //X => [0-640]
                    //Y => [0-480]
                    double dMaxX = 640.0;
                    double dMaxY = 480.0;

                    PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));

                    if (PlayerData.RIController.Computed_X < 0)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 0)
                        PlayerData.RIController.Computed_Y = 0;
                    if (PlayerData.RIController.Computed_X > (int)dMaxX)
                        PlayerData.RIController.Computed_X = (int)dMaxX;
                    if (PlayerData.RIController.Computed_Y > (int)dMaxY)
                        PlayerData.RIController.Computed_Y = (int)dMaxY;

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

        /// <summary>
        /// Genuine Hack, just blocking Axis and Triggers input to replace them.
        /// </summary>        
        protected override void Apply_InputsMemoryHack()
        {
            SetHack_CoinKey();

            //Disabling the calls to the functions overriding controls with IO board
            //SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1IoControls);
            //SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2IoControls);

            //Removing Mouse events to have full controls on P1 
            //Shunt WM buttons message tests
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Patch_MouseButtons_Offset, new byte[]{ 0xEB, 0x0D });
            //Same thing for Axis
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_MouseAxis);

            /*SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_LbuttonUp);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_RbuttonDown);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_RbuttonUp);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_MouseMove);

            //If a gamepad is detected, looks like it's looping and overriding values
            //This removes the whole loop, maybe just noping +10F5E / +10F70 is enough and less risky ?
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_GamePadLoop);

            //Some other kind of devices reset buttons states too
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_HidLoopTrigger_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_HidLoopTrigger_2);

            //By default, this PC-version force the coins counter to 9 when displaying the start screen. Removing it....
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_InitCoinsCounter);

            if (_HideCrosshair)
                SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_ShowCrosshairInGame);*/

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Coin KEY is set to ENTER and Handled in the message loop.
        /// The original code is switching from the parameter value, and not simply comparing it, so we need to change the value before the switch. 
        /// </summary>
        private void SetHack_CoinKey()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //cmp dword ptr [ebp+10],35
            CaveMemory.Write_StrBytes("83 7D 10 35");
            //jne Next
            CaveMemory.Write_StrBytes("75 04");
            //mov [ebp+10],0000000D
            CaveMemory.Write_StrBytes("C6 45 10 0D");
            //movsx edx,word ptr [ebp+14]
            CaveMemory.Write_StrBytes("0F BF 55 14");
            //push edx
            CaveMemory.Write_StrBytes("52");
            //Inject it
            CaveMemory.InjectToOffset(_CoinKeyCode_InjectionStruct, "Coin KEY");
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>        
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((UInt32)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((UInt32)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x00);
                }

                //To reload, set back Gaz tank capacity to 1.0f (full)
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Ammo_Offset, new byte[] { 0x00, 0x00, 0x80, 0x3F }); //1.0f
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    //
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Ammo_Offset, new byte[] { 0x00, 0x00, 0x80, 0x3F }); //1.0f
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    //
                }
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x00);
                }

                //To reload, set back Gaz tank capacity to 1.0f (full)
                /*if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Ammo_Offset, new byte[] { 0x00, 0x00, 0x80, 0x3F }); //1.0f
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    //
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Ammo_Offset, new byte[] { 0x00, 0x00, 0x80, 0x3F }); //1.0f
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    //
                }*/
            }
        }

        #endregion
    }
}
