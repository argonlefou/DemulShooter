using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;

namespace DemulShooter
{
    class Game_RwGunman : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\ringwide\mng";

        /*** MEMORY ADDRESSES **/
        private UInt32 _Credits_Offset = 0x006CBB40;
        private UInt32 _pGunCylinderMgr_Offset = 0x006C9B30;
        private InjectionStruct _Axis_InjectionStruct = new InjectionStruct(0x00142447, 9);
        private InjectionStruct _Lamp_InjectionStruct = new InjectionStruct(0x001DC8C1, 5);
        private UInt32 _P1_TriggerPatch_Offset = 0x0034A1DD;
        private UInt32 _P1_OtherClickPatch_Offset = 0x0034A21E; //equivalent of right-click, used in TEST menu only to navigate
        private UInt32 _P1_ReloadPatch_Offset = 0x0034A25F;
        private InjectionStruct _P2_Buttons_InjectionStruct = new InjectionStruct(0x349622, 6);
        private InjectionStruct _P1_NoCrosshair_InjectionStruct = new InjectionStruct(0x0014386C, 5);
        private InjectionStruct _P2_NoCrosshair_InjectionStruct = new InjectionStruct(0x001436C6, 5);

        //Custom Input Address
        private UInt32 _P1_X_Address;
        private UInt32 _P1_Y_Address;
        private UInt32 _P2_X_Address;
        private UInt32 _P2_Y_Address;
        private UInt32 _P1_Trigger_Address;
        private UInt32 _P1_Reload_Address;
        private UInt32 _P1_Other_Address;
        private UInt32 _P2_Trigger_Address;
        private UInt32 _P2_Reload_Address;
        private UInt32 _P2_Other_Address;

        //custom Outputs Address
        private UInt32 _P1_CustomRecoil_CaveAddress;
        private UInt32 _P2_CustomRecoil_CaveAddress;
        private UInt32 _CustomLamps_CaveAddress;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_RwGunman(String RomName, bool HideGameCrosshair, bool DisableInputHack, bool Verbose)
            : base(RomName, "gunman_dxF", DisableInputHack, Verbose)
        {
            _HideCrosshair = HideGameCrosshair;

            //_KnownMd5Prints.Add("gunman_dxD.exe - build 8796", "304bdb086204d6fa53eb65ad7073a2e0");   // Different code !
            _KnownMd5Prints.Add("gunman_dxF.exe - build 8796", "ffd6e3c06a2bf4a0abbe0961589432cc");
            _KnownMd5Prints.Add("gunman_dxR.exe - build 8796", "12666f3917779bf2bda624224a2dd346");
            _tProcess.Start();

            Logger.WriteLog("Waiting for RingWide " + _RomName + " game to hook.....");
        }

        /// <summary>
        /// Timer event when looking for Demul Process (auto-Hook and auto-close)
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
                            ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
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

                    double dMaxX = 1023.0;
                    double dMaxY = 767.0;

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

        /// <summary>
        /// Genuine Hack, just blocking Axis and Triggers input to replace them
        /// Reverse back to it when DumbJVSCommand will be working with ParrotLoader, without DumbJVSManager
        /// </summary>
        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _P1_X_Address = _InputsDatabank_Address;
            _P1_Y_Address = _InputsDatabank_Address + 0x04;
            _P2_X_Address = _InputsDatabank_Address + 0x08;
            _P2_Y_Address = _InputsDatabank_Address + 0x0C;
            _P1_Trigger_Address = _InputsDatabank_Address + 0x10;
            _P1_Reload_Address = _InputsDatabank_Address + 0x11;
            _P1_Other_Address = _InputsDatabank_Address + 0x12;
            _P2_Trigger_Address = _InputsDatabank_Address + 0x14;
            _P2_Reload_Address = _InputsDatabank_Address + 0x15;
            _P2_Other_Address = _InputsDatabank_Address + 0x16;

            SetHack_Axis();
            SetHack_Buttons();

