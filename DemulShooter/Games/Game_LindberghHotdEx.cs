using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;

namespace DemulShooter
{
    class Game_LindberghHotdEx : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\lindbergh\hotdex";

        //Inputs       
        private InjectionStruct _JvsRawAxes_InjectionStruct = new InjectionStruct(0x084BD186, 7);
        private InjectionStruct _AdjustedAxes_InjectionStruct = new InjectionStruct(0x81039C1, 7);  //Not Called !
        private InjectionStruct _Buttons_InjectionStruct = new InjectionStruct(0x084BCF45, 7);

        //Outputs
        private UInt32 _JvsMgrPtr_Address = 0x0A9DFEBC;
        private UInt32 _GameMgrPtr_Address = 0xA9DFE98;
        private UInt32 _Credits_Address = 0xAA3DD80;
        private UInt32 _Freeplay_Address = 0xAA3DD67;

        //Custom Data
        private UInt32 _JvsRawAxes_CaveAddress;
        private UInt32 _AdjustedAxes_CaveAddress;
        private UInt32 _Buttons_CaveAddress;

        //Rom loaded + Rom version check
        private UInt32 _RomLoaded_Check_Address = 0x0815C65A;   //Intruction writing Lamps

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_LindberghHotdEx(String RomName)
            : base(RomName, "linuxloader")
        {

            _KnownMd5Prints.Add("House of The Dead EX (SBRC)", "69a85f6f2ba82e014cf7e3f65f7b3d2a");

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
                            if (_Target_Process_Name.ToLower().Contains("budgieloader"))
                            {
                                if (!FindGameWindow_Contains("HoD4"))
                                {
                                    Logger.WriteLog("Game Window not found, waiting...");
                                    return;
                                }
                            }
                            else if (_Target_Process_Name.ToLower().Contains("linuxloader"))
                            {
                                if (!FindGameWindow_Contains("FPS"))
                                {
                                    Logger.WriteLog("Game Window not found, waiting...");
                                    return;
                                }
                            }

                            //To make sure LinuxLoader has loaded the rom entirely, we're looking for some random instruction to be present in memory before starting                            
                            //And this instruction is also helping us detecting whether the game file is Rev.A or Rev.B or Rev.C binary, to call the corresponding hack
                            byte[] buffer = ReadBytes(_RomLoaded_Check_Address, 3);
                            if (buffer.SequenceEqual(new byte[] { 0x8B, 0x15, 0xBC }))
                            {
                                Logger.WriteLog("House Of The Dead EX (SBRC) binary detected");
                                _TargetProcess_Md5Hash = _KnownMd5Prints["House of The Dead EX (SBRC)"];
                            }
                            else
                            {
                                Logger.WriteLog("Game not Loaded, waiting...");
                                return;
                            }

                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
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

        #region Outputs

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

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _JvsRawAxes_CaveAddress = _InputsDatabank_Address;
            _AdjustedAxes_CaveAddress = _InputsDatabank_Address + 0x10;
            _Buttons_CaveAddress = _InputsDatabank_Address + 0x20;

            SetHack_Axes();
            //SetHack_AdjustedAxes();
            SetHack_Buttons();
        }

