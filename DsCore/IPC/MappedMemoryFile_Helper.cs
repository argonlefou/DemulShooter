using System;
using DsCore.Win32;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;

namespace DsCore.IPC
{
    public class MemoryMappedFileHelper
    {
        private IntPtr _hSharedMemoryFile = IntPtr.Zero;
        private IntPtr _pwData = IntPtr.Zero;
        private bool _bAlreadyExist = false;
        private bool _bInit = false;
        private long _MemSize = 0;
        private String _sName = String.Empty;

        private Mutex _Mutex;
        private bool _IsMutexCreated;

        private MMF_DataStruct[] _mmfData;

        //Total length of data exported :
        //For each Player : 5 * 4 Bits fields (UInt32 / Int32)
        //4 Players = 80 Bytes

        private const int DATA_LENGTH_TOTAL = 80;

        #region Accessors

        public bool IsNewCreation
        {
            get { return !_bAlreadyExist; }
        }
        public bool IsAlreadyExisting
        {
            get { return _bAlreadyExist; }
        }
        public bool IsAvailable
        {
            get { return _bInit; }
        }
        public String MemoryFileName
        {
            get { return _sName; }
        }        

        #endregion

        public MemoryMappedFileHelper(String strMutexName)
        {
            _mmfData = new MMF_DataStruct[4];
            for (int i = 0; i < 4; i++)
            {
                _mmfData[i] = new MMF_DataStruct();
            }

            if (strMutexName.Length > 0)
                _Mutex = new Mutex(false, strMutexName, out _IsMutexCreated); //false to not take the mutex at creation !!                
        }

        ~MemoryMappedFileHelper()
        {
            MMFClose();
        }

        /// <summary>        
        ///  Initialize shared memory         
        /// </summary>        
        /// <param name="strName"> Shared memory name </param>        
        /// <param name="lngSize"> Shared memory size </param>        
        /// <returns></returns>        
        public int MMFInit(string strName, long lngSize)
        {
            _sName = strName;
            if (lngSize <= 0 || lngSize > 0x00800000) lngSize = 0x00800000;
            _MemSize = lngSize;
            if (strName.Length > 0)
            {
                // Create a memory share (INVALID_HANDLE_VALUE)                
                _hSharedMemoryFile = Win32API.CreateFileMapping(Win32Define.INVALID_HANDLE_VALUE, IntPtr.Zero, PageProtection.ReadWrite, 0, (uint)lngSize, strName);
                if (_hSharedMemoryFile == IntPtr.Zero)
                {
                    _bAlreadyExist = false;
                    _bInit = false;
                    Logger.WriteLog("CreateFileMapping() error for mapped file : '" + strName + "'");
                    return 2; // Failed to create shared body					                 
                }
                else
                {
                    if (Marshal.GetLastWin32Error() == Win32Define.ERROR_ALREADY_EXISTS)  // Already Created   
                    {
                        _bAlreadyExist = true;
                        Logger.WriteLog("Successfully opened already existing mapped file '" + strName + "'");
                    }
                    else
                    {
                        _bAlreadyExist = false; // New creation 
                        Logger.WriteLog("Successfully created new mapped file '" + strName + "'");
                    }
                }
                //---------------------------------------                
                // Create memory maps              
                //FileMapAccessType.AllAccess gives Memory protected ERROR !!!
                _pwData = Win32API.MapViewOfFile(_hSharedMemoryFile, FileMapAccessType.Write, 0, 0, (uint)lngSize);
                if (_pwData == IntPtr.Zero)
                {
                    _bInit = false;
                    Win32API.CloseHandle(_hSharedMemoryFile);
                    Logger.WriteLog("Failed to create memory map '" + strName + "'");
                    return 3; // Failed to create memory map						                 
                }
                else
                {
                    _bInit = true;
                    if (_bAlreadyExist == false)
                    {
                        // initialization                     
                    }
                }
                //----------------------------------------            
            }
            else
            {
                return 1; // Parameter error 					                
            }
            Logger.WriteLog("Memory map '" + strName + "' succesfully initialized");
            return 0; // Create success         
        }

        /// <summary>       
        ///  Turn off shared memory         
        /// </summary>        
        public void MMFClose()
        {
            if (_bInit)
            {
                Win32API.UnmapViewOfFile(_pwData);
                Win32API.CloseHandle(_hSharedMemoryFile);
                Logger.WriteLog("Memory map '" + _sName + "' succesfully closed");
            }
        }
        
        /// <summary>
        /// Write Full 4-Players data to mapped memory file
        /// </summary>
        /// <returns></returns>
        public int WriteData()
        {
            int wIndex = 0;
            byte[] bData = new byte[DATA_LENGTH_TOTAL];
            for (int i = 0; i < _mmfData.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(_mmfData[i].RawValue_X), 0, bData, wIndex, 4);
                wIndex += 4;
                Array.Copy(BitConverter.GetBytes(_mmfData[i].RawValue_Y), 0, bData, wIndex, 4);
                wIndex += 4;
                Array.Copy(BitConverter.GetBytes(_mmfData[i].ComputedValue_X), 0, bData, wIndex, 4);
                wIndex += 4;
                Array.Copy(BitConverter.GetBytes(_mmfData[i].ComputedValue_Y), 0, bData, wIndex, 4);
                wIndex += 4;
                Array.Copy(BitConverter.GetBytes(_mmfData[i].ComputedButtonEvent), 0, bData, wIndex, 4);
                wIndex += 4;
            }
            return WriteByteArray(bData, 0, DATA_LENGTH_TOTAL);  
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
                catch (Exception ex)
                {
                    Logger.WriteLog("MappedMemoryFile_Helper.WriteByteArray() error : " + ex.Message.ToString());
                }
            }
            else
            {
                return 1; // Shared memory not initialized             
            }
            return 0; // Write a successful         
        }

        /// <summary>        
        ///  Reading data         
        /// </summary>        
        /// <param name="bytData"> data </param>        
        /// <param name="lngAddr"> Initial address </param>        
        /// <param name="lngSize"> Number </param>        
        /// <returns></returns>        
        private int ReadByteArray(ref byte[] bytData, int lngAddr, int lngSize)
        {
            if (lngAddr + lngSize > _MemSize)
                return 2; // Beyond data area 

            if (_bInit)
            {
                Marshal.Copy(_pwData, bytData, lngAddr, lngSize);
            }
            else
            {
                return 1; // Shared memory not initialized             
            }
            return 0; // Read successfully         
        }

        public void UpdateRawPlayerData(int PlayerID, UInt32 NewX, UInt32 NewY)
        {
            if (_mmfData != null)
                _mmfData[PlayerID -1].UpdateRawValues(NewX, NewY);
        }

        public void UpdateComputedPlayerData(int PlayerID, Int32 NewX, Int32 NewY, bool[] NewButtons)
        {
            if (_mmfData != null)
                _mmfData[PlayerID -1].UpdateComputedValues(NewX, NewY, NewButtons);
        }
    }
}
