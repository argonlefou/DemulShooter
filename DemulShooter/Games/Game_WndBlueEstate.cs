using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using DsCore.MameOutput;

namespace DemulShooter
{
    class Game_WndBlueEstate : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\windows\bestate";

        //MEMORY ADDRESSES
        private InjectionStruct _XinputGetState_InjectionStruct = new InjectionStruct(0x00879457, 7);       //
        private InjectionStruct _MouseButtons_InjectionStruct = new InjectionStruct(0x00870130, 5);         //
        private InjectionStruct _P1_Axis_InjectionStruct = new InjectionStruct(0x008F09E8, 6);              //
        private UInt32 _P2_Axis_Patch_Offset = 0x008F0E08;                                                  //
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x008AEA80, 6);               //
        private InjectionStruct _Damaged_InjectionStruct = new InjectionStruct(0x008B105C, 8);             //
        private InjectionStruct _PlayerInfo_InjectionStruct = new InjectionStruct(0x008B1075, 7);

        private UInt32 _ForceP2InControllerSelectScreen_Offset = 0x00A29114;
        private UInt32 _BlockP1InControllerSelectScreen_Offset = 0x00A29171;
        private NopStruct _Nop_P2Axis_1 = new NopStruct(0x00A2624C, 6);
        private NopStruct _Nop_P2Axis_2 = new NopStruct(0x00A26284, 2);
        private NopStruct _Nop_P2Axis_3 = new NopStruct(0x008F0E52, 2);                                     //
        private NopStruct _Nop_P1BlockButton = new NopStruct(0x00A2921B, 5);

        private UInt32 _P1_Buttons_CaveAddress;
        private UInt32 _P1_X_CaveAddress;
        private UInt32 _P1_Y_CaveAddress;
        private UInt32 _P2_Buttons_CaveAddress;
        private UInt32 _P2_X_CaveAddress;
        private UInt32 _P2_Y_CaveAddress;
        private UInt32 _P1_Recoil_CaveAddress;
        private UInt32 _P2_Recoil_CaveAddress;
        private UInt32 _P1_Damaged_CaveAddress;
        private UInt32 _P2_Damaged_CaveAddress;
        private UInt32 _P1_Life_CaveAddress;
        private UInt32 _P2_Life_CaveAddress;
        private UInt32 _P1_Ammo_CaveAddress;
        private UInt32 _P2_Ammo_CaveAddress;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndBlueEstate(String RomName)
            : base(RomName, "BEGame")
        {
            _KnownMd5Prints.Add("Blue Estate CODEX x86", "188605d4083377e4ee3552b4c89f52fb");

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
                        Logger.WriteLog(_TargetProcess_MemoryBaseAddress.ToString("X8"));

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            /* Wait until Splash Screen is closed and real windows displayed */
                            /* Game Windows classname = "LaunchUnrealUWindowsClient" */
                            StringBuilder ClassName = new StringBuilder(256);
                            int nRet = Win32API.GetClassName(_TargetProcess.MainWindowHandle, ClassName, ClassName.Capacity);
                            if (nRet != 0 && ClassName.ToString() != "SplashScreenClass")
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                                Apply_MemoryHacks();
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
                            }
                        }
                    }
                }
                catch (Exception Ex)
                {
                    Logger.WriteLog("Error trying to hook " + _Target_Process_Name + ".exe");
                    Logger.WriteLog(Ex.Message.ToString());
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

                    //X => [-1 ; 1] float
                    //Y => [-1 ; 1] float

                    float X_Value = (2.0f * PlayerData.RIController.Computed_X / TotalResX) - 1.0f;
                    float Y_Value = (2.0f * PlayerData.RIController.Computed_Y / TotalResY) - 1.0f;

                    if (X_Value < -1.0f)
                        X_Value = -1.0f;
                    if (Y_Value < -1.0f)
                        Y_Value = -1.0f;
                    if (X_Value > 1.0f)
                        X_Value = 1.0f;
                    if (Y_Value > 1.0f)
                        Y_Value = 1.0f;

                    PlayerData.RIController.Computed_X = (int)(X_Value * 1000.0f);
                    PlayerData.RIController.Computed_Y = (int)(Y_Value * 1000.0f);

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

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _P1_Buttons_CaveAddress = _InputsDatabank_Address;
            _P1_X_CaveAddress = _InputsDatabank_Address + 0x10;
            _P1_Y_CaveAddress = _InputsDatabank_Address + 0x14;
            _P2_Buttons_CaveAddress = _InputsDatabank_Address + 0x20;
            _P2_X_CaveAddress = _InputsDatabank_Address + 0x30;
            _P2_Y_CaveAddress = _InputsDatabank_Address + 0x34;

            //Block MouseButton down/up events to remove unwanted inputs from lightgun
            //Use custom WM_APP message to send our inputs instead of mouse WM
            SetHack_MouseButtons();

            //Inject custom values and force Player2 (Gamepad) XInput read
            SetHack_XInputGetState();

            SetHack_P1Axis();
            SetHack_P2Axis();

            /*

            //Block button disabling for P1
            SetNops(_TargetProcess_MemoryBaseAddress, _Nop_P1BlockButton);

            

            //block P2 coordinates changes
            SetNops(_TargetProcess_MemoryBaseAddress, _Nop_P2Axis_1);
            SetNops(_TargetProcess_MemoryBaseAddress, _Nop_P2Axis_2);
            SetNops(_TargetProcess_MemoryBaseAddress, _Nop_P2Axis_3);

            //Force use P2 controller to START P2 in controller select screen
            //nop + mov al,1 + nop
            WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _ForceP2InControllerSelectScreen_Offset), new byte[] { 0x90, 0x90, 0xB0, 0x01, 0x90, 0x90, 0x90, 0x90, 0x90 });

            //Block all P1 inputs in controller select screen
            WriteByte((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _BlockP1InControllerSelectScreen_Offset), 0xEB);

            */

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Replace Float values computed by the game, by our own [-1.0, +1.0] value
        /// </summary>
        private void SetHack_P1Axis()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //fstp dword ptr [esi+000002B4]
            CaveMemory.Write_StrBytes("D9 9E B4 02 00 00");
            //fld dword ptr [_P1_X_CaveAddress]
            CaveMemory.Write_StrBytes("D9 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_X_CaveAddress));
            //fstp dword ptr [esi+000002B0]
            CaveMemory.Write_StrBytes("D9 9E B0 02 00 00");
            //fld dword ptr [_P1_Y_CaveAddress]
            CaveMemory.Write_StrBytes("D9 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Y_CaveAddress));
            //fstp dword ptr [esi+000002B4]
            CaveMemory.Write_StrBytes("D9 9E B4 02 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_P1_Axis_InjectionStruct, "P1 Axis");
        }

        /// <summary>
        /// Load custom values intoi XInput buffer result and replace initial call by success return
        /// [rsp+60] has XINPUT STRUCTURE
        /// [rdx+04] has BUTTONS AND TRIGGERS
        /// [rdx+0C] has RIGHT PAD
        /// </summary>
        private void SetHack_XInputGetState()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov edx,eax
            CaveMemory.Write_StrBytes("8B D0");
            //mov eax,[_P2_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Buttons_CaveAddress));
            //mov [edx+04],eax
            CaveMemory.Write_StrBytes("89 42 04");
            //mov [edx+0C],00000000
            CaveMemory.Write_StrBytes("C7 42 0C 00 00 00 00");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");

            //Inject it
            CaveMemory.InjectToOffset(_XinputGetState_InjectionStruct, "XInput");
        }

        /// <summary>
        /// Loading X or Y CaveAddress instead of original value, based on ECX
        /// </summary>
        private void SetHack_P2Axis()
        {
            //push rcx
            //shl rcx, 02
            //mov rax, _P2_X_CaveAddress
            //add rax, rcx
            //pop rcx
            //movss xmm0, [rax]

            //push ecx
            //shl ecx,02
            //add ecx,12345678
            //movss xmm0,[ecx]
            //pop ecx
            //jmp BEGame.exe+8F0E49
            //nop 
            //nop 
            //nop 
            //nop 
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Axis_Patch_Offset, new byte[] { 0x51, 0xC1, 0xE1, 0x02, 0x81, 0xC1 });
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Axis_Patch_Offset + 6, BitConverter.GetBytes(_P2_X_CaveAddress));
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Axis_Patch_Offset + 10, new byte[] { 0xF3, 0x0F, 0x10, 0x01, 0x59, 0xEB, 0x30, 0x90, 0x90, 0x90, 0x90 });
        }

        /// <summary>
        /// At the start of the wndProc procedure :
        /// Setting MSg to 0 if it's any of WM_LBUTTONDOWN/UP WM_RBUTTONDOWN/UP
        /// Using WM_APP+Index Msg for custom Inputs to replace original mouse Msg
        /// That way, any mouse lightgun would not be controlling buttons anymore
        /// </summary>
        private void SetHack_MouseButtons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx,[esp+0C]
            CaveMemory.Write_StrBytes("8B 5C 24 0C");

            //cmp ebx,00008000
            CaveMemory.Write_StrBytes("81 FB 00 80 00 00");
            //jle Check_WmButton
            CaveMemory.Write_StrBytes("7E 08");
            //sub ebx,00007E00
            CaveMemory.Write_StrBytes("81 EB 00 7E 00 00");
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 25");
            //cmp ebx,00000201
            CaveMemory.Write_StrBytes("81 FB 01 02 00 00");
            //je Block
            CaveMemory.Write_StrBytes("74 18");
            //cmp ebx,00000202
            CaveMemory.Write_StrBytes("81 FB 02 02 00 00");
            //je Block
            CaveMemory.Write_StrBytes("74 10");
            //cmp ebx,00000204
            CaveMemory.Write_StrBytes("81 FB 04 02 00 00");
            //je Block
            CaveMemory.Write_StrBytes("74 08");
            //cmp ebx,00000205
            CaveMemory.Write_StrBytes("81 FB 05 02 00 00");
            //jne Exit
            CaveMemory.Write_StrBytes("75 05");
            //mov ebx,00000000
            CaveMemory.Write_StrBytes("BB 00 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_MouseButtons_InjectionStruct, "Mouse Buttons");
        }

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_Recoil_CaveAddress = _OutputsDatabank_Address;
            _P2_Recoil_CaveAddress = _OutputsDatabank_Address + 0x04;
            _P1_Damaged_CaveAddress = _OutputsDatabank_Address + 0x08;
            _P2_Damaged_CaveAddress = _OutputsDatabank_Address + 0x0C;
            _P1_Life_CaveAddress = _OutputsDatabank_Address + 0x10;
            _P1_Ammo_CaveAddress = _OutputsDatabank_Address + 0x14;
            _P2_Life_CaveAddress = _OutputsDatabank_Address + 0x18;
            _P2_Ammo_CaveAddress = _OutputsDatabank_Address + 0x1C;

            SetHack_Recoil();
            SetHack_Damage();
            SetHack_PlayerInfo();
            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// In ABEWeaponexecHasAmmo() called before a shot :
        /// EDI looks like a "Weapon Struct", and Ammo value is available in [EDI+2C0]
        /// EDI is changing when the gun is changing
        /// 
        /// Trying to get info if it's for a player (also used for ennemies) and if it's P1 or P2
        /// [EDI+0x9C] is pointing to what look like to be a "player" owner struct
        /// [EDI+0x9C] + 0x030 => Owner ID (1 or 2)
        /// [EDI+0x9C] + 0x2DC => Owner Life
        /// To differentiate Onwer type (player / Ennemy) we can check either :
        /// - [EDI+0x9C] + 0x50 => 0x16 / 0x2F
        /// - [EDI+0x9C] + 0x94 => 00 01 03 05 / 01 01 03 07
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov ecx,edi
            CaveMemory.Write_StrBytes("8B CF");
            //cmp ecx,00
            CaveMemory.Write_StrBytes("83 F9 00");
            //je Exit
            CaveMemory.Write_StrBytes("74 2D");
            //mov ecx,[ecx+0000009C]
            CaveMemory.Write_StrBytes("8B 89 9C 00 00 00");
            //cmp ecx,00
            CaveMemory.Write_StrBytes("83 F9 00");
            //je Exit
            CaveMemory.Write_StrBytes("74 22");
            //cmp byte ptr [ecx+50],2F
            CaveMemory.Write_StrBytes("80 79 50 2F");
            //jne Exit
            CaveMemory.Write_StrBytes("75 1C");
            //cmp byte ptr [ecx+30],01
            CaveMemory.Write_StrBytes("80 79 30 01");
            //jne Player2
            CaveMemory.Write_StrBytes("75 09");
            //mov [_P1_Recoil_CaveAddress], 01
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Recoil_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 0D");
            //Player2:
            //cmp dword ptr [ecx+30],02
            CaveMemory.Write_StrBytes("83 79 30 02");
            //jne Exit
            CaveMemory.Write_StrBytes("75 07");
            //mov [_P2_Recoil_CaveAddress],00000001
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Recoil_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //Exit:            
            //mov ecx,[esp+10]
            CaveMemory.Write_StrBytes("8B 4C 24 10");
            //mov eax,[edi]
            CaveMemory.Write_StrBytes("8B 07");

            //Inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
        }

        /// /// <summary>
        /// In ABEPlayerPawnexecPlayerPadHitEffect() called before a shot :
        /// EDI has the value of ECX at the start of the function, same "Player" owner structure as previously used for recoil
        /// [EDI+0x030] => Player ID (1 or 2)
        /// [EDI+0x2DC] => Player Life        
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //cmp edi,00
            CaveMemory.Write_StrBytes("83 FF 00");
            //je Exit
            CaveMemory.Write_StrBytes("74 15");
            //cmp dword ptr [edi+30],02
            CaveMemory.Write_StrBytes("83 7F 30 02");
            //je Player2
            CaveMemory.Write_StrBytes("74 07");
            //mov eax,_P1_Damaged_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Damaged_CaveAddress));
            //jmp SetFlag
            CaveMemory.Write_StrBytes("EB 05");
            //Player2:
            //mov eax,_P2_Damaged_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Damaged_CaveAddress));
            //SetFlag:
            //mov byte ptr [eax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //Exit:
            //mov eax,[edi]
            CaveMemory.Write_StrBytes("8B 07");
            //mov edx,[eax+000004FC]
            CaveMemory.Write_StrBytes("8B 90 FC 04 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Damaged_InjectionStruct, "Damaged");
        }

        /// <summary>
        /// ABEPlayerPawnexecPlayerPadUpdate() is called in a loop
        /// In it, we can acces the "Player" owner struct in ECX and we can access :
        /// [ECX+0x030] => Player ID (1 or 2)
        /// [ECX+0x050] => Player playing ? (0x2F) - Not playing is 0x16
        /// [ECX+0x2DC] => Player Life
        /// [ECX+0x3C4] => Player Weapon Struct pointer
        /// +[ECX+0x3C4]+0x2C0 => Weapon Ammo
        /// </summary>
        private void SetHack_PlayerInfo()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //cmp byte ptr [ecx+30],01
            CaveMemory.Write_StrBytes("80 79 30 01");
            //jne Player2
            CaveMemory.Write_StrBytes("75 52");
            //cmp byte ptr [ecx+50],2F
            CaveMemory.Write_StrBytes("80 79 50 2F");
            //je Player1Playing
            CaveMemory.Write_StrBytes("74 19");
            //mov [_P1_Life_CaveAddress],00000000
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Life_CaveAddress));
            CaveMemory.Write_StrBytes("00 00 00 00");
            //mov [_P1_Ammo_CaveAddress],00000000
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Ammo_CaveAddress));
            CaveMemory.Write_StrBytes("00 00 00 00");
            //jmp Exit
            CaveMemory.Write_StrBytes("E9 86 00 00 00");
            //Player1Playing:
            //mov eax,[ecx+000002DC]
            CaveMemory.Write_StrBytes("8B 81 DC 02 00 00");
            //mov [_P1_Life_CaveAddress],eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Life_CaveAddress));
            //cmp dword ptr [ecx+000003C4],00
            CaveMemory.Write_StrBytes("83 B9 C4 03 00 00 00");
            //jne HasWeapon
            CaveMemory.Write_StrBytes("75 0C");
            //mov [_P1_Ammo_CaveAddress],00000000
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Ammo_CaveAddress));
            CaveMemory.Write_StrBytes("00 00 00 00");
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 66");
            //HasWeapon:
            //mov eax,[ecx+000003C4]
            CaveMemory.Write_StrBytes("8B 81 C4 03 00 00");
            //mov eax,[eax+000002C0]
            CaveMemory.Write_StrBytes("8B 80 C0 02 00 00");
            //mov [_P1_Ammo_CaveAddress],eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Ammo_CaveAddress));
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 53");

            //Player2:
            //cmp byte ptr [ecx+30],02
            CaveMemory.Write_StrBytes("80 79 30 02");
            //jne Exit
            CaveMemory.Write_StrBytes("75 4D");
            //cmp byte ptr [ecx+50],2F
            CaveMemory.Write_StrBytes("80 79 50 2F");
            //je Player2Playing
            CaveMemory.Write_StrBytes("74 16");
            //mov [_P2_Life_CaveAddress],00000000
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Life_CaveAddress));
            CaveMemory.Write_StrBytes("00 00 00 00");
            //mov [_P2_Ammo_CaveAddress],00000000
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Ammo_CaveAddress));
            CaveMemory.Write_StrBytes("00 00 00 00");
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 31");
            //Player2Playing:
            //mov eax,[ecx+000002DC]
            CaveMemory.Write_StrBytes("8B 81 DC 02 00 00");
            //mov [_P2_Life_CaveAddress],eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Life_CaveAddress));
            //cmp dword ptr [ecx+000003C4],00
            CaveMemory.Write_StrBytes("83 B9 C4 03 00 00 00");
            //jne HasWeapon
            CaveMemory.Write_StrBytes("75 0C");
            //mov [_P2_Ammo_CaveAddress],00000000
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Ammo_CaveAddress));
            CaveMemory.Write_StrBytes("00 00 00 00");
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 11");
            //HasWeapon:
            //mov eax,[ecx+000003C4]
            CaveMemory.Write_StrBytes("8B 81 C4 03 00 00");
            //mov eax,[eax+000002C0]
            CaveMemory.Write_StrBytes("8B 80 C0 02 00 00");
            //mov [_P2_Ammo_CaveAddress],eax
            CaveMemory.Write_StrBytes("A3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Ammo_CaveAddress));

            //Exit:
            //mov esi,[esp+0C]
            CaveMemory.Write_StrBytes("8B 74 24 0C");
            //mov eax,[esi+18]
            CaveMemory.Write_StrBytes("8B 46 18");


            //Inject it
            CaveMemory.InjectToOffset(_PlayerInfo_InjectionStruct, "PlayerInfo");
        }

        #endregion

        #region Inputs

        public override void SendInput(PlayerSettings PlayerData)
        {
            float fX = (float)PlayerData.RIController.Computed_X / 1000.0f;
            float fY = (float)PlayerData.RIController.Computed_Y / 1000.0f;

            if (PlayerData.ID == 1)
            {
                WriteBytes(_P1_X_CaveAddress, BitConverter.GetBytes(fX));
                WriteBytes(_P1_Y_CaveAddress, BitConverter.GetBytes(fY));

                //Sending WM_APP+Index to simulate mouse click
                //As the message will be translated to WM_xBUTTONDOWN/UP, Wparam must be set to according pressed button too
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Win32API.PostMessage(_GameWindowHandle, Win32Define.WM_APP | 0x001, new IntPtr(1), new IntPtr(0));
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Win32API.PostMessage(_GameWindowHandle, Win32Define.WM_APP | 0x002, new IntPtr(0), new IntPtr(0));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {

                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {

                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Win32API.PostMessage(_GameWindowHandle, Win32Define.WM_APP | 0x004, new IntPtr(2), new IntPtr(0));
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Win32API.PostMessage(_GameWindowHandle, Win32Define.WM_APP | 0x005, new IntPtr(0), new IntPtr(0));
            }

            if (PlayerData.ID == 2)
            {
                WriteBytes(_P2_X_CaveAddress, BitConverter.GetBytes(fX));
                WriteBytes(_P2_Y_CaveAddress, BitConverter.GetBytes(fY));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress + 3, 0xFF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress + 3, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {

                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {

                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress + 2, 0x40);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress + 2, 0xBF);
            }
        }

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    if (s.scanCode == HardwareScanCode.DIK_Z)
                    {
                        Apply_OR_ByteMask(_P2_Buttons_CaveAddress + 2, 0x10);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_X)
                    {
                        Apply_OR_ByteMask(_P2_Buttons_CaveAddress + 2, 0x40);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_C)
                    {
                        Apply_OR_ByteMask(_P2_Buttons_CaveAddress + 2, 0x80);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_SPACE)
                    {
                        Apply_OR_ByteMask(_P2_Buttons_CaveAddress + 3, 0xFF);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_0)
                    {
                        Win32API.PostMessage(_GameWindowHandle, 0x8001, new IntPtr(1), new IntPtr(0));
                    }
                    if (s.scanCode == HardwareScanCode.DIK_9)
                    {
                        Win32API.PostMessage(_GameWindowHandle, 0x201, new IntPtr(1), new IntPtr(0));
                    }

                }
                else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                {
                    if (s.scanCode == HardwareScanCode.DIK_Z)
                    {
                        Apply_AND_ByteMask(_P2_Buttons_CaveAddress + 2, 0xEF);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_X)
                    {
                        Apply_AND_ByteMask(_P2_Buttons_CaveAddress + 2, 0xBF);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_C)
                    {
                        Apply_AND_ByteMask(_P2_Buttons_CaveAddress + 2, 0x7F);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_SPACE)
                    {
                        Apply_AND_ByteMask(_P2_Buttons_CaveAddress + 3, 0x00);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_0)
                    {
                        Win32API.PostMessage(_GameWindowHandle, 0x8002, new IntPtr(0), new IntPtr(0));
                    }
                    if (s.scanCode == HardwareScanCode.DIK_9)
                    {
                        Win32API.PostMessage(_GameWindowHandle, 0x202, new IntPtr(0), new IntPtr(0));
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
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            if (ReadByte(_P1_Recoil_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                WriteByte(_P1_Recoil_CaveAddress, 0x00);
            }

            if (ReadByte(_P2_Recoil_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte(_P2_Recoil_CaveAddress, 0x00);
            }

            if (ReadByte(_P1_Damaged_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P1_Damaged, 1);
                WriteByte(_P1_Damaged_CaveAddress, 0x00);
            }

            if (ReadByte(_P2_Damaged_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P2_Damaged, 1);
                WriteByte(_P2_Damaged_CaveAddress, 0x00);
            }

            SetOutputValue(OutputId.P1_Life, BitConverter.ToInt32(ReadBytes(_P1_Life_CaveAddress, 4), 0));
            SetOutputValue(OutputId.P2_Life, BitConverter.ToInt32(ReadBytes(_P2_Life_CaveAddress, 4), 0));
            SetOutputValue(OutputId.P1_Ammo, BitConverter.ToInt32(ReadBytes(_P1_Ammo_CaveAddress, 4), 0));
            SetOutputValue(OutputId.P2_Ammo, BitConverter.ToInt32(ReadBytes(_P2_Ammo_CaveAddress, 4), 0));
        }

        #endregion
    }

}
