using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Media;
using System.Runtime.InteropServices;

namespace DemulShooter
{
    class Game_GvrFarCryV2 : Game
    {
        /*** MEMORY ADDRESSES **/
        protected const int P1_AXIS_PTR_OFFSET = 0x00592EE4;
        protected int _P1_X_Address = 0;
        protected int _P1_Y_Address = 0;
        protected string X_NOP_OFFSET = "0x003AC52C|3";
        protected string XMIN_NOP_OFFSET = "0x003AC544|3";
        protected string XMAX_NOP_OFFSET = "0x003AC550|7";
        protected string Y_NOP_OFFSET = "0x003AC53A|3";
        protected string YMIN_NOP_OFFSET = "0x003AC55B|3";
        protected string YMAX_NOP_OFFSET = "0x003AC568|7";

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_GvrFarCryV2(string RomName, bool Verbose) 
            : base ()
        {
            GetScreenResolution();
            
            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "FarCry_r";
            _KnownMd5Prints.Add("FarCry_r.exe by Mohkerz", "557d065632eaa3c8adb5764df1609976");

            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for Global VR " + _RomName + " game to hook.....");
        }

        /// <summary>
        /// Timer event when looking for Process (auto-Hook and auto-close)
        /// </summary>
        private void tProcess_Tick(Object Sender, EventArgs e)
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
                            byte[] Buffer = ReadBytes((int)_TargetProcess_MemoryBaseAddress + P1_AXIS_PTR_OFFSET, 4);
                            int i = BitConverter.ToInt32(Buffer, 0);

                            if (i != 0)
                            {
                                _P1_X_Address = i + 0x25C;
                                _P1_Y_Address = i + 0x260;
                                WriteLog("Player1 Axis address = 0x" + _P1_X_Address.ToString("X8"));

                                _ProcessHooked = true;
                                WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                ChecExeMd5();
                                SetHack();
                            }
                        }
                    }
                }
                catch
                {
                    WriteLog("Error trying to hook " + _Target_Process_Name + ".exe");
                }
            }
            else
            {
                Process[] processes = Process.GetProcessesByName(_Target_Process_Name);
                if (processes.Length <= 0)
                {
                    _ProcessHooked = false;
                    RemoveMouseHook();
                    _TargetProcess = null;
                    _ProcessHandle = IntPtr.Zero;
                    _TargetProcess_MemoryBaseAddress = IntPtr.Zero;
                    WriteLog(_Target_Process_Name + ".exe closed");
                    Environment.Exit(0);
                }
            }
        }

        #region MemoryHack

        private void SetHack()
        {
            SetNops((int)_TargetProcess_MemoryBaseAddress, X_NOP_OFFSET);
            SetNops((int)_TargetProcess_MemoryBaseAddress, XMIN_NOP_OFFSET);
            SetNops((int)_TargetProcess_MemoryBaseAddress, XMAX_NOP_OFFSET);
            SetNops((int)_TargetProcess_MemoryBaseAddress, Y_NOP_OFFSET);
            SetNops((int)_TargetProcess_MemoryBaseAddress, YMIN_NOP_OFFSET);
            SetNops((int)_TargetProcess_MemoryBaseAddress, YMAX_NOP_OFFSET);

            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            byte[] bufferX = BitConverter.GetBytes(mouse.pTarget.X);
            byte[] bufferY = BitConverter.GetBytes(mouse.pTarget.Y);

            if (Player == 1)
            {
                //Write Axis
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
