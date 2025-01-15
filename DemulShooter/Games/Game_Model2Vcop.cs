﻿using System;
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
    class Game_Model2Vcop : Game
    {     
        /*** MEMORY ADDRESSES **/
        private UInt32 _Buttons_Injection_Offset = 0x000CBA64;
        private UInt32 _Reload_Injection_Offset = 0x000C8A3E;
        private UInt32 _P1_Axis_Injection_Offset_1 = 0x000CAAF5;
        private UInt32 _P2_Axis_Injection_Offset_1 = 0x000CAB2F;
        private UInt32 _P1_X_CaveAddress;
        private UInt32 _P1_Y_CaveAddress;
        private UInt32 _P2_X_CaveAddress;
        private UInt32 _P2_Y_CaveAddress;
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _Reload_CaveAddress;

        //Outputs
        private UInt32 _OutputsPtr_Offset = 0x001AA730;
        private UInt32 _Outputs_BaseAddress;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_Model2Vcop(String RomName)
            : base(RomName, "EMULATOR")
        {   
            _KnownMd5Prints.Add("Model2Emulator 1.1a", "26bd488f9a391dcac1c5099014aa1c9e");
            _KnownMd5Prints.Add("Model2Emulator 1.1a multicpu", "ac59ce7cfb95d6d639c0f0d1afba1192");

            _tProcess.Start();
            Logger.WriteLog("Waiting for Model2 " + _RomName + " game to hook.....");
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
                        if (p.ProcessName.Equals("EMULATOR") || p.ProcessName.Equals("emulator_multicpu"))
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

                    //X [0x0000 -> 0x01EF]
                    //Y [0x0000 -> 0x017F]
                    double dMaxX = 496.0;
                    double dMaxY = 384.0;

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

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _P1_X_CaveAddress = _InputsDatabank_Address;
            _P1_Y_CaveAddress = _InputsDatabank_Address + 0x04;
            _P2_X_CaveAddress = _InputsDatabank_Address + 0x08;
            _P2_Y_CaveAddress = _InputsDatabank_Address + 0x0C;
            _Buttons_CaveAddress = _InputsDatabank_Address + 0x10;
            _Reload_CaveAddress = _InputsDatabank_Address + 0x11;

            //Buttons : The game is reading the Byte containing Buttons info. Replacing the real address with our own
            byte[] b = BitConverter.GetBytes(_Buttons_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Offset + 2, b);
            //Same thing for Reload flag
            b = BitConverter.GetBytes(_Reload_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Reload_Injection_Offset + 1, b);

            //Axis
            SetHack_P1Axis(_P1_Axis_Injection_Offset_1, 8);
            SetHack_P2Axis(_P2_Axis_Injection_Offset_1, 8);

            //Initial values
            WriteBytes(_P1_X_CaveAddress, BitConverter.GetBytes((Int32)0xA5));
            WriteBytes(_P1_Y_CaveAddress, BitConverter.GetBytes((Int32)0xC0));
            WriteBytes(_P2_X_CaveAddress, BitConverter.GetBytes((Int32)0x14A));
            WriteBytes(_P2_Y_CaveAddress, BitConverter.GetBytes((Int32)0xC0));
            WriteByte(_Buttons_CaveAddress, 0xFF);
            WriteByte(_Reload_CaveAddress, 0x00);

            Logger.WriteLog("nputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        private void SetHack_P1Axis(UInt32 InjectionOffset, UInt32 Length)
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            //mov ecx,[_P1_X_CaveAddress]
            CaveMemory.Write_StrBytes("8B 0D");
            byte[] b = BitConverter.GetBytes(_P1_X_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov edx,[_P1_Y_CaveAddress]
            CaveMemory.Write_StrBytes("8B 15");
            b = BitConverter.GetBytes(_P1_Y_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess_MemoryBaseAddress + InjectionOffset + Length);

            Logger.WriteLog("Adding P1Axis CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess_MemoryBaseAddress + InjectionOffset) - 5;
            List<byte> Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess_MemoryBaseAddress + InjectionOffset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        private void SetHack_P2Axis(UInt32 InjectionOffset, UInt32 Length)
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            //mov ecx,[_P1_X_CaveAddress]
            CaveMemory.Write_StrBytes("8B 0D");
            byte[] b = BitConverter.GetBytes(_P2_X_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov edx,[_P1_Y_CaveAddress]
            CaveMemory.Write_StrBytes("8B 15");
            b = BitConverter.GetBytes(_P2_Y_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess_MemoryBaseAddress + InjectionOffset + Length);

            Logger.WriteLog("Adding P2Axis CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess_MemoryBaseAddress + InjectionOffset) - 5;
            List<byte> Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess_MemoryBaseAddress + InjectionOffset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
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
                WriteBytes(_P1_X_CaveAddress, bufferX);
                WriteBytes(_P1_Y_CaveAddress, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFE);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x01);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    Apply_OR_ByteMask(_Reload_CaveAddress, 0x01);
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFE);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {                    
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x01);
                    Apply_AND_ByteMask(_Reload_CaveAddress, 0xFE);
                }
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes(_P2_X_CaveAddress, bufferX);
                WriteBytes(_P2_Y_CaveAddress, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFD);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x02);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    Apply_OR_ByteMask(_Reload_CaveAddress, 0x2);
                    Apply_AND_ByteMask(_Buttons_CaveAddress, 0xFD);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress, 0x02);
                    Apply_AND_ByteMask(_Reload_CaveAddress, 0xFD);                    
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
            UInt32 iTemp = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _OutputsPtr_Offset);
            _Outputs_BaseAddress = ReadPtr(iTemp + 0x630);

            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_BaseAddress + 0x0050169C) >> 2 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Outputs_BaseAddress + 0x0050169C) >> 3 & 0x01);

            //custom Outputs  
            int P1_Status = ReadByte(_Outputs_BaseAddress + 0x0050EE30);
            int P2_Status = ReadByte(_Outputs_BaseAddress + 0x0050EE34);

            _P1_Life = ReadByte(_Outputs_BaseAddress + 0x0050EE70);
            _P2_Life = ReadByte(_Outputs_BaseAddress + 0x0050EE74);
            _P1_Ammo = ReadByte(_Outputs_BaseAddress + 0x0050EE38);
            _P2_Ammo = ReadByte(_Outputs_BaseAddress + 0x0050EE3C);

            if (P1_Status == 1)
            {
                //Custom Recoil
                if (_P1_Ammo < _P1_LastAmmo)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Clip Empty] custom Output
                if (_P1_Ammo <= 0)
                    SetOutputValue(OutputId.P1_Clip, 0);
                else
                    SetOutputValue(OutputId.P1_Clip, 1);

                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                _P1_LastAmmo = _P1_Ammo;
                _P1_LastLife = _P1_Life;
            }
            else
            {
                SetOutputValue(OutputId.P1_Clip, 0);
                _P1_Ammo = 0;
                _P1_LastAmmo = 0;
                _P1_Life = 0;
                _P1_LastLife = 0;
            }

            if (P2_Status == 1)
            {
                //Custom Recoil
                if (_P2_Ammo < _P2_LastAmmo)
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                //[Clip Empty] custom Output
                if (_P2_Ammo <= 0)
                    SetOutputValue(OutputId.P2_Clip, 0);
                else
                    SetOutputValue(OutputId.P2_Clip, 1);

                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);

                _P2_LastAmmo = _P2_Ammo;
                _P2_LastLife = _P2_Life;
            }
            else
            {
                SetOutputValue(OutputId.P2_Clip, 0);
                _P2_Ammo = 0;
                _P2_LastAmmo = 0;
                _P2_Life = 0;
                _P2_LastLife = 0;
            }
            
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.Credits, ReadByte(_Outputs_BaseAddress + 0x00559270));
        }

        #endregion
    }
}
