using System;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;

namespace DemulShooter
{
    class Game_Dolphin5 : Game
    {        
        /*** MEMORY ADDRESSES **/
        private UInt32 Controls_Ptr_Offsett = 0x00E8BC80;
        private UInt32 KeybMouse_X_Offset = 0x168;
        private UInt32 KeybMouse_Y_Offset = 0x170;
        private UInt32 KeybMouse_LBtn_Offset = 0x15C;
        private UInt32 KeybMouse_MBtn_Offset = 0x15E;
        private UInt32 KeybMouse_RBtn_Offset = 0x15D;
        private NopStruct _Nop_KeybMouse_X_1 = new NopStruct(0x004E7B2B, 3);
        private NopStruct _Nop_KeybMouse_X_2 = new NopStruct(0x004E7AE6, 4);
        private NopStruct _Nop_KeybMouse_Y_1 = new NopStruct(0x004E7B2E, 3);
        private NopStruct _Nop_KeybMouse_Y_2 = new NopStruct(0x004E7B08, 4);
        private NopStruct _Nop_KeybMouse_Btn = new NopStruct(0x004E765D, 7);
        private const UInt32 ATRAK_X_Offset = 0x44;
        private const UInt32 ATRAK_Y_Offset = 0x48;
        private NopStruct _Nop_Atrak_Axis = new NopStruct(0x004E587C, 4);

