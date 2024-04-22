using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.MemoryX64;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooterX64
{
    class Game_UnisRaccoonRampage : Game
    {
        private string _UsbPluginsDllName = "usbpluginsdll.dll";
        private IntPtr _UsbPluginsDll_BaseAddress = IntPtr.Zero;

        /*** Game Data for Memory Hack ***/
        /*** MEMORY ADDRESSES **/
        private UInt64 _P1_X_CaveAddress = 0;
        private UInt64 _P1_Y_CaveAddress = 0;
        private UInt64 _SystemButtons_CaveAddress = 0;
        private UInt64 _StartButtonsAndWater_CaveAddress = 0;
        private UInt64 _P1_Coins_CaveAddress = 0;
        private UInt64 _P2_Coins_CaveAddress = 0;
        private UInt64 _P3_Coins_CaveAddress = 0;
        private UInt64 _P4_Coins_CaveAddress = 0;

        //Outputs
        private UInt64 _Outputs_BaseOffset = 0x2E740;
        public enum OutputIndex
        { 
            P1_NewGunLightState = 0x19A,        //
            P2_NewGunLightState,                //Changed at the same time than GunLightState    
            P3_NewGunLightState,                //So....Useless ?
            P4_NewGunLightState,                //            
            P1_GunLightState = 0x5AC,
            P2_GunLightState = 0x5B4,
            P3_GunLightState = 0x5BC,
            P4_GunLightState = 0x5C4,
            LackTicketLight_12P = 0x5E8,
            LackTicketLight_34P = 0x5EC,
            DisinfectionLight = 0x5F0,
            P1_InnerWater = 0x5F4,
            P2_InnerWater,
            P3_InnerWater,
            P4_InnerWater,
            P1_OuterWater,
            P2_OuterWater,
            P3_OuterWater,
            P4_OuterWater,
            P1_GunShock = 0x62C,
            P2_GunShock = 0x630,
            P3_GunShock = 0x634,
            P4_GunShock = 0x638,
            Marquee_Character1 = 0x688,
            Marquee_Character2 = 0x68C
        }

        private UInt64 _P1_InnerWater_CaveAddress = 0;
        private UInt64 _P1_OuterWater_CaveAddress = 0;
        private UInt64 _P1_Damage_CaveAddress = 0;

        private HardwareScanCode _Settings_Key = HardwareScanCode.DIK_9;
        private HardwareScanCode _P1_Start_Key = HardwareScanCode.DIK_1;
        private HardwareScanCode _P2_Start_Key = HardwareScanCode.DIK_2;
        private HardwareScanCode _P3_Start_Key = HardwareScanCode.DIK_3;
        private HardwareScanCode _P4_Start_Key = HardwareScanCode.DIK_4;
        private HardwareScanCode _P1_Credits_Key = HardwareScanCode.DIK_5;
        private HardwareScanCode _P2_Credits_Key = HardwareScanCode.DIK_6;
        private HardwareScanCode _P3_Credits_Key = HardwareScanCode.DIK_7;
        private HardwareScanCode _P4_Credits_Key = HardwareScanCode.DIK_8;
        private HardwareScanCode _MenuUp_Key = HardwareScanCode.DIK_NUMPAD8;
        private HardwareScanCode _MenuDown_Key = HardwareScanCode.DIK_NUMPAD2;
        private HardwareScanCode _MenuEnter_Key = HardwareScanCode.DIK_RETURN;

        private InjectionStruct _PlayerDamage_Injection = new InjectionStruct(0xA3C480, 15);
        private UInt64 _IsGunCalibrate_Offset = 0x9B7290;
        private UInt64 _CustomCoins_Offset = 0x2BF5D50;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_UnisRaccoonRampage(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "RSGame-Win64-Shipping", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Raccon Rampage v1.4 clean dump", "d3d3a8b3c7a8f7254d3c7641bb91bd03");
            _KnownMd5Prints.Add("Racoon Rampage v1.4 patched by Argonlefou", "8e3a7b0e5c1065c5669c3f169e5a5c5f");
            _tProcess.Start();
            Logger.WriteLog("Waiting for Unis " + _RomName + " game to hook.....");
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
                            ProcessModuleCollection c = _TargetProcess.Modules;
                            foreach (ProcessModule m in c)
                            {
                                if (m.ModuleName.ToLower().Equals(_UsbPluginsDllName))
                                {
                                    _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                    _UsbPluginsDll_BaseAddress = m.BaseAddress;
                                    Logger.WriteLog(_UsbPluginsDllName + " = 0x" + _UsbPluginsDll_BaseAddress);
                                    String UsbPluginPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", @"..\..\Plugins\" + _UsbPluginsDllName);
                                    CheckMd5(UsbPluginPath);
                                    if (!_DisableInputHack)
                                        SetHack();
                                    else
                                        Logger.WriteLog("Input Hack disabled");
                                    SetHack_Outputs();
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

        #region MemoryHack

        private void SetHack()
        {
            //+B85E4A --> NOP | 5 --> Nop mousebuttons and keyboard keys
            //SetNops(_TargetProcess_MemoryBaseAddress, "0x0E38CA7|5");

            //Force return OK at the end of IsGunCalibrate() function
            WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IsGunCalibrate_Offset), new byte[]{ 0xB0, 0x01});   //mov al, 01

            //Using unused place at the end of Data segment to store custom Credits value (so that it can live between different DemulShooter session)
            _P1_Coins_CaveAddress = (UInt64)_TargetProcess_MemoryBaseAddress + _CustomCoins_Offset;
            _P2_Coins_CaveAddress = (UInt64)_TargetProcess_MemoryBaseAddress + _CustomCoins_Offset +1;
            _P3_Coins_CaveAddress = (UInt64)_TargetProcess_MemoryBaseAddress + _CustomCoins_Offset +2;
            _P4_Coins_CaveAddress = (UInt64)_TargetProcess_MemoryBaseAddress + _CustomCoins_Offset +3;

            Create_DataBank();
            SetHack_Axis();
            SetHack_StartButtons();
            SetHack_GetCoinsNum();
            SetHack_DecCoins();

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }


        private void Create_DataBank()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            _P1_X_CaveAddress = CaveMemory.CaveAddress;         //
            _P1_Y_CaveAddress = CaveMemory.CaveAddress + 0x04;  // P2, P3 and P4 will be accessed from P1 address, with added offset                
            _SystemButtons_CaveAddress = CaveMemory.CaveAddress + 0x20;
            _StartButtonsAndWater_CaveAddress = CaveMemory.CaveAddress + 0x21;  

            //Set Water Level to "normal"
            Apply_OR_ByteMask((IntPtr)(_StartButtonsAndWater_CaveAddress), 0x04);

            Logger.WriteLog("Adding custom input data at : " + CaveMemory.CaveAddress.ToString("X16"));
        }

        /// <summary>
        /// Replace UBPGameInstance::SetMouseLocation() floatX and floatY parameter with already screen-accurate data for each player
        /// </summary>
        private void SetHack_Axis()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //push rdx
            CaveMemory.Write_StrBytes("52");
            //movabs rax, [_P1_X_Offset]
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_X_CaveAddress));
            //shl rdx, 3
            CaveMemory.Write_StrBytes("48 C1 E2 03");
            //add rax, rdx
            CaveMemory.Write_StrBytes("48 01 D0");
            //movss xmm2,[rax]
            CaveMemory.Write_StrBytes("F3 0F 10 10");
            //movss xmm3,[rax+04]
            CaveMemory.Write_StrBytes("F3 0F 10 58 04");
            //pop rdx
            CaveMemory.Write_StrBytes("5A");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //mov eax,[rcx+00000624]
            CaveMemory.Write_StrBytes("8B 81 24 06 00 00");
            //mov r14,rcx
            CaveMemory.Write_StrBytes("4C 8B F1");
            //movaps [rsp+30],xmm7
            CaveMemory.Write_StrBytes("0F 29 7C 24 30");

            //jmp back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + 0x9CFF96);

            Logger.WriteLog("Adding Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

            //Code Injection
            List<Byte> Buffer = new List<Byte>();
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer = new List<byte>();
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress));
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x9CFF88), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        //Replace GetIoData() call by reading custom buttons bytes
        private void SetHack_StartButtons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //lea rcx,[r14+000002C0]
            CaveMemory.Write_StrBytes("49 8D 8E C0 02 00 00");

            ////movabs rax, [_SystemButtons_CaveAddress]
            //CaveMemory.Write_StrBytes("48 B8");
            //CaveMemory.Write_Bytes(BitConverter.GetBytes(_SystemButtons_CaveAddress));
            ////mov rax, [rax]
            //CaveMemory.Write_StrBytes("48 8B 00");
            ////mov [rcx+02],al
            //CaveMemory.Write_StrBytes("88 41 02");

            //movabs rax, [_StartButtonsAndWater_CaveAddress]
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_StartButtonsAndWater_CaveAddress));
            //mov rax, [rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //mov [rcx+02],al
            CaveMemory.Write_StrBytes("88 41 05");

            //xor rax,rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al,01
            CaveMemory.Write_StrBytes("B0 01");
            //movzx esi,al
            CaveMemory.Write_StrBytes("0F B6 F0");
            //test rbx,rbx
            CaveMemory.Write_StrBytes("48 85 DB");
            //jmp back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + 0x9B10D7);

            Logger.WriteLog("Adding Buttons Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

            //Code Injection
            List<Byte> Buffer = new List<Byte>();
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer = new List<byte>();
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress));
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x9B10C8), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        /// <summary>
        /// Force the game to read our own coin counters
        /// </summary>
        private void SetHack_GetCoinsNum()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push rcx
            CaveMemory.Write_StrBytes("51");
            //movabs rax, [_P1_Coins_CaveAddress]
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Coins_CaveAddress));
            //add rcx,rax
            CaveMemory.Write_StrBytes("48 01 C1");
            //xor rax,rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al,[rcx]
            CaveMemory.Write_StrBytes("8A 01");
            //pop rcx
            CaveMemory.Write_StrBytes("59");

            //jmp back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + 0x9DC345);

            Logger.WriteLog("Adding GetCoinsNum Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

            //Code Injection
            List<Byte> Buffer = new List<Byte>();
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer = new List<byte>();
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress));
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x9DC2AF), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        /// <summary>
        /// Force the game to decrement our own coin counters
        /// </summary>
        private void SetHack_DecCoins()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //push rbx
            CaveMemory.Write_StrBytes("53");
            //movabs rax, [_P1_Coins_CaveAddress]
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Coins_CaveAddress));
            //add rax,rbp
            CaveMemory.Write_StrBytes("48 01 E8");
            //mov rbx,rax
            CaveMemory.Write_StrBytes("48 8B D8");
            //xor rax,rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al,[rbx]
            CaveMemory.Write_StrBytes("8A 03");
            //sub rax,rdx
            CaveMemory.Write_StrBytes("48 29 D0");
            //mov [rbx],al
            CaveMemory.Write_StrBytes("88 03");
            //pop rbx
            CaveMemory.Write_StrBytes("5B");
            //pop rax
            CaveMemory.Write_StrBytes("58");

            //jmp back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + 0x9DC224);

            Logger.WriteLog("Adding DecCoins Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

            //Code Injection
            List<Byte> Buffer = new List<Byte>();
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer = new List<byte>();
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress));
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x9DC17E), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        private void SetHack_Outputs()
        {
            Create_Outputs_DataBank();
            SetHack_GetDamage();
        }

        private void Create_Outputs_DataBank()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            _P1_InnerWater_CaveAddress = CaveMemory.CaveAddress;
            _P1_OuterWater_CaveAddress = CaveMemory.CaveAddress + 0x01;
            _P1_Damage_CaveAddress = CaveMemory.CaveAddress + 0x08;

            Logger.WriteLog("Adding custom outputs data at : " + CaveMemory.CaveAddress.ToString("X16"));
        }

        private void SetHack_GetDamage()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push rbx
            CaveMemory.Write_StrBytes("53");

            //movabs rax, [_P1_Damage_CaveAddress]
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Damage_CaveAddress));
            //cmp rbx, 4
            CaveMemory.Write_StrBytes("48 83 FB 04");
            //je All Damaged
            CaveMemory.Write_StrBytes("74 11");

            //Single damage:
            //add rax, rbx
            CaveMemory.Write_StrBytes("48 01 D8");
            //mov rbx, 1
            CaveMemory.Write_StrBytes("48 BB 01 00 00 00 00 00 00 00");
            //mov [rax], bl
            CaveMemory.Write_StrBytes("88 18");   
            //jmp exit
            CaveMemory.Write_StrBytes("EB 0C");  

            //All Damaged :
            //mov rbx, 01 01 01 01
            CaveMemory.Write_StrBytes("48 BB 01 01 01 01 00 00 00 00");
            //mov [rax], bx
            CaveMemory.Write_StrBytes("89 18"); 

            //Exit:
            //sub rsp,000000C0
            CaveMemory.Write_StrBytes("48 81 EC C0 00 00 00");
            //mov rax,[rcx]
            CaveMemory.Write_StrBytes("48 8B 01");
            //mov rbx,rcx
            CaveMemory.Write_StrBytes("48 8B D9");
            //jmp back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _PlayerDamage_Injection.InjectionReturnOffset);

            Logger.WriteLog("Adding Damage Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

            //Code Injection
            List<Byte> Buffer = new List<Byte>();
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer = new List<byte>();
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _PlayerDamage_Injection.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }
        

        #endregion

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

                    double dMaxX = 1920.0;
                    double dMaxY = 1080.0;

                    PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));
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

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>   
        public override void SendInput(PlayerSettings PlayerData)
        {
            if (!_DisableInputHack)
            {
                //Writing Axis
                WriteBytes((IntPtr)(_P1_X_CaveAddress + 8 * (UInt64)(PlayerData.ID - 1)), BitConverter.GetBytes((float)PlayerData.RIController.Computed_X));
                WriteBytes((IntPtr)(_P1_Y_CaveAddress + 8 * (UInt64)(PlayerData.ID - 1)), BitConverter.GetBytes((float)PlayerData.RIController.Computed_Y));

                //Using Trigger And Middle Button as Start Button                 
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    int bData = 0x10 << (4 - PlayerData.ID);
                    Apply_OR_ByteMask((IntPtr)(_StartButtonsAndWater_CaveAddress), (byte)bData); 
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    int bData = 0x10 << (4 - PlayerData.ID);
                    Apply_AND_ByteMask((IntPtr)(_StartButtonsAndWater_CaveAddress), (byte)~bData);  
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    int bData = 0x10 << (4 - PlayerData.ID);
                    Apply_OR_ByteMask((IntPtr)(_StartButtonsAndWater_CaveAddress), (byte)bData);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    int bData = 0x10 << (4 - PlayerData.ID);
                    Apply_AND_ByteMask((IntPtr)(_StartButtonsAndWater_CaveAddress), (byte)~bData);
                }
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
                        /*if (s.scanCode == _P1_Start_Key)                        
                            Apply_OR_ByteMask((IntPtr)(_StartButtonsAndWater_CaveAddress), 0x80);                        
                        else if (s.scanCode == _P2_Start_Key)                        
                            Apply_OR_ByteMask((IntPtr)(_StartButtonsAndWater_CaveAddress), 0x40);                        
                        if (s.scanCode == _P3_Start_Key)                        
                            Apply_OR_ByteMask((IntPtr)(_StartButtonsAndWater_CaveAddress), 0x20);                        
                        else if (s.scanCode == _P4_Start_Key)                        
                            Apply_OR_ByteMask((IntPtr)(_StartButtonsAndWater_CaveAddress), 0x10); */                       
                        if (s.scanCode == _Settings_Key)                        
                            Apply_OR_ByteMask((IntPtr)(_SystemButtons_CaveAddress), 0x04);                       
                        else if (s.scanCode == _MenuUp_Key)                        
                            Apply_OR_ByteMask((IntPtr)(_SystemButtons_CaveAddress), 0x04);                        
                        else if (s.scanCode == _MenuDown_Key)                        
                            Apply_OR_ByteMask((IntPtr)(_SystemButtons_CaveAddress), 0x02);                        
                        else if (s.scanCode == _MenuEnter_Key)                        
                            Apply_OR_ByteMask((IntPtr)(_SystemButtons_CaveAddress), 0x01);                        
                        else if (s.scanCode == _P1_Credits_Key)
                        {
                            byte c = ReadByte((IntPtr)_P1_Coins_CaveAddress);
                            if (c < 99)
                            {
                                c++;
                                WriteByte((IntPtr)_P1_Coins_CaveAddress, c);
                            }
                        }
                        else if (s.scanCode == _P2_Credits_Key)
                        {
                            byte c = ReadByte((IntPtr)_P2_Coins_CaveAddress);
                            if (c < 99)
                            {
                                c++;
                                WriteByte((IntPtr)_P2_Coins_CaveAddress, c);
                            }
                        }
                        else if (s.scanCode == _P3_Credits_Key)
                        {
                            byte c = ReadByte((IntPtr)_P3_Coins_CaveAddress);
                            if (c < 99)
                            {
                                c++;
                                WriteByte((IntPtr)_P3_Coins_CaveAddress, c);
                            }
                        }
                        else if (s.scanCode == _P4_Credits_Key)
                        {
                            byte c = ReadByte((IntPtr)_P4_Coins_CaveAddress);
                            if (c < 99)
                            {
                                c++;
                                WriteByte((IntPtr)_P4_Coins_CaveAddress, c);
                            }
                        }
                    }
                    else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                    {
                        /*if (s.scanCode == _P1_Start_Key)                        
                            Apply_AND_ByteMask((IntPtr)(_StartButtonsAndWater_CaveAddress), 0x7F);                        
                        else if (s.scanCode == _P2_Start_Key)                        
                            Apply_AND_ByteMask((IntPtr)(_StartButtonsAndWater_CaveAddress), 0xBF);                        
                        if (s.scanCode == _P3_Start_Key)                        
                            Apply_AND_ByteMask((IntPtr)(_StartButtonsAndWater_CaveAddress), 0xDF);                        
                        else if (s.scanCode == _P4_Start_Key)                        
                            Apply_AND_ByteMask((IntPtr)(_StartButtonsAndWater_CaveAddress), 0xEF); */                       
                        if (s.scanCode == _Settings_Key)                        
                            Apply_AND_ByteMask((IntPtr)(_SystemButtons_CaveAddress), 0xFB);                        
                        else if (s.scanCode == _MenuUp_Key)                        
                            Apply_AND_ByteMask((IntPtr)(_SystemButtons_CaveAddress), 0xFB);                        
                        else if (s.scanCode == _MenuDown_Key)                        
                            Apply_AND_ByteMask((IntPtr)(_SystemButtons_CaveAddress), 0xFD);                        
                        else if (s.scanCode == _MenuEnter_Key)                        
                            Apply_AND_ByteMask((IntPtr)(_SystemButtons_CaveAddress), 0xFE);                        
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

            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun, OutputId.P1_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun, OutputId.P2_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_LmpGun, OutputId.P3_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_LmpGun, OutputId.P4_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpLeft, OutputId.LmpLeft));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpRight, OutputId.LmpRight));
            _Outputs.Add(new GameOutput("Lmp_Disinfection", (OutputId)10000));
            _Outputs.Add(new GameOutput("Lmp_P1-P2_OutOfTickets", (OutputId)10001));
            _Outputs.Add(new GameOutput("Lmp_P3-P4_OutOfTickets", (OutputId)10002));
            _Outputs.Add(new GameOutput("Lmp_Marquee_1", (OutputId)10003));
            _Outputs.Add(new GameOutput("Lmp_Marquee_2", (OutputId)10004));
            _Outputs.Add(new GameOutput("P1_InnerWater", (OutputId)10005));
            _Outputs.Add(new GameOutput("P2_InnerWater", (OutputId)10006));
            _Outputs.Add(new GameOutput("P3_InnerWater", (OutputId)10007));
            _Outputs.Add(new GameOutput("P4_InnerWater", (OutputId)10008));
            _Outputs.Add(new GameOutput("P1_OuterWater", (OutputId)10009));
            _Outputs.Add(new GameOutput("P2_OuterWater", (OutputId)10010));
            _Outputs.Add(new GameOutput("P3_OuterWater", (OutputId)10011));
            _Outputs.Add(new GameOutput("P4_OuterWater", (OutputId)10012));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_GunMotor, OutputId.P3_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_GunMotor, OutputId.P4_GunMotor));           
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P3_Damaged, OutputId.P3_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P4_Damaged, OutputId.P4_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Credits, OutputId.P1_Credit));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Credits, OutputId.P2_Credit));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Credits, OutputId.P3_Credit));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Credits, OutputId.P4_Credit));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Gun LED status can have different states (BIND_SetGunLight_State())
            // 0 = OFF 
            // 1 = Continuous 
            // 2 = Flash 
            SetOutputValue(OutputId.P1_LmpGun, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P1_GunLightState)));
            SetOutputValue(OutputId.P2_LmpGun, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P2_GunLightState)));
            SetOutputValue(OutputId.P3_LmpGun, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P3_GunLightState)));
            SetOutputValue(OutputId.P4_LmpGun, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P4_GunLightState)));
            
            //Side LED status can have different states (BIND_SetLRSideLight_State())
            // 0 = OFF 
            // 1 = Continuous 
            // 2 = Flash 

            //-- TODO : Only one value as the state is changed with 2 calls : one to set the desired LED and one to set the State. Impossible to read everytime both sides status

            //Disinfection LED
            SetOutputValue((OutputId)10000, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.DisinfectionLight)));

            //Tickets LED status can have different states (BIND_SetLackTicketLight())
            // 0 = OFF 
            // 1 = Continuous 
            // 2 = Flash 
            SetOutputValue((OutputId)10001, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.LackTicketLight_12P)));
            SetOutputValue((OutputId)10002, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.LackTicketLight_34P)));

            //MarqueeLEDs (BIND_SetICO_1() and BIND_SetICO_2())
            SetOutputValue((OutputId)10003, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.Marquee_Character1)));
            SetOutputValue((OutputId)10004, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.Marquee_Character2)));

            //Water mechanics
            SetOutputValue((OutputId)10005, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P1_InnerWater)));
            SetOutputValue((OutputId)10006, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P2_InnerWater)));
            SetOutputValue((OutputId)10007, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P3_InnerWater)));
            SetOutputValue((OutputId)10008, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P4_InnerWater)));
            SetOutputValue((OutputId)10009, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P1_OuterWater)));
            SetOutputValue((OutputId)10010, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P2_OuterWater)));
            SetOutputValue((OutputId)10011, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P3_OuterWater)));
            SetOutputValue((OutputId)10012, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P4_OuterWater)));

            //Gun motor data goes from 0 to 2. Power of rumble ?
            SetOutputValue(OutputId.P1_GunMotor, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P1_GunShock)));
            SetOutputValue(OutputId.P2_GunMotor, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P2_GunShock)));
            SetOutputValue(OutputId.P3_GunMotor, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P3_GunShock)));
            SetOutputValue(OutputId.P4_GunMotor, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + _Outputs_BaseOffset + (uint)OutputIndex.P4_GunShock)));         

            ////Custom Damage will be generated by a call to one of the game Gun function
            int P1_Damage = ReadByte((IntPtr)_P1_Damage_CaveAddress);
            if (P1_Damage != 0)
            {
                WriteByte((IntPtr)_P1_Damage_CaveAddress, 0x00);
                SetOutputValue(OutputId.P1_Damaged, 1);
            }
            int P2_Damage = ReadByte((IntPtr)(_P1_Damage_CaveAddress + 1));
            if (P2_Damage != 0)
            {
                WriteByte((IntPtr)(_P1_Damage_CaveAddress + 1), 0x00);
                SetOutputValue(OutputId.P2_Damaged, 1);
            }
            int P3_Damage = ReadByte((IntPtr)(_P1_Damage_CaveAddress + 2));
            if (P3_Damage != 0)
            {
                WriteByte((IntPtr)(_P1_Damage_CaveAddress + 2), 0x00);
                SetOutputValue(OutputId.P3_Damaged, 1);
            }
            int P4_Damage = ReadByte((IntPtr)(_P1_Damage_CaveAddress + 3));
            if (P4_Damage != 0)
            {
                WriteByte((IntPtr)(_P1_Damage_CaveAddress + 3), 0x00);
                SetOutputValue(OutputId.P4_Damaged, 1);
            } 

            //Credits
            SetOutputValue(OutputId.P1_Credit, ReadByte((IntPtr)(_P1_Coins_CaveAddress)));
            SetOutputValue(OutputId.P2_Credit, ReadByte((IntPtr)(_P2_Coins_CaveAddress)));
            SetOutputValue(OutputId.P3_Credit, ReadByte((IntPtr)(_P3_Coins_CaveAddress)));
            SetOutputValue(OutputId.P4_Credit, ReadByte((IntPtr)(_P4_Coins_CaveAddress)));
        }

        #endregion
    }
}
