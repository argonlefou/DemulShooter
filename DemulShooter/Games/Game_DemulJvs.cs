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
    class Game_DemulJvs : Game_Demul
    {
        private UInt32 _Outputs_Outputs_Offset = 0x0022C42D;
        private UInt32 _Outputs_Credits_Offset = 0x00480D0C;
        private UInt32 _Outputs_PlayerData_Offset = 0x003D9D30;
        private int _P1_LastDammage = 0;
        private int _P2_LastDammage = 0;

        public Game_DemulJvs(String Rom, String DemulVersion, bool DisableInputHack, bool Verbose, bool DisableWindow, bool WidescreenHack)
            : base(Rom, "naomiJvs", DemulVersion, DisableInputHack, Verbose, DisableWindow, WidescreenHack)
        {

            if (Rom.Equals("ninjaslt"))
            {
                _Outputs_Outputs_Offset = 0x0022C42D;
                _Outputs_Credits_Offset = 0x00480D0C;
                _Outputs_PlayerData_Offset = 0x003D9D30;
                //_Outputs_PlayerData_Offset = 0x003D9CB0; ?
            }
            else if (Rom.Equals("ninjaslta"))
            {
                _Outputs_Outputs_Offset = 0x0022C4AD;
                _Outputs_Credits_Offset = 0x00480D8C;
                _Outputs_PlayerData_Offset = 0x003D9D30;
            }
            else if (Rom.Equals("ninjasltj"))
            {
                _Outputs_Outputs_Offset = 0x0022C3F1;
                _Outputs_Credits_Offset = 0x00480CD4;
                _Outputs_PlayerData_Offset = 0x003D9C78;
            }
            else if (Rom.Equals("ninjasltu"))
            {
                _Outputs_Outputs_Offset = 0x0022C4AD;
                _Outputs_Credits_Offset = 0x00480D8C;
                _Outputs_PlayerData_Offset = 0x003D9D30;
            }
        
        }

        #region Memory Hack

        protected override void SetHack_057()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //cmp ecx, 2
            CaveMemory.Write_StrBytes("83 F9 02");
            //je @
            CaveMemory.Write_StrBytes("0F 84 4C 00 00 00");
            //cmp ecx, 41
            CaveMemory.Write_StrBytes("83 F9 41");
            //je @
            CaveMemory.Write_StrBytes("0F 84 43 00 00 00");
            //cmp ecx, 03
            CaveMemory.Write_StrBytes("83 F9 03");
            //je @
            CaveMemory.Write_StrBytes("0F 84 35 00 00 00");
            //cmp ecx, 42
            CaveMemory.Write_StrBytes("83 F9 42");
            //je @
            CaveMemory.Write_StrBytes("0F 84 2C 00 00 00");
            //cmp edi, 04
            CaveMemory.Write_StrBytes("83 FF 04");
            //je @
            CaveMemory.Write_StrBytes("0F 84 23 00 00 00");
            //cmp edi, 05
            CaveMemory.Write_StrBytes("83 FF 05");
            //je @
            CaveMemory.Write_StrBytes("0F 84 1A 00 00 00");
            //cmp edi, 43
            CaveMemory.Write_StrBytes("83 FF 43");
            //je @
            CaveMemory.Write_StrBytes("0F 84 11 00 00 00");
            //cmp edi, 44
            CaveMemory.Write_StrBytes("83 FF 44");
            //je @
            CaveMemory.Write_StrBytes("0F 84 08 00 00 00");
            //mov [edi*2+padDemul.dll+OFFSET],ax
            CaveMemory.Write_StrBytes("66 89 0C 7D");
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //jmp @
            CaveMemory.Write_jmp((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Return_Offset);
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //and ecx,80
            CaveMemory.Write_StrBytes("81 E1 80 00 00 00");
            //cmp ecx,80
            CaveMemory.Write_StrBytes("81 F9 80 00 00 00");
            //je @
            CaveMemory.Write_StrBytes("0F 84 24 00 00 00");
            //and dword ptr [edi*2+padDemul.dll+OFFSET],7F
            CaveMemory.Write_StrBytes("81 24 7D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("7F FF FF FF");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //push ecx
            CaveMemory.Write_StrBytes("51");
            //and ecx,40
            CaveMemory.Write_StrBytes("83 E1 40");
            //cmp ecx,40
            CaveMemory.Write_StrBytes("83 F9 40");
            //je @
            CaveMemory.Write_StrBytes("0F 84 19 00 00 00");
            //and dword ptr [edi*2+padDemul.dll+OFFSET],BF
            CaveMemory.Write_StrBytes("83 24 7D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("BF");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //jmp @
            CaveMemory.Write_StrBytes("EB C4");
            //or [edi*2+padDemul.dll+OFFSET],00000080
            CaveMemory.Write_StrBytes("81 0C 7D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("80 00 00 00");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //jmp @
            CaveMemory.Write_StrBytes("EB DA");
            //or [edi*2+padDemul.dll+OFFSET],40
            CaveMemory.Write_StrBytes("83 0C 7D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("40");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //jmp @
            CaveMemory.Write_StrBytes("EB AB");

            Logger.WriteLog("Adding CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

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
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Offset,Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

            //Guns Init
            byte[] init = { 0xff, 0x07, 0xff, 0x07 };
            WriteBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, init);
            WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x00);
            WriteBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, init);
            WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x00);

            Logger.WriteLog("NaomiJVS Memory Hack complete !");
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
            CaveMemory.Write_StrBytes("0F 84 4C 00 00 00");
            //cmp ecx, 41
            CaveMemory.Write_StrBytes("83 F9 41");
            //je @
            CaveMemory.Write_StrBytes("0F 84 43 00 00 00");
            //cmp ecx, 03
            CaveMemory.Write_StrBytes("83 F9 03");
            //je @
            CaveMemory.Write_StrBytes("0F 84 35 00 00 00");
            //cmp ecx, 42
            CaveMemory.Write_StrBytes("83 F9 42");
            //je @
            CaveMemory.Write_StrBytes("0F 84 2C 00 00 00");
            //cmp ecx, 04
            CaveMemory.Write_StrBytes("83 F9 04");
            //je @
            CaveMemory.Write_StrBytes("0F 84 23 00 00 00");
            //cmp ecx, 05
            CaveMemory.Write_StrBytes("83 F9 05");
            //je @
            CaveMemory.Write_StrBytes("0F 84 1A 00 00 00");
            //cmp ecx, 43
            CaveMemory.Write_StrBytes("83 F9 43");
            //je @
            CaveMemory.Write_StrBytes("0F 84 11 00 00 00");
            //cmp ecx, 44
            CaveMemory.Write_StrBytes("83 F9 44");
            //je @
            CaveMemory.Write_StrBytes("0F 84 08 00 00 00");
            //mov [ecx*2+padDemul.dll+OFFSET],ax
            CaveMemory.Write_StrBytes("66 89 04 4D");
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //jmp @
            CaveMemory.Write_jmp((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Return_Offset);
            //push eax
            CaveMemory.Write_StrBytes("50");           
            //and eax,80
            CaveMemory.Write_StrBytes("25 80 00 00 00");
            //cmp eax,80
            CaveMemory.Write_StrBytes("3D 80 00 00 00");
            //je @
            CaveMemory.Write_StrBytes("0F 84 24 00 00 00");
            //and dword ptr [ecx*2+padDemul.dll+OFFSET],7F
            CaveMemory.Write_StrBytes("81 24 4D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("7F FF FF FF");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //and eax,40
            CaveMemory.Write_StrBytes("83 E0 40");
            //cmp eax,40
            CaveMemory.Write_StrBytes("83 F8 40");
            //je @
            CaveMemory.Write_StrBytes("0F 84 19 00 00 00");
            //and dword ptr [ecx*2+padDemul.dll+OFFSET],BF
            CaveMemory.Write_StrBytes("83 24 4D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("BF");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //jmp @
            CaveMemory.Write_StrBytes("EB C6");
            //or [ecx*2+padDemul.dll+OFFSET],00000080
            CaveMemory.Write_StrBytes("81 0C 4D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("80 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");            
            //jmp @
            CaveMemory.Write_StrBytes("EB DA");
            //or [ecx*2+padDemul.dll+OFFSET],40
            CaveMemory.Write_StrBytes("83 0C 4D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("40");
            //pop eax
            CaveMemory.Write_StrBytes("58");            
            //jmp @
            CaveMemory.Write_StrBytes("EB AD");

            Logger.WriteLog("Adding CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

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

            //Guns Init
            byte[] init = { 0xFF, 0x07, 0xFF, 0x07 };
            WriteBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, init);
            WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x00);
            WriteBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, init);
            WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x00);

            Logger.WriteLog("NaomiJVS Memory Hack complete !");
            Logger.WriteLog("-");
        }

        #endregion

        #region Inputs

        public override void SendInput(PlayerSettings PlayerData)
        {           
            //creating X-Y Hex value buffer to write memory
            byte[] buffer = 
            {   (byte)(PlayerData.RIController.Computed_X & 0xFF),
                (byte)(PlayerData.RIController.Computed_X >> 8), 
                (byte)(PlayerData.RIController.Computed_Y & 0xFF), 
                (byte)(PlayerData.RIController.Computed_Y >> 8)
            }; 
            
            if (PlayerData.ID == 1)
            {
                WriteBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, buffer);

                //Inputs
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0) 
                {
                    //Set On Screen flag
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset + 2, 0x1);
                    //Trigger pressed
                    Apply_OR_ByteMask((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x30);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0) 
                    Apply_AND_ByteMask((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0xCF);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0) 
                {
                    //Set Out Of Screen flag
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset + 2, 0);
                    //Trigger pressed
                    Apply_OR_ByteMask((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x30);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0) 
                {
                    //Set On Screen flag
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset + 2, 0x1);
                    //Trigger released
                    Apply_AND_ByteMask((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0xCF);
                }
            }
            else if (PlayerData.ID == 2)
            {
                //Write Axis
                WriteBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, buffer);

                //Inputs
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    //Set On Screen flag
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset + 2, 0x1);
                    //Trigger pressed
                    Apply_OR_ByteMask((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x30);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0xCF);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    //Set Out Of Screen flag
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset + 2, 0);
                    //Trigger pressed
                    Apply_OR_ByteMask((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x30);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    //Set On Screen flag
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset + 2, 0x1);
                    //Trigger released
                    Apply_AND_ByteMask((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0xCF);
                }
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunRecoil, OutputId.P2_GunRecoil));
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
            UInt32 Outputs_Address = _GameRAM_Address + _Outputs_Outputs_Offset;

            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            //Check if the game is in Gameplay mode 
            //2 and 3 seem to be gameplay and cutscene
            //0,1,9 in attract mode
            if (ReadByte(_GameRAM_Address + _Outputs_PlayerData_Offset) == 3 || ReadByte(_GameRAM_Address + _Outputs_PlayerData_Offset) == 2)
            {
                //Didn't find any reliable "player state", but Life seem to stay at 0 when not playing, so we will use that
                //Note that at start, life may be > 0 if the player has never entered a game :(
                _P1_Life = (int)BitConverter.ToInt16(ReadBytes(_GameRAM_Address + _Outputs_PlayerData_Offset + 0x54, 2), 0);
                _P2_Life = (int)BitConverter.ToInt16(ReadBytes(_GameRAM_Address + _Outputs_PlayerData_Offset + 0x56, 2), 0);

                //For custom dammaged : 
                //1) Solution 1 : Decrease life = small delay between the hit and the life beeing lost
                /*//[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);*/

                //2) solution 2 : Read a byte value wich is != 0 when hit (invicibility duration ?) but the "1" state duration is long and may trigger the output multiple times                
                int P1_Dammage = ReadByte(_GameRAM_Address + _Outputs_PlayerData_Offset + 0xBC);
                int P2_Dammage = ReadByte(_GameRAM_Address + _Outputs_PlayerData_Offset + 0xBE);
                if (P1_Dammage != 0 & _P1_LastDammage == 0)
                    SetOutputValue(OutputId.P1_Damaged, 1);
                if (P2_Dammage != 0 & _P2_LastDammage == 0)
                    SetOutputValue(OutputId.P2_Damaged, 1);
                _P1_LastDammage = P1_Dammage;
                _P2_LastDammage = P2_Dammage;

                if (_P1_Life > 0)
                {
                    _P1_Ammo = (int)BitConverter.ToInt16(ReadBytes(_GameRAM_Address + _Outputs_PlayerData_Offset + 0x62, 2), 0);

                    //Custom Recoil
                    if (_P1_Ammo < _P1_LastAmmo)
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (_P1_Ammo > 0)
                        P1_Clip = 1;
                }

                if (_P2_Life > 0)
                {
                    _P2_Ammo = (int)BitConverter.ToInt16(ReadBytes(_GameRAM_Address + _Outputs_PlayerData_Offset + 0x64, 2), 0);

                    //Custom Recoil
                    if (_P2_Ammo < _P2_LastAmmo)
                        SetOutputValue(OutputId.P2_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (_P2_Ammo > 0)
                        P2_Clip = 1;
                }
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
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(Outputs_Address) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(Outputs_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, ReadByte(Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, ReadByte(Outputs_Address) >> 5 & 0x01);

            //Custom recoil will be activated just like original one
            //REMOVED !! The recoil is activated when shooting offscreen !!
            /*SetOutputValue(OutputId.P1_CtmRecoil, ReadByte((IntPtr)Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_CtmRecoil, ReadByte((IntPtr)Outputs_Address) >> 5 & 0x01);*/

            //Credits
            SetOutputValue(OutputId.Credits, ReadByte(_GameRAM_Address + _Outputs_Credits_Offset));
        }

        #endregion
    }
}
