using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace DsCore.IPC
{
    public class MMFH_RabbidsHollywood : MappedMemoryFileHelper
    {
        private const String MAPPED_FILE_NAME = "DemulShooter_MMF_Rha";
        private const String MUTEX_NAME = "DemulShooter_Mutex_Rha";
        private const long MAPPED_FILE_CAPACITY = 2048;

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
        // Game -> DemulShooter :
        //Byte[80]          : P1_Life           [Float]
        //Byte[84]          : P2_Life           [Float]
        //Byte[88]          : P3_Life           [Float]
        //Byte[92]          : P4_Life           [Float]
        //Byte[96]          : P1_Motor          [Float]
        //Byte[100]         : P2_Motor          [Float]
        //Byte[104]         : P3_Motor          [Float]
        //Byte[108]         : P4_Motor          [Float]
        //Byte[112]         : P1_Credits        [int]
        //Byte[116]         : P2_Credits        [int]
        //Byte[120]         : P3_Credits        [int]
        //Byte[124]         : P4_Credits        [int]

        private Byte[] _bPayload;

        #region Payload Indexes
        public const int PAYLOAD_LENGTH = 130;

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

        public const int INDEX_P1_LIFE = 80;
        public const int INDEX_P2_LIFE = 84;
        public const int INDEX_P3_LIFE = 88;
        public const int INDEX_P4_LIFE = 92;

        public const int INDEX_P1_MOTOR = 96;
        public const int INDEX_P2_MOTOR = 100;
        public const int INDEX_P3_MOTOR = 104;
        public const int INDEX_P4_MOTOR = 108;

        public const int INDEX_P1_CREDITS = 112;
        public const int INDEX_P2_CREDITS = 116;
        public const int INDEX_P3_CREDITS = 120;
        public const int INDEX_P4_CREDITS = 124;

        #endregion

        public Byte[] Payload
        {
            get { return _bPayload; }
            set { _bPayload = value; }
        }

        public MMFH_RabbidsHollywood() : base(MAPPED_FILE_NAME, MUTEX_NAME, MAPPED_FILE_CAPACITY)
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
            return WriteByteArray(_bPayload, 0, 80);
        }
    }
}
