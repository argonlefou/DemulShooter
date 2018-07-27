using System;
using System.Collections.Generic;

namespace DemulShooter
{
    class Game_DemulJvs : Game_Demul
    {
        public Game_DemulJvs(String Rom, String DemulVersion, bool Verbose, bool DisableWindow, bool WidescreenHack)
            : base(Rom, "naomiJvs", DemulVersion, Verbose, DisableWindow, WidescreenHack)
        {

        }

        protected override void SetHack_057()
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //cmp edi, 2
            CaveMemory.Write_StrBytes("83 FF 02");
            //je @
            CaveMemory.Write_StrBytes("0F 84 3A 00 00 00");
            //cmp edi, 41
            CaveMemory.Write_StrBytes("83 FF 41");
            //je @
            CaveMemory.Write_StrBytes("0F 84 31 00 00 00");
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
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //jmp @
            CaveMemory.Write_jmp((int)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Return_Offset);
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
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
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
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("BF");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //jmp @
            CaveMemory.Write_StrBytes("EB C4");
            //or [edi*2+padDemul.dll+OFFSET],00000080
            CaveMemory.Write_StrBytes("81 0C 7D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("80 00 00 00");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //jmp @
            CaveMemory.Write_StrBytes("EB DA");
            //or [edi*2+padDemul.dll+OFFSET],40
            CaveMemory.Write_StrBytes("83 0C 7D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("40");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //jmp @
            CaveMemory.Write_StrBytes("EB AB");

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
            byte[] init = { 0xff, 0x07, 0xff, 0x07 };
            WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, init);
            //WriteByte(P1_INOUT_ADDRESS, 0x01);
            WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x00);
            WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, init);
            //WriteByte(P2_INOUT_ADDRESS, 0x01);
            WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x00);

            WriteLog("NaomiJVS Memory Hack complete !");
            WriteLog("-");
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
            CaveMemory.Write_StrBytes("0F 84 3A 00 00 00");
            //cmp ecx, 41
            CaveMemory.Write_StrBytes("83 F9 41");
            //je @
            CaveMemory.Write_StrBytes("0F 84 31 00 00 00");
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
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //jmp @
            CaveMemory.Write_jmp((int)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Return_Offset);
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
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
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
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("BF");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //jmp @
            CaveMemory.Write_StrBytes("EB C6");
            //or [ecx*2+padDemul.dll+OFFSET],00000080
            CaveMemory.Write_StrBytes("81 0C 4D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("80 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");            
            //jmp @
            CaveMemory.Write_StrBytes("EB DA");
            //or [ecx*2+padDemul.dll+OFFSET],40
            CaveMemory.Write_StrBytes("83 0C 4D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("40");
            //pop eax
            CaveMemory.Write_StrBytes("58");            
            //jmp @
            CaveMemory.Write_StrBytes("EB AD");

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
            byte[] init = { 0xFF, 0x07, 0xFF, 0x07 };
            WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, init);
            //WriteByte(P1_INOUT_ADDRESS, 0x01);
            WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, 0x00);
            WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, init);
            //WriteByte(P2_INOUT_ADDRESS, 0x01);
            WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, 0x00);

            WriteLog("NaomiJVS Memory Hack complete !");
            WriteLog("-");
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            byte[] buffer = { (byte)(mouse.pTarget.X & 0xFF), (byte)(mouse.pTarget.X >> 8), (byte)(mouse.pTarget.Y & 0xFF), (byte)(mouse.pTarget.Y >> 8) };
            if (Player == 1)
            {
                //Write Axis
                WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, buffer);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    //Read Value and set Bits for left click
                    byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset);
                    val = (byte)(val | 0x30);
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, val);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    //Read Value and set Bits for left click
                    byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset);
                    val = (byte)(val & 0xCF);
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, val);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    /*
                     * 
                    //Set X axis off-screen and left click
                    byte[] OutOfScreen_buffer = { 0xFF, 0x07, 0x40, 0x00 };
                    WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, OutOfScreen_buffer);
                    //Read Value and set Bits for left click
                    byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset);
                    val = (byte)(val | 0x30);
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, val);
                    WriteLog("Reload :");
                    WriteLog(ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset).ToString("X") + " " + ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset + 1).ToString("X")+ " " + ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset + 2).ToString("X")+ " " + ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset + 3).ToString("X"));
                
                    */
                    WriteByte(_P1_Ammo_Address, (byte)0x08);

                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    /*
                    //Read Value and set Bits for left click
                    byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset);
                    val = (byte)(val & 0xCF);
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_Buttons_Offset, val);
                    */
                }
            }
            else if (Player == 2)
            {
                //Write Axis
                WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, buffer);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    //Read Value and set Bits for left click
                    byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset);
                    val = (byte)(val | 0x30);
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, val);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    //Read Value and set Bits for left click
                    byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset);
                    val = (byte)(val & 0xCF);
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, val);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                   /*
                    //Set X axis off-screen and left click
                    byte[] OutOfScreen_buffer = { 0xFF, 0x07, 0x40, 0x00 };
                    //Read Value and set Bits for left click
                    byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset);
                    val = (byte)(val | 0x30);
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, val);
                    * */

                    WriteByte(_P2_Ammo_Address, (byte)0x08);
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    /*
                    //Read Value and set Bits for left click
                    byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset);
                    val = (byte)(val & 0xCF);
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_Buttons_Offset, val);
                     * */
                }
            }
        }

    }
}
