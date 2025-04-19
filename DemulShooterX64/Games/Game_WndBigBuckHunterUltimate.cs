using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.MemoryX64;
using DsCore.Win32;
using DsCore.RawInput;

namespace DemulShooterX64
{
    public class Game_WndBigBuckHunterUltimate : Game
    {
        /*** MEMORY ADDRESSES **/
        private UInt64 _Hud_SetReticleVisible_Function_Offset = 0x0060C320;
        private UInt64 _BaseFirearm_ToggleRendering_Function_Offset = 0x0067B890;
        private UInt64 _Cursor_SetVisible_Function_Offset = 0x0236CA10;
        private UInt64 _ControlManager_ValidateControler_Function_Offset = 0x0066F520;
        private UInt64 _ForceUseMouse_Offset = 0x006762BE;
        private UInt64 _ForceWeaponRenderingOff_Offset = 0x0067B9C9;
        private NopStruct _Nop_ValidateController = new NopStruct(0x00066F62D, 2);
        private NopStruct _Nop_PlayMuzzleFlash = new NopStruct(0x00067CD0C, 5);
        private InjectionStruct _Axis_InjectionStruct = new InjectionStruct(0x00676506, 5);
        private InjectionStruct _Buttons_InjectionStruct = new InjectionStruct(0x00678DB5, 15);
        private InjectionStruct _NoGuns_InjectionStruct = new InjectionStruct(0x0067A231, 14);
        private InjectionStruct _NoCrosshair_InjectionStruct = new InjectionStruct(0x0060C008, 7);
        private InjectionStruct _NoCursor_InjectionStruct = new InjectionStruct(0x0067407C, 7);
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x0067A859, 17);
        private UInt64 _Axis_TrampolineJmp_Offset = 0x006766E1;
        private UInt64 _NoCrosshair_TrampolineJmp_Offset = 0x0060DE60;
        private UInt64 _NoCursor_TrampolineJmp_Offset = 0x006740F2;

        //Custom Values
        private UInt64 _P1_Axis_CaveAddress = 0;
        private UInt64 _P2_Axis_CaveAddress = 0;
        private UInt64 _Buttons_CaveAddress = 0;

        //Outputs
        private UInt64 _Recoil_CaveAddress = 0;

