using System;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MemoryX64;
using DsCore.Win32;
using System.Runtime.InteropServices;
using DsCore.RawInput;
using System.Text;
using DsCore.MameOutput;
using System.Collections.Generic;

namespace DemulShooterX64
{
    class Game_WndBlueEstate : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\windows\bestate";

        //MEMORY ADDRESSES
        private InjectionStruct _XinputGetState_InjectionStruct = new InjectionStruct(0x0099FF7D, 24);
        private InjectionStruct _MouseButtons_InjectionStruct = new InjectionStruct(0x00995EC0, 15);
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x009DA951, 16);
        private InjectionStruct _Damaged_InjectionStruct = new InjectionStruct(0x009DD642, 16);
        private UInt64 _P1_X_Patch_Offset = 0x00A25EC7;
        private UInt64 _P1_Y_Patch_Offset = 0x00A25E98;
        private UInt64 _P2_Axis_Patch_Offset = 0x00A26297;
        private UInt64 _ScePadRead_Patch_Offset = 0x0099FFAB;
        private UInt64 _ForceP2InControllerSelectScreen_Offset = 0x00A29114;
        private UInt64 _BlockP1InControllerSelectScreen_Offset = 0x00A29171;        
        private NopStruct _Nop_P2Axis_1 = new NopStruct(0x00A2624C, 6);
        private NopStruct _Nop_P2Axis_2 = new NopStruct(0x00A26284, 2);
        private NopStruct _Nop_P2Axis_3 = new NopStruct(0x00A262DC, 2);
        private NopStruct _Nop_P1BlockButton = new NopStruct(0x00A2921B, 5);

        private UInt64 _P1_Buttons_CaveAddress;
        private UInt64 _P1_X_CaveAddress;
        private UInt64 _P1_Y_CaveAddress;
        private UInt64 _P2_Buttons_CaveAddress;
        private UInt64 _P2_X_CaveAddress;
        private UInt64 _P2_Y_CaveAddress;
        private UInt64 _P1_Recoil_CaveAddress;
        private UInt64 _P2_Recoil_CaveAddress;
        private UInt64 _P1_Damaged_CaveAddress;
        private UInt64 _P2_Damaged_CaveAddress;


        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndBlueEstate(String RomName)
            : base(RomName, "BEGame")
        {
            _KnownMd5Prints.Add("Blue Estate - CODEX x64", "0eaf35ce7de0b8d41490abc86b588f66");
            _KnownMd5Prints.Add("Blue Estate - STEAM x64", "5db6275cfd6d1294e3b08aec6bc46eb5");
            _tProcess.Start();
            Logger.WriteLog("Waiting for " + _RomName + " game to hook.....");
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
                        Logger.WriteLog(_TargetProcess_MemoryBaseAddress.ToString("X16"));

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
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
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

            SetHack_P1Axis();
            SetHack_P2Axis();

            //Block button disabling for P1
            SetNops(_TargetProcess_MemoryBaseAddress, _Nop_P1BlockButton);

            SetHack_XInputGetState();
            //Jump over the calling of ScePad reading procedure if no Xinput is detected
            WriteByte((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _ScePadRead_Patch_Offset), 0xEB);

            //block P2 coordinates changes
            SetNops(_TargetProcess_MemoryBaseAddress, _Nop_P2Axis_1);
            SetNops(_TargetProcess_MemoryBaseAddress, _Nop_P2Axis_2);
            SetNops(_TargetProcess_MemoryBaseAddress, _Nop_P2Axis_3);

            //Force use P2 controller to START P2 in controller select screen
            //nop + mov al,1 + nop
            WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _ForceP2InControllerSelectScreen_Offset), new byte[] { 0x90, 0x90, 0xB0, 0x01, 0x90, 0x90, 0x90, 0x90, 0x90 });
            
            //Block all P1 inputs in controller select screen
            WriteByte((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _BlockP1InControllerSelectScreen_Offset), 0xEB);

            //Block MouseButton down/up events to remove unwanted inputs from lightgun
            //Use custom WM_APP message to send our inputs instead of mouse WM
            SetHack_MouseButtons();

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Replace Xmm0 value computed by the game, by our own [-1.0, +1.0] value
        /// </summary>
        private void SetHack_P1Axis()
        {
            //push rax
            //mov rax, [_P1_X_CaveAddress]
            //movss xmm0,[rax]
            //pop rax
            //nop 
            //nop 
            //nop 
            //nop 
            //nop 
            //nop
            WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P1_Y_Patch_Offset), new byte[] { 0x50, 0x48, 0xB8 });
            WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P1_Y_Patch_Offset + 3), BitConverter.GetBytes(_P1_X_CaveAddress));
            WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P1_Y_Patch_Offset + 11), new byte[] { 0xF3, 0x0F, 0x10, 0x00, 0x58, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 });   


            //push rax
            //mov rax, [_P1_Y_CaveAddress]
            //movss xmm0,[rax]
            //pop rax
            //jmp BEGame.exe+A25F0D
            //nop 
            //nop 
            //nop 
            WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P1_X_Patch_Offset), new byte[] { 0x50, 0x48, 0xB8 });
            WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P1_X_Patch_Offset + 3), BitConverter.GetBytes(_P1_Y_CaveAddress));
            WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P1_X_Patch_Offset + 11), new byte[] { 0xF3, 0x0F, 0x10, 0x00, 0x58, 0xEB, 0x34, 0x90, 0x90, 0x90});   
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

            //lea rdx,[rsp+60]
            CaveMemory.Write_StrBytes("48 8D 54 24 60");
            //xor rax,rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov eax,[_P2_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Buttons_CaveAddress));
            //mov [rdx+04],eax
            CaveMemory.Write_StrBytes("89 42 04");        
            //mov [rdx+0C],00000000
            CaveMemory.Write_StrBytes("C7 42 0C 00 00 00 00");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");
            //mov cl,01
            CaveMemory.Write_StrBytes("B1 01");            

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
            WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P2_Axis_Patch_Offset), new byte[] { 0x50, 0x51, 0x48, 0xC1, 0xE1, 0x02, 0x48, 0xB8 });
            WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P2_Axis_Patch_Offset + 8), BitConverter.GetBytes(_P2_X_CaveAddress));
            WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P2_Axis_Patch_Offset + 16), new byte[] { 0x48, 0x01, 0xC8, 0x59, 0xF3, 0x0F, 0x10, 0x00, 0x58, 0xEB, 0x25 });
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

            //cmp edx,00008000
            CaveMemory.Write_StrBytes("81 FA 00 80 00 00");
            //jle Check_WmButton
            CaveMemory.Write_StrBytes("7E 08");
            //sub edx,00007E00
            CaveMemory.Write_StrBytes("81 EA 00 7E 00 00");
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 25");
            //cmp edx,00000201
            CaveMemory.Write_StrBytes("81 FA 01 02 00 00");
            //je Block
            CaveMemory.Write_StrBytes("74 18");
            //cmp edx,00000202
            CaveMemory.Write_StrBytes("81 FA 02 02 00 00");
            //je Block
            CaveMemory.Write_StrBytes("74 10");
            //cmp edx,00000204
            CaveMemory.Write_StrBytes("81 FA 04 02 00 00");
            //je Block
            CaveMemory.Write_StrBytes("74 08");
            //cmp edx,00000205
            CaveMemory.Write_StrBytes("81 FA 05 02 00 00");
            //jne Exit
            CaveMemory.Write_StrBytes("75 05");
            //mov edx,00000000
            CaveMemory.Write_StrBytes("BA 00 00 00 00");

            //mov [rsp+10],rbx
            CaveMemory.Write_StrBytes("48 89 5C 24 10");
            //mov [rsp+18],rbp
            CaveMemory.Write_StrBytes("48 89 6C 24 18");
            //push rsi
            CaveMemory.Write_StrBytes("56");
            //push r12
            CaveMemory.Write_StrBytes("41 54");
            //push r13
            CaveMemory.Write_StrBytes("41 55");

            //Inject it
            CaveMemory.InjectToOffset(_MouseButtons_InjectionStruct, "Mouse Buttons");
        }

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_Recoil_CaveAddress = _OutputsDatabank_Address;
            _P2_Recoil_CaveAddress = _OutputsDatabank_Address + 4;
            _P1_Damaged_CaveAddress = _OutputsDatabank_Address + 8;
            _P2_Damaged_CaveAddress = _OutputsDatabank_Address + 12;

            SetHack_Recoil();
            SetHack_Damage();
            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// In ABEWeaponexecHasAmmo() called before a shot :
        /// Trying to get info if it's for a player (also used for ennemies) and if it's P1 or P2
        /// [RAX] should be 0x1B8 for players check
        /// Then [RCX+0x120] can be check between 0 (P1) and 0.4f(P2)
        /// Or also [RCX+4C] can be check between 1(P1) and 2(P2) (???)
        /// And Ammo value is available in [RCX+384]
        /// RCX is changing when the gun is changing
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);


            //mov rax,[rdi]
            CaveMemory.Write_StrBytes("48 8B 07");
            //mov r8d,[rsp+48]
            CaveMemory.Write_StrBytes("44 8B 44 24 48");
            //movzx edx,byte ptr [rsp+40]
            CaveMemory.Write_StrBytes("0F B6 54 24 40");
            //mov rcx,rdi
            CaveMemory.Write_StrBytes("48 8B CF");

            //cmp rax,00
            CaveMemory.Write_StrBytes("48 83 F8 00");
            //je Exit
            CaveMemory.Write_StrBytes("74 41");
            //mov rax,[rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //cmp rax,00
            CaveMemory.Write_StrBytes("48 83 F8 00");
            //je Exit1
            CaveMemory.Write_StrBytes("74 35");
            //mov rax,[rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //cmp ax,01B8
            CaveMemory.Write_StrBytes("66 3D B8 01");
            //jne Exit1
            CaveMemory.Write_StrBytes("75 2C");
            //cmp rcx,00
            CaveMemory.Write_StrBytes("48 83 F9 00");
            //je Exit1
            CaveMemory.Write_StrBytes("74 26");
            //mov rax,[rcx+00000120]
            CaveMemory.Write_StrBytes("48 8B 81 20 01 00 00");
            //cmp ax,CCCD
            CaveMemory.Write_StrBytes("66 3D CD CC");
            //je Player2
            CaveMemory.Write_StrBytes("74 0C");
            //mov rax,_P1_Recoil_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Recoil_CaveAddress));
            //jmp SetRecoil:
            CaveMemory.Write_StrBytes("EB 0A");
            //Player2
            //mov rax,_P2_Recoil_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Recoil_CaveAddress));
            //SetRecoil:
            //mov byte ptr [rax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //Exit1
            //mov rax,[rdi]
            CaveMemory.Write_StrBytes("48 8B 07");

            //Inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
        }

        /// <summary>
        /// In ABEPlayerPawnexecPlayerPadHitEffect() called before a shot :
        /// Player ID seems to be in RDI+4C (1 or 2)
        /// Life value in RDI+0x374 ??
        /// RDI has the value of RCX at the start of the function
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //cmp rdi,00
            CaveMemory.Write_StrBytes("48 83 FF 00");
            //je Exit
            CaveMemory.Write_StrBytes("74 22");
            //cmp dword ptr [rdi+4C],02
            CaveMemory.Write_StrBytes("83 7F 4C 02");
            //je Player2
            CaveMemory.Write_StrBytes("74 0C");
            //mov rax,_P1_Damage_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Damaged_CaveAddress));
            //jmp SetFlag
            CaveMemory.Write_StrBytes("EB 0A");
            //Player2:
            //mov rax,_P2_Damage_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Damaged_CaveAddress));
            //Setflag:
            //mov byte ptr [rax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //mov rax,[rdi]
            CaveMemory.Write_StrBytes("48 8B 07");
         
            //Inject it
            CaveMemory.InjectToOffset(_Damaged_InjectionStruct, "Damaged");
        }

        #endregion

        #region Inputs

        public override void SendInput(PlayerSettings PlayerData)
        {
            float fX = (float)PlayerData.RIController.Computed_X / 1000.0f;
            float fY = (float)PlayerData.RIController.Computed_Y / 1000.0f;

            if (PlayerData.ID == 1)
            {
                WriteBytes((IntPtr)((UInt64)_P1_X_CaveAddress), BitConverter.GetBytes(fX));
                WriteBytes((IntPtr)((UInt64)_P1_Y_CaveAddress), BitConverter.GetBytes(fY));

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
                WriteBytes((IntPtr)((UInt64)_P2_X_CaveAddress), BitConverter.GetBytes(fX));
                WriteBytes((IntPtr)((UInt64)_P2_Y_CaveAddress), BitConverter.GetBytes(fY));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 3), 0xFF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 3), 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {

                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {

                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 2), 0x40);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 2), 0xBF);
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
                        Apply_OR_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 2), 0x10);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_X)
                    {
                        Apply_OR_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 2), 0x40);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_C)
                    {
                        Apply_OR_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 2), 0x80);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_SPACE)
                    {
                        Apply_OR_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 3), 0xFF);
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
                        Apply_AND_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 2), 0xEF);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_X)
                    {
                        Apply_AND_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 2), 0xBF);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_C)
                    {
                        Apply_AND_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 2), 0x7F);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_SPACE)
                    {
                        Apply_AND_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 3), 0x00);
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
            //Gun motor : Is activated for every bullet fired AND when player gets
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            if (ReadByte((IntPtr)(_P1_Recoil_CaveAddress)) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                WriteByte((IntPtr)(_P1_Recoil_CaveAddress), 0x00);
            }

            if (ReadByte((IntPtr)(_P2_Recoil_CaveAddress)) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte((IntPtr)(_P2_Recoil_CaveAddress), 0x00);
            }

            if (ReadByte((IntPtr)(_P1_Damaged_CaveAddress)) == 1)
            {
                SetOutputValue(OutputId.P1_Damaged, 1);
                WriteByte((IntPtr)(_P1_Damaged_CaveAddress), 0x00);
            }

            if (ReadByte((IntPtr)(_P2_Damaged_CaveAddress)) == 1)
            {
                SetOutputValue(OutputId.P2_Damaged, 1);
                WriteByte((IntPtr)(_P2_Damaged_CaveAddress), 0x00);
            }
        }

        #endregion
    }
}
