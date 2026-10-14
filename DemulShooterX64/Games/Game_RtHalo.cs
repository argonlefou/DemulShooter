using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.MemoryX64;
using DsCore.RawInput;

namespace DemulShooterX64.Games
{
    public class Game_RtHalo : Game
    {
        //Rom loaded + Rom version check
        private UInt64 _RomLoaded_Check_Instruction = 0x0053F28B;

        //Outputs Address
        private UInt64 _P1_Status_Address = 0x01C0C7C0;
        private UInt64 _P1_Life_Address = 0x01C0C7D4;
        private UInt64 _P1_Ammo_Address = 0x01C0C870;
        //private UInt32 _P2_Struct_Address = 0x09010274;
        private UInt64 _Lamp_Address = 0x01C0D1E0;

        //Custom Values
        private UInt64 _Axes_CaveAddress = 0;
        private UInt64 _Buttons_CaveAddress = 0;
        private UInt64 _RecoilStatus_CaveAddress = 0;
        private UInt64 _DamageStatus_CaveAddress = 0;


        /*** MEMORY ADDRESSES **/
        private InjectionStruct _Axes_InjectionStruct = new InjectionStruct(0x006241DB, 15);
        private InjectionStruct _Buttons_Injectionstruct = new InjectionStruct(0x004AD764, 18);
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x0042EB32, 15);
        private InjectionStruct _Damage_InjectionStruct = new InjectionStruct(0x00419954, 15);
        private InjectionStruct _NoCrosshair_InjectionStruct = new InjectionStruct(0x0044085F, 18);

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_RtHalo(String RomName)
            : base(RomName, "halo-loader64")
        {
            _KnownMd5Prints.Add("Halo Fireteam Raven - v3.11", "279f21163f0c3832d4d180b5e2671b58");

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
                            byte[] buffer = ReadBytes((IntPtr)_RomLoaded_Check_Instruction, 3);
                            if (buffer[0] == 0x8B && buffer[1] == 0x40 && buffer[2] == 0x50)
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Halo Fireteam Raven - v3.11 Detected");
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
            _Axes_CaveAddress = _InputsDatabank_Address;             //4 x 4 Bytes for X and 4 x 4  Bytes for Y => 32 Bytes
            _Buttons_CaveAddress = _InputsDatabank_Address + 0x20;   //Each button slot will take 16 Bytes, there are up to 72 slots => 1152 Bytes (0x480)

            SetHack_Axes();
            SetHack_Buttons();
        }

        /// <summary>
        /// Instead of calling 00DD6C60() to get float axes values, replacing by our own
        /// </summary>
        private void SetHack_Axes()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov rsi,[rsp+10]
            CaveMemory.Write_StrBytes("48 8B 74 24 10");
            //mov edi,ebx
            CaveMemory.Write_StrBytes("89 DF");
            //mov r13,rax
            CaveMemory.Write_StrBytes("49 89 C5");

            //mov rax,_Axes_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_CaveAddress));
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //mov rax,[rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //mov [rsi],rax
            CaveMemory.Write_StrBytes("48 89 06");