        private IntPtr _GameAssemblyDll_BaseAddress = IntPtr.Zero;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndBigBuckHunterUltimate(String RomName) : 
            base(RomName, "BBH")
        {
            _KnownMd5Prints.Add("Big Buck Hunter Ultimate Trophy - Original release", "344902fbe7cc36087212a38f329123f7");
            _tProcess.Start();

            Logger.WriteLog("Waiting for Windows Game " + _RomName + " game to hook.....");
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

                        //Looking for the game's window based on it's Title
                        _GameWindowHandle = IntPtr.Zero;
                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            ProcessModuleCollection c = _TargetProcess.Modules;
                            foreach (ProcessModule m in c)
                            {
                                if (m.ModuleName.ToLower().Equals("gameassembly.dll"))
                                {
                                    _GameAssemblyDll_BaseAddress = m.BaseAddress;
                                    if (_GameAssemblyDll_BaseAddress != IntPtr.Zero)
                                    {
                                        // The game may start with other Windows than the main one (BepInEx console, other stuff.....) so we need to filter
                                        // the displayed window according to the Title, if DemulShooter is started before the game,  to hook the correct one
                                        if (FindGameWindow_Equals("BigBuckHunter_UltimateTrophy"))
                                        {
                                            String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", "GameAssembly.dll");
                                            CheckMd5(AssemblyDllPath);
                                            Apply_MemoryHacks();
                                            Apply_NoCursorHack();
                                            if (_HideGuns)
                                                Apply_NoGunsMemoryHack();
                                            _ProcessHooked = true;
                                            RaiseGameHookedEvent();
                                        }
                                        else
                                        {
                                            Logger.WriteLog("Game Window not found");
                                            return;
                                        }
                                    }
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

        public override bool GameScale(PlayerSettings PlayerData)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //Coordinates are Window size range, but inverted Y
                    int X_Value = PlayerData.RIController.Computed_X;
                    int Y_Value = (int)TotalResY - PlayerData.RIController.Computed_Y;

                    if (X_Value < 0)
                        X_Value = 0;
                    if (Y_Value < 0)
                        Y_Value = 0;
                    if (X_Value > (int)TotalResX)
                        X_Value = (int)TotalResX;
                    if (Y_Value > (int)TotalResY)
                        Y_Value = (int)TotalResY;

                    PlayerData.RIController.Computed_X = X_Value;
                    PlayerData.RIController.Computed_Y = Y_Value;

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
            _P1_Axis_CaveAddress = _InputsDatabank_Address;
            _P2_Axis_CaveAddress = _InputsDatabank_Address + 0x08;
            _Buttons_CaveAddress = _InputsDatabank_Address + 0x10;

            SetHack_InGameAxis();
            SetHack_Buttons();

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }
        
        private void SetHack_InGameAxis()
        {
            //ControllerManager.Validatecontroller() force return 1 even if call to Rewired.Player.get_JoystickCount is 0
            //SetNops(_GameAssemblyDll_BaseAddress, _Nop_ValidateController);

            //Replace ControllerManager.Validatecontroller() function by return 1
            WriteBytes((IntPtr)((UInt64)_GameAssemblyDll_BaseAddress + _ControlManager_ValidateControler_Function_Offset), new byte[] { 0x48, 0xB8, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC3, 0x90, 0x90 });
            
            //Forcing BBH_character.PlayerAvatar.GetInputScreenPos() to use Mouse Cursor Position
            WriteBytes((IntPtr)((UInt64)_GameAssemblyDll_BaseAddress + _ForceUseMouse_Offset), new byte[] { 0xE9, 0xBE, 0x01, 0x00, 0x00, 0x90});

            //At that point the BBH_character.PlayerAvatar.GetInputScreenPos() will get MousePosition and store it
            //as Vector2 in [RBX+48] / [RBX+4C]
            //Replacing values with our custom ones
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push rcx
            CaveMemory.Write_StrBytes("51");
            //mov rcx,[rbx+70]
            CaveMemory.Write_StrBytes("48 8B 4B 70");
            //movzx rcx,byte ptr [rcx+24]
            CaveMemory.Write_StrBytes("48 0F B6 49 24");
            //shl rcx,03
            CaveMemory.Write_StrBytes("48 C1 E1 03");
            //mov rax, _P1_Axis_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Axis_CaveAddress));
            //add rax,rcx
            CaveMemory.Write_StrBytes("48 01 C8");
            //mov rax,[rax]
            CaveMemory.Write_StrBytes("48 8B 00");
            //mov [rbx+48],rax
            CaveMemory.Write_StrBytes("48 89 43 48");
            //pop rcx
            CaveMemory.Write_StrBytes("59");

            //Inject it
            CaveMemory.InjectToOffset_WithTrampoline(_Axis_InjectionStruct, _Axis_TrampolineJmp_Offset, "Axis");
             
        }

        /// <summary>
        /// At the end of PlayerAvatar.GetButtonDown(), we replace the return value by our own
        /// </summary>
        private void SetHack_Buttons()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov rbx,[rdi+70]
            CaveMemory.Write_StrBytes("48 8B 5F 70");
            //movzx rbx,byte ptr [rbx+28]
            CaveMemory.Write_StrBytes("48 0F B6 5B 28");
            //shl rbx,04
            CaveMemory.Write_StrBytes("48 C1 E3 04");
            //mov rax, _Buttons_CaveAddress
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Buttons_CaveAddress));
            //add rax,rbx
            CaveMemory.Write_StrBytes("48 01 D8");
            //add rax,rbp
            CaveMemory.Write_StrBytes("48 01 E8");
            //mov rbx,rax
            CaveMemory.Write_StrBytes("48 8B D8");
            //xor rax,rax
            CaveMemory.Write_StrBytes("48 31 C0");
            //mov al,[rbx]
            CaveMemory.Write_StrBytes("8A 03");
            //mov rbx,[rsp+40]
            CaveMemory.Write_StrBytes("48 8B 5C 24 40");
            //mov rbp,[rsp+48]
            CaveMemory.Write_StrBytes("48 8B 6C 24 48");
            //mov rsi,[rsp+50]
            CaveMemory.Write_StrBytes("48 8B 74 24 50");

