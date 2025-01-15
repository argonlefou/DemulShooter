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
    class Game_RwSGG : Game
    {
        private const string GAMEDATA_FOLDER = @"MemoryData\ringwide\sgg";

        /*** MEMORY ADDRESSES **/
        private UInt32 _P1_X_Offset = 0x00478F9D;
        private UInt32 _P1_Y_Offset = 0x00478FA3;
        private UInt32 _P2_X_Offset = 0x00478FA9;
        private UInt32 _P2_Y_Offset = 0x00478FAF;
        private NopStruct _Nop_Axis = new NopStruct(0x0018B0B9, 3);
        private UInt32 _Buttons_Injection_Offset = 0x0018B031;
        private UInt32 _Buttons_Injection_Return_Offset = 0x0018B036;
        private UInt32 _GameTestMenuSettings_Offsets = 0x00681D38;

        private UInt32 _P1_Buttons_CaveAddress;
        private UInt32 _P2_Buttons_CaveAddress;

        //Those 3 are used to store :
        // - The number of credit added since the game has started,
        // - The max value of credits we can add in a game
        // - The max value of credits at a given time during the game
        //These settings are hardcoded (MemoryBaseAddress + 0x000602E9, MemoryBaseAddress + 0x000602EB)
        //and can be changed by hex-editing the binary.
        //In order to let binaries as untouched as possible, DemulShooter will use one of the Codecave to reset the value stored in memory
        private UInt32 _HardcodedNumberOfCredits_SinceBeginning_Offset = 0x0065C60F;
        //private UInt32 _HardcoredMaxNumberOfCreditsToAdd = 0x0065C608;
        private UInt32 _HardcodedMaxNumberOfCredits = 0x0065C609;
        
        //Outputs
        private UInt32 _OutputsPtr_Offset = 0x027034A8;
        private UInt32 _Outputs_Address = 0;
        private UInt32 _GameStatusPtr_Offset = 0x027034BC;
        private UInt32 _Credits_Offset = 0x0065C410;
        private UInt32 _PlayersPtr_Offset = 0x027034E4;
        private UInt32 _P1_CustomRecoil_Address = 0;
        private UInt32 _P2_CustomRecoil_Address = 0;

        private UInt32 _P1_Recoil_Injection_Offset = 0x00075B60;
        private UInt32 _P1_Recoil_Injection_Return_Offset = 0x00075B65;
        private UInt32 _P2_Recoil_Injection_Offset = 0x00075B82;
        private UInt32 _P2_Recoil_Injection_Return_Offset = 0x00075B87;

        private Timer _Tmr_NoAutofireP1;
        private Timer _Tmr_NoAutofireP2;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_RwSGG(String RomName)
            : base(RomName, "RingGunR_RingWide")
        {
            if (_NoAutoFire)
            {
                _Tmr_NoAutofireP1 = new Timer();
                _Tmr_NoAutofireP1.Interval = 20;
                _Tmr_NoAutofireP1.Enabled = true;
                _Tmr_NoAutofireP1.Tick +=new EventHandler(_Tmr_NoAutofireP1_Tick);

                _Tmr_NoAutofireP2 = new Timer();
                _Tmr_NoAutofireP2.Interval = 20;
                _Tmr_NoAutofireP2.Enabled = true;
                _Tmr_NoAutofireP2.Tick += new EventHandler(_Tmr_NoAutofireP2_Tick);
            }
            _KnownMd5Prints.Add("GoldenGun - For TeknoParrot", "9a94458ca852b8b33d8b17b2cfdd663d");
            _KnownMd5Prints.Add("GoldenGun - For JConfig", "338efedcffdfead481dc5ff51d25f570");
            _tProcess.Start();

            Logger.WriteLog("Waiting for RingWide " + _RomName + " game to hook.....");
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
                            //Wait for game to load settings
                            if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _GameTestMenuSettings_Offsets) != 0)
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                //Force Calibration values to get fixed and maximum range
                                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _GameTestMenuSettings_Offsets + 0x44, new byte[] { 0x00, 0x06, 0x00, 0x0A });
                                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _GameTestMenuSettings_Offsets + 0x4C, new byte[] { 0x00, 0xFA, 0x00, 0xF6 });
                                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _GameTestMenuSettings_Offsets + 0x54, new byte[] { 0x00, 0x06, 0x00, 0x0A });
                                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _GameTestMenuSettings_Offsets + 0x5C, new byte[] { 0x00, 0xFA, 0x00, 0xF6 });

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
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Error trying to hook " + _Target_Process_Name + ".exe");
                    Logger.WriteLog(ex.Message.ToString());
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

                    //We can't access the TEST menu to do calibration
                    //Choosen solution is to force Calibration Values for Min-Max axis to [0x00-0xFF] when we write axis values in memory
                    //So we can safely use full range of values now :
                    double dMaxX = 255.0;
                    double dMaxY = 255.0; 

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

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _P2_Buttons_CaveAddress = _InputsDatabank_Address;
            _P1_Buttons_CaveAddress = _InputsDatabank_Address + 0x04;

            SetHack_Axis();
            SetHack_Buttons();

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }
                
        private void SetHack_Axis()
        {
            //NOPing Axis proc
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Axis);

            //Centering Crosshair at start
            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, 0x80);
            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, 0x80);
            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, 0x80);
            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, 0x80);
        }

        /// <summary>
        /// START, SERVICE and TRIGGER/RELOAD are on the same byte
        /// This hack will just block update of the concerned bits so that we can injct our own value
        /// That way, other buttons will work as usual
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //This is not related to input hack, this is to remove the credits limitation :
            //mov byte ptr[_HardcodedNumberOfCredits_SinceBeginning], 0
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _HardcodedNumberOfCredits_SinceBeginning_Offset));
            CaveMemory.Write_StrBytes("00");
            //mov byte ptr[_HardcodedMaxNumberOfCredits], 0
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _HardcodedMaxNumberOfCredits));
            CaveMemory.Write_StrBytes("FF");

            List<Byte> Buffer = new List<Byte>();
            //mov dl, [esp+esi+14]
            CaveMemory.Write_StrBytes("8A 54 34 14");
            //cmp esi, 0
            CaveMemory.Write_StrBytes("83 FE 00");
            //jne exit code
            CaveMemory.Write_StrBytes("0F 85 0F 00 00 00");
            //shl ebx, 1
            CaveMemory.Write_StrBytes("D1 E3");
            //add ebx, _P2_Buttons_CaveAddress
            byte[] b = BitConverter.GetBytes(_P2_Buttons_CaveAddress);
            CaveMemory.Write_StrBytes("81 C3");
            CaveMemory.Write_Bytes(b);
            //mov ebx, [ebx]
            CaveMemory.Write_StrBytes("8B 1B");
            //and dl, 0xFC
            CaveMemory.Write_StrBytes("80 E2 FC");
            //or dl, bl
            CaveMemory.Write_StrBytes("08 DA");
            //Exit:
            //mov bl, al
            CaveMemory.Write_StrBytes("88 C3");
            //Jump back
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Return_Offset);

            Logger.WriteLog("Adding CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code Injection
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

        /// <summary>
        /// HAcks for recoil output :
        /// Data is available on the Output offset but the game is resetting the value too quickly for DemulShooter to get it everytime
        /// So we are using a custom flag in memory, and will put it to 0 or 1 when the game is changing original recoil state.
        /// </summary>
        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_CustomRecoil_Address = _OutputsDatabank_Address;
            _P2_CustomRecoil_Address = _OutputsDatabank_Address + 0x04;

            SetHack_Recoil_P1();
            SetHack_Recoil_P2();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }
        
        private void SetHack_Recoil_P1()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //xor ecx,ecx
            CaveMemory.Write_StrBytes("31 C9");
            //or ebx,40
            CaveMemory.Write_StrBytes("83 CB 40");
            //mov [_P1_CustomRecoil_Address], 1
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_CustomRecoil_Address));
            CaveMemory.Write_StrBytes("01 00 00 00");
            //Jump back
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Recoil_Injection_Return_Offset);

            Logger.WriteLog("Adding P1 Recoil output CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code Injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Recoil_Injection_Offset) - 5;
            List<Byte> Buffer = new List<Byte>();            
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Recoil_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        private void SetHack_Recoil_P2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov ecx, 1
            CaveMemory.Write_StrBytes("B9 01 00 00 00");
            //mov [_P2_CustomRecoil_Address], 1
            CaveMemory.Write_StrBytes("C7 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_CustomRecoil_Address));
            CaveMemory.Write_StrBytes("01 00 00 00");
            //Jump back
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Recoil_Injection_Return_Offset);

            Logger.WriteLog("Adding P2 Recoil output CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code Injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Recoil_Injection_Offset) - 5;
            List<Byte> Buffer = new List<Byte>();            
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
            byte[] bufferX = BitConverter.GetBytes((UInt16)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((UInt16)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX[0]);
                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY[0]);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    Apply_OR_ByteMask(_P1_Buttons_CaveAddress, 0x02);
                    //Force single shot instead of Auto-fire mode
                    if (_NoAutoFire)
                        _Tmr_NoAutofireP1.Start();
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress, 0xFD);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask(_P1_Buttons_CaveAddress, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress, 0xFD);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_P1_Buttons_CaveAddress, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress, 0xFE);
            }
            else if (PlayerData.ID == 2)
            {
                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX[0]);
                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY[0]);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress, 0x02);
                    //Force single shot instead of Auto-fire mode
                    if (_NoAutoFire)
                        _Tmr_NoAutofireP2.Start();
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress, 0xFD);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress, 0xFD);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress, 0xFE);     
            }
        }

        private void _Tmr_NoAutofireP1_Tick(Object Sender, EventArgs e)
        {
            Apply_AND_ByteMask(_P1_Buttons_CaveAddress, 0xFD);
            _Tmr_NoAutofireP1.Stop();
        }

        private void _Tmr_NoAutofireP2_Tick(Object Sender, EventArgs e)
        {
            Apply_AND_ByteMask(_P2_Buttons_CaveAddress, 0xFD);
            _Tmr_NoAutofireP2.Stop();
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpPanel, OutputId.P1_LmpPanel));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpPanel, OutputId.P2_LmpPanel));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunRecoil, OutputId.P2_GunRecoil));
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
            _Outputs_Address = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _OutputsPtr_Offset) + 0x08;
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Outputs_Address) >> 4 & 0x01);
            //According to the manual, those 2 Outputs are mulicolor LEDs drawing a "X" or a "+" on the main panel
            //Hard to get them exactly without a GameTest binary
            SetOutputValue(OutputId.P1_LmpPanel, 0);
            SetOutputValue(OutputId.P2_LmpPanel, 0);
            SetOutputValue(OutputId.P1_GunRecoil, ReadByte(_Outputs_Address) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, ReadByte(_Outputs_Address) >> 3 & 0x01);

            int P1_Clip = 0;
            int P2_Clip = 0;

            //Game Status :
            //2,3,4: Logos at start
            //5: Attract video
            //6: Title Menu
            //9: Demo instructions
            //10: Attract gameplay video
            //12: Playing
            //11: Ranking
            UInt32 GameStatus = ReadByte(ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _GameStatusPtr_Offset) + 0x34);
            if (GameStatus == 12)
            {
                UInt32 PlayersPtr_BaseAddress = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersPtr_Offset);
                if (PlayersPtr_BaseAddress != 0)
                {
                    UInt32 P1_StructAddress = ReadPtr(PlayersPtr_BaseAddress + 0x34);
                    UInt32 P2_StructAddress = ReadPtr(PlayersPtr_BaseAddress + 0x38);

                    //Player Status:
                    //0: Not Playing
                    //3: InGame
                    //4: CutScene
                    //5: Bonus select
                    //12/14: Continu Screen
                    int P1_Status = ReadByte(P1_StructAddress + 0x38);
                    int P2_Status = ReadByte(P2_StructAddress + 0x38);

                    if (P1_Status == 03 || P1_Status == 04)
                    {
                        _P1_Life = ReadByte(P1_StructAddress + 0x3C);
                        _P1_Ammo = ReadByte(P1_StructAddress + 0x38C);

                        //[Clip Empty] custom Output
                        if (_P1_Ammo > 0)
                            P1_Clip = 1;

                        //[Damaged] custom Output                
                        if (_P1_Life < _P1_LastLife)
                            SetOutputValue(OutputId.P1_Damaged, 1);
                    }

                    if (P2_Status == 03 || P2_Status == 04)
                    {
                        _P2_Life = ReadByte(P2_StructAddress + 0x3C);
                        _P2_Ammo = ReadByte(P2_StructAddress + 0x38C);

                        //[Clip Empty] custom Output
                        if (_P2_Ammo > 0)
                            P2_Clip = 1;

                        //[Damaged] custom Output                
                        if (_P2_Life < _P2_LastLife)
                            SetOutputValue(OutputId.P2_Damaged, 1);
                    }
                }
            }


            //Custom Recoil will be read by our own codecave
            if (ReadByte(_P1_CustomRecoil_Address) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                WriteByte(_P1_CustomRecoil_Address, 0);
            }
            if (ReadByte(_P2_CustomRecoil_Address) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte(_P2_CustomRecoil_Address, 0);
            }

            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
