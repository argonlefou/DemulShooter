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
    public class Game_NuLuigiMansion_v2 : Game
    {
        private UInt64 _Input_BaseAddress;

        /*** MEMORY ADDRESSES **/
        private const UInt64 P1_X_OFFSET = 0x2C;
        private const UInt64 P1_Y_OFFSET = 0x30;
        private const UInt64 P2_X_OFFSET = 0x40;
        private const UInt64 P2_Y_OFFSET = 0x44;
        private UInt64 _P1_Buttons_CaveAddress = 0;
        private UInt64 _P2_Buttons_CaveAddress = 0;
        private UInt64 _P1_Injection_Offset = 0x00017EB8;
        private UInt64 _P1_Injection_Return_Offset = 0x00017EC8;
        private UInt64 _P2_Injection_Offset = 0x00017F1D;
        private UInt64 _P2_Injection_Return_Offset = 0x00017F2D;

        //Check instruction for game loaded
        private const UInt64 ROM_LOADED_CHECK_INSTRUCTION_OFFSET = 0x00017E6E;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_NuLuigiMansion_v2(String RomName) : base(RomName, "vacuum")
        {
            _KnownMd5Prints.Add("VACUUM.EXE - Original Dump", "5120bbe464b35f4cc894238bd9f9e11b");
            _KnownMd5Prints.Add("VACUUM.EXE - 'SpeedFix'", "8ddfab1cd2140670d9437738c9c331c8");
            _KnownMd5Prints.Add("VACUUM.EXE - For JConfig", "63c70cf8b080c1972e9e753f258e9507");
            _tProcess.Start();
            Logger.WriteLog("Waiting for SEGA Nu " + _RomName + " game to hook.....");
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
                            UInt64 aTest = (UInt64)_TargetProcess_MemoryBaseAddress + ROM_LOADED_CHECK_INSTRUCTION_OFFSET;
                            
                            byte[] buffer = ReadBytes((IntPtr)aTest, 2);
                            if (buffer[0] == 0x74 && buffer[1] == 0x58)
                            {
                                aTest = (UInt64)_TargetProcess_MemoryBaseAddress + 0x004F6CB8;
                                Logger.WriteLog("aTest = 0x" + aTest.ToString("X16")); 
                                buffer = ReadBytes((IntPtr)aTest, 8);
                                _Input_BaseAddress = BitConverter.ToUInt64(buffer, 0);
                                Logger.WriteLog("_Input_BaseAddress (1st step) = 0x" + _Input_BaseAddress.ToString("X16")); 

                                buffer = ReadBytes((IntPtr)_Input_BaseAddress, 8);
                                _Input_BaseAddress = BitConverter.ToUInt64(buffer, 0);
                                Logger.WriteLog("_Input_BaseAddress = 0x" + _Input_BaseAddress.ToString("X16")); 

                                if (_Input_BaseAddress != 0)
                                {
                                    _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
                                    CheckExeMd5();
                                    Apply_MemoryHacks();
                                    _ProcessHooked = true;
                                    RaiseGameHookedEvent(); 
                                }
                            }
                            else
                            {
                                Logger.WriteLog("ROM not Loaded...");
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

                    //X => [0 - 1920]
                    //Y => [0 - 1080]
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

        #region Memory Hack

        protected override void  Apply_InputsMemoryHack()
        {
            SetHackP1();
            SetHackP2();

            byte[] InitX = BitConverter.GetBytes((int)960);
            byte[] InitY = BitConverter.GetBytes((int)540);
            WriteBytes((IntPtr)(_Input_BaseAddress + P1_X_OFFSET), InitX);
            WriteBytes((IntPtr)(_Input_BaseAddress + P1_Y_OFFSET), InitY);
            WriteBytes((IntPtr)(_Input_BaseAddress + P1_X_OFFSET), InitX);
            WriteBytes((IntPtr)(_Input_BaseAddress + P1_Y_OFFSET), InitY);

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Axis values and Buttons states are written by Teknoparrot DLL, we can't block it.
        /// So instead we're targeting the instructions which are reading the values to make them read ou own instead.
        /// As usual, many buttons are sharing the sam Byte so we are filtering to block only gun buttons and still allow both START buttons
        /// </summary>
        private void SetHackP1()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            /* Because of 64bit asm, I dont know how to load a 64bit data segment address into a register.
             * But there is an instruction to load address with an offset from RIP (instruction pointer) 
             * That's why data are stored just after the code (to know the offset)
             * */
            _P1_Buttons_CaveAddress = CaveMemory.CaveAddress + 0x30;
            Logger.WriteLog("_P1_Buttons_CaveAddress = 0x" + _P1_Buttons_CaveAddress.ToString("X16"));

            //push rbx
            CaveMemory.Write_StrBytes("53");
            //mov rbx, [RIP+100]     (==> _P1_Button_CaveAddress)
            CaveMemory.Write_StrBytes("48 8B 1D 28 00 00 00");
            //and r10d, 0x1000
            CaveMemory.Write_StrBytes("41 81 E2 00 10 00 00");
            //or r10d, rbx
            CaveMemory.Write_StrBytes("49 09 DA");
            //pop rbx
            CaveMemory.Write_StrBytes("5B");
            //mov [rax+34],r10d
            CaveMemory.Write_StrBytes("44 89 50 34");
            //mov [rax+38],r11d
            CaveMemory.Write_StrBytes("44 89 58 38");
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _P1_Injection_Return_Offset);

            Logger.WriteLog("Adding P1 Buttons Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

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
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P1_Injection_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }
        private void SetHackP2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            
            _P2_Buttons_CaveAddress = CaveMemory.CaveAddress + 0x30;
            Logger.WriteLog("_P2_Buttons_CaveAddress = 0x" + _P2_Buttons_CaveAddress.ToString("X16"));
            
            //push rbx
            CaveMemory.Write_StrBytes("53");
            //mov rbx, [RIP+100]     (==> _P2_Button_CaveAddress)
            CaveMemory.Write_StrBytes("48 8B 1D 28 00 00 00");
            //and r10d, 0x1000
            CaveMemory.Write_StrBytes("41 81 E2 00 10 00 00");
            //or r10d, rbx
            CaveMemory.Write_StrBytes("49 09 DA");
            //pop rbx
            CaveMemory.Write_StrBytes("5B");
            //mov [rax+48],r10d
            CaveMemory.Write_StrBytes("44 89 50 48");
            //mov [rax+4c],r11d
            CaveMemory.Write_StrBytes("44 89 58 4C");
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _P2_Injection_Return_Offset);

            Logger.WriteLog("Adding P2 Buttons Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code Injection
            List<Byte> Buffer = new List<Byte>(); IntPtr ProcessHandle = _TargetProcess.Handle;
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
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P2_Injection_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
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
                WriteBytes((IntPtr)(_Input_BaseAddress + P1_X_OFFSET), bufferX);
                WriteBytes((IntPtr)(_Input_BaseAddress + P1_Y_OFFSET), bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((IntPtr)(_P1_Buttons_CaveAddress + 1), 0x40);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((IntPtr)(_P1_Buttons_CaveAddress + 1), 0xBF);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask((IntPtr)(_P1_Buttons_CaveAddress + 1), 0x20);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask((IntPtr)(_P1_Buttons_CaveAddress + 1), 0xDF);
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((IntPtr)(_Input_BaseAddress + P2_X_OFFSET), bufferX);
                WriteBytes((IntPtr)(_Input_BaseAddress + P2_Y_OFFSET), bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 1), 0x40);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 1), 0xBF);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 1), 0x20);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask((IntPtr)(_P2_Buttons_CaveAddress + 1), 0xDF);
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_R, OutputId.P1_Lmp_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_G, OutputId.P1_Lmp_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_B, OutputId.P1_Lmp_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_R, OutputId.P2_Lmp_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_G, OutputId.P2_Lmp_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_B, OutputId.P2_Lmp_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun_R, OutputId.P1_LmpGun_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun_G, OutputId.P1_LmpGun_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun_B, OutputId.P1_LmpGun_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun_R, OutputId.P2_LmpGun_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun_G, OutputId.P2_LmpGun_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun_B, OutputId.P2_LmpGun_B));    
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpWindow_R, OutputId.P1_LmpWindow_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpWindow_G, OutputId.P1_LmpWindow_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpWindow_B, OutputId.P1_LmpWindow_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpWindow_R, OutputId.P2_LmpWindow_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpWindow_G, OutputId.P2_LmpWindow_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpWindow_B, OutputId.P2_LmpWindow_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunRecoil, OutputId.P2_GunRecoil));

            _Outputs.Add(new GameOutput(OutputDesciption.P3_GunRecoil, OutputId.P3_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_GunRecoil, OutputId.P4_GunRecoil));

            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));           
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Clip, OutputId.P2_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            
            UInt64 Ptr1 = ReadPtr((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x004F6C98));
            Ptr1 = ReadPtr((IntPtr)Ptr1);            
            UInt64 Ptr2 = ReadPtr((IntPtr)(Ptr1 + 0x18));

            SetOutputValue(OutputId.P1_LmpStart, ReadByte((IntPtr)(Ptr2 + 0x4C)) & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((IntPtr)(Ptr2 + 0x50)) & 0x01);
            SetOutputValue(OutputId.P1_Lmp_R, ReadByte((IntPtr)(Ptr1 + 0x101)));
            SetOutputValue(OutputId.P1_Lmp_G, ReadByte((IntPtr)(Ptr1 + 0x102)));
            SetOutputValue(OutputId.P1_Lmp_B, ReadByte((IntPtr)(Ptr1 + 0x103)));
            SetOutputValue(OutputId.P2_Lmp_R, ReadByte((IntPtr)(Ptr1 + 0x105)));
            SetOutputValue(OutputId.P2_Lmp_G, ReadByte((IntPtr)(Ptr1 + 0x106)));
            SetOutputValue(OutputId.P2_Lmp_B, ReadByte((IntPtr)(Ptr1 + 0x107)));
            SetOutputValue(OutputId.P1_LmpGun_R, ReadByte((IntPtr)(Ptr1 + 0x1AD)));
            SetOutputValue(OutputId.P1_LmpGun_G, ReadByte((IntPtr)(Ptr1 + 0x1AE)));
            SetOutputValue(OutputId.P1_LmpGun_B, ReadByte((IntPtr)(Ptr1 + 0x1AF)));
            SetOutputValue(OutputId.P2_LmpGun_R, ReadByte((IntPtr)(Ptr1 + 0x1B1)));
            SetOutputValue(OutputId.P2_LmpGun_G, ReadByte((IntPtr)(Ptr1 + 0x1B2)));
            SetOutputValue(OutputId.P2_LmpGun_B, ReadByte((IntPtr)(Ptr1 + 0x1B3)));
            SetOutputValue(OutputId.P1_LmpWindow_R, ReadByte((IntPtr)(Ptr1 + 0x16D)));
            SetOutputValue(OutputId.P1_LmpWindow_G, ReadByte((IntPtr)(Ptr1 + 0x16E)));
            SetOutputValue(OutputId.P1_LmpWindow_B, ReadByte((IntPtr)(Ptr1 + 0x16F)));
            SetOutputValue(OutputId.P2_LmpWindow_R, ReadByte((IntPtr)(Ptr1 + 0x18D)));
            SetOutputValue(OutputId.P2_LmpWindow_G, ReadByte((IntPtr)(Ptr1 + 0x18E)));
            SetOutputValue(OutputId.P2_LmpWindow_B, ReadByte((IntPtr)(Ptr1 + 0x18F)));
            SetOutputValue(OutputId.P1_GunMotor, ReadByte((IntPtr)(Ptr2 + 0x2C)));
            SetOutputValue(OutputId.P2_GunMotor, ReadByte((IntPtr)(Ptr2 + 0x30)));

            //For recoil, I found 2 bytes that are changed.
            //The second one seems to be some kind of "duration" (3-2-1-0) of the recoil effect
            //The recoil seems to fire each time a ghost is sucked in the controller
            Byte Recoil1 = ReadByte((IntPtr)(Ptr2 + 0x44));
            Byte Recoil2 = ReadByte((IntPtr)(Ptr2 + 0x48));
            SetOutputValue(OutputId.P1_GunRecoil, Recoil1);
            SetOutputValue(OutputId.P2_GunRecoil, Recoil2);
            //Changing the real output value to a simple 0/1 recoil effect for the custom recoil output
            if (Recoil1 != 0)
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
            else
                SetOutputValue(OutputId.P1_CtmRecoil, 0);

            if (Recoil2 != 0)
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
            else
                SetOutputValue(OutputId.P2_CtmRecoil, 0);
            
            //In-game values are pointer affected only during a game (not valid in menus)
            //Player status :
            //[1] = Playing
            //[0] = Not Playing
            UInt32 P1_Status = 0;
            UInt32 P2_Status = 0;
            int P1_Life = 0;
            int P2_Life = 0;
            int P1_Ammo = 0;
            int P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            UInt64 Ptr3 = ReadPtr((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x004FB0C0));
            if (Ptr3 != 0)    
            {
                Ptr3 = ReadPtr((IntPtr)(Ptr3 + 0x48));
                if (Ptr3 != 0)
                {
                    P1_Status = ReadByte((IntPtr)(Ptr3 + 0x548));
                    P2_Status = ReadByte((IntPtr)(Ptr3 + 0x560));
                }     
            }
            if (P1_Status == 1)
            {
                P1_Life = ReadByte((IntPtr)(Ptr3 + 0x550));
                P1_Ammo = ReadByte((IntPtr)(Ptr3 + 0x57C));
                
                //[Clip Empty] custom Output
                if (P1_Ammo > 0)
                    P1_Clip = 1;

                //[Damaged] custom Output                
                if (P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }
            if (P2_Status == 1)
            {
                P2_Life = ReadByte((IntPtr)(Ptr3 + 0x568));
                P2_Ammo = ReadByte((IntPtr)(Ptr3 + 0x588));

                //[Clip Empty] custom Output
                if (P2_Ammo > 0)
                    P2_Clip = 1;

                //[Damaged] custom Output                
                if (P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);
            }

            _P1_LastAmmo = P1_Ammo;
            _P2_LastAmmo = P2_Ammo;
            _P1_LastLife = P1_Life;
            _P2_LastLife = P2_Life;

            SetOutputValue(OutputId.P1_Ammo, P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, P1_Life);
            SetOutputValue(OutputId.P2_Life, P2_Life);
            
            SetOutputValue(OutputId.Credits, ReadByte((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x004E19A8)));
        }

        #endregion
    
    }
}
