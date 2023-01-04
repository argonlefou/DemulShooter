using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace DsCore.IPC
{
    public class MMFH_HotdRemakeArcade : MappedMemoryFileHelper
    {
        private const String MAPPED_FILE_NAME = "DemulShooter_MMF_Hotdra";
        private const String MUTEX_NAME = "DemulShooter_Mutex_Hotdra";
        private const long MAPPED_FILE_CAPACITY = 2048;

        //Shared Memory Payload between DemulSHooter and the game, structured as followed:
        // Demulshooter --> Game :
        //Byte[0]-Byte[3]   : P1_X      [0-WindowWidth]
        //Byte[4]-Byte[7]   : P1_Y      [0-WindowHeight]
        //Byte[8]-Byte[11]  : P2_X      [0-WindowWidth]
        //Byte[12]-Byte[15] : P2_Y      [0-WindowHeight]
        //Byte[16]          : P1_Trigger        [0-1-2]
        //Byte[17]          : P2_Trigger        [0-1-2]
        //Byte[18]          : P1_Reload         [0-1]
        //Byte[19]          : P2_Reload         [0-1]
        // Game -> DemulShooter :
        //Byte[20]         : P1_Life           [int]
        //Byte[21]         : P2_Life           [int]
        //Byte[22]         : P1_Ammo           [int]
        //Byte[23]         : P2_Ammo           [int]
        //Byte[24]         : P1_Recoil         [0-1]
        //Byte[25]         : P2_Recoil         [0-1]
        //Byte[26]         : P1_Damaged        [int]
        //Byte[27]         : P2_Damaged        [int]
        //Byte[28]         : Credits           [int]

        private Byte[] _bPayload;

        #region Payload Indexes
        public const int PAYLOAD_LENGTH = 30;
        public const int PAYLOAD_INPUT_LENGTH = 20;
        public const int PAYLOAD_OUTPUTS_LENGTH = 9;

        public enum Payload_Inputs_Index
        {
            P1_AxisX = 0,
            P1_AxisY = 4,
            P2_AxisX = 8,
            P2_AxisY = 12,
            P1_Trigger = 16,
            P2_Trigger,
            P1_Reload,
            P2_Reload            
        }

        public enum Payload_Outputs_Index
        {
            P1_Life = PAYLOAD_INPUT_LENGTH,
            P2_Life,
            P1_Ammo,
            P2_Ammo,
            P1_Recoil,
            P2_Recoil,
            P1_Damaged,
            P2_Damaged,
            Credits
        }

        #endregion

        public Byte[] Payload
        {
            get { return _bPayload; }
            set { _bPayload = value; }
        }

        public MMFH_HotdRemakeArcade()
            : base(MAPPED_FILE_NAME, MUTEX_NAME, MAPPED_FILE_CAPACITY)
        {
            _bPayload = new Byte[PAYLOAD_LENGTH];
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

        /// <summary>
        /// Write all Payload in one hit
        /// </summary>
        /// <returns></returns>
        public int Writeall()
        {
            return WriteByteArray(_bPayload, 0, PAYLOAD_LENGTH);
        }

        //Writing the input part of the shared memory
        public int WriteInputs()
        {
            return WriteByteArray(_bPayload, 0, PAYLOAD_INPUT_LENGTH);
        }
    }
}
