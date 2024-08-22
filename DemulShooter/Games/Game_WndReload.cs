using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;
using DsCore.MameOutput;

namespace DemulShooter
{
    class Game_WndReload : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\windows\reload";

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
        //For Outputs
        private UInt32 _P1_Recoil_CaveAddress;
        private UInt32 _P1_Ammo_CaveAddress;

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

        private InjectionStruct _NoCrosshair_InjectionStruct = new InjectionStruct(0x0021874F, 5);
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x00263C0C, 5);
        private InjectionStruct _Ammo_InjectionStruct = new InjectionStruct(0x0021E3AC, 10);
        protected IntPtr _RldGameDll_ModuleBaseAddress = IntPtr.Zero;

        //Custom data to inject
        protected float _P1_X_Menu_Value;
        protected float _P1_Y_Menu_Value;
        protected float _P1_X_Shoot_Value;
        protected float _P1_Y_Shoot_Value;
        protected float _P1_Crosshair_X_Value;
        protected float _P1_Crosshair_Y_Value;

        //Keyboard KEYS
        protected HardwareScanCode _P1_HoldBreath_DIK = HardwareScanCode.DIK_SPACE;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndReload(String RomName, bool HideCrosshair, bool DisableInputHack, bool Verbose) 
            : base(RomName, "Reload", DisableInputHack, Verbose)
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
                                    _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                    Logger.WriteLog("rld_game.dll Module Base Address = 0x " + _RldGameDll_ModuleBaseAddress.ToString("X8"));
                                    CheckExeMd5();
                                    ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                                    Apply_MemoryHacks();
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
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

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
                    _P1_X_Shoot_Value = (2.0f * PlayerData.RIController.Computed_X / (float)TotalResX) - 1.0f;
                    _P1_Y_Shoot_Value = (2.0f * PlayerData.RIController.Computed_Y / (float)TotalResY) - 1.0f;

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
        protected override void Apply_InputsMemoryHack()
        {
            //Common procedure
            Create_InputsDataBank();
            _P1_X_Menu_CaveAddress = _InputsDatabank_Address;
            _P1_Y_Menu_CaveAddress = _InputsDatabank_Address + 0x04;
            _P1_X_Shoot_CaveAddress = _InputsDatabank_Address + 0x10;
            _P1_Y_Shoot_CaveAddress = _InputsDatabank_Address + 0x14;
            _P1_X_Crosshair_CaveAddress = _InputsDatabank_Address + 0x20;
            _P1_Y_Crosshair_CaveAddress = _InputsDatabank_Address + 0x24;

            if (_TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - IGG"])
            {
                SetHack_P1X_Menu();
                SetHack_P1Y_Menu();

                //If HideCrossair option is activated, the P1_Crosshair codecave will overwrite the NoCrosshair injection
                //That why we need to check before installing it
                if (!_HideCrosshair)
                    SetHack_P1_Crosshair();

                SetHack_P1_X_Shoot();
                SetHack_P1_Y_Shoot();
            }
            else if (_TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - Unknown release 1"] || _TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - Unknown release 2"])
            {
                SetHack_P1X_Menu_V2();
                SetHack_P1Y_Menu_V2();

                //If HideCrossair option is activated, the P1_Crosshair codecave will overwrite the NoCrosshair injection
                //That why we need to check before installing it
                if (!_HideCrosshair)
                    SetHack_P1_Crosshair_V2();

                SetHack_P1_X_Shoot_V2();
                SetHack_P1_Y_Shoot_V2();
            }

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// For outputs, we need to create a custom DataBank and inject code to intercept the gun firing event to create recoil
        /// </summary>
        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_Recoil_CaveAddress = _OutputsDatabank_Address;
            _P1_Ammo_CaveAddress = _OutputsDatabank_Address + 0x04;
            
            if (_TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - IGG"])
            {
                SetHack_Recoil();
                SetHack_Ammo();
            }
            else if (_TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - Unknown release 1"] || _TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - Unknown release 2"])
            {
                SetHack_Recoil_V2();
                SetHack_Ammo_V2();
            }
            
            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        protected override void Apply_NoCrosshairMemoryHack()
        {
            if (_TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - IGG"])
                SetHack_NoCrosshair();
            else if (_TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - Unknown release 1"] || _TargetProcess_Md5Hash == _KnownMd5Prints["Reload v1.0.0.1 - Unknown release 2"])
                SetHack_NoCrosshair_V2();
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

        /// <summary>
        /// csControllerActor::Shoot()  calls csControllerActorPlayer::Shoot() 	
        /// Intercepting the call will make us create a recoil signal+ 
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov byte ptr[_P1_Recoil_CaveAddress], 1
            CaveMemory.Write_StrBytes("C6 05");
            byte[] b = BitConverter.GetBytes(_P1_Recoil_CaveAddress);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("01");
            //mov ecx, esi
            CaveMemory.Write_StrBytes("8B CE");
            //fstp dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 1C 24");

            //Inject it
            CaveMemory.InjectToOffset(_RldGameDll_ModuleBaseAddress, _Recoil_InjectionStruct, "Recoil"); 
        }

        /// <summary>
        /// Reading the result of GetRemainingBullets() call in the ActorHud::StatusBar_AdvanceTime() loop
        /// </summary>
        private void SetHack_Ammo()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov [_P1_Ammo_CaveAddress], eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Ammo_CaveAddress));
            //mov esi,eax
            CaveMemory.Write_StrBytes("8B F0");
            //mov edx,[ecx]
            CaveMemory.Write_StrBytes("8B 11");
            //mov [ebp-00000148],esi
            CaveMemory.Write_StrBytes("89 B5 B8 FE FF FF");

            //Inject it
            CaveMemory.InjectToOffset(_RldGameDll_ModuleBaseAddress, _Ammo_InjectionStruct, "Ammo"); 
        }

        /// <summary>
        /// Set values to [-1.0; -1.0] to display crosshair in ActorHud::Crosshair_SetPosition() function
        /// </summary>
        private void SetHack_NoCrosshair()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov edi,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 7D 0C");
            //mov [edi],BF800000 { -1.00 }
            CaveMemory.Write_StrBytes("C7 07 00 00 80 BF");
            //mov [edi+04],BF800000 { -1.00 }
            CaveMemory.Write_StrBytes("C7 47 04 00 00 80 BF");
            //test edx,edx
            CaveMemory.Write_StrBytes("85 D2");

            //Inject it
            CaveMemory.InjectToOffset(_RldGameDll_ModuleBaseAddress, _NoCrosshair_InjectionStruct, "No Crosshair"); 
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

        /// <summary>
        /// csControllerActor::Shoot()  calls csControllerActorPlayer::Shoot() 	
        /// Intercepting the call will make us create a recoil signal+ 
        /// </summary>
        private void SetHack_Recoil_V2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov byte ptr[_P1_Recoil_CaveAddress], 1
            CaveMemory.Write_StrBytes("C6 05");
            byte[] b = BitConverter.GetBytes(_P1_Recoil_CaveAddress);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("01");
            //mov ecx, edi
            CaveMemory.Write_StrBytes("8B CF");
            //fstp dword ptr [esp]
            CaveMemory.Write_StrBytes("D9 1C 24");

            //Inject it
            CaveMemory.InjectToOffset(_RldGameDll_ModuleBaseAddress, _Recoil_InjectionStruct, "Recoil"); 
        }

        /// <summary>
        /// Reading the result of GetRemainingBullets() call in the ActorHud::StatusBar_AdvanceTime() loop
        /// </summary>
        private void SetHack_Ammo_V2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov [_P1_Ammo_CaveAddress], eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Ammo_CaveAddress));
            //mov edx,[ecx]
            CaveMemory.Write_StrBytes("8B 11");
            //mov ebp,eax
            CaveMemory.Write_StrBytes("8B E8");
            //mov eax,[edx+60]
            CaveMemory.Write_StrBytes("8B 42 60");

            //Inject it
            CaveMemory.InjectToOffset(_RldGameDll_ModuleBaseAddress, _Ammo_InjectionStruct, "Ammo"); 
        }

        /// <summary>
        /// Set values to [-1.0; -1.0] to display crosshair in ActorHud::Crosshair_SetPosition() function
        /// </summary>
        private void SetHack_NoCrosshair_V2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //push edi
            CaveMemory.Write_StrBytes("57");
            //mov edi,[esp+10]
            CaveMemory.Write_StrBytes("8B 7C 24 10");
            //mov [edi],BF800000 { -1.00 }
            CaveMemory.Write_StrBytes("C7 07 00 00 80 BF");
            //mov [edi+04],BF800000 { -1.00 }
            CaveMemory.Write_StrBytes("C7 47 04 00 00 80 BF");

            //Inject it
            CaveMemory.InjectToOffset(_RldGameDll_ModuleBaseAddress, _NoCrosshair_InjectionStruct, "No Crosshair"); 
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
                
                buffer = BitConverter.GetBytes(_P1_Crosshair_X_Value);
                WriteBytes(_P1_X_Crosshair_CaveAddress, buffer);
                buffer = BitConverter.GetBytes(_P1_Crosshair_Y_Value);
                WriteBytes(_P1_Y_Crosshair_CaveAddress, buffer);

                buffer = BitConverter.GetBytes(_P1_X_Shoot_Value);
                WriteBytes(_P1_X_Shoot_CaveAddress, buffer);
                buffer = BitConverter.GetBytes(_P1_Y_Shoot_Value);
                WriteBytes(_P1_Y_Shoot_CaveAddress, buffer);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    SendKeyDown(_P1_HoldBreath_DIK); 
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    SendKeyUp(_P1_HoldBreath_DIK);
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Custom Recoil will simply be read on memory and reset
            //the codecave injected will update it for the "ON" state
            byte P1_RecoilState = ReadByte(_P1_Recoil_CaveAddress);
            SetOutputValue(OutputId.P1_CtmRecoil, P1_RecoilState);
            if (P1_RecoilState == 1)
                WriteByte(_P1_Recoil_CaveAddress, 0x00);

            int P1_ammo = BitConverter.ToInt32(ReadBytes(_P1_Ammo_CaveAddress, 4), 0);
            SetOutputValue(OutputId.P1_Ammo, P1_ammo);
        }

        #endregion
    }
}
