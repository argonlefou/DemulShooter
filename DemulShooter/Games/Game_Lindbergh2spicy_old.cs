using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_Lindbergh2spicy_old : Game
    {
        //Address to find InputStruct values (read at instruction 0x82EFC63)
        private UInt32 _InputStruct_Address = 0x0C8B2430;

        //INPUT_STRUCT offset in game
        private UInt32 _Input_X_Offset = 0x17D;
        private UInt32 _Input_Y_Offset = 0x183;

        //NOP for Gun axis and buttons in-game
        private NopStruct _Nop_Axix_X_1 = new NopStruct(0x082F0109, 7);
        private NopStruct _Nop_Axix_X_2 = new NopStruct(0x082EFEFC, 7);
        private NopStruct _Nop_Axix_Y_1 = new NopStruct(0x082F0153, 7);
        private NopStruct _Nop_Axix_Y_2 = new NopStruct(0x082EFF13, 7);

        private UInt32 _Custom_Buttons_Bank_Ptr = 0;
        private UInt32 _Trigger_Injection_Address = 0x080820B9;
        private UInt32 _Trigger_Injection_Return_Address = 0x080820C0;
        private UInt32 _Reload_Injection_Address = 0x080820F3;

        //Check instruction for game loaded
        private UInt32 _RomLoaded_Check_Instruction = 0x082EFC63;

        //Outputs
        private UInt32 _OutputsPtr_Address = 0x0A89F944;
        private UInt32 _Outputs_Address;
        private UInt32 _Credits_Address = 0x0C8C0240;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_Lindbergh2spicy_old(String RomName, double _ForcedXratio, bool Verbose)
            : base(RomName, "BudgieLoader", _ForcedXratio, Verbose)
        {
            _tProcess.Start();
            Logger.WriteLog("Waiting for Lindbergh " + _RomName + " game to hook.....");
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
                            //To make sure BurgieLoader has loaded the rom entirely, we're looking for some random instruction to be present in memory before starting 
                            byte[] buffer = ReadBytes(_RomLoaded_Check_Instruction, 5);
                            if (buffer[0] == 0xB8 && buffer[1] == 0x30 && buffer[2] == 0x24 && buffer[3] == 0x8B && buffer[4] == 0x0C)
                            {
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                SetHack();
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();                                
                            }
                            else
                            {
                                Logger.WriteLog("Game not Loaded, waiting...");
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
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");
                    //X => [0x0A - 0xF5]
                    //Y => [0x06 - 0xFA]                   
                    double dMaxX = 236.0;
                    double dMaxY = 245.0;

                    PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX) + 0x0A);
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY) + 0x06);                    
                    if (PlayerData.RIController.Computed_X < 10)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 6)
                        PlayerData.RIController.Computed_Y = 0;
                    if (PlayerData.RIController.Computed_X > 245)
                        PlayerData.RIController.Computed_X = 255;
                    if (PlayerData.RIController.Computed_Y > 250)
                        PlayerData.RIController.Computed_Y = 255;

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
            SetHack_DataBank();
            SetHack_OverwriteTrigger();
            SetHack_OverwriteReload();

            SetNops(0, _Nop_Axix_X_1);
            SetNops(0, _Nop_Axix_X_2);
            SetNops(0, _Nop_Axix_Y_1);
            SetNops(0, _Nop_Axix_Y_2);            
            
            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Creating a custom memory bank to store our Buttons data
        /// </summary>
        private void SetHack_DataBank()
        {
            //1st Codecave : storing P1 and P2 input structure data, read from register in main program code
            Codecave DataCaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            DataCaveMemory.Open();
            DataCaveMemory.Alloc(0x800);

            _Custom_Buttons_Bank_Ptr = DataCaveMemory.CaveAddress;

            Logger.WriteLog("Custom data will be stored at : 0x" + _Custom_Buttons_Bank_Ptr.ToString("X8"));
        }

        private void SetHack_OverwriteTrigger()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //movzx edx, [_Custom_Buttons_Bank_Ptr]
            CaveMemory.Write_StrBytes("0F B6 15");
            Buffer.AddRange(BitConverter.GetBytes(_Custom_Buttons_Bank_Ptr));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //and [_Custom_Buttons_Bank_Ptr], 0xFFFFFFFD
            CaveMemory.Write_StrBytes("80 25");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes(_Custom_Buttons_Bank_Ptr));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("FD");
            CaveMemory.Write_jmp(_Trigger_Injection_Address);

            Logger.WriteLog("Adding Trigger Codecave_1 at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - (_Reload_Injection_Address) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, _Trigger_Injection_Return_Address, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        private void SetHack_OverwriteReload()
        {
            List<byte> Buffer = new List<byte>();
            Buffer.Add(0x0F);
            Buffer.Add(0xB6);
            Buffer.Add(0x15);
            Buffer.AddRange(BitConverter.GetBytes(_Custom_Buttons_Bank_Ptr));
            WriteBytes(_Reload_Injection_Address, Buffer.ToArray());
        }        

        #endregion

        #region Inputs

        public override void SendInput(PlayerSettings PlayerData)
        {
            if (PlayerData.ID == 1)
            {
                WriteByte(_InputStruct_Address + _Input_X_Offset, (byte)PlayerData.RIController.Computed_X);
                WriteByte(_InputStruct_Address + _Input_Y_Offset, (byte)PlayerData.RIController.Computed_Y);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Custom_Buttons_Bank_Ptr, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Custom_Buttons_Bank_Ptr, 0xFD);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask(_Custom_Buttons_Bank_Ptr, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_Custom_Buttons_Bank_Ptr, 0xFE);
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : Is activated permanently while trigger is pressed
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpPanel, OutputId.LmpPanel));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp1, OutputId.Lmp1));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp2, OutputId.Lmp2));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp3, OutputId.Lmp3));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp4, OutputId.Lmp4));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp5, OutputId.Lmp5));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp6, OutputId.Lmp6));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            _Outputs_Address = BitConverter.ToUInt32(ReadBytes(_OutputsPtr_Address, 4), 0);
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.LmpPanel, ReadByte(_Outputs_Address + 1) >> 7 & 0x01);
            SetOutputValue(OutputId.Lmp1, ReadByte(_Outputs_Address + 1) >> 1 & 0x01);
            SetOutputValue(OutputId.Lmp2, ReadByte(_Outputs_Address + 1) >> 2 & 0x01);
            SetOutputValue(OutputId.Lmp3, ReadByte(_Outputs_Address + 1) >> 3 & 0x01);
            SetOutputValue(OutputId.Lmp4, ReadByte(_Outputs_Address + 1) >> 4 & 0x01);
            SetOutputValue(OutputId.Lmp5, ReadByte(_Outputs_Address + 1) >> 5 & 0x01);
            SetOutputValue(OutputId.Lmp6, ReadByte(_Outputs_Address + 1) >> 6 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, ReadByte(_Outputs_Address) >> 6 & 0x01);
            SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
    }
}
