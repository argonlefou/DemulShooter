using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_WndBE : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\windows\bestate";

        /*** MEMORY ADDRESSES **/
        private UInt32 _P1_X_CaveAddress;
        private UInt32 _P1_Y_CaveAddress;
        private UInt32 _P2_X_CaveAddress;
        private UInt32 _P2_Y_CaveAddress;
        private UInt32 _P1_X_Injection_Offset_1 = 0x008F09FE;
        private UInt32 _P1_X_Injection_Return_Offset_1 = 0x008F0A04;
        private UInt32 _P1_X_Injection_Offset_2 = 0x008F0CD2;
        private UInt32 _P1_X_Injection_Return_Offset_2 = 0x008F0CDA;
        private UInt32 _P1_Y_Injection_Offset_1 = 0x008F0A0B;
        //private UInt32 _P1_Y_Injection_Return_Offset_1 = 0x008F0A13;
        private UInt32 _P1_Y_Injection_Offset_2 = 0x008F0CFF;
        //private UInt32 _P1_Y_Injection_Return_Offset_2 = 0x008F0D07;
        private UInt32 _P2_Axis_Injection_Offset_1 = 0x008F0E20;
        //private UInt32 _P2_Axis_Injection_Return_Offset_1 = 0x008F0E29;
        private UInt32 _P2_Axis_Injection_Offset_2 = 0x008F0E49;
        //private UInt32 _P2_Axis_Injection_Return_Offset_2 = 0x008F0E52;

        //Keys to send
        //For player 2 if used, keys are choosed for x360kb.ini:
        //I usually prefer to send VirtualKeycodes (less troublesome when no physical Keyboard is plugged)
        //But with x360kb only DIK keycodes are working
        private HardwareScanCode _P2_Trigger_DIK = HardwareScanCode.DIK_T;
        private HardwareScanCode _P2_Reload_DIK = HardwareScanCode.DIK_Y;

        //Custom data to inject
        private float _P1_X_Value;
        private float _P1_Y_Value;
        private float _P2_X_Value;
        private float _P2_Y_Value;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndBE(String RomName, String GamePath, bool DisableInputHack, bool Verbose)
            : base(RomName, "BEGame", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Blue Estate CODEX - Cracked", "188605d4083377e4ee3552b4c89f52fb");

            // To play as Player2 the game needs a Joypad
            // By using x360kb.ini and xinput1_3.dll in the game's folder, we can add a virtual X360 Joypad to act as player 2
            /*try
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
            }*/

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
                                Apply_MemoryHacks();
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

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X => [-1 ; 1] float
                    //Y => [-1 ; 1] float

                    float X_Value = (2.0f * PlayerData.RIController.Computed_X / TotalResX) - 1.0f;
                    float Y_Value = (2.0f * PlayerData.RIController.Computed_Y / TotalResY) - 1.0f;

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

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _P1_X_CaveAddress = _InputsDatabank_Address;
            _P1_Y_CaveAddress = _InputsDatabank_Address + 0x04;
            _P2_X_CaveAddress = _InputsDatabank_Address + 0x20;
            _P2_Y_CaveAddress = _InputsDatabank_Address + 0x24;

            SetHack_P1X();
            SetHack_P1X_2();
            SetHack_P1Y((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Y_Injection_Offset_1);
            SetHack_P1Y((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Y_Injection_Offset_2);
            SetHack_P2Axis((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Axis_Injection_Offset_1);
            SetHack_P2Axis((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Axis_Injection_Offset_2);
            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
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

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0) 
                {
                    //Apply_OR_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0) 
                {
                    //Apply_AND_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFE);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0) 
                {

                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0) 
                {

                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0) 
                {
                    //Apply_OR_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x02);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0) 
                {
                    //Apply_AND_ByteMask((int)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFD);
                }
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
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0) 
                    SendKeyDown(_P2_Reload_DIK);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0) 
                    SendKeyUp(_P2_Reload_DIK);
            }
        }

        #endregion        
    }
}
