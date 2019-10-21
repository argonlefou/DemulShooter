using System;
using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DemulShooter
{
    class Game_HotdoPc : Game
    {

        /*** MEMORY ADDRESSES **/
        protected int _Credits_Address = 0x2F9D5B3C;

        //Play the "Coins" sound when adding coin
        SoundPlayer _SndPlayer;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_HotdoPc(string RomName, bool Verbose) 
            : base ()
        {                      
            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "HOTD_NG";
            _KnownMd5Prints.Add("Typing Of The Dead SEGA Windows", "da39156a426e3f3faca25d3c8cb2b401");

            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();
            
            WriteLog("Waiting for Windows Game " + _RomName + " game to hook.....");
        }

        /// <summary>
        /// Timer event when looking for Process (auto-Hook and auto-close)
        /// </summary>
        private void tProcess_Tick(Object Sender, EventArgs e)
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
                            _ProcessHooked = true;
                            WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            ChecExeMd5();                            
                            ApplyKeyboardHook();
                            try
                            {
                                String strCoinSndPath = _TargetProcess.MainModule.FileName;
                                strCoinSndPath = strCoinSndPath.Substring(0, strCoinSndPath.Length - 10);
                                strCoinSndPath += @"..\media\coin002.aif";
                                _SndPlayer = new SoundPlayer(strCoinSndPath);
                            }
                            catch
                            {
                                WriteLog("Unable to find/open the coin002.aif file for Hotd3");
                            }
                        }
                    }
                }
                catch
                {
                    WriteLog("Error trying to hook " + _Target_Process_Name + ".exe");
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
                    WriteLog(_Target_Process_Name + ".exe closed");
                    Environment.Exit(0);
                }
            }
        }

        // Keyboard callback used for adding coins
        protected override IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                Win32.KBDLLHOOKSTRUCT s = (Win32.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32.WM_KEYDOWN)
                {
                    if (s.scanCode == 0x06 /* [5] Key */)
                    {
                        byte Credits = ReadByte((int)_Credits_Address);
                        Credits++;
                        WriteByte((int)_Credits_Address, Credits);
                        if (_SndPlayer != null)
                            _SndPlayer.Play();
                    }
                }
            }
            return Win32.CallNextHookEx(_KeyboardHookID, nCode, wParam, lParam);
        } 
    }
}
