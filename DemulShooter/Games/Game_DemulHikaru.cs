using System;
using System.Collections.Generic;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.MameOutput;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_DemulHikaru : Game_Demul
    {
        public Game_DemulHikaru(String Rom, String DemulVersion, double ForcedXratio, bool Verbose, bool DisableWindow, bool WidescreenHack)
            : base(Rom, "hikaru", DemulVersion, ForcedXratio, Verbose, DisableWindow, WidescreenHack)
        {}

        #region Memory Hack

        protected override void SetHack_07()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //cmp ecx, 2
            CaveMemory.Write_StrBytes("83 F9 02");
            //je @
            CaveMemory.Write_StrBytes("0F 84 47 00 00 00");
            //cmp ecx, 3
            CaveMemory.Write_StrBytes("83 F9 03");
            //je @
            CaveMemory.Write_StrBytes("0F 84 3E 00 00 00");
            //cmp ecx, 4
            CaveMemory.Write_StrBytes("83 F9 04");
            //je @
            CaveMemory.Write_StrBytes("0F 84 35 00 00 00");
            //cmp ecx, 42
            CaveMemory.Write_StrBytes("83 F9 42");
            //je @
            CaveMemory.Write_StrBytes("0F 84 2C 00 00 00");
            //cmp ecx, 43
            CaveMemory.Write_StrBytes("83 F9 43");
            //je @
            CaveMemory.Write_StrBytes("0F 84 23 00 00 00");
            //cmp ecx, 44
            CaveMemory.Write_StrBytes("83 F9 44");
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
            //or [ecx*2+padDemul.dll+OFFSET],00000080
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
            WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x00);
            WriteBytes((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, init);
            WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x00);

            Logger.WriteLog("Hikaru Memory Hack complete !");
            Logger.WriteLog("-");
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>  
        public override void SendInput(PlayerSettings PlayerData)
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
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0) 
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset + 2, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0) 
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset + 2, 0x00);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0) 
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x01);
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

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset + 2, 0x80);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset + 2, 0x00);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    WriteByte((UInt32)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x01);
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpPanel, OutputId.P1_LmpPanel));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpPanel, OutputId.P2_LmpPanel));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            if (_RomName.Equals("braveff"))
                Compute_Braveff_Outputs();
        }

        private void Compute_Braveff_Outputs()
        { 
            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(0x007000C4) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(0x007000C4) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_LmpPanel, ReadByte(0x007000C4) >> 5 & 0x01);
            SetOutputValue(OutputId.P2_LmpPanel, ReadByte(0x007000C4) >> 2 & 0x01);
            SetOutputValue(OutputId.P1_GunMotor, ReadByte(0x007000C4) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, ReadByte(0x007000C4) >> 3 & 0x01);
            
            SetOutputValue(OutputId.Credits, ReadByte(0x20C00068));
        }

        #endregion
    }    
}
