using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;

namespace DemulShooter
{
    class Game_RtWalkingDead : Game
    {
        //Rom loaded + Rom version check
        private UInt32 _RomLoaded_check_Instruction_v105 = 0x0819DE0B;  //Check ForceFeedback setting

        private UInt32 _Calibration_Patch1_Address = 0x081C52BE;
        private UInt32 _Calibration_Patch2_Address = 0x081C52E2;
        private InjectionStruct _RawAxis_InjectionStruct = new InjectionStruct(0x081CB412, 10);
        private InjectionStruct _CalibratedAxis_InjectionStruct = new InjectionStruct(0x081C52B4, 5);
        private InjectionStruct _GunButtons_InjectionStruct = new InjectionStruct(0x081CB475, 7);

        //Custom Values
        private UInt32 _RawAxes_CaveAddress = 0;
        private UInt32 _CalibratedAxes_CaveAddress = 0;
        private UInt32 _GunButtons_CaveAddress = 0;

        //Outputs Address
        private UInt32 _P1_Struct_Address = 0x090100A0;
        private UInt32 _P2_Struct_Address = 0x09010274;
        private UInt32 _Lamp_Address = 0x08AC2188;

        //Custom Values
        private UInt32 _P1_RecoilStatus_CaveAddress = 0;
        private UInt32 _P2_RecoilStatus_CaveAddress = 0;
        private UInt32 _P1_DamageStatus_CaveAddress = 0;
        private UInt32 _P2_DamageStatus_CaveAddress = 0;

        /*** MEMORY ADDRESSES **/
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x0808B1D9, 7);
        private InjectionStruct _Damage_InjectionStruct = new InjectionStruct(0x080762D6, 8);
        private InjectionStruct _NoCrosshair_InjectionStruct = new InjectionStruct(0x0806DF47, 8);

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_RtWalkingDead(String RomName)
            : base(RomName, "linuxloader")
        {
            _KnownMd5Prints.Add("Walking Dead - 01.05 - Teknoparrot Patched", "5158b185b977b38749845be958caddb6");
            _KnownMd5Prints.Add("Walking Dead - 01.05 - Original dump", "6951619cd907173862c5eb337e263af5");

            _tProcess.Start();
            Logger.WriteLog("Waiting for Raw Thrill " + _RomName + " game to hook.....");
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
                            byte[] buffer = ReadBytes(_RomLoaded_check_Instruction_v105, 3);
                            if (buffer[0] == 0x8B && buffer[1] == 0x40 && buffer[2] == 0x48)
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Walking Dead - 01.05 binary detected");
                                //Check would be needed on the game MD5 to find if it is the original or patched version.....
                                //_TargetProcess_Md5Hash = _KnownMd5Prints["Walking Dead - 01.05 - Teknoparrot Patched"];
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                Apply_MemoryHacks();
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

        public override bool GameScale(PlayerSettings PlayerData)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    double dRatio = TotalResX / TotalResY;
                    Logger.WriteLog("Game Window ratio = " + dRatio);

                    double dMinX = 0.0;
                    double dMaxX = 1023.0;
                    double dMinY = 0.0;
                    double dMaxY = 1023.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    PlayerData.RIController.Computed_X = Convert.ToInt32(Math.Round(dRangeX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt32(dRangeY - Math.Round(dRangeY * PlayerData.RIController.Computed_Y / TotalResY));

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

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _RawAxes_CaveAddress = _InputsDatabank_Address;               //8 x 4 Bytes
            _CalibratedAxes_CaveAddress = _InputsDatabank_Address + 32;   //8 x 4 Bytes
            _GunButtons_CaveAddress = _InputsDatabank_Address + 64;       //8 x 1 Byte

            //Jumping over "invert" axis option for calibration
            WriteByte(_Calibration_Patch1_Address, 0xEB);

            //Jumping over last optional calibration calculation, after our own value is inserted
            WriteByte(_Calibration_Patch2_Address, 0xEB);

            SetHack_RawAxis();
            SetHack_CalibratedAxes();
            SetHack_GunButtons();

            Logger.WriteLog(MethodBase.GetCurrentMethod().Name + "() complete");
        }

        /// <summary>
        /// Change the float value used to be sent to the calibration procedure by our own
        /// 8 slots are used (0-based):
        /// - Slot 2 = P1 Reload
        /// - Slot 3 = P2 Reload
        /// - Slots 4,5,6,7 = P1x, P1y, P2x, P2y
        /// </summary>
        private void SetHack_RawAxis()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov eax,[eax*4+_Axes_CaveAddress]
            CaveMemory.Write_StrBytes("8B 04 85");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RawAxes_CaveAddress));

