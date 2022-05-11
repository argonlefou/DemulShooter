using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace DsCore.IPC
{
    public class MMFH_WildWestShoutout : MappedMemoryFileHelper
    {
        private const String MAPPED_FILE_NAME = "DemulShooter_MMF_Wws";
        private const String MUTEX_NAME = "DemulShooter_Mutex_Wws";
        private const long MAPPED_FILE_CAPACITY = 2048;

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
        //Byte[36]-Byte[39] : CreditsP1         [0 - ??]
        //Byte[40]-Byte[43] : CreditsP2         [0 - ??]
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

        public const int INDEX_P2_UISCREEN_X = 16;
        public const int INDEX_P2_UISCREEN_Y = 20;
        public const int INDEX_P2_INGAME_X = 24;
        public const int INDEX_P2_INGAME_Y = 28;

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
            set { _bPayload = value; }
        }

        public MMFH_WildWestShoutout() : base(MAPPED_FILE_NAME, MUTEX_NAME, MAPPED_FILE_CAPACITY)
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
    }
}