            //InjectIt
            CaveMemory.InjectToOffset(_Buttons_InjectionStruct, "Buttons");
        }

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _Recoil_CaveAddress = _OutputsDatabank_Address;
            SetHack_Recoil();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Intercepting recoil event in BaseFirearm.fire() procedure
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov r8d,[r8+28]
            CaveMemory.Write_StrBytes("45 8B 40 28");
            //mov r9, _Recoil_CaveAddress
            CaveMemory.Write_StrBytes("49 B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Recoil_CaveAddress));
            //add r9,r8
            CaveMemory.Write_StrBytes("4D 01 C1");
            //mov byte ptr [r9],01
            CaveMemory.Write_StrBytes("41 C6 01 01");
            //xor r9d,r9d
            CaveMemory.Write_StrBytes("45 33 C9");
            //xor edx,edx
            CaveMemory.Write_StrBytes("33 D2");
            //mov rcx,rax
            CaveMemory.Write_StrBytes("48 8B C8");

            //Inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
        }

        /// <summary>
        /// Inserting a call at the end of Hud.Update() procedure to force call Hud.SetReticleVisible(false)
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push rdx
            CaveMemory.Write_StrBytes("52");
            //push r8
            CaveMemory.Write_StrBytes("41 50");
            //xor r8,r8
            CaveMemory.Write_StrBytes("4D 31 C0");
            //xor rdx,rdx
            CaveMemory.Write_StrBytes("48 31 D2");
            //mov rcx,rbx
            CaveMemory.Write_StrBytes("48 8B CB");
            
            /*//mov rax, GameAssembly.dll+60C320
            CaveMemory.Write_StrBytes("48 B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt64)_GameAssemblyDll_BaseAddress + _Hud_SetReticleVisible_Function_Offset));
            //call rax
            CaveMemory.Write_StrBytes("FF D0");*/

            //Call Hud.SetReticleVisible()
            CaveMemory.Write_call_absolute((UInt64)_GameAssemblyDll_BaseAddress + _Hud_SetReticleVisible_Function_Offset);            
            //pop r8
            CaveMemory.Write_StrBytes("41 58");
            //pop rdx
            CaveMemory.Write_StrBytes("5A");
            //add rsp,00000090
            CaveMemory.Write_StrBytes("48 81 C4 90 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset_WithTrampoline(_NoCrosshair_InjectionStruct, _NoCrosshair_TrampolineJmp_Offset, "No Cursor");
        }


        private void Apply_NoGunsMemoryHack()
        {
            //First, noping the call to PlayMuzzleFlash() to remove muzzle flash effect 
            SetNops(_GameAssemblyDll_BaseAddress, _Nop_PlayMuzzleFlash);

            //Then force Basefirearm.ToggleRendering() to always have FALSE as value
            WriteBytes((IntPtr)((UInt64)_GameAssemblyDll_BaseAddress + _ForceWeaponRenderingOff_Offset), new byte[] { 0x30, 0xD2, 0x90 });

            //Last, injecting in Basefirearm.Update() procedure a call to the modified Basefirearm.ToggleRendering()
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov rcx,rbx
            CaveMemory.Write_StrBytes("48 8B CB");
            //call BaseFirearm.ToggleRendering()
            CaveMemory.Write_call_absolute((UInt64)_GameAssemblyDll_BaseAddress + _BaseFirearm_ToggleRendering_Function_Offset);
            //movaps xmm6,[rsp+20]
            CaveMemory.Write_StrBytes("0F 28 74 24 20");
            //mov rbx,[rsp+40]
            CaveMemory.Write_StrBytes("48 8B 5C 24 40");
            //add rsp,30
            CaveMemory.Write_StrBytes("48 83 C4 30");

            //Inject it
            CaveMemory.InjectToOffset(_NoGuns_InjectionStruct, "No Guns");
        }

        /// <summary>
        /// Adding a call to Cursor.SetVisible(False) in SteamManager.Update() procedure
        /// Choosing that procedure because it's started fast at start and always runs in game
        /// </summary>
        private void Apply_NoCursorHack()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //xor rcx,rcx
            CaveMemory.Write_StrBytes("48 31 C9");
            //call Cursor.SetVisible(False)
            CaveMemory.Write_call_absolute((UInt64)_GameAssemblyDll_BaseAddress + _Cursor_SetVisible_Function_Offset);
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //mov rcx,[rax+000000B8]
            CaveMemory.Write_StrBytes("48 8B 88 B8 00 00 00");
            
            //Inject it
            CaveMemory.InjectToOffset_WithTrampoline(_NoCursor_InjectionStruct, _NoCursor_TrampolineJmp_Offset, "No Cursor");
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>  
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((float)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((float)PlayerData.RIController.Computed_Y);

            int PlayerAxisIndex = (PlayerData.ID - 1) * 8;
            int PlayerButtonsIndex = (PlayerData.ID - 1) * 0x10;

            WriteBytes((IntPtr)(_P1_Axis_CaveAddress + (UInt64)PlayerAxisIndex), bufferX);
            WriteBytes((IntPtr)(_P1_Axis_CaveAddress + (UInt64)PlayerAxisIndex + 4), bufferY);

            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
            {
                WriteByte((IntPtr)(_Buttons_CaveAddress + (UInt64)PlayerButtonsIndex + 0x02), 1);
                WriteByte((IntPtr)(_Buttons_CaveAddress + (UInt64)PlayerButtonsIndex + 0x04), 1);
            }
            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
            {
                WriteByte((IntPtr)(_Buttons_CaveAddress + (UInt64)PlayerButtonsIndex + 0x02), 0);
                WriteByte((IntPtr)(_Buttons_CaveAddress + (UInt64)PlayerButtonsIndex + 0x04), 0);
            }

            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
            {
                WriteByte((IntPtr)(_Buttons_CaveAddress + (UInt64)PlayerButtonsIndex + 0x0C), 1);
            }
            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
            {
                WriteByte((IntPtr)(_Buttons_CaveAddress + (UInt64)PlayerButtonsIndex + 0x0C), 0);
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : Is activated for every bullet fired AND when player gets
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            if (ReadByte((IntPtr)(_Recoil_CaveAddress)) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                WriteByte((IntPtr)(_Recoil_CaveAddress), 0x00);
            }

            if (ReadByte((IntPtr)(_Recoil_CaveAddress + 1)) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte((IntPtr)(_Recoil_CaveAddress + 1), 0x00);
            }
        }

        #endregion


    }
}
