using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace UnityPlugin_BepInEx_NHA2
{
    public class NHA2_MemoryMappedFile_Controller
    {
        #region WIN32

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFileMapping(int hFile, IntPtr lpAttributes, PageProtection flProtect, uint dwMaxSizeHi, uint dwMaxSizeLow, string lpName);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr OpenFileMapping(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, string lpName);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMapping, FileMapAccessType dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool UnmapViewOfFile(IntPtr pvBaseAddress);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [Flags]
        public enum PageProtection : uint
        {
            NoAccess = 0x01,
            Readonly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            Guard = 0x100,
            NoCache = 0x200,
            WriteCombine = 0x400,
        }

        [Flags]
        public enum FileMapAccessType : uint
        {
            Copy = 0x01,
            Write = 0x02,
            Read = 0x04,
            AllAccess = 0x08,
            Execute = 0x20,
        }

        public const UInt32 ERROR_ALREADY_EXISTS = 183;
        public const Int32 INVALID_HANDLE_VALUE = -1;

        #endregion

        private IntPtr _hSharedMemoryFile = IntPtr.Zero;
        private String _sMmfName = String.Empty;
        private bool _bMmfAlreadyExist = false;
        private long _MemSize = 0;

        private Mutex _Mutex;
        private String _sMutexName = String.Empty;
        private bool _IsMutexCreated;

        private IntPtr _pwData = IntPtr.Zero;
        private bool _bInit = false;

        //Shared Memory Payload between DemulSHooter and the game, structured as followed:
        // Demulshooter --> Game :
        //Byte[0]           : P1_InGameX        [0-WindowX]
        //Byte[4]           : P1_InGameY        [0-WindowY]
        //Byte[8]           : P2_InGameX        [0-WindowX]
        //Byte[12]          : P2_InGameY        [0-WindowY]
        //Byte[16]          : P1_Trigger        [0-1-2]
        //Byte[20]          : P2_Trigger        [0-1-2]
        //Byte[24]          : P1_ChangeWeapon   [0-1-2]
        //Byte[28]          : P2_ChangeWeapon   [0-1-2]
        //Byte[32]          : P1_Special        [0-1-2]
        //Byte[36]          : P2_Special        [0-1-2]

        // Game -> DemulShooter :
        //Byte[40]         : P1_Life            [int]
        //Byte[44]         : P2_Life            [int]
        //Byte[48]         : P1_Motor           [0-1]
        //Byte[52]         : P2_Motor           [0-1]        
        //Byte[56]         : Credits            [int]
    

        private Byte[] _bPayload;

        #region Payload Indexes
        public const int PAYLOAD_LENGTH = 200;

        public const int INDEX_P1_INGAME_X = 0;
        public const int INDEX_P1_INGAME_Y = 4;
        public const int INDEX_P2_INGAME_X = 8;
        public const int INDEX_P2_INGAME_Y = 12;

        public const int INDEX_P1_TRIGGER = 16;
        public const int INDEX_P2_TRIGGER = 20;
        public const int INDEX_P1_WEAPON = 24;
        public const int INDEX_P2_WEAPON = 28;
        public const int INDEX_P1_SPECIAL = 32;
        public const int INDEX_P2_SPECIAL = 36;
        
        public const int INDEX_P1_LIFE = 40;
        public const int INDEX_P2_LIFE = 44;
        public const int INDEX_P1_MOTOR = 48;
        public const int INDEX_P2_MOTOR = 52;
        public const int INDEX_CREDITS = 56;

        #endregion

        public Byte[] Payload
        {
            get { return _bPayload; }
            set { _bPayload = value; }
        }

        public bool IsOpened
        {
            get { return _bInit; }
        }

        public NHA2_MemoryMappedFile_Controller(string MMF_Name, string Mutex_Name, long lngSize)
        {
            _sMmfName = MMF_Name;
            _sMutexName = Mutex_Name;

            if (lngSize <= 0 || lngSize > 0x00800000) lngSize = 0x00800000;
            _MemSize = lngSize;

            _bPayload = new Byte[PAYLOAD_LENGTH];
        }

        ~NHA2_MemoryMappedFile_Controller()
        {
            MMFClose();
        }

        /// <summary>        
        ///  Initialize shared memory         
        /// </summary>        
        /// <param name="strName"> Shared memory name </param>        
        /// <param name="lngSize"> Shared memory size </param>        
        /// <returns></returns>        
        public int MMFOpen()
        {
            if (_sMmfName.Length > 0)
            {
                // Create a memory share (INVALID_HANDLE_VALUE)                
                _hSharedMemoryFile = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, PageProtection.ReadWrite, 0, (uint)_MemSize, _sMmfName);
                if (_hSharedMemoryFile == IntPtr.Zero)
                {
                    _bMmfAlreadyExist = false;
                    _bInit = false;
                    return 2; // Failed to create shared body					                 
                }
                else
                {
                    if (Marshal.GetLastWin32Error() == ERROR_ALREADY_EXISTS)  // Already Created   
                    {
                        _bMmfAlreadyExist = true;
                    }
                    else
                    {
                        _bMmfAlreadyExist = false; // New creation 
                    }
                }
                //---------------------------------------                
                // Create memory maps              
                //FileMapAccessType.AllAccess gives Memory protected ERROR !!!
                _pwData = MapViewOfFile(_hSharedMemoryFile, FileMapAccessType.Write, 0, 0, (uint)_MemSize);
                if (_pwData == IntPtr.Zero)
                {
                    _bInit = false;
                    CloseHandle(_hSharedMemoryFile);
                    return 3; // Failed to create memory map						                 
                }
                else
                {
                    if (_bMmfAlreadyExist == false)
                    {
                        // initialization                     
                    }
                }
                //----------------------------------------  

                if (_sMutexName.Length > 0)
                {
                    _Mutex = new Mutex(false, _sMutexName, out _IsMutexCreated); //false to not take the mutex at creation !!  
                    if (_Mutex == null)
                        return 4; //Mutex Error 
                }
                else
                {
                    return 5; //Mutex Parameter Error
                }
            }
            else
            {
                return 1; // Parameter error 					                
            }

            _bInit = true;

            return 0; // Create success         
        }

        /// <summary>        
        ///  Reading data         
        /// </summary>        
        /// <param name="bytData"> data </param>        
        /// <param name="lngAddr"> Initial address </param>        
        /// <param name="lngSize"> Number </param>        
        /// <returns></returns>        
        public int ReadAll()
        {
            if (PAYLOAD_LENGTH > _MemSize)
                return 2; // Beyond data area 

            if (_bInit)
            {
                if (_Mutex != null)
                    _Mutex.WaitOne();

                Marshal.Copy(_pwData, _bPayload, 0, (int)PAYLOAD_LENGTH);

                if (_Mutex != null)
                    _Mutex.ReleaseMutex();
            }
            else
            {
                return 1; // Shared memory not initialized             
            }
            return 0; // Read successfully         
        }

        public int Writeall()
        {
            return WriteByteArray(_bPayload, 0, PAYLOAD_LENGTH);
        }

        /// <summary>        
        ///  Writing data         
        /// </summary>        
        /// <param name="bytData"> data </param>        
        /// <param name="lngAddr"> Initial address </param>        
        /// <param name="lngSize"> Number </param>        
        /// <returns></returns>        
        private int WriteByteArray(byte[] bytData, int lngAddr, int lngSize)
        {
            if (lngAddr + lngSize > _MemSize)
                return 2; // Beyond data area             
            if (_bInit)
            {
                try
                {
                    if (_Mutex != null)
                        _Mutex.WaitOne();

                    Marshal.Copy(bytData, lngAddr, _pwData, lngSize);

                    if (_Mutex != null)
                        _Mutex.ReleaseMutex();
                }
                catch
                {
                    return 2;
                }
            }
            else
            {
                return 1; // Shared memory not initialized             
            }
            return 0; // Write a successful         
        }

        /// <summary>       
        ///  Turn off shared memory         
        /// </summary>        
        public void MMFClose()
        {
            if (_bInit)
            {
                UnmapViewOfFile(_pwData);
                CloseHandle(_hSharedMemoryFile);
            }
        }
    }
}
