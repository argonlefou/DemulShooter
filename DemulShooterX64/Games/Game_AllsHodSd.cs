using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.MemoryX64;
using DsCore.Win32;

namespace DemulShooterX64
{
    public class Game_AllsHodSd : Game
    {
        //Memory values
        private InjectionStruct _Jvs_P1X_Injection = new InjectionStruct(0x001E28D2, 15);
        private InjectionStruct _Jvs_P1Y_Injection = new InjectionStruct(0x001E28F5, 15);
        private InjectionStruct _Jvs_P2X_Injection = new InjectionStruct(0x001E291C, 15);
        private InjectionStruct _Jvs_P2Y_Injection = new InjectionStruct(0x001E293F, 15);
        private InjectionStruct _MenuFlag_Injection = new InjectionStruct(0x00DA3050, 17);
        private InjectionStruct _AxisCorrection_Injection = new InjectionStruct(0x001DED89, 17);
        //private UInt32 _RemoveMenuCorrection_Offset = 0x00DA3041;
        private UInt64 _PlayerStatusPointer_Offset = 0x03087750;     

        //Custom Values
        private UInt64 _Databank_WindowWidth_Address = 0;
        private UInt64 _Databank_WindowHeight_Address = 0;
        private UInt64 _Databank_JVS_P1X_Address = 0;
        private UInt64 _Databank_JVS_P1Y_Address = 0;
        private UInt64 _Databank_JVS_P2X_Address = 0;
        private UInt64 _Databank_JVS_P2Y_Address = 0;
        private UInt64 _Databank_InMenu_Address = 0;



        /// <summary>
        /// Constructor
        /// </summary>
        public Game_AllsHodSd(String RomName, bool DisableInputHack, bool Verbose) : base(RomName, "Hodzero-Win64-Shipping", DisableInputHack, Verbose)
        {            
            _KnownMd5Prints.Add("Hodzero-Win64-Shipping.exe - Original Dump", "cde48c217d04caa64ee24a72f73dcce4");
            //Add amdaemon check ? (difference between original/Jconfig)
            //amdaemon.exe - original : 2ccb852ba8f98d6adf42ed62d1d1b759
            //amdaemon.exe - Jconfig  : 2cd0d9d4771f84724b6ea9a008f53ea4
            _tProcess.Start();
            Logger.WriteLog("Waiting for SEGA ALLS " + _RomName + " game to hook.....");
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
                            // The game may start with other Windows than the main one (because of AMDaemon app console) so we need to filter
                            // the displayed window according to the Title, if DemulShooter is started before the game,  to hook the correct one
                            if (FindGameWindow_Equals("Hodzero ") || FindGameWindow_Equals("TeknoParrot - House of the Dead: Scarlet Dawn"))
                            {
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
                                CheckExeMd5();
                                /*if (!_DisableInputHack)
                                    SetHack();*/
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
                            }
                            else
                            {
                                Logger.WriteLog("Game Window not found");
                                return;
                            }                           
                        }
                        else
                        {
                            Logger.WriteLog("ROM not Loaded...");
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

                    //X => [0x000 - 0x3FF]
                    //Y => [0x000 - 0x3FF]
                    double dMaxX = 1024.0;
                    double dMaxY = 1024.0;
                    PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(dMaxY - Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));
                    if (PlayerData.RIController.Computed_X < 0)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 0)
                        PlayerData.RIController.Computed_Y = 0;
                    if (PlayerData.RIController.Computed_X > (int)dMaxX)
                        PlayerData.RIController.Computed_X = (int)dMaxX;
                    if (PlayerData.RIController.Computed_Y > (int)dMaxY)
                        PlayerData.RIController.Computed_Y = (int)dMaxY;     
              
                    //Need to store data for InGame dynamic correction
                    WriteBytes((IntPtr)_Databank_WindowWidth_Address, BitConverter.GetBytes((UInt32)TotalResX));
                    WriteBytes((IntPtr)_Databank_WindowHeight_Address, BitConverter.GetBytes((UInt32)TotalResY));

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

