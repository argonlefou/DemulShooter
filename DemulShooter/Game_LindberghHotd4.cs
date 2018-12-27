using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

namespace DemulShooter
{
    class Game_LindberghHotd4 : Game
    {
        /*
        private const string P1_X_NOP_OFFSET_1 = "0x07772EDB|3";
        private const string P1_X_NOP_OFFSET_2 = "0x077E9601|3";
        private const string P1_Y_NOP_OFFSET_1 = "0x07772EE3|3";
        private const string P1_Y_NOP_OFFSET_2 = "0x077E9604|3";
        private const string P1_OUT_NOP_OFFSET_1 = "0x07772B6C|3";
        private const string P1_OUT_NOP_OFFSET_2 = "0x07772F00|7";
        private const string P1_OUT_NOP_OFFSET_3 = "0x077E960A|3";
        private const string P1_TRIGGER_NOP_OFFSET_1 = "0x07772B69|3";
        private const string P1_TRIGGER_NOP_OFFSET_2 = "0x07772F31|7";
        private const string P1_TRIGGER_NOP_OFFSET_3 = "0x077E9607|3";
        private const string P1_BTN_NOP_OFFSET_1 = "0x07772B6F|3";
        private const string P1_BTN_NOP_OFFSET_2 = "0x07772F4B|7";
        private const string P1_BTN_NOP_OFFSET_3 = "0x077E960D|3";*/
        private const string P1_X_NOP_ADDRESS_1 = "0x08152EDB|3";
        private const string P1_X_NOP_ADDRESS_2 = "0x081C9601|3";
        private const string P1_Y_NOP_ADDRESS_1 = "0x08152EE3|3";
        private const string P1_Y_NOP_ADDRESS_2 = "0x081C9604|3";
        private const string P1_OUT_NOP_ADDRESS_1 = "0x08152B6C|3";
        private const string P1_OUT_NOP_ADDRESS_2 = "0x08152F00|7";
        private const string P1_OUT_NOP_ADDRESS_3 = "0x081C960A|3";
        private const string P1_TRIGGER_NOP_ADDRESS_1 = "0x08152B69|3";
        private const string P1_TRIGGER_NOP_ADDRESS_2 = "0x08152F31|7";
        private const string P1_TRIGGER_NOP_ADDRESS_3 = "0x081C9607|3";
        private const string P1_BTN_NOP_ADDRESS_1 = "0x08152B6F|3";
        private const string P1_BTN_NOP_ADDRESS_2 = "0x08152F4B|7";
        private const string P1_BTN_NOP_ADDRESS_3 = "0x081C960D|3";

        private const int BASE_PLAYER_DATA_PTR_OFFSET = 0x0013BF8C;

        private int _Base_Player_Data_Address = 0;
        private const int P1_X_OFFSET = 0x04;
        private const int P1_Y_OFFSET = 0x05;
        private const int P1_OUT_OFFSET = 0x0C;
        private const int P1_TRIGGER_OFFSET = 0x08;
        private const int P1_BTN_OFFSET = 0x10;
        
        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_LindberghHotd4(string RomName, bool Verbose)
            : base()
        {
            GetScreenResolution();

            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "BudgieLoader";

            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for Lindbergh " + _RomName + " game to hook.....");
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
                            byte[] Buffer = ReadBytes((int)_TargetProcess_MemoryBaseAddress + BASE_PLAYER_DATA_PTR_OFFSET, 4);
                            int i = BitConverter.ToInt32(Buffer, 0);
                            Buffer = ReadBytes(i + 0x300, 4);
                            i = BitConverter.ToInt32(Buffer, 0);
                            Buffer = ReadBytes(i, 4);
                            i = BitConverter.ToInt32(Buffer, 0);
                            
                            if (i != 0)
                            {
                                _Base_Player_Data_Address = i + 0x34;
                                WriteLog("PlayerData_Base_Address = 0x" + _Base_Player_Data_Address.ToString("X8"));

                                _ProcessHooked = true;
                                WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));

