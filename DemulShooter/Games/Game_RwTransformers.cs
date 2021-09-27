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
    class Game_RwTransformers : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\ringwide\tha";

        /*** MEMORY ADDRESSES **/
        private UInt32 _P1_X_Offset = 0x0098CCBB;
        private UInt32 _P1_Y_Offset = 0x0098CCB5;
        private UInt32 _P1_Buttons_Offset = 0x0098CC9C;
        private UInt32 _P2_X_Offset = 0x0098CCC7;
        private UInt32 _P2_Y_Offset = 0x0098CCC1;
        private UInt32 _P2_Buttons_Offset = 0x0098CCA8;
        private UInt32 _Buttons_Injection_Offset = 0x00419FDB;
        private UInt32 _Buttons_Injection_Return_Offset = 0x00419FE0;
        private NopStruct _Nop_Axis_1 = new NopStruct(0x0041A059, 3);
        private NopStruct _Nop_Axis_2 = new NopStruct(0x0041A05F, 4);
        private UInt32 _GameTestMenuSettings_Offsets = 0x009BB838;
        
        //For custom recoil output
        private UInt32 _P1_RecoilState_Address = 0;
        private UInt32 _P2_RecoilState_Address = 0;
        private UInt32 _P1_Recoil_Injection_Offset = 0x0002D518;
        private UInt32 _P1_Recoil_Injection_Return_Offset = 0x0002D51D;
        private UInt32 _P2_Recoil_Injection_Offset = 0x0002D548;
        private UInt32 _P2_Recoil_Injection_Return_Offset = 0x0002D54D;

        //Those 3 are used to store :
        // - The number of credit added since the game has started,
        // - The max value of credits we can add in a game
        // - The max value of credits at a given time during the game
        //These settings are hardcoded (MemoryBaseAddress + 0x000602E9, MemoryBaseAddress + 0x000602EB)
        //and can be changed by hex-editing the binary.
        //In order to let binaries as untouched as possible, DemulShooter will use one of the Codecave to reset the value stored in memory
        private UInt32 _HardcodedNumberOfCredits_SinceBeginning_Offset = 0x009650FB;
        //private UInt32 _HardcoredMaxNumberOfCreditsToAdd = 0x009650F5;
        private UInt32 _HardcodedMaxNumberOfCredits = 0x009650F4;

        //Outputs
        private UInt32 _OutputsPtr_Offset = 0x1A3CBA0;
        private UInt32 _Outputs_Address = 0;
        private UInt32 _Credits_Offset = 0x00964E60;
        private UInt32 _PlayersPtr_Offset = 0x1A3CC1C; 
        private int _P1_LastLife = 0;
        private int _P2_LastLife = 0;
        private int _P1_Life = 0;
        private int _P2_Life = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_RwTransformers(String RomName, double _ForcedXratio, bool Verbose)
            : base(RomName, "TF_Gun_R_Ring_dumped", _ForcedXratio, Verbose)
        {
            _KnownMd5Prints.Add("Transformers Final  - For TeknoParrot", "7e11f7e78ed566a277edba1a8aab0749");
            _KnownMd5Prints.Add("Transformers Final  - For JConfig", "0d23fead523ea91eaea5047e652dff69");
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
                            //Wait for game to load settings
                            if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _GameTestMenuSettings_Offsets) != 0)
                            {
                                //Force Calibration values
                                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _GameTestMenuSettings_Offsets + 0x58, new byte[] { 0x00, 0xFF, 0x00, 0xFF });
                                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _GameTestMenuSettings_Offsets + 0x60, new byte[] { 0x00, 0xFF, 0x00, 0xFF });

                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                CheckExeMd5();
                                ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                                SetHack();
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
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

                    //Player 1 and 2 axis limits are different, and we can't access the TEST menu to do calibration
                    //Choosen solution is to force Calibration Values for Min-Max axis to [0x00-0xFF] when we write axis values in memory
                    //So we can safely use full range of values now :
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;                   

                    //Inverted Axis : 0 = bottom right
                    PlayerData.RIController.Computed_X = Convert.ToInt32(dMaxX - Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt32(dMaxY - Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));
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
        /// Genuine Hack, just blocking Axis and filtering Triggers input to replace them without blocking other input
        /// </summary>
        private void SetHack()
        {
            //NOPing axis proc
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Axis_1);
            SetHackInput();
            CreateDataBank();
            SetHack_RecoilP1();
            SetHack_RecoilP2();
                        
            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// This will be used to store custom recoil values, updated by the program code each time it will check for rumble (for each bullet fired)
        /// </summary>
        private void CreateDataBank()
        {
            Codecave InputMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            InputMemory.Open();
            InputMemory.Alloc(0x800);
            _P1_RecoilState_Address = InputMemory.CaveAddress;
            _P2_RecoilState_Address = InputMemory.CaveAddress + 0x04;
            Logger.WriteLog("Custom OutputRecoil data will be stored at : 0x" + _P1_RecoilState_Address.ToString("X8"));
        }

        /// <summary>
        ///Hacking buttons proc : 
        ///Same byte is used for both triggers, start and service (for each player)
        ///0b10000000 is start
        ///0b01000000 is Px Service
        ///0b00000001 is TriggerL
        ///0b00000010 is TriggerR
        ///So we need to make a mask to accept Start button moodification and block other so we can inject   
        /// </summary>>
        private void SetHackInput()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //This is not related to input hack, this is to remove the credits limitation :
            //mov byte ptr[_HardcodedNumberOfCredits_SinceBeginning], 0
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _HardcodedNumberOfCredits_SinceBeginning_Offset));
            CaveMemory.Write_StrBytes("00");
            //mov byte ptr[_HardcodedMaxNumberOfCredits], 0
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _HardcodedMaxNumberOfCredits));
            CaveMemory.Write_StrBytes("FF");

            //Start of input hack :
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov al,[ecx-01]
            CaveMemory.Write_StrBytes("8A 41 FF");
            //and al,03
            CaveMemory.Write_StrBytes("24 03");
            //and dl,C0
            CaveMemory.Write_StrBytes("80 E2 C0");
            //add dl,al
            CaveMemory.Write_StrBytes("00 C2");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //mov [ecx-01],dl
            CaveMemory.Write_StrBytes("88 51 FF");
            //not dl
            CaveMemory.Write_StrBytes("F6 D2");
            //Jump back
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Return_Offset);

            Logger.WriteLog("Adding CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

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
        /// The game is checking if Recoil is enabled for each bullet fired.
        /// Using this request call, we can generate the start of our own CustomRecoil output event
        /// </summary>
        private void SetHack_RecoilP1()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov byte ptr[_P1_RecoilState_Address], 1
            CaveMemory.Write_StrBytes("C6 05");
            byte[] b = BitConverter.GetBytes(_P1_RecoilState_Address);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("01");
            //mov eax,[ecx+34]
            CaveMemory.Write_StrBytes("8B 41 34");
            //test eax, eax
            CaveMemory.Write_StrBytes("85 C0");            
            //Jump back
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Recoil_Injection_Return_Offset);

            Logger.WriteLog("Adding P1_Recoil CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Recoil_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Recoil_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
       
        }
        private void SetHack_RecoilP2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov byte ptr[_P2_RecoilState_Address], 1
            CaveMemory.Write_StrBytes("C6 05");
            byte[] b = BitConverter.GetBytes(_P2_RecoilState_Address);
            CaveMemory.Write_Bytes(b);
            CaveMemory.Write_StrBytes("01");
            //mov eax,[ecx+38]
            CaveMemory.Write_StrBytes("8B 41 38");
            //test eax, eax
            CaveMemory.Write_StrBytes("85 C0");
            //Jump back
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Recoil_Injection_Return_Offset);

            Logger.WriteLog("Adding P2_Recoil CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Recoil_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Recoil_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
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
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFE);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFD);
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset, 0xFE);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Buttons_Offset, 0xFD);
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpGun, OutputId.P1_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpGun, OutputId.P2_LmpGun));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpBillboard, OutputId.LmpBillboard));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpBreak, OutputId.P1_LmpBreak));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpBreak, OutputId.P2_LmpBreak));
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
            _Outputs_Address = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _OutputsPtr_Offset) + 0x08;
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Outputs_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_LmpGun, ReadByte(_Outputs_Address) >> 1 & 0x01);
            SetOutputValue(OutputId.P2_LmpGun, ReadByte(_Outputs_Address) & 0x01);
            SetOutputValue(OutputId.LmpBillboard, ReadByte(_Outputs_Address + 1) >> 2 & 0x01);
            SetOutputValue(OutputId.P1_LmpBreak, ReadByte(_Outputs_Address + 1) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpBreak, ReadByte(_Outputs_Address + 1) >> 6 & 0x01);
            //There are others lamps (Logo, Sides ? RGB Leds ?) coded on [Outputs_Address + 1] 0x20, 0x10, 0x08
            //But without Test menu I can't know how these one are working
            //Besides, lamps name are different on the TestMode binary than on any Arcade Manual I could find...
            SetOutputValue(OutputId.P1_GunMotor, ReadByte(_Outputs_Address) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, ReadByte(_Outputs_Address) >> 3 & 0x01);

            //custom Outputs  
            _P1_Life = 0;
            _P2_Life = 0;
            UInt32 PlayersPtr_BaseAddress = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersPtr_Offset);
            if (PlayersPtr_BaseAddress != 0)
            {
                UInt32 P1_StructAddress = ReadPtr(PlayersPtr_BaseAddress + 0x34);
                UInt32 P2_StructAddress = ReadPtr(PlayersPtr_BaseAddress + 0x38);
                //Player Status :
                //3: Game
                //4: Cutscene
                //15: Contnue
                int P1_Status = ReadByte(P1_StructAddress + 0x5C);
                int P2_Status = ReadByte(P1_StructAddress + 0x5C);
                if (P1_Status == 3 || P1_Status == 4)
                {
                    _P1_Life = ReadByte(P1_StructAddress + 0x60);

                    //[Damaged] custom Output                
                    if (_P1_Life < _P1_LastLife)
                        SetOutputValue(OutputId.P1_Damaged, 1);
                }

                if (P2_Status == 3 || P2_Status == 4)
                {
                    _P2_Life = ReadByte(P2_StructAddress + 0x60);

                    //[Damaged] custom Output                
                    if (_P2_Life < _P2_LastLife)
                        SetOutputValue(OutputId.P2_Damaged, 1);
                }                
            }

            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            //Custom Recoil will simply be read on memory and reset
            //the codecave injected will update it for the "ON" state
            byte P1_RecoilState = ReadByte(_P1_RecoilState_Address);
            SetOutputValue(OutputId.P1_CtmRecoil, P1_RecoilState);
            if (P1_RecoilState == 1)
                WriteByte(_P1_RecoilState_Address, 0x00);
            byte P2_RecoilState = ReadByte(_P2_RecoilState_Address);
            SetOutputValue(OutputId.P2_CtmRecoil, P2_RecoilState);
            if (P2_RecoilState == 1)
                WriteByte(_P2_RecoilState_Address, 0x00);  

            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
