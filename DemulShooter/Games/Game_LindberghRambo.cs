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
    class Game_LindberghRambo : Game
    {
        private UInt32 _InputStructPtr_Address = 0x085CE940;
        private UInt32 _P1_InputStruct_Address = 0;
        private UInt32 _P2_InputStruct_Address = 0;

        //INPUT_STRUCT offset in game
        //Hacking axis in this structure, to be sure there will be no issue with reload and aim offset
        private const UInt32 INPUT_X_OFFSET = 0x04;
        private const UInt32 INPUT_Y_OFFSET = 0x05;        

        //static load JVS data structure from code at 0x082C72B1 procedure
        private UInt32 _JvsStruct_Address = 0x0ADDF360;
        private const UInt32 JVS_P1_TRIGGER_OFFSET = 0x19C;
        private const UInt32 JVS_P1_RAGE_OFFSET = 0x1A0;
        private const UInt32 JVS_P2_TRIGGER_OFFSET = 0x1A4;
        private const UInt32 JVS_P2_RAGE_OFFSET = 0x1A8;
        private const UInt32 JVS_P1_X_OFFSET = 0x1AD;
        private const UInt32 JVS_P1_Y_OFFSET = 0x1B3;
        private const UInt32 JVS_P2_X_OFFSET = 0x1B9;
        private const UInt32 JVS_P2_Y_OFFSET = 0x1BF;        

        //NOP for Gun init procedure, in CGunMgr::Init() procedure
        private NopStruct _Nop_X_Init = new NopStruct(0x08073833, 4);
        private NopStruct _Nop_Y_Init = new NopStruct(0x0807383A, 4);

        //NOP for Gun axis and buttons in-game, in JvsNodeReceive() procedure        
        //Used to NOP JVS data at source, this is used to validate coordinates On/Out of screen for reload
        private NopStruct _Nop_Jvs_Src_Axis_1 = new NopStruct(0x082C7E47, 3);
        private NopStruct _Nop_Jvs_Src_Axis_2 = new NopStruct(0x082C78D3, 3);

        //NOP for post-calculated cordinates, in CGunMgr::MainProc() procedure
        private NopStruct _Nop_Axis_X = new NopStruct(0x08073EBB, 3);
        private NopStruct _Nop_Axis_Y = new NopStruct(0x08073EC5, 3);

        //Codecave injection blocking gun buttons
        private UInt32 _Btn_Injection_Address = 0x082C7B3C;
        private UInt32 _Btn_Injection_Return_Address = 0x082C7B41;

        //Show crosshair flag
        private UInt32 _DrawSightFlag_Address = 0x85A3bE8;
        
        //Outputs
        private UInt32 _OutputsPtr_Address = 0x085DD158;
        private UInt32 _Outputs_Address;
        private UInt32 _pCreditsMgr_Address = 0x085DC2B0;
        private UInt32 _PlayerStructPtr_Address = 0x85CE9B0;

        private UInt32 _RomLoaded_Check_Instruction = 0x08073BC7;

        private bool _HideCrosshair = false;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_LindberghRambo(String RomName, bool HideCrosshair, bool DisableInputHack, bool Verbose)
            : base(RomName, "BudgieLoader", DisableInputHack, Verbose)
        {
            _HideCrosshair = HideCrosshair;
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
                            byte[] buffer = ReadBytes(_RomLoaded_Check_Instruction, 5);
                            if (buffer[0] == 0xE8 && buffer[1] == 0x76 && buffer[2] == 0x01 && buffer[3] == 0x00 && buffer[4] == 0x00)
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));

                                //Hiding crosshair option
                                if (_HideCrosshair)
                                    WriteByte(_DrawSightFlag_Address, 0);

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

                    //X and Y axis => 0x00 - 0xFF                    
                    double dMaxX = 256.0;
                    double dMaxY = 256.0;

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
            SetHack_Btn();

            // CgunMgr::Init() => 0x0807382A ~ 0x080738D1
            // Init Axis and buttons values to 0
            // Not called that often (maybe after START or CONTINUE or new level), maybe not necessary to block them
            SetNops(0, _Nop_X_Init);
            SetNops(0, _Nop_Y_Init);            
            
            //These one were here to NOP axis data in JVS read procedure.
            //These values are used to compute aiming at screen with gun calibration parameters,
            //But we need to set these properly to allow shoot (on-screen values) or to allow Reload (out-of-screen values)
            //We init them on-screen and will set them out-of-screen on a right click event
            SetNops(0, _Nop_Jvs_Src_Axis_1);
            SetNops(0, _Nop_Jvs_Src_Axis_2);
            WriteByte(_JvsStruct_Address + JVS_P1_X_OFFSET, 0x80);
            WriteByte(_JvsStruct_Address + JVS_P1_Y_OFFSET, 0x80);
            WriteByte(_JvsStruct_Address + JVS_P2_X_OFFSET, 0x80);
            WriteByte(_JvsStruct_Address + JVS_P2_Y_OFFSET, 0x80);            

            //These one will block axis values later after calibration calculations and will give us our aim
            SetNops(0, _Nop_Axis_X);
            SetNops(0, _Nop_Axis_Y);

            //Change init for JVS values axis to set values to 0x80 instead of 00
            List<byte> Buffer = new List<byte>();
            Buffer.Add(0xC7);
            Buffer.Add(0x01);
            Buffer.Add(0x00);
            Buffer.Add(0x80);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            WriteBytes(0x082C7EE8, Buffer.ToArray());
            
            //Hiding crosshair option
            if (_HideCrosshair)
                WriteByte(_DrawSightFlag_Address, 0);

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Gun Buttons and Start are on same memory word, so we need to filter to let Teknoparrot Start button work
        /// </summary>
        private void SetHack_Btn()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            //cmp [ebp-10], 1
            CaveMemory.Write_StrBytes("83 7D F0 01");
            //je exit
            CaveMemory.Write_StrBytes("0F 84 0A 00 00 00");
            //and al, 0x80
            CaveMemory.Write_StrBytes("24 80");
            //and [edx], 0xFFFFFF0F
            CaveMemory.Write_StrBytes("81 22 0F FF FF FF");
            //or [edx], al
            CaveMemory.Write_StrBytes("08 02");
            //Exit:
            //mov eax, [ebp-0C]
            CaveMemory.Write_StrBytes("8B 45 F4");
            //jmp back
            CaveMemory.Write_jmp(_Btn_Injection_Return_Address);

            Logger.WriteLog("Adding Buttons Codecave_1 at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - (_Btn_Injection_Address) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, _Btn_Injection_Address, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
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
                            Apply_OR_ByteMask(_JvsStruct_Address + JVS_P1_TRIGGER_OFFSET, 0x02);
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                            Apply_AND_ByteMask(_JvsStruct_Address + JVS_P1_TRIGGER_OFFSET, 0xFD);
                        
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                            Apply_OR_ByteMask(_JvsStruct_Address + JVS_P1_RAGE_OFFSET, 0x80);
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                            Apply_AND_ByteMask(_JvsStruct_Address + JVS_P1_RAGE_OFFSET, 0x0F);
                        
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                        {
                            WriteByte(_JvsStruct_Address + JVS_P1_X_OFFSET, 0xFF);
                            WriteByte(_JvsStruct_Address + JVS_P1_Y_OFFSET, 0xFF);
                            Apply_OR_ByteMask(_JvsStruct_Address + JVS_P1_TRIGGER_OFFSET, 0x01);
                        }
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                        {
                            WriteByte(_JvsStruct_Address + JVS_P1_X_OFFSET, 0x80);
                            WriteByte(_JvsStruct_Address + JVS_P1_Y_OFFSET, 0x80);
                            Apply_AND_ByteMask(_JvsStruct_Address + JVS_P1_TRIGGER_OFFSET, 0xFE);
                        }
                    }
                }
                catch
                { }
            }
            else if (PlayerData.ID == 2)
            {
                try
                {
                    _P2_InputStruct_Address = BitConverter.ToUInt32(buffer, 0) + 0x64;
                    if (_P2_InputStruct_Address != 0)
                    {
                        WriteByte(_P2_InputStruct_Address + INPUT_X_OFFSET, (byte)PlayerData.RIController.Computed_X);
                        WriteByte(_P2_InputStruct_Address + INPUT_Y_OFFSET, (byte)PlayerData.RIController.Computed_Y);

                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                            Apply_OR_ByteMask(_JvsStruct_Address + JVS_P2_TRIGGER_OFFSET, 0x02);
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                            Apply_AND_ByteMask(_JvsStruct_Address + JVS_P2_TRIGGER_OFFSET, 0xFD);

                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                            Apply_OR_ByteMask(_JvsStruct_Address + JVS_P2_RAGE_OFFSET, 0x80);
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                            Apply_AND_ByteMask(_JvsStruct_Address + JVS_P2_RAGE_OFFSET, 0x0F);

                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                        {
                            WriteByte(_JvsStruct_Address + JVS_P2_X_OFFSET, 0xFF);
                            WriteByte(_JvsStruct_Address + JVS_P2_Y_OFFSET, 0xFF);
                            Apply_OR_ByteMask(_JvsStruct_Address + JVS_P2_TRIGGER_OFFSET, 0x01);
                        }
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                        {
                            WriteByte(_JvsStruct_Address + JVS_P2_X_OFFSET, 0x80);
                            WriteByte(_JvsStruct_Address + JVS_P2_Y_OFFSET, 0x80);
                            Apply_AND_ByteMask(_JvsStruct_Address + JVS_P2_TRIGGER_OFFSET, 0xFE);
                        }
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
            //Gun motor : Is activated for every bullet fired
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
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
            //Original Outputs
            _Outputs_Address = BitConverter.ToUInt32(ReadBytes(_OutputsPtr_Address, 4), 0);
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_Address) & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Outputs_Address) >> 1 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, ReadByte(_Outputs_Address) >> 2 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, ReadByte(_Outputs_Address) >> 3 & 0x01);

            //Custom Outputs
            UInt32 PlayersPtr_BaseAddress = ReadPtr(_PlayerStructPtr_Address);
            int P1_Ammo = 0;
            int P2_Ammo = 0;
            _P1_Life = 0;
            _P2_Life = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            if (PlayersPtr_BaseAddress != 0)
            {
                UInt32 P1_StructAddress = ReadPtr(PlayersPtr_BaseAddress + 0x34);
                UInt32 P2_StructAddress = ReadPtr(PlayersPtr_BaseAddress + 0x38);
                //PlayerMode:
                //0: Standby
                //3: Game
                //4: Hold
                //A, C: Continue
                int P1_Status = ReadByte(P1_StructAddress + 0x38);
                int P2_Status = ReadByte(P2_StructAddress + 0x38);
                if (P1_Status == 3 || P1_Status == 4)
                {
                    _P1_Life = ReadByte(P1_StructAddress + 0x3C);
                    P1_Ammo = ReadByte(P1_StructAddress + 0x400);
                    
                    //[Damaged] custom Output                
                    if (_P1_Life < _P1_LastLife)
                        SetOutputValue(OutputId.P1_Damaged, 1);
                    
                    //[Clip] custom Output   
                    if (P1_Ammo > 0)
                        P1_Clip = 1;
                }

                if (P2_Status == 3 || P2_Status == 4)
                {
                    _P2_Life = ReadByte(P2_StructAddress + 0x3C);
                    P2_Ammo = ReadByte(P2_StructAddress + 0x400);

                    //[Damaged] custom Output                
                    if (_P2_Life < _P2_LastLife)
                        SetOutputValue(OutputId.P2_Damaged, 1);

                    //[Clip] custom Output   
                    if (P2_Ammo > 0)
                        P2_Clip = 1;
                }
            }            
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            //Custom recoil will be recoil just like the original one
            SetOutputValue(OutputId.P1_CtmRecoil, ReadByte(_Outputs_Address) >> 2 & 0x01);
            SetOutputValue(OutputId.P2_CtmRecoil, ReadByte(_Outputs_Address) >> 3 & 0x01);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            UInt32 CreditsMgr = ReadPtr(_pCreditsMgr_Address); 
            SetOutputValue(OutputId.Credits, (int)(ReadByte(CreditsMgr + 0x38)));
        }

        #endregion
    }
}
