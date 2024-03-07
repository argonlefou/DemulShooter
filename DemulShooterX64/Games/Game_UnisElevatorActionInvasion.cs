using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MemoryX64;
using DsCore.RawInput;
using DsCore.Win32;
using System.Runtime.InteropServices;
using DsCore.MameOutput;

namespace DemulShooterX64
{
    class Game_UnisElevatorActionInvasion : Game
    {
        private string _UsbPluginsDllName = "usbpluginsdll.dll";
        private IntPtr _UsbPluginsDll_BaseAddress = IntPtr.Zero;

        /*** Game Data for Memory Hack ***/
        /*** MEMORY ADDRESSES **/
        private static UInt32 _P1_X_Offset = 0x3A91850;
        private static UInt32 _P1_Y_Offset = 0x3A91854;
        private static UInt32 _P1_Trigger_Offset = 0x3A91858;
        private static UInt32 _P1_Credits_Offset = 0x3A9185C;
        private static UInt32 _P2_X_Offset = 0x3A91860;
        private static UInt32 _P2_Y_Offset = 0x3A91864;
        private static UInt32 _P2_Trigger_Offset = 0x3A91868;
        private static UInt32 _P2_Credits_Offset = 0x3A9186C;
        private static UInt32 _IoBoard_Payload_Offset = 0x3A91870 + 0x2CC;
        private NopStruct _Nop_KeyboardAndMouse = new NopStruct(0xB85E4A, 5);
        private UInt64 _Recoil_Injection_Offset = 0x794D54;
        private UInt64 _Recoil_Injection_Return_Offset = 0x794D63;
        private UInt64 _Damage_Injection_Offset = 0x796360;
        private UInt64 _Damage_Injection_Return_Offset = 0x796370;

        //Outputs
        private UInt64 _P1_Recoil_CaveAddress = 0;
        private UInt64 _P2_Recoil_CaveAddress = 0; 
        private UInt64 _P1_Damage_CaveAddress = 0;
        private UInt64 _P2_Damage_CaveAddress = 0;


