using System;
using System.Collections.Generic;

namespace DemulShooter
{
    class Game_DemulHikaru : Game_Demul
    {
        public Game_DemulHikaru(String Rom, String DemulVersion, bool Verbose, bool DisableWindow, bool WidescreenHack)
            : base(Rom, "hikaru", DemulVersion, Verbose, DisableWindow, WidescreenHack)
        {

        }

        protected override void SetHack_057()
        {
        }

        protected override void SetHack_07()
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
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
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //jmp @
            CaveMemory.Write_jmp((int)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Return_Offset);
            //cmp ax,80
            CaveMemory.Write_StrBytes("3D 80 00 00 00");
            //jnl @
            CaveMemory.Write_StrBytes("0F 8D 0D 00 00 00");
            //and dword ptr [ecx*2+padDemul.dll+OFFSET],7F
            CaveMemory.Write_StrBytes("81 24 4D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("7F FF FF FF");
            //jmp @
            CaveMemory.Write_StrBytes("EB E3");
            //or [ecx*2+padDemul.dll+OFFSET],00000080
            CaveMemory.Write_StrBytes("81 0c 4D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("80 00 00 00");
            //jmp @
            CaveMemory.Write_StrBytes("EB D6");

            WriteLog("Adding CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Injection de code
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((int)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);

            //Initialise pour prise en compte des guns direct
            byte[] init = { 0, 0x7f, 0, 0x7f };
            WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, init);
            //WriteByte(P1_INOUT_ADDRESS, 0x01);
            WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x00);
            WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, init);
            //WriteByte(P2_INOUT_ADDRESS, 0x01);
            WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x00);

            WriteLog("Hikaru Memory Hack complete !");
            WriteLog("-");
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            byte[] buffer = { (byte)(mouse.pTarget.X >> 8), (byte)(mouse.pTarget.X & 0xFF), (byte)(mouse.pTarget.Y >> 8), (byte)(mouse.pTarget.Y & 0xFF) };
            if (Player == 1)
            {
                //Write Axis
                WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, buffer);
                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x02);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x00);
                }
                if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset + 2, 0x80);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset + 2, 0x00);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x00);
                }
            }
            else if (Player == 2)
            {
                //Write Axis
                WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, buffer);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x02);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x00);
                }
                if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset + 2, 0x80);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset + 2, 0x00);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x00);
                }
            }
        }
    }
}
