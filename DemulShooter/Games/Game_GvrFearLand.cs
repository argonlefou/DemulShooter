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
    class Game_GvrFearLand : Game
    {
        /*** MEMORY ADDRESSES **/
        private UInt32 _Axis_Injection_Offset = 0x0009EE10;
        private UInt32 _Axis_Injection_Return_Offset = 0x0009EE1D;
        private UInt32 _Buttons_Injection_Offset = 0x000B31CA;
        private UInt32 _Buttons_Injection_Return_Offset = 0x000B31D0;
        private UInt32 _GetKeyboardState_Address = 0x002D6388;
        private UInt32 _LpKeyState_Address = 0x00C55118;

        private UInt32 _ScreenWidth_Offset = 0x00498B50;
        private UInt32 _ScreenHeight_Offset = 0x00498B54;

        private UInt32 _P1_X_CaveAddress;
        private UInt32 _P1_Y_CaveAddress;
        private UInt32 _P2_X_CaveAddress;
        private UInt32 _P2_Y_CaveAddress;
        private UInt32 _P1_Trigger_CaveAddress;
        private UInt32 _P2_Trigger_CaveAddress;
        private UInt32 _P1_Action_CaveAddress;
        private UInt32 _P2_Action_CaveAddress;        

        private bool _AlternativeGameplay = false;


        /// <summary>
        /// Constructor
        /// </summary>
        public Game_GvrFearLand(String RomName, bool AlternativeGameplay)
            : base(RomName, "game")
        {
            _AlternativeGameplay = AlternativeGameplay;
            _KnownMd5Prints.Add("Haunted Museum 2 v1.01 - Original", "0320d68acfb7ee7b4784ed43b113f0a0");
            _KnownMd5Prints.Add("Haunted Museum 2 v1.01 - Original + Patched Header", "06ef6c57a1200bed1c9b0fa8ad172aee");
            _KnownMd5Prints.Add("Haunted Museum 2 v1.01 - Original + Patched Header + NoCrosshair", "93dcb39c43ce0906676d6673eb09c5e6");
            _KnownMd5Prints.Add("Haunted Museum 2 v1.00 - 'Game Full'", "d3639bf04ababc7246df5a0a0892408b");
            _KnownMd5Prints.Add("Haunted Museum 2 v1.00 - 'Game Full' + Patched Header", "228a3d0e81141c3850459bfaf968321a");
            _KnownMd5Prints.Add("Haunted Museum 2 v1.00 - 'Game Full' + Patched Header + NoCrosshair", "80e10a8e5363205b670f9366e3fe78b1");
            _tProcess.Start();
            Logger.WriteLog("Waiting for Global VR " + _RomName + " game to hook.....");
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
                            _GameWindowHandle = _TargetProcess.MainWindowHandle;
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();
                            Apply_MemoryHacks();
                            _ProcessHooked = true;
                            RaiseGameHookedEvent();                            
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

                    //This engine (common with other TTX shooter) is waiting for X and Y value in range [0 ; WindowSize]
                    //BUT using the raw window size is troublesome when the game is combined with DxWnd as the
                    //resulting real window is not the same size as the game engine parameters (SCREEN_WITH, RENDER_WIDTH, etc...)
                    //That's why we're going to read the memory to find the INI parameter and scale the X,Y values accordingly
                    byte[] bufferX = ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _ScreenWidth_Offset, 4);
                    double GameResX = (double)BitConverter.ToInt32(bufferX, 0);
                    byte[] bufferY = ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _ScreenHeight_Offset, 4);
                    double GameResY = (double)BitConverter.ToInt32(bufferY, 0);

                    Logger.WriteLog("Game engine render resolution (Px) = [ " + GameResX + "x" + GameResY + " ]");

                    double dMinX = 0.0;
                    double dMaxX = GameResX;
                    double dMinY = 0.0;
                    double dMaxY = GameResY;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    double RatioX = GameResX / TotalResX;
                    double RatioY = GameResY / TotalResY;

                    PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(RatioX * PlayerData.RIController.Computed_X));
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(Math.Round(RatioY * PlayerData.RIController.Computed_Y));

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
            _P1_X_CaveAddress = _InputsDatabank_Address + 0x40;
            _P1_Y_CaveAddress = _InputsDatabank_Address + 0x48;
            _P1_Trigger_CaveAddress = _InputsDatabank_Address + 0x50;
            _P1_Action_CaveAddress = _InputsDatabank_Address + 0x58;
            _P2_X_CaveAddress = _InputsDatabank_Address + 0x60;
            _P2_Y_CaveAddress = _InputsDatabank_Address + 0x68;
            _P2_Trigger_CaveAddress = _InputsDatabank_Address + 0x70;
            _P2_Action_CaveAddress = _InputsDatabank_Address + 0x78;

            SetHack_Axis();
            SetHack_Trigger();

            //Initialize values
            if (_AlternativeGameplay)
            {
                WriteByte(_P1_Action_CaveAddress, 0x80);
                WriteByte(_P2_Action_CaveAddress, 0x80);
            }

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// P1 and P2 share same memory values so we split them :
        /// Changing proc so that X and Y will be read on custom memomy values.
        /// We will feed it with device axis data.
        /// </summary> 
        private void SetHack_Axis()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();            
            //cmp eax,O1
            CaveMemory.Write_StrBytes("83 F8 01");
            //je P2X
            CaveMemory.Write_StrBytes("0F 84 0B 00 00 00");
            //mov edx,[P1_X]
            byte[] b = BitConverter.GetBytes(_P1_X_CaveAddress);
            CaveMemory.Write_StrBytes("8B 15");
            CaveMemory.Write_Bytes(b);
            //jmp 
            CaveMemory.Write_StrBytes("E9 06 00 00 00");
            //P2X: mov edx,[P2_X]
            b = BitConverter.GetBytes(_P2_X_CaveAddress);
            CaveMemory.Write_StrBytes("8B 15");
            CaveMemory.Write_Bytes(b);
            //mov [edi],edx
            CaveMemory.Write_StrBytes("89 17");

            //cmp eax,O1
            CaveMemory.Write_StrBytes("83 F8 01");
            //je P2Y
            CaveMemory.Write_StrBytes("0F 84 0A 00 00 00");
            //mov eax,[P1_Y]
            b = BitConverter.GetBytes(_P1_Y_CaveAddress);
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(b);
            //jmp exit
            CaveMemory.Write_StrBytes("E9 05 00 00 00");
            //P2Y: mov eax,[P2_Y]
            b = BitConverter.GetBytes(_P2_Y_CaveAddress);
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _Axis_Injection_Return_Offset);

            Logger.WriteLog("Adding Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _Axis_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _Axis_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }        

        /// <summary>
        /// For this hack we will wait the GetKeyboardState call,
        /// and immediately after we will read on our custom memory storage
        /// to replace lpKeystate bytes for mouse buttons (see WINUSER.H for virtualkey codes)
        /// </summary>
        private void SetHack_Trigger()
        {  
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //call USER32.GetKEyboardState
            CaveMemory.Write_StrBytes("FF 15");
            byte[] b = BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _GetKeyboardState_Address);
            CaveMemory.Write_Bytes(b);
            //and [lpkeystate + 1], 0xFF00FFFF
            CaveMemory.Write_StrBytes("81 25");
            b = BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _LpKeyState_Address + 1); /*0x00C55119*/
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("00 00 FF 00");  
            //cmp [_P1_Trigger], 80
            CaveMemory.Write_StrBytes("81 3D");
            b = BitConverter.GetBytes(_P1_Trigger_CaveAddress);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("80 00 00 00");
            //jne P1_Action
            CaveMemory.Write_StrBytes("0F 85 0A 00 00 00");
            //or [lpkeystate + 1], 80
            CaveMemory.Write_StrBytes("81 0D");
            b = BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _LpKeyState_Address + 1); /*0x00C55119*/
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("80 00 00 00");
            //P1_Action:
            //cmp [_P1_Action], 80
            CaveMemory.Write_StrBytes("81 3D");
            b = BitConverter.GetBytes(_P1_Action_CaveAddress);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("80 00 00 00");
            //jne P2_Trigger
            CaveMemory.Write_StrBytes("0F 85 0A 00 00 00");
            //or [lpkeystate + X], 80
            CaveMemory.Write_StrBytes("81 0D");
            b = BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _LpKeyState_Address + 51); /*0x00C5514B*/
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("80 00 00 00");

            //P2_Trigger:
            //cmp [_P1_Trigger], 80
            CaveMemory.Write_StrBytes("81 3D");
            b = BitConverter.GetBytes(_P2_Trigger_CaveAddress);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("80 00 00 00");
            //jne P2_Action
            CaveMemory.Write_StrBytes("0F 85 0A 00 00 00");
            //or [lpkeystate + 2], 80
            CaveMemory.Write_StrBytes("81 0D");
            b = BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _LpKeyState_Address + 2); /*0x00C5511A*/
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("80 00 00 00");
            //P2_Action:
            //cmp [_P2_Action], 80
            CaveMemory.Write_StrBytes("81 3D");
            b = BitConverter.GetBytes(_P2_Action_CaveAddress);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("80 00 00 00");
            //jne exit
            CaveMemory.Write_StrBytes("0F 85 0A 00 00 00");
            //or [lpkeystate + X], 80
            CaveMemory.Write_StrBytes("81 0D");
            b = BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _LpKeyState_Address + 52); /*0x00C5514C*/
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("80 00 00 00");
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Return_Offset);
           
            Logger.WriteLog("Adding Trigger CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
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
                    WriteByte(_P1_Trigger_CaveAddress, 0x80);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte(_P1_Trigger_CaveAddress, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    if (_AlternativeGameplay)
                        WriteByte(_P1_Action_CaveAddress, 0x00);
                    else
                        WriteByte(_P1_Action_CaveAddress, 0x80);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    if (_AlternativeGameplay)
                        WriteByte(_P1_Action_CaveAddress, 0x80);
                    else
                        WriteByte(_P1_Action_CaveAddress, 0x00);
                }
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes(_P2_X_CaveAddress, bufferX);
                WriteBytes(_P2_Y_CaveAddress, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteByte(_P2_Trigger_CaveAddress, 0x80);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte(_P2_Trigger_CaveAddress, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    if (_AlternativeGameplay)
                        WriteByte(_P2_Action_CaveAddress, 0x00);
                    else
                        WriteByte(_P2_Action_CaveAddress, 0x80);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    if (_AlternativeGameplay)
                        WriteByte(_P2_Action_CaveAddress, 0x80);
                    else
                        WriteByte(_P2_Action_CaveAddress, 0x00);
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
            //Gun recoil : is handled by the game like it should (On/Off with every bullets)
            //Gun motor  : is activated when player gets hit
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_R, OutputId.P1_Lmp_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_G, OutputId.P1_Lmp_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_B, OutputId.P1_Lmp_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_R, OutputId.P2_Lmp_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_G, OutputId.P2_Lmp_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_B, OutputId.P2_Lmp_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunRecoil, OutputId.P2_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
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
            int P1_RecoilState = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002B140C0) & 0x01;
            int P1_RumbleState = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002B140B6) >> 2 & 0x01;
            int P2_RecoilState = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002B140C1) & 0x01;
            int P2_RumbleState = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002B140B6) >> 1 & 0x01;

            //Orginal
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002B140B6) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002B140B6) >> 5 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_R, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002B140B6) >> 7 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_G, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002B140B6) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_B, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002B140B6) >> 3 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_R, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002B140B5) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_G, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002B140B5) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_B, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x002B140B5) >> 5 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, P1_RecoilState);
            SetOutputValue(OutputId.P1_GunMotor, P1_RumbleState);            
            SetOutputValue(OutputId.P2_GunRecoil, P2_RecoilState);
            SetOutputValue(OutputId.P2_GunMotor, P2_RumbleState);

            _P1_Life = 0;
            _P1_Ammo = 0;
            _P2_Life = 0;
            _P2_Ammo = 0;

            //Customs Outputs
            //Player Status :
            //[0] : Inactive
            //[1] : In-Game
            //[2] : Continue Screen
            //[3] : Game Over    
            int P1_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00C37364);            
            if (P1_Status == 1)
            {
                _P1_Ammo = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x00C39D3C, 4), 0);
                _P1_Life = (int)(100.0 * BitConverter.ToSingle(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x00C39C88, 4), 0));
                
                if (_P1_Life < 0)
                    _P1_Life = 0;

                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);                
            }


            int P2_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00C3736C);
            if (P2_Status == 1)
            {
                _P2_Ammo = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x00C39DC8, 4), 0);
                _P2_Life = (int)(100.0 * BitConverter.ToSingle(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x00C39C94, 4), 0));
                
                if (_P2_Life < 0)
                    _P2_Life = 0;

                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);      
            }

            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            //Custom Recoil will simply be activated just like original Recoil
            SetOutputValue(OutputId.P1_CtmRecoil, P1_RecoilState);
            SetOutputValue(OutputId.P2_CtmRecoil, P2_RecoilState);
            //Custom Damaged will simply be activated just like original rumble
            /* De-activated : Rumble also occurs with environmental actions !
            SetOutputValue(OutputId.P1_Damaged, P1_RumbleState);
            SetOutputValue(OutputId.P2_Damaged, P2_RumbleState);
            */
            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00C4D758));
        }

        #endregion
    }
}
