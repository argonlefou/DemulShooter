using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;

namespace DemulShooter
{
    class Game_ArcadepcHsfr : Game
    {
        /*** MEMORY ADDRESSES **/
        private InjectionStruct _UAfControlManager_MoveGunController_InjectionStruct = new InjectionStruct(0x0042E9DB, 5);

        //Outputs
        private InjectionStruct _UafGameManager_Update_InjectionStruct = new InjectionStruct(0x0043A14A, 6);
        private InjectionStruct _UafControlManager_OpenLight_InjectionStruct = new InjectionStruct(0x00428844, 6);
        private InjectionStruct _UafControlManager_CloseLight_InjectionStruct = new InjectionStruct(0x00428A34, 6);
        private InjectionStruct _UafControlManager_StartSeatShake_InjectionStruct = new InjectionStruct(0x00430CB8, 6);
        private InjectionStruct _UafControlManager_CloseSeatShake_InjectionStruct = new InjectionStruct(0x00430E48, 6);
        private InjectionStruct _UafControlManager_StartShakeApparatusOut_InjectionStruct = new InjectionStruct(0x00430A48, 6);
        private InjectionStruct _UafControlManager_CloseShake_InjectionStruct = new InjectionStruct(0x00430978, 6);
        private InjectionStruct _UafControlManager_WaterMistControl_InjectionStruct = new InjectionStruct(0x0042FA03, 6);
        private InjectionStruct _UafControlManager_Update_InjectionStruct = new InjectionStruct(0x00428DB2, 5);
        private InjectionStruct _SgPlayer_PlayEffectHitted_InjectionStruct = new InjectionStruct(0x003BD333, 6);

        //Custom Input Address
        private UInt32 _AxisX_Array_CaveAddress;
        private UInt32 _AxisY_Array_CaveAddress;
        private UInt32 _Light_Array_CaveAddress;
        private UInt32 _Motor_Array_CaveAddress;
        private UInt32 _DamageArray_CaveAddress;
        private UInt32 _UafGameManager_Instance_CaveAddress;
        private UInt32 _UafControlManager_Instance_CaveAddress;
        private UInt32 _WaterMistState_CaveAddress;

        private IntPtr _GameAssemblyDll_BaseAddress = IntPtr.Zero;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_ArcadepcHsfr(String RomName)
            : base(RomName, "FireHero")
        {
            _KnownMd5Prints.Add("Hot Shots Fire Rescue - v3.3.20221123r.60.1PUMP", "eec51a30066a2a19fa7d3ad494518eab");
            _tProcess.Start();
            Logger.WriteLog("Waiting for Coastal " + _RomName + " game to hook.....");
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
                                        if (FindGameWindow_Equals("FireHero"))
                                        {
                                            String AssemblyDllPath = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", "GameAssembly.dll");
                                            CheckMd5(AssemblyDllPath);
                                            Apply_MemoryHacks();
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

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// Game is using 2 differents axis :
        /// 1 For the shooting target and collision (which is the window size boundaries)
        /// 1 To display crosshair on screen (which seems to be 1920 for X and Y changes according to the window ratio
        /// Origin is Bottom-Left (inverted from Windows Lightgun data origins, Top-Left)
        /// The regular PlayerData.RIController.Computed_X and PlayerData.RIController.Computed_Y will store shooting values
        /// Other private fields will store Crosshair display values
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

                    double dRatio = TotalResX / TotalResY;
                    Logger.WriteLog("Game Window ratio = " + dRatio);

                    //Crosshair X => [-960, 960]
                    //Crosshair Y => [-540, 540]
                    double dMinX = -960.0;
                    double dMaxX = 960.0;
                    double dMinY = -540.0;
                    double dMaxY = 540.0;
                    double dRangeX = dMaxX - dMinX;
                    double dRangeY = dMaxY - dMinY;

                    PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dRangeX * PlayerData.RIController.Computed_X / TotalResX) - dRangeX / 2);
                    PlayerData.RIController.Computed_Y = Convert.ToInt16((Math.Round(dRangeY * PlayerData.RIController.Computed_Y / TotalResY) - dRangeY / 2) * -1);
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
            _AxisX_Array_CaveAddress = _InputsDatabank_Address;
            _AxisY_Array_CaveAddress = _InputsDatabank_Address + 0x10;

            SetHack_Axis();

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Inside UAF_ControllerManager.MoveGunController(), if CONTROL_TYPE is keyboard, the game waits for a KEY to be pressed to update Values
        /// Removing that part and inserting our own values, forcing a TRUE return (UAF_ControllerManager.flag = 1)
        /// </summary>
        private void SetHack_Axis()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov byte ptr [esi+000001D4],01
            CaveMemory.Write_StrBytes("C6 86 D4 01 00 00 01");
            //mov eax,[ebp+1C]
            CaveMemory.Write_StrBytes("8B 45 1C");

