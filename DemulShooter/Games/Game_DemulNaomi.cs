using System;
using System.Collections.Generic;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_DemulNaomi : Game_Demul
    {
        public Game_DemulNaomi(String RomName, String DemulVersion, bool DisableInputHack, bool Verbose, bool DisableWindow, bool WidescreenHack)
            : base(RomName, "naomi", DemulVersion, DisableInputHack, Verbose, DisableWindow, WidescreenHack)
        {}

        #region Memory Hack

        protected override void SetHack_057()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //cmp ecx, 2
            CaveMemory.Write_StrBytes("83 FF 02");
            //je @
            CaveMemory.Write_StrBytes("0F 84 35 00 00 00");
            //cmp ecx, 3
            CaveMemory.Write_StrBytes("83 FF 03");
            //je @
            CaveMemory.Write_StrBytes("0F 84 2C 00 00 00");
            //cmp ecx, 42
            CaveMemory.Write_StrBytes("83 FF 42");
            //je @
            CaveMemory.Write_StrBytes("0F 84 23 00 00 00");
            //cmp ecx, 43
            CaveMemory.Write_StrBytes("83 FF 43");
            //je @
            CaveMemory.Write_StrBytes("0F 84 1A 00 00 00");
            //cmp ecx, 1
            CaveMemory.Write_StrBytes("83 FF 01");
            //je @
            CaveMemory.Write_StrBytes("0F 84 16 00 00 00");
            //cmp ecx, 41
            CaveMemory.Write_StrBytes("83 FF 41");
            //je @
            CaveMemory.Write_StrBytes("0F 84 0D 00 00 00");
            //mov [edi*2+padDemul.dll+OFFSET],cx
            CaveMemory.Write_StrBytes("66 89 0C 7D");
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //jmp @
            CaveMemory.Write_jmp((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Return_Offset);
            //cmp ax,80
            CaveMemory.Write_StrBytes("81 F9 80 00 00 00");
            //jnl @
            CaveMemory.Write_StrBytes("0F 8D 0D 00 00 00");
            //and dword ptr [edi*2+padDemul.dll+OFFSET],7F
            CaveMemory.Write_StrBytes("81 24 7D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("7F FF FF FF");
            //jmp @
            CaveMemory.Write_StrBytes("EB E2");
            //or [edi*2+padDemul.dll+OFFSET],00000080
            CaveMemory.Write_StrBytes("81 0C 7D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("80 00 00 00");
            //jmp @
            CaveMemory.Write_StrBytes("EB D5");

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

            //Center Guns position at start
            byte[] init = { 0, 0x7f, 0, 0x7f };
            WriteBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, init);
            WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x00);
            WriteBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, init);
            WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x00);

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }
         
        protected override void SetHack_07()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //cmp ecx, 2
            CaveMemory.Write_StrBytes("83 F9 02");
            //je @
            CaveMemory.Write_StrBytes("0F 84 35 00 00 00");
            //cmp ecx, 3
            CaveMemory.Write_StrBytes("83 F9 03");
            //je @
            CaveMemory.Write_StrBytes("0F 84 2C 00 00 00");
            //cmp ecx, 42
            CaveMemory.Write_StrBytes("83 F9 42");
            //je @
            CaveMemory.Write_StrBytes("0F 84 23 00 00 00");
            //cmp ecx, 43
            CaveMemory.Write_StrBytes("83 F9 43");
            //je @
            CaveMemory.Write_StrBytes("0F 84 1A 00 00 00");
            //cmp ecx, 1
            CaveMemory.Write_StrBytes("83 F9 01");
            //je @
            CaveMemory.Write_StrBytes("0F 84 16 00 00 00");
            //cmp ecx, 41
            CaveMemory.Write_StrBytes("83 F9 41");
            //je @
            CaveMemory.Write_StrBytes("0F 84 0D 00 00 00");
            //mov [ecx*2+padDemul.dll+OFFSET],ax
            CaveMemory.Write_StrBytes("66 89 04 4D");
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //jmp @
            CaveMemory.Write_jmp((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Return_Offset);
            //cmp ax,80
            CaveMemory.Write_StrBytes("3D 80 00 00 00");
            //jnl @
            CaveMemory.Write_StrBytes("0F 8D 0D 00 00 00");
            //and dword ptr [ecx*2+padDemul.dll+OFFSET],7F
            CaveMemory.Write_StrBytes("81 24 4D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("7F FF FF FF");
            //jmp @
            CaveMemory.Write_StrBytes("EB E3");
            //or [ecx*2+padDemul.dll+35E50],00000080
            CaveMemory.Write_StrBytes("81 0c 4D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("80 00 00 00");
            //jmp @
            CaveMemory.Write_StrBytes("EB D6");

            Logger.WriteLog("Adding CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code Injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

            //Center Guns position at start
            byte[] init = { 0, 0x7f, 0, 0x7f };
            WriteBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, init);
            //WriteByte(P1_INOUT_ADDRESS, 0x01);
            WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x00);
            WriteBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, init);
            //WriteByte(P2_INOUT_ADDRESS, 0x01);
            WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x00);

            Logger.WriteLog("Naomi Memory Hack complete !");
            Logger.WriteLog("-");

            //Codecave for WidescreenHack
            /*if (_Rom.Equals("lupinsho") && _WidescreenHack)
            {
                CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
                CaveMemory.Open();
                CaveMemory.Alloc(0x800);
                //cmp ecx, 00BD6154
                CaveMemory.Write_StrBytes("81 F9 54 61 BD 00");
                //je @
                CaveMemory.Write_StrBytes("0F 84 06 00 00 00");
                //mov [ecx+2C000000],edx
                CaveMemory.Write_StrBytes("89 91 00 00 00 2C");
                //jmp @
                CaveMemory.Write_jmp((int)_TargetProcess.MainModule.BaseAddress + 0x0019CCDC);

                jumpTo = CaveMemory.CaveAddress - ((int)_TargetProcess.MainModule.BaseAddress + 0x0019CCD6) - 5;
                Buffer.Clear();
                Buffer.Add(0xE9);
                Buffer.AddRange(BitConverter.GetBytes(jumpTo));
                Buffer.Add(0x90);
                Win32.WriteProcessMemory((int)ProcessHandle, (int)_TargetProcess.MainModule.BaseAddress + 0x0019CCD6, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
                Logger.WriteLog("Added lupinsho special Widescreen codecave at 0x" + CaveMemory.CaveAddress.ToString("X8"));
                //WriteBytes(0x2CBD6154, BitConverter.GetBytes(0x3F800000));
            }*/
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>
        public override void  SendInput(PlayerSettings PlayerData)
        {
            //creating X-Y Hex value buffer to write memory
            byte[] buffer = 
            {   (byte)(PlayerData.RIController.Computed_X >> 8), 
                (byte)(PlayerData.RIController.Computed_X & 0xFF),
                (byte)(PlayerData.RIController.Computed_Y >> 8), 
                (byte)(PlayerData.RIController.Computed_Y & 0xFF) 
            };
            
            if (PlayerData.ID == 1)
            {
                WriteBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, buffer);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0) 
                   WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x02);                        
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0) 
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    //lupinsho reloads only when cursor get to max value ( = out of window) so we force it for Gun who don't update in realtime
                    if (_RomName.Equals("lupinsho"))
                    {
                        buffer[0] = (byte)(0);
                        buffer[1] = (byte)(0xFF);
                        buffer[2] = (byte)(0);
                        buffer[3] = (byte)(0xFF);
                        WriteBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, buffer);
                        WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x01);
                        System.Threading.Thread.Sleep(50);
                    }
                    else
                        WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x00);
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, buffer);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0) 
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0) 
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x00);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0) 
                {
                    //lupinsho reloads only when cursor get to max value ( = out of window) so we force it for Gun who don't update in realtime
                    if (_RomName.Equals("lupinsho"))
                    {
                        buffer[0] = (byte)(0);
                        buffer[1] = (byte)(0xFF);
                        buffer[2] = (byte)(0);
                        buffer[3] = (byte)(0xFF);
                        WriteBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, buffer);
                        WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x01);
                    }
                    else
                        WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0) 
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x00);
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();            
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
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
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            if (_RomName.Equals("confmiss"))
                Compute_Confmiss_Outputs();
            else if (_RomName.Equals("deathcox"))
                Compute_Deathcox_Outputs();         //Need better filter (Playing vs Not Playing to display ammo/life)
            else if (_RomName.Equals("hotd2"))
                Compute_Hotd2_Outputs(0x00096FA0);
            else if (_RomName.Equals("hotd2o"))
                Compute_Hotd2_Outputs(0x00096F58);
            else if (_RomName.Equals("hotd2p"))
                Compute_Hotd2_Outputs(0x00082D00);
            else if (_RomName.Equals("lupinsho"))   
                Compute_Lupinsho_Outputs();
            else if (_RomName.Equals("mok"))        //Todo : Check for Status !!
                Compute_Mok_Outputs();            
        }

        private void Compute_Confmiss_Outputs()
        {
            //Player status :
            //[0] = Calibration/InGame
            //[1] = InGame
            //[2] = Continue
            //[4] = Game Over / Attract Mode / Menu
            UInt32 P1_Status_Address = _GameRAM_Address + 0x00154CB8;
            UInt32 P2_Status_Address = P1_Status_Address + 0x40;
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            UInt32 P1_Ammo_Address = _GameRAM_Address + 0x00155804;  // = P1_Status_Address + 0xB4C ?
            UInt32 P2_Ammo_Address = _GameRAM_Address + 0x00155918;
            UInt32 Credits_Address = _GameRAM_Address + 0x001B551C;

            if (ReadByte(P1_Status_Address) == 0 || ReadByte(P1_Status_Address) == 1)
            {
                _P1_Life = ReadByte((P1_Status_Address + 0x14));
                _P1_Ammo = ReadByte(P1_Ammo_Address);

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

            if (ReadByte(P2_Status_Address) == 0 || ReadByte(P2_Status_Address) == 1)
            {
                _P2_Life = ReadByte((P2_Status_Address + 0x14));
                _P2_Ammo = ReadByte(P2_Ammo_Address);

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

            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_GameRAM_Address + 0x000152AEA) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_GameRAM_Address + 0x000152AEA) >> 4 & 0x01);

            SetOutputValue(OutputId.Credits, ReadByte(Credits_Address));
        }

        private void Compute_Deathcox_Outputs()
        {
            //InGame Status : 0 = AttractMode/Demo/Menu, 1 = InGame
            UInt32 InGame_Address = _GameRAM_Address + 0x00096680;
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            UInt32 P1_Ammo_Address = _GameRAM_Address + 0x0018BFC9;
            UInt32 P2_Ammo_Address = P1_Ammo_Address + 0x3C;
            UInt32 Credits_Address = _GameRAM_Address + 0x00974A8;
            //P1 and P2 Enable : Display ammo and life when it's 0 ( not reliable but well...)
            UInt32 P1_Enable_Address = _GameRAM_Address + 0x00096634;
            UInt32 P2_Enable_Address = _GameRAM_Address + 0x00096638;

            if (ReadByte(P1_Enable_Address) == 0 && ReadByte(InGame_Address) == 1)
            {
                _P1_Life = (int)(BitConverter.ToSingle(ReadBytes(Credits_Address + 0x04, 4), 0) * 100);
                _P1_Ammo = ReadByte(P1_Ammo_Address);

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

            if (ReadByte(P2_Enable_Address) == 0 && ReadByte(InGame_Address) == 1)
            {
                _P2_Life = (int)(BitConverter.ToSingle(ReadBytes(Credits_Address + 0x08, 4), 0) * 100);
                _P2_Ammo = ReadByte(P2_Ammo_Address);

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

            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_GameRAM_Address + 0x0001C8D16) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_GameRAM_Address + 0x0001C8D16) >> 4 & 0x01);
            SetOutputValue(OutputId.Credits, ReadByte(Credits_Address));
        }

        private void Compute_Hotd2_Outputs(UInt32 DataPtr)
        {
            //Player status :
            //[4] = Continue Screen
            //[5] = InGame
            //[6] = Game Over
            //[9] = Menu or Attract Mode             
            UInt32 P1_Status_Address = _GameRAM_Address + (BitConverter.ToUInt32(ReadBytes(_GameRAM_Address + DataPtr, 4), 0) & 0x01FFFFFF) + 0x04;
            UInt32 P2_Status_Address = P1_Status_Address + 0x100;

            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            if (ReadByte(P1_Status_Address) == 5)
            {
                _P1_Life = ReadByte(P1_Status_Address + 0x0C);
                _P1_Ammo = ReadByte(P1_Status_Address + 0x20);

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

            if (ReadByte(P2_Status_Address) == 5)
            {
                _P2_Life = ReadByte(P2_Status_Address + 0x0C);
                _P2_Ammo = ReadByte(P2_Status_Address + 0x20);

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

            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(P1_Status_Address + 0xFB) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(P2_Status_Address + 0xFB) >> 6 & 0x01);
            SetOutputValue(OutputId.Credits, ReadByte(P1_Status_Address + 0x75C));
        }

        private void Compute_Lupinsho_Outputs()
        {
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            //Game Status (float) :
            //0 : Title Screen
            //1 : Gameplay
            //2 : Demo play
            float GameStatus = BitConverter.ToSingle(ReadBytes(_GameRAM_Address + 0x00A4C758, 4), 0);

            if (GameStatus == 1.0f)
            {
                //Check if P1 and P2 are active to display their information
                float P1_Active = BitConverter.ToSingle(ReadBytes(_GameRAM_Address + 0x00A4C738, 4), 0);
                float P2_Active = BitConverter.ToSingle(ReadBytes(_GameRAM_Address + 0x00A4C73C, 4), 0);

                //if (true)//if (P1_Active == 1.0f)
                //{
                    _P1_Life = (int)(BitConverter.ToSingle(ReadBytes(_GameRAM_Address + 0x00A4C888, 4), 0));
                    _P1_Ammo = (int)(BitConverter.ToSingle(ReadBytes(_GameRAM_Address + 0x00A4C808, 4), 0));

                    //Custom Recoil
                    if (_P1_Ammo < _P1_LastAmmo)
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (_P1_Ammo > 0)
                        P1_Clip = 1;

                    //[Damaged] custom Output                
                    if (_P1_Life < _P1_LastLife)
                        SetOutputValue(OutputId.P1_Damaged, 1);
                //}

                //if(true)//if (P2_Active == 1.0f)
                //{
                    _P2_Life = (int)(BitConverter.ToSingle(ReadBytes(_GameRAM_Address + 0x00A4C88C, 4), 0));
                    _P2_Ammo = (int)(BitConverter.ToSingle(ReadBytes(_GameRAM_Address + 0x00A4C80C, 4), 0));

                    //Custom Recoil
                    if (_P2_Ammo < _P2_LastAmmo)
                        SetOutputValue(OutputId.P2_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (_P2_Ammo > 0)
                        P2_Clip = 1;

                    //[Damaged] custom Output                
                    if (_P2_Life < _P2_LastLife)
                        SetOutputValue(OutputId.P2_Damaged, 1);
                //}
            }

            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_GameRAM_Address + 0x00B77BF6) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_GameRAM_Address + 0x00B77BF6) >> 4 & 0x01);
            SetOutputValue(OutputId.Credits, ReadByte(_GameRAM_Address + 0x00B69FD8));
        }

        private void Compute_Mok_Outputs()
        {
            //Player status :
            UInt32 P1_Data_Address = _GameRAM_Address + (BitConverter.ToUInt32(ReadBytes(_GameRAM_Address + 0x00023464, 4), 0) & 0x01FFFFFF);
            UInt32 P2_Data_Address = P1_Data_Address + 0x64;
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            //Player Status :
            //1 : Title Screen
            //2,3,4 : Demo
            //5 : Attract Mode
            //6 : Game Over
            //7 : Playing (cut scene)
            //8 : Playing
            //9 : continue Screen
            Byte P1_Status = ReadByte(P1_Data_Address + 0x48);
            Byte P2_Status = ReadByte(P2_Data_Address + 0x48);
            if (P1_Status == 8 || P1_Status == 7)
            {
                _P1_Life = ReadByte(P1_Data_Address + 0x5C);
                _P1_Ammo = ReadByte(P1_Data_Address + 0x58);

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
            if (P2_Status == 8 || P2_Status == 7)
            {
                _P2_Life = ReadByte(P2_Data_Address + 0x5C);
                _P2_Ammo = ReadByte(P2_Data_Address + 0x58);

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

            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_GameRAM_Address + 0x000EC396) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_GameRAM_Address + 0x000EC396) >> 4 & 0x01);
            SetOutputValue(OutputId.Credits, ReadByte(P1_Data_Address + 0x7C));
        }
         
        #endregion

    }
}