        /// <summary>
        /// Constructor
        /// </summary>
        public Game_UnisElevatorActionInvasion(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "ESGame-Win64-Shipping", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Elevator Action  Invasion v1.6.1[C] clean dump", "b7a68a80fa682673a61120aadf5b2ec35e1f7474");
            _KnownMd5Prints.Add("Elevator Action  Invasion v1.6.1[C] patched by Argonlefou", "d2c4c99a86648b34316ca4f61a23c1ea");
            _tProcess.Start();
            Logger.WriteLog("Waiting for Unis " + _RomName + " game to hook.....");
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
                            ProcessModuleCollection c = _TargetProcess.Modules;
                            foreach (ProcessModule m in c)
                            {
                                if (m.ModuleName.ToLower().Equals(_UsbPluginsDllName))
                                {
                                    _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                    _UsbPluginsDll_BaseAddress = m.BaseAddress;
                                    Logger.WriteLog(_UsbPluginsDllName + " = 0x" + _UsbPluginsDll_BaseAddress);
                                    CheckExeMd5();
                                    SetHack();
                                    _ProcessHooked = true;
                                    RaiseGameHookedEvent();
                                    _ProcessHooked = true;
                                    break;
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

        #region MemoryHack

        private void SetHack()
        {
            //+B85E4A --> NOP | 5 --> Nop mousebuttons and keyboard keys
            SetNops(_TargetProcess_MemoryBaseAddress, _Nop_KeyboardAndMouse);
            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");

            SetHack_Recoil();
            SetHack_Damage();
        }

        //To sync recoil pulse with fired bullets, we will retrieve the information when the game call the "PlayFireMV"
        //Player Index is in edx
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            /* Because of 64bit asm, I dont know how to load a 64bit data segment address into a register.
             * But there is an instruction to load address with an offset from RIP (instruction pointer) 
             * That's why data are stored just after the code (to know the offset)
             * */
            _P1_Recoil_CaveAddress = CaveMemory.CaveAddress + 0x40;
            _P2_Recoil_CaveAddress = CaveMemory.CaveAddress + 0x44;
            Logger.WriteLog("_Recoil_CaveAddress = 0x" + _P1_Recoil_CaveAddress.ToString("X16"));

            //push rax
            CaveMemory.Write_StrBytes("50");
            //push rdx
            CaveMemory.Write_StrBytes("52");
            //lea rax, [RIP+ 0x37] = P1_Recoil_CaveAddress
            CaveMemory.Write_StrBytes("48 8D 05 37 00 00 00");
            //shl rdx, 2
            CaveMemory.Write_StrBytes("48 C1 E2 02");
            //add rax, rdx
            CaveMemory.Write_StrBytes("48 01 D0");
            //mov [rax], 1
            CaveMemory.Write_StrBytes("C7 00 01 00 00 00"); 
            //pop rdx
            CaveMemory.Write_StrBytes("5A");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //OriginalCode
            CaveMemory.Write_StrBytes("53 48 83 EC 20 48 63 DA 48 8B 91 38 02 00 00");
            //jmp back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _Recoil_Injection_Return_Offset);

            Logger.WriteLog("Adding Recoil Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

            //Code Injection
            List<Byte> Buffer = new List<Byte>();
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer = new List<byte>();
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _Recoil_Injection_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        //Intercept a call to PlayTakeDamageSound() function to get dammage event
        //Player Index is in edx
        private void SetHack_Damage()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            /* Because of 64bit asm, I dont know how to load a 64bit data segment address into a register.
             * But there is an instruction to load address with an offset from RIP (instruction pointer) 
             * That's why data are stored just after the code (to know the offset)
             * */
            _P1_Damage_CaveAddress = CaveMemory.CaveAddress + 0x40;
            _P2_Damage_CaveAddress = CaveMemory.CaveAddress + 0x44;
            Logger.WriteLog("_Damage_CaveAddress = 0x" + _P1_Recoil_CaveAddress.ToString("X16"));

            //push rax
            CaveMemory.Write_StrBytes("50");
            //push rdx
            CaveMemory.Write_StrBytes("52");
            //lea rax, [RIP+ 0x37] = P1_Damage_CaveAddress
            CaveMemory.Write_StrBytes("48 8D 05 37 00 00 00");
            //shl rdx, 2
            CaveMemory.Write_StrBytes("48 C1 E2 02");
            //add rax, rdx
            CaveMemory.Write_StrBytes("48 01 D0");
            //mov [rax], 1
            CaveMemory.Write_StrBytes("C7 00 01 00 00 00");
            //pop rdx
            CaveMemory.Write_StrBytes("5A");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //OriginalCode
            CaveMemory.Write_StrBytes("40 57 41 56 41 57 48 81 EC 80 00 00 00 48 8B F9");
            //jmp back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _Damage_Injection_Return_Offset);

            Logger.WriteLog("Adding Recoil Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

            //Code Injection
            List<Byte> Buffer = new List<Byte>();
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer = new List<byte>();
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _Damage_Injection_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        #endregion

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

                    double dMaxX = 1080.0;
                    double dMaxY = 1920.0;

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

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>   
        public override void SendInput(PlayerSettings PlayerData)
        {
            if (PlayerData.ID == 1)
            {
                WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P1_X_Offset), BitConverter.GetBytes(PlayerData.RIController.Computed_X));
                WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset), BitConverter.GetBytes(PlayerData.RIController.Computed_Y));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteByte((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset), 1);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset), 0);                
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P2_X_Offset), BitConverter.GetBytes(PlayerData.RIController.Computed_X));
                WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset), BitConverter.GetBytes(PlayerData.RIController.Computed_Y));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteByte((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset), 1);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset), 0);   
            }
        }

        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (!_DisableInputHack)
            {
                if (nCode >= 0)
                {
                    KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                    if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                    {
                        if (s.scanCode == Configurator.GetInstance().DIK_Eai_P1_Start)
                            Apply_OR_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 2), 0x20);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Eai_P2_Start)
                            Apply_OR_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 2), 0x10);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Eai_Settings)
                            Apply_OR_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 2), 0x04);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Eai_MenuUp)
                            Apply_OR_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 2), 0x04);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Eai_MenuDown)
                            Apply_OR_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 2), 0x02);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Eai_MenuEnter)
                            Apply_OR_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 2), 0x01);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Eai_P1_Credits)
                        {
                            UInt32 c = BitConverter.ToUInt32(ReadBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P1_Credits_Offset), 4), 0);
                            c++;
                            WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P1_Credits_Offset), BitConverter.GetBytes(c));
                        }
                        else if (s.scanCode == Configurator.GetInstance().DIK_Eai_P1_Credits)
                        {
                            UInt32 c = BitConverter.ToUInt32(ReadBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P2_Credits_Offset), 4), 0);
                            c++;
                            WriteBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P2_Credits_Offset), BitConverter.GetBytes(c));
                        }

                        /*else if (s.scanCode == HardwareScanCode.DIK_F)
                            SetDoorSensor(SensorDoor.A, DoorState.ClosedOn);
                        else if (s.scanCode == HardwareScanCode.DIK_G)
                            SetDoorSensor(SensorDoor.B, DoorState.ClosedOn);
                        else if (s.scanCode == HardwareScanCode.DIK_V)
                            SetDoorSensor(SensorDoor.A, DoorState.InfraredSwitchOn);
                        else if (s.scanCode == HardwareScanCode.DIK_B)
                            SetDoorSensor(SensorDoor.B, DoorState.InfraredSwitchOn);*/
                    }
                    else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                    {
                        if (s.scanCode == Configurator.GetInstance().DIK_Eai_P1_Start)
                            Apply_AND_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 2), 0xDF);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Eai_P2_Start)
                            Apply_AND_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 2), 0xEF);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Eai_Settings)
                            Apply_AND_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 2), 0xFB);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Eai_MenuUp)
                            Apply_AND_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 2), 0xFB);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Eai_MenuDown)
                            Apply_AND_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 2), 0xFD);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Eai_MenuEnter)
                            Apply_AND_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 2), 0xFE);

                        /*else if (s.scanCode == HardwareScanCode.DIK_F)
                            SetDoorSensor(SensorDoor.A, DoorState.ClosedOff);
                        else if (s.scanCode == HardwareScanCode.DIK_G)
                            SetDoorSensor(SensorDoor.B, DoorState.ClosedOff);
                        else if (s.scanCode == HardwareScanCode.DIK_V)
                            SetDoorSensor(SensorDoor.A, DoorState.InfraredSwitchOff);
                        else if (s.scanCode == HardwareScanCode.DIK_B)
                            SetDoorSensor(SensorDoor.B, DoorState.InfraredSwitchOff);*/
                    }
                }
            }
            return Win32API.CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
        }

        //Door A (Left) = 1 / Door B (Right = 0)
        //State 
        public enum SensorDoor
        {
            B=0,
            A
        }
        public enum DoorState
        {
            InfraredSwitchOn,
            InfraredSwitchOff,
            ErrorOn,
            ErrorOff,
            ClosedOn,
            ClosedOff,
        }
        private void SetDoorSensor(SensorDoor DoorNumber, DoorState State)
        {
            if (State == DoorState.InfraredSwitchOn)
            {
                Apply_OR_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 3 + (uint)DoorNumber), 0x80);
            }
            else if (State == DoorState.ErrorOn)
            {
                Apply_OR_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 3 + (uint)DoorNumber), 0x40);
            }
            else if (State == DoorState.ClosedOn)
            {
                Apply_OR_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 3 + (uint)DoorNumber), 0x0F);
            }

            else if (State == DoorState.InfraredSwitchOff)
            {
                Apply_AND_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 3 + (uint)DoorNumber), 0x7F);
            }
            else if (State == DoorState.ErrorOff)
            {
                Apply_AND_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 3 + (uint)DoorNumber), 0xBF);
            }
            else if (State == DoorState.ClosedOff)
            {
                Apply_AND_ByteMask((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _IoBoard_Payload_Offset + 3 + (uint)DoorNumber), 0xF0);
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

            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.DoorA, OutputId.DoorA));
            _Outputs.Add(new GameOutput(OutputDesciption.DoorB, OutputId.DoorB));
            _Outputs.Add(new GameOutput(OutputDesciption.ElevatorLedsStatus, OutputId.ElevatorLedsStatus));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            /*_Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));*/
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Credits, OutputId.P1_Credit));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Credits, OutputId.P2_Credit));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Gun motor data goes from 0 to 10. Power of rumble ?
            SetOutputValue(OutputId.P1_GunMotor, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + 0x2a290 + 0x45C)));
            SetOutputValue(OutputId.P2_GunMotor, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + 0x2a290 + 0x460)));            
            //Start Led have 3 original values : 
            //0 = ON
            //1 = BLINK
            //2 = OFF
            byte b = ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + 0x2a290 + 0x450));
            SetOutputValue(OutputId.P1_LmpStart, 2 - b);
            b = ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + 0x2a290 + 0x454));
            SetOutputValue(OutputId.P2_LmpStart, 2 - b);
            //Door can be 0x09 or 0x11...don't know how it works
            SetOutputValue(OutputId.DoorB, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + 0x2a290 + 0x448)));
            SetOutputValue(OutputId.DoorA, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + 0x2a290 + 0x44C))); 
            //Game LED status can have a lot of states :
            // 0 = Standby 
            // 1 = OFF 
            // 2 = NormalFight 
            // 3 = Intense Fight
            // 4 = climax Fight 
            // 5 = Damage 
            // 6 = Continue 
            // 7 = Level Passed 
            // 8 = Elevator CLosed
            //I suppose this is handling the whole set of lights on the cabinet
            SetOutputValue(OutputId.ElevatorLedsStatus, ReadByte((IntPtr)((UInt64)_UsbPluginsDll_BaseAddress + 0x2a290 + 0x458))); 

            //Custom recoil will be generated by a call to one of the game Gun function
            int P1_Recoil = ReadByte((IntPtr)_P1_Recoil_CaveAddress);
            if (P1_Recoil != 0)
            {
                WriteByte((IntPtr)_P1_Recoil_CaveAddress, 0x00);
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
            }
            int P2_Recoil = ReadByte((IntPtr)_P2_Recoil_CaveAddress);
            if (P2_Recoil != 0)
            {
                WriteByte((IntPtr)_P2_Recoil_CaveAddress, 0x00);
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
            }

            //Same Thing for custom dammage
            int P1_Damage = ReadByte((IntPtr)_P1_Damage_CaveAddress);
            if (P1_Damage != 0)
            {
                WriteByte((IntPtr)_P1_Damage_CaveAddress, 0x00);
                SetOutputValue(OutputId.P1_Damaged, 1);
            }
            int P2_Damage = ReadByte((IntPtr)_P2_Damage_CaveAddress);
            if (P2_Damage != 0)
            {
                WriteByte((IntPtr)_P2_Damage_CaveAddress, 0x00);
                SetOutputValue(OutputId.P2_Damaged, 1);
            }

            //Credits
            SetOutputValue(OutputId.P1_Credit, BitConverter.ToInt32(ReadBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P1_Credits_Offset), 4), 0));
            SetOutputValue(OutputId.P2_Credit, BitConverter.ToInt32(ReadBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _P2_Credits_Offset), 4), 0));
        }

        #endregion
    }
}
