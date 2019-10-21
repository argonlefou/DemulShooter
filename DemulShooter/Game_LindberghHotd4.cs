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
        private const string P1_X_INIT_NOP_ADDRESS          = "0x081C9601|3";
        private const string P1_Y_INIT_NOP_ADDRESS          = "0x081C9604|3";
        private const string P1_TRIGGER_INIT_NOP_ADDRESS    = "0x081C9607|3";        
        private const string P1_RELOAD_INIT_NOP_ADDRESS     = "0x081C960A|3";
        private const string P1_WEAPONBTN_INIT_NOP_ADDRESS  = "0x081C960D|3";

        // Pointer address used to find the INPUT_SET struct containing both players data in game
        private const int BASE_PLAYER_DATA_PTR_OFFSET = 0x007157A8;
        // INPUT_SET direct address
        private int _Base_Player_Data_Address = 0;
        // INPUT_SET offsets to find data
        private const int P1_X_OFFSET       = 0x04;
        private const int P1_Y_OFFSET       = 0x05;
        private const int P1_TRIGGER_OFFSET = 0x08;
        private const int P1_RELOAD_OFFSET  = 0x0C;        
        private const int P1_WPNBTN_OFFSET  = 0x10;
        //+0x14 => ? 
        //+0x18 => ?
        //+0x1C => ?
        //                P1_START          = 0x20; 
        private const int P2_X_OFFSET       = 0x28;
        private const int P2_Y_OFFSET       = 0x29;
        private const int P2_TRIGGER_OFFSET = 0x2C;
        private const int P2_RELOAD_OFFSET  = 0x30;
        private const int P2_WPNBTN_OFFSET  = 0x34;
        //                P2_START          = 0x44;
                
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
                            
                            Buffer = ReadBytes(i + 0x130, 4);
                            i = BitConverter.ToInt32(Buffer, 0);
                            
                            Buffer = ReadBytes(i + 0x10, 4);
                            i = BitConverter.ToInt32(Buffer, 0);

                            Buffer = ReadBytes(i + 0x24, 4);
                            i = BitConverter.ToInt32(Buffer, 0);
                                                       
                            if (i != 0)
                            {
                                _Base_Player_Data_Address = i + 0x34;
                                WriteLog("PlayerData_Base_Address = 0x" + _Base_Player_Data_Address.ToString("X8"));

                                _ProcessHooked = true;
                                WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));

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
            SetHack_GunInit();
            SetHackV2();
            SetHackEnableP2();
            
            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        // CgunMgr::Init() => 0x081C95F4 ~ 0x081C9619
        // Init Axis and buttons values to 0
        // Not called that often (maybe after START or CONTINUE or new level), maybe not necessary to block them
        private void SetHack_GunInit()
        {
            SetNops(0, P1_X_INIT_NOP_ADDRESS);            
            SetNops(0, P1_Y_INIT_NOP_ADDRESS);
            //SetNops(0, P1_TRIGGER_INIT_NOP_ADDRESS);
            //SetNops(0, P1_RELOAD_INIT_NOP_ADDRESS);
            //SetNops(0, P1_WEAPONBTN_INIT_NOP_ADDRESS);
        }

        private void SetHackV2()
        {
            //Axis blocking
            SetNops(0, "0x08152EDB|3");
            SetNops(0, "0x08152EE3|3");
            //Trigger
            SetNops(0, "0x08152B69|3");
            //Weapon
            SetNops(0, "0x08152B6F|3");
            //Reload
            SetNops(0, "0x08152B6C|3");
            SetNops(0, "0x08152F00|7");
            WriteByte(_Base_Player_Data_Address + P1_RELOAD_OFFSET, 0x00);
            WriteByte(_Base_Player_Data_Address + P2_RELOAD_OFFSET, 0x00);
            //Init : center cursors for P1 and P2
            WriteByte(_Base_Player_Data_Address + P1_X_OFFSET, 0x7F);
            WriteByte(_Base_Player_Data_Address + P1_Y_OFFSET, 0x7F);
            WriteByte(_Base_Player_Data_Address + P2_X_OFFSET, 0x7F);
            WriteByte(_Base_Player_Data_Address + P2_Y_OFFSET, 0x7F);
        }

        // amCreditIsEnough() => 0x0831D800 ~~ 0x0831D895
        // Even though Freeplay is forced by TeknoParrot, this procedure always find "NO CREDITS" for P2
        // Replacing conditionnal Jump by single Jump force OK (for both players)
        private void SetHackEnableP2()
        {
            WriteByte(0x831d827, 0xEB);
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
                    WriteByte(_Base_Player_Data_Address + P1_WPNBTN_OFFSET, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    WriteByte(_Base_Player_Data_Address + P1_WPNBTN_OFFSET, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    WriteByte(_Base_Player_Data_Address + P1_RELOAD_OFFSET, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    WriteByte(_Base_Player_Data_Address + P1_RELOAD_OFFSET, 0x00);
                }
            }
            else if (Player == 2)
            {
                WriteByte(_Base_Player_Data_Address + P2_X_OFFSET, (byte)mouse.pTarget.X);
                WriteByte(_Base_Player_Data_Address + P2_Y_OFFSET, (byte)mouse.pTarget.Y);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte(_Base_Player_Data_Address + P2_TRIGGER_OFFSET, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte(_Base_Player_Data_Address + P2_TRIGGER_OFFSET, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    WriteByte(_Base_Player_Data_Address + P2_WPNBTN_OFFSET, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    WriteByte(_Base_Player_Data_Address + P2_WPNBTN_OFFSET, 0x00);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    WriteByte(_Base_Player_Data_Address + P2_RELOAD_OFFSET, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    WriteByte(_Base_Player_Data_Address + P2_RELOAD_OFFSET, 0x00);
                }
            }
        }

        #endregion
    }
}
