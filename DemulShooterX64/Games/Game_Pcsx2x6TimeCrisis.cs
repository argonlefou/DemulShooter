using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;

namespace DemulShooterX64.Games
{
    /// <summary>
    /// Time Crisis 3 (Namco System 246) and Time Crisis 4 (Namco System 256) in PCSX2x6, the arcade PCSX2
    /// fork that TeknoParrot runs them with.
    /// Outputs only : the emulator reads the guns (TeknoParrot JVS light gun mode), so use -noinput.
    /// The emulated EE memory is found through the "EEmem" pointer that PCSX2 exports for debuggers, so no
    /// emulator build is hard-coded. The game is recognized by a few code bytes before outputs are read.
    /// </summary>
    public class Game_Pcsx2x6TimeCrisis : Game
    {
        private const String PCSX2_AVX2_PROCESS_NAME = "pcsx2-qtx64-avx2";
        private const String PCSX2_PROCESS_NAME = "pcsx2-qtx64";

        /*** MEMORY ADDRESSES (EE RAM offsets) **/
        //Check bytes for game loaded (start of the player update routine)
        private UInt32 _RomLoaded_Check_Offset;
        private byte[] _RomLoaded_Check_Bytes;

        //In-game flag : holds _InGame_Value while a game is in progress, other values in attract mode (whose demos
        //shoot and lose lives too)
        private UInt32 _InGame_Offset;
        private int _InGame_Value;

        //Player 1 data : 32-bit values. Ammo is one count per weapon, indexed by the current weapon
        private UInt32 _P1_Life_Offset;
        private UInt32 _P1_Weapon_Offset;
        private UInt32 _P1_Ammo_Offset;
        private int _WeaponCount;

        private IntPtr _EEmem_Variable = IntPtr.Zero;

        //Custom Outputs
        private int _P1_LastWeapon = -1;
        private bool _LastInGame = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Pcsx2x6TimeCrisis(String RomName) : base(RomName, PCSX2_AVX2_PROCESS_NAME)
        {
            switch (_RomName)
            {
                case "tc3":
                    {
                        //Time Crisis 3 (TST2 Ver.A, World)
                        _RomLoaded_Check_Offset = 0x0015B150;
                        _RomLoaded_Check_Bytes = new byte[] { 0x80, 0xFF, 0xBD, 0x27, 0x70, 0x00, 0xBF, 0x7F, 0x60, 0x00, 0xB6, 0x7F, 0x50, 0x00, 0xB5, 0x7F };
                        _InGame_Offset = 0x0037A138;     //1 from the credit to the game over
                        _InGame_Value = 1;
                        _P1_Life_Offset = 0x0187CEC0;
                        _P1_Weapon_Offset = 0x0187D030;
                        _P1_Ammo_Offset = 0x0187D034;
                        _WeaponCount = 4;
                    } break;
                case "tc4":
                    {
                        //Time Crisis 4 (TSF1002-NA-A, World)
                        _RomLoaded_Check_Offset = 0x0027BF80;
                        _RomLoaded_Check_Bytes = new byte[] { 0xE0, 0xFE, 0xBD, 0x27, 0xA0, 0x00, 0xBF, 0xFF, 0x90, 0x00, 0xBE, 0x7F, 0x80, 0x00, 0xB7, 0x7F };
                        _InGame_Offset = 0x00C1EF58;     //3 while playing, 0 on the credit screen and in attract mode
                        _InGame_Value = 3;
                        _P1_Life_Offset = 0x00A6D854;
                        _P1_Weapon_Offset = 0x00A6D9E4;
                        _P1_Ammo_Offset = 0x00A6D9C4;
                        _WeaponCount = 4;
                    } break;
            }
            //Builds it was tested with (the EEmem export makes other builds work too)
            _KnownMd5Prints.Add("PCSX2x6 v0.0.22-24-gc6e731ac0 AVX2 (TeknoParrot)", "15764e7ac3eb18d258a24673cdbf5b69");
            _KnownMd5Prints.Add("PCSX2x6 v0.0.22-24-gc6e731ac0 (TeknoParrot)", "75deb06c6ff91daea4c965805ac493a8");
            _tProcess.Start();
            Logger.WriteLog("Waiting for PCSX2x6 " + _RomName + " game to hook.....");
        }

