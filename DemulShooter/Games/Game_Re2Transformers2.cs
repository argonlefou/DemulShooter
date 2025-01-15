﻿using System;
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
    class Game_Re2Transformers2 : Game
    {
        //Memory values
        private UInt32 _pPlayerManager_Address = 0x200F7CD8;
        private UInt32 _pCreditsManager_Address = 0x200E9750;
        private UInt32 _CPlayer1_Offset = 0x34;
        private UInt32 _CPlayer2_Offset = 0x38;
        private UInt32 _Cplayer_ID_Offset = 0x60;
        private UInt32 _Cplayer_Mode_Offset = 0x64;
        private UInt32 _Cplayer_Life_Offset = 0x68;

        private UInt32 _CreditMgr_IncCoin_Function_Offset = 0x00066DE0;
        private UInt32 _FrameRateMgr_ExecServer_Function_Offset = 0x00D70D0;
        private UInt32 _GmMgr_GetGameMode_Function_Offset = 0x000E43C0;
        private UInt32 _GetResolution_Function_Offset = 0x002608B0;
        private UInt32 _PlMgr_IsHoldMode_FunctionOffset = 0x0024DDE0;
        private UInt32 _LaserSight_Patch_Offset = 0x002E558A;
        private UInt32 _PlayerInputMode_Patch_Offset = 0x000F751A;

        private InjectionStruct _Axis_InjectionStruct = new InjectionStruct(0x000F75CD, 6);
        private InjectionStruct _Buttons_InjectionStruct = new InjectionStruct(0x000F787F, 5);
        private InjectionStruct _NoCrosshair_InjectionStruct = new InjectionStruct(0x000F6DB0, 5);
        private InjectionStruct _FixCrosshair_InjectionStruct = new InjectionStruct(0x000F6DB5, 7);
        private InjectionStruct _FixEnnemyTarget_InjectionStruct = new InjectionStruct(0x004EEF9D, 5);
        private InjectionStruct _Credits_InjectionStruct = new InjectionStruct(0x00240BCB, 5);
        private InjectionStruct _StartLamps_InjectionStruct = new InjectionStruct(0x00067159, 5);
        private InjectionStruct _GunLamps_InjectionStruct = new InjectionStruct(0x000FC124, 5);
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x0024AAFF, 6);
        private InjectionStruct _Damage_InjectionStruct = new InjectionStruct(0x00246C69, 6);

        //Custom Input Address
        private UInt32 _P1_StartOn_Address;
        private UInt32 _P1_StartPress_Address;
        private UInt32 _P2_StartOn_Address;
        private UInt32 _P2_StartPress_Address;
        private UInt32 _P1_TriggerOn_Address;
        private UInt32 _P1_TriggerPress_Address;
        private UInt32 _P2_TriggerOn_Address;
        private UInt32 _P2_TriggerPress_Address;
        private UInt32 _LeverFrontOn_Address;
        private UInt32 _LeverFrontPress_Address;
        private UInt32 _LeverBackOn_Address;
        private UInt32 _LeverBackPress_Address;
        private UInt32 _P1_X_Address;
        private UInt32 _P1_Y_Address;
        private UInt32 _P2_X_Address;
        private UInt32 _P2_Y_Address;
        private UInt32 _AddCredit_Address;
        private UInt32 _P1_LmpStart_Address;
        private UInt32 _P2_LmpStart_Address;
        private UInt32 _P1_LmpGun_Address;
        private UInt32 _P2_LmpGun_Address;
        private UInt32 _P1_Recoil_Address;
        private UInt32 _P2_Recoil_Address;
        private UInt32 _P1_Damage_Address;
        private UInt32 _P2_Damage_Address;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Re2Transformers2(String RomName)
            : base(RomName, "Transformers2")
        {
            _KnownMd5Prints.Add("Transformers Shadow Rising v180605 - Original Dump", "b3b1f4ad6408d6ee946761a00f761455");
            _tProcess.Start();

            Logger.WriteLog("Waiting for RingEdge2 " + _RomName + " game to hook.....");
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

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            _GameWindowHandle = _TargetProcess.MainWindowHandle;
                            //Running DemulShooter before the game can cause it to find an empty window at start
                            if (_GameWindowHandle == IntPtr.Zero)
                                return;

                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            Logger.WriteLog("GameWindow Title = " + Get_GameWindowTitle());
                            CheckExeMd5();
                            //ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
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

                    //Axis values : 0x00 -> 0xFF
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;

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
        /// Genuine Hack, just blocking Axis and filtering Triggers input to replace them without blocking other input
        /// </summary>
        protected override void Apply_InputsMemoryHack()
        {
            // This will be used to store custom input values
            // Games has 2 distinct requets for button : "press" and "on"
            Create_InputsDataBank();
            _P1_StartOn_Address = _InputsDatabank_Address;
            _P2_StartOn_Address = _InputsDatabank_Address + 0x04;
            _P1_StartPress_Address = _InputsDatabank_Address + 0x08;
            _P2_StartPress_Address = _InputsDatabank_Address + 0x0C;
            _P1_TriggerOn_Address = _InputsDatabank_Address + 0x10;
            _P2_TriggerOn_Address = _InputsDatabank_Address + 0x14;
            _P1_TriggerPress_Address = _InputsDatabank_Address + 0x18;
            _P2_TriggerPress_Address = _InputsDatabank_Address + 0x1C;
            _LeverFrontOn_Address = _InputsDatabank_Address + 0x20;
            _LeverFrontPress_Address = _InputsDatabank_Address + 0x24;
            _LeverBackOn_Address = _InputsDatabank_Address + 0x28;
            _LeverBackPress_Address = _InputsDatabank_Address + 0x2C;
            _P1_X_Address = _InputsDatabank_Address + 0x30;
            _P1_Y_Address = _InputsDatabank_Address + 0x34;
            _P2_X_Address = _InputsDatabank_Address + 0x38;
            _P2_Y_Address = _InputsDatabank_Address + 0x3C;
            _AddCredit_Address = _InputsDatabank_Address + 0x40;

            SetHack_Axis();
            SetHack_Buttons();
            SetHack_Credits();
            SetHack_CorrectReticlePosition();
            SetHack_CorrectEnnemyTarget();

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }        

        /// <summary>
        ///Hacking Axis proc : 
        ///In CGunMgr::MainProc()
        ///Input type beeing force to "Mouse", the hack will separate P1/P2 proc (EBX value) and put values in ShotX and ShotX members of the INPUT struct
        ///At the end : ESI+4 receives X, and ESI+5 for Y (one byte each)
        /// </summary>>
        private void SetHack_Axis()
        {
            //Forcing The InputMode to "1" (mouse) in the switch check in CGunMgr::MainProc loop
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _PlayerInputMode_Patch_Offset, new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0x90, 0x90, 0x90, 0x90 });

            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //sar eax, 8
            CaveMemory.Write_StrBytes("C1 F8 08");
            //test ebx,ebx
            CaveMemory.Write_StrBytes("85 DB");
            //jne Player2
            CaveMemory.Write_StrBytes("75 14");
            //mov eax,[_P1_X_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_X_Address));
            //mov [esi+4], al
            CaveMemory.Write_StrBytes("88 46 04");
            //mov eax,[_P1_Y_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Y_Address));
            //mov [esi+5], al
            CaveMemory.Write_StrBytes("88 46 05");
            //jmp exit
            CaveMemory.Write_StrBytes("EB 12");
            //mov eax,[_P2_X_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_X_Address));
            //mov [esi+4], al
            CaveMemory.Write_StrBytes("88 46 04");
            //mov eax,[_P2_Y_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Y_Address));
            //mov [esi+5], al
            CaveMemory.Write_StrBytes("88 46 05");

            //Inject it
            CaveMemory.InjectToOffset(_Axis_InjectionStruct, "Axis");
        }

        /// <summary>
        /// Ath the end of the CGun::MainProc we are overwritting buttons values with data read on our custom Databank
        /// Looks like putting 1 in the Button_On byte is not necesary for gameplay (autofire still on) and causes repeated inputs on Name entry screen
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //push edi
            CaveMemory.Write_StrBytes("57");
            //shl ebx, 2
            CaveMemory.Write_StrBytes("C1 E3 02");

            //mov eax, _P1_TriggerPress_Address
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_TriggerPress_Address));
            //add eax, ebx
            CaveMemory.Write_StrBytes("01 D8");
            //mov edi, [eax]
            CaveMemory.Write_StrBytes("8B 38");
            //mov [esi+08],edi
            CaveMemory.Write_StrBytes("89 7E 08");
            
            ////mov eax, _P1_TriggerOn_Address
            //CaveMemory.Write_StrBytes("B8");
            //CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_TriggerOn_Address));
            ////add eax, ebx
            //CaveMemory.Write_StrBytes("01 D8");
            ////mov edi, [eax]
            //CaveMemory.Write_StrBytes("8B 38");
            ////mov [esi+0C],edi
            //CaveMemory.Write_StrBytes("89 7E 0C");

            //mov eax, _P1_StartPress_Address
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_StartPress_Address));
            //add eax, ebx
            CaveMemory.Write_StrBytes("01 D8");
            //mov edi, [eax]
            CaveMemory.Write_StrBytes("8B 38");
            //mov [esi+28],edi
            CaveMemory.Write_StrBytes("89 7E 28");

            ////mov eax, _P1_StartOn_Address
            //CaveMemory.Write_StrBytes("B8");
            //CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_StartOn_Address));
            ////add eax, ebx
            //CaveMemory.Write_StrBytes("01 D8");
            ////mov edi, [eax]
            //CaveMemory.Write_StrBytes("8B 38");
            ////mov [esi+2C],edi
            //CaveMemory.Write_StrBytes("89 7E 2C");

            //mov eax, _LeverFrontPress_Address
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_LeverFrontPress_Address));
            //mov edi, [eax]
            CaveMemory.Write_StrBytes("8B 38");
            //mov [esi+30],edi
            CaveMemory.Write_StrBytes("89 7E 30");

            //mov eax, _LeverFrontOn_Address
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_LeverFrontOn_Address));
            //mov edi, [eax]
            CaveMemory.Write_StrBytes("8B 38");
            //mov [esi+34],edi
            CaveMemory.Write_StrBytes("89 7E 34");

            //mov eax, _LeverBackPress_Address
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_LeverBackPress_Address));
            //mov edi, [eax]
            CaveMemory.Write_StrBytes("8B 38");
            //mov [esi+38],edi
            CaveMemory.Write_StrBytes("89 7E 38");

            //mov eax, _LeverBackOn_Address
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_LeverBackOn_Address));
            //mov edi, [eax]
            CaveMemory.Write_StrBytes("8B 38");
            //mov [esi+3C],edi
            CaveMemory.Write_StrBytes("89 7E 3C");

            //shr ebx, 2
            CaveMemory.Write_StrBytes("C1 EB 02");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //xor eax, eax
            CaveMemory.Write_StrBytes("31 C0");

            //Inject it
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Buttons");
        }

        /// <summary>
        /// Adding a byte check on a custom "Set Credits" flag in the App::Main loop
        /// That way we can call an existing function to add credits and play sound
        /// Changing the credits value is quicker but sound will not be played....
        /// </summary>
        private void SetHack_Credits()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //cmp dword ptr [_AddCredit_Address],00
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AddCredit_Address));
            CaveMemory.Write_StrBytes("00");
            //je originalcode
            CaveMemory.Write_StrBytes("0F 84 14 00 00 00");
            //mov [_AddCredit_Address],00
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AddCredit_Address));
            CaveMemory.Write_StrBytes("00 00 00 00");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");
            //call CreditMgr_IncCoin()
            CaveMemory.Write_call((UInt32)_TargetProcess_MemoryBaseAddress + _CreditMgr_IncCoin_Function_Offset);
            //add esp, 4
            CaveMemory.Write_StrBytes("83 C4 04");
            //Originalcode
            //call _FrameRateMgr_ExecServer_Function_Offset()
            CaveMemory.Write_call((UInt32)_TargetProcess_MemoryBaseAddress + _FrameRateMgr_ExecServer_Function_Offset);

            //Inject it
            CaveMemory.InjectToOffset(_Credits_InjectionStruct, "Credits");
        }

        /// <summary>
        /// Reticle seems to be misaligned if resolution is not 1920x1080
        /// </summary>
        private void SetHack_CorrectReticlePosition()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov [esp+14],00000780
            CaveMemory.Write_StrBytes("C7 44 24 14 80 07 00 00");
            //mov [esp+10],00000438
            CaveMemory.Write_StrBytes("C7 44 24 10 38 04 00 00");
            //mov eax,[ebp-04]
            CaveMemory.Write_StrBytes("8B 45 FC");
            //movd xmm0,eax
            CaveMemory.Write_StrBytes("66 0F 6E C0");

            //Inject it
            CaveMemory.InjectToOffset(_FixCrosshair_InjectionStruct, "FixCrosshair");
        }

        /// <summary>
        /// On a different resolution than 1920x1080, ennemy targets on screen are mislocated
        /// The game is measuring window width and window height but even with this, it's buggy
        /// Forcing window dimension to 1920p fix the issue (strangely ?)
        /// For that, the game calls to get center values
        /// Then the game calls for WindowSize
        /// = Forcing Center and max values
        /// </summary>
        private void SetHack_CorrectEnnemyTarget()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov [ebp-4], 44700000
            CaveMemory.Write_StrBytes("C7 45 FC 00 00 70 44");    

            //mov [ebp-8], 44070000
            CaveMemory.Write_StrBytes("C7 45 F8 00 00 07 44");

            //mov [ebp-18], 44870000
            CaveMemory.Write_StrBytes("C7 45 E8 00 00 87 44");  

            //mov [ebp-10], 44f00000
            CaveMemory.Write_StrBytes("C7 45 F0 00 00 F0 44");

            //movss xmm0,[ebp-18]
            CaveMemory.Write_StrBytes("F3 0F 10 45 E8");

            //Inject it
            CaveMemory.InjectToOffset(_FixEnnemyTarget_InjectionStruct, "FixEnnemyTarget");
        }

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_LmpStart_Address = _OutputsDatabank_Address;
            _P2_LmpStart_Address = _OutputsDatabank_Address + 0x04;
            _P1_LmpGun_Address = _OutputsDatabank_Address + 0x08;
            _P2_LmpGun_Address = _OutputsDatabank_Address + 0x0C;
            _P1_Recoil_Address = _OutputsDatabank_Address + 0x10;
            _P2_Recoil_Address = _OutputsDatabank_Address + 0x14;
            _P1_Damage_Address = _OutputsDatabank_Address + 0x18;
            _P2_Damage_Address = _OutputsDatabank_Address + 0x1C;

            SetHack_Output_StartLamp();
            SetHack_Output_GunLamp();
            SetHack_Recoil();
            SetHack_Damage();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// CCreditMgr::ExecServer() ends it's loop by calling a blanked function to send Lamp Data
        /// Intercepting the call will allow to read the values
        /// </summary>
        private void SetHack_Output_StartLamp()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov [_P1_LmpStart_Address], edi
            CaveMemory.Write_StrBytes("89 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_LmpStart_Address));
            //mov [_P2_LmpStart_Address], ebx
            CaveMemory.Write_StrBytes("89 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_LmpStart_Address));

            //Inject it
            CaveMemory.InjectToOffset(_StartLamps_InjectionStruct, "StartLamps");
        }

        /// <summary>
        /// CCreditMgr::ExecServer() ends it's loop by calling a blanked function to send Lamp Data
        /// Intercepting the call will allow to read the values
        /// </summary>
        private void SetHack_Output_GunLamp()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [esp+4]
            CaveMemory.Write_StrBytes("8B 44 24 04");
            //mov [_P1_LmpGun_Address], eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_LmpGun_Address));
            //mov eax, [esp+8]
            CaveMemory.Write_StrBytes("8B 44 24 08");
            //mov [_P2_LmpGun_Address], eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_LmpGun_Address));

            //Inject it
            CaveMemory.InjectToOffset(_GunLamps_InjectionStruct, "GunLamps");
        }

        /// <summary>
        /// When a bullet is fired, the game sends a message to the  Shell executable and increment a bullet counter
        /// Intercepting the call allow us to put our own flag
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //movsx eax,byte ptr [esi+60]
            CaveMemory.Write_StrBytes("0F BE 46 60");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _P1_Recoil_Address
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Recoil_Address));
            //mov [eax], 1
            CaveMemory.Write_StrBytes("C7 00 01 00 00 00");
            //movsx eax,byte ptr [esi+60]
            CaveMemory.Write_StrBytes("0F BE 46 60");
            //push 00
            CaveMemory.Write_StrBytes("6A 00");

            //Inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
        }

        /// <summary>
        /// When a player takes dammage, the game sends a command to the Shell executable.
        /// Intercepting the call allow us to put our own flag
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov [ebp+15],al
            CaveMemory.Write_StrBytes("88 45 15");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");
            //mov al,[edi+60]
            CaveMemory.Write_StrBytes("8A 47 60");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _P1_Damage_Address
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Damage_Address));
            //mov [eax], 1
            CaveMemory.Write_StrBytes("C7 00 01 00 00 00");
            //lea eax,[ebp+14]
            CaveMemory.Write_StrBytes("8D 45 14");

            //Inject it
            CaveMemory.InjectToOffset(_Damage_InjectionStruct, "Dammage");
        }

        /// <summary>
        /// The game is checking if Recoil is enabled for each bullet fired.
        /// Using this request call, we can generate the start of our own CustomRecoil output event
        /// </summary>
        private void SetHack_RecoilP1()
        {

        }

        private void SetHack_RecoilP2()
        {

        }

        /// <summary>
        /// Removing all UI targetting display :
        /// - Reticle (?)
        /// - Laser
        /// - Gun Model (deactivated when KID MODE is on)
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {
            if (_HideCrosshair)
            {
                // PlayerGunObj::UpdateLaserSight(PlayerGunObj *this)
                // Check if laser is on -> force JmMP to remove sight
                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _LaserSight_Patch_Offset, 0xEB);
            }

            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //push eax
            CaveMemory.Write_StrBytes("50");
            //call GmMgr_GetGameMode()
            CaveMemory.Write_call((UInt32)_TargetProcess_MemoryBaseAddress + _GmMgr_GetGameMode_Function_Offset);
            //cmp eax,06
            CaveMemory.Write_StrBytes("83 F8 06");
            //jb original code
            CaveMemory.Write_StrBytes("72 1A");
            //cmp eax,07
            CaveMemory.Write_StrBytes("83 F8 07");
            //ja original code
            CaveMemory.Write_StrBytes("77 15");
            //call _PlMgr_IsHoldMode
            CaveMemory.Write_call((UInt32)_TargetProcess_MemoryBaseAddress + _PlMgr_IsHoldMode_FunctionOffset);
            //test eax, eax
            CaveMemory.Write_StrBytes("85 C0");
            //jne originalcode
            CaveMemory.Write_StrBytes("75 0C");
            //mov [esi],C1F00000
            CaveMemory.Write_StrBytes("C7 06 00 00 F0 C1");
            //mov [edi],C1F00000
            CaveMemory.Write_StrBytes("C7 07 00 00 F0 C1");
            //Originalcode:
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //call GetResolution()
            CaveMemory.Write_call((UInt32)_TargetProcess_MemoryBaseAddress + _GetResolution_Function_Offset);
            
            //Inject it
            CaveMemory.InjectToOffset(_NoCrosshair_InjectionStruct, "No Crosshair");
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

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    WriteByte(_P1_TriggerOn_Address, 0x01);
                    WriteByte(_P1_TriggerPress_Address, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    WriteByte(_P1_TriggerOn_Address, 0x00);
                    WriteByte(_P1_TriggerPress_Address, 0x00);
                }
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes(_P2_X_Address, bufferX);
                WriteBytes(_P2_Y_Address, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    WriteByte(_P2_TriggerOn_Address, 0x01);
                    WriteByte(_P2_TriggerPress_Address, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte(_P2_TriggerPress_Address, 0x00);
            }
        }

        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (!_DisableInputHack)
            {
                if (nCode >= 0)
                {
                    KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                    if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                    {
                        if (s.scanCode == Configurator.GetInstance().DIK_Tsr_Start_P1)
                        {
                            WriteByte(_P1_StartOn_Address, 0x01);
                            WriteByte(_P1_StartPress_Address, 0x01);
                        }
                        else if (s.scanCode == Configurator.GetInstance().DIK_Tsr_Start_P2)
                        {
                            WriteByte(_P2_StartOn_Address, 0x01);
                            WriteByte(_P2_StartPress_Address, 0x01);
                        }
                        else if (s.scanCode == Configurator.GetInstance().DIK_Tsr_LeverFront)
                        {
                            WriteByte(_LeverFrontOn_Address, 0x01);
                            WriteByte(_LeverFrontPress_Address, 0x01);
                        }
                        else if (s.scanCode == Configurator.GetInstance().DIK_Tsr_LeverBack)
                        {
                            WriteByte(_LeverBackOn_Address, 0x01);
                            WriteByte(_LeverBackPress_Address, 0x01);
                        }
                        else if (s.scanCode == Configurator.GetInstance().DIK_Tsr_Credits)
                            WriteByte(_AddCredit_Address, 0x01);
                    }
                    else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                    {
                        if (s.scanCode == Configurator.GetInstance().DIK_Tsr_Start_P1)
                        {
                            WriteByte(_P1_StartOn_Address, 0x00);
                            WriteByte(_P1_StartPress_Address, 0x00);
                        }
                        else if (s.scanCode == Configurator.GetInstance().DIK_Tsr_Start_P2)
                        {
                            WriteByte(_P2_StartOn_Address, 0x00);
                            WriteByte(_P2_StartPress_Address, 0x00);
                        }
                        else if (s.scanCode == Configurator.GetInstance().DIK_Tsr_LeverFront)
                        {
                            WriteByte(_LeverFrontOn_Address, 0x00);
                            WriteByte(_LeverFrontPress_Address, 0x00);
                        }
                        else if (s.scanCode == Configurator.GetInstance().DIK_Tsr_LeverBack)
                        {
                            WriteByte(_LeverBackOn_Address, 0x00);
                            WriteByte(_LeverBackPress_Address, 0x00);
                        }
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
            //Gun motor : stays activated when trigger is pulled
            //Gun recoil : not used ??
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun, OutputId.P1_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun, OutputId.P2_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpRoom, OutputId.LmpRoom));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpLever, OutputId.LmpLever));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpDownlight, OutputId.Lmp_Downlight));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunRecoil, OutputId.P2_GunRecoil));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_P1_LmpStart_Address));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_P2_LmpStart_Address));
            SetOutputValue(OutputId.P1_LmpGun, ReadByte(_P1_LmpGun_Address));
            SetOutputValue(OutputId.P2_LmpGun, ReadByte(_P2_LmpGun_Address));

            //To-do : 
            //Lamp side, Lever and recoil ??

            //Custom Outputs
            _P1_Life = 0;
            _P2_Life = 0;
            int Credits = 0;

            //Life is read in a struct that is only created when a game has started
            UInt32 PlayerManager = ReadPtr(_pPlayerManager_Address);
            if (PlayerManager != 0)
            {
                UInt32 CPlayer1 = ReadPtr(PlayerManager + _CPlayer1_Offset);
                if (CPlayer1 != 0)
                {
                    if (ReadByte(CPlayer1 + _Cplayer_Mode_Offset) == 3 && ReadByte(CPlayer1 + _Cplayer_ID_Offset) == 0)
                    {
                        _P1_Life = BitConverter.ToInt32(ReadBytes(CPlayer1 + _Cplayer_Life_Offset, 4), 0);
                    }
                }

                UInt32 CPlayer2 = ReadPtr(PlayerManager + _CPlayer2_Offset);
                if (CPlayer2 != 0)
                {
                    if (ReadByte(CPlayer2 + _Cplayer_Mode_Offset) == 3 && ReadByte(CPlayer2 + _Cplayer_ID_Offset) == 1)
                    {
                        _P2_Life = BitConverter.ToInt32(ReadBytes(CPlayer2 + _Cplayer_Life_Offset, 4), 0);
                    }
                }
            }

            //Reading our own flags and resetting them
            if (ReadByte(_P1_Recoil_Address) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                WriteByte(_P1_Recoil_Address, 0);
            }
            if (ReadByte(_P2_Recoil_Address) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte(_P2_Recoil_Address, 0);
            }
            if (ReadByte(_P1_Damage_Address) == 1)
            {
                SetOutputValue(OutputId.P1_Damaged, 1);
                WriteByte(_P1_Damage_Address, 0);
            }
            if (ReadByte(_P2_Damage_Address) == 1)
            {
                SetOutputValue(OutputId.P2_Damaged, 1);
                WriteByte(_P2_Damage_Address, 0);
            }

            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            UInt32 CCreditsManager = ReadPtr(_pCreditsManager_Address);
            if (CCreditsManager != 0)
            {
                Credits = ReadByte(CCreditsManager + 0x38);
            }
            SetOutputValue(OutputId.Credits, Credits);
        }

        #endregion
    }
}
