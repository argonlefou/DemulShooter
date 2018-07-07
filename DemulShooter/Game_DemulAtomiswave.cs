using System;
using System.Collections.Generic;

namespace DemulShooter
{
    class Game_DemulAtomiswave : Game_Demul
    {
        public Game_DemulAtomiswave(String Rom, String DemulVersion, bool Verbose, bool DisableWindow, bool WidescreenHack)
            : base(Rom, "atomiswave", DemulVersion, Verbose, DisableWindow, WidescreenHack)
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
            //cmp ecx, 42
            CaveMemory.Write_StrBytes("83 F9 42");
            //je @
            CaveMemory.Write_StrBytes("0F 84 35 00 00 00");
            //cmp ecx, 43
            CaveMemory.Write_StrBytes("83 F9 43");
            //je @
            CaveMemory.Write_StrBytes("0F 84 2C 00 00 00");
            //cmp ecx, 0
            CaveMemory.Write_StrBytes("83 F9 00");
            //je @
            CaveMemory.Write_StrBytes("0F 84 28 00 00 00");
            //cmp ecx, 1
            CaveMemory.Write_StrBytes("83 F9 01");
            //je @
            CaveMemory.Write_StrBytes("0F 84 1A 00 00 00");
            //cmp ecx, 40
            CaveMemory.Write_StrBytes("83 F9 40");
            //je @
            CaveMemory.Write_StrBytes("0F 84 16 00 00 00");
            //cmp ecx, 41
            CaveMemory.Write_StrBytes("83 F9 41");
            //je @
            CaveMemory.Write_StrBytes("0F 84 08 00 00 00");
            //mov [ecx*2+padDemul.dll+2FE50],ax
            CaveMemory.Write_StrBytes("66 89 04 4D");
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //jmp @
            CaveMemory.Write_jmp((int)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Return_Offset);
            //and eax, 08
            CaveMemory.Write_StrBytes("83 E0 08");
            //cmp eax, 0
            CaveMemory.Write_StrBytes("83 F8 00");
            //je @
            CaveMemory.Write_StrBytes("0F 84 0A 00 00 00");
            //or dword ptr [ecx*2+padDemul.dll+2FE50],08
            CaveMemory.Write_StrBytes("83 0C 4D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("08");
            //jmp @
            CaveMemory.Write_StrBytes("EB E5");
            //and dword ptr [ecx*2+padDemul.dll+2FE50],-09 { 247 }
            CaveMemory.Write_StrBytes("83 24 4D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("F7");
            //jmp @
            CaveMemory.Write_StrBytes("EB DB");

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
            WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Start_Button_Offset, 0xFF);
            WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Fire_Button_Offset, 0xFF);
            WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, init);
            WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Start_Button_Offset, 0xFF);
            WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Fire_Button_Offset, 0xFF);

            WriteLog("Atomiswave Memory Hack complete !");
            WriteLog("-");
        }

        public override void  SendInput(MouseInfo mouse, int Player)
        {
            byte[] buffer = { (byte)(mouse.pTarget.X >> 8), (byte)(mouse.pTarget.X & 0xFF), (byte)(mouse.pTarget.Y >> 8), (byte)(mouse.pTarget.Y & 0xFF) };
            if (Player == 1)
            {
                //Write Axis
                WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, buffer);
                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Fire_Button_Offset, 0xFB);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Fire_Button_Offset, 0xFF);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    /* For Sprtshot, we force offscreen shoot for reload */
                    if (_RomName.Equals("sprtshot"))
                    {
                        buffer[0] = (byte)(0);
                        buffer[1] = (byte)(0);
                        buffer[2] = (byte)(0);
                        buffer[3] = (byte)(0);
                        WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, buffer);
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Fire_Button_Offset, 0xFB);
                        System.Threading.Thread.Sleep(50);
                    }
                    else
                    {
                        //Read Value and set Bit 2 to 0
                        byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Start_Button_Offset);
                        val = (byte)(val & 0xFB);
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Start_Button_Offset, val);
                    }
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    if (_RomName.Equals("sprtshot"))
                    {
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Fire_Button_Offset, 0xFF);
                    }
                    else
                    {
                        //Read Value and set Bit 2 to 1
                        byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Start_Button_Offset);
                        val = (byte)(val | 0x04);
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Start_Button_Offset, val);
                    }
                }
            }
            else if (Player == 2)
            {
                //Write Axis
                WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, buffer);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Fire_Button_Offset, 0xFB);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Fire_Button_Offset, 0xFF);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    /* For Sprtshot, we force offscreen shoot for reload */
                    if (_RomName.Equals("sprtshot"))
                    {
                        buffer[0] = (byte)(0);
                        buffer[1] = (byte)(0);
                        buffer[2] = (byte)(0);
                        buffer[3] = (byte)(0);
                        WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, buffer);
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Fire_Button_Offset, 0xFB);
                        System.Threading.Thread.Sleep(50);
                    }
                    else
                    {
                        //Read Value and set Bit 2 to 0
                        byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Start_Button_Offset);
                        val = (byte)(val & 0xFB);
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Start_Button_Offset, val);
                    }
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    if (_RomName.Equals("sprtshot"))
                    {
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Fire_Button_Offset, 0xFF);
                    }
                    else
                    {
                        //Read Value and set Bit 2 to 1
                        byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Start_Button_Offset);
                        val = (byte)(val | 0x04);
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Start_Button_Offset, val);
                    }
                }
            }
        }
    }

}