        /// <summary>
        /// Timer event when looking for Process (auto-Hook and auto-close)
        /// PCSX2x6 comes as an AVX2 build and a plain one : either is hooked unless -pname is given
        /// </summary>
        protected override void tProcess_Elapsed(Object Sender, EventArgs e)
        {
            if (!_ProcessHooked)
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName(_Target_Process_Name);
                    if (processes.Length == 0 && _CustomTargetProcessName == String.Empty)
                    {
                        processes = Process.GetProcessesByName(PCSX2_PROCESS_NAME);
                        if (processes.Length > 0)
                            _Target_Process_Name = PCSX2_PROCESS_NAME;
                    }
                    if (processes.Length > 0)
                    {
                        _TargetProcess = processes[0];
                        _ProcessHandle = _TargetProcess.Handle;
                        _TargetProcess_MemoryBaseAddress = _TargetProcess.MainModule.BaseAddress;

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            if (_EEmem_Variable == IntPtr.Zero)
                            {
                                _EEmem_Variable = FindExport("EEmem");
                                if (_EEmem_Variable == IntPtr.Zero)
                                {
                                    Logger.WriteLog("EEmem export not found in " + _Target_Process_Name + ".exe");
                                    return;
                                }
                                Logger.WriteLog("EEmem pointer at 0x" + _EEmem_Variable.ToString("X16"));
                            }

                            UInt64 EEmem = ReadPtr(_EEmem_Variable);
                            if (EEmem != 0 && CheckBytes((IntPtr)(EEmem + _RomLoaded_Check_Offset), _RomLoaded_Check_Bytes))
                            {
                                _GameWindowHandle = _TargetProcess.MainWindowHandle;
                                Logger.WriteLog("EE memory at 0x" + EEmem.ToString("X16"));
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
                                Logger.WriteLog("MainWindowHandle = 0x" + _TargetProcess.MainWindowHandle.ToString("X16"));
                                Logger.WriteLog("MainWindowTitle" + _TargetProcess.MainWindowTitle);
                                CheckExeMd5();
                                if (!_DisableInputHack)
                                    Logger.WriteLog("Inputs are read by the emulator, run with -noinput");
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

        /// <summary>
        /// Address of an exported variable of the emulator, from the export table of its loaded image
        /// </summary>
        private IntPtr FindExport(String Name)
        {
            UInt64 Module = (UInt64)_TargetProcess_MemoryBaseAddress;
            byte[] Header = ReadBytes((IntPtr)Module, 0x400);
            int PeOffset = BitConverter.ToInt32(Header, 0x3C);
            UInt32 ExportRva = BitConverter.ToUInt32(Header, PeOffset + 0x88);    //PE32+ data directory 0
            if (ExportRva == 0)
                return IntPtr.Zero;

            byte[] Directory = ReadBytes((IntPtr)(Module + ExportRva), 40);
            int NamesCount = BitConverter.ToInt32(Directory, 24);
            UInt32 FunctionsRva = BitConverter.ToUInt32(Directory, 28);
            UInt32 NamesRva = BitConverter.ToUInt32(Directory, 32);
            UInt32 OrdinalsRva = BitConverter.ToUInt32(Directory, 36);
            byte[] Names = ReadBytes((IntPtr)(Module + NamesRva), (UInt32)NamesCount * 4);
            byte[] Ordinals = ReadBytes((IntPtr)(Module + OrdinalsRva), (UInt32)NamesCount * 2);
            for (int i = 0; i < NamesCount; i++)
            {
                byte[] Str = ReadBytes((IntPtr)(Module + BitConverter.ToUInt32(Names, 4 * i)), (UInt32)Name.Length + 1);
                if (Encoding.ASCII.GetString(Str, 0, Name.Length) == Name && Str[Name.Length] == 0)
                {
                    UInt16 Ordinal = BitConverter.ToUInt16(Ordinals, 2 * i);
                    UInt32 VariableRva = BitConverter.ToUInt32(ReadBytes((IntPtr)(Module + FunctionsRva + 4 * (UInt32)Ordinal), 4), 0);
                    return (IntPtr)(Module + VariableRva);
                }
            }
            return IntPtr.Zero;
        }

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputId.P1_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputId.P1_Life));
            _Outputs.Add(new AsyncGameOutput(OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// The game has no recoil output of its own that is visible here, so recoil is computed from the ammo
        /// count of the weapon in hand : every bullet fired (handgun, machine gun, shotgun or grenade) lowers it.
        /// A life lost lowers the life count by one. Bigger drops are not events (the game resetting the player
        /// data), rises are reloads, pickups and new games. Attract mode demos shoot and lose lives too, so
        /// events only count while a game is in progress (or was at the previous read, for the last life)
        /// </summary>
        public override void UpdateOutputValues()
        {
            UInt64 EEmem = ReadPtr(_EEmem_Variable);
            if (EEmem == 0)
                return;

            bool InGame = ReadInt32(EEmem + _InGame_Offset) == _InGame_Value;
            int P1_Life = ReadInt32(EEmem + _P1_Life_Offset);
            int P1_Weapon = ReadInt32(EEmem + _P1_Weapon_Offset);
            int P1_Ammo = 0;
            if (P1_Weapon >= 0 && P1_Weapon < _WeaponCount)
                P1_Ammo = ReadInt32(EEmem + _P1_Ammo_Offset + 4 * (UInt32)P1_Weapon);

            if (InGame || _LastInGame)
            {
                //[Recoil] custom Output : a few bullets at most between two reads (machine gun)
                if (P1_Weapon == _P1_LastWeapon && P1_Ammo < _P1_LastAmmo && _P1_LastAmmo - P1_Ammo <= 3)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Damaged] custom Output
                if (P1_Life == _P1_LastLife - 1)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }

            _P1_LastAmmo = P1_Ammo;
            _P1_LastLife = P1_Life;
            _P1_LastWeapon = P1_Weapon;
            _LastInGame = InGame;

            if (!InGame)
            {
                P1_Life = 0;
                P1_Ammo = 0;
            }
            SetOutputValue(OutputId.P1_Ammo, P1_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Ammo > 0 ? 1 : 0);
            SetOutputValue(OutputId.P1_Life, P1_Life);
        }

        private int ReadInt32(UInt64 Address)
        {
            return BitConverter.ToInt32(ReadBytes((IntPtr)Address, 4), 0);
        }

        #endregion
    }
}
