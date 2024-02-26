using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.MemoryX64;
using DsCore.RawInput;
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

        //Custom Outputs
        private int _P1_LastLife = 0;
        private int _P2_LastLife = 0;
        private int _P1_LastAmmo = 0;
        private int _P2_LastAmmo = 0;
        private int _P1_Life = 0;
        private int _P2_Life = 0;
        private int _P1_Ammo = 0;
        private int _P2_Ammo = 0;       

        //Custom Values
        private UInt64 _Databank_JVS_P1X_Address = 0;
        private UInt64 _Databank_JVS_P1Y_Address = 0;
        private UInt64 _Databank_JVS_P2X_Address = 0;
        private UInt64 _Databank_JVS_P2Y_Address = 0;

        

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_AllsHodSd(String RomName, bool DisableInputHack, bool Verbose) : base(RomName, "Hodzero-Win64-Shipping", DisableInputHack, Verbose)
        {
            _DisableInputHack = true; //Still WIP

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
                            if (FindGameWindow_Equals("Hodzero "))
                            {
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
                                CheckExeMd5();
                                if (!_DisableInputHack)
                                    SetHack();
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
                    
                    
                    
                    
                    /*double dMinX = 132.0;
                    double dMaxX = 916.0;
                    double dMaxY = 1023.0;

                    double deltaX = dMaxX - dMinX;
                    PlayerData.RIController.Computed_X = Convert.ToInt16(dMinX + Math.Round(deltaX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(dMaxY - Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));
                    if (PlayerData.RIController.Computed_X < 0)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 0)
                        PlayerData.RIController.Computed_Y = 0;
                    if (PlayerData.RIController.Computed_X > (int)dMaxX)
                        PlayerData.RIController.Computed_X = (int)dMaxX;
                    if (PlayerData.RIController.Computed_Y > (int)dMaxY)
                        PlayerData.RIController.Computed_Y = (int)dMaxY;*/

                    /*double dMaxX = 1024.0;
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
                     * */

                    double dMaxX = (double)TotalResX;
                    double dMaxY = (double)TotalResY;
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

        #region Memory Hack

        private void SetHack()
        {
            //First part is to set our own JVS base values when the game is reading them in the JVS data array
            /*SetHack_JVS_P1_X();
            SetHack_JVS_P1_Y();
            SetHack_JVS_P2_X();
            SetHack_JVS_P2_Y();*/
            SetHack_JVS_Test();

            //Then to correct offsets between the cursors on menu screens and real data, we need to change the cursor position so that it's correct
            //whatever the window size is (otherwise, only aligned in 1920x1080)


            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        private void SetHack_JVS_Test()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            /* Because of 64bit asm, I dont know how to load a 64bit data segment address into a register.
             * But there is an instruction to load address with an offset from RIP (instruction pointer) 
             * That's why data are stored just after the code (to know the offset)
             * */
            _Databank_JVS_P1X_Address = CaveMemory.CaveAddress + 0x30;
            _Databank_JVS_P1Y_Address = CaveMemory.CaveAddress + 0x38;
            Logger.WriteLog("_Databank_JVS_P1X_Address = 0x" + _Databank_JVS_P1X_Address.ToString("X16"));

            //mov eax, [RIP+0x2A] (==> _Databank_JVS_P1X_Address)
            CaveMemory.Write_StrBytes("8B 05 2A 00 00 00");
            //mov [rdi+00000BB8],eax
            CaveMemory.Write_StrBytes("89 87 B8 0B 00 00");
            //mov eax, [RIP+0x26] (==> _Databank_JVS_P1X_Address)
            CaveMemory.Write_StrBytes("8B 05 26 00 00 00");
            //mov [rdi+00000BBC],eax
            CaveMemory.Write_StrBytes("89 87 BC 0B 00 00");

            //comis xmm4, xmm0
            CaveMemory.Write_StrBytes("0F 2F E0");
            //mov eax,[rdi+00000BC0]
            CaveMemory.Write_StrBytes("8B 87 C0 0B 00 00");

            //Jump back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + 0x1deee4);

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
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x1deed5), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }


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
            byte[] bufferX = BitConverter.GetBytes(PlayerData.RIController.Computed_X);
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
            }
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
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));            
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            
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

            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            UInt64 Ptr1 = ReadPtr((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x03087750));            
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
                    _P1_Ammo = ReadByte((IntPtr)(PtrAmmo + 0x378));
                    _P1_Life = ReadByte((IntPtr)(Ptr1 + 0x364));

                    //Custom Recoil
                    if (_P1_Ammo < _P1_LastAmmo)
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (_P1_Ammo > 0)
                        P1_Clip = 1;

                    //[Damaged] custom Output                
                    if (_P1_Life < _P1_LastLife)
                        SetOutputValue(OutputId.P1_Damaged, 1);
                }

                if (P2_Status != 0)
                {
                    /*UInt64 PtrAmmo = ReadPtr((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032B5B88));
                    PtrAmmo = ReadPtr((IntPtr)(PtrAmmo + 0x30));
                    PtrAmmo = ReadPtr((IntPtr)(PtrAmmo + 0xB0));
                    PtrAmmo = ReadPtr((IntPtr)(PtrAmmo + 0x98));*/
                    UInt64 PtrAmmo = ReadPtrChain((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032B5B88), new UInt64[] { 0x30, 0xB0, 0x98 });                    
                    _P2_Ammo = ReadByte((IntPtr)(PtrAmmo + 0x378));
                    _P2_Life = ReadByte((IntPtr)(Ptr1 + 0x43C));

                    //Custom Recoil
                    if (_P2_Ammo < _P2_LastAmmo)
                        SetOutputValue(OutputId.P2_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (_P2_Ammo > 0)
                        P2_Clip = 1;

                    //[Damaged] custom Output  
                    if (_P2_Life < _P2_LastLife)
                        SetOutputValue(OutputId.P2_Damaged, 1);
                }   
            }

            _P1_LastAmmo = _P1_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastAmmo = _P2_Ammo;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

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
