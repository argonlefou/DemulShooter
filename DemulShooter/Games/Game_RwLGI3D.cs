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
using System.Text;

namespace DemulShooter
{
    class Game_RwLGI3D : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\ringwide\lgi3d";

        /*** MEMORY ADDRESSES **/
        private UInt32 _Data_Base_Address;
        private UInt32 _Data_Base_Address_Ptr_Offset = 0x0082E34C;       
        private UInt32 _P1_X_Offset = 0x00000011;
        private UInt32 _P1_Y_Offset = 0x0000000F;
        private UInt32 _P1_Buttons_Offset = 0x00000005;
        private UInt32 _P2_X_Offset = 0x00000015;
        private UInt32 _P2_Y_Offset = 0x00000013;
        private UInt32 _P2_Buttons_Offset = 0x00000009;
        private NopStruct _Nop_Axis = new NopStruct(0x002FE60C, 3);
        private InjectionStruct _Buttons_InjectionStruct = new InjectionStruct(0x002FE592, 8);
        private InjectionStruct _CalibrationValues_InjectionStruct = new InjectionStruct(0x0049DC27, 6);
        private UInt32 _StrlenFunction_Offset = 0x002FF3EC;

        //Outputs
        private UInt32 _OutputsPtr_Offset = 0x0065DA20;
        private UInt32 _PlayersStructPtr_Offset = 0x008429F0;

