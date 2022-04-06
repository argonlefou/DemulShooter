using System;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;

namespace DemulShooter
{
    class Game_GvrFarCryV2 : Game
    {
        /*** MEMORY ADDRESSES **/
        private const UInt32 P1_AXIS_PTR_OFFSET = 0x00592EE4;
        private UInt32 _P1_X_Address = 0;
        private UInt32 _P1_Y_Address = 0;
        private NopStruct _Nop_X = new NopStruct(0x003AC52C, 3);
        private NopStruct _Nop_XMin = new NopStruct(0x003AC544, 3);
        private NopStruct _Nop_XMax = new NopStruct(0x003AC550, 7);
        private NopStruct _Nop_Y = new NopStruct(0x003AC53A, 3);
        private NopStruct _Nop_YMin = new NopStruct(0x003AC55B, 3);
        private NopStruct _Nop_YMax = new NopStruct(0x003AC568, 7);

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_GvrFarCryV2(String RomName, double _ForcedXratio, bool DisableInputHack, bool Verbose)
            : base(RomName, "FarCry_r", _ForcedXratio, DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Far Cry Paradise", "263325AC3F7685CBA12B280D8E927A5D");
            _KnownMd5Prints.Add("Far Cry Paradise by Mohkerz", "557d065632eaa3c8adb5764df1609976");

            _tProcess.Start();
            Logger.WriteLog("Waiting for Global VR " + _RomName + " game to hook.....");
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
                            byte[] Buffer = ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + P1_AXIS_PTR_OFFSET, 4);
                            UInt32 i = BitConverter.ToUInt32(Buffer, 0);

                            if (i != 0)
                            {
                                _P1_X_Address = i + 0x25C;
                                _P1_Y_Address = i + 0x260;
                                Logger.WriteLog("Player1 Axis address = 0x" + _P1_X_Address.ToString("X8"));
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                CheckExeMd5();
                                if (_DisableInputHack)
                                    SetHack();
                                else
                                    Logger.WriteLog("Input Hack disabled");
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

        #region Memory Hack

        private void SetHack()
        {
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_X);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_XMin);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_XMax);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Y);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_YMin);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_YMax);

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }
        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes(PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes(PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes(_P1_X_Address, bufferX);
                WriteBytes(_P1_Y_Address, bufferY);

                //Inputs
                /*if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    SendKeyDown(_P1_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    SendKeyUp(_P1_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    if (P1_Next_Dir.Equals("left"))
                        SendKeyDown(_P1_Left_DIK);
                    else
                        SendKeyDown(_P1_Right_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    if (P1_Next_Dir.Equals("left"))
                    {
                        SendKeyUp(_P1_Left_DIK);
                        P1_Next_Dir = "right";
                    }
                    else
                    {
                        SendKeyUp(_P1_Right_DIK);
                        P1_Next_Dir = "left";
                    }
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    SendKeyDown(_P1_Reload_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    SendKeyUp(_P1_Reload_DIK);
                }*/
            }
            /*else if (Player == 2)
            {
                //Write Axis
                WriteBytes(_P2_X_Address, bufferX);
                WriteBytes(_P2_Y_Address, bufferY);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    SendKeyDown(_P2_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    SendKeyUp(_P2_Trigger_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    if (P2_Next_Dir.Equals("left"))
                        SendKeyDown(_P2_Left_DIK);
                    else
                        SendKeyDown(_P2_Right_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    if (P2_Next_Dir.Equals("left"))
                    {
                        SendKeyUp(_P2_Left_DIK);
                        P2_Next_Dir = "right";
                    }
                    else
                    {
                        SendKeyUp(_P2_Right_DIK);
                        P2_Next_Dir = "left";
                    }
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    SendKeyDown(_P2_Reload_DIK);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    SendKeyUp(_P2_Reload_DIK);
                }
            }*/
        }

        #endregion
    }
}