            //Inject it
            CaveMemory.InjectToAddress(_RawAxis_InjectionStruct, "Raw Axes");
        }

        /// <summary>
        /// At the end of that procedure, the game has moved the raw axis value to [-1.0, 1.0] axis value
        /// Overriding the result to align the crosshair with the gun sight
        /// 8 slots are used (0-based):
        /// - Slot 2 = P1 Reload
        /// - Slot 3 = P2 Reload
        /// - Slots 4,5,6,7 = P1x, P1y, P2x, P2y
        /// </summary>
        private void SetHack_CalibratedAxes()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx,[ebp+30]
            CaveMemory.Write_StrBytes("8B 5D 30");
            //shl ebx,02
            CaveMemory.Write_StrBytes("C1 E3 02");
            //add ebx,_CalibratedAxes_CaveAddress
            CaveMemory.Write_StrBytes("81 C3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CalibratedAxes_CaveAddress));
            //mov ebx,[ebx]
            CaveMemory.Write_StrBytes("8B 1B");
            //mov eax,[ebp+08]
            CaveMemory.Write_StrBytes("8B 45 08");
            //mov [eax+04],ebx
            CaveMemory.Write_StrBytes("89 58 04");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //mov eax,[eax]
            CaveMemory.Write_StrBytes("8B 00");

            //Inject it
            CaveMemory.InjectToAddress(_CalibratedAxis_InjectionStruct, "Calibrated Axes");
        }

        /// <summary>
        /// Gun buttons (= trigger only ?) is just a counter, incremented with each press/release
        /// There again, 8 slots are available (0-based) :
        /// - Slot 4 : P1 Trigger 
        /// - Slot 6 : P2 Trigger
        /// </summary>
        private void SetHack_GunButtons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //movzx eax,byte ptr [eax+_GunButtons_CaveAddress]
            CaveMemory.Write_StrBytes("0F B6 80");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_GunButtons_CaveAddress));

            //Inject it
            CaveMemory.InjectToAddress(_GunButtons_InjectionStruct, "Gun Buttons");
        }

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_RecoilStatus_CaveAddress = _OutputsDatabank_Address;
            _P2_RecoilStatus_CaveAddress = _OutputsDatabank_Address + 4;
            _P1_DamageStatus_CaveAddress = _OutputsDatabank_Address + 0x10;
            _P2_DamageStatus_CaveAddress = _OutputsDatabank_Address + 0x14;

            SetHack_Damage();
            SetHack_Recoil();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Intercep call to log damage taken by a player to set a custom flag to "1"
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //push edi
            CaveMemory.Write_StrBytes("57");
            //sub edi,01
            CaveMemory.Write_StrBytes("83 EF 01");
            //shl edi,02
            CaveMemory.Write_StrBytes("C1 E7 02");
            //add edi,BudgieLoader.exe+56789
            CaveMemory.Write_StrBytes("81 C7");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_DamageStatus_CaveAddress));
            //mov [edi],00000001
            CaveMemory.Write_StrBytes("C7 07 01 00 00 00");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //mov [esp+04],00000040
            CaveMemory.Write_StrBytes("C7 44 24 04 40 00 00 00");
            
            //Inject it
            CaveMemory.InjectToAddress(_Damage_InjectionStruct, "Damage");
        }

        /// <summary>
        /// Intercept call for checking ForceFeedback enabled setting on shoot
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();            
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //shl ebx,02
            CaveMemory.Write_StrBytes("C1 E3 02");
            //add ebx, _P1_RecoilStatus_CaveAddress
            CaveMemory.Write_StrBytes("81 C3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_RecoilStatus_CaveAddress));
            //mov [ebx],00000001
            CaveMemory.Write_StrBytes("C7 03 01 00 00 00");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");

            //mov [esp],088C735C
            CaveMemory.Write_StrBytes("C7 04 24 5C 73 8C 08");

            //Inject it
            CaveMemory.InjectToAddress(_Recoil_InjectionStruct, "Recoil");
        }


        /// <summary>
        /// Originally, changing the fld value loaded with our own -1.0f stored in databank
        /// Changing the parameter in the called function a bit further allow to do a simple Injection without custom stored value
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //lea edx,[esp+18]
            CaveMemory.Write_StrBytes("8D 54 24 18");
            //mov [edx],BF800000
            CaveMemory.Write_StrBytes("C7 02 00 00 80 BF");
            //mov [esp+04],edx
            CaveMemory.Write_StrBytes("89 54 24 04");

            //InjectIt
            CaveMemory.InjectToAddress(_NoCrosshair_InjectionStruct, "NoCrosshair");
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>  
        public override void SendInput(PlayerSettings PlayerData)
        {
            //Raw axes value
            byte[] bufferX = BitConverter.GetBytes(PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes(PlayerData.RIController.Computed_Y);
            
            //Float 'calibrated' axes values
            float cX = 1.0f * (float)PlayerData.RIController.Computed_X / 1024.0f;
            float cY = 1.0f * (float)PlayerData.RIController.Computed_Y / 1024.0f;

            if (PlayerData.ID == 1)
            {
                WriteBytes(_RawAxes_CaveAddress + 16, bufferX);
                WriteBytes(_RawAxes_CaveAddress + 20, bufferY);
                WriteBytes(_CalibratedAxes_CaveAddress + 16, BitConverter.GetBytes(cX));
                WriteBytes(_CalibratedAxes_CaveAddress + 20, BitConverter.GetBytes(cY));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    UInt32 ButtonAddress = _GunButtons_CaveAddress + 4;
                    byte b = ReadByte(ButtonAddress);
                    WriteByte(ButtonAddress, ++b);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    UInt32 ButtonAddress = _GunButtons_CaveAddress + 4;
                    byte b = ReadByte(ButtonAddress);
                    WriteByte(ButtonAddress, ++b);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    //Reload is an analog value, when player is pulling the lever. Max raw power is 0x3FF and calibrated power 1023.0f
                    WriteBytes(_RawAxes_CaveAddress + 8, BitConverter.GetBytes(0x000003FF));
                    WriteBytes(_CalibratedAxes_CaveAddress + 8, BitConverter.GetBytes((float)1023));
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    //Reload is an analog value, when player is pulling the lever. Min power is 0
                    WriteBytes(_RawAxes_CaveAddress + 8, BitConverter.GetBytes(0));
                    WriteBytes(_CalibratedAxes_CaveAddress + 8, BitConverter.GetBytes(0));
                }
            }

            else if (PlayerData.ID == 2)
            {
                WriteBytes(_RawAxes_CaveAddress + 24, bufferX);
                WriteBytes(_RawAxes_CaveAddress + 28, bufferY);
                WriteBytes(_CalibratedAxes_CaveAddress + 24, BitConverter.GetBytes(cX));
                WriteBytes(_CalibratedAxes_CaveAddress + 28, BitConverter.GetBytes(cY));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    UInt32 ButtonAddress = _GunButtons_CaveAddress + 6;
                    byte b = ReadByte(ButtonAddress);
                    WriteByte(ButtonAddress, ++b);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    UInt32 ButtonAddress = _GunButtons_CaveAddress + 6;
                    byte b = ReadByte(ButtonAddress);
                    WriteByte(ButtonAddress, ++b);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    //Reload is an analog value, when player is pulling the lever. Max raw power is 0x3FF and calibrated power 1023.0f
                    WriteBytes(_RawAxes_CaveAddress + 12, BitConverter.GetBytes(0x000003FF));
                    WriteBytes(_CalibratedAxes_CaveAddress + 12, BitConverter.GetBytes((float)1023));
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    //Reload is an analog value, when player is pulling the lever. Min power is 0
                    WriteBytes(_RawAxes_CaveAddress + 12, BitConverter.GetBytes(0));
                    WriteBytes(_CalibratedAxes_CaveAddress + 12, BitConverter.GetBytes(0));
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
            //Gun motor : Is activated permanently while trigger is pressed
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputId.P2_LmpStart));            
            _Outputs.Add(new GameOutput(OutputId.P1_LmpGun));
            _Outputs.Add(new GameOutput(OutputId.P2_LmpGun));
            _Outputs.Add(new GameOutput(OutputId.P1_LmpHolder));
            _Outputs.Add(new GameOutput(OutputId.P2_LmpHolder));
            _Outputs.Add(new GameOutput(OutputId.P1_LmpPanel));
            _Outputs.Add(new GameOutput(OutputId.P2_LmpPanel));
            _Outputs.Add(new GameOutput(OutputId.LmpRoof_R));
            _Outputs.Add(new GameOutput(OutputId.LmpRoof_G));
            _Outputs.Add(new GameOutput(OutputId.LmpRoof_B));
            _Outputs.Add(new GameOutput(OutputId.LmpLgMarquee));
            _Outputs.Add(new GameOutput(OutputId.LmpSqMarquee));
            _Outputs.Add(new GameOutput(OutputId.LmpWalker_R));
            _Outputs.Add(new GameOutput(OutputId.LmpWalker_G));
            _Outputs.Add(new GameOutput(OutputId.LmpWalker_B));
            _Outputs.Add(new GameOutput(OutputId.LmpWalkerEyes));
            _Outputs.Add(new GameOutput(OutputId.LmpWalkerCeiling));
            _Outputs.Add(new GameOutput(OutputId.LmpPosts));
            _Outputs.Add(new GameOutput(OutputId.LmpRear_R));
            
            //In Teknoparrot, Guns hardware is not emulated, so the game is not running original gun recoil procedures 
            /*_Outputs.Add(new GameOutput(OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputId.P2_GunMotor));*/
            _Outputs.Add(new GameOutput(OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputId.P1_Clip));
            _Outputs.Add(new GameOutput(OutputId.P2_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            //_Outputs.Add(new GameOutput(OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Lamp_Address + 0x18));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Lamp_Address + 0x1C));
            SetOutputValue(OutputId.P1_LmpGun, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address, 4), 0) * 200));
            SetOutputValue(OutputId.P2_LmpGun, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0x04, 4), 0) * 200));
            SetOutputValue(OutputId.P1_LmpHolder, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0x84, 4), 0) * 200));
            SetOutputValue(OutputId.P2_LmpHolder, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0xB4, 4), 0) * 200));
            SetOutputValue(OutputId.P1_LmpPanel, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0x7C, 4), 0) * 200));
            SetOutputValue(OutputId.P2_LmpPanel, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0x80, 4), 0) * 200));
            SetOutputValue(OutputId.LmpRoof_R, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0x88, 4), 0) * 200));
            SetOutputValue(OutputId.LmpRoof_G, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0x8C, 4), 0) * 200));
            SetOutputValue(OutputId.LmpRoof_B, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0x90, 4), 0) * 200));

            //Long Marquee uses 4 outputs, so we will add them to be sure it won't be missed if the game only triggers a part of them
            byte LongMarqueeStatus = 0;
            for (uint i = 0; i < 3; i++)
            {
                if (ReadByte(_Lamp_Address + 0x94 + 4 * i + 3) != 0x00)
                    LongMarqueeStatus |= 1;
            }
            SetOutputValue(OutputId.LmpLgMarquee, LongMarqueeStatus);
            SetOutputValue(OutputId.LmpSqMarquee, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0xA4, 4), 0) * 200));
            SetOutputValue(OutputId.LmpWalker_R, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0xA8, 4), 0) * 200));
            SetOutputValue(OutputId.LmpWalker_G, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0xAC, 4), 0) * 200));
            SetOutputValue(OutputId.LmpWalker_B, (int)(BitConverter.ToSingle(ReadBytes(_Lamp_Address + 0xB0, 4), 0) * 200));
            SetOutputValue(OutputId.LmpWalkerEyes, ReadByte(_Lamp_Address + 0x2C));
            SetOutputValue(OutputId.LmpWalkerCeiling, ReadByte(_Lamp_Address + 0x28));
            SetOutputValue(OutputId.LmpPosts, ReadByte(_Lamp_Address + 0x20));
            SetOutputValue(OutputId.LmpRear_R, ReadByte(_Lamp_Address + 0x24));

            //Custom Outputs
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            //Check if the Player is currently playing
            if (ReadByte(_P1_Struct_Address + 0x01) == 1)
            {
                _P1_Ammo = BitConverter.ToInt32(ReadBytes(_P1_Struct_Address + 0x6C, 4), 0);
                if (_P1_Ammo < 0)
                    _P1_Ammo = 0;

                //[Clip] custom Output   
                if (_P1_Ammo > 0)
                    P1_Clip = 1;

                _P1_Life = BitConverter.ToInt32(ReadBytes(_P1_Struct_Address + 0x10, 4), 0);
                if (_P1_Life < 0)
                    _P1_Life = 0;
            }

            //Check if the Player is currently playing
            if (ReadByte(_P2_Struct_Address + 0x01) == 1)
            {
                _P2_Ammo = BitConverter.ToInt32(ReadBytes(_P2_Struct_Address + 0x6C, 4), 0);
                if (_P2_Ammo < 0)
                    _P2_Ammo = 0;

                //[Clip] custom Output   
                if (_P2_Ammo > 0)
                    P2_Clip = 1;

                _P2_Life = BitConverter.ToInt32(ReadBytes(_P2_Struct_Address + 0x10, 4), 0);
                if (_P2_Life < 0)
                    _P2_Life = 0;
            }

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            //Reading our own flags for damage and recoil and resetting them
            if (ReadByte(_P1_RecoilStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                WriteByte(_P1_RecoilStatus_CaveAddress, 0);
            }
            if (ReadByte(_P2_RecoilStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte(_P2_RecoilStatus_CaveAddress, 0);
            }
            if (ReadByte(_P1_DamageStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P1_Damaged, 1);
                WriteByte(_P1_DamageStatus_CaveAddress, 0);
            }
            if (ReadByte(_P2_DamageStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P2_Damaged, 1);
                WriteByte(_P2_DamageStatus_CaveAddress, 0);
            }

            //SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
        
    }
}
