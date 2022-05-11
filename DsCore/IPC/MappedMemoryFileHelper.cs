using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DsCore.Win32;
using System.Runtime.InteropServices;

namespace DsCore.IPC
{
    public class MappedMemoryFileHelper
    {
        protected IntPtr _hSharedMemoryFile = IntPtr.Zero;
        protected String _sMmfName = String.Empty;
        protected bool _bMmfAlreadyExist = false;
        protected long _MemSize = 0;

        protected Mutex _Mutex;
        protected String _sMutexName = String.Empty;
        protected bool _IsMutexCreated;

        protected IntPtr _pwData = IntPtr.Zero;
        protected bool _bInit = false;

        public bool IsOpened
        {
            get { return _bInit; }
        }

        public bool IsMutexCreated
        {
            get { return _IsMutexCreated; }
        }

        public MappedMemoryFileHelper(string MMF_Name, string Mutex_Name, long lngSize)
        {
            _sMmfName = MMF_Name;
            _sMutexName = Mutex_Name;

            if (lngSize <= 0 || lngSize > 0x00800000) lngSize = 0x00800000;
                _MemSize = lngSize;
        }

        ~MappedMemoryFileHelper()
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
                _hSharedMemoryFile = Win32API.CreateFileMapping(Win32Define.INVALID_HANDLE_VALUE, IntPtr.Zero, PageProtection.ReadWrite, 0, (uint)_MemSize, _sMmfName);
                if (_hSharedMemoryFile == IntPtr.Zero)
                {
                    _bMmfAlreadyExist = false;
                    _bInit = false;
                    return 2; // Failed to create shared body					                 
                }
                else
                {
                    if (Marshal.GetLastWin32Error() == Win32Define.ERROR_ALREADY_EXISTS)  // Already Created   
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
                _pwData = Win32API.MapViewOfFile(_hSharedMemoryFile, FileMapAccessType.Write, 0, 0, (uint)_MemSize);
                if (_pwData == IntPtr.Zero)
                {
                    _bInit = false;
                    Win32API.CloseHandle(_hSharedMemoryFile);
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
        ///  Writing data         
        /// </summary>        
        /// <param name="bytData"> data </param>        
        /// <param name="lngAddr"> Initial address </param>        
        /// <param name="lngSize"> Number </param>        
        /// <returns></returns>        
        public int WriteByteArray(byte[] bytData, int lngAddr, int lngSize)
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
                Win32API.UnmapViewOfFile(_pwData);
                Win32API.CloseHandle(_hSharedMemoryFile);
            }
        }
    
    }
}
