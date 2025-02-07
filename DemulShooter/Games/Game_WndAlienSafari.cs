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
    class Game_WndAlienSafari : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\windows\ads";

        //Memory locations
        private InjectionStruct _Axis_InjectionStruct = new InjectionStruct(0x0004C0BB, 6); //also 44C0FD ?
        private InjectionStruct _Buttons_InjectionStruct = new InjectionStruct(0x00017AE0A, 8);
        private InjectionStruct _IsPlaying_InjectionStruct = new InjectionStruct(0x000502BE, 6);
        private InjectionStruct _StartLevel_InjectionStruct = new InjectionStruct(0x00188E93, 5);
        private InjectionStruct _EndLevel_InjectionStruct = new InjectionStruct(0x00188EC8, 7);
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x000502FD, 6);
        private InjectionStruct _NoCrosshair_InjectionStruct = new InjectionStruct(0x00015F71, 6);

        //Custom values
        private UInt32 _P1_X_CaveAddress = 0;
        private UInt32 _P1_Y_CaveAddress = 0;
        private UInt32 _P1_Trigger_CaveAddress = 0;
        private UInt32 _P1_TriggerEventCountdown_CaveAddress = 0;
        private UInt32 _P1_Reload_CaveAddress = 0;
        private UInt32 _P1_ReloadEventCountdown_CaveAddress = 0;
        private UInt32 _FloatZero_CaveAddress = 0;
        private UInt32 _FloatOne_CaveAddress = 0;

        //Outputs custom values
        private UInt32 _PlayerIsPlaying_CaveAddress = 0;
        private UInt32 _WeaponMaxAmmo_CaveAddress = 0;
        private UInt32 _WeaponCurrentAmmo_CaveAddress = 0;
        private UInt32 _WeaponCurrentAmmo2_CaveAddress = 0;
        private int _P1_WeaponMaxAmmo_Last = 0;  //Use to check if weapon has changed, to prevent triggering recoil when switching for a lower-munition weapon

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndAlienSafari(String RomName)
            : base(RomName, "Alien")
        {
            _KnownMd5Prints.Add("Alien 1.0.0.1 - ToEng", "8b9db55dd8bf8af653f30fc0301cad6c");
            _KnownMd5Prints.Add("Alien 1.0.0.1 - Original", "2618455f69182c9c47e2d8a959cd00e9");

            _tProcess.Start();
            Logger.WriteLog("Waiting for Windows Game " + _RomName + " game to hook.....");
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

                    //X => 800
                    //Y => 600
                    double dMaxX = 800.0;
                    double dMaxY = 600.0;

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
            _P1_Trigger_CaveAddress = _InputsDatabank_Address + 0x08;
            _P1_TriggerEventCountdown_CaveAddress = _InputsDatabank_Address + 0x09;
            _P1_Reload_CaveAddress = _InputsDatabank_Address + 0x0C;
            _P1_ReloadEventCountdown_CaveAddress = _InputsDatabank_Address + 0x0D;
            _FloatZero_CaveAddress = _InputsDatabank_Address + 0x20;
            WriteBytes(_FloatZero_CaveAddress, BitConverter.GetBytes(0.0f));
            _FloatOne_CaveAddress = _InputsDatabank_Address + 0x24;
            WriteBytes(_FloatOne_CaveAddress, BitConverter.GetBytes(1.0f));

            SetHack_Axis();
            SetHack_Buttons();

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }              

        private void SetHack_Axis()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //fstp dword ptr [eax+000000C0]
            CaveMemory.Write_StrBytes("D9 98 C0 00 00 00");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx, [_P1_X_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_X_CaveAddress));
            //mov [eax+BC], ebx
            CaveMemory.Write_StrBytes("89 98 BC 00 00 00");
            //mov ebx, [_P1_Y_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Y_CaveAddress));
            //mov [eax+C0], ebx
            CaveMemory.Write_StrBytes("89 98 C0 00 00 00");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");

            //Inject it
            CaveMemory.InjectToOffset(_Axis_InjectionStruct, "AxisY");
        }

        /// <summary>
        /// The game is using 1 procedure to call for button state, based on a parameter index
        /// Result is a float value, thresold is 0.5f
        /// So it's safe to reply 0 for not presses and 0x3F80 (1.0f) for pressed
        /// 15 = Axis X (relative movement)     
        /// 16 = Axis Y (relative movement)     Both called by the previously done hack IN-GAME (move a bit in menus ?)
        /// 
        /// 5 = Trigger (EventDown)
        /// 8 = Trigger (Down)
        /// 7 = Reload (Eventdown)
        /// 
        /// 6,9 = ? called for trigger check also. Maybe a second button ?
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov ecx,[esp+04]
            CaveMemory.Write_StrBytes("8B 4C 24 04");

            //P1 Trigger Down event
            //cmp ecx, 05
            CaveMemory.Write_StrBytes("83 F9 05");
            //jne P1_TriggerDown
            CaveMemory.Write_StrBytes("75 22");
            //cmp byte ptr [_P1_Trigger_CaveAddress],00
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Trigger_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //je Zero
            CaveMemory.Write_StrBytes("74 56");
            //cmp byte ptr [_P1_TriggerEventCountdown_CaveAddress],00
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_TriggerEventCountdown_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //jg 00DE001D
            CaveMemory.Write_StrBytes("7F 02");
            //jmp Zero
            CaveMemory.Write_StrBytes("EB 4B");
            //fld dword ptr [_FloatOne_CaveAddress]
            CaveMemory.Write_StrBytes("D9 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_FloatOne_CaveAddress));
            //shr [_P1_TriggerEventCountdown_CaveAddress],1
            CaveMemory.Write_StrBytes("D1 2D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_TriggerEventCountdown_CaveAddress));
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 43");
            
            //P1_TriggerDown:
            //cmp ecx,08
            CaveMemory.Write_StrBytes("83 F9 08");
            //jne Reload
            CaveMemory.Write_StrBytes("75 11");
            //cmp byte ptr [_P1_Trigger_CaveAddress],00
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Trigger_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //je Zero
            CaveMemory.Write_StrBytes("74 2F");
            //fld dword ptr [_P1_Trigger_CaveAddress]
            CaveMemory.Write_StrBytes("D9 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_FloatOne_CaveAddress));
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 2D");

            //P1 Reload Down event
            //cmp ecx, 07
            CaveMemory.Write_StrBytes("83 F9 07");
            //jne Next
            CaveMemory.Write_StrBytes("75 22");
            //cmp byte ptr [_P1_Reload_CaveAddress],00
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Reload_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //je Zero
            CaveMemory.Write_StrBytes("74 19");
            //cmp byte ptr [_P1_ReloadEventCountdown_CaveAddress],00
            CaveMemory.Write_StrBytes("80 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_ReloadEventCountdown_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //jg 00DE001D
            CaveMemory.Write_StrBytes("7F 02");
            //jmp Zero
            CaveMemory.Write_StrBytes("EB 0E");
            //fld dword ptr [_FloatOne_CaveAddress]
            CaveMemory.Write_StrBytes("D9 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_FloatOne_CaveAddress));
            //shr [_P1_ReloadEventCountdown_CaveAddress],1
            CaveMemory.Write_StrBytes("D1 2D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_ReloadEventCountdown_CaveAddress));
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 06");

            //Next:
            //fld dword ptr [_FloatZero_CaveAddress]
            CaveMemory.Write_StrBytes("D9 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_FloatZero_CaveAddress));

            //Inject it
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Buttons");
        }

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _PlayerIsPlaying_CaveAddress = _OutputsDatabank_Address;
            _WeaponCurrentAmmo_CaveAddress = _OutputsDatabank_Address + 0x04;
            _WeaponCurrentAmmo2_CaveAddress = _OutputsDatabank_Address + 0x08;
            _WeaponMaxAmmo_CaveAddress = _OutputsDatabank_Address + 0x0C;

            SetHack_StartLevel();
            SetHack_UpdateOutputs();
            SetHack_EndLevel();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Intercept Start of level event to start generating recoil/ammo outputs
        /// </summary>
        private void SetHack_StartLevel()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //push eax
            CaveMemory.Write_StrBytes("50");
            //lea edx,[esp+04]
            CaveMemory.Write_StrBytes("8D 54 24 04");
            //mov [_PlayerIsPlaying_CaveAddress],1
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_PlayerIsPlaying_CaveAddress));
            CaveMemory.Write_StrBytes("01");

            //Inject it
            CaveMemory.InjectToOffset(_StartLevel_InjectionStruct, "EndLevel");
        }


        /// <summary>
        /// This procedure is looped when a level is running to check for inputs.
        /// </summary>
        private void SetHack_UpdateOutputs()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //push [ebp-000000A4]
            CaveMemory.Write_StrBytes("FF B5 5C FF FF FF");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx,[eax+34]
            CaveMemory.Write_StrBytes("8B 58 34");
            //mov [_WeaponCurrentAmmo_CaveAddress],ebx
            CaveMemory.Write_StrBytes("89 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_WeaponCurrentAmmo_CaveAddress));
            //mov ebx,[eax+38]
            CaveMemory.Write_StrBytes("8B 58 38");
            //mov [_WeaponMaxAmmo_CaveAddress],ebx
            CaveMemory.Write_StrBytes("89 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_WeaponMaxAmmo_CaveAddress));
            //mov ebx,[eax+70]
            CaveMemory.Write_StrBytes("8B 58 70");
            //mov [_WeaponCurrentAmmo_CaveAddress],ebx
            CaveMemory.Write_StrBytes("89 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_WeaponCurrentAmmo2_CaveAddress));
            //pop ebx
            CaveMemory.Write_StrBytes("5B");

            //Inject it
            CaveMemory.InjectToOffset(_IsPlaying_InjectionStruct, "IsPlaying");
        }

        /// <summary>
        /// Intercept End of level event to stop generating recoil/ammo outputs
        /// </summary>
        private void SetHack_EndLevel()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov esi,[esp+6C]
            CaveMemory.Write_StrBytes("8B 74 24 6C");
            //mov mov eax,[esi+28]
            CaveMemory.Write_StrBytes("8B 46 28");
            //mov [_PlayerIsPlaying_CaveAddress],0
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_PlayerIsPlaying_CaveAddress));
            CaveMemory.Write_StrBytes("00");

            //Inject it
            CaveMemory.InjectToOffset(_EndLevel_InjectionStruct, "EndLevel");
        }

        protected override void Apply_NoCrosshairMemoryHack()
        {     
            //If level is already running, force crosshair to 0
            UInt32 CursorAddress = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + 0x31FCC4);
            if (CursorAddress != 0)
                WriteByte(CursorAddress + 0xD4, 0);


            //Force the game to believe cursor is disabled, to not render the drawing
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov mov eax,[ebx+000000D4]
            CaveMemory.Write_StrBytes("8B 83 D4 00 00 00");
            //cmp dword ptr [_PlayerIsPlaying_CaveAddress],00
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_PlayerIsPlaying_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //je OriginalCode
            CaveMemory.Write_StrBytes("74 02");
            //xor ecx, ecx
            CaveMemory.Write_StrBytes("31 C0");

            //Inject it
            CaveMemory.InjectToOffset(_NoCrosshair_InjectionStruct, "NoCrosshair");
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>   
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((float)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((float)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes(_P1_X_CaveAddress, bufferX);
                WriteBytes(_P1_Y_CaveAddress, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    WriteByte(_P1_Trigger_CaveAddress, 0x01);
                    WriteByte(_P1_TriggerEventCountdown_CaveAddress, 0x02);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    WriteByte(_P1_Trigger_CaveAddress, 0x00);
                    WriteByte(_P1_TriggerEventCountdown_CaveAddress, 0x00);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    WriteByte(_P1_Reload_CaveAddress, 0x01);
                    WriteByte(_P1_ReloadEventCountdown_CaveAddress, 0x02);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    WriteByte(_P1_Reload_CaveAddress, 0x00);
                    WriteByte(_P1_ReloadEventCountdown_CaveAddress, 0x00);
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            int MotorState = 0;
            int P1_WeaponMaxAmmo = 0;
            _P1_Ammo = 0;
            int P1_Clip = 0;

            int PlayerIsPlaying = ReadByte((_PlayerIsPlaying_CaveAddress));

            if (PlayerIsPlaying == 1)
            {
                _P1_Ammo = BitConverter.ToInt32(ReadBytes(_WeaponCurrentAmmo_CaveAddress, 4), 0);
                if (_P1_Ammo < 0)
                    _P1_Ammo = 0;

                if (_P1_Ammo > 0)
                    P1_Clip = 1;

                //Max Ammo is 0 ==> Laser guns, activate rumble when trigger is pressed
                P1_WeaponMaxAmmo = BitConverter.ToInt32(ReadBytes(_WeaponMaxAmmo_CaveAddress, 4), 0);                
                if (P1_WeaponMaxAmmo == 0)
                {
                    MotorState = ReadByte(_P1_Trigger_CaveAddress);
                    //Those weapons have different type of ammo counter
                    float fAmmo = BitConverter.ToSingle(ReadBytes(_WeaponCurrentAmmo2_CaveAddress, 4), 0);
                    _P1_Ammo = (int)fAmmo;
                }
                else
                {
                    //Bullets-firing gun : activate recoil when a munition is fired
                    if (P1_WeaponMaxAmmo == _P1_WeaponMaxAmmo_Last && _P1_Ammo < _P1_LastAmmo)
                    {
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);                        
                    }
                }
            }  

            SetOutputValue(OutputId.P1_GunMotor, MotorState);
            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);

            _P1_WeaponMaxAmmo_Last = P1_WeaponMaxAmmo;
            _P1_LastAmmo = _P1_Ammo;
        }

        #endregion

    }
}
