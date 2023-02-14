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
    class Game_LindberghHotd4 : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\lindbergh\hotd4";
        private UInt32 _InputStructPtr_Address = 0xA6F27A8;
        private UInt32 _P1_InputStruct_Address = 0;
        private UInt32 _P2_InputStruct_Address = 0;

        //INPUT_SET structure offset in game
        private const UInt32 INPUT_X_OFFSET = 0x04;
        private const UInt32 INPUT_Y_OFFSET = 0x05;
        private const UInt32 INPUT_TRIGGER_OFFSET = 0x08;
        private const UInt32 INPUT_RELOAD_OFFSET = 0x0C;
        private const UInt32 INPUT_WEAPONBTN_OFFSET = 0x10;

        //NOP for Gun init procedure : CGunMgr::Init()
        private NopStruct _Nop_Gun_Init = new NopStruct(0x081C9ED5, 6);     //only X,Y axis
        //private NopStruct _Nop_Gun_Init = new NopStruct(0x081C9ED5, 15);  //Axis + Buttons

        //NOP for Gun axis and buttons in-game
        private NopStruct _Nop_Axis_X = new NopStruct(0x081536EF, 3);
        private NopStruct _Nop_Axis_Y = new NopStruct(0x081536F7, 3);
        private NopStruct _Nop_Trigger = new NopStruct(0x0815337D, 3);
        private NopStruct _Nop_Reload_1 = new NopStruct(0x08153380, 3);
        private NopStruct _Nop_Reload_2 = new NopStruct(0x08153714, 7);
        private NopStruct _Nop_WeaponBtn = new NopStruct(0x08153383, 3);       
        
        //Outputs Address
        private UInt32 _OutputsPtr_Address = 0x0A6F2754;
        private UInt32 _Outputs_Address = 0;
        private UInt32 _Credits_Address = 0x0A715CC0;
        private UInt32 _GameModePtr_Address = 0x0A6F27A8;
        private UInt32 _PlayerStructPtr_Address = 0x0A6F27F8;
        private int _P1_LastLife = 0;
        private int _P2_LastLife = 0;
        private int _P1_Life = 0;
        private int _P2_Life = 0;
        private int _P1_LastAmmo = 0;
        private int _P2_LastAmmo = 0;
        private int _P1_Ammo = 0;
        private int _P2_Ammo = 0;

        //Rom loaded + Rom version check
        private UInt32 _RomLoaded_check_Instruction_RevA = 0x08152851;
        private UInt32 _RomLoaded_check_Instruction_RevC = 0x08153065;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_LindberghHotd4(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "BudgieLoader", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("House of The Dead 4 - Rev.A", "7db931d8238dbd02325c0e0f8869a06d");
            _KnownMd5Prints.Add("House of The Dead 4 - Rev.C", "036408020b362255455b84028618352b");

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
                            //And this instruction is also helping us detecting whether the game file is Rev.A or Rev.C binary, to call the corresponding hack
                            byte[] buffer = ReadBytes(_RomLoaded_check_Instruction_RevA, 5);
                            if (buffer[0] == 0xE8 && buffer[1] == 0x42 && buffer[2] == 0x0D && buffer[3] == 0x00 && buffer[4] == 0x00)
                            {
                                Logger.WriteLog("House Of The Dead 4 - Rev. A binary detected");
                                _TargetProcess_Md5Hash = _KnownMd5Prints["House of The Dead 4 - Rev.A"];
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                                if (!_DisableInputHack)
                                    SetHack();
                                else
                                    Logger.WriteLog("Input Hack disabled");
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
                            }
                            else
                            {
                                buffer = ReadBytes(_RomLoaded_check_Instruction_RevC, 5);
                                if (buffer[0] == 0xE8 && buffer[1] == 0x42 && buffer[2] == 0x0D && buffer[3] == 0x00 && buffer[4] == 0x00)
                                {
                                    Logger.WriteLog("House Of The Dead 4 - Rev.C binary detected");
                                    _TargetProcess_Md5Hash = _KnownMd5Prints["House of The Dead 4 - Rev.C"];
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                    ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                                    if (!_DisableInputHack)
                                        SetHack();
                                    else
                                        Logger.WriteLog("Input Hack disabled");
                                    _ProcessHooked = true;
                                    RaiseGameHookedEvent();
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
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");
                    //X and Y axis => 0x00 - 0xFF                    
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;

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

        private void SetHack()
        {
            // CgunMgr::Init()
            // Init Axis and buttons values to 0
            // Not called that often (maybe after START or CONTINUE or new level), maybe not necessary to block them
            SetNops(0, _Nop_Gun_Init);

            // CGunMgr::MainProc()
            SetNops(0, _Nop_Axis_X);
            SetNops(0, _Nop_Axis_Y);
            SetNops(0, _Nop_Trigger);
            SetNops(0, _Nop_Reload_1);
            SetNops(0, _Nop_Reload_2);
            SetNops(0, _Nop_WeaponBtn);            

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary> 
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] buffer = ReadBytes(_InputStructPtr_Address, 4);    
            
            if (PlayerData.ID == 1)
            {
                try
                {
                    _P1_InputStruct_Address = BitConverter.ToUInt32(buffer, 0) + 0x34;
                    if (_P1_InputStruct_Address != 0)
                    {
                        WriteByte(_P1_InputStruct_Address + INPUT_X_OFFSET, (byte)PlayerData.RIController.Computed_X);
                        WriteByte(_P1_InputStruct_Address + INPUT_Y_OFFSET, (byte)PlayerData.RIController.Computed_Y);

                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                            WriteByte(_P1_InputStruct_Address + INPUT_TRIGGER_OFFSET, 0x01);
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                            WriteByte(_P1_InputStruct_Address + INPUT_TRIGGER_OFFSET, 0x00);

                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                            WriteByte(_P1_InputStruct_Address + INPUT_WEAPONBTN_OFFSET, 0x01);
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                            WriteByte(_P1_InputStruct_Address + INPUT_WEAPONBTN_OFFSET, 0x00);

                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                            WriteByte(_P1_InputStruct_Address + INPUT_RELOAD_OFFSET, 0x01);
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                            WriteByte(_P1_InputStruct_Address + INPUT_RELOAD_OFFSET, 0x00);
                    }
                }
                catch
                { }
            }
            else if (PlayerData.ID == 2)
            {
                try
                {
                    _P2_InputStruct_Address = BitConverter.ToUInt32(buffer, 0) + 0x58;
                    if (_P2_InputStruct_Address != 0)
                    {
                        WriteByte(_P2_InputStruct_Address + INPUT_X_OFFSET, (byte)PlayerData.RIController.Computed_X);
                        WriteByte(_P2_InputStruct_Address + INPUT_Y_OFFSET, (byte)PlayerData.RIController.Computed_Y);

                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                            WriteByte(_P2_InputStruct_Address + INPUT_TRIGGER_OFFSET, 0x01);
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                            WriteByte(_P2_InputStruct_Address + INPUT_TRIGGER_OFFSET, 0x00);

                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                            WriteByte(_P2_InputStruct_Address + INPUT_WEAPONBTN_OFFSET, 0x01);
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                            WriteByte(_P2_InputStruct_Address + INPUT_WEAPONBTN_OFFSET, 0x00);

                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                            WriteByte(_P2_InputStruct_Address + INPUT_RELOAD_OFFSET, 0x01);
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                            WriteByte(_P2_InputStruct_Address + INPUT_RELOAD_OFFSET, 0x00);
                    }
                }
                catch
                { }
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Clip, OutputId.P2_Clip));
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
            _Outputs_Address = BitConverter.ToUInt32(ReadBytes(_OutputsPtr_Address, 4), 0);
            int P1_Motor_Status = ReadByte(_Outputs_Address) >> 6 & 0x01;
            int P2_Motor_Status = ReadByte(_Outputs_Address) >> 3 & 0x01;
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Outputs_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_GunMotor, P1_Motor_Status);
            SetOutputValue(OutputId.P2_GunMotor, P2_Motor_Status);

            //Custom Outputs
            UInt32 GameModePtr = BitConverter.ToUInt32(ReadBytes(ReadPtr(_GameModePtr_Address) + 0x2C, 4), 0);
            int GameMode = ReadByte(GameModePtr + 0x38);            
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            if (GameMode == 8)
            {
                UInt32 PlayersPtr_BaseAddress = ReadPtr(_PlayerStructPtr_Address);
                if (PlayersPtr_BaseAddress != 0)
                {
                    UInt32 P1_StructAddress = ReadPtr(PlayersPtr_BaseAddress + 0x34);
                    UInt32 P2_StructAddress = ReadPtr(PlayersPtr_BaseAddress + 0x38);
                    //PlayerMode:
                    //4: CutScene
                    //3: InGame
                    //9, 11: Continue
                    //0: GameOver
                    int P1_Mode = ReadByte(P1_StructAddress + 0x38);
                    int P2_Mode = ReadByte(P2_StructAddress + 0x38);
                    if (P1_Mode == 3 || P1_Mode == 4)
                    {
                        _P1_Life = ReadByte(P1_StructAddress + 0x3C);
                        _P1_Ammo = ReadByte(P1_StructAddress + 0x274);

                        //[Damaged] custom Output                
                        if (_P1_Life < _P1_LastLife)
                            SetOutputValue(OutputId.P1_Damaged, 1);

                        //[Clip] custom Output   
                        if (_P1_Ammo > 0)
                            P1_Clip = 1;

                        //[Recoil] custom output :
                        //Generate new event for each bullet fired, only while original motor event is ON
                        //(this way, no recoil during attract mode)
                        if (_P1_Ammo < _P1_LastAmmo)
                            SetOutputValue(OutputId.P1_CtmRecoil, 1);
                    }

                    if (P2_Mode == 3 || P2_Mode == 4)
                    {
                        _P2_Life = ReadByte(P2_StructAddress + 0x3C);
                        _P2_Ammo = ReadByte(P2_StructAddress + 0x274);

                        //[Damaged] custom Output      
                        if (_P2_Life < _P2_LastLife)
                            SetOutputValue(OutputId.P2_Damaged, 1);

                        //[Clip] custom Output      
                        if (_P2_Ammo > 0)
                            P2_Clip = 1;

                        //[Recoil] custom output :
                        //Generate new event for each bullet fired, only while original motor event is ON
                        //(this way, no recoil during attract mode)
                        if (_P2_Ammo < _P2_LastAmmo)
                            SetOutputValue(OutputId.P2_CtmRecoil, 1);
                    } 
                }
            }

            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;
            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip); 
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
            SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
    }
}
