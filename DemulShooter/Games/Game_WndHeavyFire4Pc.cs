using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    /*** Heavy Fire 4 => Heavy Fire Shattered Spear ***/
    class Game_WndHeavyFire4Pc : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\windows\hfss";
        private const String HFA_STEAM_FILENAME = "hf4.exe";
        private const String HFA_FILENAME = "HeavyFire4.exe";
        private String _ExecutableFilePath = String.Empty;
        private bool _EnableP2 = false;

        /*** MEMORY ADDRESSES **/
        private UInt32 _P1_X_CaveAddress;
        private UInt32 _P1_Y_CaveAddress;
        private UInt32 _P1_Buttons_Offset = 0x0027D040;
        private NopStruct _Nop_P1_Buttons = new NopStruct(0x0013D8E9, 7);
        private UInt32 _P2_X_CaveAddress;
        private UInt32 _P2_Y_CaveAddress;
        private UInt32 _P1_X_Injection_Offset = 0x000218DF;
        private UInt32 _P1_X_Injection_Return_Offset = 0x000218E6;
        private UInt32 _P1_Y_Injection_Offset = 0x000218FC;
        private UInt32 _P1_Y_Injection_Return_Offset = 0x00021903;
        private UInt32 _P2_X_Injection_Offset = 0x000238B2;
        private UInt32 _P2_X_Injection_Return_Offset = 0x000238B9;
        private UInt32 _P2_Y_Injection_Offset = 0x000238CB;
        private UInt32 _P2_Y_Injection_Return_Offset = 0x000238D2;

        //Keys to send
        //For Player 1, hardcoded by the game :
        //Cover Left = A
        //Cover Bottom = S
        //Cover Right = D
        //QTE = Space
        private VirtualKeyCode _P1_QTE_W_VK = VirtualKeyCode.VK_W;
        private VirtualKeyCode _P1_CoverLeft_VK = VirtualKeyCode.VK_A;
        private VirtualKeyCode _P1_CoverRight_VK = VirtualKeyCode.VK_D;
        private VirtualKeyCode _P1_CoverBottom_VK = VirtualKeyCode.VK_S;
        private VirtualKeyCode _P1_QTE_Space_VK = VirtualKeyCode.VK_SPACE;
        //For player 2 if used, keys are choosed for x360kb.ini:
        //I usually prefer to send VirtualKeycodes (less troublesome when no physical Keyboard is plugged)
        //But with x360kb only DIK keycodes are working
        private HardwareScanCode _P2_Trigger_DIK = HardwareScanCode.DIK_T;
        private HardwareScanCode _P2_Reload_DIK = HardwareScanCode.DIK_U;
        private HardwareScanCode _P2_CoverLeft_DIK = HardwareScanCode.DIK_I;
        private HardwareScanCode _P2_CoverDown_DIK = HardwareScanCode.DIK_O;
        private HardwareScanCode _P2_CoverRight_DIK = HardwareScanCode.DIK_P;

        //Keys to read
        private const HardwareScanCode _P1_Grenade_ScanCode = HardwareScanCode.DIK_G;

        //Custom data to inject
        protected float _P1_X_Value;
        protected float _P1_Y_Value;
        protected float _P2_X_Value;
        protected float _P2_Y_Value;

        protected float _Axis_X_Min;
        protected float _Axis_X_Max;
        protected bool _Reversecover = false;
        protected float _CoverDelta = 0.3f;
        protected bool _CoverLeftEnabled = false;
        protected bool _CoverBottomEnabled = false;
        protected bool _CoverRightEnabled = false;
        protected bool _QTE_Spacebar_Enabled = false;
        protected bool _QTE_W_Enabled = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndHeavyFire4Pc(String RomName, String GamePath, int CoverSensibility, bool Reversecover, bool EnableP2, bool DisableInputHack, bool Verbose) 
            : base(RomName, "HeavyFire4", DisableInputHack, Verbose)
        {
            _ExecutableFilePath = GamePath + @"\" + HFA_FILENAME;

            Logger.WriteLog("Game path : " + GamePath);

            //Steam version of the game has a different .exe name
            if (File.Exists(GamePath + @"\" + HFA_STEAM_FILENAME))
            {
                _Target_Process_Name = "hf4";
                _ExecutableFilePath = GamePath + @"\" + HFA_STEAM_FILENAME;
            }
            Logger.WriteLog("Executable file path : " + _ExecutableFilePath);

            _KnownMd5Prints.Add("Heavy Fire 4 - MASTIFF", "9476f9bba48aea6ca04d06158be07f1c");
            _KnownMd5Prints.Add("Heavy Fire 4 - STEAM", "7f8bf20aaba80ac1239efc553d94a53f");

            _Reversecover = Reversecover;
            _EnableP2 = EnableP2;
            _CoverDelta = (float)CoverSensibility / 10.0f;
            Logger.WriteLog("Setting Cover delta to screen border to " + _CoverDelta.ToString());

            // To play as Player2 the game needs a Joypad
            // The game is detecting each Aimtrack as additionnal Joypad but unfortunatelly, each detected Joypad HAS TO play.
            // Copying the dinput8.dll file blocks Dinput so no more gamepads with Aimtrak
            // And by using x360kb.ini and xinput1_3.dll in the game's folder, we can add a virtual X360 Joypad to act as player 2
            // Again, to play solo we need to set the x360kb.ini accordingly so that no Joypad is emulated
            try
            {
                using (StreamWriter sw = new StreamWriter(GamePath + @"\x360kb.ini", false))
                {
                    if (_EnableP2)
                        sw.Write(DemulShooter.Properties.Resources.x360kb_hfirea2p);
                    else
                        sw.Write(DemulShooter.Properties.Resources.x360kb_hfirea1p);
                }
                Logger.WriteLog("File \"" + GamePath + "\\x360kb.ini\" successfully written !");
            }
            catch (Exception ex)
            {
                Logger.WriteLog("Error trying to write file " + GamePath + "\\x360kb.ini\" :" + ex.Message.ToString());
            }

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

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            _GameWindowHandle = _TargetProcess.MainWindowHandle;
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();
                            ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                            if (!_DisableInputHack)
                                SetHack();
                            else
                                Logger.WriteLog("Input Hack disabled");
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

                    //Y => [-1 ; 1] float
                    //X => depend on ration Width/Height (ex : [-1.7777; 1.7777] with 1920x1080)

                    float fRatio = (float)TotalResX / (float)TotalResY;
                    _Axis_X_Min = -fRatio;
                    _Axis_X_Max = fRatio;

                    float _Y_Value = (2.0f * PlayerData.RIController.Computed_Y / (float)TotalResY) - 1.0f;
                    float _X_Value = (fRatio * 2 * PlayerData.RIController.Computed_X / (float)TotalResX) - fRatio;

                    if (_X_Value < _Axis_X_Min)
                        _X_Value = _Axis_X_Min;
                    if (_Y_Value < -1.0f)
                        _Y_Value = -1.0f;
                    if (_X_Value > _Axis_X_Max)
                        _X_Value = _Axis_X_Max;
                    if (_Y_Value > 1.0f)
                        _Y_Value = 1.0f;

                    if (PlayerData.ID == 1)
                    {
                        _P1_X_Value = _X_Value;
                        _P1_Y_Value = _Y_Value;
                    }
                    else if (PlayerData.ID == 2)
                    {
                        _P2_X_Value = _X_Value;
                        _P2_Y_Value = _Y_Value;
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

        private void SetHack()
        {
            CreateDataBank();
            SetHack_P1X();
            SetHack_P1Y();
            SetHack_P2X();
            SetHack_P2Y();
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_Buttons);
            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Creating a custom memory bank to store our datas
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
        /// All Axis codecave are the same :
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

            //fstp [esi+edi*8+00000114]
            CaveMemory.Write_StrBytes("D9 9C FE 14 01 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [P1_X]
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P1_X_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov [esi+edi*8+00000114], eax
            CaveMemory.Write_StrBytes("89 84 FE 14 01 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Return_Offset);

            Logger.WriteLog("Adding P1 X Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _P1_X_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        private void SetHack_P1Y()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //fstp [esi+edi*8+00000118]
            CaveMemory.Write_StrBytes("D9 9C FE 18 01 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [P1_Y]
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P1_Y_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov [esi+edi*8+00000118], eax
            CaveMemory.Write_StrBytes("89 84 FE 18 01 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Y_Injection_Return_Offset);

            Logger.WriteLog("Adding P1 Y Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Y_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Y_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        private void SetHack_P2X()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //fstp [edi+esi*8+00000114]
            CaveMemory.Write_StrBytes("D9 9C F7 14 01 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [P2_X]
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P2_X_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov [edi+esi*8+00000114], eax
            CaveMemory.Write_StrBytes("89 84 F7 14 01 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_X_Injection_Return_Offset);

            Logger.WriteLog("Adding P2 X Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_X_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _P2_X_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        private void SetHack_P2Y()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //fstp [edi+esi*8+00000118]
            CaveMemory.Write_StrBytes("D9 9C F7 18 01 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [P2_Y]
            CaveMemory.Write_StrBytes("A1");
            byte[] b = BitConverter.GetBytes(_P2_Y_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov [edi+esi*8+00000118], eax
            CaveMemory.Write_StrBytes("89 84 F7 18 01 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Y_Injection_Return_Offset);

            Logger.WriteLog("Adding P2 Y Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Y_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Y_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
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

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFE);


                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0) 
                {
                    //If the player is aiming on the left side of the screen before pressing the button
                    //=> Cover Left
                    if ((_P1_X_Value < _Axis_X_Min + _CoverDelta) && !_Reversecover)
                    {
                        Send_VK_KeyDown(_P1_CoverLeft_VK);
                        _CoverLeftEnabled = true;
                    }
                    //If the player is aiming on the right side of the screen before pressing the button
                    //=> Cover Left
                    else if ((_P1_X_Value > _Axis_X_Max - _CoverDelta) && _Reversecover)
                    {
                        Send_VK_KeyDown(_P1_CoverLeft_VK);
                        _CoverLeftEnabled = true;
                    }

                    //If the player is aiming on the right side of the screen before pressing the button
                    //=> Cover Right
                    else if ((_P1_X_Value > _Axis_X_Max - _CoverDelta) && !_Reversecover)
                    {
                        Send_VK_KeyDown(_P1_CoverRight_VK);
                        _CoverRightEnabled = true;
                    }
                    //If the player is aiming on the left side of the screen before pressing the button
                    //=> Cover Right
                    else if ((_P1_X_Value < _Axis_X_Min + _CoverDelta) && _Reversecover)
                    {
                        Send_VK_KeyDown(_P1_CoverRight_VK);
                        _CoverRightEnabled = true;
                    }

                    //If the player is aiming on the bottom side of the screen before pressing the button
                    //=> Cover Down
                    else if (_P1_Y_Value > (1.0f - _CoverDelta))
                    {
                        Send_VK_KeyDown(_P1_CoverBottom_VK);
                        _CoverBottomEnabled = true;
                    }
                    //If the player is aiming on the top side of the screen before pressing the button
                    //=> W [QTE]
                    else if (_P1_Y_Value < (-1.0f + _CoverDelta))
                    {
                        Send_VK_KeyDown(_P1_QTE_W_VK);
                        _QTE_W_Enabled = true;
                    }
                    //If nothing above
                    //=> Spacebar [QTE]
                    else
                    {
                        Send_VK_KeyDown(_P1_QTE_Space_VK);
                        _QTE_Spacebar_Enabled = true;
                    }
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0) 
                {
                    if (_CoverLeftEnabled)
                    {
                        Send_VK_KeyUp(_P1_CoverLeft_VK);
                        _CoverLeftEnabled = false;
                    }
                    if (_CoverBottomEnabled)
                    {
                        Send_VK_KeyUp(_P1_CoverBottom_VK);
                        _CoverBottomEnabled = false;
                    }
                    if (_CoverRightEnabled)
                    {
                        Send_VK_KeyUp(_P1_CoverRight_VK);
                        _CoverRightEnabled = false;
                    }
                    if (_QTE_W_Enabled)
                    {
                        Send_VK_KeyUp(_P1_QTE_W_VK);
                        _QTE_W_Enabled = false;
                    }
                    if (_QTE_Spacebar_Enabled)
                    {
                        Send_VK_KeyUp(_P1_QTE_Space_VK);
                        _QTE_Spacebar_Enabled = false;
                    }
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFD);
            }
            else if (PlayerData.ID == 2)
            {
                //Setting Values in memory for the Codecave to read it
                byte[] buffer = BitConverter.GetBytes(_P2_X_Value);
                WriteBytes(_P2_X_CaveAddress, buffer);
                buffer = BitConverter.GetBytes(_P2_Y_Value);
                WriteBytes(_P2_Y_CaveAddress, buffer);

                // Player 2 buttons are simulated by x360kb.ini so we just send needed Keyboard strokes
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0) 
                    SendKeyDown(_P2_Trigger_DIK);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0) 
                    SendKeyUp(_P2_Trigger_DIK);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0) 
                {
                    if ((_P2_X_Value < _Axis_X_Min + _CoverDelta) && !_Reversecover)
                    {
                        SendKeyDown(_P2_CoverLeft_DIK);
                    }
                    else if ((_P2_X_Value > _Axis_X_Max - _CoverDelta) && !_Reversecover)
                    {
                        SendKeyDown(_P2_CoverLeft_DIK);
                    }
                    else if ((_P2_X_Value > _Axis_X_Max - _CoverDelta) && _Reversecover)
                    {
                        SendKeyDown(_P2_CoverRight_DIK);
                    }
                    else if ((_P2_X_Value < _Axis_X_Min + _CoverDelta) && _Reversecover)
                    {
                        SendKeyDown(_P2_CoverRight_DIK);
                    }
                    else if (_P2_Y_Value > (1.0f - _CoverDelta))
                    {
                        SendKeyDown(_P2_CoverDown_DIK);
                    }        
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0) 
                {
                    SendKeyUp(_P2_CoverLeft_DIK);
                    SendKeyUp(_P2_CoverDown_DIK);
                    SendKeyUp(_P2_CoverRight_DIK);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0) 
                    SendKeyDown(_P2_Reload_DIK);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0) 
                    SendKeyUp(_P2_Reload_DIK);
            }
        }

        /// <summary>
        /// Low-level Keyboard hook callback.
        /// This is used to allow the usage of Grenades
        /// </summary>
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    switch (s.scanCode)
                    {
                        case _P1_Grenade_ScanCode:
                            {
                                Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x04);
                            } break;
                        default:
                            break;
                    }
                }
                else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                {
                    switch (s.scanCode)
                    {
                        case _P1_Grenade_ScanCode:
                            {
                                Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFB);
                            } break;
                        default:
                            break;
                    }
                }
            }
            return Win32API.CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
        }

        #endregion

    }
}
