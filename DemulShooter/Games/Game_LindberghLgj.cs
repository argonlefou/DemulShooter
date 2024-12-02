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
    class Game_LindberghLgj : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\lindbergh\lgj";

        //INPUT_STRUCT offset in game
        private const UInt32 INPUT_X_OFFSET = 0x134;
        private const UInt32 INPUT_Y_OFFSET = 0x138;

        //NOP for gun axis
        private NopStruct _Nop_Axis_X = new NopStruct(0x080A9AD2, 8);
        private NopStruct _Nop_Axis_Y = new NopStruct(0x080A97FE, 8);

        //Codecave injection for Buttons
        private InjectionStruct _Buttons_InjectionStruct = new InjectionStruct(0x0840F40C,6);
        
        //Base PTR to find P1 & P2 Input struct
        private UInt32 _Player1_InputPtr_Address = 0x087D3BB0;
        private UInt32 _Player2_InputPtr_Address = 0x087D3BAC;

        //Base PTR to find Buttons values
        private UInt32 _Buttons_Address = 0x08BECB79;

        //Outputs
        private UInt32 _Outputs_Address = 0x087D186D;
        private UInt32 _Credits_Address = 0x08C08420;
        private UInt32 _PlayersStatus_Address = 0x87D3DC0;

        //Custom recoil injection
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x080AFA22, 6);
        private UInt32 _P1_CustomRecoil_CaveAddress = 0;
        private UInt32 _P2_CustomRecoil_CaveAddress = 0;

        //Check instruction for game loaded
        private UInt32 _RomLoaded_Check_Instruction_Original = 0x0807925B;
        private UInt32 _RomLoaded_check_Instruction_RevA = 0x0807934B;

        private UInt32 _Player1_InputStruct_Address = 0;
        private UInt32 _Player2_InputStruct_Address = 0;
        private float _P1_X_Float;
        private float _P1_Y_Float;
        private float _P2_X_Float;
        private float _P2_Y_Float;             

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_LindberghLgj(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "BudgieLoader", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Let's Go Jungle - Original", "b3f71e9defa7f71d777958138e4a0ebb");
            _KnownMd5Prints.Add("Let's Go Jungle - Rev.A", "875486650a16abce5b1901fd319001c0");

            _tProcess.Start();
            Logger.WriteLog("Waiting for Lindbergh " + _RomName + " game to hook.....");
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
                            //To make sure BurgieLoader has loaded the rom entirely, we're looking for some random instruction to be present in memory before starting                            
                            byte[] buffer = ReadBytes(_RomLoaded_Check_Instruction_Original, 6);
                            if (buffer[0] == 0x8B && buffer[1] == 0x1D && buffer[2] == 0xB0 && buffer[3] == 0x3B && buffer[4] == 0x7D && buffer[5] == 0x08)
                            {
                                Logger.WriteLog("Let's Go Jungle - Original binary detected");
                                _TargetProcess_Md5Hash = _KnownMd5Prints["Let's Go Jungle - Original"];
                                ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);

                                buffer = ReadBytes(_Player1_InputPtr_Address, 4);
                                _Player1_InputStruct_Address = BitConverter.ToUInt32(buffer, 0);

                                buffer = ReadBytes(_Player2_InputPtr_Address, 4);
                                _Player2_InputStruct_Address = BitConverter.ToUInt32(buffer, 0);

                                if (_Player1_InputStruct_Address != 0 && _Player2_InputStruct_Address != 0)
                                {
                                    _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));                                    

                                    Logger.WriteLog("P1 InputStruct address = 0x" + _Player1_InputStruct_Address.ToString("X8"));
                                    Logger.WriteLog("P2 InputStruct address = 0x" + _Player2_InputStruct_Address.ToString("X8"));

                                    Apply_MemoryHacks();
                                    _ProcessHooked = true;
                                    RaiseGameHookedEvent();                                    
                                }
                            }
                            else
                            {
                                buffer = ReadBytes(_RomLoaded_check_Instruction_RevA, 6);
                                if (buffer[0] == 0x8B && buffer[1] == 0x1D && buffer[2] == 0x10 && buffer[3] == 0xBE && buffer[4] == 0x7D && buffer[5] == 0x08)
                                {
                                    Logger.WriteLog("Let's Go Jungle - Rev.A binary detected");
                                    _TargetProcess_Md5Hash = _KnownMd5Prints["Let's Go Jungle - Rev.A"];
                                    ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);

                                    buffer = ReadBytes(_Player1_InputPtr_Address, 4);
                                    _Player1_InputStruct_Address = BitConverter.ToUInt32(buffer, 0);

                                    buffer = ReadBytes(_Player2_InputPtr_Address, 4);
                                    _Player2_InputStruct_Address = BitConverter.ToUInt32(buffer, 0);

                                    if (_Player1_InputStruct_Address != 0 && _Player2_InputStruct_Address != 0)
                                    {
                                        _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                        Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                        Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));                                    

                                        Logger.WriteLog("P1 InputStruct address = 0x" + _Player1_InputStruct_Address.ToString("X8"));
                                        Logger.WriteLog("P2 InputStruct address = 0x" + _Player2_InputStruct_Address.ToString("X8"));

                                        Apply_MemoryHacks();
                                        _ProcessHooked = true;
                                        RaiseGameHookedEvent();                                    
                                    }
                                }
                                else
                                {
                                    Logger.WriteLog("Game not Loaded, waiting...");
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

                    //X => [-1 ; 1] float
                    //Y => [-1 ; 1] float

                    float X_Value = (2.0f * PlayerData.RIController.Computed_X / (float)TotalResX) - 1.0f;
                    float Y_Value = 1.0f - (2.0f * PlayerData.RIController.Computed_Y / (float)TotalResY);

                    if (X_Value < -1.0f)
                        X_Value = -1.0f;
                    if (Y_Value < -1.0f)
                        Y_Value = -1.0f;
                    if (X_Value > 1.0f)
                        X_Value = 1.0f;
                    if (Y_Value > 1.0f)
                        Y_Value = 1.0f;

                    if (PlayerData.ID == 1)
                    {
                        _P1_X_Float = X_Value;
                        _P1_Y_Float = Y_Value;
                    }
                    else if (PlayerData.ID == 2)
                    {
                        _P2_X_Float = X_Value;
                        _P2_Y_Float = Y_Value;
                    }
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
            // Noping X,Y axis values update in the following procedure :
            // acpPlayer::input()
            SetNops(0, _Nop_Axis_X);
            SetNops(0, _Nop_Axis_Y);

            SetHack_Buttons();

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }
        
        /// <summary>
        /// Start and Trigger are on the same Byte, so we can't simply NOP, to keep Teknoparrot Start button working
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //cmp edx, 0
            CaveMemory.Write_StrBytes("83 FA 00");
            //je Hack
            CaveMemory.Write_StrBytes("0F 84 0E 00 00 00");
            //cmp edx, 0
            CaveMemory.Write_StrBytes("83 FA 04");
            //je Hack
            CaveMemory.Write_StrBytes("0F 84 05 00 00 00");
            //je original code
            CaveMemory.Write_StrBytes("E9 17 00 00 00");
            //Hack
            //and al, 0xF0
            CaveMemory.Write_StrBytes("24 F0");
            //and [edx+08BECB79],FFFFFF0F
            CaveMemory.Write_StrBytes("81 A2");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_Address));
            CaveMemory.Write_StrBytes("0F FF FF FF");
            //or [edx+08BECB79],al
            CaveMemory.Write_StrBytes("08 82");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_Address));
            //jmp exit
            CaveMemory.Write_StrBytes("E9 06 00 00 00");
            //OriginalCode
            //mov [edx+08BECB79],al
            CaveMemory.Write_StrBytes("88 82");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_Address));

            //Inject it
            CaveMemory.InjectToAddress(_Buttons_InjectionStruct, "Recoil");
        }

        // amCreditIsEnough() => 0x084E4A2F ~~ 0x084E4AC4
        // Even though Freeplay is forced by TeknoParrot, this procedure always find "NO CREDITS" for P2
        // Replacing conditionnal Jump by single Jump force OK (for both players)
        private void SetHackEnableP2()
        {
            WriteByte(0x84E4A56, 0xEB);
        }

        //Original game is simply setting a Motor to vibrate, so simply using this data to create or pulsed custom recoil will not be synchronized with bullets shot
        //as the pulses lenght and spaceing will depend on DemulShooter output pulse config data.
        //To synch recoil pulse with projectiles, this hack allows to intercept the code increasing the "PlayerShotCounter" variable
        protected override void Apply_OutputsMemoryHack()
        {
            //Create Databak to store our value
            Create_OutputsDataBank();
            _P1_CustomRecoil_CaveAddress = _OutputsDatabank_Address;
            _P2_CustomRecoil_CaveAddress = _OutputsDatabank_Address + 0x04;

            SetHack_Recoil();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Intercepting the bullet count increase call to create our own recoil
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov    eax,DWORD PTR [ebx+0x118] 
            CaveMemory.Write_StrBytes("8B 83 18 01 00 00");
            //shl eax, 2
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _P1_CustomRecoil_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_CustomRecoil_CaveAddress));
            //mov [eax], 1
            CaveMemory.Write_StrBytes("C7 00 01 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //mov ds:g_PlayerShotCounter, edx
            //ds:g_PlayerShotCounter is changing with build so we just read/copy the original code line
            CaveMemory.Write_Bytes(ReadBytes(_Recoil_InjectionStruct.InjectionOffset, 6));

            //Inject it
            CaveMemory.InjectToAddress(_Recoil_InjectionStruct, "Recoil");
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>
        public override void SendInput(PlayerSettings PlayerData)
        {
            if (PlayerData.ID == 1)
            {
                WriteBytes(_Player1_InputStruct_Address + INPUT_X_OFFSET, BitConverter.GetBytes(_P1_X_Float));
                WriteBytes(_Player1_InputStruct_Address + INPUT_Y_OFFSET, BitConverter.GetBytes(_P1_Y_Float));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)             
                    Apply_OR_ByteMask(_Buttons_Address, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)             
                    Apply_AND_ByteMask(_Buttons_Address, 0xFD);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)             
                    Apply_OR_ByteMask(_Buttons_Address, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)  
                    Apply_AND_ByteMask(_Buttons_Address, 0xFE);
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes(_Player2_InputStruct_Address + INPUT_X_OFFSET, BitConverter.GetBytes(_P2_X_Float));
                WriteBytes(_Player2_InputStruct_Address + INPUT_Y_OFFSET, BitConverter.GetBytes(_P2_Y_Float));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Buttons_Address + 4, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Buttons_Address + 4, 0xFD);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask(_Buttons_Address + 4, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_Buttons_Address + 4, 0xFE);
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : Is activated permanently while trigger is pressed
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpRoom, OutputId.LmpRoom));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpCoin, OutputId.LmpCoin));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
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
            //Original Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Outputs_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.LmpRoom, ReadByte(_Outputs_Address) >> 5 & 0x01);
            SetOutputValue(OutputId.LmpCoin, ReadByte(_Outputs_Address) >> 2 & 0x01);
            SetOutputValue(OutputId.P1_GunMotor, ReadByte(_Outputs_Address) >> 3 & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, ReadByte(_Outputs_Address) >> 6 & 0x01);
            

            //Custom Outputs
            UInt32 P1_StructAddress = ReadPtr(_Player1_InputPtr_Address);
            UInt32 P2_StructAddress = ReadPtr(_Player2_InputPtr_Address);

            _P1_Life = 0;
            _P2_Life = 0;
            //Checking this byte (player playing ?) otherwise the game is still enabing rumble during Attract Mode
            if (ReadByte(_PlayersStatus_Address) == 1)
            {
                if (P1_StructAddress != 0)
                {
                    _P1_Life = (int)BitConverter.ToSingle(ReadBytes(P1_StructAddress + 0x4BC, 4), 0);
                    //[Damaged] custom Output                
                    if (_P1_Life < _P1_LastLife)
                        SetOutputValue(OutputId.P1_Damaged, 1);                

                    if (ReadByte(_P1_CustomRecoil_CaveAddress) == 1 && (ReadByte(_Outputs_Address) >> 3 & 0x01) == 1)
                    {
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);
                        WriteByte(_P1_CustomRecoil_CaveAddress, 0);
                    }
                    
                }
            }

            //Checking this byte (player playing ?) otherwise the game is still enabing rumble during Attract Mode
            if (ReadByte(_PlayersStatus_Address + 4) == 1)
            {
                if (P2_StructAddress != 0)
                {
                    _P2_Life = (int)BitConverter.ToSingle(ReadBytes(P2_StructAddress + 0x4BC, 4), 0);
                    //[Damaged] custom Output                
                    if (_P2_Life < _P2_LastLife)
                        SetOutputValue(OutputId.P2_Damaged, 1);

                    if (ReadByte(_P2_CustomRecoil_CaveAddress) == 1 && (ReadByte(_Outputs_Address) >> 6 & 0x01) == 1)
                    {
                        SetOutputValue(OutputId.P2_CtmRecoil, 1);
                        WriteByte(_P2_CustomRecoil_CaveAddress, 0);
                    }
                }
            }

            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);            
            SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
    }
}
