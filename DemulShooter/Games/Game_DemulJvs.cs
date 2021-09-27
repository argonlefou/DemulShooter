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
        public Game_DemulJvs(String Rom, String DemulVersion, double ForcedXratio, bool Verbose, bool DisableWindow, bool WidescreenHack)
            : base(Rom, "naomiJvs", DemulVersion, ForcedXratio, Verbose, DisableWindow, WidescreenHack)
        {}

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
            UInt32 Outputs_Address = (UInt32)_TargetProcess_MemoryBaseAddress + 0x00300364;
            UInt32 Credits_Address = 0x2C480D8C;
            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(Outputs_Address) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(Outputs_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, ReadByte(Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, ReadByte(Outputs_Address) >> 5 & 0x01);

            //Custom recoil will be activated just like original one
            SetOutputValue(OutputId.P1_CtmRecoil, ReadByte(Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_CtmRecoil, ReadByte(Outputs_Address) >> 5 & 0x01);

            //Credits
            if (_RomName.Equals("ninjasltj"))
                Credits_Address = 0x2C3D9D90;
            SetOutputValue(OutputId.Credits, ReadByte(Credits_Address));
        }

        #endregion
    }
}
