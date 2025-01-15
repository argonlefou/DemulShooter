using System;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;

namespace DemulShooter
{
    class Game_GvrAliensHasp : Game
    {
        /*** MEMORY ADDRESSES **/
        private const UInt32 P1_X_OFFSET = 0x0556AEA8;
        private const UInt32 P1_Y_OFFSET = 0x0556AEAC;
        private const UInt32 P1_BTN_OFFSET = 0x0556ACBC;
        private const UInt32 P2_X_OFFSET = 0x0556B1C0;
        private const UInt32 P2_Y_OFFSET = 0x0556B1C4;
        private const UInt32 P2_BTN_OFFSET = 0x0556AFD4;
        private NopStruct _Nop_X = new NopStruct(0x0002EE9C, 6);
        private NopStruct _Nop_Y = new NopStruct(0x0002EEA5, 6);
        private NopStruct _Nop_Btn_1 = new NopStruct(0x0002EE75, 6);
        private NopStruct _Nop_Btn_2 = new NopStruct(0x0002EE81, 6);
        private NopStruct _Nop_Btn_3 = new NopStruct(0x0002EE8D, 3);
        private NopStruct _Nop_Btn_4 = new NopStruct(0x00048825, 2);

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_GvrAliensHasp(string RomName)
            : base(RomName, "abhRelease")
        {
            _KnownMd5Prints.Add("Aliens Extermination v1.03 US - Original", "755eea8c196592d63090bf56b4b0651b");
            

            _tProcess.Start();
            Logger.WriteLog("Waiting for Global VR " + _RomName + " game to hook.....");
        }


        /// <summary>
        /// Timer event when looking for Demul Process (auto-Hook and auto-close)
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

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero && _TargetProcess.MainWindowHandle != IntPtr.Zero)
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

                    //X => [0-FFFF] = 65535
                    //Y => [0-FFFF] = 65535
                    double dMaxX = 65535.0;
                    double dMaxY = 65535.0;

                    PlayerData.RIController.Computed_X = Convert.ToInt32(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt32(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));
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
        /// Easy hack, just blocking instructions to be able to inject custom values
        /// </summary>
        protected override void Apply_InputsMemoryHack()
        {
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_X);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Y);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Btn_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Btn_2);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Btn_3);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Btn_4);

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + P1_X_OFFSET, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + P1_Y_OFFSET, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + P1_BTN_OFFSET, 0x10);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + P1_BTN_OFFSET, 0xEF);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + P1_BTN_OFFSET, 0x20);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + P1_BTN_OFFSET, 0xDF);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + P1_BTN_OFFSET, 0x40);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + P1_BTN_OFFSET, 0xBF);
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + P2_X_OFFSET, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + P2_Y_OFFSET, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + P2_BTN_OFFSET, 0x10);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + P2_BTN_OFFSET, 0xEF);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + P2_BTN_OFFSET, 0x20);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + P2_BTN_OFFSET, 0xDF);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + P2_BTN_OFFSET, 0x40);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + P2_BTN_OFFSET, 0xBF);
            }
        }

        #endregion
    }
}
