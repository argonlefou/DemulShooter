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
    public class Game_Es3Tc5 : Game
    {
        /*** MEMORY ADDRESSES **/
        private UInt64 _P1_Axis_CaveAddress = 0;
        private const Int32 _P1_X_OFFSET = 0;
        private const Int32 _P1_Y_OFFSET = 0x08;
        private const Int32 _P1_MAXX_OFFSET = 0x10;
        private const Int32 _P1_MAXY_OFFSET = 0x18;

        private UInt64 GUNMAX_X_OFFSET = 0x0185E388;
        private UInt64 GUNMAX_Y_OFFSET = 0x0185E38C;

        //Because of 64bits process, I only know how to alloc memory and use a long jump (14 bytes instruction !)
        //In this case, I have to use a 15 bytes long "0xCC" between 2 function to write the long jump
        //So the original hack will short jump (not enough available bytes) to the place where I can put the long jump
        private UInt64 _P1_Injection_Offset = 0x0006B063;
        private UInt64 _LongJump_Offset = 0x000ABA41;
        private UInt64 _P1_Injection_Return_Offset = 0x0006B069;
        
        //Check instruction for game loaded
        private const UInt64 ROM_LOADED_CHECK_INSTRUCTION_OFFSET = 0x0006B060;        

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Es3Tc5(String RomName, bool Verbose) : base(RomName, "TimeCrisisGame-Win64-Shipping", Verbose)
        {
            _KnownMd5Prints.Add("TimeCrisisGame-Win64-Shipping.exe - Original Dump", "5297b9296708d4f83181f244ee2bc3db");
            _tProcess.Start();
            Logger.WriteLog("Waiting for Namco ES3 " + _RomName + " game to hook.....");
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
                            UInt64 aTest = (UInt64)_TargetProcess_MemoryBaseAddress + GUNMAX_X_OFFSET;
                            byte[] buffer = ReadBytes((IntPtr)aTest, 4);
                            int x = BitConverter.ToInt32(buffer, 0);

                            aTest = (UInt64)_TargetProcess_MemoryBaseAddress + GUNMAX_Y_OFFSET;
                            buffer = ReadBytes((IntPtr)aTest, 4);
                            int y = BitConverter.ToInt32(buffer, 0);



                            //UInt64 aTest = (UInt64)_TargetProcess_MemoryBaseAddress + ROM_LOADED_CHECK_INSTRUCTION_OFFSET;
                            
                            //byte[] buffer = ReadBytes((IntPtr)aTest, 3);
                            //if (buffer[0] == 0x0F && buffer[1] == 0x4C && buffer[2] == 0xC1)
                            if(x != 0 && y != 0)
                            {
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
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
        /// Fullscreen mode causes issue with windows size, so for now this will only work with fullscreen mode
        /// Game resolution will be read in memory
        /// </summary>
        /// <param name="PlayerData"></param>
        /// <returns></returns>
        public override bool ClientScale(PlayerSettings PlayerData)
        {
            return true;
        }

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
                    /*Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;*/

                    /*
                    UInt64 aTest = (UInt64)_TargetProcess_MemoryBaseAddress + GUNMAX_X_OFFSET;
                    byte[] buffer = ReadBytes((IntPtr)aTest, 4);
                    double TotalResX = Convert.ToDouble(BitConverter.ToInt32(buffer, 0));
                    aTest = (UInt64)_TargetProcess_MemoryBaseAddress + GUNMAX_Y_OFFSET;
                    buffer = ReadBytes((IntPtr)aTest, 4);
                    double TotalResY = Convert.ToDouble(BitConverter.ToInt32(buffer, 0));
                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    PlayerData.RIController.Computed_X = Convert.ToInt32(Math.Round(PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt32(Math.Round(PlayerData.RIController.Computed_Y / TotalResY));
                    if (PlayerData.RIController.Computed_X < 0)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 0)
                        PlayerData.RIController.Computed_Y = 0;
                    if (PlayerData.RIController.Computed_X > (int)TotalResX)
                        PlayerData.RIController.Computed_X = (int)TotalResX;
                    if (PlayerData.RIController.Computed_Y > (int)TotalResY)
                        PlayerData.RIController.Computed_Y = (int)TotalResY;

                    /*
                    //X => [0 - GameX]
                    //Y => [0 - GameY]
                    
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
                    */

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
            SetHackAxis();

            UInt64 aTest = (UInt64)_TargetProcess_MemoryBaseAddress + GUNMAX_X_OFFSET;
            byte[] buffer = ReadBytes((IntPtr)aTest, 4);
            WriteBytes((IntPtr)(_P1_Axis_CaveAddress + _P1_MAXX_OFFSET), buffer);

            aTest = (UInt64)_TargetProcess_MemoryBaseAddress + GUNMAX_Y_OFFSET;
            buffer = ReadBytes((IntPtr)aTest, 4);
            WriteBytes((IntPtr)(_P1_Axis_CaveAddress + _P1_MAXY_OFFSET), buffer);

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// The game is working well with mouse but causes issue with a lightgun :
        /// Except in the menu (everything OK) it read Absolute values as relative movements -> stick in the corners
        /// This is a first try to inject data into memory on the fly, as I can't find a unique procedure for Axis writing.
        /// </summary>
        private void SetHackAxis()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            /* Because of 64bit asm, I dont know how to load a 64bit data segment address into a register.
             * But there is an instruction to load address with an offset from RIP (instruction pointer) 
             * That's why data are stored just after the code (to know the offset)
             * */
            _P1_Axis_CaveAddress = CaveMemory.CaveAddress + 0x40;
            Logger.WriteLog("_P1_Axis_CaveAddress = 0x" + _P1_Axis_CaveAddress.ToString("X16"));

            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[rsp+60]
            CaveMemory.Write_StrBytes("8B 44 24 60");
            //cmp eax,[RIP+45] => (MAx_X)
            CaveMemory.Write_StrBytes("3B 05 45 00 00 00");
            //je AxisX
            CaveMemory.Write_StrBytes("74 0B");
            //cmp eax,[RIP+45] => (MAx_Y)
            CaveMemory.Write_StrBytes("3B 05 45 00 00 00");
            //je AxisY
            CaveMemory.Write_StrBytes("74 0D");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //jmp exit
            CaveMemory.Write_StrBytes("EB 12");

            //AxisX:
            //pop rax
            CaveMemory.Write_StrBytes("58");
            ////mov rax, [RIP+20]     (X ==> _P1_Axis_CaveAddress + 0)
            CaveMemory.Write_StrBytes("48 8B 05 20 00 00 00");
            //jmp exit
            CaveMemory.Write_StrBytes("EB 08");

            //AxisY:
            //pop rax
            CaveMemory.Write_StrBytes("58");
            ////mov rax, [RIP+1E]     (Y ==> _P1_Axis_CaveAddress + 4)
            CaveMemory.Write_StrBytes("48 8B 05 1E 00 00 00");

            //Exit:
            //mov [rdi],eax
            CaveMemory.Write_StrBytes("89 07");
            //add rsp,20
            CaveMemory.Write_StrBytes("48 83 C4 20");
            //jmp back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _P1_Injection_Return_Offset);
            Logger.WriteLog("Adding P1 Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

            //Code Injection
            //1st Step : writing the long jump
            IntPtr ProcessHandle = _TargetProcess.Handle;
            List<Byte> Buffer = new List<Byte>();
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress));
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _LongJump_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);

            //2nd step : writing the short jump
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.Add(0xD9);
            Buffer.Add(0x09);
            Buffer.Add(0x04);
            Buffer.Add(0x00);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P1_Injection_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
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
                WriteBytes((IntPtr)(_P1_Axis_CaveAddress + _P1_X_OFFSET), bufferX);
                WriteBytes((IntPtr)(_P1_Axis_CaveAddress + _P1_Y_OFFSET), bufferY);

                /*if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((IntPtr)(_P1_Axis_CaveAddress + 1), 0x40);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((IntPtr)(_P1_Axis_CaveAddress + 1), 0xBF);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask((IntPtr)(_P1_Axis_CaveAddress + 1), 0x20);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask((IntPtr)(_P1_Axis_CaveAddress + 1), 0xDF);*/
            }            
        }

        #endregion
    }
}