        /// <summary>
        /// At the end of amJvspAckAnalogInput(), replacing Axis value before memory copy
        /// </summary>
        private void SetHack_Axes()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //lea ecx,[ebx-B]
            CaveMemory.Write_StrBytes("8D 4B F5");
            //movzx ecx,word ptr [ecx+_Axes_CaveAddress]
            CaveMemory.Write_StrBytes("0F B6 89");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_JvsRawAxes_CaveAddress));
            //lea ebx,[ebx+esi+00000102]
            CaveMemory.Write_StrBytes("8D 9C 33 02 01 00 00");
            //mov [ebx],cx
            CaveMemory.Write_StrBytes("66 89 0B");

            //Inject it
            CaveMemory.InjectToAddress(_JvsRawAxes_InjectionStruct, "Axes");
        }

        /// <summary>
        /// At the end of amJvspAckSwInput(), removing the wanted buttons bit states from the source memory (Trigger, Reload, and Grenade)
        /// and changing the bits with custom values before memorycopy
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //cmp esi,05
            CaveMemory.Write_StrBytes("83 FE 05");
            //je originalcode
            CaveMemory.Write_StrBytes("74 28");
            //and byte ptr [edx+esi+00000102],FC
            CaveMemory.Write_StrBytes("80 A4 32 02 01 00 00 FC");
            //and byte ptr [edx+esi+00000103],7F
            CaveMemory.Write_StrBytes("80 A4 32 03 01 00 00 7F");
            //movzx ecx,word ptr [esi+_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("0F B7 8E");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //or [edx+esi+00000102],cl
            CaveMemory.Write_StrBytes("08 8C 32 02 01 00 00");
            //shr ecx,08
            CaveMemory.Write_StrBytes("C1 E9 08");
            //or [edx+esi+00000102],cl
            CaveMemory.Write_StrBytes("08 8C 32 03 01 00 00");
            //originalcode:
            //lea eax,[edx+esi+00000102]
            CaveMemory.Write_StrBytes("8D 84 32 02 01 00 00");

            //Inject it
            CaveMemory.InjectToAddress(_Buttons_InjectionStruct, "Buttons");
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
                WriteByte(_JvsRawAxes_CaveAddress, (byte)PlayerData.RIController.Computed_X);
                WriteByte(_JvsRawAxes_CaveAddress + 0x02, (byte)PlayerData.RIController.Computed_Y);

                //WriteBytes(_ComputedAxes_BaseAddress + 0xC8, BitConverter.GetBytes(X_Value));
                //WriteBytes(_ComputedAxes_BaseAddress + 0xCC, BitConverter.GetBytes(Y_Value));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 6, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 6, 0xFD);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 6, 0x08);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 6, 0xF7);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 6, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 6, 0xFE);
            }
            else if (PlayerData.ID == 2)
            {
                WriteByte(_JvsRawAxes_CaveAddress + 0x04, (byte)PlayerData.RIController.Computed_X);
                WriteByte(_JvsRawAxes_CaveAddress + 0x06, (byte)PlayerData.RIController.Computed_Y);

                // WriteBytes(_ComputedAxes_BaseAddress + 0xD0, BitConverter.GetBytes(X_Value));
                //WriteBytes(_ComputedAxes_BaseAddress + 0xD4, BitConverter.GetBytes(Y_Value));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 8, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 8, 0xFD);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 6, 0x04);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 6, 0xFB);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 8, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 8, 0xFE);
            }
        }

        #endregion

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : Is activated permanently while trigger is pressed
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputId.LmpPanel));
            _Outputs.Add(new GameOutput(OutputId.Lmp1));
            _Outputs.Add(new GameOutput(OutputId.Lmp2));
            _Outputs.Add(new GameOutput(OutputId.Lmp3));
            _Outputs.Add(new GameOutput(OutputId.Lmp4));
            _Outputs.Add(new GameOutput(OutputId.Lmp5));
            _Outputs.Add(new GameOutput(OutputId.Lmp6));
            _Outputs.Add(new GameOutput(OutputId.P1_LmpFoot));
            _Outputs.Add(new GameOutput(OutputId.P2_LmpFoot));
            _Outputs.Add(new GameOutput(OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            /*
            _Outputs.Add(new AsyncGameOutput(OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputId.P2_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));            
             */
            _Outputs.Add(new GameOutput(OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            UInt32 Outputs_Address = BitConverter.ToUInt32(ReadBytes(_JvsMgrPtr_Address, 4), 0);
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(Outputs_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.LmpPanel, ReadByte(Outputs_Address + 1) >> 7 & 0x01);
            SetOutputValue(OutputId.Lmp1, ReadByte(Outputs_Address + 1) >> 6 & 0x01);
            SetOutputValue(OutputId.Lmp2, ReadByte(Outputs_Address + 1) >> 5 & 0x01);
            SetOutputValue(OutputId.Lmp3, ReadByte(Outputs_Address + 1) >> 4 & 0x01);
            SetOutputValue(OutputId.Lmp4, ReadByte(Outputs_Address + 1) >> 3 & 0x01);
            SetOutputValue(OutputId.Lmp5, ReadByte(Outputs_Address + 1) >> 2 & 0x01);
            SetOutputValue(OutputId.Lmp6, ReadByte(Outputs_Address + 1) >> 1 & 0x01);
            SetOutputValue(OutputId.P1_LmpFoot, ReadByte(Outputs_Address) >> 5 & 0x01);
            SetOutputValue(OutputId.P2_LmpFoot, ReadByte(Outputs_Address) >> 2 & 0x01);            

            //Custom Outputs
            int P1_Life = 0;
            int P2_Life = 0;

            UInt32 GameManager_Address = ReadPtr(_GameMgrPtr_Address);
            if (GameManager_Address != 0)
            {
                P1_Life = ReadByte(GameManager_Address + 0x48);
                if (P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                P2_Life = ReadByte(GameManager_Address + 0x4C);
                if (P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);
            }

            _P1_LastLife = P1_Life;
            _P2_LastLife = P2_Life;

            

            /*_P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;
            if (ReadPtr(_GameInfos_Address) != 0)
            {
                _Life = ReadByte(ReadPtr(_GameInfos_Address) + 0x40);
                //[Damaged] custom Output                
                if (_Life < _LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                _P1_Ammo = ReadByte(ReadPtrChain(_GameInfos_Address, new UInt32[] { 0x34 }) + 0x2D4);
                //[Clip] custom Output   
                if (_P1_Ammo > 0)
                    P1_Clip = 1;
                //No attract mode so we can just reuse the original recoil flag
                SetOutputValue(OutputId.P1_CtmRecoil, P1_Motor_Status);

                _P2_Ammo = ReadByte(ReadPtrChain(_GameInfos_Address, new UInt32[] { 0x38 }) + 0x2D4);
                //[Clip] custom Output   
                if (_P2_Ammo > 0)
                    P2_Clip = 1;
                //No attract mode so we can just reuse the original recoil flag
                SetOutputValue(OutputId.P2_CtmRecoil, P2_Motor_Status);
            }

            _LastLife = _Life;
            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _Life);*/

            SetOutputValue(OutputId.P1_Life, P1_Life);
            SetOutputValue(OutputId.P2_Life, P2_Life);

            //Displaying Credits if no FREEPLAY enabled
            if (ReadByte(_Freeplay_Address) == 0)
                SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
            else
                SetOutputValue(OutputId.Credits, 0);
        }

        #endregion
    }
}
