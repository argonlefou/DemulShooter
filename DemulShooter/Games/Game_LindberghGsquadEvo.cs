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
    class Game_LindberghGsquadEvo : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\lindbergh\ghostsev";

        //@80e8780 -> copy source in multiple places. desactivating copy stops input to be updated...

        //Inputs
        private UInt32 _ComputedAxes_BaseAddress = 0x086553AC;  //-> store float [-1;1] in +C8, +CC, +D0,+D4, but still offset from calibration
        private NopStruct _Nop_ComputedAxes = new NopStruct(0x80E86CA, 9);
        private InjectionStruct _JvsRawAxes_InjectionStruct = new InjectionStruct(0x08185951, 7);
        private InjectionStruct _Buttons_InjectionStruct = new InjectionStruct(0x08185710, 7);

        //MEMORY ADDRESSES
        //private UInt32 _GameMode_Address = 0x08660420;
        private UInt32 _P1_GameState_Address = 0x086617E8;
        private UInt32 _P2_GameState_Address = 0x8661994;
        private UInt32 _P1_LifePtr_Address = 0x086618D4;
        private UInt32 _P2_LifePtr_Address = 0x08661A80;
        private UInt32 _Outputs_Address = 0x08656358;
        private UInt32 _Credits_Address = 0x00AE9FBA0;
        private InjectionStruct _PlayerDamage_Injection = new InjectionStruct(0x810C9D9, 6);
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x080E52EC, 6);

        //Custom Data
        private UInt32 _JvsRawAxes_CaveAddress;
        private UInt32 _Buttons_CaveAddress;
        private UInt32 _Damage_CaveAddress;
        private UInt32 _Recoil_Caveaddress;

        private UInt32 _RomLoaded_Check_Address = 0x0807C9A0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_LindberghGsquadEvo(String RomName)
            : base(RomName, "linuxloader")
        {
            _KnownMd5Prints.Add("Ghost Squad Evolution (SBNJ) (Rev.A)", "bf8fcf04d6d90f9f5dd33ca76f1b670a");

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
                                if (!FindGameWindow_Contains("TeknoBudgie") && !FindGameWindow_Contains("FREEGLUT"))
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
                            if (buffer.SequenceEqual(new byte[] { 0xA1, 0x90, 0x05 }))
                            {
                                Logger.WriteLog("Ghost Squad Evolution (Rev .A) binary detected");
                                _TargetProcess_Md5Hash = _KnownMd5Prints["Ghost Squad Evolution (SBNJ) (Rev.A)"];
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
            _Buttons_CaveAddress = _InputsDatabank_Address + 0x10;

            SetHack_Axes();
            SetHack_Buttons();

            //Removing post-calibration storage of float data
            //SetNops(0, _Nop_ComputedAxes);
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

        #region Outputs Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _Damage_CaveAddress = _OutputsDatabank_Address;
            _Recoil_Caveaddress = _OutputsDatabank_Address + 0x04;

            SetHack_Recoil();
            SetHack_Damage();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        //Just reading the JVS output bytes miss some bullets recoil on x3 or auto fire
        //Instead, getting the signal directly from the set_gun_reaction() call
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[esp+08]
            CaveMemory.Write_StrBytes("8B 44 24 08");
            //add eax,_Recoil_Caveaddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Recoil_Caveaddress));
            //mov byte ptr [eax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //push ebp
            CaveMemory.Write_StrBytes("55");
            //mov ebp,esp
            CaveMemory.Write_StrBytes("8B EC");
            //sub esp,18
            CaveMemory.Write_StrBytes("83 EC 18");

            //Inject it it
            CaveMemory.InjectToAddress(_Recoil_InjectionStruct, "Recoil");
        }

        //Intercept a call to set_player_damage_internal() function to get dammage event
        //Player Index is in edx
        private void SetHack_Damage()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[ebp+08]
            CaveMemory.Write_StrBytes("8B 45 08");
            //add eax,_Damage_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Damage_CaveAddress));
            //mov byte ptr [eax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //mov [ebx+000000B8],edx
            CaveMemory.Write_StrBytes("89 93 B8 00 00 00");

            //Inject it it
            CaveMemory.InjectToAddress(_PlayerDamage_Injection, "Damage");
        }

        #endregion

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary> 
        public override void SendInput(PlayerSettings PlayerData)
        {
            float X_Value = (2.0f * (float)PlayerData.RIController.Computed_X / 255.0f) - 1.0f;
            float Y_Value = (2.0f * (float)PlayerData.RIController.Computed_Y / 255.0f) - 1.0f;

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
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 7, 0x80);  //ACTION
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 7, 0x40);  //CHANGE WEAPON
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 7, 0x7F);
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 7, 0xBF);
                }

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
                {
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 9, 0x80);  //ACTION
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 9, 0x40);  //CHANGE WEAPON
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 9, 0x7F);
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 9, 0xBF);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Buttons_CaveAddress + 8, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Buttons_CaveAddress + 8, 0xFE);
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
            _Outputs.Add(new GameOutput(OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputId.P1_LmpHolder));
            _Outputs.Add(new GameOutput(OutputId.P2_LmpHolder));
            _Outputs.Add(new GameOutput(OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputId.P2_GunRecoil));
            _Outputs.Add(new AsyncGameOutput(OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Outputs_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_LmpHolder, ReadByte(_Outputs_Address) >> 5 & 0x01);
            SetOutputValue(OutputId.P2_LmpHolder, ReadByte(_Outputs_Address) >> 2 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, ReadByte(_Outputs_Address) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, ReadByte(_Outputs_Address) >> 3 & 0x01);

            //Custom Outputs
            _P1_Life = 0;
            _P2_Life = 0;

            // Checking the Player state to not trigger Damage event during Attract mode:
            // 0x00 = Not playing
            // 0x02 = In-Game
            // 0x08 = Continue
            // 0x10 = Game-Over
            // 0x80 = Attract Mode
            if (ReadByte(_P1_GameState_Address) == 2)
            {
                _P1_Life = ReadByte(ReadPtr(_P1_LifePtr_Address) + 0xB8);
                //P1_Ammo = ReadByte(P1_StructAddress + 0x400);

                //[Damaged] custom Output                
                if (ReadByte(_Damage_CaveAddress) == 1)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                //[Recoil] custom Output
                if (ReadByte(_Recoil_Caveaddress) == 1)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);
            }
            WriteByte(_Damage_CaveAddress, 0);
            WriteByte(_Recoil_Caveaddress, 0);

            // Checking the Player state to not trigger Damage event during Attract mode:
            // 0x00 = Not playing
            // 0x02 = In-Game
            // 0x08 = Continue
            // 0x10 = Game-Over
            // 0x80 = Attract Mode
            if (ReadByte(_P2_GameState_Address) == 2)
            {
                _P2_Life = ReadByte(ReadPtr(_P2_LifePtr_Address) + 0xB8);
                //P1_Ammo = ReadByte(P1_StructAddress + 0x400);

                //[Damaged] custom Output                
                if (ReadByte(_Damage_CaveAddress + 1) == 1)
                    SetOutputValue(OutputId.P2_Damaged, 1);

                //[Recoil] custom Output
                if (ReadByte(_Recoil_Caveaddress + 1) == 1)
                    SetOutputValue(OutputId.P1_CtmRecoil + 1, 1);
            }
            WriteByte(_Damage_CaveAddress + 1, 0);
            WriteByte(_Recoil_Caveaddress + 1, 0);

            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            SetOutputValue(OutputId.Credits, (int)(ReadByte(_Credits_Address)));
        }

        #endregion
    }
}