            //mov rax,_Axes_CaveAddress + 0x10
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axes_CaveAddress + 0x10));
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //add rax,rdi
            CaveMemory.Write_StrBytes("48 01 F8");
            //mov rax,[rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //mov [rdx],rax
            CaveMemory.Write_StrBytes("48 89 02");

            //Inject it
            CaveMemory.InjectToAddress(_Axes_InjectionStruct, "Axes");
        }

        /// <summary>
        /// Halo-Loader handles the inputs and sets memory values that are used by the game to detect button states.
        /// Inserting a custom-buttons-buffer to create similar key press data (increasing a byte each frame the key is pressed) to replace the original ones
        /// 
        /// This is not correct for 2 reasons :
        /// 1 - Key pressed should be increased by ms instead of by frames
        /// 2 - The "released" info is not properly hacked, meaning keys will continously send pressed messages (not bad for trigger, but others...no so good)
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            /*** Uncomment This to jump over the whole proc ***/
            //jmp
            //CaveMemory.Write_StrBytes("EB 60");

            /*** That check only applies the hack for Player Guns Buttons ***/
            //cmp r14,10
            CaveMemory.Write_StrBytes("49 83 FE 10");
            //jb OriginalCode
            CaveMemory.Write_StrBytes("72 5A");
            //cmp r14,2A
            CaveMemory.Write_StrBytes("49 83 FE 2A");
            //ja OriginalCode
            CaveMemory.Write_StrBytes("77 54");

            //mov r10,0000000000000010
            CaveMemory.Write_StrBytes("49 BA 10 00 00 00 00 00 00 00");
            //imul r10d,r14d
            CaveMemory.Write_StrBytes("45 0F AF D6");
            //mov rax,_Buttons_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //add rax,r10
            CaveMemory.Write_StrBytes("4C 01 D0");
            //cmp byte ptr [rax],01
            CaveMemory.Write_StrBytes("80 38 01");
            //je Pressed
            CaveMemory.Write_StrBytes("74 10");

            //mov [rax+04],00000000
            CaveMemory.Write_StrBytes("C7 40 04 00 00 00 00");
            //mov [rax+0C],00000000
            CaveMemory.Write_StrBytes("C7 40 0C 00 00 00 00");
            //jmp Copy
            CaveMemory.Write_StrBytes("EB 12");

            //Pressed:
            //mov [rax+04],00000001
            CaveMemory.Write_StrBytes("C7 40 04 01 00 00 00");
            //mov r10,[rax+0C]
            CaveMemory.Write_StrBytes("4C 8B 50 0C");
            //inc r10
            CaveMemory.Write_StrBytes("49 FF C2");
            //mov [rax+0C],r10
            CaveMemory.Write_StrBytes("4C 89 50 0C");

            //Copy:
            //mov r10d,[rax+04]
            CaveMemory.Write_StrBytes("44 8B 50 04");
            //mov [r12+04],r10d
            CaveMemory.Write_StrBytes("45 89 54 24 04");
            //mov r10d,[rax+0C]
            CaveMemory.Write_StrBytes("44 8B 50 0C");
            //mov [r12+0C],r10d
            CaveMemory.Write_StrBytes("45 89 54 24 0C");

            //OriginalCode
            //mov eax,[r12+0C]
            CaveMemory.Write_StrBytes("41 8B 44 24 0C");
            //cmp eax,[rbx+0C]
            CaveMemory.Write_StrBytes("3B 43 0C");
            //jna 
            CaveMemory.Write_StrBytes("76 03");
            //mov [rbx+0C],eax
            CaveMemory.Write_StrBytes("89 43 0C");
            //mov eax,[r12+04]
            CaveMemory.Write_StrBytes("41 8B 44 24 04");

            //Inect it
            CaveMemory.InjectToAddress(_Buttons_Injectionstruct, "Buttons");

        }

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _RecoilStatus_CaveAddress = _OutputsDatabank_Address;
            _DamageStatus_CaveAddress = _OutputsDatabank_Address + 0x04;

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

            //mov r9,_DamageStatus_CaveAddress
            CaveMemory.Write_StrBytes("49 B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_DamageStatus_CaveAddress));
            //add r9,r12
            CaveMemory.Write_StrBytes("4D 01 E1");
            //mov byte ptr [r9],01
            CaveMemory.Write_StrBytes("41 C6 01 01");

            //lea rdi,[rsp+20]
            CaveMemory.Write_StrBytes("48 8D 7C 24 20");
            //lea r9d,[r12+01]
            CaveMemory.Write_StrBytes("45 8D 4C 24 01");
            //mov esi,00000040
            CaveMemory.Write_StrBytes("BE 40 00 00 00");

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

            //mov rax,_RecoilStatus_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_RecoilStatus_CaveAddress));
            //add rax,rbp
            CaveMemory.Write_StrBytes("48 01 E8");
            //mov byte ptr [rax],01
            CaveMemory.Write_StrBytes("C6 00 01");
            //xor rax,rax
            CaveMemory.Write_StrBytes("48 31 C0");

            //mov ecx,00000040
            CaveMemory.Write_StrBytes("B9 40 00 00 00");
            //mov edx,00000001
            CaveMemory.Write_StrBytes("BA 01 00 00 00");
            //mov esi,00000040
            CaveMemory.Write_StrBytes("BE 40 00 00 00");

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

            //lea rdx,[rsp+20]
            CaveMemory.Write_StrBytes("48 8D 54 24 20");
            //lea rsi,[rsp+10]
            CaveMemory.Write_StrBytes("48 8D 74 24 10");
            //mov [rsp+18],00000000
            CaveMemory.Write_StrBytes("C7 44 24 18 00 00 00 00");
            //mov [rsi],BF800000
            CaveMemory.Write_StrBytes("C7 06 00 00 80 BF");

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
            ulong Index = (ulong)PlayerData.ID - 1;

            //Float 'calibrated' axes values
            float cX = 1.0f * (float)PlayerData.RIController.Computed_X / 1024.0f;
            float cY = 1.0f * (float)PlayerData.RIController.Computed_Y / 1024.0f;

            WriteBytes((IntPtr)(_Axes_CaveAddress + (4 * Index)), BitConverter.GetBytes(cX));
            WriteBytes((IntPtr)(_Axes_CaveAddress + 0x10 + (4 * Index)), BitConverter.GetBytes(cY));

            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                Set_InputSlot_State(0x10 + 8 * (int)Index, 1);
            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                Set_InputSlot_State(0x10 + 8 * (int)Index, 0);

            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                Set_InputSlot_State(0x11 + 8 * (int)Index, 1);
            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                Set_InputSlot_State(0x11 + 8 * (int)Index, 0);

            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                Set_InputSlot_State(0x12 + 8 * (int)Index, 1);
            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                Set_InputSlot_State(0x12 + 8 * (int)Index, 0);
        }

        private void Set_InputSlot_State(int SlotId, byte State)
        {
            UInt64 BufferIndex = (UInt64)(0x10 * SlotId);

            WriteByte((IntPtr)(_Buttons_CaveAddress + BufferIndex), State);
            if (State == 1)
                WriteByte((IntPtr)(_Buttons_CaveAddress + BufferIndex + 1), State);
            else
                WriteByte((IntPtr)(_Buttons_CaveAddress + BufferIndex + 2), State);            
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

            _Outputs.Add(new GameOutput(OutputId.Lmp_PodLeft_Panel));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodLeft_Panel_R));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodLeft_Panel_G));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodLeft_Panel_B));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodLeft_Panel_Front));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodLeft_Panel_Under));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodLeft_GunChase_R));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodLeft_GunChase_G));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodLeft_GunChase_B));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodRight_Panel));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodRight_Panel_R));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodRight_Panel_G));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodRight_Panel_B));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodRight_Panel_Front));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodRight_Panel_Under));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodRight_GunChase_R));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodRight_GunChase_G));
            _Outputs.Add(new GameOutput(OutputId.Lmp_PodRight_GunChase_B));
            _Outputs.Add(new GameOutput(OutputId.Lmp_Marquee_R));
            _Outputs.Add(new GameOutput(OutputId.Lmp_Marquee_G));
            _Outputs.Add(new GameOutput(OutputId.Lmp_Marquee_B));
            _Outputs.Add(new GameOutput(OutputId.Lmp_Speaker));
            _Outputs.Add(new GameOutput(OutputId.Lmp_Speaker_R));
            _Outputs.Add(new GameOutput(OutputId.Lmp_Speaker_G));
            _Outputs.Add(new GameOutput(OutputId.Lmp_Speaker_B));
            _Outputs.Add(new GameOutput(OutputId.Lmp_RearSoldier));
            _Outputs.Add(new GameOutput(OutputId.Lmp_MasterChief));
            _Outputs.Add(new GameOutput(OutputId.Lmp_MonitorStand));

            _Outputs.Add(new GameOutput(OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputId.P3_LmpStart));
            _Outputs.Add(new GameOutput(OutputId.P4_LmpStart));

            _Outputs.Add(new GameOutput(OutputId.Lmp_P1_Holster));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P1_Holster_R));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P1_Holster_G));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P1_Holster_B));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P2_Holster));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P2_Holster_R));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P2_Holster_G));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P2_Holster_B));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P3_Holster));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P3_Holster_R));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P3_Holster_G));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P3_Holster_B));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P4_Holster));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P4_Holster_R));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P4_Holster_G));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P4_Holster_B));

            _Outputs.Add(new GameOutput(OutputId.Lmp_P1_Side_R));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P1_Side_G));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P1_Side_B));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P2_Side_R));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P2_Side_G));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P2_Side_B));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P3_Side_R));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P3_Side_G));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P3_Side_B));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P4_Side_R));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P4_Side_G));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P4_Side_B));

            _Outputs.Add(new GameOutput(OutputId.Lmp_P1_GunAmmo));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P2_GunAmmo));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P3_GunAmmo));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P4_GunAmmo));

            _Outputs.Add(new GameOutput(OutputId.Lmp_P1_GunGrenade));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P2_GunGrenade));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P3_GunGrenade));
            _Outputs.Add(new GameOutput(OutputId.Lmp_P4_GunGrenade));

            //In Teknoparrot, Guns hardware is not emulated, so the game is not running original gun recoil procedures 
            /*_Outputs.Add(new GameOutput(OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputId.P2_GunMotor));*/
            _Outputs.Add(new GameOutput(OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputId.P3_Ammo));
            _Outputs.Add(new GameOutput(OutputId.P4_Ammo));
            _Outputs.Add(new AsyncGameOutput(OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputId.P3_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputId.P4_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputId.P2_Life));
            _Outputs.Add(new GameOutput(OutputId.P3_Life));
            _Outputs.Add(new GameOutput(OutputId.P4_Life));
            _Outputs.Add(new AsyncGameOutput(OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputId.P3_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputId.P4_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            //_Outputs.Add(new GameOutput(OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Original Outputs

            //Left POD
            SetOutputValue(OutputId.P1_LmpStart, 0);    //Start Lamp are not animated (!!)
            SetOutputValue(OutputId.P2_LmpStart, 0);
            SetOutputValue(OutputId.Lmp_PodLeft_Panel, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address, 4), 0) * 20));

            SetOutputValue(OutputId.Lmp_P1_Holster, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x04, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P1_Holster_R, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x08, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P1_Holster_G, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x0C, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P1_Holster_B, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x10, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P2_Holster, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x14, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P2_Holster_R, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x18, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P2_Holster_G, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x1C, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P2_Holster_B, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x20, 4), 0) * 20));

            SetOutputValue(OutputId.Lmp_P1_Side_R, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x24, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P1_Side_G, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x28, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P1_Side_B, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x2C, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P2_Side_R, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x30, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P2_Side_G, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x34, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P2_Side_B, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x38, 4), 0) * 20));

            SetOutputValue(OutputId.Lmp_P1_GunAmmo, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x50, 4), 0) * 20)); 
            SetOutputValue(OutputId.Lmp_P2_GunAmmo, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x54, 4), 0) * 20));

            SetOutputValue(OutputId.Lmp_PodLeft_Panel_R, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x44, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_PodLeft_Panel_G, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x48, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_PodLeft_Panel_B, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x4C, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_PodLeft_Panel_Front, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x64, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_PodLeft_Panel_Under, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x5C, 4), 0) * 20));

            //Right POD
            SetOutputValue(OutputId.P3_LmpStart, 0);    //Start Lamp are not animated (!!)
            SetOutputValue(OutputId.P4_LmpStart, 0);
            SetOutputValue(OutputId.Lmp_PodRight_Panel, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x80, 4), 0) * 20));

            SetOutputValue(OutputId.Lmp_P3_Holster, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x84, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P3_Holster_R, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x88, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P3_Holster_G, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x8C, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P3_Holster_B, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x90, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P4_Holster, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x94, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P4_Holster_R, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x98, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P4_Holster_G, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0x9C, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P4_Holster_B, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xA0, 4), 0) * 20));

            SetOutputValue(OutputId.Lmp_P3_Side_R, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xA4, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P3_Side_G, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xA8, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P3_Side_B, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xAC, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P4_Side_R, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xB0, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P4_Side_G, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xB4, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P4_Side_B, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xB8, 4), 0) * 20));

            SetOutputValue(OutputId.Lmp_P3_GunAmmo, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xD0, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_P4_GunAmmo, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xD4, 4), 0) * 20));

            SetOutputValue(OutputId.Lmp_Marquee_R, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xC4, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_Marquee_G, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xC8, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_Marquee_B, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xCC, 4), 0) * 20));

            SetOutputValue(OutputId.Lmp_RearSoldier, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xE4, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_Marquee_1, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xE0, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_MasterChief, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xDC, 4), 0) * 20));

            SetOutputValue(OutputId.Lmp_Speaker_R, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xE8, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_Speaker_B, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xEC, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_Speaker_G, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xF0, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_Speaker, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xC0, 4), 0) * 20));
            SetOutputValue(OutputId.Lmp_MonitorStand, (int)(BitConverter.ToSingle(ReadBytes((IntPtr)_Lamp_Address + 0xBC, 4), 0) * 20));

            //Custom Outputs           
            for (UInt64 i = 0; i < 4; i++)
            {
                //Status :
                //0 = Game Over
                //2 = Continue
                //3 = Playing
                byte PlayerStatus = ReadByte((IntPtr)(_P1_Status_Address + (i * 0x250)));

                if (PlayerStatus == 3)
                {
                    SetOutputValue((OutputId)((int)OutputId.P1_Ammo + i), BitConverter.ToInt32(ReadBytes((IntPtr)(_P1_Ammo_Address + (i * 0x250)), 4), 0));
                    SetOutputValue((OutputId)((int)OutputId.P1_Life + i), BitConverter.ToInt32(ReadBytes((IntPtr)(_P1_Life_Address + (i * 0x250)), 4), 0));
                }
                else
                {
                    SetOutputValue((OutputId)((int)OutputId.P1_Ammo + i), 0);
                    SetOutputValue((OutputId)((int)OutputId.P1_Life + i), 0);
                }

                //Reading our own flags for damage and recoil and resetting them
                if (ReadByte((IntPtr)(_RecoilStatus_CaveAddress + i)) == 1)
                {
                    SetOutputValue((OutputId)((int)OutputId.P1_CtmRecoil + (int)i), 1);
                    WriteByte((IntPtr)(_RecoilStatus_CaveAddress + i), 0);
                }

                if (ReadByte((IntPtr)(_DamageStatus_CaveAddress + i)) == 1)
                {
                    SetOutputValue((OutputId)((int)OutputId.P1_Damaged + (int)i), 1);
                    WriteByte((IntPtr)(_DamageStatus_CaveAddress + i), 0);
                }
            }

            //SetOutputValue(OutputId.Credits, ReadByte(_Credits_Address));
        }

        #endregion
    }
}
