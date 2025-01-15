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
    class Game_TtxGaiaAttack4 : Game
    {
        /*** MEMORY ADDRESSES **/        
        private const UInt32 AXIS_INJECTION_OFFSET = 0x00114C99;
        private const UInt32 AXIS_INJECTION_RETURN_OFFSET = 0x00114CA6;
        private const UInt32 BUTTONS_INJECTION_OFFSET = 0x000012FB;
        private const UInt32 BUTTONS_INJECTION_RETURN_OFFSET = 0x00001300;

        private const UInt32 _ScreenWidth_Offset = 0x0032FC10;
        private const UInt32 _ScreenHeight_Offset = 0x0032FC14;

        private UInt32 _P1_X_CaveAddress;
        private UInt32 _P1_Y_CaveAddress;
        private UInt32 _P2_X_CaveAddress;
        private UInt32 _P2_Y_CaveAddress;
        private UInt32 _P3_X_CaveAddress;
        private UInt32 _P3_Y_CaveAddress;
        private UInt32 _P4_X_CaveAddress;
        private UInt32 _P4_Y_CaveAddress;
        private UInt32 _P1_Trigger_CaveAddress;
        private UInt32 _P2_Trigger_CaveAddress;
        private UInt32 _P3_Trigger_CaveAddress;
        private UInt32 _P4_Trigger_CaveAddress;

        //Outputs
        private UInt32 _CustomRecoil_Injection_Offset = 0x00073BDA;
        private UInt32 _CustomRecoil_InjectionReturn_Offset = 0x00073BDF;
        private UInt32 _CustomDamage_Injection_Offset_1 = 0x00076658;
        private UInt32 _CustomDamage_InjectionReturn_Offset_1 = 0x0007665D;
        private UInt32 _CustomDamage_Injection_Offset_2 = 0x000764A0;
        private UInt32 _CustomDamage_InjectionReturn_Offset_2 = 0x000764A5;
        private UInt32 _P1_RecoilStatus_CaveAddress = 0;
        private UInt32 _P2_RecoilStatus_CaveAddress = 0;
        private UInt32 _P3_RecoilStatus_CaveAddress = 0;
        private UInt32 _P4_RecoilStatus_CaveAddress = 0;
        private UInt32 _P1_DamageStatus_CaveAddress = 0;
        private UInt32 _P2_DamageStatus_CaveAddress = 0;
        private UInt32 _P3_DamageStatus_CaveAddress = 0;
        private UInt32 _P4_DamageStatus_CaveAddress = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_TtxGaiaAttack4(String RomName)
            : base(RomName, "game")
        {
            _KnownMd5Prints.Add("Gaia Attack 4 v1.02 - Original", "f2d8e8f7d3a9a29d4804ff7cb29aa8a2");
            _KnownMd5Prints.Add("Gaia Attack 4 v1.02 - For JConfig", "94fe4e73f6fd915ffb10885dc091d5cb");

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
                    double GameResX = (double)BitConverter.ToUInt32(bufferX, 0);
                    byte[] bufferY = ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _ScreenHeight_Offset, 4);
                    double GameResY = (double)BitConverter.ToInt32(bufferY, 0);

                    Logger.WriteLog("Game engine render resolution (Px) = [ " + GameResX + "x" + GameResY + " ]");

                    double RatioX = GameResX / TotalResX;
                    double RatioY = GameResY / TotalResY;

                    PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(RatioX * PlayerData.RIController.Computed_X));
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(Math.Round(RatioY * PlayerData.RIController.Computed_Y));

                    if (PlayerData.RIController.Computed_X < 0)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 0)
                        PlayerData.RIController.Computed_Y = 0;
                    if (PlayerData.RIController.Computed_X > (int)GameResX)
                        PlayerData.RIController.Computed_X = (int)GameResX;
                    if (PlayerData.RIController.Computed_Y > (int)GameResY)
                        PlayerData.RIController.Computed_Y = (int)GameResY;

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
            _P1_X_CaveAddress = _InputsDatabank_Address + 0x00;
            _P2_X_CaveAddress = _InputsDatabank_Address + 0x04;
            _P3_X_CaveAddress = _InputsDatabank_Address + 0x08;
            _P4_X_CaveAddress = _InputsDatabank_Address + 0x0C;
            _P1_Y_CaveAddress = _InputsDatabank_Address + 0x10;
            _P2_Y_CaveAddress = _InputsDatabank_Address + 0x14;
            _P3_Y_CaveAddress = _InputsDatabank_Address + 0x18;
            _P4_Y_CaveAddress = _InputsDatabank_Address + 0x1C;
            _P1_Trigger_CaveAddress = _InputsDatabank_Address + 0x20;
            _P2_Trigger_CaveAddress = _InputsDatabank_Address + 0x28;
            _P3_Trigger_CaveAddress = _InputsDatabank_Address + 0x30;
            _P4_Trigger_CaveAddress = _InputsDatabank_Address + 0x38;

            SetHack_Axis();
            SetHack_Trigger(); 

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// With debug controls, all players share same memory values so we split them :
        /// Changing proc so that X and Y will be read on custom memomy values
        /// We will feed it with devices axis data
        /// Player index is stored on ESI ( from 0 to 3) so basically we will use some indexing to get values :
        /// _P1_X_CaveAddress[4*ESI] and _P1_Y_CaveAddress[4*ESI]
        /// </summary>
        private void SetHack_Axis()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //
            //FIRST CODECAVE : SPLIT P1/ P2 /P3 / P4 AXIS
            //
            List<Byte> Buffer = new List<Byte>();
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //mov eax, 04
            CaveMemory.Write_StrBytes("B8 04 00 00 00");
            //mul esi
            CaveMemory.Write_StrBytes("F7 E6");
            //mov ecx, eax
            CaveMemory.Write_StrBytes("8B C8");
            //mov eax, P1_X_CaveAddress
            byte[] b = BitConverter.GetBytes(_P1_X_CaveAddress);
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(b);
            //add eax, ecx
            CaveMemory.Write_StrBytes("01 C8");
            //mov edx, [eax]
            CaveMemory.Write_StrBytes("8B 10");
            //mov edi, [edx]
            CaveMemory.Write_StrBytes("89 17");
            //mov eax, P1_Y_CaveAddress
            b = BitConverter.GetBytes(_P1_Y_CaveAddress);
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(b);
            //add eax, ecx
            CaveMemory.Write_StrBytes("01 C8");
            //mov eax, [eax]
            CaveMemory.Write_StrBytes("8B 00");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + AXIS_INJECTION_RETURN_OFFSET);

            Logger.WriteLog("Adding Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + AXIS_INJECTION_OFFSET) - 5;
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
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + AXIS_INJECTION_OFFSET, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);        
        }

        /// <summary>
        /// For this hack we will wait the GetKeyboardState call
        /// And immediately after we will read on our custom memory storage
        /// to replace lpKeystate bytes for mouse buttons (see WINUSER.H for virtualkey codes)
        /// then the game will continue...            
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

            //and [esi-9d], 0xFFFF0000
            CaveMemory.Write_StrBytes("81 A6 63 FF FF FF 00 00 FF FF");

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

            
            //cmp [_P3_Trigger], 80
            CaveMemory.Write_StrBytes("81 3D");
            b = BitConverter.GetBytes(_P3_Trigger_CaveAddress);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("80 00 00 00");
            //jne P2_Trigger
            CaveMemory.Write_StrBytes("0F 85 0A 00 00 00");
            //or [esi-9D], 80
            CaveMemory.Write_StrBytes("81 8E 63 FF FF FF 80 00 00 00");

            //cmp [_P4_Trigger], 80
            CaveMemory.Write_StrBytes("81 3D");
            b = BitConverter.GetBytes(_P4_Trigger_CaveAddress);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("80 00 00 00");
            //jne P2_Trigger
            CaveMemory.Write_StrBytes("0F 85 0A 00 00 00");
            //or [esi-9C], 80
            CaveMemory.Write_StrBytes("81 8E 64 FF FF FF 80 00 00 00");
            
            //OriginalCode
            //call game.exe+12F250
            CaveMemory.Write_call((UInt32)_TargetProcess.MainModule.BaseAddress + 0x12F250);
            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + BUTTONS_INJECTION_RETURN_OFFSET);

            Logger.WriteLog("Adding Trigger CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + BUTTONS_INJECTION_OFFSET) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + BUTTONS_INJECTION_OFFSET, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_RecoilStatus_CaveAddress = _OutputsDatabank_Address + 0x00;
            _P2_RecoilStatus_CaveAddress = _OutputsDatabank_Address + 0x04;
            _P3_RecoilStatus_CaveAddress = _OutputsDatabank_Address + 0x08;
            _P4_RecoilStatus_CaveAddress = _OutputsDatabank_Address + 0x0C;
            _P1_DamageStatus_CaveAddress = _OutputsDatabank_Address + 0x10;
            _P2_DamageStatus_CaveAddress = _OutputsDatabank_Address + 0x14;
            _P3_DamageStatus_CaveAddress = _OutputsDatabank_Address + 0x18;
            _P4_DamageStatus_CaveAddress = _OutputsDatabank_Address + 0x1C;

            SetHack_Damage(_CustomDamage_Injection_Offset_1, _CustomDamage_InjectionReturn_Offset_1);
            SetHack_Damage(_CustomDamage_Injection_Offset_2, _CustomDamage_InjectionReturn_Offset_2);
            SetHack_Recoil();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
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
            byte[] b = BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x00B16E34);
            //cmp dword ptr [ecx*8+game.exe+B16E34], 00
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
            //pop edx
            CaveMemory.Write_StrBytes("59");
            //OriginalCode:
            //call game.exe+74200
            CaveMemory.Write_call((UInt32)_TargetProcess_MemoryBaseAddress + 0x00074200);
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
        private void SetHack_Damage(UInt32 InjectionOffset, UInt32 InjectionReturnOffset)
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            byte[] b = BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x00B16E34);
            //cmp dword ptr [ecx*8+game.exe+B16E34], 00
            CaveMemory.Write_StrBytes("83 3C CD");
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("00");
            //jne originalCode
            CaveMemory.Write_StrBytes("0F 85 11 00 00 00");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //shl ecx, 2
            CaveMemory.Write_StrBytes("C1 E1 02");
            //add ecx, _P1_DammageStatus_CaveAddress
            b = BitConverter.GetBytes(_P1_DamageStatus_CaveAddress);
            CaveMemory.Write_StrBytes("81 C1");
            CaveMemory.Write_Bytes(b);
            //mov [ecx], 1
            CaveMemory.Write_StrBytes("C7 01 01 00 00 00");
            //pop edx
            CaveMemory.Write_StrBytes("59");
            //OriginalCode:
            //call game.exe+74200
            CaveMemory.Write_call((UInt32)_TargetProcess_MemoryBaseAddress + 0x00074200);
            //jmp return
            CaveMemory.Write_jmp((UInt32)_TargetProcess_MemoryBaseAddress + InjectionReturnOffset);

            Logger.WriteLog("Adding Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + InjectionOffset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + InjectionOffset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
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
            else if (PlayerData.ID == 3)
            {
                WriteBytes(_P3_X_CaveAddress, bufferX);
                WriteBytes(_P3_Y_CaveAddress, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteByte(_P3_Trigger_CaveAddress, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte(_P3_Trigger_CaveAddress, 0x00);
            }
            else if (PlayerData.ID == 4)
            {
                WriteBytes(_P4_X_CaveAddress, bufferX);
                WriteBytes(_P4_Y_CaveAddress, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteByte(_P4_Trigger_CaveAddress, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte(_P4_Trigger_CaveAddress, 0x00);
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor  : Is activated for every bullet fired AND when player gets
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_LmpStart, OutputId.P3_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_LmpStart, OutputId.P4_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_R, OutputId.P1_Lmp_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_G, OutputId.P1_Lmp_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_B, OutputId.P1_Lmp_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_R, OutputId.P2_Lmp_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_G, OutputId.P2_Lmp_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_B, OutputId.P2_Lmp_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_GunMotor, OutputId.P3_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_GunMotor, OutputId.P4_GunMotor));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P3_CtmRecoil, OutputId.P3_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P4_CtmRecoil, OutputId.P4_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Life, OutputId.P3_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Life, OutputId.P4_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P3_Damaged, OutputId.P3_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P4_Damaged, OutputId.P4_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01FE7202) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01FE7202) >> 5 & 0x01);
            SetOutputValue(OutputId.P3_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01FE7200) >> 5 & 0x01);
            SetOutputValue(OutputId.P4_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01FE7200) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_R, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01FE7202) >> 7 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_G, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01FE7202) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_B, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01FE7202) >> 3 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_R, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01FE7201) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_G, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01FE7201) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_B, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01FE7201) >> 5 & 0x01);
            SetOutputValue(OutputId.P1_GunMotor, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01FE7202) >> 2 & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01FE7202) >> 1 & 0x01);
            SetOutputValue(OutputId.P3_GunMotor, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01FE7200) >> 3 & 0x01);
            SetOutputValue(OutputId.P4_GunMotor, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x01FE7200) >> 2 & 0x01);

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
            if (ReadByte(_P3_DamageStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P3_Damaged, 1);
                WriteByte(_P3_DamageStatus_CaveAddress, 0x00);
            }
            if (ReadByte(_P4_DamageStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P4_Damaged, 1);
                WriteByte(_P4_DamageStatus_CaveAddress, 0x00);
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
            if (ReadByte(_P3_RecoilStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P3_CtmRecoil, 1);
                WriteByte(_P3_RecoilStatus_CaveAddress, 0x00);
            }
            if (ReadByte(_P4_RecoilStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P3_CtmRecoil, 1);
                WriteByte(_P4_RecoilStatus_CaveAddress, 0x00);
            }

            //Life if player is InGame
            //Player Status :
            //[0] : Inactive
            //[1] : In-Game
            //[2] : Continue Screen
            //[3] : Game Over
            int P1_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00B16A98);
            int P2_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00B16AA0);
            int P3_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00B16AA8);
            int P4_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00B16AB0);

            if (P1_Status == 1)
                SetOutputValue(OutputId.P1_Life, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00B16E60));
            else
                SetOutputValue(OutputId.P1_Life, 0);

            if (P2_Status == 1)
                SetOutputValue(OutputId.P2_Life, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00B16E68));
            else
                SetOutputValue(OutputId.P2_Life, 0);

            if (P3_Status == 1)
                SetOutputValue(OutputId.P3_Life, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00B16E70));
            else
                SetOutputValue(OutputId.P3_Life, 0);

            if (P4_Status == 1)
                SetOutputValue(OutputId.P4_Life, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00B16E78));
            else
                SetOutputValue(OutputId.P4_Life, 0);

            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00B3B7C8));
        }

        #endregion
    }
}
