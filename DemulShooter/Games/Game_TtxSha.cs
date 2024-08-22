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
    class Game_TtxSha : Game
    {        
        /*** MEMORY ADDRESSES **/
        private UInt32 _P1_X_Offset = 0x002E3964;
        private UInt32 _P1_Y_Offset = 0x002E3968;
        private UInt32 _P1_Out_Offset = 0x002E3960;
        private UInt32 _P2_X_Offset = 0x002E3970;
        private UInt32 _P2_Y_Offset = 0x002E3974;
        private UInt32 _P2_Out_Offset = 0x002E396C;

        private NopStruct _Nop_X = new NopStruct(0x000D1305, 6);
        private NopStruct _Nop_Y = new NopStruct(0x000D130B, 6);
        private NopStruct _Nop_P1_Out = new NopStruct(0x000D12FC, 7);
        private NopStruct _Nop_P2_Out = new NopStruct(0x000D147E, 4);

        private UInt32 _Triggers_Injection_Offset = 0x000D1464;
        private UInt32 _Triggers_Injection_Return_Offset = 0x000D146A;
        private UInt32 _Triggers_CaveAddress;
        private UInt32 _Recoil_Injection_Offset = 0x000D0CD0;
        private UInt32 _P1_Recoil_CaveAddress;
        private UInt32 _P2_Recoil_CaveAddress;

        private UInt32 _GameResolutionWidth_Offset = 0x000083D4;
        private UInt32 _GameResolutionHeight_Offset = 0x000083CF;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public Game_TtxSha(String RomName, bool DisableInputHack, bool Verbose) 
            : base (RomName, "KSHG", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Silent Hill Arcade - Original KSHG_no_cursor.exe", "0e58fd1c7bcb5e0cace0e4ee548ecd0c");
            _KnownMd5Prints.Add("Silent Hill Arcade - Original KSHG.exe", "2778219dcaf3d8e09fb197417b17dc1b");
        
            _tProcess.Start();
            Logger.WriteLog("Waiting for Taito Type X " + _RomName + " game to hook.....");
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
                    Process[] processes = Process.GetProcesses();
                    foreach (Process p in processes)
                    {
                        if (p.ProcessName.StartsWith("KSHG"))
                        {
                            _Target_Process_Name = p.ProcessName;
                            _TargetProcess = p;
                            _ProcessHandle = _TargetProcess.Handle;
                            _TargetProcess_MemoryBaseAddress = _TargetProcess.MainModule.BaseAddress;

                            if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                CheckExeMd5();
                                Apply_MemoryHacks();
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
                                break;
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

                    byte[] buffer = ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _GameResolutionWidth_Offset, 4);
                    double GameResX = (double)BitConverter.ToUInt32(buffer, 0);
                    buffer = ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _GameResolutionHeight_Offset, 4);
                    double GameResY = (double)BitConverter.ToUInt32(buffer, 0);

                    Logger.WriteLog("Detected resolution (px) = [" + GameResX.ToString() + "x" + GameResY.ToString() + "]");

                    double dMinX = 0.0;
                    double dMaxX = GameResX;
                    double dMinY = 0.0;
                    double dMaxY = GameResY;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;
                    
                    PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dRangeX * PlayerData.RIController.Computed_X / GameResX)); 
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(Math.Round(dRangeY * PlayerData.RIController.Computed_Y / GameResY));
                    if (PlayerData.RIController.Computed_X < (int)dMinX)
                        PlayerData.RIController.Computed_X = (int)dMinX;
                    if (PlayerData.RIController.Computed_Y < (int)dMinY)
                        PlayerData.RIController.Computed_Y = (int)dMinY;
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

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _Triggers_CaveAddress = _InputsDatabank_Address;

            SetHack_Triggers();
            
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_X);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Y);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_Out);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_Out);
            
            //Set P2 IN_SCREEN
            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Out_Offset, 0x01);
            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Out_Offset, 0x01);
            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Trigger status is handled by the IO dll and analysed after by the game
        /// This will just modify the values (and force OK status for P2) before the game read it
        /// </summary>
        private void SetHack_Triggers()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer;
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, esi
            CaveMemory.Write_StrBytes("8B C6");
            //shl eax,2
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _Triggers_CaveAddress
            byte[] b = BitConverter.GetBytes(_Triggers_CaveAddress);
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(b);
            //mov eax, [eax]
            CaveMemory.Write_StrBytes("8B 00");            
            //mov [edi-04, eax]
            CaveMemory.Write_StrBytes("89 47 FC");
            //mov [edi],00000000
            CaveMemory.Write_StrBytes("C7 07 00 00 00 00");   
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //cmp dword ptr [edi-04],00
            CaveMemory.Write_StrBytes("83 7F FC 00");
            //je KSHG_no_cursor.exe+D1497
            CaveMemory.Write_je((UInt32)_TargetProcess_MemoryBaseAddress + 0xD1497);
            CaveMemory.Write_jmp((UInt32)_TargetProcess_MemoryBaseAddress + _Triggers_Injection_Return_Offset);

            Logger.WriteLog("Adding Triggers Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess_MemoryBaseAddress + _Triggers_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess_MemoryBaseAddress + _Triggers_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);              
        }

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_Recoil_CaveAddress = _OutputsDatabank_Address;
            _P2_Recoil_CaveAddress = _OutputsDatabank_Address + 0x04;

            SetHack_Recoil();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }        

        /// <summary>
        /// To activate gun recoil, the game is calling the GUN_Reaction() function of shaiolib.dll.
        /// This function has been ripped from the cracked Dll, so we are injecting a piece of code
        /// to intercept the calls and handle the Recoil ourselves when the game wants to
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer;
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[esp+08]
            CaveMemory.Write_StrBytes("8B 44 24 08");
            //shl eax,2
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _P1_Recoil_CaveAddress
            byte[] b = BitConverter.GetBytes(_P1_Recoil_CaveAddress);
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(b);
            //mov [eax], 1
            CaveMemory.Write_StrBytes("C7 00 01 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //ret
            CaveMemory.Write_StrBytes("C3");

            Logger.WriteLog("Adding Recoil Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess_MemoryBaseAddress + _Recoil_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess_MemoryBaseAddress + _Recoil_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);                  
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>        
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteBytes(_Triggers_CaveAddress, new byte[]{ 0xFF, 0xFF, 0xFF, 0xFF });
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteBytes(_Triggers_CaveAddress, new byte[] { 0x00, 0x00, 0x00, 0x00 });

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    //Set out of screen Byte 
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Out_Offset, 0x00);
                    //Trigger a shoot to reload !!
                    WriteBytes(_Triggers_CaveAddress, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    WriteBytes(_Triggers_CaveAddress, new byte[] { 0x00, 0x00, 0x00, 0x00 });
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Out_Offset, 0x01);
                }
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteBytes(_Triggers_CaveAddress + 4, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteBytes(_Triggers_CaveAddress + 4, new byte[] { 0x00, 0x00, 0x00, 0x00 });

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    //Set out of screen Byte 
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Out_Offset, 0x00);
                    //Trigger a shoot to reload !!
                    WriteBytes(_Triggers_CaveAddress + 4, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    WriteBytes(_Triggers_CaveAddress + 4, new byte[] { 0x00, 0x00, 0x00, 0x00 });
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Out_Offset, 0x01);
                }
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpLeft, OutputId.LmpLeft));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpRight, OutputId.LmpRight));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpCard_R, OutputId.P1_LmpCard_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpCard_G, OutputId.P1_LmpCard_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpCard_R, OutputId.P2_LmpCard_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpCard_G, OutputId.P2_LmpCard_G));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            byte bOutput = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002E38C0);
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002E38C4));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002E38CC));
            SetOutputValue(OutputId.P1_LmpCard_R, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002E38D4));
            SetOutputValue(OutputId.P1_LmpCard_G, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002E38DC));
            SetOutputValue(OutputId.P2_LmpCard_R, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002E38E4));
            SetOutputValue(OutputId.P2_LmpCard_G, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002E38EC));
            SetOutputValue(OutputId.LmpLeft, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002E38F4));
            SetOutputValue(OutputId.LmpRight, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002E38FC));
            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002F9008));

            //Custom Outputs
            //Original recoil Handling is stripped from the DLL so we are forced to handle the duration ourselve with an Async-reset output
            if (ReadByte(_P1_Recoil_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                WriteByte(_P1_Recoil_CaveAddress, 0);
            }
            if (ReadByte(_P2_Recoil_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte(_P2_Recoil_CaveAddress, 0);
            }            
        }

        #endregion
    }
}
