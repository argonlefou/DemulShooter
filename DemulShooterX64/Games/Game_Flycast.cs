using System;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;

namespace DemulShooterX64
{
    public class Game_Flycast : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\flycast";

        /* 
         * Output status with derived class : 
         * Ninja Assault (ninjaslt, ninjaslta, ninjasltj, ninjasltu) => working. Need better filter for player playing (force life = 0 at start ?)
         * Naomi games :
         *  - confmiss => working
         *  - deathcox => working. Need better filter for player playing
         *  - hotd2, hotd2o => working
         *  - hotd2p => no controls on game ?
         *  - hotd2e => need to find memory values
         *  - lupinsho => working
         * Pokasuka ghost / Manic Panic ghost => I/O emulation not done (time out error)
         * Atomiswave : no outputs
         * 
         */


        //Output values
        protected UInt64 _GameRAMPtr_Offset = 0x2087490;
        protected UInt64 _GameRAM_Address = 0;

        public Game_Flycast(String RomName) : 
            base(RomName, "flycast")
        {
            _KnownMd5Prints.Add("Flycast v2.0", "84b08b9aa61d8c46ff47abcc77f690f7");
            _KnownMd5Prints.Add("Flycast v2.1", "cf56b386e1a9e82f5a92f8aadb2b6df9");
            _KnownMd5Prints.Add("Flycast v2.2", "1fa2952ada82345ae743bc9110c6dbec");
            _KnownMd5Prints.Add("Flycast v2.3", "d5e819796226078c6ce51dc8f304550b");
            _KnownMd5Prints.Add("Flycast v2.4", "3ac610caf584b9891de6b8a8924862e5");
            _KnownMd5Prints.Add("Flycast v2.5", "c978b832b137ced294a13948a2115d37");
            _tProcess.Start();
            Logger.WriteLog("Waiting for Flycast " + _RomName + " game to hook.....");
        }

        /// <summary>
        /// Timer event when looking for Process (auto-Hook and auto-close)
        /// </summary>
        protected override void tProcess_Elapsed(Object Sender, EventArgs e)
        {
            if (!_ProcessHooked)
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName(_Target_Process_Name);
                    if (processes.Length > 0)
                    {
                        _TargetProcess = processes[0];
                        _ProcessHandle = _TargetProcess.Handle;
                        _TargetProcess_MemoryBaseAddress = _TargetProcess.MainModule.BaseAddress;

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            CheckExeMd5();
                            ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);

                            _GameRAM_Address = BitConverter.ToUInt64(ReadBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _GameRAMPtr_Offset), 8), 0);

                            if (_GameRAM_Address != 0)
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
                                Logger.WriteLog("MainWindowHandle = 0x" + _TargetProcess.MainWindowHandle.ToString("X16"));
                                Logger.WriteLog("MainWindowTitle" + _TargetProcess.MainWindowTitle);
                                Logger.WriteLog("Game RAM loaded at 0x" + _GameRAM_Address.ToString("X16"));                                

                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
                            }
                            else
                            {
                                Logger.WriteLog("ROM not Loaded...");
                            }
                        }
                    }
                }
                catch
                {
                    Logger.WriteLog("Error trying to hook " + _Target_Process_Name + ".exe");
                }
            }
            else
            {
                Process[] processes = Process.GetProcessesByName(_Target_Process_Name);
                if (processes.Length <= 0)
                {
                    _ProcessHooked = false;
                    _TargetProcess = null;
                    _ProcessHandle = IntPtr.Zero;
                    _TargetProcess_MemoryBaseAddress = IntPtr.Zero;
                    Logger.WriteLog(_Target_Process_Name + ".exe closed");
                    Application.Exit();
                }
            }
        }
    }
}
