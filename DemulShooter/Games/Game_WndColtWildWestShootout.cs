using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Media;
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
    class Game_WndColtWildWestShootout : Game
    {
        /*** MEMORY ADDRESSES **/
        private UInt32 _P1_X_Offset = 0x00022030;
        private UInt32 _P1_Y_Offset = 0x00022034;
        private UInt32 _P1_Buttons_Offset = 0x00093B68;
        private NopStruct _Nop_X = new NopStruct(0x00009AB4, 6);
        private NopStruct _Nop_Y = new NopStruct(0x00009ABA, 6);
        private UInt32 _MouseControlMessage_Patch_Offset = 0x0000C480;
        InjectionStruct _NoCrosshair_InjectionStruct = new InjectionStruct(0x000071D9, 5);
        private UInt32 _NoGun_InstructionOffset = 0x000079F0;

        //Outputs
        private UInt32 _P1_IsDead_Offset = 0x000924BC;
        private UInt32 _P1_Life_Offset = 0x000924AC;
        private UInt32 _P1_Weapon_Offset = 0x000924D8;
        private UInt32 _P1_Ammo_Offset = 0x000924E0;
        private UInt32 _P1_CrosshairId_Offset = 0x0006AF88;

        private int _P1_LastWeapon = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndColtWildWestShootout(String RomName, bool HideCrosshairs, bool DisableInputHack, bool Verbose)
            : base(RomName, "Wild West Shootout", DisableInputHack, Verbose)
        {
            _HideCrosshair = HideCrosshairs;
            _KnownMd5Prints.Add("Colt's Wild West Shootout", "97c9e516a287aab33a455a396dadaa45");

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

                    //X => [0 ; 319] => 320
                    //Y => [0; 239] => 240
                    double dMaxX = 320.0;
                    double dMaxY = 240.0;

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
        /// Simple Hack : NOPing Axis procedures
        /// </summary>
        protected override void Apply_InputsMemoryHack()
        {
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_X);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Y);

            //Jump over the switch case handling MOUSE BUTTONS event
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _MouseControlMessage_Patch_Offset, new byte[] { 0x31, 0xC0, 0x90, 0x90, 0x90, 0x90 });

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        protected override void Apply_NoCrosshairMemoryHack()
        {
            Apply_CrosshairMod();
            Apply_GunMod();
        }

        /// <summary>
        /// Crosshair ID is used to change sprite:
        /// 1 = Star
        /// 2 = Exit
        /// 3 = Reticle
        /// Changing it to 0 displays nothing, instead of the "3"
        /// </summary>
        private void Apply_CrosshairMod()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov eax,["Wild West Shootout.exe" + _P1_CrosshairId_Offset]
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_CrosshairId_Offset));
            //cmp eax,03
            CaveMemory.Write_StrBytes("83 F8 03");
            //jne exit
            CaveMemory.Write_StrBytes("75 02");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");

            //Inject it
            CaveMemory.InjectToOffset(_NoCrosshair_InjectionStruct, "No Crosshair");
        }

        private void Apply_GunMod()
        {
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _NoGun_InstructionOffset, new byte[] { 0xE9, 0x8E, 0x00, 0x00, 0x00 });
        }

        #endregion

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>   
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFE);

                /*if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    Send_VK_KeyDown(_P1_Reload_VK);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    Send_VK_KeyUp(_P1_Reload_VK);*/

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Buttons_Offset, 0xFD);
            }
        }

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P1_CtmLmpStart, OutputId.P1_CtmLmpStart, 500));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));            
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            int P1_Clip = 0;
            _P1_Life = 0;
            _P1_Ammo = 0;
            int P1_CurrentWeapon = 0;         
            
            if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_IsDead_Offset) == 0)
            {
                _P1_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Life_Offset);

                //Force Start Lamp to Off
                SetOutputValue(OutputId.P1_CtmLmpStart, 0);

                _P1_Ammo = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Ammo_Offset);

                //Different weapons can change max ammo to have a lesser ammo count and cause recoil
                P1_CurrentWeapon = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Weapon_Offset);
                if (P1_CurrentWeapon == _P1_LastWeapon)
                {
                    //Custom Recoil
                    if (_P1_Ammo < _P1_LastAmmo)
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (_P1_Ammo > 0)
                        P1_Clip = 1;
                }

                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }
            else
            {
                //Enable Start Lamp Blinking
                SetOutputValue(OutputId.P1_CtmLmpStart, -1);
            }

            _P1_LastAmmo = _P1_Ammo;
            _P1_LastLife = _P1_Life;
            _P1_LastWeapon = P1_CurrentWeapon;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
        }

        #endregion
    }
}
