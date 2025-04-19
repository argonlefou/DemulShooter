using System;
using System.Collections.Generic;
using System.Diagnostics;
using DsCore.Win32;

namespace DsCore.MemoryX64
{
    public class Codecave
    {
        private Process _Process;
        private IntPtr _ProcessHandle;
        private IntPtr _ModuleBaseAddress = IntPtr.Zero;
        private UInt64 _Cave_Address = 0;
        private UInt64 _CaveOffset = 0;

        public UInt64 CaveAddress
        {
            get { return _Cave_Address; }
        }
        public UInt64 CaveOffset
        {
            get { return _CaveOffset; }
        }

        public Codecave(Process p, IntPtr BaseAddress)
        {
            _Process = p;
            _ModuleBaseAddress = BaseAddress;
        }

        /// <summary>
        /// Trying to access the process
        /// </summary>
        /// <returns>True if success, otherwise False</returns>
        public bool Open()
        {
            _ProcessHandle = _Process.Handle;
            if (_ProcessHandle != IntPtr.Zero)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Reserves a region of memory within the virtual address space of a specified process. 
        /// The function initializes the memory it allocates to zero.
        /// </summary>
        /// <param name="Size">The size of the region of memory to allocate, in bytes.</param>
        /// <returns>True is success, otherwise False</returns>
        public bool Alloc(UInt32 Size)
        {
            //Allocation mémoire
            _Cave_Address = (UInt64)Win32API.VirtualAllocEx(_ProcessHandle, IntPtr.Zero, Size, MemoryAllocType.MEM_COMMIT, MemoryPageProtect.PAGE_EXECUTE_READWRITE);
            if (_Cave_Address != 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// To call an absolute X64 Address, using the following sequence :
        /// call RIP+2
        /// JMP 08 (to jump over the RIP+2 address and get back to code)
        /// </summary>
        /// <param name="AbsoluteAddress"></param>
        /// <returns></returns>
        public bool Write_call_absolute(UInt64 AbsoluteAddress)
        {
            List<Byte> Buffer = new List<byte>();
            Buffer.Add(0xFF);
            Buffer.Add(0x15);
            Buffer.Add(0x02);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0xEB);
            Buffer.Add(0x08);
            Buffer.AddRange(BitConverter.GetBytes(AbsoluteAddress));
            return Write_Bytes(Buffer.ToArray());
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

        /// <summary>
        /// Write bytes in memory, read from a string like "00 00 00 00"
        /// </summary>
        /// <param name="StrBuffer">String formated series of bytes to write</param>
        /// <returns>True if success, otherwise False</returns>
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

        /// <summary>
        /// Write bytes in memory, read from an array of bytes
        /// </summary>
        /// <param name="Buffer">Array of bytes to write</param>
        /// <returns>True if success, otherwise False</returns>
        public bool Write_Bytes(Byte[] Buffer)
        {
            UIntPtr BytesWritten = UIntPtr.Zero;
            if (Win32API.WriteProcessMemoryX64(_ProcessHandle, (IntPtr)(_Cave_Address + _CaveOffset), Buffer, (UIntPtr)Buffer.Length, out BytesWritten))
            {
                _CaveOffset += (UInt64)BytesWritten;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Create both Jump-To and Jump-Back instruction and inject the codecave in memory 
        /// </summary>
        /// <param name="iStruct">InjectionStruct with needed offset to inject the codecave to the good paert of the game code</param>
        /// <param name="sCodeCaveName">Name that will be used to log the codecave creation</param>
        public void InjectToOffset(InjectionStruct iStruct, string sCodeCaveName, bool bCreateButNotInject = false)
        {
            //Jump back
            Write_jmp((UInt64)_ModuleBaseAddress + iStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding " + sCodeCaveName + " CodeCave at : 0x" + _Cave_Address.ToString("X16"));

            //Code injection
            List<byte> Buffer = new List<byte>();
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer = new List<byte>();
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(_Cave_Address));
            for (int i = 0; i < iStruct.NeededNops; i++)
            {
                Buffer.Add(0x90);
            }

            //For "crash" debug purpose, sometimes I need to create the codecave to examine it, without making the game jump to it.
            if (!bCreateButNotInject)
                Win32API.WriteProcessMemoryX64(_ProcessHandle, (IntPtr)((UInt64)_ModuleBaseAddress + iStruct.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        /// <summary>
        /// Create both Jump-To and Jump-Back instruction and inject the codecave in memory 
        /// </summary>
        /// <param name="iStruct">InjectionStruct with needed offset to inject the codecave to the good paert of the game code</param>
        /// <param name="sCodeCaveName">Name that will be used to log the codecave creation</param>
        public void InjectToAddress(InjectionStruct iStruct, string sCodeCaveName, bool bCreateButNotInject = false)
        {
            //Jump back
            Write_jmp(iStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding " + sCodeCaveName + " CodeCave at : 0x" + _Cave_Address.ToString("X16"));

            //Code injection
            List<byte> Buffer = new List<byte>();
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer = new List<byte>();
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(_Cave_Address));
            for (int i = 0; i < iStruct.NeededNops; i++)
            {
                Buffer.Add(0x90);
            }

            //For "crash" debug purpose, sometimes I need to create the codecave to examine it, without making the game jump to it.
            if (!bCreateButNotInject)
                Win32API.WriteProcessMemoryX64(_ProcessHandle, (IntPtr)iStruct.InjectionOffset, Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        /// <summary>
        /// Asolute jump Codecave will need 14 bytes to be written.
        /// When space is limited, we can use a Trampoline address to use a short 5-bytes long short jump to some close free area
        /// In which we will be able to write the 14-bytes long JMP
        /// </summary>
        /// <param name="iStruct"></param>
        /// <param name="TrampolineAddress"></param>
        /// <param name="sCodeCaveName"></param>
        /// <param name="bCreateButNotInject"></param>
        public void InjectToOffset_WithTrampoline(InjectionStruct iStruct, UInt64 TrampolineOffset, string sCodeCaveName, bool bCreateButNotInject = false)
        {
            //Jump back
            Write_jmp((UInt64)_ModuleBaseAddress + iStruct.InjectionReturnOffset);

            Logger.WriteLog("Adding " + sCodeCaveName + " CodeCave at : 0x" + _Cave_Address.ToString("X16"));

            //Code injection
            //1st Step : writing the long jump in the Trampoline
            List<byte> Buffer = new List<byte>();
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer = new List<byte>();
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(_Cave_Address));
            
            //For "crash" debug purpose, sometimes I need to create the codecave to examine it, without making the game jump to it.
            if (!bCreateButNotInject)
                Win32API.WriteProcessMemoryX64(_ProcessHandle, (IntPtr)((UInt64)_ModuleBaseAddress + TrampolineOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);

            //2nd step : writing the short jump
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes((UInt32)(TrampolineOffset - iStruct.InjectionOffset - 5)));
            
            //InjectionStruct inner NeededNops is based on 14-bytes length absolute jump injection
            //Using a custom one for 5-bytes length short JMP
            uint NeededNops = iStruct.Length - 5;
            for (int i = 0; i < NeededNops; i++)
            {
                Buffer.Add(0x90);
            }
            if (!bCreateButNotInject)
                Win32API.WriteProcessMemoryX64(_ProcessHandle, (IntPtr)((UInt64)_ModuleBaseAddress + iStruct.InjectionOffset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }
    }
}
