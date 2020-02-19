using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DemulShooter
{
    class MemoryX64
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

        public UInt64 CaveAddress
        {
            get { return _Cave_Address; }
        }
        public UInt64 CaveOffset
        {
            get { return _CaveOffset; }
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, AllocType flAllocationType, Protect flProtect);
        [DllImport("kernel32.dll")]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, FreeType dwFreeType);
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);        
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "ReadProcessMemory")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean ReadProcessMemoryX64([In] IntPtr hProcess, [In] IntPtr lpBaseAddress, [Out] Byte[] lpBuffer, [In] UIntPtr nSize, [Out] out UIntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "WriteProcessMemory")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean WriteProcessMemoryX64([In] IntPtr hProcess, [In] IntPtr lpBaseAddress, [Out] Byte[] lpBuffer, [In] UIntPtr nSize, [Out] out UIntPtr lpNumberOfBytesWritten);

        
        private const int PROCESS_WM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_OPERATION = 0x0008;

        private Process _Process;
        private IntPtr _ProcessHandle;
        private IntPtr _ModuleBaseAddress = IntPtr.Zero;
        private UInt64 _Cave_Address = 0;
        private UInt64 _CaveOffset = 0;

        public MemoryX64(Process p, IntPtr BaseAddress)
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
            _Cave_Address = (UInt64)VirtualAllocEx(_ProcessHandle, IntPtr.Zero, Size, AllocType.Commit, Protect.ExecuteReadWrite);
            if (_Cave_Address != 0)
                return true;
            else
                return false;
        }        

        //jmp [Address]
        public bool Write_jmp(UInt64 AbsoluteAddress)
        {
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(AbsoluteAddress));
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
            UIntPtr BytesWritten = UIntPtr.Zero;
            if (WriteProcessMemoryX64(_ProcessHandle, (IntPtr)(_Cave_Address + _CaveOffset), Buffer, (UIntPtr)Buffer.Length, out BytesWritten))
            {
                _CaveOffset += (UInt64)BytesWritten;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
