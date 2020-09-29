using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
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
        public Game_NuLuigiMansion_v2(String RomName, bool Verbose) : base(RomName, "vacuum", Verbose)
        {
            _KnownMd5Prints.Add("VACUUM.EXE - Original Dump", "5120bbe464b35f4cc894238bd9f9e11b");
            _KnownMd5Prints.Add("VACUUM.EXE - 'SpeedFix'", "8ddfab1cd2140670d9437738c9c331c8");
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
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
                                    CheckExeMd5();
                                    SetHack();
                                    _ProcessHooked = true;
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
                    //Window size
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");
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

        private void SetHack()
        {
            SetHackP1();
            SetHackP2();

            Logger.WriteLog("Memory Hack complete !");
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
            List<Byte> Buffer = new List<Byte>();IntPtr ProcessHandle = _TargetProcess.Handle;
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
    }
}
