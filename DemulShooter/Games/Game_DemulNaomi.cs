using System;
using System.Collections.Generic;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_DemulNaomi : Game_Demul
    {
        public Game_DemulNaomi(String RomName, String DemulVersion, double ForcedXratio, bool Verbose, bool DisableWindow, bool WidescreenHack)
            : base(RomName, "naomi", DemulVersion, ForcedXratio, Verbose, DisableWindow, WidescreenHack)
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
    }
}