            Logger.WriteLog("Inputs memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Game is using mouse coordinates to set P1 values
        /// And also for P2 if a command line option is set, or otherwise uses some other data
        /// Changing the values in memory for both after the whole procedure allow us to set data whatever mode is choosen
        /// </summary>
        private void SetHack_Axis()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov ecx,[ebp-2C]
            CaveMemory.Write_StrBytes("8B 4D D4");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[_P1_X_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_X_Address));
            //mov [ecx+000000A8],eax
            CaveMemory.Write_StrBytes("89 81 A8 00 00 00");
            //mov eax,[_P1_Y_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Y_Address));
            //mov [ecx+000000AC],eax
            CaveMemory.Write_StrBytes("89 81 AC 00 00 00");
            //mov eax,[_P2_X_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_X_Address));
            //mov [ecx+000000B0],eax
            CaveMemory.Write_StrBytes("89 81 B0 00 00 00");
            //mov eax,[_P2_Y_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Y_Address));
            //mov [ecx+000000B4],eax
            CaveMemory.Write_StrBytes("89 81 B4 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //add ecx,000000A8
            CaveMemory.Write_StrBytes("81 C1 A8 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Axis_InjectionStruct, "Axis");
        }

        /// <summary>
        /// Replacing the address of the buttons status loaded in the procedure to get trigger status
        /// by our own address
        /// </summary>
        private void SetHack_Buttons()
        {
            //P1:
            //Replace Trigger flag test with our own value
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerPatch_Offset, new byte[] { 0x0F, 0xB6, 0x15 });
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerPatch_Offset + 3, BitConverter.GetBytes(_P1_Trigger_Address));
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerPatch_Offset + 7, new byte[] { 0x80, 0xE2, 0x80 });

            //Replace Reload flag test with our own value
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_OtherClickPatch_Offset, new byte[] { 0x0F, 0xB6, 0x15 });
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_OtherClickPatch_Offset + 3, BitConverter.GetBytes(_P1_Reload_Address));
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_OtherClickPatch_Offset + 7, new byte[] { 0x80, 0xE2, 0x80 });

            //Replace flag test with our own value
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_ReloadPatch_Offset, new byte[] { 0x0F, 0xB6, 0x15 });
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_ReloadPatch_Offset + 3, BitConverter.GetBytes(_P1_Other_Address));
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_ReloadPatch_Offset + 7, new byte[] { 0x80, 0xE2, 0x80 });

            //P2:
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov eax,[_P2_Trigger_Address]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Trigger_Address));
            //mov [ecx+48],eax
            CaveMemory.Write_StrBytes("89 41 48");
            //mov [ecx+4C],edx
            CaveMemory.Write_StrBytes("89 51 4C");

            //Inject it
            CaveMemory.InjectToOffset(_P2_Buttons_InjectionStruct, "P2 Buttons");
        }

        protected override void Apply_OutputsMemoryHack()
        {
            //Create Databak to store our value
            Create_OutputsDataBank();
            _P1_CustomRecoil_CaveAddress = _OutputsDatabank_Address;
            _P2_CustomRecoil_CaveAddress = _OutputsDatabank_Address + 0x04;
            _CustomLamps_CaveAddress = _OutputsDatabank_Address + 0x10;

            SetHack_Lamp();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Intercepting the call to the Lamp-related functions so that we can read the parameters (Lamp ID and status)
        /// Unfortunatelly, the game does not call that function once in-game (only in test-menu)
        /// </summary>
        private void SetHack_Lamp()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov ebp,[esp+0C]
            CaveMemory.Write_StrBytes("8B 6C 24 0C");
            //add ebp,_CustomLamps_CaveAddress
            CaveMemory.Write_StrBytes("81 C5");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CustomLamps_CaveAddress));
            //mov eax,[esp+10]
            CaveMemory.Write_StrBytes("8B 44 24 10");
            //mov [ebp+00],al
            CaveMemory.Write_StrBytes("88 45 00");
            //mov ebp,esp
            CaveMemory.Write_StrBytes("8B EC");
            //mov eax,[ebp+08]
            CaveMemory.Write_StrBytes("8B 45 08");

            //Inject it
            CaveMemory.InjectToOffset(_Lamp_InjectionStruct, "Lamp");
        }

        /// <summary>
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {
            if (_HideCrosshair)
            {
                SetHack_NoCrosshair(_P1_NoCrosshair_InjectionStruct, "P1 Nocrosshair");
                SetHack_NoCrosshair(_P2_NoCrosshair_InjectionStruct, "P2 Nocrosshair");
            }
        }
        /// <summary>
        /// Forcing float value [-100.0, -100.0] for both player reticle display
        /// </summary>
        private void SetHack_NoCrosshair(InjectionStruct PlayerInjectionStruct, String InjectionName)
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov edx,C2C80000
            CaveMemory.Write_StrBytes("BA 00 00 C8 C2"); 
            //mov eax,C2C80000
            CaveMemory.Write_StrBytes("B8 00 00 C8 C2");

            //Inject it
            CaveMemory.InjectToOffset(PlayerInjectionStruct, InjectionName);
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>  
        public override void SendInput(PlayerSettings PlayerData)
        {
            float x = (float)PlayerData.RIController.Computed_X;

            byte[] bufferX = BitConverter.GetBytes(x);
            byte[] bufferY = BitConverter.GetBytes((float)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes(_P1_X_Address, bufferX);
                WriteBytes(_P1_Y_Address, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)                
                    WriteByte(_P1_Trigger_Address, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte(_P1_Trigger_Address, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    WriteByte(_P1_Reload_Address, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    WriteByte(_P1_Reload_Address, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    WriteByte(_P1_Other_Address, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    WriteByte(_P1_Other_Address, 0x00);
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes(_P2_X_Address, bufferX);
                WriteBytes(_P2_Y_Address, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)                    
                    WriteByte(_P2_Trigger_Address, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte(_P2_Trigger_Address, 0x00);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    WriteByte(_P2_Reload_Address, 0x04);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    WriteByte(_P2_Reload_Address, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    WriteByte(0x11c0100, 1);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    WriteByte(0x11c0100, 0x00);
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : stays activated when trigger is pulled
            //Gun recoil : not used ??
            _Outputs = new List<GameOutput>();
            /*_Outputs.Add(new GameOutput(OutputDesciption.Lmp_Horn_R, OutputId.Lmp_Horn_R));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_Horn_G, OutputId.Lmp_Horn_G));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_Horn_B, OutputId.Lmp_Horn_B));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_LeftBulletMark, OutputId.Lmp_LeftBulletMark));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_RightBulletMark, OutputId.Lmp_RightBulletMark));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_W, OutputId.Lmp_W));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_A, OutputId.Lmp_A));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_N, OutputId.Lmp_N));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_T, OutputId.Lmp_T));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_E, OutputId.Lmp_E));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_D, OutputId.Lmp_D));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_LeftReload, OutputId.Lmp_LeftReload));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_RightReload, OutputId.Lmp_RightReload));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp_Payout, OutputId.Lmp_Payout));*/
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Clip, OutputId.P2_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Genuine Outputs
            /*SetOutputValue(OutputId.Lmp_Horn_R, ReadByte(_CustomLamps_CaveAddress + 0x3));
            SetOutputValue(OutputId.Lmp_Horn_G, ReadByte(_CustomLamps_CaveAddress + 0x4));
            SetOutputValue(OutputId.Lmp_Horn_B, ReadByte(_CustomLamps_CaveAddress + 0x5));
            SetOutputValue(OutputId.Lmp_LeftBulletMark, ReadByte(_CustomLamps_CaveAddress + 0x8));
            SetOutputValue(OutputId.Lmp_RightBulletMark, ReadByte(_CustomLamps_CaveAddress + 0x9));
            SetOutputValue(OutputId.Lmp_W, ReadByte(_CustomLamps_CaveAddress + 0xA));
            SetOutputValue(OutputId.Lmp_A, ReadByte(_CustomLamps_CaveAddress + 0xB));
            SetOutputValue(OutputId.Lmp_N, ReadByte(_CustomLamps_CaveAddress + 0xC));
            SetOutputValue(OutputId.Lmp_T, ReadByte(_CustomLamps_CaveAddress + 0xD));
            SetOutputValue(OutputId.Lmp_E, ReadByte(_CustomLamps_CaveAddress + 0xE));
            SetOutputValue(OutputId.Lmp_D, ReadByte(_CustomLamps_CaveAddress + 0xF));
            SetOutputValue(OutputId.Lmp_LeftReload, ReadByte(_CustomLamps_CaveAddress + 0x0));
            SetOutputValue(OutputId.Lmp_RightReload, ReadByte(_CustomLamps_CaveAddress + 0x1));
            SetOutputValue(OutputId.Lmp_Payout, ReadByte(_CustomLamps_CaveAddress + 0x2));*/

            //Custom Outputs
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;
            int Credits = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset, 4), 0);

            //Credits are needed to play (=reload bullets) so it can be used as a filter, as there is no "life"
            if (Credits > 0)
            {
                _P1_Ammo = ReadByte(ReadPtrChain((UInt32)_TargetProcess_MemoryBaseAddress + _pGunCylinderMgr_Offset, new UInt32[] { 0x34 }) + 0x1C8);                
                //Custom Recoil
                if (_P1_Ammo < _P1_LastAmmo)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Clip Empty] custom Output
                if (_P1_Ammo > 0)
                    P1_Clip = 1;

                _P2_Ammo = ReadByte(ReadPtrChain((UInt32)_TargetProcess_MemoryBaseAddress + _pGunCylinderMgr_Offset, new UInt32[] { 0x38 }) + 0x1C8);
                //Custom Recoil
                if (_P2_Ammo < _P2_LastAmmo)
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                //[Clip Empty] custom Output
                if (_P2_Ammo > 0)
                    P2_Clip = 1;
            }

            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.Credits, Credits);
        }

        #endregion
    }
}
