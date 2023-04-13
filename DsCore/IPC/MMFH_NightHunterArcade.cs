using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace DsCore.IPC
{
    public class MMFH_NightHunterArcade : MappedMemoryFileHelper
    {
        private const String MAPPED_FILE_NAME = "DemulShooter_MMF_Nha2";
        private const String MUTEX_NAME = "DemulShooter_Mutex_Nha2";
        private const long MAPPED_FILE_CAPACITY = 2048;

        //Shared Memory Payload between DemulSHooter and the game, structured as followed:
        // Demulshooter --> Game :
        //Byte[0]           : P1_InGameX        [0-WindowX]
        //Byte[4]           : P1_InGameY        [0-WindowY]
        //Byte[8]           : P2_InGameX        [0-WindowX]
        //Byte[12]          : P2_InGameY        [0-WindowY]
        //Byte[16]          : P1_Trigger        [0-1]
        //Byte[20]          : P2_Trigger        [0-1]
        //Byte[24]          : P1_ChangeWeapon   [0-1]
        //Byte[28]          : P2_ChangeWeapon   [0-1]
        //Byte[32]          : P1_Special        [0-1]
        //Byte[36]          : P2_Special        [0-1]

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

        public MMFH_NightHunterArcade()
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
            return WriteByteArray(_bPayload, 0, 40);
        }
    }
}
