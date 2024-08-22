using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_WndHotdoPc : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\windows\hodo";

        /*** MEMORY ADDRESSES **/
        private UInt32 _PlayerStatus_Offset = 0x0059C378;
        private UInt32 _CreditsPtr_Offset = 0x005996C8;
        private UInt32 _PlayerInfoPtr_Offset = 0x00599688;

        //Play the "Coins" sound when adding coin
        //SoundPlayer _SndPlayer;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndHotdoPc(String RomName, bool DisableInputHack, bool Verbose) 
            : base (RomName, "HOTD_NG", DisableInputHack, Verbose)
        {                      
            _KnownMd5Prints.Add("Typing Of The Dead SEGA Windows", "da39156a426e3f3faca25d3c8cb2b401");
            _KnownMd5Prints.Add("Typing Of The Dead SEGA Windows STEAM", "9dcb7083e3e3ede186c9a809498a0d3b");

            _tProcess.Start();            
            Logger.WriteLog("Waiting for Windows Game " + _RomName + " game to hook.....");
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
                            _GameWindowHandle = _TargetProcess.MainWindowHandle;
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();
                            ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);

                            _ProcessHooked = true;                            
                            RaiseGameHookedEvent();
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

        #region Inputs

        /// <summary>
        /// Low-level Keyboard hook callback.
        /// This is used to add coin to the game
        /// </summary>
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    if (s.scanCode ==  HardwareScanCode.DIK_5)
                    {
                        UInt32 Credits_Address = ReadPtrChain((UInt32)_TargetProcess_MemoryBaseAddress + _CreditsPtr_Offset, new UInt32[] { 0x228 }) + 0x17C;// ReadByte(_Credits_Address);
                        byte Credits = ReadByte(Credits_Address);
                        Credits++;
                        WriteByte(Credits_Address, Credits);                        
                    }
                }
            }
            return Win32API.CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Player status :
            //[0] = Not Playing
            //[1] = Playing
            UInt32 P1_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _PlayerStatus_Offset);

            UInt32 iTemp = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + _PlayerInfoPtr_Offset);
            UInt32 PlayerInfo = 0;
            //Player nfo struct is created and allocated on game, point to NULL in menus
            if (iTemp != 0)
            {
                PlayerInfo = ReadPtr(iTemp + 0x24);
                _P1_Life = ReadByte(PlayerInfo + 0x20C);
                _P1_Ammo = ReadByte(PlayerInfo + 0x28C);
            }            

            if (P1_Status  != 0)
            {
                //Custom Recoil
                if (_P1_Ammo < _P1_LastAmmo)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                //[Clip Empty] custom Output
                if (_P1_Ammo <= 0)
                    SetOutputValue(OutputId.P1_Clip, 0);
                else
                    SetOutputValue(OutputId.P1_Clip, 1);

                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }
            else
            {
                SetOutputValue(OutputId.P1_Clip, 0);
            }

            _P1_LastAmmo = _P1_Ammo;
            _P1_LastLife = _P1_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.Credits, ReadByte(ReadPtrChain((UInt32)_TargetProcess_MemoryBaseAddress + _CreditsPtr_Offset, new UInt32[] { 0x228 }) + 0x17C));
        }

        #endregion
    }
}
