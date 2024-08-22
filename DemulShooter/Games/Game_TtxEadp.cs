using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_TtxEadp : Game
    {
        /*** MEMORY ADDRESSES **/
        private UInt32 _P1_X_Offset = 0x00201BEC;
        private UInt32 _P1_Y_Offset = 0x00201BEE;
        private UInt32 _P1_Trigger_Offset = 0x00201BF0;
        private UInt32 _P1_Grenade_Offset = 0x00212235;
        private UInt32 _P1_Out_Offset = 0x00201BEA;
        private UInt32 _P2_X_Offset = 0x00201BF6;
        private UInt32 _P2_Y_Offset = 0x00201BF8;
        private UInt32 _P2_Trigger_Offset = 0x00201BFA;
        private UInt32 _P2_Grenade_Offset = 0x0021228D;
        private UInt32 _P2_Out_Offset = 0x00201BF4;

        private NopStruct _Nop_P1_X = new NopStruct(0x0014F583, 7);
        private NopStruct _Nop_P1_Y = new NopStruct(0x0014F5A8, 7);
        private NopStruct _Nop_P1_Trigger = new NopStruct(0x0014F653, 7);
        private NopStruct _Nop_P1_Out_1 = new NopStruct(0x00014F60C, 7);
        private NopStruct _Nop_P1_Out_2 = new NopStruct(0x00014F613, 7);
        private NopStruct _Nop_P2_X = new NopStruct(0x0014F5C5, 7);
        private NopStruct _Nop_P2_Y = new NopStruct(0x0014F5EA, 7);
        private NopStruct _Nop_P2_Trigger = new NopStruct(0x0014F66C, 7);
        private NopStruct _Nop_P2_Out_1 = new NopStruct(0x0014F633, 7);
        private NopStruct _Nop_P2_Out_2 = new NopStruct(0x0014F63A, 7);
        private NopStruct _Nop_Grenade_1 = new NopStruct(0x000C509A, 3);
        private NopStruct _Nop_Grenade_2 = new NopStruct(0x000C50AB, 3);

        //Outputs
        private UInt32 _CustomRecoil_Injection_Offset = 0x0003ED23;
        private UInt32 _CustomRecoil_InjectionReturn_Offset = 0x0003ED28;
        private UInt32 _CustomDamage_Injection_Offset = 0x0006E29A;
        private UInt32 _CustomDamage_InjectionReturn_Offset = 0x0006E29F;
        private UInt32 _P1_RecoilStatus_CaveAddress = 0;
        private UInt32 _P2_RecoilStatus_CaveAddress = 0;
        private UInt32 _P1_DamageStatus_CaveAddress = 0;
        private UInt32 _P2_DamageStatus_CaveAddress = 0;


        /// <summary>
        /// Constructor
        /// </summary>
        public Game_TtxEadp(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "game", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Elevator Action Dead Parade - Original", "59dbaf264bb96323cb3127dee0f809ca");
            _KnownMd5Prints.Add("Elevator Action Dead Parade - For JConfig", "1bc36915ac4a4658feeca80ca1b6ca10");
           _tProcess.Start();

            Logger.WriteLog("Waiting for Taito Type X " + _RomName + " game to hook.....");
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

                    //X => [0x0000 ; 0x4000] = 16384
                    //Y => [0x0000 ; 0x4000] = 16384
                    double dMinX = 0.0;
                    double dMaxX = 16384.0;
                    double dMinY = 0.0;
                    double dMaxY = 16384.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;
                    
                    PlayerData.RIController.Computed_X = Convert.ToInt32(Math.Round(dRangeX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt32(Math.Round(dRangeY * PlayerData.RIController.Computed_Y / TotalResY));
                    
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

        /// <summary>
        /// Genuine Hack, just blocking Axis and Triggers input to replace them.
        /// </summary>        
        protected override void Apply_InputsMemoryHack()
        {
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_X);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_Y);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_Trigger);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_Out_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_Out_2);

            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_X);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_Y);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_Trigger);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_Out_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P2_Out_2);

            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Grenade_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Grenade_2);

            //Set P1 & P2 IN_SCREEN
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Out_Offset, new byte[] { 0x00, 0x00 });
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Out_Offset, new byte[] { 0x00, 0x00 });

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _P1_RecoilStatus_CaveAddress = _OutputsDatabank_Address;
            _P2_RecoilStatus_CaveAddress = _OutputsDatabank_Address + 0x04;
            _P1_DamageStatus_CaveAddress = _OutputsDatabank_Address + 0x10;
            _P2_DamageStatus_CaveAddress = _OutputsDatabank_Address + 0x14;

            SetHack_Damage();
            SetHack_Recoil();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Code injection where the game is calling for rumble because of damage.
        /// That way we can known when a player is damaged and make our own output.
        /// </summary>
        private void SetHack_Damage()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //push edx
            CaveMemory.Write_StrBytes("52");
            //shl edx, 2
            CaveMemory.Write_StrBytes("C1 E2 02");
            //add edx, _P1_DamageStatus_CaveAddress
            byte[] b = BitConverter.GetBytes(_P1_DamageStatus_CaveAddress);
            CaveMemory.Write_StrBytes("81 C2");
            CaveMemory.Write_Bytes(b);
            //mov [edx], 1
            CaveMemory.Write_StrBytes("C7 02 01 00 00 00");
            //pop edx
            CaveMemory.Write_StrBytes("5A");
            //push 14
            CaveMemory.Write_StrBytes("6A 14");
            //push 01
            CaveMemory.Write_StrBytes("6A 01");
            //push edx
            CaveMemory.Write_StrBytes("52");
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _CustomDamage_InjectionReturn_Offset);

            Logger.WriteLog("Adding Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _CustomDamage_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _CustomDamage_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);       
        }

        /// <summary>
        /// Code injection where the game is calling for rumble because of Recoil
        /// That way we can knwo when a bullet is fired and create our own output
        /// </summary>
        private void SetHack_Recoil()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //shl edx, 2
            CaveMemory.Write_StrBytes("C1 E2 02");
            //add edx, _P1_RecoilStatus_CaveAddress
            byte[] b = BitConverter.GetBytes(_P1_RecoilStatus_CaveAddress);
            CaveMemory.Write_StrBytes("81 C2");
            CaveMemory.Write_Bytes(b);
            //mov [edx], 1
            CaveMemory.Write_StrBytes("C7 02 01 00 00 00");
            CaveMemory.Write_call((UInt32)_TargetProcess_MemoryBaseAddress + 0xC56E0);
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _CustomRecoil_InjectionReturn_Offset);

            Logger.WriteLog("Adding Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _CustomRecoil_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _CustomRecoil_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);       
        
        }
        #endregion
    
        #region Inputs

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

                //Inputs
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0) 
                {
                    //Set out of screen Byte 
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Out_Offset, new byte[] { 0x01, 0x01 });
                    //Trigger a shoot to reload !!
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0) 
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, 0x00);
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Out_Offset, new byte[] { 0x00, 0x00 });
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0) 
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Grenade_Offset, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0) 
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Grenade_Offset, 0x00);                
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);

                //Inputs
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    //Set out of screen Byte 
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Out_Offset, new byte[] { 0x01, 0x01 });
                    //Trigger a shoot to reload !!
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, 0x00);
                    WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Out_Offset, new byte[] { 0x00, 0x00 });
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Grenade_Offset, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Grenade_Offset, 0x00);
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //GunMotor : Activated on every bullet + On damage
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.LmpSide_R, OutputId.LmpSide_R));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpSide_G, OutputId.LmpSide_G));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpSide_B, OutputId.LmpSide_B));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpUpBtn, OutputId.LmpUpBtn));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpDownBtn, OutputId.LmpDownBtn));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpCloseBtn, OutputId.LmpCloseBtn));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            byte Outputs1 = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00204EBC);
            byte Outputs2 = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00204EBE);
            byte Outputs3 = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00204EBB);

            //Original Outputs
            SetOutputValue(OutputId.LmpSide_R, Outputs2 >> 6 & 0x01);
            SetOutputValue(OutputId.LmpSide_G, Outputs2 >> 5 & 0x01);
            SetOutputValue(OutputId.LmpSide_B, Outputs2 >> 4 & 0x01);
            SetOutputValue(OutputId.LmpUpBtn, Outputs1 >> 6 & 0x01);
            SetOutputValue(OutputId.LmpDownBtn, Outputs1 >> 5 & 0x01);
            SetOutputValue(OutputId.LmpCloseBtn, Outputs1 >> 4 & 0x01);
            SetOutputValue(OutputId.P1_GunMotor, Outputs3 >> 4 & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, Outputs3 >> 3 & 0x01);

            //Custom Outputs
            if (ReadByte(_P1_DamageStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P1_Damaged, 1);
                WriteByte(_P1_DamageStatus_CaveAddress, 0x00);
            }
            if (ReadByte(_P2_DamageStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P2_Damaged, 1);
                WriteByte(_P2_DamageStatus_CaveAddress, 0x00);
            }

            if (ReadByte(_P1_RecoilStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                WriteByte(_P1_RecoilStatus_CaveAddress, 0x00);
            }
            if (ReadByte(_P2_RecoilStatus_CaveAddress) == 1)
            {
                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                WriteByte(_P2_RecoilStatus_CaveAddress, 0x00);
            }

            //Ammo + Life
            UInt32 PlayersStatusPtr_Offset = 0x00212CD4;
            UInt32 iTemp = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + PlayersStatusPtr_Offset);
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;

            if (iTemp != 0)
            {
                //Status Byte, 0x01 if playing, else 0x00
                if (ReadByte(ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + 0x210A80) + 0x1794C) != 0)
                {
                    _P1_Ammo = ReadByte(ReadPtr(iTemp + 0x54) + 0x150);
                    _P1_Life = ReadByte(ReadPtr(iTemp + 0x4C) + 0x150);
                }

                if (ReadByte(ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + 0x210A80) + 0x17A38) != 0)
                {
                    _P2_Ammo = ReadByte(ReadPtr(iTemp + 0x58) + 0x150);
                    _P2_Life = ReadByte(ReadPtr(iTemp + 0x50) + 0x150);
                }
            }

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P2_Life, _P2_Life); 
        }

        #endregion
    }
}
