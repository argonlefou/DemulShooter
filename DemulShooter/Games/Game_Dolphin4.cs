using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_Dolphin4 : Game
    {
        /*** MEMORY ADDRESSES **/
        private const UInt32 CONTROLS_PTR_OFFSET = 0x00F37C48;
        private const UInt32 KEYBMOUSE_X_OFFSET = 0x144;
        private const UInt32 KEYBMOUSE_Y_OFFSET = 0x148;
        private const UInt32 KEYBMOUSE_LBTN_OFFSET = 0x13C;
        private const UInt32 KEYBMOUSE_MBTN_OFFSET = 0x13E;
        private const UInt32 KEYBMOUSE_RBTN_OFFSET = 0x13D;        
        private const UInt32 KEYBMOUSE_X_INJECTION_OFFSET = 0x004E64D9;
        private const UInt32 KEYBMOUSE_X_INJECTION_RETURN_OFFSET = 0x004E64DF;
        private const UInt32 KEYBMOUSE_Y_INJECTION_OFFSET = 0x004E64F5;
        private const UInt32 KEYBMOUSE_Y_INJECTION_RETURN_OFFSET = 0x004E64FA;
        private NopStruct _Nop_KeybMouse_Btn = new NopStruct(0x004E6602, 6);
        private const UInt32 ATRAK_X_OFFSET = 0x2c;
        private const UInt32 ATRAK_Y_OFFSET = 0x30;
        protected NopStruct _Nop_Atrak_Axis = new NopStruct(0x004E418A, 4);

        private const HardwareScanCode DIK_KEY_LCLICK = HardwareScanCode.DIK_S;
        private const HardwareScanCode DIK_KEY_MCLICK = HardwareScanCode.DIK_D;
        private const HardwareScanCode DIK_KEY_RCLICK = HardwareScanCode.DIK_F;

        /*** Process variables **/
        protected UInt32 _DinputNumber = 0;
        protected UInt32 _DinputControls_BaseAddress = 0;
        protected UInt32 _BasePtr = 0;
        protected UInt32 _KeybMouse_BaseAddress = 0;
        protected UInt32 _ATRAK_BaseAddress = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Dolphin4(String RomName, UInt32 DinputNumber, double _ForcedXratio, bool DisableInputHack, bool Verbose)
            : base(RomName, "Dolphin", _ForcedXratio, DisableInputHack, Verbose)
        {
            _DinputNumber = DinputNumber;
            _KnownMd5Prints.Add("","");

            _tProcess.Start();
            Logger.WriteLog("Waiting for Dolphin " + _RomName + " game to hook.....");
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
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            System.Threading.Thread.Sleep(2000);
                            SetHack();
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
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //Direct input mouse : float from -1 to +1
                    if (PlayerData.ID == 1)
                    {
                        //Convert client coordonnate to [-1, 1-] coordonates
                        double dX = PlayerData.RIController.Computed_X / TotalResX * 2.0 - 1.0;
                        double dY = PlayerData.RIController.Computed_Y / TotalResY * 2.0 - 1.0;
                        if (dX < -1)
                            dX = -1;
                        else if (dX > 1)
                            dX = 1;
                        if (dY < -1)
                            dY = -1;
                        else if (dY > 1)
                            dY = 1;

                        PlayerData.RIController.Computed_X =(int)(dX * 1000);
                        PlayerData.RIController.Computed_Y = (int)(dY * 1000);
                    }
                    //Dinput ATRAK : 
                    //min = FFFFFF80 max 0000080 , change from FFFFFF to 000000 at zero
                    //min = top left
                    else if (PlayerData.ID == 2)
                    {
                        double dMax = 254.0;

                        if (PlayerData.RIController.Computed_X < 0)
                            PlayerData.RIController.Computed_X = 0xFF80;
                        else if (PlayerData.RIController.Computed_X > (int)TotalResX)
                            PlayerData.RIController.Computed_X = 0x0080;
                        else
                            PlayerData.RIController.Computed_X = Convert.ToInt32(Math.Round(dMax * PlayerData.RIController.Computed_X / TotalResX) - 0x7F);

                        if (PlayerData.RIController.Computed_Y < 0)
                            PlayerData.RIController.Computed_Y = 0xFF80;
                        else if (PlayerData.RIController.Computed_Y > (int)TotalResY)
                            PlayerData.RIController.Computed_Y = 0x0080;
                        else
                            PlayerData.RIController.Computed_Y = Convert.ToInt32(Math.Round(dMax * PlayerData.RIController.Computed_Y / TotalResY) - 0x7F);
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
            //Calulation of base addresses for Dinput Keyboard/Mouse
            byte[] bTampon = ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + CONTROLS_PTR_OFFSET, 8);
            _BasePtr = BitConverter.ToUInt32(bTampon, 0);
            Logger.WriteLog("ControlsPtr address = 0x" + _BasePtr.ToString("X8"));
            try
            {
                bTampon = ReadBytes(_BasePtr, 8);
                _KeybMouse_BaseAddress = BitConverter.ToUInt32(bTampon, 0);
            }
            catch { }
            Logger.WriteLog("DInput Keyboard/Mouse address = 0x" + _KeybMouse_BaseAddress.ToString("X8"));
            //ATRAK #2 -> 2e manette en Ptr+10 (1ere en Ptr + 4) si 2 aimtrak connectés !! 
            Logger.WriteLog("DInput Player2 device number in the list : " + _DinputNumber + 1);     
            try
            {
                bTampon = ReadBytes(_BasePtr + (0x4 * _DinputNumber), 8);
                _ATRAK_BaseAddress = BitConverter.ToUInt32(bTampon, 0);
            }
            catch { }
            Logger.WriteLog("DInput Device#2 address = 0x" + _ATRAK_BaseAddress.ToString("X8"));
            
            //Nops
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_KeybMouse_Btn);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Atrak_Axis);

            //CodeCave Axe X
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, @
            Buffer.Add(0xB8);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress + 18));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //fstp dword ptr [eax]
            CaveMemory.Write_StrBytes("D9 18");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //fild dword ptr [esp+08]
            CaveMemory.Write_StrBytes("DB 44 24 08");
            //jmp Exit
            CaveMemory.Write_jmp((UInt32)_TargetProcess_MemoryBaseAddress + KEYBMOUSE_X_INJECTION_RETURN_OFFSET);

            //Code Injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess_MemoryBaseAddress + KEYBMOUSE_X_INJECTION_OFFSET) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess_MemoryBaseAddress + KEYBMOUSE_X_INJECTION_OFFSET, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

            Logger.WriteLog("Adding CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            Buffer = new List<Byte>();
            //push edx
            CaveMemory.Write_StrBytes("52");
            //mov edx, @
            Buffer.Add(0xBA);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress + 17));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //fstp dword ptr [edx]
            CaveMemory.Write_StrBytes("D9 1A");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //add esp,1C { 28 }
            CaveMemory.Write_StrBytes("83 C4 1C");
            //jmp Exit
            CaveMemory.Write_jmp((UInt32)_TargetProcess_MemoryBaseAddress + KEYBMOUSE_Y_INJECTION_RETURN_OFFSET);

            //Code injection
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess_MemoryBaseAddress + KEYBMOUSE_Y_INJECTION_OFFSET) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess_MemoryBaseAddress + KEYBMOUSE_Y_INJECTION_OFFSET, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

            Logger.WriteLog("Adding CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Center guns at start
            if (_KeybMouse_BaseAddress != 0)
            {
                WriteBytes(_KeybMouse_BaseAddress + KEYBMOUSE_X_OFFSET, new byte[] { 0, 0, 0, 0 });
                WriteBytes(_KeybMouse_BaseAddress + KEYBMOUSE_X_OFFSET, new byte[] { 0, 0, 0, 0 });
                WriteByte(_KeybMouse_BaseAddress + KEYBMOUSE_LBTN_OFFSET, 0x00);
                WriteByte(_KeybMouse_BaseAddress + KEYBMOUSE_RBTN_OFFSET, 0x00);
                WriteByte(_KeybMouse_BaseAddress + KEYBMOUSE_MBTN_OFFSET, 0x00);
            }
            if (_ATRAK_BaseAddress != 0)
            {
                WriteBytes(_ATRAK_BaseAddress + ATRAK_X_OFFSET, new byte[] { 0, 0, 0, 0 });
                WriteBytes(_ATRAK_BaseAddress + ATRAK_Y_OFFSET, new byte[] { 0, 0, 0, 0 });
            }

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }       

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>
        public override void SendInput(PlayerSettings PlayerData)
        {
            if (PlayerData.ID == 1 && _KeybMouse_BaseAddress != 0)
            {
                float fX = (float)PlayerData.RIController.Computed_X / (float)1000;
                float fY = (float)PlayerData.RIController.Computed_Y / (float)1000;
                byte[] bufferX = BitConverter.GetBytes(fX);
                byte[] bufferY = BitConverter.GetBytes(fY);

                WriteBytes(_KeybMouse_BaseAddress + KEYBMOUSE_X_OFFSET, bufferX);
                WriteBytes(_KeybMouse_BaseAddress + KEYBMOUSE_Y_OFFSET, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0) 
                    WriteByte(_KeybMouse_BaseAddress + KEYBMOUSE_LBTN_OFFSET, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0) 
                    WriteByte(_KeybMouse_BaseAddress + KEYBMOUSE_LBTN_OFFSET, 0x00);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0) 
                    WriteByte(_KeybMouse_BaseAddress + KEYBMOUSE_MBTN_OFFSET, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0) 
                    WriteByte(_KeybMouse_BaseAddress + KEYBMOUSE_MBTN_OFFSET, 0x00);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0) 
                    WriteByte(_KeybMouse_BaseAddress + KEYBMOUSE_RBTN_OFFSET, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0) 
                    WriteByte(_KeybMouse_BaseAddress + KEYBMOUSE_RBTN_OFFSET, 0x00);
            }
            else if (PlayerData.ID == 2 && _ATRAK_BaseAddress != 00)
            {
                //Converting 0xFF80 to 0xFFFFFF80 and so on...
                byte[] bufferX = { (byte)(PlayerData.RIController.Computed_X & 0xFF), (byte)(PlayerData.RIController.Computed_X >> 8), (byte)(PlayerData.RIController.Computed_X >> 8), (byte)(PlayerData.RIController.Computed_X >> 8) };
                byte[] bufferY = { (byte)(PlayerData.RIController.Computed_Y & 0xFF), (byte)(PlayerData.RIController.Computed_Y >> 8), (byte)(PlayerData.RIController.Computed_Y >> 8), (byte)(PlayerData.RIController.Computed_Y >> 8) };

                WriteBytes(_ATRAK_BaseAddress + ATRAK_X_OFFSET, bufferX);
                WriteBytes(_ATRAK_BaseAddress + ATRAK_Y_OFFSET, bufferY);

                //Inputs
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0) 
                    SendKeyDown(DIK_KEY_LCLICK);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0) 
                    SendKeyUp(DIK_KEY_LCLICK);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0) 
                    SendKeyDown(DIK_KEY_MCLICK);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0) 
                    SendKeyUp(DIK_KEY_MCLICK);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0) 
                    SendKeyDown(DIK_KEY_RCLICK);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0) 
                    SendKeyUp(DIK_KEY_RCLICK);
            }
        }

        #endregion        
    }
}