        private void SetHack()
        {
            CreateDataBank();

            //First part is to set our own JVS base values when the game is reading them in the JVS data array
            SetHack_JVS_P1_X();
            SetHack_JVS_P1_Y();
            SetHack_JVS_P2_X();
            SetHack_JVS_P2_Y();

            //Then to correct offsets between the cursors on menu screens and real data, we need to change the cursor position so that it's correct
            //whatever the window size is (otherwise, only aligned in 1920x1080)
            //Based on [0-1024] JVS data, the game calculates in-menu coordinates based on 1920x1080 range, whereas in-game it is based on window size
            //This also removes the 1.5x multiplier offset between cursor and aim in menu
            SetHack_AxisCorrection();

            //LAstly, forcing jump to not use the in-game part screwing the menus aim with screen multiplier according to resolution Height
            //WriteByte((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _RemoveMenuCorrection_Offset), 0xEB);
            
            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Space for our own axis data
        /// </summary>
        private void CreateDataBank()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            _Databank_WindowWidth_Address = CaveMemory.CaveAddress;
            _Databank_WindowHeight_Address = CaveMemory.CaveAddress + 0x08;
            _Databank_InMenu_Address = CaveMemory.CaveAddress + 0x10;            

            Logger.WriteLog("Custom data will be stored at : 0x" + CaveMemory.CaveAddress.ToString("X16"));
        }

