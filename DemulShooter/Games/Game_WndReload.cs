using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_WndReload : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\windows\reload";
        private bool _HideCrosshair = false;

        /*** MEMORY ADDRESSES **/
        /// The game needs 3 different values to be overwritten :
        /// - Menu axis (float between 0 and windows size)
        /// - In Game crosshair (between -0.5 and +0.5)
        /// - In Game shooting axis (between -1.0 and +1.0 )        
        private UInt32 _P1_X_Menu_CaveAddress;
        private UInt32 _P1_Y_Menu_CaveAddress;
        private UInt32 _P1_X_Shoot_CaveAddress;
        private UInt32 _P1_Y_Shoot_CaveAddress;
        private UInt32 _P1_X_Crosshair_CaveAddress;
        private UInt32 _P1_Y_Crosshair_CaveAddress;

        private UInt32 _P1_X_Menu_Injection_Offset = 0x001BD16C;
        private UInt32 _P1_X_Menu_Injection_Return_Offset = 0x001BD174;
        private UInt32 _P1_Y_Menu_Injection_Offset = 0x001BD1D3;
        private UInt32 _P1_Y_Menu_Injection_Return_Offset = 0x001BD1DB;
        private UInt32 _P1_Menu_Fix_Offset = 0x001BD185;
        private UInt32 _P1_InGame_Crosshair_Injection_Offset = 0x00218756;
        private UInt32 _P1_InGame_Crosshair_Injection_Return_Offset = 0x00218761;
        private UInt32 _P1_InGame_X_Injection_Offset = 0x0027CFD1;
        private UInt32 _P1_InGame_X_Injection_Return_Offset = 0x0027CFD7;
        private UInt32 _P1_InGame_Y_Injection_Offset = 0x0027D027;
        private UInt32 _P1_InGame_Y_Injection_Return_Offset = 0x0027D02E;       

        protected IntPtr _RldGameDll_ModuleBaseAddress = IntPtr.Zero;

        //Custom data to inject
        protected float _P1_X_Menu_Value;
        protected float _P1_Y_Menu_Value;
        protected float _P1_X_Shoot_Value;
        protected float _P1_Y_Shoot_Value;
        protected float _P1_Crosshair_X_Value;
        protected float _P1_Crosshair_Y_Value;

        //Keyboard KEYS
        protected HardwareScanCode _P1_HoldBreath_DIK = HardwareScanCode.DIK_T;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndReload(String RomName, bool HideCrosshair, bool DisableInputHack, bool Verbose) 
            : base(RomName, "Reload", 00.0, DisableInputHack, Verbose)
        {
            _HideCrosshair = HideCrosshair;
            _KnownMd5Prints.Add("Reload v1.0.0.1 - IGG", "aaaf22c6671c12176d8317d4cc4b478d");
            _KnownMd5Prints.Add("Reload v1.0.0.1 - Unknown release 1", "f3c4068a49f07aa99d2a92544d5c5748");
            _KnownMd5Prints.Add("Reload v1.0.0.1 - Unknown release 2", "2e1e22229c90b53153d2c015371e643a");   

            _tProcess.Start();
            Logger.WriteLog("Waiting for Windows Game " + _RomName + " game to hook.....");
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

                        ProcessModuleCollection c = _TargetProcess.Modules;
                        foreach (ProcessModule m in c)
                        {
                            if (m.ModuleName.ToLower().Equals("rld_game.dll"))
                            {
                                _RldGameDll_ModuleBaseAddress = m.BaseAddress;
                                if (_RldGameDll_ModuleBaseAddress != IntPtr.Zero)
                                {
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                    Logger.WriteLog("rld_game.dll Module Base Address = 0x " + _RldGameDll_ModuleBaseAddress.ToString("X8"));
                                    CheckExeMd5();
                                    ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                                    if (!_DisableInputHack)
                                        SetHack();
                                    else
                                        Logger.WriteLog("Input Hack disabled");
                                    _ProcessHooked = true;
                                    RaiseGameHookedEvent();

                                    break;
                                }
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
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    int TotalResX = TotalRes.Right - TotalRes.Left;
                    int TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //During Menus :
                    //Y => [0; RezY] en float
                    //X => [0; RezX] en float                    

                    _P1_X_Menu_Value = (float)PlayerData.RIController.Computed_X;
                    _P1_Y_Menu_Value = (float)PlayerData.RIController.Computed_Y;

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
                    _P1_X_Shoot_Value = (2.0f * PlayerData.RIController.Computed_X / TotalResX) - 1.0f;
                    _P1_Y_Shoot_Value = (2.0f * PlayerData.RIController.Computed_Y / TotalResY) - 1.0f;

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
                    Logger.WriteLog("Error scaling mouse coordonates to GameFormat : " + ex.Message.ToString());
                }
            }
            return false;
        }

        #endregion

        #region Memory Hack

        /// <summary>
        /// So far I ran into 2 versions of the game, both with different .exe and .dll files
        /// Although the hack is the same, .dll files have slighty different asm codes so for some of them we need to rewrite
        /// it all differently, because only changing offsets won't be enough
        /// </summary>
        private void SetHack()
        {
            //Common procedure
            CreateDataBank();

            if (_TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - IGG"])
            {
                SetHack_P1X_Menu();
                SetHack_P1Y_Menu();
                SetHack_P1_Crosshair();
                SetHack_P1_X_Shoot();
                SetHack_P1_Y_Shoot();
            }
            else if (_TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - Unknown release 1"]
                        || _TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - Unknown release 2"])
            {
                SetHack_P1X_Menu_V2();
                SetHack_P1Y_Menu_V2();
                SetHack_P1_Crosshair_V2();
                SetHack_P1_X_Shoot_V2();
                SetHack_P1_Y_Shoot_V2();
            }

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Creating a custom memory bank to store our data
        /// </summary>
        private void CreateDataBank()
        {
            //1st Codecave : storing our Axis Data
            Codecave DataCaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            DataCaveMemory.Open();
            DataCaveMemory.Alloc(0x800);

            _P1_X_Menu_CaveAddress = DataCaveMemory.CaveAddress;
            _P1_Y_Menu_CaveAddress = DataCaveMemory.CaveAddress + 0x04;
            _P1_X_Shoot_CaveAddress = DataCaveMemory.CaveAddress + 0x10;
            _P1_Y_Shoot_CaveAddress = DataCaveMemory.CaveAddress + 0x14;
            _P1_X_Crosshair_CaveAddress = DataCaveMemory.CaveAddress + 0x20;
            _P1_Y_Crosshair_CaveAddress = DataCaveMemory.CaveAddress + 0x24;

            Logger.WriteLog("Custom data will be stored at : 0x" + _P1_X_Menu_CaveAddress.ToString("X8"));
        }

        #endregion

        #region Memory Hack_ExeV1

        /// <summary>
        /// All Axis codecave are the same :
        /// The game use some fstp [XXX] instruction, but we can't just NOP it as graphical glitches may appear.
        /// So we just add another set of instructions immediatelly after to change the register 
        /// to our own desired value
        /// </summary>
        private void SetHack_P1X_Menu()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_X_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P1_X_Menu_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //movss xmm1, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 08");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [ecx+0000013C],xmm1
            CaveMemory.Write_StrBytes("F3 0F 11 89 3C 01 00 00");
            //return
            CaveMemory.Write_jmp((UInt32)_RldGameDll_ModuleBaseAddress + _P1_X_Menu_Injection_Return_Offset);

            Logger.WriteLog("Adding P1_X CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_RldGameDll_ModuleBaseAddress + _P1_X_Menu_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_RldGameDll_ModuleBaseAddress + _P1_X_Menu_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        private void SetHack_P1Y_Menu()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_Y_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P1_Y_Menu_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //movss xmm0, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [ecx+0000014C],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 81 40 01 00 00");
            //return
            CaveMemory.Write_jmp((UInt32)_RldGameDll_ModuleBaseAddress + _P1_Y_Menu_Injection_Return_Offset);

            Logger.WriteLog("Adding P1_X CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_RldGameDll_ModuleBaseAddress + _P1_Y_Menu_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_RldGameDll_ModuleBaseAddress + _P1_Y_Menu_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

            //Cursor is not stable, so a little mod:
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_RldGameDll_ModuleBaseAddress + _P1_Menu_Fix_Offset, new byte[] { 0xD9 }, 1, ref bytesWritten);

        }

        /// <summary>
        /// The game is using different procedures to handle mouse position in menu and in-game
        /// Only one codecave for Menus handling as X and Y are 4 instructions next to each other
        /// </summary>
        private void SetHack_P1_Crosshair()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //fld dword ptr[_P1_X_Address]
            CaveMemory.Write_StrBytes("D9 05");
            byte[] b = BitConverter.GetBytes(_P1_X_Crosshair_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //fstp dword ptr[edx+28]
            CaveMemory.Write_StrBytes("D9 5A 28");
            //fld dword ptr[_P1_Y_Address]
            CaveMemory.Write_StrBytes("D9 05");
            b = BitConverter.GetBytes(_P1_Y_Crosshair_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //fstp dword ptr[edx+2C]
            CaveMemory.Write_StrBytes("D9 5A 2C");
            //return
            CaveMemory.Write_jmp((UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_Crosshair_Injection_Return_Offset);

            Logger.WriteLog("Adding P1_InGame_Crosshair CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_Crosshair_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_Crosshair_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        private void SetHack_P1_X_Shoot()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
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
            byte[] b = BitConverter.GetBytes(_P1_X_Shoot_CaveAddress);
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
            CaveMemory.Write_jmp((UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_X_Injection_Return_Offset);

            Logger.WriteLog("Adding P1_InGame_X CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_X_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_X_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

        }

        private void SetHack_P1_Y_Shoot()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_Y_Shoot_Address
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P1_Y_Shoot_CaveAddress);
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
            CaveMemory.Write_jmp((UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_Y_Injection_Return_Offset);

            Logger.WriteLog("Adding P1_InGame_Y CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_Y_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_Y_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        #endregion
        
        #region Memory Hack_ExeV2

        /// <summary>
        /// All Axis codecave are the same :
        /// The game use some fstp [XXX] instruction, but we can't just NOP it as graphical glitches may appear.
        /// So we just add another set of instructions immediatelly after to change the register 
        /// to our own desired value
        /// </summary>
        private void SetHack_P1X_Menu_V2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_X_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P1_X_Menu_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //movss xmm1, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 08");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [esp+4+arg0],xmm1
            CaveMemory.Write_StrBytes("F3 0F 11 4C 24 08");
            //return
            CaveMemory.Write_jmp((UInt32)_RldGameDll_ModuleBaseAddress + _P1_X_Menu_Injection_Return_Offset);

            Logger.WriteLog("Adding P1_X CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_RldGameDll_ModuleBaseAddress + _P1_X_Menu_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_RldGameDll_ModuleBaseAddress + _P1_X_Menu_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        private void SetHack_P1Y_Menu_V2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_Y_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P1_Y_Menu_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //movss xmm0, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [ecx+00000140],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 81 40 01 00 00");
            //return
            CaveMemory.Write_jmp((UInt32)_RldGameDll_ModuleBaseAddress + _P1_Y_Menu_Injection_Return_Offset);

            Logger.WriteLog("Adding P1_X CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_RldGameDll_ModuleBaseAddress + _P1_Y_Menu_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_RldGameDll_ModuleBaseAddress + _P1_Y_Menu_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

            //Cursor is not stable, so a little mod:
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_RldGameDll_ModuleBaseAddress + _P1_Menu_Fix_Offset, new byte[] { 0xD9 }, 1, ref bytesWritten);

        }

        /// <summary>
        /// The game is using different procedures to handle mouse position in menu and in-game
        /// Only one codecave for Menus handling as X and Y are 4 instructions next to each other
        /// </summary>
        private void SetHack_P1_Crosshair_V2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //fld dword ptr[_P1_X_Address]
            CaveMemory.Write_StrBytes("D9 05");
            byte[] b = BitConverter.GetBytes(_P1_X_Crosshair_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //fstp dword ptr[edx+24]
            CaveMemory.Write_StrBytes("D9 5A 24");
            //fld dword ptr[_P1_Y_Address]
            CaveMemory.Write_StrBytes("D9 05");
            b = BitConverter.GetBytes(_P1_Y_Crosshair_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //fstp dword ptr[edx+28]
            CaveMemory.Write_StrBytes("D9 5A 28");
            //return
            CaveMemory.Write_jmp((UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_Crosshair_Injection_Return_Offset);

            Logger.WriteLog("Adding P1_InGame_Crosshair CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_Crosshair_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_Crosshair_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        private void SetHack_P1_X_Shoot_V2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //mov eax [esp + 54h + arg_8]
            CaveMemory.Write_StrBytes("8B 4C 24 60");
            //fstp dword ptr[esi+48]
            CaveMemory.Write_StrBytes("D9 5E 48");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_X_Shoot_Address
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P1_X_Shoot_CaveAddress);
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
            CaveMemory.Write_jmp((UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_X_Injection_Return_Offset);

            Logger.WriteLog("Adding P1_InGame_X CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_X_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_X_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

        }

        private void SetHack_P1_Y_Shoot_V2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_Y_Shoot_Address
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P1_Y_Shoot_CaveAddress);
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
            CaveMemory.Write_jmp((UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_Y_Injection_Return_Offset);

            Logger.WriteLog("Adding P1_InGame_Y CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_Y_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_RldGameDll_ModuleBaseAddress + _P1_InGame_Y_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
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
                //Setting Values in memory for the Codecave to read it
                byte[] buffer = BitConverter.GetBytes(_P1_X_Menu_Value);
                WriteBytes(_P1_X_Menu_CaveAddress, buffer);
                buffer = BitConverter.GetBytes(_P1_Y_Menu_Value);
                WriteBytes(_P1_Y_Menu_CaveAddress, buffer);

                if (_HideCrosshair)
                    _P1_Crosshair_X_Value = -1.0f;
                buffer = BitConverter.GetBytes(_P1_Crosshair_X_Value);
                WriteBytes(_P1_X_Crosshair_CaveAddress, buffer);
                buffer = BitConverter.GetBytes(_P1_Crosshair_Y_Value);
                WriteBytes(_P1_Y_Crosshair_CaveAddress, buffer);

                buffer = BitConverter.GetBytes(_P1_X_Shoot_Value);
                WriteBytes(_P1_X_Shoot_CaveAddress, buffer);
                buffer = BitConverter.GetBytes(_P1_Y_Shoot_Value);
                WriteBytes(_P1_Y_Shoot_CaveAddress, buffer);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    SendKeyDown(_P1_HoldBreath_DIK); 
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    SendKeyUp(_P1_HoldBreath_DIK);
            }
        }

        #endregion
    }
}
