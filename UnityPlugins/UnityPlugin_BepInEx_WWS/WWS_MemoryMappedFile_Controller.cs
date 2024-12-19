using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace UnityPlugin_BepInEx_WWS
{
    public class WWS_MemoryMappedFile_Controller
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
        //Byte[0]-Byte[3]   : P1_UIScreenX      [0-800]
        //Byte[4]-Byte[7]   : P1_UIScreenY      [0-600]
        //Byte[8]-Byte[11]  : P1_InGameX        [0-1920]
        //Byte[12]-Byte[15] : P1_InGameY        [0-xxx]
        //Byte[16]-Byte[19] : P2_UIScreenX      [0-800]
        //Byte[20]-Byte[23] : P2_UIScreenY      [0-600]
        //Byte[24]-Byte[27] : P2_InGameX        [0-1920]
        //Byte[28]-Byte[31] : P2_InGameY        [0-xxx]
        //Byte[32]          : P1_Trigger        [0 / 1 / 2]
        //Byte[33]          : P2_Trigger        [0 / 1 / 2]
        //Byte[34]          : TEST              [0 / 1]
        //Byte[36]-Byte[39] : P1_COIN           [0 - ??]
        //Byte[40]-Byte[43] : P2_COIN           [0 - ??]
        //Byte[44]          : P1_Reload         [0 / 1]
        //Byte[45]          : P2_Reload         [0 / 1]
        // Game -> DemulShooter :
        //Byte[50]          : GunOpenedP1       [0 - 1]
        //Byte[51]          : GunTestedP1       [0 - 1]
        //Byte[52]          : GunStateP1        [0 - 1]
        //Byte[53]          : GunsignalP1       [0 - 1]
        //Byte[54]          : GunOpenedP2       [0 - 1]
        //Byte[55]          : GunTestedP2       [0 - 1]
        //Byte[56]          : GunStateP2        [0 - 1]
        //Byte[57]          : GunsignalP2       [0 - 1]
        //Byte[60]-Byte[63] : P1_Credits        
        //Byte[64]-Byte[67] : P2_Credits     
        //Byte[68]          : P1_Life
        //Byte[69]          : P2_Life
        //Byte[70]          : P1_Ammo
        //Byte[71]          : P2_Ammo
        //Byte[72]          : P1_Recoil
        //Byte[73]          : P2_Recoil
        //Byte[74]-Byte[77] : ViewPort_width
        //Byte[78]-Byte[81] : ViewPort_Height

        private Byte[] _bPayload;

        #region Payload Indexes
        public const int PAYLOAD_LENGTH = 82;

        public const int INDEX_P1_UISCREEN_X = 0;
        public const int INDEX_P1_UISCREEN_Y = 4;
        public const int INDEX_P1_INGAME_X = 8;
        public const int INDEX_P1_INGAME_Y = 12;

        public const int INDEX_P1_TRIGGER = 32;
        public const int INDEX_P2_TRIGGER = 33;
        public const int INDEX_TEST = 34;

        public const int INDEX_P1_COIN = 36;
        public const int INDEX_P2_COIN = 40;

        public const int INDEX_P1_RELOAD = 44;
        public const int INDEX_P2_RELOAD = 45;

        public const int INDEX_P1_GUNOPEN = 50;
        public const int INDEX_P1_GUNTEST = 51;
        public const int INDEX_P1_GUNSTATE = 52;
        public const int INDEX_P1_GUNSIGNAL = 53;
        public const int INDEX_P2_GUNOPEN = 54;
        public const int INDEX_P2_GUNTEST = 55;
        public const int INDEX_P2_GUNSTATE = 56;
        public const int INDEX_P2_GUNSIGNAL = 57;

        public const int INDEX_P1_CREDITS = 60;
        public const int INDEX_P2_CREDITS = 64;
        public const int INDEX_P1_LIFE = 68;
        public const int INDEX_P2_LIFE = 69;
        public const int INDEX_P1_AMMO = 70;
        public const int INDEX_P2_AMMO = 71;

        public const int INDEX_P1_RECOIL = 72;
        public const int INDEX_P2_RECOIL = 73;

        public const int INDEX_VIEWPORT_WIDTH = 74;
        public const int INDEX_VIEWPORT_HEIGHT = 78;
        #endregion

        public Byte[] Payload
        {
            get { return _bPayload; }
            set {_bPayload = value; }
        }

        public bool IsOpened
        {
            get { return _bInit; }
        }


        public WWS_MemoryMappedFile_Controller(string MMF_Name, string Mutex_Name, long lngSize)
        {
            _sMmfName = MMF_Name;
            _sMutexName = Mutex_Name;

            if (lngSize <= 0 || lngSize > 0x00800000) lngSize = 0x00800000;
                _MemSize = lngSize;

            _bPayload = new Byte[PAYLOAD_LENGTH];
        }

        ~WWS_MemoryMappedFile_Controller()
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