        //Custom injection
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x003518F1, 5);
        private InjectionStruct _CurrentSequence_InjectionStruct = new InjectionStruct(0x0002615BF, 6);

        //Custom Data
        private UInt32 _P1_CustomRecoil_CaveAddress = 0;
        private UInt32 _P2_CustomRecoil_CaveAddress = 0;
        private UInt32 _CurrentSequenceStringLength = 0;
        private UInt32 _CurrentSequenceString = 0;

        private bool _IsAttractMode = true;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_RwLGI3D(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "LGI", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Let's Go Island - For TeknoParrot", "ef9e3625684e0d52eab5bc1f0c68c7c3");
            _KnownMd5Prints.Add("Let's Go Island - For JConfig", "49c5de5df60f475a965b2d894b3477c6");
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
                            byte[] buffer = ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Data_Base_Address_Ptr_Offset, 4);
                            UInt32 Calc_Addr1 = BitConverter.ToUInt32(buffer, 0);
                            Logger.WriteLog("CalcAddrr1 = 0x" + Calc_Addr1.ToString("X4"));
                            if (Calc_Addr1 != 0)
                            {
                                buffer = ReadBytes(Calc_Addr1, 4);
                                _Data_Base_Address = BitConverter.ToUInt32(buffer, 0);
                                if (_Data_Base_Address != 0)
                                {
                                    _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                    Logger.WriteLog("Data base adddress =  0x" + _Data_Base_Address.ToString("X8"));
                                    CheckExeMd5();
                                    ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                                    Apply_MemoryHacks();
                                    _ProcessHooked = true;
                                    RaiseGameHookedEvent();
                                }
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

                    //We can't access the TEST menu to do calibration
                    //Choosen solution is to force Calibration Values for Min-Max axis to [0x00-0xFF] when we write axis values in memory
                    //So we can safely use full range of values now :                    
                    //Axes inversés : 0 = Bas et Droite
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;

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
        /// Genuine Hack, just blocking Axis and Triggers input to replace them
        /// Reverse back to it when DumbJVSCommand will be working with ParrotLoader, without DumbJVSManager
        /// </summary>
        protected override void Apply_InputsMemoryHack()
        {
            //NOPing axis proc
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Axis);

            SetHack_Buttons();
            SetHack_Calibration();           
            
            Logger.WriteLog("Inputs memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        ///Hacking buttons proc : 
        ///Same byte is used for both triggers, start and service (for each player)
        ///0b10000000 is start
        ///0b01000000 is Px Service
        ///0b00000001 is TriggerL
        ///0b00000010 is TriggerR
        ///So we need to make a mask to accept Start button moodification and block other so we can inject
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //and ecx,00000080
            CaveMemory.Write_StrBytes("81 E1 80 00 00 00");
            //cmp ecx,00
            CaveMemory.Write_StrBytes("83 F9 00");
            //jg @ => if Start PRessed
            CaveMemory.Write_StrBytes("0F 8F 09 00 00 00");
            //and dword ptr [ebx-01],7F => Putting the start bit to 0
            CaveMemory.Write_StrBytes("83 63 FF 7F");
            //jmp @
            CaveMemory.Write_StrBytes("E9 07 00 00 00");
            //or [ebx-01],00000080 ==> start is pressed, putting bit to 1
            CaveMemory.Write_StrBytes("81 4B FF 80 00 00 00");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //and ecx,00000040
            CaveMemory.Write_StrBytes("83 E1 40");
            //cmp ecx,00
            CaveMemory.Write_StrBytes("83 F9 00");
            //jg @ => if Service PRessed
            CaveMemory.Write_StrBytes("0F 8F 0C 00 00 00");
            //and dword ptr [ebx-01],BF => Putting the Service bit to 0
            CaveMemory.Write_StrBytes("81 63 FF BF 00 00 00");
            //jmp @
            CaveMemory.Write_StrBytes("E9 04 00 00 00");
            //or [ebx-01],00000040 ==> Service is pressed, putting bit to 1
            CaveMemory.Write_StrBytes("83 4B FF 40");
            //movzx ecx,byte ptr [esp+13]
            CaveMemory.Write_StrBytes("0F B6 4C 24 13");
            //Jump back
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _Buttons_InjectionStruct.InjectionReturnOffset);

            //Inject it
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Buttons");
        }

        /// <summary>
        ///To bypass the need of Calibration (calculated at each level entry), we can force the game to use max range (0x00-0xFF) for JVS data
        ///The game uses these values as Min values (we can force them to 0):
        ///00C17D29 (p1 min x)
        ///00C17D2D (p1 min y)
        ///00C17D31 (p2 min x)
        ///00C17D35 (p2 min y)
        ///For max value, the game is using complicated FloatingPoint/Double operations with a kind of ratio values.
        ///Forcing the following values enable the use of 0x00-0xFF full range values to match with (0.0 - 1024.0) calculated values.                                         
        /// </summary>
        private void SetHack_Calibration()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //xor edi, edi
            CaveMemory.Write_StrBytes("31 FF");
            //xor ebx,ebx
            CaveMemory.Write_StrBytes("31 DB");
            //xor ebp, ebp
            CaveMemory.Write_StrBytes("31 ED");
            //xor ecx, ecx
            CaveMemory.Write_StrBytes("31 C9");
            //mov [esp+10], 0x00000000
            CaveMemory.Write_StrBytes("C7 44 24 10 00 00 00 00");
            //mov [esp+14], 0x0000FF00
            CaveMemory.Write_StrBytes("C7 44 24 14 00 FF 00 00");
            //mov [esp+18], 0x0000FF00
            CaveMemory.Write_StrBytes("C7 44 24 18 00 FF 00 00");
            //mov [esp+1C], 0x0000FF00
            CaveMemory.Write_StrBytes("C7 44 24 1C 00 FF 00 00");
            //mov esi, 0x0000FF00
            CaveMemory.Write_StrBytes("BE 00 FF 00 00");
            //mov     dword_C17D28, edi
            CaveMemory.Write_StrBytes("89 3D 28 7D C1 00");
            //Jump back
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _CalibrationValues_InjectionStruct.InjectionReturnOffset);

            //Inject it
            CaveMemory.InjectToOffset(_CalibrationValues_InjectionStruct, "Calibration Values");
        }

        protected override void Apply_OutputsMemoryHack()
        {
            //Create Databak to store our value
            Create_OutputsDataBank();
            _P1_CustomRecoil_CaveAddress = _OutputsDatabank_Address;
            _P2_CustomRecoil_CaveAddress = _OutputsDatabank_Address + 0x04;
            _CurrentSequenceStringLength = _OutputsDatabank_Address + 0x10;
            _CurrentSequenceString = _OutputsDatabank_Address + 0x14;

            SetHack_Recoil();
            SetHack_GetCurrentSequence();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        //Original game is simply setting a Motor to vibrate, so simply using this data to create or pulsed custom recoil will not be synchronized with bullets shot
        //as the pulses lenght and spaceing will depend on DemulShooter output pulse config data.
        //To synch recoil pulse with projectiles, this hack allows to intercept the code shooting the actual projectile to generate the pulse
        
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov ecx,[edi+04]
            CaveMemory.Write_StrBytes("8B 4F 04");
            //mov eax,ecx
            CaveMemory.Write_StrBytes("8B C1");
            //shl eax, 2
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _P1_CustomRecoil_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_CustomRecoil_CaveAddress));
            //mov [eax], 1
            CaveMemory.Write_StrBytes("C7 00 01 00 00 00");
            //xor al,al
            CaveMemory.Write_StrBytes("30 C0");
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _Recoil_InjectionStruct.InjectionReturnOffset);

            //Inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
        }

        /// <summary>
        /// Get the current sequence string, this will be used later to filter when the game is in "Advertise" mode
        /// </summary>
        private void SetHack_GetCurrentSequence()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov mov eax,[esp+54]
            CaveMemory.Write_StrBytes("8B 44 24 54");
            //test eax, eax
            CaveMemory.Write_StrBytes("85 C0");
            //je Exit
            CaveMemory.Write_StrBytes("74 2A");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //call strlen()
            CaveMemory.Write_call((UInt32)_TargetProcess_MemoryBaseAddress + _StrlenFunction_Offset);
            //add esp, 4
            CaveMemory.Write_StrBytes("83 C4 04");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //push esi
            CaveMemory.Write_StrBytes("56");
            //push edi
            CaveMemory.Write_StrBytes("57");
            //mov ecx, _CurrentSequenceStringLength
            CaveMemory.Write_StrBytes("B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CurrentSequenceStringLength));
            //mov [ecx], eax
            CaveMemory.Write_StrBytes("89 01");
            //mov ecx,eax
            CaveMemory.Write_StrBytes("8B C8");
            //inc ecx
            CaveMemory.Write_StrBytes("41");
            //mov mov esi,[esp+60]
            CaveMemory.Write_StrBytes("8B 74 24 60");
            //mov edi, _CurrentSequenceString
            CaveMemory.Write_StrBytes("BF");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CurrentSequenceString));
            //repe movsb
            CaveMemory.Write_StrBytes("F3 A4");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //mov mov eax,[esp+54]
            CaveMemory.Write_StrBytes("8B 44 24 54");
            //test eax,eax
            CaveMemory.Write_StrBytes("85 C0");

            //Inject it
            CaveMemory.InjectToOffset(_CurrentSequence_InjectionStruct, "Current Sequence");
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
                WriteByte(_Data_Base_Address + _P1_X_Offset, bufferX[0]);
                WriteByte(_Data_Base_Address + _P1_Y_Offset, bufferY[0]);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Data_Base_Address + _P1_Buttons_Offset, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Data_Base_Address + _P1_Buttons_Offset, 0xFD);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask(_Data_Base_Address + _P1_Buttons_Offset, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_Data_Base_Address + _P1_Buttons_Offset, 0xFE);
            }
            else if (PlayerData.ID == 2)
            {
                WriteByte(_Data_Base_Address + _P2_X_Offset, bufferX[0]);
                WriteByte(_Data_Base_Address + _P2_Y_Offset, bufferY[0]);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Data_Base_Address + _P2_Buttons_Offset, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Data_Base_Address + _P2_Buttons_Offset, 0xFD);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask(_Data_Base_Address + _P2_Buttons_Offset, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_Data_Base_Address + _P2_Buttons_Offset, 0xFE);
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.Lmp2D3D, OutputId.Lmp2D3D));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_AirFront, OutputId.P1_AirFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_AirFront, OutputId.P2_AirFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_AirRear, OutputId.P1_AirRear));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_AirRear, OutputId.P2_AirRear));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunRecoil, OutputId.P2_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
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
            byte[] buffer = ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _OutputsPtr_Offset, 4);
            byte OutputData1 = ReadByte(BitConverter.ToUInt32(buffer, 0) + 0x44);
            byte OutputData2 = ReadByte(BitConverter.ToUInt32(buffer, 0) + 0x45);
            SetOutputValue(OutputId.P1_LmpStart, OutputData1 >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, OutputData1 >> 4 & 0x01);
            SetOutputValue(OutputId.Lmp2D3D, OutputData1 >> 1 & 0x01);
            SetOutputValue(OutputId.P1_AirFront, OutputData2 >> 7 & 0x01);
            SetOutputValue(OutputId.P2_AirFront, OutputData2 >> 6 & 0x01);
            SetOutputValue(OutputId.P1_AirRear, OutputData2 >> 5 & 0x01);
            SetOutputValue(OutputId.P2_AirRear, OutputData2 >> 4 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, OutputData1 >> 5 & 0x01);
            SetOutputValue(OutputId.P1_GunMotor, OutputData1 >> 6 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, OutputData1 >> 2 & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, OutputData1 >> 3 & 0x01);

            //Custom Outputs
            UInt32 iTemp = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersStructPtr_Offset);
            UInt32 P1_Strutc_Address = ReadPtr(iTemp + 0x08);
            UInt32 P2_Strutc_Address = ReadPtr(iTemp + 0x0C);
            P1_Strutc_Address = ReadPtr(P1_Strutc_Address + 0x14);
            P2_Strutc_Address = ReadPtr(P2_Strutc_Address + 0x14);
            int P1_Status = ReadByte(P1_Strutc_Address + 0x38);
            int P2_Status = ReadByte(P2_Strutc_Address + 0x38);  
            _P1_Life = 0;
            _P2_Life = 0;

            UInt32 StrLength = BitConverter.ToUInt32(ReadBytes(_CurrentSequenceStringLength, 4), 0);
            byte[] bCurrentSeq = ReadBytes(_CurrentSequenceString, StrLength);
            string sCurrentSeq = System.Text.ASCIIEncoding.ASCII.GetString(bCurrentSeq);
            if (sCurrentSeq.StartsWith("Adv"))
                _IsAttractMode = true;
            if (sCurrentSeq.StartsWith("GameSeq"))
                _IsAttractMode = false;

            if (!_IsAttractMode)
            {
                _P1_Life = (int)BitConverter.ToSingle(ReadBytes(P1_Strutc_Address + 0x20, 4), 0);
                if (_P1_Life < 0)
                    _P1_Life = 0;

                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                _P2_Life = (int)BitConverter.ToSingle(ReadBytes(P2_Strutc_Address + 0x20, 4), 0);
                if (_P2_Life < 0)
                    _P2_Life = 0;

                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);
            }
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            //Using constant "ON" value from motor to create asynch outputs for recoil
            //Pulses are not sync with bullets, but with DemulShooter output on/off timings only
            /*SetOutputValue(OutputId.P1_CtmRecoil, OutputData1 >> 6 & 0x01);
            SetOutputValue(OutputId.P2_CtmRecoil, OutputData1 >> 3 & 0x01);*/

            //New method : reading intercepted value for bullet fired, so that recoil is sync with bullets
            //Need to filter this with MOTOR_ON, if not : recoil will be activated when bullets are fired during attract
            if (ReadByte(_P1_CustomRecoil_CaveAddress) == 1 && (OutputData1 >> 6 & 0x01) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                WriteByte(_P1_CustomRecoil_CaveAddress, 0);
            }
            if (ReadByte(_P2_CustomRecoil_CaveAddress) == 1 && (OutputData1 >> 3 & 0x01) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte(_P2_CustomRecoil_CaveAddress, 0);
            }
            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x0082E6A0)); //0x00842A88 also possible
        }

        #endregion
    }
}