                                SetHack();
                                /*byte[] b = new byte[3];
                                b = ReadBytes((int)_TargetProcess_MemoryBaseAddress + Test_OFfset, 3);
                                for (int i = 0; i < 3; i++)
                                {
                                    WriteLog(b[0].ToString("X2") + " " + b[1].ToString("X2") + " " + b[2].ToString("X2"));
                                }*/
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
                    _TargetProcess = null;
                    _ProcessHandle = IntPtr.Zero;
                    _TargetProcess_MemoryBaseAddress = IntPtr.Zero;
                    WriteLog(_Target_Process_Name + ".exe closed");
                    Environment.Exit(0);
                }
            }
        }

        #region Screen

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// </summary>
        public override bool GameScale(MouseInfo Mouse, int Player)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    //Demul Window size
                    Win32.Rect TotalRes = new Win32.Rect();
                    Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");
                    //X and Y axis => 0x00 - 0xFF                    
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;
                    
                    Mouse.pTarget.X = Convert.ToInt16(Math.Round(dMaxX * Mouse.pTarget.X / TotalResX));
                    Mouse.pTarget.Y = Convert.ToInt16(Math.Round(dMaxY * Mouse.pTarget.Y / TotalResY));
                    if (Mouse.pTarget.X < 0)
                        Mouse.pTarget.X = 0;
                    if (Mouse.pTarget.Y < 0)
                        Mouse.pTarget.Y = 0;
                    if (Mouse.pTarget.X > (int)dMaxX)
                        Mouse.pTarget.X = (int)dMaxX;
                    if (Mouse.pTarget.Y > (int)dMaxY)
                        Mouse.pTarget.Y = (int)dMaxY;
                    return true;
                }
                catch (Exception ex)
                {
                    WriteLog("Error scaling mouse coordonates to GameFormat : " + ex.Message.ToString());
                }
            }
            return false;
        }

        #endregion

        #region MemoryHack

        private void SetHack()
        {            
            SetNops(0, P1_X_NOP_ADDRESS_1);
            SetNops(0, P1_X_NOP_ADDRESS_2);
            SetNops(0, P1_Y_NOP_ADDRESS_1);
            SetNops(0, P1_Y_NOP_ADDRESS_2);
            SetNops(0, P1_OUT_NOP_ADDRESS_1);
            SetNops(0, P1_OUT_NOP_ADDRESS_2);
            SetNops(0, P1_OUT_NOP_ADDRESS_3);
            SetNops(0, P1_TRIGGER_NOP_ADDRESS_1);
            SetNops(0, P1_TRIGGER_NOP_ADDRESS_2);
            SetNops(0, P1_TRIGGER_NOP_ADDRESS_3);
            SetNops(0, P1_BTN_NOP_ADDRESS_1);
            SetNops(0, P1_BTN_NOP_ADDRESS_2);
            SetNops(0, P1_BTN_NOP_ADDRESS_3);
            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }


        public override void SendInput(MouseInfo mouse, int Player)
        {
            if (Player == 1)
            {
                WriteByte(_Base_Player_Data_Address + P1_X_OFFSET, (byte)mouse.pTarget.X);
                WriteByte(_Base_Player_Data_Address + P1_Y_OFFSET, (byte)mouse.pTarget.Y);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte(_Base_Player_Data_Address + P1_TRIGGER_OFFSET, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte(_Base_Player_Data_Address + P1_TRIGGER_OFFSET, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    WriteByte(_Base_Player_Data_Address + P1_BTN_OFFSET, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    WriteByte(_Base_Player_Data_Address + P1_BTN_OFFSET, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    WriteByte(_Base_Player_Data_Address + P1_OUT_OFFSET, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    WriteByte(_Base_Player_Data_Address + P1_OUT_OFFSET, 0x00);
                }
            }
            else if (Player == 2)
            {
                
            }
        }

        #endregion
    }
}
