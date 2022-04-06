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
    class Game_TtxHauntedMuseum : Game
    {  
        /*** MEMORY ADDRESSES **/
        private const UInt32 _Axis_Injection_Offset = 0x0009F0DA;
        private const UInt32 _Axis_Injection_Return_Offset = 0x0009F0E7;
        private const UInt32 _Buttons_Injection_Offset = 0x000012BE;
        private const UInt32 _Buttons_Injection_Return_Offset = 0x000012C3;

        private UInt32 _ScreenWidth_Offset = 0x00328520;
        private UInt32 _ScreenHeight_Offset = 0x00328524;

        private UInt32 _P1_X_CaveAddress;
        private UInt32 _P1_Y_CaveAddress;
        private UInt32 _P2_X_CaveAddress;
        private UInt32 _P2_Y_CaveAddress;
        private UInt32 _P1_Trigger_CaveAddress;
        private UInt32 _P2_Trigger_CaveAddress;

        //Outputs
        private UInt32 _CustomRecoil_Injection_Offset = 0x0006FDCA;
        private UInt32 _CustomRecoil_InjectionReturn_Offset = 0x0006FDCF;
        private UInt32 _CustomDamage_Injection_Offset = 0x00072798;
        private UInt32 _CustomDamage_InjectionReturn_Offset = 0x0007279D;
        private UInt32 _P1_RecoilStatus_CaveAddress = 0;
        private UInt32 _P2_RecoilStatus_CaveAddress = 0;
        private UInt32 _P1_DamageStatus_CaveAddress = 0;
        private UInt32 _P2_DamageStatus_CaveAddress = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_TtxHauntedMuseum(String RomName, double _ForcedXratio, bool DisableInputHack, bool Verbose)
            : base(RomName, "game", _ForcedXratio, DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Haunted Museum v1.00 - Original", "ea27e06f3f918697fe9f924e728f5e80");
            _KnownMd5Prints.Add("Haunted Museum v1.00 - For JConfig", "792b34d2451c7a6c1fd347a29aaf0b35");

            _tProcess.Start();
            Logger.WriteLog("Waiting for TTX " + _RomName + " game to hook.....");
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
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();
                            if (!_DisableInputHack)
                                SetHack();
                            else
                                Logger.WriteLog("Input Hack disabled");
                            SetHack_Outputs();
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
                    //Window size
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //This engine (common with other TTX shooter) is waiting for X and Y value in range [0 ; WindowSize]
                    //BUT using the raw window size is troublesome when the game is combined with DxWnd as the
                    //resulting real window is not the same size as the game engine parameters (SCREEN_WITH, RENDER_WIDTH, etc...)
                    //That's why we're going to read the memory to find the INI parameter and scale the X,Y values accordingly
                    byte[] bufferX = ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _ScreenWidth_Offset, 4);
                    double GameResX = (double)BitConverter.ToUInt32(bufferX, 0);
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

                    if (_ForcedXratio != 0)
                    {
                        Logger.WriteLog("Forcing X Ratio to = " + _ForcedXratio.ToString());
                        double ViewportHeight = GameResY;
                        double ViewportWidth = GameResX / (1 / _ForcedXratio);
                        double SideBarsWidth = (ViewportWidth - GameResX) / 2;
                        Logger.WriteLog("Game Viewport size (Px) = [ " + ((int)ViewportWidth).ToString() + "x" + ((int)ViewportHeight).ToString() + " ]");
                        Logger.WriteLog("SideBars Width (px) = " + ((int)SideBarsWidth).ToString());
                        RatioX = ViewportWidth / TotalResX;
                        PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(RatioX * PlayerData.RIController.Computed_X) - SideBarsWidth);
                    }
                    else
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

        private void SetHack()
        {
            CreateDataBank();
            SetHack_Axis();            
            SetHack_Trigger();    

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        //For custom outputs :
        //The gun is vibrating on hit and on fire, so we the purpose is to generate
        //a custom Flag for each of them separatly
        private void SetHack_Outputs()
        {
            CreateDataBank_Outputs();
            SetHack_Damage();
            SetHack_Recoil();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Creating a zone in memory where we will save our devices Axis and Buttons status.
        /// This memory will then be read by the game thanks to the following hacks.
        /// </summary>
        private void CreateDataBank()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            _P1_X_CaveAddress = CaveMemory.CaveAddress; ;
            _P1_Y_CaveAddress = CaveMemory.CaveAddress + 0x08;
            _P1_Trigger_CaveAddress = CaveMemory.CaveAddress + 0x10;
            _P2_X_CaveAddress = CaveMemory.CaveAddress + 0x20;
            _P2_Y_CaveAddress = CaveMemory.CaveAddress + 0x28;
            _P2_Trigger_CaveAddress = CaveMemory.CaveAddress + 0x30;

            Logger.WriteLog("Custom data will be stored at : 0x" + _P1_X_CaveAddress.ToString("X8"));
        }

        private void CreateDataBank_Outputs()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);           

            _P1_RecoilStatus_CaveAddress = CaveMemory.CaveAddress + 0x00;
            _P2_RecoilStatus_CaveAddress = CaveMemory.CaveAddress + 0x04;
            _P1_DamageStatus_CaveAddress = CaveMemory.CaveAddress + 0x00;
            _P2_DamageStatus_CaveAddress = CaveMemory.CaveAddress + 0x04;

            Logger.WriteLog("Custom Outputs data will be stored at : 0x" + _P1_X_CaveAddress.ToString("X8"));
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
            //cmp ecx,O1
            CaveMemory.Write_StrBytes("83 F9 01");
            //je P2
            CaveMemory.Write_StrBytes("0F 84 12 00 00 00");
            //mov edx,[P1_X]
            byte[] b = BitConverter.GetBytes(_P1_X_CaveAddress);
            CaveMemory.Write_StrBytes("8B 15");
            CaveMemory.Write_Bytes(b);
            //mov [ebx], edx
            CaveMemory.Write_StrBytes("89 13");
            //mov eax,[P1_Y]
            b = BitConverter.GetBytes(_P1_Y_CaveAddress);
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(b);
            //jmp exit
            CaveMemory.Write_StrBytes("E9 0D 00 00 00");
            //P2
            //mov edx,[P2_X]
            b = BitConverter.GetBytes(_P2_X_CaveAddress);
            CaveMemory.Write_StrBytes("8B 15");
            CaveMemory.Write_Bytes(b);
            //mov [ebx],edx
            CaveMemory.Write_StrBytes("89 13");
            //mov eax,[P2_Y]
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
            //(lpKeystate+0x100) address is in ESI register  
            //and [esi-100], 0xFF0000FF
            CaveMemory.Write_StrBytes("81 A6 00 FF FF FF FF 00 00 FF");
            //cmp [_P1_Trigger], 80
            CaveMemory.Write_StrBytes("81 3D");
            byte[] b = BitConverter.GetBytes(_P1_Trigger_CaveAddress);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("80 00 00 00");
            //jne P2_Trigger
            CaveMemory.Write_StrBytes("0F 85 0A 00 00 00");
            //or [esi-FF], 80
            CaveMemory.Write_StrBytes("81 8E 01 FF FF FF 80 00 00 00");
            
            //P2_Trigger:
            //cmp [_P1_Trigger], 80
            CaveMemory.Write_StrBytes("81 3D");
            b = BitConverter.GetBytes(_P2_Trigger_CaveAddress);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("80 00 00 00");
            //jne originalcode
            CaveMemory.Write_StrBytes("0F 85 0A 00 00 00");
            //or [esi-FF], 80
            CaveMemory.Write_StrBytes("81 8E 02 FF FF FF 80 00 00 00");
            //OriginalCode
            //call game.exe+AFCD0
            CaveMemory.Write_call((UInt32)_TargetProcess.MainModule.BaseAddress + 0xAFCD0);
            //return
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
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        /// <summary>
        /// Code injection where the game is calling for rumble because of Recoil
        /// That way we can knwo when a bullet is fired and create our own output
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            byte[] b = BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x00975E54);
            //cmp dword ptr [ecx*8+game.exe+975E54], 00
            CaveMemory.Write_StrBytes("83 3C CD");
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("00");
            //jne originalCode
            CaveMemory.Write_StrBytes("0F 85 11 00 00 00");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //shl ecx, 2
            CaveMemory.Write_StrBytes("C1 E1 02");
            //add ecx, _P1_RecoilStatus_CaveAddress
            b = BitConverter.GetBytes(_P1_RecoilStatus_CaveAddress);
            CaveMemory.Write_StrBytes("81 C1");
            CaveMemory.Write_Bytes(b);
            //mov [ecx], 1
            CaveMemory.Write_StrBytes("C7 01 01 00 00 00");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //OriginalCode:
            //call game.exe+702C0
            CaveMemory.Write_call((UInt32)_TargetProcess_MemoryBaseAddress + 0x000702C0);
            //jmp return
            CaveMemory.Write_jmp((UInt32)_TargetProcess_MemoryBaseAddress + _CustomRecoil_InjectionReturn_Offset);

            Logger.WriteLog("Adding Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess_MemoryBaseAddress + _CustomRecoil_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess_MemoryBaseAddress + _CustomRecoil_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);                     
        }

        /// <summary>
        /// Code injection where the game is calling for rumble because of damage.
        /// That way we can known when a player is damaged and make our own output.
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            byte[] b = BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x00975E54);
            //cmp dword ptr [ecx*8+game.exe+975E54], 00
            CaveMemory.Write_StrBytes("83 3C CD");
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("00");
            //jne originalCode
            CaveMemory.Write_StrBytes("0F 85 11 00 00 00");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //shl ecx, 2
            CaveMemory.Write_StrBytes("C1 E1 02");
            //add ecx, _P1_DamageStatus_CaveAddress
            b = BitConverter.GetBytes(_P1_DamageStatus_CaveAddress);
            CaveMemory.Write_StrBytes("81 C1");
            CaveMemory.Write_Bytes(b);
            //mov [ecx], 1
            CaveMemory.Write_StrBytes("C7 01 01 00 00 00");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //OriginalCode:
            //call game.exe+702C0
            CaveMemory.Write_call((UInt32)_TargetProcess_MemoryBaseAddress + 0x000702C0);
            //jmp return
            CaveMemory.Write_jmp((UInt32)_TargetProcess_MemoryBaseAddress + _CustomDamage_InjectionReturn_Offset);

            Logger.WriteLog("Adding Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _CustomDamage_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _CustomDamage_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);              
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
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes(_P2_X_CaveAddress, bufferX);
                WriteBytes(_P2_Y_CaveAddress, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteByte(_P2_Trigger_CaveAddress, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte(_P2_Trigger_CaveAddress, 0x00);
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
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
            //Original Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01E27022) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01E27022) >> 5 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_R, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01E27022) >> 7 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_G, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01E27022) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_B, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01E27022) >> 3 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_R, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01E27021) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_G, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01E27021) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_B, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01E27021) >> 5 & 0x01);
            SetOutputValue(OutputId.P1_GunMotor, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01E27022) >> 2 & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01E27022) >> 1 & 0x01);           

            //Custom Outputs:
            //[Damaged] custom Output
            if (ReadByte(_P1_DamageStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P1_Damaged, 1);
                WriteByte(_P1_DamageStatus_CaveAddress, 0x00);
            }
            if (ReadByte(_P2_DamageStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P2_Damaged, 1);
                WriteByte(_P2_DamageStatus_CaveAddress, 0x00);
            }
            //[Recoil] custom Output
            if (ReadByte(_P1_RecoilStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                WriteByte(_P1_RecoilStatus_CaveAddress, 0x00);
            }
            if (ReadByte(_P2_RecoilStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte(_P2_RecoilStatus_CaveAddress, 0x00);
            }
            //Life if player is InGame
            //Player Status :
            //[0] : Inactive
            //[1] : In-Game
            //[2] : Continue Screen
            //[3] : Game Over
            int P1_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00975EB0);
            if (P1_Status == 1)
            {
                int Life = (int)(100.0 * BitConverter.ToSingle(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x00975F6C, 4), 0));
                if (Life < 0)
                    Life = 0;
                SetOutputValue(OutputId.P1_Life, Life);
            }
            else
            {
                SetOutputValue(OutputId.P1_Life, 0);
            }
            //Life if player is InGame
            int P2_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00975EF4);
            if (P2_Status == 1)
            {
                int Life = (int)(100.0 * BitConverter.ToSingle(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x00975F8C, 4), 0));
                if (Life < 0)
                    Life = 0;
                SetOutputValue(OutputId.P2_Life, Life);
            }
            else
            {
                SetOutputValue(OutputId.P2_Life, 0);
            }

            //Credits
            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x0098B3F8));
        }

        #endregion
    }
}
