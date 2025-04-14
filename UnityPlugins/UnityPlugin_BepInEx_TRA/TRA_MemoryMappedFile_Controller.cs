using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace TombRaider_BepInEx_DemulShooter_Plugin
{
    public class TRA_MemoryMappedFile_Controller
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
        //Byte[0]-Byte[3]   : P1_UIScreenX      [0-1920]
        //Byte[4]-Byte[7]   : P1_UIScreenY      [0-1080]
        //Byte[8]-Byte[11]  : P1_InGameX        [0-1]
        //Byte[12]-Byte[15] : P1_InGameY        [0-1]
        //Byte[16]-Byte[19] : P2_UIScreenX      [0-1920]
        //Byte[20]-Byte[23] : P2_UIScreenY      [0-1080]
        //Byte[24]-Byte[27] : P2_InGameX        [0-1]
        //Byte[28]-Byte[31] : P2_InGameY        [0-1]
        //Byte[32]-Byte[35] : P3_UIScreenX      [0-1920]
        //Byte[36]-Byte[39] : P3_UIScreenY      [0-1080]
        //Byte[40]-Byte[43] : P3_InGameX        [0-1]
        //Byte[44]-Byte[47] : P3_InGameY        [0-1]
        //Byte[48]-Byte[51] : P4_UIScreenX      [0-1920]
        //Byte[52]-Byte[55] : P4_UIScreenY      [0-1080]
        //Byte[56]-Byte[59] : P4_InGameX        [0-1]
        //Byte[60]-Byte[63] : P4_InGameY        [0-1]
        //Byte[64]          : P1_Trigger        [0-1-2]
        //Byte[68]          : P2_Trigger        [0-1-2]
        //Byte[72]          : P3_Trigger        [0-1-2]
        //Byte[76]          : P4_Trigger        [0-1-2]
        //Byte[80]          : P1_Reload         [0-1]
        //Byte[84]          : P2_Reload         [0-1]
        //Byte[88]          : P3_Reload         [0-1]
        //Byte[92]          : P4_Reload         [0-1]
        // Game -> DemulShooter :
        //Byte[96]          : P1_Life           [Float]
        //Byte[100]         : P2_Life           [Float]
        //Byte[104]         : P3_Life           [Float]
        //Byte[108]         : P4_Life           [Float]
        //Byte[112]         : P1_Motor          [0-1]
        //Byte[116]         : P2_Motor          [0-1]
        //Byte[120]         : P3_Motor          [0-1]
        //Byte[124]         : P4_Motor          [0-1]
        //Byte[128]         : P1_Ammo           [int]
        //Byte[132]         : P2_Ammo           [int]
        //Byte[136]         : P3_Ammo           [int]
        //Byte[140]         : P4_Ammo           [int]
        //Byte[144]         : P1_Credits        [int]
        //Byte[148]         : P2_Credits        [int]
        //Byte[152]         : P3_Credits        [int]
        //Byte[156]         : P4_Credits        [int]
        
        private Byte[] _bPayload;

        #region Payload Indexes
        public const int PAYLOAD_LENGTH = 200;

        public const int INDEX_P1_UISCREEN_X = 0;
        public const int INDEX_P1_UISCREEN_Y = 4;
        public const int INDEX_P1_INGAME_X = 8;
        public const int INDEX_P1_INGAME_Y = 12;

        public const int INDEX_P2_UISCREEN_X = 16;
        public const int INDEX_P2_UISCREEN_Y = 20;
        public const int INDEX_P2_INGAME_X = 24;
        public const int INDEX_P2_INGAME_Y = 28;

        public const int INDEX_P3_UISCREEN_X = 32;
        public const int INDEX_P3_UISCREEN_Y = 36;
        public const int INDEX_P3_INGAME_X = 40;
        public const int INDEX_P3_INGAME_Y = 44;

        public const int INDEX_P4_UISCREEN_X = 48;
        public const int INDEX_P4_UISCREEN_Y = 52;
        public const int INDEX_P4_INGAME_X = 56;
        public const int INDEX_P4_INGAME_Y = 60;

        public const int INDEX_P1_TRIGGER = 64;
        public const int INDEX_P2_TRIGGER = 68;
        public const int INDEX_P3_TRIGGER = 72;
        public const int INDEX_P4_TRIGGER = 76;

        public const int INDEX_P1_RELOAD = 80;
        public const int INDEX_P2_RELOAD = 84;
        public const int INDEX_P3_RELOAD = 88;
        public const int INDEX_P4_RELOAD = 92;

        public const int INDEX_P1_LIFE = 96;
        public const int INDEX_P2_LIFE = 100;
        public const int INDEX_P3_LIFE = 104;
        public const int INDEX_P4_LIFE = 108;

        public const int INDEX_P1_MOTOR = 112;
        public const int INDEX_P2_MOTOR = 116;
        public const int INDEX_P3_MOTOR = 120;
        public const int INDEX_P4_MOTOR = 124;

        public const int INDEX_P1_AMMO = 128;
        public const int INDEX_P2_AMMO = 132;
        public const int INDEX_P3_AMMO = 136;
        public const int INDEX_P4_AMMO = 140;

        public const int INDEX_P1_CREDITS = 144;
        public const int INDEX_P2_CREDITS = 148;
        public const int INDEX_P3_CREDITS = 152;
        public const int INDEX_P4_CREDITS = 156;

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


        public TRA_MemoryMappedFile_Controller(string MMF_Name, string Mutex_Name, long lngSize)
        {
            _sMmfName = MMF_Name;
            _sMutexName = Mutex_Name;

            if (lngSize <= 0 || lngSize > 0x00800000) lngSize = 0x00800000;
            _MemSize = lngSize;

            _bPayload = new Byte[PAYLOAD_LENGTH];
        }

        ~TRA_MemoryMappedFile_Controller()
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