        /// <summary>
        /// At this point the game uses that code only when in-menu
        /// We can :
        /// 1 - Remove the 1.5x multiplier applied to the cursor hand in menu
        /// 2 - Set a flag to tell we are in menu for later use (settle the offset)
        /// </summary>
        private void SetHack_InMenuFlag()
        {            
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //xorps xmm0,xmm0
            CaveMemory.Write_StrBytes("0F 57 C0");
            //movaps xmm1,xmm2
            CaveMemory.Write_StrBytes("0F 28 CA");
            //mulss xmm2,[rsp+24]
            CaveMemory.Write_StrBytes("F3 0F 59 54 24 24");
            //push rax
            CaveMemory.Write_StrBytes("50");
            //movabs rax, [_Databank_InMenu_Address]
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Databank_InMenu_Address));
            //mov byte ptr[rax], 1
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop rax
            CaveMemory.Write_StrBytes("58");

            //Jump back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _MenuFlag_Injection.InjectionReturnOffset);

            Logger.WriteLog("Adding InMenuFlag Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

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
            Buffer.Add(0x90);
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _MenuFlag_Injection.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        /// <summary>
        /// At this point the game uses the Raw JVS values (0-1024) to compute real position of the gun in game
        /// We can :
        /// 1 - Remove the gun calibration values that are screwing our aim
        /// 2 - Look at the InMenu flag to change the calculation accordingly : in-menu range is 1920x1080 whereas in-game is WindowWidth x WindowHeight
        /// </summary>
        private void SetHack_AxisCorrection()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push rax
            CaveMemory.Write_StrBytes("50");

            //mov rax, _PlayerstatusPointer_Offset
            CaveMemory.Write_StrBytes("48 B8");
            UInt64 uAddress = (UInt64)_TargetProcess_MemoryBaseAddress + _PlayerStatusPointer_Offset;
            CaveMemory.Write_Bytes(BitConverter.GetBytes(uAddress));
            //mov rax, [rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //add rax, 0x231
            CaveMemory.Write_StrBytes("48 05 31 02 00 00"); 
            //cmp dword ptr [rax],01
            CaveMemory.Write_StrBytes("83 38 01");
            //je InGame
            CaveMemory.Write_StrBytes("0F 84 26 00 00 00");
            //add rax, 1
            CaveMemory.Write_StrBytes("48 83 C0 01");
            //cmp dword ptr [rax],01
            CaveMemory.Write_StrBytes("83 38 01");
            //je InGame
            CaveMemory.Write_StrBytes("0F 84 19 00 00 00");

            //InMenu:
            //mov [rdi+00000D4C],00000780   //1920
            CaveMemory.Write_StrBytes("C7 87 4C 0D 00 00 80 07 00 00");
            //mov [rdi+00000D50],00000438   //1080
            CaveMemory.Write_StrBytes("C7 87 50 0D 00 00 38 04 00 00");
            //jmp Next
            CaveMemory.Write_StrBytes("E9 26 00 00 00");
            
            //InGame:
            //mov rax, _Databank_windowWidth
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Databank_WindowWidth_Address));
            //mov rax,[rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //mov [rdi+00000D4C],rax
            CaveMemory.Write_StrBytes("89 87 4C 0D 00 00");

            //mov rax, _Databank_windowHeight
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Databank_WindowHeight_Address));
            //mov rax,[rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //mov [rdi+00000D50],rax
            CaveMemory.Write_StrBytes("89 87 50 0D 00 00");

            //Next:
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //mov [rdi+00000D04],00000000
            CaveMemory.Write_StrBytes("C7 87 04 0D 00 00 00 00 00 00");
            //mov [rdi+00000D0C],00000400
            CaveMemory.Write_StrBytes("C7 87 0C 0D 00 00 00 04 00 00");
            //mov [rdi+00000D14],00000200
            CaveMemory.Write_StrBytes("C7 87 14 0D 00 00 00 02 00 00");
            //mov [rdi+00000D44],00000000
            CaveMemory.Write_StrBytes("C7 87 44 0D 00 00 00 00 00 00");
            //movd xmm8,[rdi+00000D4C]
            CaveMemory.Write_StrBytes("66 44 0F 6E 87 4C 0D 00 00");
            //movd xmm0,[rdi+00000D0C]
            CaveMemory.Write_StrBytes("66 0F 6E 87 0C 0D 00 00");

            //mov [rdi+00000CF8],00000000
            CaveMemory.Write_StrBytes("C7 87 F8 0C 00 00 00 00 00 00");
            //mov [rdi+00000D00],00000400
            CaveMemory.Write_StrBytes("C7 87 00 0D 00 00 00 04 00 00");
            //mov [rdi+00000D18],00000200
            CaveMemory.Write_StrBytes("C7 87 18 0D 00 00 00 02 00 00");
            //mov [rdi+00000D48],00000000
            CaveMemory.Write_StrBytes("C7 87 48 0D 00 00 00 00 00 00");

            //movd xmm8,[rdi+00000D4C]
            CaveMemory.Write_StrBytes("66 44 0F 6E 87 4C 0D 00 00");
            //movd xmm0,[rdi+00000D0C]
            CaveMemory.Write_StrBytes("66 0F 6E 87 0C 0D 00 00");

            //Jump back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _AxisCorrection_Injection.InjectionReturnOffset);

            Logger.WriteLog("Adding AxisCorrection Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

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
            Buffer.Add(0x90);
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _AxisCorrection_Injection.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        /**/
        /* Overwrite raw JVS values with our own
        /**/
        private void SetHack_JVS_P1_X()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            /* Because of 64bit asm, I dont know how to load a 64bit data segment address into a register.
             * But there is an instruction to load address with an offset from RIP (instruction pointer) 
             * That's why data are stored just after the code (to know the offset)
             * */
            _Databank_JVS_P1X_Address = CaveMemory.CaveAddress + 0x20;
            Logger.WriteLog("_Databank_JVS_P1X_Address = 0x" + _Databank_JVS_P1X_Address.ToString("X16"));

            //movzx eax,byte ptr [rbx+52]
            CaveMemory.Write_StrBytes("0F B6 43 52");
            //movzx ecx,byte ptr [rbx+53]
            CaveMemory.Write_StrBytes("0F B6 4B 53");
            //mov eax, [RIP+0x12] (==> _Databank_JVS_P1X_Address)
            CaveMemory.Write_StrBytes("8B 05 12 00 00 00");
            //Jump back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _Jvs_P1X_Injection.InjectionReturnOffset);

            Logger.WriteLog("Adding JVS_P1X Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

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
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _Jvs_P1X_Injection.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }
        private void SetHack_JVS_P1_Y()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            /* Because of 64bit asm, I dont know how to load a 64bit data segment address into a register.
             * But there is an instruction to load address with an offset from RIP (instruction pointer) 
             * That's why data are stored just after the code (to know the offset)
             * */
            _Databank_JVS_P1Y_Address = CaveMemory.CaveAddress + 0x20;
            Logger.WriteLog("_Databank_JVS_P1Y_Address = 0x" + _Databank_JVS_P1Y_Address.ToString("X16"));

            //movzx eax,byte ptr [rbx+55]
            CaveMemory.Write_StrBytes("0F B6 43 55");
            //movzx ecx,byte ptr [rbx+54]
            CaveMemory.Write_StrBytes("0F B6 4B 54");
            //mov ecx, [RIP+0x12] (==> _Databank_JVS_P1Y_Address)
            CaveMemory.Write_StrBytes("8B 0D 12 00 00 00");
            //mov eax, edx
            CaveMemory.Write_StrBytes("8B C2");
            //Jump back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _Jvs_P1Y_Injection.InjectionReturnOffset);

            Logger.WriteLog("Adding JVS_P1Y Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

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
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _Jvs_P1Y_Injection.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }
        private void SetHack_JVS_P2_X()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            /* Because of 64bit asm, I dont know how to load a 64bit data segment address into a register.
             * But there is an instruction to load address with an offset from RIP (instruction pointer) 
             * That's why data are stored just after the code (to know the offset)
             * */
            _Databank_JVS_P2X_Address = CaveMemory.CaveAddress + 0x20;
            Logger.WriteLog("_Databank_JVS_P2X_Address = 0x" + _Databank_JVS_P2X_Address.ToString("X16"));

            //movzx eax,byte ptr [rbx+57]
            CaveMemory.Write_StrBytes("0F B6 43 57");
            //movzx ecx,byte ptr [rbx+56]
            CaveMemory.Write_StrBytes("0F B6 4B 56");
            //mov eax, [RIP+0x12] (==> _Databank_JVS_P2X_Address)
            CaveMemory.Write_StrBytes("8B 05 12 00 00 00");
            //Jump back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _Jvs_P2X_Injection.InjectionReturnOffset);

            Logger.WriteLog("Adding JVS_P2X Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

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
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _Jvs_P2X_Injection.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }
        private void SetHack_JVS_P2_Y()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            /* Because of 64bit asm, I dont know how to load a 64bit data segment address into a register.
             * But there is an instruction to load address with an offset from RIP (instruction pointer) 
             * That's why data are stored just after the code (to know the offset)
             * */
            _Databank_JVS_P2Y_Address = CaveMemory.CaveAddress + 0x20;
            Logger.WriteLog("_Databank_JVS_P2Y_Address = 0x" + _Databank_JVS_P2Y_Address.ToString("X16"));

            //movzx eax,byte ptr [rbx+55]
            CaveMemory.Write_StrBytes("0F B6 43 55");
            //movzx ecx,byte ptr [rbx+54]
            CaveMemory.Write_StrBytes("0F B6 4B 54");
            //mov ecx, [RIP+0x12] (==> _Databank_JVS_P2Y_Address)
            CaveMemory.Write_StrBytes("8B 0D 12 00 00 00");
            //mov eax, edx
            CaveMemory.Write_StrBytes("8B C2");
            //Jump back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _Jvs_P2Y_Injection.InjectionReturnOffset);

            Logger.WriteLog("Adding JVS_P2Y Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

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
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _Jvs_P2Y_Injection.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }       

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>   
        public override void SendInput(PlayerSettings PlayerData)
        {
            /*byte[] bufferX = BitConverter.GetBytes(PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes(PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes((IntPtr)_Databank_JVS_P1X_Address, bufferX);
                WriteBytes((IntPtr)_Databank_JVS_P1Y_Address, bufferY);               
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((IntPtr)_Databank_JVS_P2X_Address, bufferX);
                WriteBytes((IntPtr)_Databank_JVS_P2Y_Address, bufferY);
            }*/
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Clip, OutputId.P2_Clip));
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
            /*UInt64 PtrOutput1 = ReadPtr((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032FC700));
            PtrOutput1 = ReadPtr((IntPtr)(PtrOutput1 + 0x40));
            PtrOutput1 = ReadPtr((IntPtr)(PtrOutput1 + 0xA0));
            PtrOutput1 = ReadPtr((IntPtr)PtrOutput1);*/

            UInt64 PtrOutput1 = ReadPtrChain((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032FC700), new UInt64[] { 0x40, 0xA0, 0x00 });                  
            if (PtrOutput1 != 0)
            {
                SetOutputValue(OutputId.P1_LmpStart, ReadByte((IntPtr)(PtrOutput1 + 0x4A0)) >> 6 & 0x01);
                SetOutputValue(OutputId.P2_LmpStart, ReadByte((IntPtr)(PtrOutput1 + 0x4A0)) >> 3 & 0x01);
                //SetOutputValue(OutputId.P1_CoinBlocker, ReadByte((IntPtr)(PtrOutput1 + 0x478)) >> 5 & 0x01);
                //SetOutputValue(OutputId.P2_CoinBlocker, ReadByte((IntPtr)(PtrOutput1 + 0x478)) >> 2 & 0x01);
            }

            int P1_Life = 0;
            int P2_Life = 0;
            int P1_Ammo = 0;
            int P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            UInt64 Ptr1 = ReadPtr((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _PlayerStatusPointer_Offset));            
            if (Ptr1 != 0)
            {
                //Customs Outputs
                //Player Status :
                //[0] : Inactive
                //[1] : In-Game
                int P1_Status = ReadByte((IntPtr)(Ptr1 + 0x231));
                int P2_Status = ReadByte((IntPtr)(Ptr1 + 0x232));

                if (P1_Status != 0)
                { 
                    /*UInt64 PtrAmmo = ReadPtr((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032B5B88));
                    PtrAmmo = ReadPtr((IntPtr)(PtrAmmo + 0x30));
                    PtrAmmo = ReadPtr((IntPtr)(PtrAmmo + 0xB0));
                    PtrAmmo = ReadPtr((IntPtr)(PtrAmmo + 0x90));*/
                    UInt64 PtrAmmo = ReadPtrChain((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032B5B88), new UInt64[] { 0x30, 0xB0, 0x90 });
                    P1_Ammo = ReadByte((IntPtr)(PtrAmmo + 0x378));
                    P1_Life = ReadByte((IntPtr)(Ptr1 + 0x364));

                    //Custom Recoil
                    if (P1_Ammo < _P1_LastAmmo)
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (P1_Ammo > 0)
                        P1_Clip = 1;

                    //[Damaged] custom Output                
                    if (P1_Life < _P1_LastLife)
                        SetOutputValue(OutputId.P1_Damaged, 1);
                }

                if (P2_Status != 0)
                {
                    /*UInt64 PtrAmmo = ReadPtr((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032B5B88));
                    PtrAmmo = ReadPtr((IntPtr)(PtrAmmo + 0x30));
                    PtrAmmo = ReadPtr((IntPtr)(PtrAmmo + 0xB0));
                    PtrAmmo = ReadPtr((IntPtr)(PtrAmmo + 0x98));*/
                    UInt64 PtrAmmo = ReadPtrChain((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032B5B88), new UInt64[] { 0x30, 0xB0, 0x98 });                    
                    P2_Ammo = ReadByte((IntPtr)(PtrAmmo + 0x378));
                    P2_Life = ReadByte((IntPtr)(Ptr1 + 0x43C));

                    //Custom Recoil
                    if (P2_Ammo < _P2_LastAmmo)
                        SetOutputValue(OutputId.P2_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (P2_Ammo > 0)
                        P2_Clip = 1;

                    //[Damaged] custom Output  
                    if (P2_Life < _P2_LastLife)
                        SetOutputValue(OutputId.P2_Damaged, 1);
                }   
            }

            _P1_LastAmmo = P1_Ammo;
            _P1_LastLife = P1_Life;
            _P2_LastAmmo = P2_Ammo;
            _P2_LastLife = P2_Life;

            SetOutputValue(OutputId.P1_Ammo, P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, P1_Life);
            SetOutputValue(OutputId.P2_Life, P2_Life);

            /*UInt64 PtrCredits = ReadPtr((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032FC700));
            PtrCredits = ReadPtr((IntPtr)(PtrCredits + 0x88));
            PtrCredits = ReadPtr((IntPtr)(PtrCredits + 0x80));
            PtrCredits = ReadPtr((IntPtr)(PtrCredits + 0x50));*/
            UInt64 PtrCredits = ReadPtrChain((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032FC700), new UInt64[] { 0x88, 0x90, 0x50 });                   
            SetOutputValue(OutputId.Credits, ReadByte((IntPtr)(PtrCredits + 0x21C)));
        }

        #endregion
    
    }
}
