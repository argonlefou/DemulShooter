using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooterX64
{
    public class Game_S357SailorZombie : Game
    {
        //MEMORY ADDRESSES
        /*private UInt64 _GameCode_Ptr_Offset = 0x03C37700;
        private UInt64 _GameCode_Address = 0;
        private byte[] _GameLoadedInstruction = new byte[] { 0x48, 0x83, 0xEC, 0x28 };*/
        private UInt64 _P1_Axis_Address = 0x336CA6E00;
        private UInt64 _P2_Axis_Address = 0x336CA6E28;
        private UInt64 _Buttons_Address = 0x300D91B1D;

        //Outputs
        private UInt64 _Outputs_Address = 0x300D92248;
        private UInt64 _Credits_Address = 0x300D91AEB;
        private int _P1_LastRumble = 0;
        private int _P2_LastRumble = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_S357SailorZombie(String RomName)
            : base(RomName, "rpcs3-gun")
        {
            _KnownMd5Prints.Add("RPCS3 v0.0.27 fork for System 357, GUN version", "3321f7771ae74e8027f0d4e18167d635");
            _KnownMd5Prints.Add("RPCS3 v0.0.37-2ef2310e Alpha (TeknoParrot)", "56B69E6A8D95FE0AC7BF7BB5D57321DC");
            _tProcess.Start();
            Logger.WriteLog("Waiting for RPCS3 Sailor Zombie " + _RomName + " game to hook.....");
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
                            // The game may start with other Windows than the main one (BepInEx console, other stuff.....) so we need to filter
                            // the displayed window according to the Title, if DemulShooter is started before the game,  to hook the correct one
                            if (FindGameWindow_Contains("Sailor zombie") || FindGameWindow_Contains("RPCS3 via TeknoParrot"))
                            {
                                Check_PatchedFiles_Ok();
                                CheckExeMd5();
                                if (_DisableInputHack)
                                    Logger.WriteLog("Input Hack disabled");
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
                            }
                            else
                            {
                                Logger.WriteLog("Game Window not found");
                                return;
                            }  

                            /*_GameCode_Address = ReadPtrChain((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _GameCode_Ptr_Offset), new UInt64[] { 0xB0, 0x00, 0x00, 0x30, 0x00 });
                            if (_GameCode_Address != 0)
                            {
                                Logger.WriteLog("EBOT.BIN PPU cache memory segment address = 0x" + _GameCode_Address.ToString("X16"));
                                byte[] TestLoadedInstruction = ReadBytes((IntPtr)_GameCode_Address, 4);

                                if (TestLoadedInstruction[0] == 0x48 && TestLoadedInstruction[1] == 0x83 && TestLoadedInstruction[2] == 0xEC && TestLoadedInstruction[3] == 0x28)
                                {
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
                                    Logger.WriteLog("MainWindowHandle = 0x" + _TargetProcess.MainWindowHandle.ToString("X16"));
                                    Logger.WriteLog("MainWindowTitle" + _TargetProcess.MainWindowTitle);

                                    CheckExeMd5();
                                    if (_DisableInputHack)
                                        Logger.WriteLog("Input Hack disabled");
                                    _ProcessHooked = true;
                                    RaiseGameHookedEvent();
                                }
                                else
                                {
                                    string s = string.Empty;
                                    foreach (byte b in TestLoadedInstruction)
                                        s += b.ToString("X2") + " ";
                                    Logger.WriteLog("Read bytes at address 0x" + _GameCode_Address.ToString("X16") + " : " + s);

                                    s = string.Empty;
                                    foreach (byte b in _GameLoadedInstruction)
                                        s += b.ToString("X2") + " ";
                                    Logger.WriteLog("Expected : " + s);

                                    Logger.WriteLog("ROM not Loaded...");
                                }
                            }
                            else
                            {
                                Logger.WriteLog("ROM not Loaded...");
                            }*/
                        }
                    }
                }
                catch (Exception)
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

        #region PPU cache check

        //Check if pre-patched PPU cache files are present in the cache folder
        //If not, the game won't be able to be control by DemulShooter
        private void Check_PatchedFiles_Ok()
        {
            String EmulatorBasePath = _TargetProcess.MainModule.FileName.Substring(0, _TargetProcess.MainModule.FileName.Length - 13);
            Logger.WriteLog("Emulator path = " + EmulatorBasePath);

            String PPU_CachePath = EmulatorBasePath + @"cache\SCEEXE000\ppu-se1PtZVS5iF9A6M5Y1vu0wNqiASu-EBOOT.BIN";
            if (!Directory.Exists(PPU_CachePath))
            {
                Logger.WriteLog("Error : PPU cache folder not found (" + PPU_CachePath + ")");
                return;
            }

            string CPU_Type = string.Empty;
            foreach (String File in Directory.GetFiles(PPU_CachePath))
            {
                String sFile = Path.GetFileName(File);

                if (sFile.StartsWith("v5-kusa-1jdW1xvsTpXqZVxiPnPS3q-00001G"))
                {
                    try
                    {
                        CPU_Type = sFile.Split(new string[] { ".obj.gz" }, StringSplitOptions.None)[0].Split(new string[] { "0001G-" }, StringSplitOptions.None)[1];
                    }
                    catch
                    {
                        Logger.WriteLog("Error : Impossible to detect LLVM CPU type");
                    }
                    if (CPU_Type != string.Empty)
                        break;
                }
            }

            if (CPU_Type != string.Empty)
            {
                Logger.WriteLog("LLVM PPU processor is " + CPU_Type);
                CompareMd5Hash(PPU_CachePath + @"\v5-kusa-aQwdyxfu7HwmKZwJpsrgjQ-00001G-" + CPU_Type + ".obj.gz", "cbb6d336413f1d068e64d90eb800d429");
                CompareMd5Hash(PPU_CachePath + @"\v5-kusa-rvYuWdKw9d618McpKm75ir-00001G-" + CPU_Type + ".obj.gz", "c13883a893110edb87029d1e61949555");
            }
            else
            {
                Logger.WriteLog("WARNING : PPU cache not found => DemulShooter may not work correctly");
            }
        }

        private void CompareMd5Hash(String Filename, String AwaitedMd5)
        {
            if (!File.Exists(Filename))
            {
                Logger.WriteLog("WARNING : PPU cache file not found : " + Filename + " => DemulShooter may not work correctly");
            }
            else
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(Filename))
                    {
                        var hash = md5.ComputeHash(stream);
                        String StrHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                        Logger.WriteLog(Filename + " MD5 Checksum = " + StrHash);
                        if (StrHash != AwaitedMd5)
                            Logger.WriteLog(@"/!\ MD5 Hash unknown, DemulShooter may not work correctly /!\");
                        else
                            Logger.WriteLog("MD5 Check OK");
                    }
                }
            }
        }

        #endregion

        #region Screen

        /// <summary>
        /// Replacing original function using TargetProcess main window handle by this one, using a specific window handle
        /// </summary>
        /// <param name="PlayerData"></param>
        /// <returns></returns>
        public override bool ClientScale(PlayerSettings PlayerData)
        {
            //Convert Screen location to Client location
            if (_TargetProcess != null)
            {
                //Window size
                Rect TotalRes = new Rect();
                Win32API.GetWindowRect(_GameWindowHandle, ref TotalRes);

                Logger.WriteLog("Window position (Px) = [ " + TotalRes.Left + ";" + TotalRes.Top + " ]");

                PlayerData.RIController.Computed_X = PlayerData.RIController.Computed_X - TotalRes.Left;
                PlayerData.RIController.Computed_Y = PlayerData.RIController.Computed_Y - TotalRes.Top;
                Logger.WriteLog("Onclient window position (Px) = [ " + PlayerData.RIController.Computed_X + "x" + PlayerData.RIController.Computed_Y + " ]");

            }
            return true;
        }

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// Windowed mode is not working (can't get window size) so full screen mode only is simpler....
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

                    double dMaxX = 1920.0;
                    double dMaxY = 1080.0;

                    PlayerData.RIController.Computed_X = Convert.ToInt32(dMaxX * PlayerData.RIController.Computed_X / TotalResX);
                    PlayerData.RIController.Computed_Y = Convert.ToInt32(dMaxY * PlayerData.RIController.Computed_Y / TotalResY);
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

        #region Input

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>   
        public override void SendInput(PlayerSettings PlayerData)
        {
            if (!_DisableInputHack)
            {
                byte[] bufferX = BitConverter.GetBytes((UInt16)PlayerData.RIController.Computed_X);
                byte[] bufferY = BitConverter.GetBytes((UInt16)PlayerData.RIController.Computed_Y);
                Array.Reverse(bufferX);
                Array.Reverse(bufferY);

                if (PlayerData.ID == 1)
                {
                    WriteBytes((IntPtr)(_P1_Axis_Address), bufferX);
                    WriteBytes((IntPtr)(_P1_Axis_Address + 2), bufferY);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        Apply_OR_ByteMask((IntPtr)(_Buttons_Address + 1), 0x80);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        Apply_AND_ByteMask((IntPtr)(_Buttons_Address + 1), 0x7F);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        Apply_OR_ByteMask((IntPtr)(_Buttons_Address + 1), 0x40);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        Apply_AND_ByteMask((IntPtr)(_Buttons_Address + 1), 0xBF);
                }

                else if (PlayerData.ID == 2)
                {
                    WriteBytes((IntPtr)(_P2_Axis_Address), bufferX);
                    WriteBytes((IntPtr)(_P2_Axis_Address + 2), bufferY);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        Apply_OR_ByteMask((IntPtr)(_Buttons_Address + 1), 0x10);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        Apply_AND_ByteMask((IntPtr)(_Buttons_Address + 1), 0xEF);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        Apply_OR_ByteMask((IntPtr)(_Buttons_Address + 1), 0x08);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        Apply_AND_ByteMask((IntPtr)(_Buttons_Address + 1), 0xF7);
                }
            }
        }

        /// <summary>
        /// Low-level Keyboard hook callback.
        /// This is used to replace system keys : 
        /// Byte #1
        /// 0x10 -> Down
        /// 0x20 -> Up
        /// 0x40 -> Service
        /// 0x02 -> Enter
        /// Byte #2
        /// 0x10 -> 2P Trigger Left
        /// 0x20 -> Start 1
        /// 0x40 -> 1P Trigger Right
        /// 0x80 -> 1P Trigger Left
        /// 0x04 -> Start 2
        /// 0x08 -> 2P Trigger Right
        /// </summary>
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (!_DisableInputHack)
            {
                if (nCode >= 0)
                {
                    KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                    if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                    {
                        if (s.scanCode == Configurator.GetInstance().DIK_Rpcs3_P1_Start)
                            Apply_OR_ByteMask((IntPtr)(_Buttons_Address + 1), 0x20);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Rpcs3_P2_Start)
                            Apply_OR_ByteMask((IntPtr)(_Buttons_Address + 1), 0x04);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Rpcs3_Down)
                            Apply_OR_ByteMask((IntPtr)_Buttons_Address, 0x10);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Rpcs3_Up)
                            Apply_OR_ByteMask((IntPtr)_Buttons_Address, 0x20);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Rpcs3_Service)
                            Apply_OR_ByteMask((IntPtr)_Buttons_Address, 0x40);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Rpcs3_Enter)
                            Apply_OR_ByteMask((IntPtr)_Buttons_Address, 0x02);
                    }
                    else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                    {
                        if (s.scanCode == Configurator.GetInstance().DIK_Rpcs3_P1_Start)
                            Apply_AND_ByteMask((IntPtr)(_Buttons_Address + 1), 0xDF);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Rpcs3_P2_Start)
                            Apply_AND_ByteMask((IntPtr)(_Buttons_Address + 1), 0xFB);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Rpcs3_Down)
                            Apply_AND_ByteMask((IntPtr)_Buttons_Address, 0xEF);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Rpcs3_Up)
                            Apply_AND_ByteMask((IntPtr)_Buttons_Address, 0xDF);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Rpcs3_Service)
                            Apply_AND_ByteMask((IntPtr)_Buttons_Address, 0xBF);
                        else if (s.scanCode == Configurator.GetInstance().DIK_Rpcs3_Enter)
                            Apply_AND_ByteMask((IntPtr)_Buttons_Address, 0xFD);
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
            //Gun motor : Is activated for every bullet fired AND when player gets
            _Outputs = new List<GameOutput>();

            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpRoom, OutputId.LmpRoom));
            //
            _Outputs.Add(new GameOutput(OutputDesciption.P1_AirFront, OutputId.P1_AirFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_AirFront, OutputId.P2_AirFront));
            _Outputs.Add(new GameOutput(OutputDesciption.VibrationSeat, OutputId.VibrationSeat));
            //Custom Outputs
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            /*_Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));*/
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //LEDs
            byte bLEDs = ReadByte((IntPtr)(_Outputs_Address + 1));
            SetOutputValue(OutputId.P1_LmpStart, bLEDs >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, bLEDs >> 6 & 0x01);
            SetOutputValue(OutputId.LmpRoom, ReadByte((IntPtr)(_Outputs_Address + 18)) & 0x0F);  //Analog ? Goes from 0xF0 to 0xFF and back to F0
            //Other
            byte bMech = ReadByte((IntPtr)_Outputs_Address);
            SetOutputValue(OutputId.P1_AirFront, bMech >> 3 & 0x01);
            SetOutputValue(OutputId.P2_AirFront, bMech >> 2 & 0x01);            

            //Seat vibration is coded on 2 Bits, and can have 4 Values (OFF/LOW/MEDIUM/HIGH)
            int SeatVbr = bLEDs >> 1 & 0x03;
            if (SeatVbr == 0)
                SetOutputValue(OutputId.VibrationSeat, 0);
            else if (SeatVbr == 1)
                SetOutputValue(OutputId.VibrationSeat, 2);
            else if (SeatVbr == 2)
                SetOutputValue(OutputId.VibrationSeat, 1);
            else if (SeatVbr == 3)
                SetOutputValue(OutputId.VibrationSeat, 3);

            //Rumble is using bit-changing values on a bi-stable state (not monostable) on the higher 4bits of the byte 
            // [Bit3] [Bit2] [Bit1] [Bit0] 
            //P1 rumble activation swap 1 from Bit3/Bit2 (i.e : 8 -> 7-> 8-> 7) 
            //P2 rumble activation swap 1 from Bit1/Bit0 (i.e : 1 -> 0-> 1-> 0) 
            int P1_Rumble = bMech & 0xC0;
            int P2_Rumble = bMech & 0x30;

            if (_P1_LastRumble != 0 && P1_Rumble != _P1_LastRumble && P1_Rumble != 0)
            {
                SetOutputValue(OutputId.P1_GunMotor, 1);
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
            }
            else
                SetOutputValue(OutputId.P1_GunMotor, 0);

            if (_P2_LastRumble != 0 && P2_Rumble != _P2_LastRumble && P2_Rumble != 0)
            {
                SetOutputValue(OutputId.P2_GunMotor, 1);
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
            }
            else
                SetOutputValue(OutputId.P2_GunMotor, 0);

            _P1_LastRumble = P1_Rumble;
            _P2_LastRumble = P2_Rumble;

            SetOutputValue(OutputId.Credits, ReadByte((IntPtr)_Credits_Address));
        }

        #endregion
        
    }
}
