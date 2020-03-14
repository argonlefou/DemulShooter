using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DemulShooter
{
    /// <summary>
    /// This class is the base class to create codecave into some 32bits process memory
    /// </summary>
    class Memory
    {
        [Flags]
        public enum AllocType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
        }
        [Flags]
        public enum Protect
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
        }
        [Flags]
        public enum FreeType
        {
            Decommit = 0x4000,
            Release = 0x8000,
        }

        public int CaveAddress
        {
            get { return _Cave_Address; }
        }
        public int CaveOffset
        {
            get { return _CaveOffset; }
        }

        [DllImport("kernel32.dll")]
        public static extern Int32 VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, AllocType flAllocationType, Protect flProtect);
        [DllImport("kernel32.dll")]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, FreeType dwFreeType);
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

        private const int PROCESS_WM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_OPERATION = 0x0008;

        private Process _Process;
        private IntPtr _ProcessHandle;
        private IntPtr _ModuleBaseAddress = IntPtr.Zero;
        private int _Cave_Address = 0;
        private int _CaveOffset = 0;

        public Memory(Process p, IntPtr BaseAddress)
        {
            _Process = p;
            _ModuleBaseAddress = BaseAddress;
        }

        //Ouverture du Process
        public bool Open()
        {
            //_ProcessHandle = OpenProcess(0x1F0FFF, false, _Process.Id);
            _ProcessHandle = _Process.Handle;
            if (_ProcessHandle != IntPtr.Zero)
                return true;
            else
                return false;
        }

        //Allocation mémoire
        public bool Alloc(int Size)
        {
            //Allocation mémoire
            _Cave_Address = VirtualAllocEx(_ProcessHandle, IntPtr.Zero, Size, AllocType.Commit, Protect.ExecuteReadWrite);
            if (_Cave_Address != 0)
                return true;
            else
                return false;
        }

        //call Address
        public bool Write_call(int Address)
        {
            int JmpAddress = Address - (_Cave_Address + _CaveOffset) - 5;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0xE8);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //cmp eax,[Value]
        public bool Write_cmp(int Value)
        {
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0x81);
            Buffer.Add(0xF9);
            Buffer.AddRange(BitConverter.GetBytes(Value));
            return Write_Bytes(Buffer.ToArray());
        }

        //je [Address]
        public bool Write_je(int Address)
        {
            int JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0x0F);
            Buffer.Add(0x84);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jb [Address]
        public bool Write_jb(int Address)
        {
            int JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0x0F);
            Buffer.Add(0x82);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jng [Address]
        public bool Write_jng(int Address)
        {
            int JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0x0F);
            Buffer.Add(0x8E);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jnl [Address]
        public bool Write_jnl(int Address)
        {
            int JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0x0F);
            Buffer.Add(0x8D);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jng [Address]
        public bool Write_jg(int Address)
        {
            int JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0x0F);
            Buffer.Add(0x8F);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jnl [Address]
        public bool Write_jl(int Address)
        {
            int JmpAddress = Address - (_Cave_Address + _CaveOffset) - 6;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0x0F);
            Buffer.Add(0x8C);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //jmp [Address]
        public bool Write_jmp(int Address)
        {
            int JmpAddress = Address - (_Cave_Address + _CaveOffset) - 5;
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(JmpAddress));
            return Write_Bytes(Buffer.ToArray());
        }

        //nop
        public bool Write_nop(int Amount = 1)
        {
            List<Byte> Buffer = new List<byte>();
            for (int i = 0; i < Amount; i++)
            {
                Buffer.Add(0x90);
            }
            return Write_Bytes(Buffer.ToArray());
        }

        //Ecriture d'une série de Bytes transmise sous forme de String "00 00 00 00"
        public bool Write_StrBytes(String StrBuffer)
        {
            String[] sBytes = StrBuffer.Split(' ');
            List<Byte> Buffer = new List<byte>();
            foreach (String hex in sBytes)
            {
                Buffer.Add((byte)Convert.ToInt32(hex, 16));
            }
            return Write_Bytes(Buffer.ToArray());
        }

        //Ecriture d'une série de Bytes
        public bool Write_Bytes(Byte[] Buffer)
        {
            int BytesWritten = 0;
            if (WriteProcessMemory((int)_ProcessHandle, _Cave_Address + _CaveOffset, Buffer, Buffer.Length, ref BytesWritten))
            {
                _CaveOffset += BytesWritten;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