            //mov esi,_AxisX_Array_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AxisX_Array_CaveAddress));
            //mov esi,[esi+ebx*4]
            CaveMemory.Write_StrBytes("8B 34 9E");
            //mov [eax],esi
            CaveMemory.Write_StrBytes("89 30");
            //mov esi,_AxisY_Array_CaveAddress
            CaveMemory.Write_StrBytes("BE");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_AxisY_Array_CaveAddress));
            //mov esi,[esi+ebx*4]
            CaveMemory.Write_StrBytes("8B 34 9E");
            //mov [eax+04],esi
            CaveMemory.Write_StrBytes("89 70 04");
            //mov eax,00000001
            CaveMemory.Write_StrBytes("B8 01 00 00 00");
            //pop edi
            CaveMemory.Write_StrBytes("5F");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //mov esp,ebp
            CaveMemory.Write_StrBytes("8B E5");
            //pop ebp
            CaveMemory.Write_StrBytes("5D");
            //ret
            CaveMemory.Write_StrBytes("C3");

            //Inject it
            CaveMemory.InjectToOffset(_UAfControlManager_MoveGunController_InjectionStruct, "UAF_ControlManager.MovegunController()");
        }

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _Light_Array_CaveAddress = _OutputsDatabank_Address;
            _Motor_Array_CaveAddress = _OutputsDatabank_Address + 0x10;
            _DamageArray_CaveAddress = _OutputsDatabank_Address + 0x20;
            _UafGameManager_Instance_CaveAddress = _OutputsDatabank_Address + 0x30;
            _WaterMistState_CaveAddress = _OutputsDatabank_Address + 0x40;
            _UafControlManager_Instance_CaveAddress = _OutputsDatabank_Address + 0x44;

            SetHack_GetGameManagerInstance();
            SetHack_ControlManagerInstance();
            SetHack_LightsOn();
            SetHack_LightsOff();
            SetHack_SeatOn();
            SetHack_SeatOff();
            SetHack_GunShakeOn();
            SetHack_GunShakeOff();
            SetHack_Damage();
            SetHack_MistState();
            

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Hooking to UAF_GameManager.Update() will allow us to get the Instance pointer of the Singleton
        /// Used later to get coins, player status, etc...
        /// </summary>
        private void SetHack_GetGameManagerInstance()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov edi,[ebp+08]
            CaveMemory.Write_StrBytes("8B 7D 08");
            //add esp,04
            CaveMemory.Write_StrBytes("83 C4 04");
            //mov [_UafGameManager_Instance_CaveAddress],edi
            CaveMemory.Write_StrBytes("89 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_UafGameManager_Instance_CaveAddress));

            //Inject it
            CaveMemory.InjectToOffset(_UafGameManager_Update_InjectionStruct, "UAF_GameManager.Update()");
        }

        /// <summary>
        /// Intercepting calls to function, where we can get Player ID, Lamp Id and Lamp State (1 = ON, 2 = Blink)
        /// </summary>
        private void SetHack_LightsOn()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov eax,[eax+08]
            CaveMemory.Write_StrBytes("8B 40 08");
            //push eax
            CaveMemory.Write_StrBytes("50");

            //mov esi,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 75 0C");
            //shl esi,02
            CaveMemory.Write_StrBytes("C1 E6 02");
            //add esi,[ebp+10]
            CaveMemory.Write_StrBytes("03 75 10");
            //add esi,_Light_Array_CaveAddress
            CaveMemory.Write_StrBytes("81 C6");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Light_Array_CaveAddress));
            //mov eax,[ebp+14]
            CaveMemory.Write_StrBytes("8B 45 14");
            //mov [esi],al
            CaveMemory.Write_StrBytes("88 06");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //mov esi,[ebp+10]
            CaveMemory.Write_StrBytes("8B 75 10");

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_OpenLight_InjectionStruct, "UAF_ControlManager.OpenLight()");
        }
        private void SetHack_LightsOff()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov eax,[eax+08]
            CaveMemory.Write_StrBytes("8B 40 08");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov esi,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 75 0C");
            //shl esi,02
            CaveMemory.Write_StrBytes("C1 E6 02");
            //add esi,[ebp+10]
            CaveMemory.Write_StrBytes("03 75 10");
            //add esi,_Light_Array_CaveAddress
            CaveMemory.Write_StrBytes("81 C6");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Light_Array_CaveAddress));
            //xor eax, eax
            CaveMemory.Write_StrBytes("31 C0");
            //mov [esi],al
            CaveMemory.Write_StrBytes("88 06");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //mov esi,[ebp+10]
            CaveMemory.Write_StrBytes("8B 75 10");

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_CloseLight_InjectionStruct, "UAF_ControlManager.CloseLight()");
        }

        /// <summary>
        /// Intercepting calls to function, where we can get Player ID
        /// </summary>
        private void SetHack_SeatOn()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov eax,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 45 0C");
            //add eax,_Motor_Array_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Motor_Array_CaveAddress));
            //mov byte ptr[eax],1
            CaveMemory.Write_StrBytes("C6 00 01");
            //mov eax,[edi+0000020C]
            CaveMemory.Write_StrBytes("8B 87 0C 02 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_StartSeatShake_InjectionStruct, "UAF_ControlManager.StartSeatShake()");
        }
        private void SetHack_SeatOff()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov eax,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 45 0C");
            //add eax,_Motor_Array_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Motor_Array_CaveAddress));
            //mov byte ptr[eax],0
            CaveMemory.Write_StrBytes("C6 00 00");
            //mov eax,[edi+0000020C]
            CaveMemory.Write_StrBytes("8B 87 0C 02 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_CloseSeatShake_InjectionStruct, "UAF_ControlManager.CloseSeatShake()");
        }

        /// <summary>
        /// Intercepting calls to function, where we can get Player ID
        /// </summary>
        private void SetHack_GunShakeOn()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            //mov eax,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 45 0C");
            //add eax,_Motor_Array_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Motor_Array_CaveAddress + 4));
            //mov byte ptr[eax],1
            CaveMemory.Write_StrBytes("C6 00 01");
            //mov eax,[edi+00000208]
            CaveMemory.Write_StrBytes("8B 87 08 02 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_StartShakeApparatusOut_InjectionStruct, "UAF_ControlManager.StartShakeApparatusOut()");
        }
        private void SetHack_GunShakeOff()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov eax,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 45 0C");
            //add eax,_Motor_Array_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Motor_Array_CaveAddress + 4));
            //mov byte ptr[eax],0
            CaveMemory.Write_StrBytes("C6 00 00");
            //mov eax,[edi+00000208]
            CaveMemory.Write_StrBytes("8B 87 08 02 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_CloseShake_InjectionStruct, "UAF_ControlManager.CloseShake()");
        }

        /// <summary>
        /// Intercepting calls to function, where we can get Player ID
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //and esp, -10
            CaveMemory.Write_StrBytes("83 E4 F0");
            //sub esp,28
            CaveMemory.Write_StrBytes("83 EC 28");
            //mov ecx,[ebp+10]
            CaveMemory.Write_StrBytes("8B 4D 10");
            //cmp ecx, -1
            CaveMemory.Write_StrBytes("83 F9 FF");
            //jne Next
            CaveMemory.Write_StrBytes("75 0D");
            //mov ecx,_DamageArray_CaveAddress
            CaveMemory.Write_StrBytes("B9");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_DamageArray_CaveAddress));
            //mov [ecx],01010101
            CaveMemory.Write_StrBytes("C7 01 01 01 01 01");
            //jmp Exit
            CaveMemory.Write_StrBytes("EB 09");
            //add ecx,_DamageArray_CaveAddress
            CaveMemory.Write_StrBytes("81 C1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_DamageArray_CaveAddress));
            //mov byte ptr[ecx],1
            CaveMemory.Write_StrBytes("C6 01 01");

            //Inject it
            CaveMemory.InjectToOffset(_SgPlayer_PlayEffectHitted_InjectionStruct, "SG_Player.PlayEffectHitted()");
        }

        /// <summary>
        /// Intercepting call to change the state
        /// </summary>
        private void SetHack_MistState()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //and esp, -10
            CaveMemory.Write_StrBytes("83 E4 F0");
            //sub esp,38
            CaveMemory.Write_StrBytes("83 EC 38");
            //mov ecx,[ebp+0C]
            CaveMemory.Write_StrBytes("8B 4D 0C");
            //mov [_WaterMistState_CaveAddress], ecx
            CaveMemory.Write_StrBytes("89 0D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_WaterMistState_CaveAddress));

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_WaterMistControl_InjectionStruct, "UAF_ControlManager.WaterMistState()");
        }

        /// <summary>
        /// Hooking to UAF_ControlManager.Update() will allow us to get the Instance pointer of the Singleton
        /// Used later to get water status for guns
        private void SetHack_ControlManagerInstance()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _GameAssemblyDll_BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov esi,[ebp+08]
            CaveMemory.Write_StrBytes("8B 75 08");
            //xor edx,edx
            //mov [_UafControlManager_Instance_CaveAddress],esi
            CaveMemory.Write_StrBytes("89 35");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_UafControlManager_Instance_CaveAddress));

            //Inject it
            CaveMemory.InjectToOffset(_UafControlManager_Update_InjectionStruct, "UAF_ControlManager.Update()");
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

            WriteBytes(_AxisX_Array_CaveAddress + (UInt32)((PlayerData.ID - 1) * 4), bufferX);
            WriteBytes(_AxisY_Array_CaveAddress + (UInt32)((PlayerData.ID - 1) * 4), bufferY);
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();           
            _Outputs.Add(new GameOutput(OutputDesciption.P1_WaterFire, OutputId.P1_WaterFire));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_WaterFire, OutputId.P2_WaterFire));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_WaterFire, OutputId.P3_WaterFire));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_WaterFire, OutputId.P4_WaterFire));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_BigGun, OutputId.P1_BigGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_BigGun, OutputId.P2_BigGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_BigGun, OutputId.P3_BigGun));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_BigGun, OutputId.P4_BigGun));
            _Outputs.Add(new GameOutput(OutputDesciption.SmokeSwitch, OutputId.SmokeSwitch));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart, 100));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart, 100));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P3_LmpStart, OutputId.P3_LmpStart, 100));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P4_LmpStart, OutputId.P4_LmpStart, 100));
            _Outputs.Add(new BlinkGameOutput(OutputDesciption.P1_LmpGun, OutputId.P1_LmpGun, 100));
            _Outputs.Add(new BlinkGameOutput(OutputDesciption.P2_LmpGun, OutputId.P2_LmpGun, 100));
            _Outputs.Add(new BlinkGameOutput(OutputDesciption.P3_LmpGun, OutputId.P3_LmpGun, 100));
            _Outputs.Add(new BlinkGameOutput(OutputDesciption.P4_LmpGun, OutputId.P4_LmpGun, 100));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_ChairShake, OutputId.P1_ChairShake));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_ChairShake, OutputId.P2_ChairShake));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_ChairShake, OutputId.P3_ChairShake));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_ChairShake, OutputId.P4_ChairShake));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_GunMotor, OutputId.P3_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_GunMotor, OutputId.P4_GunMotor));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P3_Damaged, OutputId.P3_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P4_Damaged, OutputId.P4_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Credits, OutputId.P1_Credits));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Credits, OutputId.P2_Credits));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Credits, OutputId.P3_Credits));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Credits, OutputId.P4_Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            int[] Coins = new int[4];
            byte[] IsGaming = new byte[4];
            UInt32 pGameManager = ReadPtr(_UafGameManager_Instance_CaveAddress);
            if (pGameManager != 0)
            {
                UInt32 pCoins = ReadPtr(pGameManager + 0x40);
                if (pCoins != 0)
                {
                    int ArrayLength = (int)ReadPtr(pCoins + 0x0C);
                    for (int i = 0; i < ArrayLength; i++)
                    {
                        Coins[i] = (int)ReadPtr((UInt32)(pCoins + 0x10 + 4 * i));
                    }
                }

                UInt32 pIsGaming = ReadPtr(pGameManager + 0x5C);
                if (pIsGaming != 0)
                {
                    int ArrayLength = (int)ReadPtr(pIsGaming + 0x0C);
                    for (int i = 0; i < ArrayLength; i++)
                    {
                        IsGaming[i] = ReadByte((UInt32)(pIsGaming + 0x10 + i));
                    }
                }
            }

            int[] SmallWater = new int[4];
            int[] BigWater = new int[4];
            UInt32 pControlManager = ReadPtr(_UafControlManager_Instance_CaveAddress);
            if (pControlManager != 0)
            {
                UInt32 pWaterPump = ReadPtr(pControlManager + 0x1E8);
                if (pWaterPump != 0)
                {
                    int ArrayLength = (int)ReadPtrChain(pWaterPump + 0x08, new UInt32[] { 0x00 });
                    int ItemLength = (int)ReadPtrChain(pWaterPump + 0x08, new UInt32[] { 0x08 });
                    for (int i = 0; i < ArrayLength; i++)
                    {
                        BigWater[i] = (int)ReadByte((UInt32)(pWaterPump + 0x10 + ItemLength * i));
                        SmallWater[i] = (int)ReadByte((UInt32)(pWaterPump + 0x10 + ItemLength * i + 1));
                    }
                }
            }

            SetOutputValue(OutputId.P1_WaterFire, SmallWater[0]);
            SetOutputValue(OutputId.P2_WaterFire, SmallWater[1]);
            SetOutputValue(OutputId.P3_WaterFire, SmallWater[2]);
            SetOutputValue(OutputId.P4_WaterFire, SmallWater[3]);

            SetOutputValue(OutputId.P1_BigGun, BigWater[0]);
            SetOutputValue(OutputId.P2_BigGun, BigWater[1]);
            SetOutputValue(OutputId.P3_BigGun, BigWater[2]);
            SetOutputValue(OutputId.P4_BigGun, BigWater[3]);

            SetOutputValue(OutputId.SmokeSwitch, ReadByte(_WaterMistState_CaveAddress));

            SetOutputValue(OutputId.P1_LmpStart, GetBlinkingLightState(ReadByte(_Light_Array_CaveAddress + 1)));
            SetOutputValue(OutputId.P1_LmpGun, ReadByte(_Light_Array_CaveAddress + 2));
            SetOutputValue(OutputId.P2_LmpStart, GetBlinkingLightState(ReadByte(_Light_Array_CaveAddress + 5)));
            SetOutputValue(OutputId.P2_LmpGun, ReadByte(_Light_Array_CaveAddress + 6));
            SetOutputValue(OutputId.P3_LmpStart, GetBlinkingLightState(ReadByte(_Light_Array_CaveAddress + 9)));
            SetOutputValue(OutputId.P3_LmpGun, ReadByte(_Light_Array_CaveAddress + 10));
            SetOutputValue(OutputId.P4_LmpStart, GetBlinkingLightState(ReadByte(_Light_Array_CaveAddress + 13)));
            SetOutputValue(OutputId.P4_LmpGun, ReadByte(_Light_Array_CaveAddress + 14));

            SetOutputValue(OutputId.P1_ChairShake, ReadByte(_Motor_Array_CaveAddress));
            SetOutputValue(OutputId.P2_ChairShake, ReadByte(_Motor_Array_CaveAddress + 1));
            SetOutputValue(OutputId.P3_ChairShake, ReadByte(_Motor_Array_CaveAddress + 2));
            SetOutputValue(OutputId.P4_ChairShake, ReadByte(_Motor_Array_CaveAddress + 3));
            SetOutputValue(OutputId.P1_GunMotor, ReadByte(_Motor_Array_CaveAddress + 4));
            SetOutputValue(OutputId.P2_GunMotor, ReadByte(_Motor_Array_CaveAddress + 5));
            SetOutputValue(OutputId.P3_GunMotor, ReadByte(_Motor_Array_CaveAddress + 6));
            SetOutputValue(OutputId.P4_GunMotor, ReadByte(_Motor_Array_CaveAddress + 7));

            //When the game "Hits" some player, ID can be -1 (= All Players ?)
            //In that case the codeCave will set the Damage flags to all players
            //And will will filter here with the player Status to only enable the Damage output for playing players
            if (ReadByte(_DamageArray_CaveAddress) == 1 && IsGaming[0] == 1)
            {
                SetOutputValue(OutputId.P1_Damaged, 1);
                WriteByte(_DamageArray_CaveAddress, 0x00);
            }
            if (ReadByte(_DamageArray_CaveAddress + 1) == 1 && IsGaming[1] == 1)
            {
                SetOutputValue(OutputId.P2_Damaged, 1);
                WriteByte(_DamageArray_CaveAddress + 1, 0x00);
            }
            if (ReadByte(_DamageArray_CaveAddress + 2) == 1 && IsGaming[2] == 1)
            {
                SetOutputValue(OutputId.P3_Damaged, 1);
                WriteByte(_DamageArray_CaveAddress + 2, 0x00);
            }
            if (ReadByte(_DamageArray_CaveAddress + 3) == 1 && IsGaming[3] == 1)
            {
                SetOutputValue(OutputId.P4_Damaged, 1);
                WriteByte(_DamageArray_CaveAddress + 3, 0x00);
            }

            SetOutputValue(OutputId.P1_Credits, Coins[0]);
            SetOutputValue(OutputId.P2_Credits, Coins[1]);
            SetOutputValue(OutputId.P3_Credits, Coins[2]);
            SetOutputValue(OutputId.P4_Credits, Coins[3]);

        }
        //Game sets values as this :
        //0 = OFF
        //1 = CONSTANT ON
        //2 = CONSTANT OFF
        //We need -1 instead of 2 for custom blinking output state
        private int GetBlinkingLightState(byte ReadValue)
        {
            if (ReadValue == 2)
                return -1;
            else
                return ReadValue;
        }

        #endregion
    }
}