        /*** Process variables **/
        private UInt32 _DinputNumber = 0;
        private UInt32 _BasePtr = 0;
        private UInt32 _KeybMouse_BaseAddress = 0;
        private UInt32 _ATRAK_BaseAddress = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Dolphin5(String RomName, UInt32 DinputNumber, bool DisableInputHack, bool Verbose)
            : base(RomName, "Dolphin", DisableInputHack, Verbose)
        {
            _DinputNumber = DinputNumber;
            _KnownMd5Prints.Add("Dolphin_x86 v5.0", "9660ec7cddf093a1807cb25fe0946b8e");
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
                        //_TargetProcess_MemoryBaseAddress = _TargetProcess.MainModule.BaseAddress; 
                        _TargetProcess_MemoryBaseAddress = (IntPtr)0x400000;

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            _GameWindowHandle = _TargetProcess.MainWindowHandle;
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            System.Threading.Thread.Sleep(2000);
                            Apply_MemoryHacks();
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

                    //Direct input mouse : double from -1 to +1
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

                        PlayerData.RIController.Computed_X = (int)(dX * 1000);
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

        protected override void Apply_InputsMemoryHack()
        {
            //Calulation of base addresses for Dinput Keyboard/Mouse
            byte[] bTampon = ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + Controls_Ptr_Offsett, 8);
            _BasePtr = BitConverter.ToUInt32(bTampon, 0);
            Logger.WriteLog("ControlsPtr address = 0x" + _BasePtr.ToString("X8"));
            try
            {
                bTampon = ReadBytes(_BasePtr, 8);
                _KeybMouse_BaseAddress = BitConverter.ToUInt32(bTampon, 0);
            }
            catch { }
            Logger.WriteLog("DInput Keyboard/Mouse address = 0x" + _KeybMouse_BaseAddress.ToString("X8"));
            //ATRAK #2 -> 2e manette en Ptr+10 (1ere en Ptr + 8) si 2 aimtrak connectés !!  
            Logger.WriteLog("DInput Player2 device number in the list : " + _DinputNumber + 1);     
            try
            {
                bTampon = ReadBytes(_BasePtr + (0x8 * _DinputNumber), 8);
                _ATRAK_BaseAddress = BitConverter.ToUInt32(bTampon, 0);
            }
            catch { }
            Logger.WriteLog("DInput Device#2 address = 0x" + _ATRAK_BaseAddress.ToString("X8"));            

            //Nops
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_KeybMouse_X_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_KeybMouse_X_2);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_KeybMouse_Y_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_KeybMouse_Y_2);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_KeybMouse_Btn);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Atrak_Axis);            

            //Center guns data at start
            if (_KeybMouse_BaseAddress != 0)
            {
                WriteBytes(_KeybMouse_BaseAddress + KeybMouse_X_Offset, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
                WriteBytes(_KeybMouse_BaseAddress + KeybMouse_Y_Offset, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 });
                WriteByte(_KeybMouse_BaseAddress + KeybMouse_LBtn_Offset, 0x00);
                WriteByte(_KeybMouse_BaseAddress + KeybMouse_RBtn_Offset, 0x00);
                WriteByte(_KeybMouse_BaseAddress + KeybMouse_MBtn_Offset, 0x00);
            }
            if (_ATRAK_BaseAddress != 0)
            {
                WriteBytes(_ATRAK_BaseAddress + ATRAK_X_Offset, new byte[] { 0, 0, 0, 0 });
                WriteBytes(_ATRAK_BaseAddress + ATRAK_Y_Offset, new byte[] { 0, 0, 0, 0 });
            }
            Logger.WriteLog("Inputs Memory Hack complete !");
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
                double fX = (double)PlayerData.RIController.Computed_X / (double)1000;
                double fY = (double)PlayerData.RIController.Computed_Y / (double)1000;
                byte[] bufferX = BitConverter.GetBytes(fX);
                byte[] bufferY = BitConverter.GetBytes(fY);

                WriteBytes(_KeybMouse_BaseAddress + KeybMouse_X_Offset, bufferX);
                WriteBytes(_KeybMouse_BaseAddress + KeybMouse_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0) 
                    WriteByte(_KeybMouse_BaseAddress + KeybMouse_LBtn_Offset, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0) 
                    WriteByte(_KeybMouse_BaseAddress + KeybMouse_LBtn_Offset, 0x00);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0) 
                    WriteByte(_KeybMouse_BaseAddress + KeybMouse_MBtn_Offset, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0) 
                    WriteByte(_KeybMouse_BaseAddress + KeybMouse_MBtn_Offset, 0x00);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0) 
                    WriteByte(_KeybMouse_BaseAddress + KeybMouse_RBtn_Offset, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0) 
                    WriteByte(_KeybMouse_BaseAddress + KeybMouse_RBtn_Offset, 0x00);
            }
            else if (PlayerData.ID == 2 && _ATRAK_BaseAddress != 00)
            {
                //Converting 0xFF80 to 0xFFFFFF80 and so on...
                byte[] bufferX = { (byte)(PlayerData.RIController.Computed_X & 0xFF), (byte)(PlayerData.RIController.Computed_X >> 8), (byte)(PlayerData.RIController.Computed_X >> 8), (byte)(PlayerData.RIController.Computed_X >> 8) };
                byte[] bufferY = { (byte)(PlayerData.RIController.Computed_Y & 0xFF), (byte)(PlayerData.RIController.Computed_Y >> 8), (byte)(PlayerData.RIController.Computed_Y >> 8), (byte)(PlayerData.RIController.Computed_Y >> 8) };

                WriteBytes(_ATRAK_BaseAddress + ATRAK_X_Offset, bufferX);
                WriteBytes(_ATRAK_BaseAddress + ATRAK_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0) 
                    SendKeyDown(Configurator.GetInstance().DIK_Dolphin_P2_LClick);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    SendKeyUp(Configurator.GetInstance().DIK_Dolphin_P2_LClick);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    SendKeyDown(Configurator.GetInstance().DIK_Dolphin_P2_MClick);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    SendKeyUp(Configurator.GetInstance().DIK_Dolphin_P2_MClick);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    SendKeyDown(Configurator.GetInstance().DIK_Dolphin_P2_RClick);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    SendKeyUp(Configurator.GetInstance().DIK_Dolphin_P2_RClick);
            }
        }

        #endregion

    }
}
