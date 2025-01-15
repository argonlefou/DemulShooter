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
    class Game_AagamesDrakon_NoPlugin : Game
    {
        private string _GameAssemblyDll_Name = "gameassembly.dll";
        private IntPtr _GameAssemblyDll_BaseAddress = IntPtr.Zero;

        private float _P1_X_Value;
        private float _P1_Y_Value;
        private float _P2_X_Value;
        private float _P2_Y_Value;

        /*** Game Data for Memory Hack ***/
        /*** MEMORY ADDRESSES **/
        private InjectionStruct _Axis_InjectionStruct = new InjectionStruct(0x03EFCC0, 15);
        private InjectionStruct _Trigger_InjectionStruct = new InjectionStruct(0x03EFBE0, 15);
        private UInt64 _FireBreathStatus_Ptr_Offset = 0x01EB5108;        
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x00389664, 15);
        private InjectionStruct _Damage_InjectionStruct = new InjectionStruct(0x0041B8B0, 15);
        private InjectionStruct _PlayerPlaying_InjectionStruct = new InjectionStruct(0x00EB6310, 15);
        private InjectionStruct _IsLevelSelected_InjectionStruct = new InjectionStruct(0x0417004, 15);
        private InjectionStruct _NoCrosshair_InjectionStruct = new InjectionStruct(0x040E4E4, 16);

        //Custom Input Data
        private UInt64 _P1_X_CaveAddress = 0;
        private UInt64 _P1_Y_CaveAddress = 0;
        private UInt64 _P2_X_CaveAddress = 0;
        private UInt64 _P2_Y_CaveAddress = 0;
        private UInt64 _P1_TriggerState_CaveAddress = 0;
        private UInt64 _P2_TriggerState_CaveAddress = 0;        

        //Custom Output Data
        private UInt64 _P1_Recoil_CaveAddress = 0;
        private UInt64 _P2_Recoil_CaveAddress = 0;
        private UInt64 _P1_Damage_CaveAddress = 0;
        private UInt64 _P2_Damage_CaveAddress = 0;
        private UInt64 _P1_Playing_CaveAddress = 0;
        private UInt64 _P2_Playing_CaveAddress = 0;

        private UInt64 _IsLevelChoosen_Caveaddress = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_AagamesDrakon_NoPlugin(String RomName)
            : base(RomName, "Game")
        {
            _KnownMd5Prints.Add("Drakon Realm Keepers - Development Build v227996 [Original Dump]", "783a592917167b3a3a3e42f9f0717a06");
            _KnownMd5Prints.Add("Drakon Realm Keepers - Release Build v223011 [Original Dump]", "b9eaa606548f04d684876c17f48deaa3");
            _tProcess.Start();
            Logger.WriteLog("Waiting for Adrenaline Amusements game " + _RomName + " game to hook.....");
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
                                if (m.ModuleName.ToLower().Equals(_GameAssemblyDll_Name))
                                {
                                    _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                    _GameAssemblyDll_BaseAddress = m.BaseAddress;
                                    Logger.WriteLog(_GameAssemblyDll_Name + " = 0x" + _GameAssemblyDll_BaseAddress);
                                    String GameAssemblyDll_Path = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", _GameAssemblyDll_Name);
                                    CheckMd5(GameAssemblyDll_Path);
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

        public override bool GameScale(PlayerSettings PlayerData)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //Coordinates goes from [-1.0, -1.0] in bottom left corner to [1.0, 1.0] in upper right, no matter the display ratio
                    float X_Value = (2.0f * (float)PlayerData.RIController.Computed_X / (float)TotalResX) - 1.0f;
                    float Y_Value = (1.0f - (2.0f * (float)PlayerData.RIController.Computed_Y / (float)TotalResY));

                    if (X_Value < -1.0f)
                        X_Value = -1.0f;
                    if (Y_Value < -1.0f)
                        Y_Value = 1.0f;
                    if (X_Value > (float)1.0f)
                        X_Value = (float)1.0f;
                    if (Y_Value > (float)1.0f)
                        Y_Value = (float)1.0f;

                    Logger.WriteLog("Computed float values = [ " + X_Value + "x" + Y_Value + " ]");

                    if (PlayerData.ID == 1)
                    {
                        _P1_X_Value = X_Value;
                        _P1_Y_Value = Y_Value;
                    }
                    else if (PlayerData.ID == 2)
                    {
                        _P2_X_Value = X_Value;
                        _P2_Y_Value = Y_Value;
                    }

                    PlayerData.RIController.Computed_X = Convert.ToInt16(X_Value * 100);
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(Y_Value * 100);
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

        #region MemoryHack

        #region Inputs Hack
        
        protected override void Apply_InputsMemoryHack()
        {
            
            Create_InputsDataBank();
            _P1_X_CaveAddress = _InputsDatabank_Address;         
            _P1_Y_CaveAddress = _InputsDatabank_Address + 0x04;  
            _P2_X_CaveAddress = _InputsDatabank_Address + 0x08;         
            _P2_Y_CaveAddress = _InputsDatabank_Address + 0x0C;  
            _P1_TriggerState_CaveAddress = _InputsDatabank_Address + 0x10;         
            _P2_TriggerState_CaveAddress = _InputsDatabank_Address + 0x11;  
            
            SetHack_Axis();
            SetHack_Trigger();

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Replace incomming parameter from InputsManager.SendPositionEvent() function
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
            //mov rax, _P1_X_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_X_CaveAddress));
            //shl rdx,03
            CaveMemory.Write_StrBytes("48 C1 E2 03");
            //add rax,rdx
            CaveMemory.Write_StrBytes("48 01 D0");
            //mov rdx,[rax]
            CaveMemory.Write_StrBytes("48 8B 10");
            //mov [r8],rdx
            CaveMemory.Write_StrBytes("49 89 10");
            //pop rdx
            CaveMemory.Write_StrBytes("5A");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //mov [rsp+08],rbx
            CaveMemory.Write_StrBytes("48 89 5C 24 08");
            //mov [rsp+10],rbp
            CaveMemory.Write_StrBytes("48 89 6C 24 10");
            //mov [rsp+18],rsi
            CaveMemory.Write_StrBytes("48 89 74 24 18");

            //jmp back
            CaveMemory.Write_jmp((UInt64)_GameAssemblyDll_BaseAddress + _Axis_InjectionStruct.InjectionReturnOffset);

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
            Buffer.Add(0x90);
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_GameAssemblyDll_BaseAddress + _Axis_InjectionStruct.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        /// <summary>
        /// Replace incomming parameter from InputsManager.SendButtonEvent() function
        /// </summary>
        private void SetHack_Trigger()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov rax, _P1_TriggerState_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_TriggerState_CaveAddress));
            //add rax,rdx
            CaveMemory.Write_StrBytes("48 01 D0");
            //mov rax,[rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //and rax,000000FF
            CaveMemory.Write_StrBytes("48 25 FF 00 00 00");
            //mov r8,rax
            CaveMemory.Write_StrBytes("4C 8B C0");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //mov [rsp+08],rbx
            CaveMemory.Write_StrBytes("48 89 5C 24 08");
            //mov [rsp+10],rsi
            CaveMemory.Write_StrBytes("48 89 74 24 10");
            //push rdi
            CaveMemory.Write_StrBytes("57");
            //sub rsp,20
            CaveMemory.Write_StrBytes("48 83 EC 20");

            //jmp back
            CaveMemory.Write_jmp((UInt64)_GameAssemblyDll_BaseAddress + _Trigger_InjectionStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding Trigger Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

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
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_GameAssemblyDll_BaseAddress + _Trigger_InjectionStruct.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);            
        }

        #endregion

        #region Outputs Hack
        
        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_Recoil_CaveAddress = _OutputsDatabank_Address;
            _P2_Recoil_CaveAddress = _OutputsDatabank_Address + 1;
            _P1_Damage_CaveAddress = _OutputsDatabank_Address + 0x08;
            _P2_Damage_CaveAddress = _OutputsDatabank_Address + 0x09;
            _P1_Playing_CaveAddress = _OutputsDatabank_Address + 0x10;
            _P2_Playing_CaveAddress = _OutputsDatabank_Address + 0x11;

            _IsLevelChoosen_Caveaddress = _OutputsDatabank_Address + 0x20;

            SetHack_Recoil();
            SetHack_Damage();
            SetHack_IsPlayerPlaying();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }
        
        /// <summary>
        /// Intercepting calls to SBK.Skyride.Turret.Turret.FireBullet()
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //push rbx
            CaveMemory.Write_StrBytes("53");
            //mov rax, _P1_Recoil_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Recoil_CaveAddress));
            //xor rbx,rbx
            CaveMemory.Write_StrBytes("48 31 DB");
            //mov bl,[rcx+18]
            CaveMemory.Write_StrBytes("8A 59 18");
            //add rax,rbx
            CaveMemory.Write_StrBytes("48 01 D8");
            //mov byte ptr [rax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop rbx
            CaveMemory.Write_StrBytes("5B");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //lea rbp,[rsp
            CaveMemory.Write_StrBytes("48 8D AC 24 60 FF FF FF");
            //sub rsp,000001A0
            CaveMemory.Write_StrBytes("48 81 EC A0 01 00 00");

            //jmp back
            CaveMemory.Write_jmp((UInt64)_GameAssemblyDll_BaseAddress + _Recoil_InjectionStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding Recoil Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

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
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_GameAssemblyDll_BaseAddress + _Recoil_InjectionStruct.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        /// <summary>
        /// Intercepting calls to HudWindow.OnPlayerHitByEnemy()
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov [rsp+08],rbx
            CaveMemory.Write_StrBytes("48 89 5C 24 08");
            //mov rbx, _P1_Damage_CaveAddress
            CaveMemory.Write_StrBytes("48 BB");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Damage_CaveAddress));
            //add rbx,r8
            CaveMemory.Write_StrBytes("4C 01 C3");
            //mov byte ptr [rbx],01
            CaveMemory.Write_StrBytes("C6 03 01");
            //mov [rsp+10],rsi
            CaveMemory.Write_StrBytes("48 89 74 24 10");
            //push rdi
            CaveMemory.Write_StrBytes("57");
            //sub rsp,20
            CaveMemory.Write_StrBytes("48 83 EC 20");

            //jmp back
            CaveMemory.Write_jmp((UInt64)_GameAssemblyDll_BaseAddress + _Damage_InjectionStruct.InjectionReturnOffset);

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
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_GameAssemblyDll_BaseAddress + _Damage_InjectionStruct.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        /// <summary>
        /// Intercepting return value of Player.IsPlayerPlaying() function, which is called in loop all long
        /// </summary>
        private void SetHack_IsPlayerPlaying()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push rbx
            CaveMemory.Write_StrBytes("53");
            //push rdx
            CaveMemory.Write_StrBytes("52");
            //xor rbx,rbx
            CaveMemory.Write_StrBytes("48 31 DB");
            //mov bl,[rdi+28]
            CaveMemory.Write_StrBytes("8A 5F 28");
            //mov rdx, _P1_Playing_CaveAddress
            CaveMemory.Write_StrBytes("48 BA");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Playing_CaveAddress));
            //add rdx,rbx
            CaveMemory.Write_StrBytes("48 01 DA");
            //mov [rdx],al
            CaveMemory.Write_StrBytes("88 02");
            //pop rbx
            CaveMemory.Write_StrBytes("5B");
            //pop rbx
            CaveMemory.Write_StrBytes("5B");
            //mov rsi,[rsp+38]
            CaveMemory.Write_StrBytes("48 8B 74 24 38");
            //mov rbp,[rsp+30]
            CaveMemory.Write_StrBytes("48 8B 6C 24 30");
            //mov rbx,[rsp+40]
            CaveMemory.Write_StrBytes("48 8B 5C 24 40");

            //jmp back
            CaveMemory.Write_jmp((UInt64)_GameAssemblyDll_BaseAddress + _PlayerPlaying_InjectionStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding PlayerStatus Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

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
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_GameAssemblyDll_BaseAddress + _PlayerPlaying_InjectionStruct.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        #endregion

        /// <summary>
        /// Changind axis value in Target.OnTurretPosition() to [-2.0, -2.0] will put crosshair AND lasers out of sight
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {
            SetHack_GetLevelSelectedStatus();
            SetHack_RemoveCrosshairAndLasers();
        }

        /// <summary>
        /// Getting the IsLevelSelected flag of the class during ChooseLevelWindow.Update() loop
        /// </summary>
        private void SetHack_GetLevelSelectedStatus()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //push rdx
            CaveMemory.Write_StrBytes("52");
            //movzx eax,byte ptr [rbx+000000CA]
            CaveMemory.Write_StrBytes("0F B6 83 CA 00 00 00");
            //mov rdx, _IsLevelChoosen_Caveaddress
            CaveMemory.Write_StrBytes("48 BA");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_IsLevelChoosen_Caveaddress));
            //mov [rdx],rax
            CaveMemory.Write_StrBytes("48 89 02");
            //pop rdx
            CaveMemory.Write_StrBytes("5A");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //mov [rsp+58],rbp
            CaveMemory.Write_StrBytes("48 89 6C 24 58");
            //mov [rsp+60],rdi
            CaveMemory.Write_StrBytes("48 89 7C 24 60");
            //mov [rsp+30],r14
            CaveMemory.Write_StrBytes("4C 89 74 24 30");
            //jmp back
            CaveMemory.Write_jmp((UInt64)_GameAssemblyDll_BaseAddress + _IsLevelSelected_InjectionStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding IsLevelSelected Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

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
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_GameAssemblyDll_BaseAddress + _IsLevelSelected_InjectionStruct.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }


        /// <summary>
        /// Changind axis value in Target.OnTurretPosition() to [-2.0, -2.0] will put crosshair AND lasers out of sight
        /// Unfortunately, on the level selection window, this makes impossible to choose a level
        /// Sowe can filter by checking the IsLevelChoosen flag o disable the mod
        /// </summary>
        private void SetHack_RemoveCrosshairAndLasers()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov rax, [_IsLevelChoosen_Caveaddress]
            CaveMemory.Write_StrBytes("48 A1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_IsLevelChoosen_Caveaddress));
            //test rax,rax
            CaveMemory.Write_StrBytes("48 85 C0");
            //jne next
            CaveMemory.Write_StrBytes("74 0D"); 
            //mov [rdi],C0000000
            CaveMemory.Write_StrBytes("C7 07 00 00 00 C0");
            //mov [rdi+04],C0000000
            CaveMemory.Write_StrBytes("C7 47 04 00 00 00 C0");
            //pop rax
            CaveMemory.Write_StrBytes("58");            
            //movss xmm6,[rsp+50]
            CaveMemory.Write_StrBytes("F3 0F 10 74 24 50");
            //movss xmm7,[rsp+54]
            CaveMemory.Write_StrBytes("F3 0F 10 7C 24 54");
            //mulss xmm6,[rdi]
            CaveMemory.Write_StrBytes("F3 0F 59 37");

            //jmp back
            CaveMemory.Write_jmp((UInt64)_GameAssemblyDll_BaseAddress + _NoCrosshair_InjectionStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding No Crosshair Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

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
            Buffer.Add(0x90);
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_GameAssemblyDll_BaseAddress + _NoCrosshair_InjectionStruct.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);  
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
                WriteBytes((IntPtr)(_P1_X_CaveAddress), BitConverter.GetBytes(_P1_X_Value));
                WriteBytes((IntPtr)(_P1_Y_CaveAddress), BitConverter.GetBytes(_P1_Y_Value));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteByte((IntPtr)_P1_TriggerState_CaveAddress, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte((IntPtr)_P1_TriggerState_CaveAddress, 0x00);
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((IntPtr)(_P2_X_CaveAddress), BitConverter.GetBytes(_P2_X_Value));
                WriteBytes((IntPtr)(_P2_Y_CaveAddress), BitConverter.GetBytes(_P2_Y_Value));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteByte((IntPtr)_P2_TriggerState_CaveAddress, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte((IntPtr)_P2_TriggerState_CaveAddress, 0x00);
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
                        if (s.scanCode == HardwareScanCode.DIK_K)
                        {
                            Int16 x = 400;
                            Int16 y = 300;
                            Win32API.SendMessage(_GameWindowHandle, Win32Define.WM_LBUTTONDOWN, new IntPtr(1), (IntPtr)((y << 16) | (x & 0xFFFF)));
                        }
                    }
                    else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                    {
                        if (s.scanCode == HardwareScanCode.DIK_K)
                        {
                            Int16 x = 400;
                            Int16 y = 300;
                            Win32API.SendMessage(_GameWindowHandle, Win32Define.WM_LBUTTONUP, IntPtr.Zero, (IntPtr)((y << 16) | (x & 0xFFFF)));
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
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P1_CtmLmpStart, OutputId.P1_CtmLmpStart, 500));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P2_CtmLmpStart, OutputId.P2_CtmLmpStart, 500));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            byte _P1Playing = ReadByte((IntPtr)_P1_Playing_CaveAddress);
            byte _P2Playing = ReadByte((IntPtr)_P2_Playing_CaveAddress);

            if (_P1Playing == 1)
            {
                SetOutputValue(OutputId.P1_CtmLmpStart, 0);
            }
            else
            {
                SetOutputValue(OutputId.P1_CtmLmpStart, -1);
            }

            if (_P2Playing == 1)
            {
                SetOutputValue(OutputId.P2_CtmLmpStart, 0);
            }
            else
            {
                SetOutputValue(OutputId.P2_CtmLmpStart, -1);
            }


            //Custom Recoil
            if (ReadByte((IntPtr)_P1_Recoil_CaveAddress) != 0)
            {
                WriteByte((IntPtr)_P1_Recoil_CaveAddress, 0x00);
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
            }
            if (ReadByte((IntPtr)_P2_Recoil_CaveAddress) != 0)
            {
                WriteByte((IntPtr)_P2_Recoil_CaveAddress, 0x00);
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
            }

            //Custom Damage          
            if (ReadByte((IntPtr)_P1_Damage_CaveAddress) != 0)
            {
                WriteByte((IntPtr)_P1_Damage_CaveAddress, 0x00);
                SetOutputValue(OutputId.P1_Damaged, 1);
            }
            if (ReadByte((IntPtr)_P2_Damage_CaveAddress) != 0)
            {
                WriteByte((IntPtr)_P2_Damage_CaveAddress, 0x00);
                SetOutputValue(OutputId.P2_Damaged, 1);
            }

            //Rumble will be activated while FireBreathing
            UInt64 P1_FireBreathStatus_Address = ReadPtrChain((IntPtr)((UInt64)_GameAssemblyDll_BaseAddress + _FireBreathStatus_Ptr_Offset), new UInt64[]{ 0xB8 });
            SetOutputValue(OutputId.P1_GunMotor, ReadByte((IntPtr)P1_FireBreathStatus_Address));
            SetOutputValue(OutputId.P2_GunMotor, ReadByte((IntPtr)(P1_FireBreathStatus_Address + 1)));
        }

        #endregion
    }
}
