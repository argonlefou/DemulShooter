using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_WndBonbon95 : Game
    {      
        //Memory values
        private UInt32 _Credits_Offset = 0x00049FE9;
        private UInt32 _P1_ControlType_Offset = 0x00049DE1;
        private UInt32 _P2_ControlType_Offset = 0x00049DE5;
        private UInt32 _TextureDrawingCategoryIndex_Offset = 0x0004BFB4;
        private InjectionStruct _P1_MouseControls_InjectionStruct = new InjectionStruct(0x00004105, 5);
        private InjectionStruct _P2_MouseControls_InjectionStruct = new InjectionStruct(0x00004739, 5);
        private InjectionStruct _NoCrosshair_InjectionStruct = new InjectionStruct(0x2D63A, 11);
        private InjectionStruct _P1_Recoil_InjectionStruct = new InjectionStruct(0x00009393, 6);
        private InjectionStruct _P2_Recoil_InjectionStruct = new InjectionStruct(0x00009147, 6);

        //Custom Values
        private UInt32 _P1_Buttons_CaveAddress = 0;
        private UInt32 _P1_X_CaveAddress = 0;
        private UInt32 _P1_Y_CaveAddress = 0;
        private UInt32 _P2_Buttons_CaveAddress = 0;
        private UInt32 _P2_X_CaveAddress = 0;
        private UInt32 _P2_Y_CaveAddress = 0;
        private UInt32 _P1_RecoilStatus_CaveAddress = 0;
        private UInt32 _P2_RecoilStatus_CaveAddress = 0;

        private HardwareScanCode _DIK_Credits = HardwareScanCode.DIK_5;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndBonbon95(String RomName, bool HideCrosshair, bool DisableInputHack, bool Verbose)
            : base(RomName, "Main95", DisableInputHack, Verbose)
        {
            _HideCrosshair = HideCrosshair;

            _KnownMd5Prints.Add("BONBON v1.04 - Original exe", "cd2d16ab00750d4c2ddf010aa5402407");
            _tProcess.Start();

            Logger.WriteLog("Waiting for Windows " + _RomName + " game to hook.....");
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
                            _GameWindowHandle = _TargetProcess.MainWindowHandle;
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();
                            if (!_DisableInputHack)
                                SetHack();
                            else
                                Logger.WriteLog("Input Hack disabled");
                            SetHack_Outputs();
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

                    //X => [0-640]
                    //Y => [0-480]
                    double dMaxX = 640.0;
                    double dMaxY = 480.0;

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

        /// <summary>
        /// Genuine Hack, just blocking Axis and Triggers input to replace them.
        /// </summary>        
        private void SetHack()
        {
            CreateDataBank();

            //Force the game to act as if MOUSE was choosen in the Players Controls reading function
            // Keyboard = 0
            // Mouse = 1
            // Other ? To check...
            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_ControlType_Offset, 1);
            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_ControlType_Offset, 1);

            //Create custom function to handle MOUSE controls update and call them
            SetHack_P1Controls();
            SetHack_P2Controls();

            if (_HideCrosshair)
                SetHack_NoCrosshair();
            
            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        private void CreateDataBank()
        {
            //Creating data bank
            //Codecave :
            Codecave CaveMemoryInput = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemoryInput.Open();
            CaveMemoryInput.Alloc(0x800);
            _P1_Buttons_CaveAddress = CaveMemoryInput.CaveAddress;
            _P1_X_CaveAddress = CaveMemoryInput.CaveAddress + 4;
            _P1_Y_CaveAddress = CaveMemoryInput.CaveAddress + 8;
            _P2_Buttons_CaveAddress = CaveMemoryInput.CaveAddress + 0xC;
            _P2_X_CaveAddress = CaveMemoryInput.CaveAddress + 0x10;
            _P2_Y_CaveAddress = CaveMemoryInput.CaveAddress + 0x14;
            Logger.WriteLog("Custom input data will be stored at : 0x" + CaveMemoryInput.CaveAddress.ToString("X8"));
        }

        /// <summary>
        /// Creating a function that will replace the existing one to update buttons + axis values
        /// sub_4321EA(_DWORD *a1, _DWORD *a2, _DWORD *a3)
        /// a1 = Buttons
        /// a2 = X
        /// a3 = Y
        /// </summary>
        private void SetHack_P1Controls()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //push eax
            CaveMemory.Write_StrBytes("50");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx, [_P1_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Buttons_CaveAddress));
            //mov eax,[esp+08]
            CaveMemory.Write_StrBytes("8B 44 24 08");
            //mov [eax],ebx
            CaveMemory.Write_StrBytes("89 18");

            //mov ebx, [_P1_X_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_X_CaveAddress));
            //mov eax,[esp+0C]
            CaveMemory.Write_StrBytes("8B 44 24 0C");
            //mov [eax],ebx
            CaveMemory.Write_StrBytes("89 18");

            //mov ebx, [_P1_Y_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Y_CaveAddress));
            //mov eax,[esp+10]
            CaveMemory.Write_StrBytes("8B 44 24 10");
            //mov [eax],ebx
            CaveMemory.Write_StrBytes("89 18");

            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_P1_MouseControls_InjectionStruct, "P1 Controls");
        }
        private void SetHack_P2Controls()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //push eax
            CaveMemory.Write_StrBytes("50");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx, [_P2_Buttons_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Buttons_CaveAddress));
            //mov eax,[esp+08]
            CaveMemory.Write_StrBytes("8B 44 24 08");
            //mov [eax],ebx
            CaveMemory.Write_StrBytes("89 18");

            //mov ebx, [_P2_X_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_X_CaveAddress));
            //mov eax,[esp+0C]
            CaveMemory.Write_StrBytes("8B 44 24 0C");
            //mov [eax],ebx
            CaveMemory.Write_StrBytes("89 18");

            //mov ebx, [_P2_Y_CaveAddress]
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Y_CaveAddress));
            //mov eax,[esp+10]
            CaveMemory.Write_StrBytes("8B 44 24 10");
            //mov [eax],ebx
            CaveMemory.Write_StrBytes("89 18");

            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_P2_MouseControls_InjectionStruct, "P2 Controls");
        }

        /// <summary>
        /// To remove crosshair, we can force out-of -screen corrdinates when the game is drawing the needed texture
        /// To find the texture, it looks like the _TextureDrawingCategoryIndex_Offset is 0x11 when the function is called to draw crosshair-related resources
        /// And the ECX value (Texture ID ?) is 0x17 and 0x1C for Impact texture (untouched)
        /// Other Id (0x192, 0x193, etc..) will be changed to not be visible
        /// </summary>
        private void SetHack_NoCrosshair()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //cmp dword ptr [Main95.exe+4BFB4],11
            CaveMemory.Write_StrBytes("83 3D");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _TextureDrawingCategoryIndex_Offset));
            CaveMemory.Write_StrBytes("11"); 
            //jne Originalcode
            CaveMemory.Write_StrBytes("75 11");
            //cmp ecx, 17
            CaveMemory.Write_StrBytes("83 F9 1C");
            //je OriginalCode
            CaveMemory.Write_StrBytes("74 0C");
            //cmp ecx, 1C
            CaveMemory.Write_StrBytes("83 F9 17");
            //je OriginalCode
            CaveMemory.Write_StrBytes("74 07"); 
            //Patch:
            //mov eax, 3000
            CaveMemory.Write_StrBytes("B8 00 03 00 00");
            //jmp Next
            CaveMemory.Write_StrBytes("EB 03");
            //OriginalCode:
            //mov eax,[ebx+54]
            CaveMemory.Write_StrBytes("8B 43 54");
            //mov [edi],eax
            CaveMemory.Write_StrBytes("89 07");
            //mov eax,[ebx+58]
            CaveMemory.Write_StrBytes("8B 43 58");
            //mov [edi+04],eax
            CaveMemory.Write_StrBytes("89 47 04");

            //Inject it
            CaveMemory.InjectToOffset(_NoCrosshair_InjectionStruct, "No Crosshair");
        }

        private void SetHack_Outputs()
        {
            CreateDataBank_Outputs();
            SetHack_Recoil_P1();
            SetHack_Recoil_P2();
        }

        private void CreateDataBank_Outputs()
        {
            //Creating data bank
            //Codecave :
            Codecave CaveMemoryInput = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemoryInput.Open();
            CaveMemoryInput.Alloc(0x800);
            _P1_RecoilStatus_CaveAddress = CaveMemoryInput.CaveAddress;
            _P2_RecoilStatus_CaveAddress = CaveMemoryInput.CaveAddress + 4;
            Logger.WriteLog("Custom output data will be stored at : 0x" + CaveMemoryInput.CaveAddress.ToString("X8"));
        }

        /// <summary>
        /// Intercepting some kind of call to draw impact texture when trigger is pressed
        /// </summary>
        private void SetHack_Recoil_P1()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov byte ptr[_P1_RecoilStatus_CaveAddress],00000001
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_RecoilStatus_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //mov eax,[ebp+14]
            CaveMemory.Write_StrBytes("8B 45 14");
            //push [eax+58]
            CaveMemory.Write_StrBytes("FF 70 58");      
            
             //Inject it
            CaveMemory.InjectToOffset(_P1_Recoil_InjectionStruct, "P1 Recoil");
        }

        /// <summary>
        /// Intercepting some kind of call to draw impact texture when trigger is pressed
        /// </summary>
        private void SetHack_Recoil_P2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov byte ptr[_P1_RecoilStatus_CaveAddress],00000001
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_RecoilStatus_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //mov eax,[ebp+14]
            CaveMemory.Write_StrBytes("8B 45 14");
            //push [eax+58]
            CaveMemory.Write_StrBytes("FF 70 58");      
            
             //Inject it
            CaveMemory.InjectToOffset(_P2_Recoil_InjectionStruct, "P2 Recoil");
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>        
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((UInt32)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((UInt32)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes(_P1_X_CaveAddress, bufferX);
                WriteBytes(_P1_Y_CaveAddress, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                   Apply_OR_ByteMask(_P1_Buttons_CaveAddress, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress, 0xFE);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    Apply_OR_ByteMask(_P1_Buttons_CaveAddress, 0x04);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress, 0xFB);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    Apply_OR_ByteMask(_P1_Buttons_CaveAddress, 0x02);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress, 0xFD);
                }
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes(_P2_X_CaveAddress, bufferX);
                WriteBytes(_P2_Y_CaveAddress, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress, 0xFE);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress, 0x04);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress, 0xFB);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress, 0x02);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress, 0xFD);
                }
            }
        }

        /// <summary>
        /// Low-level Keyboard hook callback.
        /// This is used to detect Pedal action for "Pedal-Mode" hack of DemulShooter
        /// </summary>
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    if (s.scanCode == _DIK_Credits)
                    {
                        int Coins = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset, 4), 0);
                        Coins++;
                        WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset, BitConverter.GetBytes(Coins));
                    }
                }
            }
            return Win32API.CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Custom Outputs
            //Original recoil Handling is stripped from the DLL so we are forced to handle the duration ourselve with an Async-reset output
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

            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset));
        }

        #endregion
    }
}
