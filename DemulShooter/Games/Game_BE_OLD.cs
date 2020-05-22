using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_BE_OLD : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\windows\bestate";

        /*** Memory LOcations ***/
        private UInt32 _P1_X_CaveAddress;
        private UInt32 _P1_Y_CaveAddress;
        private UInt32 _P2_X_CaveAddress;
        private UInt32 _P2_Y_CaveAddress;
        private UInt32 _P1_X_Injection_Offset_1 = 0x008F09FE;
        private UInt32 _P1_X_Injection_Return_Offset_1 = 0x008F0A04;
        private UInt32 _P1_X_Injection_Offset_2 = 0x008F0CD2;
        private UInt32 _P1_X_Injection_Return_Offset_2 = 0x008F0CDA;
        private UInt32 _P1_Y_Injection_Offset_1 = 0x008F0A0B;
        private UInt32 _P1_Y_Injection_Offset_2 = 0x008F0CFF;
        private UInt32 _P2_Axis_Injection_Offset_1 = 0x008F0E20;
        private UInt32 _P2_Axis_Injection_Offset_2 = 0x008F0E49;
        
        /*** Custom Data to inject ***/
        private float _P1_X_Value;
        private float _P1_Y_Value;
        private float _P2_X_Value;
        private float _P2_Y_Value;
       
        /*** False RightAnalogValue to force the game to refresh data ***/
        private short _UnusedRightAnalogX = -32000;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_BE_OLD(String RomName, double ForcedXratio, bool Verbose)
            : base(RomName, "BEGame", ForcedXratio, Verbose)
        {
            _KnownMd5Prints.Add("Blue Estate CODEX - Cracked", "188605d4083377e4ee3552b4c89f52fb");
            
            _tProcess.Start();

            /* Creating X360 controller */
            _XOutputManager = new XOutput();
            InstallX360Gamepad(2);

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

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            /* Wait until Splash Screen is closed and real windows displayed */ 
                            /* Game Windows classname = "LaunchUnrealUWindowsClient" */
                            StringBuilder ClassName = new StringBuilder(256);
                            int nRet = Win32API.GetClassName(_TargetProcess.MainWindowHandle, ClassName, ClassName.Capacity);
                            if (nRet != 0 && ClassName.ToString() != "SplashScreenClass")
                            {
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                CheckExeMd5();
                                ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                                SetHack_V2();
                                _ProcessHooked = true;                                
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
        
        /// <summary>
        /// Create a virtual XInput device for a given player
        /// </summary>
        /// <param name="Player">ID number of the created XInput device (between 1 and 4)</param>
        protected override void InstallX360Gamepad(int Player)
        {
            if (_XOutputManager != null)
            {
                if (_XOutputManager.isVBusExists())
                {
                    if (_XOutputManager.PlugIn(1))
                    {
                        if (Player == 2)
                        {
                            Logger.WriteLog("Plugged P2 virtual Gamepad to port 1");
                            _Player2_X360_Gamepad_Port = 1;
                        }
                    }
                    else
                    {
                        Logger.WriteLog("Failed to plug virtual GamePad to port 1. (Port already used ?)");
                        if (_XOutputManager.UnplugAll(true))
                        {
                            Logger.WriteLog("Force Unpluged all gamepads.");
                            System.Threading.Thread.Sleep(1000);
                            if (_XOutputManager.PlugIn(1))
                            {
                                if (Player == 2)
                                {
                                    Logger.WriteLog("Plugged P2 virtual Gamepad to port 1");
                                    _Player2_X360_Gamepad_Port = 1;
                                }
                            }
                            else
                            {
                                Logger.WriteLog("Failed to plug virtual GamePad to port 1.");
                            }
                        }
                        else
                        {
                            Logger.WriteLog("Failed to force Unplug virtual GamePad port 1.");
                        }
                    }
                }
                else
                {
                    Logger.WriteLog("ScpBus driver not found or not installed");
                }
            }
            else
            {
                Logger.WriteLog("XOutputManager Creation Failed !");
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
                    int TotalResX = TotalRes.Right - TotalRes.Left;
                    int TotalResY = TotalRes.Bottom - TotalRes.Top;

                    //X => [-1 ; 1] float
                    //Y => [-1 ; 1] float

                    float X_Value = (2.0f * PlayerData.RIController.Computed_X / TotalResX) - 1.0f;
                    float Y_Value = (2.0f * PlayerData.RIController.Computed_X / TotalResY) - 1.0f;

                    if (X_Value < -1.0f)
                        X_Value = -1.0f;
                    if (Y_Value < -1.0f)
                        Y_Value = -1.0f;
                    if (X_Value > 1.0f)
                        X_Value = 1.0f;
                    if (Y_Value > 1.0f)
                        Y_Value = 1.0f;

                    if (PlayerData.ID == 1)
                    {
                        _P1_X_Value = X_Value;
                        _P1_Y_Value = Y_Value;
                    }
                    else if (PlayerData.ID == 2)
                    {
                        _P2_X_Value = X_Value;
                        _P2_Y_Value = Y_Value;
                    }

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

        private void SetHack_V2()
        {            
            CreateDataBank();
            SetHack_P1X();
            SetHack_P1X_2();
            SetHack_P1Y((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Y_Injection_Offset_1);
            SetHack_P1Y((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Y_Injection_Offset_2);
            SetHack_P2Axis((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Axis_Injection_Offset_1);
            SetHack_P2Axis((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Axis_Injection_Offset_2);
            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Creating a custom memory bank to store our data
        /// </summary>

        private void CreateDataBank()
        {
            //1st Codecave : storing our Axis Data
            Codecave DataCaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            DataCaveMemory.Open();
            DataCaveMemory.Alloc(0x800);

            _P1_X_CaveAddress = DataCaveMemory.CaveAddress;
            _P1_Y_CaveAddress = DataCaveMemory.CaveAddress + 0x04;

            _P2_X_CaveAddress = DataCaveMemory.CaveAddress + 0x20;
            _P2_Y_CaveAddress = DataCaveMemory.CaveAddress + 0x24;

            Logger.WriteLog("Custom data will be stored at : 0x" + _P1_X_CaveAddress.ToString("X8"));
        }

        /// <summary>
        /// The game use some fstp [XXX] instruction, but we can't just NOP it as graphical glitches may appear.
        /// So we just add another set of instructions instruction immediatelly after to change the register 
        /// to our own desired value
        /// </summary>
        private void SetHack_P1X()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //fstp dwordptr [esi+000002B0]
            CaveMemory.Write_StrBytes("D9 9E B0 02 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [P1_X]
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P1_X_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov [esi+000002B0], eax
            CaveMemory.Write_StrBytes("89 86 B0 02 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Return_Offset_1);

            Logger.WriteLog("Adding P1 X Axis Codecave_1 at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Offset_1) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Offset_1, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        //This Codecave is modifying the xmm0 value with our own
        private void SetHack_P1X_2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_X_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P1_X_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //movss xmm0, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [esi+000002B0],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 86 B0 02 00 00");
            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Return_Offset_2);

            Logger.WriteLog("Adding P1_X CodeCave_2 at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Offset_2) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Offset_2, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }


        //This Codecave is modifying the xmm0 value with our own
        //This instruction is called 2 times so there will be 2 instance of this codecave at different places
        //The instruction lenght is fixed (8) so we won't use the Injection_Return_Offset, but Injection_Offset + 0x08 
        private void SetHack_P1Y(UInt32 OriginalProcAddress)
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_Y_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P1_Y_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //movss xmm0, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [esi+000002B4],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 86 B4 02 00 00");
            //return
            CaveMemory.Write_jmp(OriginalProcAddress + 0x08);

            Logger.WriteLog("Adding P1_Y CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - OriginalProcAddress - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, OriginalProcAddress, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }


        //This Codecave is modifying the xmm0 value with our own
        //This instruction is called 2 times so there will be 2 instance of this codecave at different places
        //The instruction lenght is fixed (9) so we won't use the Injection_Return_Offset, but Injection_Offset + 0x09 
        private void SetHack_P2Axis(UInt32 OriginalProcAddress)
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //cmp ecx, 01
            CaveMemory.Write_StrBytes("83 F9 01");
            //je AxisY
            CaveMemory.Write_StrBytes("0F 84 0A 00 00 00");
            //mov eax, _P2_X_Address
            CaveMemory.Write_StrBytes("B8");
            byte[] b = BitConverter.GetBytes(_P2_X_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //jmp originalcode
            CaveMemory.Write_StrBytes("E9 05 00 00 00");
            //AxisY:
            //mov eax, _P2_Y_Address
            CaveMemory.Write_StrBytes("B8");
            b = BitConverter.GetBytes(_P2_Y_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //originalcode:
            //movss xmm0, [eax]
            CaveMemory.Write_StrBytes("F3 0F 10 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movss [esi+ecx*4+000002B0],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 84 8E B0 02 00 00");
            //return
            CaveMemory.Write_jmp(OriginalProcAddress + 0x08);

            Logger.WriteLog("Adding P2_Axis CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - OriginalProcAddress - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, OriginalProcAddress, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
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
                //Setting Values in memory for the Codecave to read it
                byte[] buffer = BitConverter.GetBytes(_P1_X_Value);
                WriteBytes(_P1_X_CaveAddress, buffer);
                buffer = BitConverter.GetBytes(_P1_Y_Value);
                WriteBytes(_P1_Y_CaveAddress, buffer);
            }
            else if (PlayerData.ID == 2)
            {
                //Setting Values in memory for the Codecave to read it
                byte[] buffer = BitConverter.GetBytes(_P2_X_Value);
                WriteBytes(_P2_X_CaveAddress, buffer);
                buffer = BitConverter.GetBytes(_P2_Y_Value);
                WriteBytes(_P2_Y_CaveAddress, buffer);

                //changing the Right Axis Gamepad Value so that the game can update positioning
                //If the game does not detect a right axis change, no update is done !!
                _UnusedRightAnalogX = (short)(-_UnusedRightAnalogX);
                _XOutputManager.SetRAxis_X(_Player2_X360_Gamepad_Port, _UnusedRightAnalogX);


                /*
                //Value if [-1; 1] float so we convert to [0,32000] int for Xoutput format
                Logger.WriteLog("Float P2 X -----> " + _P2_X_Value.ToString());
                float fx = (_P2_X_Value + 1.0f) * 16000.0f;
                float fy = (_P2_Y_Value + 1.0f) * 16000.0f;
                Logger.WriteLog("           -----> " + fx.ToString());
                short ix = (short)fx;
                short iy = (short)fy;
                Logger.WriteLog("           -----> " + ix.ToString());
                buffer = BitConverter.GetBytes(ix);
                //WriteBytes(_P2_X_Address+0x30, buffer);
                */
                //_XOutputManager.SetRAxis_X(_Player2_X360_Gamepad_Port,ix);
                //_XOutputManager.SetRAxis_Y(_Player2_X360_Gamepad_Port, iy);

                //Inputs
                //
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0) 
                {
                    _XOutputManager.SetButton_R2(_Player2_X360_Gamepad_Port, 0xFF);
                    _XOutputManager.SetButton_A(_Player2_X360_Gamepad_Port, true);  //used to validate in menu
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0) 
                {
                    _XOutputManager.SetButton_R2(_Player2_X360_Gamepad_Port, 0x00);
                    _XOutputManager.SetButton_A(_Player2_X360_Gamepad_Port, false); //used to validate in menu
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0) 
                    _XOutputManager.SetButton_B(_Player2_X360_Gamepad_Port, true);                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0) 
                    _XOutputManager.SetButton_B(_Player2_X360_Gamepad_Port, false);               
            }
        }

        /// <summary>
        /// Low-level Keyboard hook callback.
        /// This is used to simulated inputs for P2
        /// </summary>
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    if (s.scanCode == HardwareScanCode.DIK_NUMPAD8)
                    {
                        _XOutputManager.SetRAxis_Y(_Player2_X360_Gamepad_Port, 32000);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_NUMPAD2)
                    {
                        _XOutputManager.SetRAxis_Y(_Player2_X360_Gamepad_Port, -32000);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_NUMPAD4)
                    {
                        _XOutputManager.SetRAxis_X(_Player2_X360_Gamepad_Port, -32000);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_NUMPAD6)
                    {
                        byte[] buffer = BitConverter.GetBytes(0.7f);
                        WriteBytes(_P2_X_CaveAddress, buffer);

                        _XOutputManager.SetRAxis_X(_Player2_X360_Gamepad_Port, 32000);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_Q)
                    {
                        _XOutputManager.SetButton_A(_Player2_X360_Gamepad_Port, true);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_B)
                    {
                        _XOutputManager.SetButton_B(_Player2_X360_Gamepad_Port, true);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_X)
                    {
                        _XOutputManager.SetButton_X(_Player2_X360_Gamepad_Port, true);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_Y)
                    {
                        _XOutputManager.SetButton_Y(_Player2_X360_Gamepad_Port, true);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_G)
                    {
                        _XOutputManager.SetButton_Guide(_Player2_X360_Gamepad_Port, true);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_BACK)
                    {
                        _XOutputManager.SetButton_Back(_Player2_X360_Gamepad_Port, true);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_RETURN)
                    {
                        _XOutputManager.SetButton_Start(_Player2_X360_Gamepad_Port, true);
                    }

                }
                else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                {
                    if (s.scanCode == HardwareScanCode.DIK_NUMPAD8)
                    {
                        _XOutputManager.SetRAxis_Y(_Player2_X360_Gamepad_Port, 0);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_NUMPAD2)
                    {
                        _XOutputManager.SetRAxis_Y(_Player2_X360_Gamepad_Port, 0);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_NUMPAD4)
                    {
                        _XOutputManager.SetRAxis_X(_Player2_X360_Gamepad_Port, 0);
                    }
                    if (s.scanCode == HardwareScanCode.DIK_NUMPAD6)
                    {
                        _XOutputManager.SetRAxis_X(_Player2_X360_Gamepad_Port, 0);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_Q)
                    {
                        _XOutputManager.SetButton_A(_Player2_X360_Gamepad_Port, false);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_B)
                    {
                        _XOutputManager.SetButton_B(_Player2_X360_Gamepad_Port, false);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_X)
                    {
                        _XOutputManager.SetButton_X(_Player2_X360_Gamepad_Port, false);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_Y)
                    {
                        _XOutputManager.SetButton_Y(_Player2_X360_Gamepad_Port, false);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_G)
                    {
                        _XOutputManager.SetButton_Guide(_Player2_X360_Gamepad_Port, false);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_BACK)
                    {
                        _XOutputManager.SetButton_Back(_Player2_X360_Gamepad_Port, false);
                    }
                    else if (s.scanCode == HardwareScanCode.DIK_RETURN)
                    {
                        _XOutputManager.SetButton_Start(_Player2_X360_Gamepad_Port, false);
                    }
                }
            }
            return Win32API.CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
        }

        #endregion
    }
}
