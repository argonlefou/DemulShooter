using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace DemulShooter
{
    class Game_Reload : Game
    {
        private const string FOLDER_GAMEDATA = @"MemoryData\windows";
        private bool _HideCrosshair = false;

        /*** MEMORY ADDRESSES **/
        /// The game needs 3 different values to be overwritten :
        /// - Menu axis (float between 0 and windows size)
        /// - In Game crosshair (between -0.5 and +0.5)
        /// - In Game shooting axis (between -1.0 and +1.0 )        
        protected int _P1_X_Menu_Address;
        protected int _P1_Y_Menu_Address;
        protected int _P1_X_Shoot_Address;
        protected int _P1_Y_Shoot_Address;
        protected int _P1_X_Crosshair_Address;
        protected int _P1_Y_Crosshair_Address;

        protected int _P1_X_Menu_Injection_Offset;
        protected int _P1_X_Menu_Injection_Return_Offset;
        protected int _P1_Y_Menu_Injection_Offset;
        protected int _P1_Y_Menu_Injection_Return_Offset;
        protected int _P1_Menu_Fix_Offset;
        protected int _P1_InGame_Crosshair_Injection_Offset;
        protected int _P1_InGame_Crosshair_Injection_Return_Offset;
        protected int _P1_InGame_X_Injection_Offset;
        protected int _P1_InGame_X_Injection_Return_Offset;
        protected int _P1_InGame_Y_Injection_Offset;
        protected int _P1_InGame_Y_Injection_Return_Offset;       

        protected IntPtr _RldGameDll_ModuleBaseAddress = IntPtr.Zero;

        //Custom data to inject
        protected float _P1_X_Menu_Value;
        protected float _P1_Y_Menu_Value;
        protected float _P1_X_Shoot_Value;
        protected float _P1_Y_Shoot_Value;
        protected float _P1_Crosshair_X_Value;
        protected float _P1_Crosshair_Y_Value;

        //Keyboard KEYS
        protected short _P1_HoldBreath_DIK = 0x39; //T
        
        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Reload(string RomName, bool HideCrosshair, bool Verbose)
            : base()
        {
            GetScreenResolution();

            _RomName = RomName;
            _HideCrosshair = HideCrosshair;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "Reload";
            _KnownMd5Prints.Add("Reload IGG", "aaaf22c6671c12176d8317d4cc4b478d");

            ReadGameData();

            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for Windows Game " + _RomName + " game to hook.....");
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

                        ProcessModuleCollection c = _TargetProcess.Modules;
                        foreach (ProcessModule m in c)
                        {
                            if (m.ModuleName.ToLower().Equals("rld_game.dll"))
                            {
                                _RldGameDll_ModuleBaseAddress = m.BaseAddress;
                                if (_RldGameDll_ModuleBaseAddress != IntPtr.Zero)
                                {
                                    _ProcessHooked = true;
                                    WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                    WriteLog("rld_game.dll Module Base Address = 0x " + _RldGameDll_ModuleBaseAddress.ToString("X8"));
                                    ChecExeMd5();
                                    SetHack();

                                    break;
                                }
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

        #region File I/O

        /// <summary>
        /// Read memory values in .cfg file
        /// </summary>
        protected override void ReadGameData()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\" + _RomName + ".cfg"))
            {
                using (StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\" + _RomName + ".cfg"))
                {
                    string line;
                    line = sr.ReadLine();
                    while (line != null)
                    {
                        string[] buffer = line.Split('=');
                        if (buffer.Length > 1)
                        {
                            try
                            {
                                switch (buffer[0].ToUpper().Trim())
                                {
                                    case "P1_X_MENU_INJECTION_OFFSET":
                                        _P1_X_Menu_Injection_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_X_MENU_INJECTION_RETURN_OFFSET":
                                        _P1_X_Menu_Injection_Return_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_Y_MENU_INJECTION_OFFSET":
                                        _P1_Y_Menu_Injection_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_Y_MENU_INJECTION_RETURN_OFFSET":
                                        _P1_Y_Menu_Injection_Return_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_MENU_FIX_OFFSET":
                                        _P1_Menu_Fix_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_INGAME_CROSSHAIR_INJECTION_OFFSET":
                                        _P1_InGame_Crosshair_Injection_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_INGAME_CROSSHAIR_INJECTION_RETURN_OFFSET":
                                        _P1_InGame_Crosshair_Injection_Return_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_INGAME_X_INJECTION_OFFSET":
                                        _P1_InGame_X_Injection_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_INGAME_X_INJECTION_RETURN_OFFSET":
                                        _P1_InGame_X_Injection_Return_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_INGAME_Y_INJECTION_OFFSET":
                                        _P1_InGame_Y_Injection_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_INGAME_Y_INJECTION_RETURN_OFFSET":
                                        _P1_InGame_Y_Injection_Return_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;  
                                    default: break;
                                }
                            }
                            catch (Exception ex)
                            {
                                WriteLog("Error reading game data : " + ex.Message.ToString());
                            }
                        }
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }
            }
            else
            {
                WriteLog("File not found : " + AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\" + _RomName + ".cfg");
            }
        }

        #endregion

        #region Screen

        public override bool GameScale(MouseInfo Mouse, int Player)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    Win32.Rect TotalRes = new Win32.Rect();
                    Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    int TotalResX = TotalRes.Right - TotalRes.Left;
                    int TotalResY = TotalRes.Bottom - TotalRes.Top;

                    WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //During Menus :
                    //Y => [0; RezY] en float
                    //X => [0; RezX] en float                    

                    _P1_X_Menu_Value = (float)Mouse.pTarget.X;
                    _P1_Y_Menu_Value = (float)Mouse.pTarget.Y;

                    if (_P1_X_Menu_Value < 0.0f)
                        _P1_X_Menu_Value = 0.0f;
                    if (_P1_Y_Menu_Value < 0.0f)
                        _P1_Y_Menu_Value = 0.0f;
                    if (_P1_X_Menu_Value > (float)TotalResX)
                        _P1_X_Menu_Value = (float)TotalResX;
                    if (_P1_Y_Menu_Value > (float)TotalResY)
                        _P1_Y_Menu_Value = (float)TotalResY;                    

                    //In Game, Shoot :
                    //Y => [-1;1] en float
                    //X => [-1;1] en float
                    _P1_X_Shoot_Value = (2.0f * Mouse.pTarget.X / TotalResX) - 1.0f;
                    _P1_Y_Shoot_Value = (2.0f * Mouse.pTarget.Y / TotalResY) - 1.0f;

                    if (_P1_X_Shoot_Value < -1.0f)
                        _P1_X_Shoot_Value = -1.0f;
                    if (_P1_Y_Shoot_Value < -1.0f)
                        _P1_Y_Shoot_Value = -1.0f;
                    if (_P1_X_Shoot_Value > 1.0f)
                        _P1_X_Shoot_Value = 1.0f;
                    if (_P1_Y_Shoot_Value > 1.0f)
                        _P1_Y_Shoot_Value = 1.0f;

                    //In Game, Crosshair :
                    //Y => [-0.5;0.5] en float
                    //X => [-0.5;0.5] en float
                    _P1_Crosshair_X_Value = _P1_X_Shoot_Value / 2.0f;
                    _P1_Crosshair_Y_Value = _P1_Y_Shoot_Value / 2.0f;

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
            SetHack_Data();
            SetHack_P1X_Menu();
            SetHack_P1Y_Menu();
            SetHack_P1_Crosshair();
            SetHack_P1_X_Shoot();
            SetHack_P1_Y_Shoot();
            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        /*** Creating a custom memory bank to store our data ***/
        private void SetHack_Data()
        {
            //1st Codecave : storing our Axis Data
            Memory DataCaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            DataCaveMemory.Open();
            DataCaveMemory.Alloc(0x800);

            _P1_X_Menu_Address = DataCaveMemory.CaveAddress;
            _P1_Y_Menu_Address = DataCaveMemory.CaveAddress + 0x04;
            _P1_X_Shoot_Address = DataCaveMemory.CaveAddress + 0x10;
            _P1_Y_Shoot_Address = DataCaveMemory.CaveAddress + 0x14;
            _P1_X_Crosshair_Address = DataCaveMemory.CaveAddress + 0x20;
            _P1_Y_Crosshair_Address = DataCaveMemory.CaveAddress + 0x24;

            WriteLog("Custom data will be stored at : 0x" + _P1_X_Menu_Address.ToString("X8"));
        }

        /// <summary>
        /// All Axis codecave are the same :
        /// The game use some fstp [XXX] instruction, but we can't just NOP it as graphical glitches may appear.
        /// So we just add another set of instructions instruction immediatelly after to change the register 
        /// to our own desired value
        /// </summary>
        private void SetHack_P1X_Menu()
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_X_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P1_X_Menu_Address);
            CaveMemory.Write_Bytes(b);
            //movss xmm1, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 08");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [ecx+0000013C],xmm1
            CaveMemory.Write_StrBytes("F3 0F 11 89 3C 01 00 00");
            //return
            CaveMemory.Write_jmp((int)_RldGameDll_ModuleBaseAddress + _P1_X_Menu_Injection_Return_Offset);

            WriteLog("Adding P1_X CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((int)_RldGameDll_ModuleBaseAddress + _P1_X_Menu_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_RldGameDll_ModuleBaseAddress + _P1_X_Menu_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        }

        private void SetHack_P1Y_Menu()
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_Y_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P1_Y_Menu_Address);
            CaveMemory.Write_Bytes(b);
            //movss xmm0, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [ecx+0000014C],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 81 40 01 00 00");
            //return
            CaveMemory.Write_jmp((int)_RldGameDll_ModuleBaseAddress + _P1_Y_Menu_Injection_Return_Offset);

            WriteLog("Adding P1_X CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((int)_RldGameDll_ModuleBaseAddress + _P1_Y_Menu_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_RldGameDll_ModuleBaseAddress + _P1_Y_Menu_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        
            //Cursor is not stable, so a little mod:
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_RldGameDll_ModuleBaseAddress + _P1_Menu_Fix_Offset, new byte[] { 0xD9 }, 1, ref bytesWritten);
        
        }

        /// <summary>
        /// The gma eis using different procedures to handle mouse position in menu and in-game
        /// Only one codecave for Menus handling as X and Y are 4 instructions next to each other
        /// </summary>
        private void SetHack_P1_Crosshair()
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //fld dword ptr[_P1_X_Address]
            CaveMemory.Write_StrBytes("D9 05");
            byte[] b = BitConverter.GetBytes(_P1_X_Crosshair_Address);
            CaveMemory.Write_Bytes(b);
            //fstp dword ptr[edx+28]
            CaveMemory.Write_StrBytes("D9 5A 28");
            //fld dword ptr[_P1_Y_Address]
            CaveMemory.Write_StrBytes("D9 05");
            b = BitConverter.GetBytes(_P1_Y_Crosshair_Address);
            CaveMemory.Write_Bytes(b);
            //fstp dword ptr[edx+2C]
            CaveMemory.Write_StrBytes("D9 5A 2C");
            //return
            CaveMemory.Write_jmp((int)_RldGameDll_ModuleBaseAddress + _P1_InGame_Crosshair_Injection_Return_Offset);

            WriteLog("Adding P1_InGame_Crosshair CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((int)_RldGameDll_ModuleBaseAddress + _P1_InGame_Crosshair_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_RldGameDll_ModuleBaseAddress + _P1_InGame_Crosshair_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);       
        }

        private void SetHack_P1_X_Shoot()
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //mov eax [ebp + 10]
            CaveMemory.Write_StrBytes("8B 45 10");
            //fstp dword ptr[esi+48]
            CaveMemory.Write_StrBytes("D9 5E 48");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_X_Shoot_Address
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P1_X_Shoot_Address);
            CaveMemory.Write_Bytes(b);
            //push eax
            CaveMemory.Write_StrBytes("50");
            //fld dword ptr[esp]
            CaveMemory.Write_StrBytes("D9 04 24");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //fstp dword ptr[esi+48]
            CaveMemory.Write_StrBytes("D9 5E 48");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //return
            CaveMemory.Write_jmp((int)_RldGameDll_ModuleBaseAddress + _P1_InGame_X_Injection_Return_Offset);

            WriteLog("Adding P1_InGame_X CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((int)_RldGameDll_ModuleBaseAddress + _P1_InGame_X_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_RldGameDll_ModuleBaseAddress + _P1_InGame_X_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        
        }

        private void SetHack_P1_Y_Shoot()
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_Y_Shoot_Address
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P1_Y_Shoot_Address);
            CaveMemory.Write_Bytes(b);
            //push eax
            CaveMemory.Write_StrBytes("50");
            //fld dword ptr[esp]
            CaveMemory.Write_StrBytes("D9 04 24");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //fstp dword ptr[esi+4C]
            CaveMemory.Write_StrBytes("D9 5E 4C");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //cmp byte ptr [esi+000000CC],00
            CaveMemory.Write_StrBytes("80 BE CC 00 00 00 00");
            //return
            CaveMemory.Write_jmp((int)_RldGameDll_ModuleBaseAddress + _P1_InGame_Y_Injection_Return_Offset);

            WriteLog("Adding P1_InGame_Y CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((int)_RldGameDll_ModuleBaseAddress + _P1_InGame_Y_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_RldGameDll_ModuleBaseAddress + _P1_InGame_Y_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            if (Player == 1)
            {
                //Setting Values in memory for the Codecave to read it
                byte[] buffer = BitConverter.GetBytes(_P1_X_Menu_Value);
                WriteBytes(_P1_X_Menu_Address, buffer);
                buffer = BitConverter.GetBytes(_P1_Y_Menu_Value);
                WriteBytes(_P1_Y_Menu_Address, buffer);

                if (_HideCrosshair)
                    _P1_Crosshair_X_Value = -1.0f;
                buffer = BitConverter.GetBytes(_P1_Crosshair_X_Value);
                WriteBytes(_P1_X_Crosshair_Address, buffer);
                buffer = BitConverter.GetBytes(_P1_Crosshair_Y_Value);
                WriteBytes(_P1_Y_Crosshair_Address, buffer);

                buffer = BitConverter.GetBytes(_P1_X_Shoot_Value);
                WriteBytes(_P1_X_Shoot_Address, buffer);
                buffer = BitConverter.GetBytes(_P1_Y_Shoot_Value);
                WriteBytes(_P1_Y_Shoot_Address, buffer);

                if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    SendKeyDown(_P1_HoldBreath_DIK); 
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    SendKeyUp(_P1_HoldBreath_DIK);
                }
            }
        }
        
        #endregion
    }
}
