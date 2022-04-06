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
    /// <summary>
    /// For this hack, Cxbx-Reloaded must be started with the following command line :
    /// Cxbx-Reloaded.exe /load [PATH_TO_ROM]
    /// This will result in one (and only one Process), and easier to target and get window handle,
    /// whereas running Cxbx GUI, then choosing a Rom will create 2 different processes (sources : Cxbx Wiki)
    /// </summary>
    public class Game_CxbxVcop3 : Game
    {
        /*** MEMORY ADDRESSES **/
        private UInt32 _P1_X_Offset = 0x3578F4;
        private UInt32 _P1_Y_Offset = 0x3578F8;
        private UInt32 _P2_X_Offset = 0x3579CC;
        private UInt32 _P2_Y_OFfset = 0x3579D0;
        private UInt32 _Buttons_Injection_Offset = 0x6C9B0;
        private UInt32 _Buttons_Injection_Return_Offset = 0x6C9B8;        
        private NopStruct _Nop_X_1 = new NopStruct(0x0006A3D8, 3);
        private NopStruct _Nop_X_2 = new NopStruct(0x0006A403, 3);
        private NopStruct _Nop_Y_1 = new NopStruct(0x0006A41E, 3);
        private NopStruct _Nop_Y_2 = new NopStruct(0x0006A3F2, 3);
        private UInt32 _RomLoaded_CheckIntructionn_Offset = 0x0006A3D8;

        private UInt32 _P1_Buttons_CaveAddress = 0;
        private UInt32 _P2_Buttons_CaveAddress = 0;

        //Outputs
        private int _P1_LastLife = 0;
        private int _P2_LastLife = 0;
        private int _P1_LastAmmo = 0;
        private int _P2_LastAmmo = 0;
        private int _P1_Life = 0;
        private int _P2_Life = 0;
        private int _P1_Ammo = 0;
        private int _P2_Ammo = 0;
       
        /// <summary>
        /// Constructor
        /// </summary>
        public Game_CxbxVcop3(String RomName, double ForcedXratio, bool DisableInputHack, bool Verbose)
            : base(RomName, "cxbx", ForcedXratio, DisableInputHack, Verbose)
        {
            _tProcess.Start();
            Logger.WriteLog("Waiting for Chihiro " + _RomName + " game to hook.....");
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
                            //This is HEX code for the instrution we're testing to see which process is the good one to hook
                            byte[] bTest = new byte[] { 0x8B, 0xE8 };

                            Logger.WriteLog("Testing instruction at 0x" + ((UInt32)_TargetProcess_MemoryBaseAddress + _RomLoaded_CheckIntructionn_Offset - 2).ToString("X8"));
                            byte[] b = ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _RomLoaded_CheckIntructionn_Offset - 2, 2);
                            Logger.WriteLog("Waiting for : 0x" + bTest[0].ToString("X2") + ", 0x" + bTest[1].ToString("X2"));
                            Logger.WriteLog("Read values : 0x" + b[0].ToString("X2") + ", 0x" + b[1].ToString("X2"));
                            if (b[0] == bTest[0] && b[1] == bTest[1])
                            {
                                Logger.WriteLog("Correct process for code injection");                
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog("WindowHandle = " + _TargetProcess.MainWindowHandle.ToString());
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                if (!_DisableInputHack)
                                    SetHack();
                                else
                                    Logger.WriteLog("Input Hack disabled");
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();  
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
                    //Window size
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    Win32API.GetWindowRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    Logger.WriteLog("Game client window location (Px) = [ " + TotalRes.Left + " ; " + TotalRes.Top + " ]");

                    //X => [-320 ; +320] => 640
                    //Y => [-240; +240] => 480
                    double dMinX = -320.0;
                    double dMaxX = 320.0;
                    double dMinY = -240.0;
                    double dMaxY = 240.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    //In case of forced scren ration (4/3)
                    if (_ForcedXratio > 0)
                    {
                        Logger.WriteLog("Forcing X Ratio to = " + _ForcedXratio.ToString());
                        double GameHeight = TotalResY;
                        double GameWidth = TotalResY * _ForcedXratio;
                        Logger.WriteLog("Game Viewport size (Px) = [ " + GameWidth + "x" + GameHeight + " ]");

                        double HorizontalRatio = TotalResX / GameWidth;
                        dRangeX = dRangeX * HorizontalRatio;
                        dMaxX = (dRangeX / 2);
                        dMinX = -dMaxX;
                        Logger.WriteLog("Horizontal Ratio = " + HorizontalRatio.ToString());
                        Logger.WriteLog("New dMaxX = " + dMaxX.ToString());
                        Logger.WriteLog("New dMinX = " + dMinX.ToString());
                    }

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

        private void SetHack()
        {   
            //Creating data bank
            //Codecave :
            Codecave CaveMemoryInput = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemoryInput.Open();
            CaveMemoryInput.Alloc(0x800);
            _P1_Buttons_CaveAddress = CaveMemoryInput.CaveAddress;
            _P2_Buttons_CaveAddress = CaveMemoryInput.CaveAddress + 0x20;
            Logger.WriteLog("Custom data will be stored at : 0x" + _P1_Buttons_CaveAddress.ToString("X8"));

            //For P1 :
            //modify [edx+0x21] so that it corresponds to our values
            //EDX + 0x21 ==> 0x10 (START)
            //EDX + 0x22 ==>
            //EDX + 0x23 ==> 0xFF (TRIGGER)
            //EDX + 0x24 ==> 0xFF (RELOAD)
            //EDX + 0x25 ==> 0xFF (CHANGE WEAPON)
            //EDX + 0x26 ==> 0xFF (PEDAL) //Not doing anything
            //EDX + 0x29 ==> 0xFF (Bullet Time)
            //[ESP + 0x18] (after our push) contains controller ID (0->4)
            //We won't change any other input than Trigger, reload and Change
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess_MemoryBaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            //mov edx, [esp+04]
            CaveMemory.Write_StrBytes("8B 54 24 04");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //cmp [esp+0x18], 0
            CaveMemory.Write_StrBytes("83 7C 24 18 00");
            //je Player1
            CaveMemory.Write_StrBytes("0F 84 10 00 00 00");
            //cmp [esp + 0x18], 1
            CaveMemory.Write_StrBytes("83 7C 24 18 01");
            //je Player2
            CaveMemory.Write_StrBytes("0F 84 1B 00 00 00");
            //jmp exit
            CaveMemory.Write_StrBytes("E9 33 00 00 00");
            //Player1 :
            //mov eax, [_P1_Buttons_CaveAddress]
            byte[] b = BitConverter.GetBytes(_P1_Buttons_CaveAddress);
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(b);
            //mov ebx, [_P1_Buttons_CaveAddress + 4]
            b = BitConverter.GetBytes(_P1_Buttons_CaveAddress + 4);
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(b);
            //mov ecx, [_P1_Buttons_CaveAddress + 8]
            b = BitConverter.GetBytes(_P1_Buttons_CaveAddress + 8);
            CaveMemory.Write_StrBytes("8B 0D");
            CaveMemory.Write_Bytes(b);
            //jmp originalcode
            CaveMemory.Write_StrBytes("E9 11 00 00 00");
            //Player2:
            //mov eax, [_P2_Buttons_CaveAddress]
            b = BitConverter.GetBytes(_P2_Buttons_CaveAddress);
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(b);
            //mov ebx, [_P2_Buttons_CaveAddress]
            b = BitConverter.GetBytes(_P2_Buttons_CaveAddress + 4);
            CaveMemory.Write_StrBytes("8B 1D");
            CaveMemory.Write_Bytes(b);
            //mov ecx, [_P2_Buttons_CaveAddress + 8]
            b = BitConverter.GetBytes(_P2_Buttons_CaveAddress + 8);
            CaveMemory.Write_StrBytes("8B 0D");
            CaveMemory.Write_Bytes(b);
            //originalcode:
            //mov [edx+0x23], ax
            CaveMemory.Write_StrBytes("66 89 42 23");
            //mov [edx+0x24], bx
            CaveMemory.Write_StrBytes("66 89 5A 24");
            //mov [edx+0x29], cx
            CaveMemory.Write_StrBytes("66 89 4A 29");
            //exit:
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //mov cx, [edx+0x21]
            CaveMemory.Write_StrBytes("66 8B 4A 21");
            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Return_Offset);

            Logger.WriteLog("Adding Trigger CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Offset) - 5;
            List<byte> Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess_MemoryBaseAddress + _Buttons_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

            //Noping Axis procedures
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_X_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_X_2);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Y_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Y_2);
        }        

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes(PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes(PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_P1_Buttons_CaveAddress, 0xFF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask(_P1_Buttons_CaveAddress + 8, 0xFF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress + 8, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_P1_Buttons_CaveAddress + 4, 0xFF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_P1_Buttons_CaveAddress + 4, 0x00);
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_OFfset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress, 0xFF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress + 8, 0xFF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress + 8, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_P2_Buttons_CaveAddress + 4, 0xFF);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_P2_Buttons_CaveAddress + 4, 0x00);
            }
        }     

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun recoil : is handled by the game like it should (On/Off with every bullets)
            //Gun motor  : is activated when player gets hit
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P1_CtmLmpStart, OutputId.P1_CtmLmpStart, 500));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P2_CtmLmpStart, OutputId.P2_CtmLmpStart, 500));            
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Clip, OutputId.P2_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {            
            //Customs Outputs
            //Player Status :
            //[0] : Inactive
            //[2] : In-Game
            //[8] : Continue Screen
            //[16] : Game Over
            //[128] : Attract Demo
            int P1_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x0036D468); 
            int P2_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x0036D514);
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            
            if (P1_Status == 2)
            {
                //Force Start Lamp to Off
                SetOutputValue(OutputId.P1_CtmLmpStart, 0);

                _P1_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x0036D474);
                _P1_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00357942); 
            
                //Custom Recoil
                if (_P1_Ammo < _P1_LastAmmo)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Clip Empty] custom Output
                if (_P1_Ammo > 0)
                    P1_Clip = 1;

                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }
            else
            {
                //Enable Start Lamp Blinking
                SetOutputValue(OutputId.P1_CtmLmpStart, -1);
            }

           if (P2_Status == 2)
            {
                //Force Start Lamp to Off
                SetOutputValue(OutputId.P2_CtmLmpStart, 0);

                _P2_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x0036D520);
                _P2_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00357A1A); 

                //Custom Recoil
                if (_P2_Ammo < _P2_LastAmmo)
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                //[Clip Empty] custom Output
                if (_P2_Ammo > 0)
                    P2_Clip = 1;

                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);
            }
           else
           {
               //Enable Start Lamp Blinking
               SetOutputValue(OutputId.P2_CtmLmpStart, -1);
           }

            _P1_LastAmmo = _P1_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastAmmo = _P2_Ammo;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
        }

        #endregion
    }
}
