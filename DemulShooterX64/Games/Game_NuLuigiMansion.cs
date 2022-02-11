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
    public class Game_NuLuigiMansion : Game
    {
        /*** MEMORY ADDRESSES **/
        private UInt64 _P1_X_Address = 0;
        private UInt64 _P1_Y_Address = 0;
        private UInt64 _P1_Buttons_Address = 0;        
        private const UInt64 P1_INJECTION_OFFSET = 0x00017E76;
        private const UInt64 P1_INJECTION_RETURN_OFFSET = 0x00017E92;

        private UInt64 _P2_X_Address = 0;
        private UInt64 _P2_Y_Address = 0;
        private UInt64 _P2_Buttons_Address = 0;
        private const UInt64 P2_INJECTION_OFFSET = 0x00017EDA;
        private const UInt64 P2_INJECTION_RETURN_OFFSET = 0x00017EF6;

        //Check instruction for game loaded
        private const int ROM_LOADED_CHECK_INSTRUCTION_OFFSET = 0x17E6E;        

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_NuLuigiMansion(String RomName, bool DisableInputHack, bool Verbose) : base(RomName, "vacuum", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("VACUUM.EXE Teknoparrot dump", "8ddfab1cd2140670d9437738c9c331c8");

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
                            Int64 aTest = _TargetProcess_MemoryBaseAddress.ToInt64() + ROM_LOADED_CHECK_INSTRUCTION_OFFSET;
                            
                            byte[] buffer = ReadBytes((IntPtr)aTest, 2);
                            if (buffer[0] == 0x74 && buffer[1] == 0x58)
                            {
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                CheckExeMd5();
                                SetHack();
                                _ProcessHooked = true;                                
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
            if (!_DisableInputHack)
            {
                SetHackP1();
                SetHackP2();
            }
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
            Logger.WriteLog("_P1_Data_Address = 0x" + (CaveMemory.CaveAddress + 0x50).ToString("X16"));
            _P1_X_Address = CaveMemory.CaveAddress + 0x50;
            _P1_Y_Address = CaveMemory.CaveAddress + 0x60;
            _P1_Buttons_Address = CaveMemory.CaveAddress + 0x70;

            List<Byte> Buffer = new List<Byte>();
            //mov r8d, [RIP+100]     (==> _P1_X_Address)
            CaveMemory.Write_StrBytes("44 8B 05 49 00 00 00");
            //mov r9d, [RIP+100]     (==> _P1_Y_Address)
            CaveMemory.Write_StrBytes("44 8B 0D 52 00 00 00");
            //mov r10d, [rax+ 000000BC]
            CaveMemory.Write_StrBytes("44 8B 90 BC 00 00 00");
            //mov r11d, [rax+ 000000C0]
            CaveMemory.Write_StrBytes("44 8B 98 C0 00 00 00");
            //and r10d, 0x1000
            CaveMemory.Write_StrBytes("41 81 E2 00 10 00 00");
            //push rbx
            CaveMemory.Write_StrBytes("53");
            //mov rbx, [RIP+100]     (==> _P1_Button_Address)
            CaveMemory.Write_StrBytes("48 8B 1D 45 00 00 00");
            //or r10d, rbx
            CaveMemory.Write_StrBytes("49 09 DA");
            //pop rbx
            CaveMemory.Write_StrBytes("5B");
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + P1_INJECTION_RETURN_OFFSET);

            Logger.WriteLog("Adding P1 Buttons Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code Injection
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
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + P1_INJECTION_OFFSET), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }
        private void SetHackP2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            Logger.WriteLog("_P2_Data_Address = 0x" + (CaveMemory.CaveAddress + 0x50).ToString("X16"));
            _P2_X_Address = CaveMemory.CaveAddress + 0x50;
            _P2_Y_Address = CaveMemory.CaveAddress + 0x60;
            _P2_Buttons_Address = CaveMemory.CaveAddress + 0x70;

            List<Byte> Buffer = new List<Byte>();
            //mov r8d, [RIP+100]     (==> _P2_X_Address)
            CaveMemory.Write_StrBytes("44 8B 05 49 00 00 00");
            //mov r9d, [RIP+100]     (==> _P2_Y_Address)
            CaveMemory.Write_StrBytes("44 8B 0D 52 00 00 00");
            //mov r10d, [rax+ 000000D0]
            CaveMemory.Write_StrBytes("44 8B 90 D0 00 00 00");
            //mov r11d, [rax+ 000000D4]
            CaveMemory.Write_StrBytes("44 8B 98 D4 00 00 00");
            //and r10d, 0x1000
            CaveMemory.Write_StrBytes("41 81 E2 00 10 00 00");
            //push rbx
            CaveMemory.Write_StrBytes("53");
            //mov rbx, [RIP+100]     (==> _P2_Button_Address)
            CaveMemory.Write_StrBytes("48 8B 1D 45 00 00 00");
            //or r10d, rbx
            CaveMemory.Write_StrBytes("49 09 DA");
            //pop rbx
            CaveMemory.Write_StrBytes("5B");
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + P2_INJECTION_RETURN_OFFSET);

            Logger.WriteLog("Adding P2 Buttons Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code Injection
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
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + P2_INJECTION_OFFSET), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
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
                WriteBytes((IntPtr)(_P1_X_Address), BitConverter.GetBytes(PlayerData.RIController.Computed_X));
                WriteBytes((IntPtr)(_P1_Y_Address), BitConverter.GetBytes(PlayerData.RIController.Computed_Y));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((IntPtr)(_P1_Buttons_Address + 1), 0x40);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((IntPtr)(_P1_Buttons_Address + 1), 0xBF);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask((IntPtr)(_P1_Buttons_Address + 1), 0x20);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask((IntPtr)(_P1_Buttons_Address + 1), 0xDF);
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((IntPtr)(_P2_X_Address), BitConverter.GetBytes(PlayerData.RIController.Computed_X));
                WriteBytes((IntPtr)(_P2_Y_Address), BitConverter.GetBytes(PlayerData.RIController.Computed_Y));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((IntPtr)(_P2_Buttons_Address + 1), 0x40);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((IntPtr)(_P2_Buttons_Address + 1), 0xBF);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask((IntPtr)(_P2_Buttons_Address + 1), 0x20);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask((IntPtr)(_P2_Buttons_Address + 1), 0xDF);
            }
        }

        #endregion
    }
}
