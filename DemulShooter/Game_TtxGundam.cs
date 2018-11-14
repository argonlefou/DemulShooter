using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace DemulShooter
{
    class Game_TtxGundam : Game
    {
        private const string FOLDER_GAMEDATA = @"MemoryData\ttx";

        /*** MEMORY ADDRESSES **/
        protected int _P1_X_Offset;
        protected int _P1_Y_Offset;
        protected string _P1_X_NOP_Offset;
        // This one is tricky ! Only used when on 2P game.exe (when mouse X > 640) and is screwing 1P X axis :
        protected string _P1_X_NOP_Offset2;
        protected string _P1_Y_NOP_Offset;
        protected int _P1_Trigger_Offset;
        protected int _P1_WeaponChange_Offset;
        protected int _P2_X_Offset;
        protected int _P2_Y_Offset;
        protected int _P2_Trigger_Offset;
        protected int _P2_WeaponChange_Offset;
        protected string _Btn_Down_NOP_Offset;
        protected string _Btn_Down_NOP_Offset2;
        protected string _Btn_Up_NOP_Offset;
        protected string _Btn_Up_NOP_Offset2;
        protected string _Btn_Reset_NOP_Offset;
        protected int _Border_Check1_Injection_Offset;
        protected int _Border_Check1_Injection_Return_Offset;
        protected int _Border_Check2_Injection_Offset;
        protected int _Border_Check2_Injection_Return_Offset;

        private int _Pedal1_Enable;
        private byte _Pedal1_Key;
        private bool _isPedal1_Pushed = false;
        private int _Pedal2_Enable;
        private byte _Pedal2_Key;
        private bool _isPedal2_Pushed = false;

        Memory _Cave_Check1, _Cave_Check2;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_TtxGundam(string RomName, int Pedal1_Enable, byte Pedal1_Key, int Pedal2_Enable, byte Pedal2_Key, bool Verbose)
            : base()
        {
            GetScreenResolution();

            _RomName = RomName;
            _Pedal1_Enable = Pedal1_Enable;
            _Pedal1_Key = Pedal1_Key;
            _Pedal2_Enable = Pedal2_Enable;
            _Pedal2_Key = Pedal2_Key;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "game";

            ReadGameData();

            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for Taito Type X " + _RomName + " game to hook.....");
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
                            SetHack();
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

        #region File I/O

        /// <summary>
        /// Read memory values in .cfg file
        /// </summary>
        protected override void ReadGameData()
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\gundamz.cfg"))
            {
                using (StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\gundamz.cfg"))
                {
                    string line;
                    line = sr.ReadLine();
                    while (line != null)
                    {
                        string[] buffer = line.Split('=');
                        if (buffer.Length > 1)
                        {
                            try
                            {
                                switch (buffer[0].ToUpper().Trim())
                                {
                                    case "P1_X_OFFSET":
                                        _P1_X_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_Y_OFFSET":
                                        _P1_Y_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_TRIGGER_OFFSET":
                                        _P1_Trigger_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_WEAPONCHANGE_OFFSET":
                                        _P1_WeaponChange_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P1_X_NOP_OFFSET":
                                        _P1_X_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "P1_X_NOP_OFFSET2":
                                        _P1_X_NOP_Offset2 = buffer[1].Trim();
                                        break;
                                    case "P1_Y_NOP_OFFSET":
                                        _P1_Y_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "P2_X_OFFSET":
                                        _P2_X_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_Y_OFFSET":
                                        _P2_Y_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_TRIGGER_OFFSET":
                                        _P2_Trigger_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "P2_WEAPONCHANGE_OFFSET":
                                        _P2_WeaponChange_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "BTN_DOWN_NOP_OFFSET":
                                        _Btn_Down_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "BTN_DOWN_NOP_OFFSET2":
                                        _Btn_Down_NOP_Offset2 = buffer[1].Trim();
                                        break;
                                    case "BTN_UP_NOP_OFFSET":
                                        _Btn_Up_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "BTN_UP_NOP_OFFSET2":
                                        _Btn_Up_NOP_Offset2 = buffer[1].Trim();
                                        break;
                                    case "BTN_RESET_NOP_OFFSET":
                                        _Btn_Reset_NOP_Offset = buffer[1].Trim();
                                        break;
                                    case "BORDER_CHECK1_INJECTION_OFFSET":
                                        _Border_Check1_Injection_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "BORDER_CHECK1_INJECTION_RETURN_OFFSET":
                                        _Border_Check1_Injection_Return_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "BORDER_CHECK2_INJECTION_OFFSET":
                                        _Border_Check2_Injection_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    case "BORDER_CHECK2_INJECTION_RETURN_OFFSET":
                                        _Border_Check2_Injection_Return_Offset = int.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                        break;
                                    default: break;
                                }
                            }
                            catch (Exception ex)
                            {
                                WriteLog("Error reading game data : " + ex.Message.ToString());
                            }
                        }
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }
            }
            else
            {
                WriteLog("File not found : " + AppDomain.CurrentDomain.BaseDirectory + @"\" + FOLDER_GAMEDATA + @"\" + _RomName + ".cfg");
            }
        }

        #endregion

        #region Screen

        public override bool ClientScale(MouseInfo mouse)
        {
            /*IntPtr hWnd = Win32.GetForegroundWindow();
            if (hWnd != IntPtr.Zero)                         
              return Win32.ScreenToClient(hWnd, ref mouse.pTarget);                
            else
                return false;*/


            /*************************************************************************************************
             * Screen has a weird behavior with this game and the background dosbox activated to launch it.
             * Process mainhandle is giving DOS windows size...
             * Foreground Windows sometimes fail on top of where the DOS box is...
             * With Game All RH launcher, fullscreen is an obligation so the game res = screen res,
             * so in-game cursor position = screen cursor position .
             * This is preventing bugs on axis positionning
             ************************************************************************************************/
            return true;
        }

        public override bool GameScale(MouseInfo Mouse, int Player)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    double dMaxX = 1280.0;
                    double dMaxY = 480.0;

                    if (_RomName.Equals("gsoz2p"))
                    {
                        //Side by Side dual screen, each one 640x480 max coordonates
                        //So total X is 1280
                        Mouse.pTarget.X = Convert.ToInt16(Math.Round(dMaxX * Mouse.pTarget.X / _screenWidth));

                        //For Y we need to change reference from Screen[X, Y] to real Game [X, Y] with black border;
                        double Ratio = _screenWidth / dMaxX;
                        WriteLog("Ratio = " + Ratio.ToString());
                        double GameHeight = Ratio * dMaxY;
                        WriteLog("Real game height (px) = " + GameHeight);
                        double Border = (_screenHeight - GameHeight) / 2.0;
                        WriteLog("Black boder top and bottom (px) = " + Border.ToString());
                        double y = Mouse.pTarget.Y - Border;
                        double percent = y * 100.0 / GameHeight;
                        Mouse.pTarget.Y = (int)(percent * dMaxY / 100.0);

                        //Player One will have X value cut-off to [0-639] next
                        //For player 2 we first shift value to the left
                        if (Player == 2)
                            Mouse.pTarget.X -= 640;
                    }
                    else if (_RomName.Equals("gsoz"))
                    {
                        //Single screen, each one 640x480 max coordonates
                        //So total Y is 480
                        Mouse.pTarget.Y = Convert.ToInt16(Math.Round(dMaxY * Mouse.pTarget.Y / _screenHeight));

                        //For X we need to change reference from Screen[X, Y] to real Game [X, Y] with black border
                        dMaxX = 640.0;

                        double Ratio = _screenHeight / dMaxY;
                        WriteLog("Ratio = " + Ratio.ToString());
                        double GameWidth = Ratio * dMaxX;
                        WriteLog("Real game width (px) = " + GameWidth);
                        double Border = (_screenWidth - GameWidth) / 2.0;
                        WriteLog("Black boder left and right (px) = " + Border.ToString());
                        double x = Mouse.pTarget.X - Border;
                        double percent = x * 100.0 / GameWidth;
                        Mouse.pTarget.X = (int)(percent * dMaxX / 100.0);
                    }

                    if (Mouse.pTarget.X < 1)
                        Mouse.pTarget.X = 1;
                    if (Mouse.pTarget.X > 639)
                        Mouse.pTarget.X = 639;
                    if (Mouse.pTarget.Y < 1)
                        Mouse.pTarget.Y = 1;
                    if (Mouse.pTarget.Y > (int)dMaxY)
                        Mouse.pTarget.Y = (int)dMaxY;

                    return true;
                }
                catch (Exception ex)
                {
                    WriteLog("Error scaling mouse coordonates to GameFormat : " + ex.Message.ToString());
                }
            }
            return false;
        }

        #endregion

        #region MemoryHack

        private void SetHack()
        {
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P1_X_NOP_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P1_X_NOP_Offset2);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _P1_Y_NOP_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _Btn_Down_NOP_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _Btn_Down_NOP_Offset2);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _Btn_Up_NOP_Offset);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _Btn_Up_NOP_Offset2);
            SetNops((int)_TargetProcess_MemoryBaseAddress, _Btn_Reset_NOP_Offset);

            /***
             * If neither Pedal1 nor Pedal2 are enabled, no need for a codecave (default game).
             * Else, We need to make 2 codecave for the 2 checking procedure (X-Y min-max)
             * Each one will have to be split for P1 and P2 separatelly
             ***/
            if (_Pedal1_Enable != 0 || _Pedal2_Enable != 0)
            {
                _Cave_Check1 = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
                _Cave_Check1.Open();
                _Cave_Check1.Alloc(0x800);
                //cmp [esi+1C], 0
                _Cave_Check1.Write_StrBytes("83 7E 1C 00");
                //je @player1
                _Cave_Check1.Write_StrBytes("0F 84 0A 00 00 00");
                //cmp [esi+1C], 1
                _Cave_Check1.Write_StrBytes("83 7E 1C 01");
                //je @player2
                _Cave_Check1.Write_StrBytes("0F 84 33 00 00 00");

                //player1:
                if (_Pedal1_Enable != 0)
                    //cmp di, 00
                    _Cave_Check1.Write_StrBytes("66 83 FF 00");
                else
                    //cmp di, 10
                    _Cave_Check1.Write_StrBytes("66 83 FF 10");
                //jng game.exe+A7B85
                _Cave_Check1.Write_jng((int)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                if (_Pedal1_Enable != 0)
                    //cmp di, 00
                    _Cave_Check1.Write_StrBytes("66 81 FF 00 00");
                else
                    //cmp di, 0270
                    _Cave_Check1.Write_StrBytes("66 81 FF 70 02");
                //jnl game.exe+A7B85
                _Cave_Check1.Write_jnl((int)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                //mov eax, edi
                _Cave_Check1.Write_StrBytes("8B C7");
                //shr eax, 10
                _Cave_Check1.Write_StrBytes("C1 E8 10");
                if (_Pedal1_Enable != 0)
                    //cmp ax, 00
                    _Cave_Check1.Write_StrBytes("66 3D 00 00");
                else
                    //cmp ax, 10
                    _Cave_Check1.Write_StrBytes("66 3D 10 00");
                //jng game.exe+A7B85
                _Cave_Check1.Write_jng((int)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                if (_Pedal1_Enable != 0)
                    //cmp ax, 00
                    _Cave_Check1.Write_StrBytes("66 3D E0 01");
                else
                    //cmp ax, 01D0
                    _Cave_Check1.Write_StrBytes("66 3D D0 01");
                //jnl game.exe+A7B85
                _Cave_Check1.Write_jnl((int)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                //jmp EXIT
                _Cave_Check1.Write_StrBytes("E9 2E 00 00 00");

                //player2:
                if (_Pedal2_Enable != 0)
                    //cmp di, 00
                    _Cave_Check1.Write_StrBytes("66 83 FF 00");
                else
                    //cmp di, 10
                    _Cave_Check1.Write_StrBytes("66 83 FF 10");
                //jng game.exe+A7B85
                _Cave_Check1.Write_jng((int)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                if (_Pedal2_Enable != 0)
                    //cmp di, 00
                    _Cave_Check1.Write_StrBytes("66 81 FF 00 00");
                else
                    //cmp di, 0270
                    _Cave_Check1.Write_StrBytes("66 81 FF 70 02");
                //jnl game.exe+A7B85
                _Cave_Check1.Write_jnl((int)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                //mov eax, edi
                _Cave_Check1.Write_StrBytes("8B C7");
                //shr eax, 10
                _Cave_Check1.Write_StrBytes("C1 E8 10");
                if (_Pedal2_Enable != 0)
                    //cmp ax, 00
                    _Cave_Check1.Write_StrBytes("66 3D 00 00");
                else
                    //cmp ax, 10
                    _Cave_Check1.Write_StrBytes("66 3D 10 00");
                //jng game.exe+A7B85
                _Cave_Check1.Write_jng((int)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                if (_Pedal2_Enable != 0)
                    //cmp ax, 00
                    _Cave_Check1.Write_StrBytes("66 3D E0 01");
                else
                    //cmp ax, 01D0
                    _Cave_Check1.Write_StrBytes("66 3D D0 01");
                //jnl game.exe+A7B85
                _Cave_Check1.Write_jnl((int)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                //jmp EXIT
                _Cave_Check1.Write_jmp((int)_TargetProcess_MemoryBaseAddress + _Border_Check1_Injection_Return_Offset);

                WriteLog("Adding check1 CodeCave at : 0x" + _Cave_Check1.CaveAddress.ToString("X8"));
                IntPtr ProcessHandle = _TargetProcess.Handle;
                int bytesWritten = 0;
                int jumpTo = 0;
                jumpTo = _Cave_Check1.CaveAddress - ((int)_TargetProcess_MemoryBaseAddress + _Border_Check1_Injection_Offset) - 5;
                List<byte> Buffer = new List<byte>();
                Buffer.Add(0xE9);
                Buffer.AddRange(BitConverter.GetBytes(jumpTo));
                Buffer.Add(0x90);
                Win32.WriteProcessMemory((int)ProcessHandle, (int)_TargetProcess_MemoryBaseAddress + _Border_Check1_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);

                _Cave_Check2 = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
                _Cave_Check2.Open();
                _Cave_Check2.Alloc(0x800);
                //cmp [esi+1C], 0
                _Cave_Check2.Write_StrBytes("83 7E 1C 00");
                //je @player1
                _Cave_Check2.Write_StrBytes("0F 84 0A 00 00 00");
                //cmp [esi+1C], 1
                _Cave_Check2.Write_StrBytes("83 7E 1C 01");
                //je @player2
                _Cave_Check2.Write_StrBytes("0F 84 33 00 00 00");
                //player1:
                if (_Pedal1_Enable != 0)
                    //cmp di, 00
                    _Cave_Check2.Write_StrBytes("66 83 FF 00");
                else
                    //cmp di, 10
                    _Cave_Check2.Write_StrBytes("66 83 FF 10");
                //jl game.exe+A7B85
                _Cave_Check2.Write_jl((int)_TargetProcess_MemoryBaseAddress + 0xA7B0C);
                if (_Pedal1_Enable != 0)
                    //cmp di, 00
                    _Cave_Check2.Write_StrBytes("66 81 FF 00 00");
                else
                    //cmp di, 0270
                    _Cave_Check2.Write_StrBytes("66 81 FF 70 02");
                //jg game.exe+A7B85
                _Cave_Check2.Write_jg((int)_TargetProcess_MemoryBaseAddress + 0xA7B0C);
                //mov eax, [esp+26]
                _Cave_Check2.Write_StrBytes("66 8B 44 24 26");
                if (_Pedal1_Enable != 0)
                    //cmp ax, 00
                    _Cave_Check2.Write_StrBytes("66 3D 00 00");
                else
                    //cmp ax, 10
                    _Cave_Check2.Write_StrBytes("66 3D 10 00");
                //jl game.exe+A7B85
                _Cave_Check2.Write_jl((int)_TargetProcess_MemoryBaseAddress + 0xA7B0C);
                if (_Pedal1_Enable != 0)
                    //cmp ax, 00
                    _Cave_Check2.Write_StrBytes("66 3D E0 01");
                else
                    //cmp ax, 01D0
                    _Cave_Check2.Write_StrBytes("66 3D D0 01");
                //jle game.exe+A7B85
                _Cave_Check2.Write_jng((int)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                //jmp EXIT
                _Cave_Check2.Write_StrBytes("E9 2E 00 00 00");

                //player2:
                if (_Pedal2_Enable != 0)
                    //cmp di, 00
                    _Cave_Check2.Write_StrBytes("66 83 FF 00");
                else
                    //cmp di, 10
                    _Cave_Check2.Write_StrBytes("66 83 FF 10");
                //jl game.exe+A7B85
                _Cave_Check2.Write_jl((int)_TargetProcess_MemoryBaseAddress + 0xA7B0C);
                if (_Pedal2_Enable != 0)
                    //cmp di, 00
                    _Cave_Check2.Write_StrBytes("66 81 FF 00 00");
                else
                    //cmp di, 0270
                    _Cave_Check2.Write_StrBytes("66 81 FF 70 02");
                //jg game.exe+A7B85
                _Cave_Check2.Write_jg((int)_TargetProcess_MemoryBaseAddress + 0xA7B0C);
                //mov eax, [esp+26]
                _Cave_Check2.Write_StrBytes("66 8B 44 24 26");
                if (_Pedal2_Enable != 0)
                    //cmp ax, 00
                    _Cave_Check2.Write_StrBytes("66 3D 00 00");
                else
                    //cmp ax, 10
                    _Cave_Check2.Write_StrBytes("66 3D 10 00");
                //jl game.exe+A7B85
                _Cave_Check2.Write_jl((int)_TargetProcess_MemoryBaseAddress + 0xA7B0C);
                if (_Pedal2_Enable != 0)
                    //cmp ax, 00
                    _Cave_Check2.Write_StrBytes("66 3D E0 01");
                else
                    //cmp ax_Cave_Check2 01D0
                    _Cave_Check2.Write_StrBytes("66 3D D0 01");
                //jle game.exe+A7B85
                _Cave_Check2.Write_jng((int)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                //jmp EXIT
                _Cave_Check2.Write_jmp((int)_TargetProcess_MemoryBaseAddress + _Border_Check2_Injection_Return_Offset);

                WriteLog("Adding check2 CodeCave at : 0x" + _Cave_Check2.CaveAddress.ToString("X8"));
                bytesWritten = 0;
                jumpTo = _Cave_Check2.CaveAddress - ((int)_TargetProcess_MemoryBaseAddress + _Border_Check2_Injection_Offset) - 5;
                Buffer = new List<byte>();
                Buffer.Add(0xE9);
                Buffer.AddRange(BitConverter.GetBytes(jumpTo));
                Buffer.Add(0x90);
                Win32.WriteProcessMemory((int)ProcessHandle, (int)_TargetProcess_MemoryBaseAddress + _Border_Check2_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);

                ApplyKeyboardHook();
            }

            //Initializing values
            byte[] initX = { 0x10, 0 };
            byte[] initY = { 0x10, 0 };
            //Write Axis
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, initX);
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, initY);
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, initX);
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, initY);
            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        // Keyboard callback used for pedal-mode
        protected override IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                Win32.KBDLLHOOKSTRUCT s = (Win32.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Win32.KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32.WM_KEYDOWN)
                {
                    if (s.scanCode == _Pedal1_Key && _Pedal1_Enable != 0)
                    {
                        _isPedal1_Pushed = true;
                        WriteBytes((int)_Cave_Check1.CaveAddress + 0x21, new byte[] { 0x80, 0x02 }); //640
                        WriteBytes((int)_Cave_Check2.CaveAddress + 0x21, new byte[] { 0x80, 0x02 }); //640
                    }
                    else if (s.scanCode == _Pedal2_Key && _Pedal2_Enable != 0)
                    {
                        _isPedal2_Pushed = true;
                        WriteBytes((int)_Cave_Check1.CaveAddress + 0x54, new byte[] { 0x80, 0x02 }); //640
                        WriteBytes((int)_Cave_Check2.CaveAddress + 0x54, new byte[] { 0x80, 0x02 }); //640
                    }
                }
                else if ((UInt32)wParam == Win32.WM_KEYUP)
                {
                    if (s.scanCode == _Pedal1_Key && _Pedal1_Enable != 0)
                    {
                        _isPedal1_Pushed = false;
                        WriteBytes((int)_Cave_Check1.CaveAddress + 0x21, new byte[] { 0x00, 0x00 }); //0
                        WriteBytes((int)_Cave_Check2.CaveAddress + 0x21, new byte[] { 0x00, 0x00 }); //0
                    }
                    else if (s.scanCode == _Pedal2_Key && _Pedal2_Enable != 0)
                    {
                        _isPedal2_Pushed = false;
                        WriteBytes((int)_Cave_Check1.CaveAddress + 0x54, new byte[] { 0x00, 0x00 }); //0
                        WriteBytes((int)_Cave_Check2.CaveAddress + 0x54, new byte[] { 0x00, 0x00 }); //0
                    }
                }
            }
            return Win32.CallNextHookEx(_KeyboardHookID, nCode, wParam, lParam);
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            byte[] bufferX = { (byte)(mouse.pTarget.X & 0xFF), (byte)(mouse.pTarget.X >> 8) };
            byte[] bufferY = { (byte)(mouse.pTarget.Y & 0xFF), (byte)(mouse.pTarget.Y >> 8) };

            if (Player == 1)
            {
                //Write Axis
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, new byte[] { 0x01, 0x01, 0x00 });
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, new byte[] { 0x00, 0x00, 0x01 });
                }
                if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_WeaponChange_Offset, new byte[] { 0x01, 0x01, 0x00 });
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_WeaponChange_Offset, new byte[] { 0x00, 0x00, 0x01 });
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    //With "Pedal mode", enable right-click to shoot only when hiding
                    if (_Pedal1_Enable != 0)
                    {
                        if (!_isPedal1_Pushed)
                            WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, new byte[] { 0x01, 0x01, 0x00 });
                    }
                    else
                        WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, new byte[] { 0x01, 0x01, 0x00 });
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P1_Trigger_Offset, new byte[] { 0x00, 0x00, 0x01 });
                }
            }
            else if (Player == 2)
            {
                //Write Axis
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, new byte[] { 0x01, 0x01, 0x00 });
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, new byte[] { 0x00, 0x00, 0x01 });
                }
                if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_WeaponChange_Offset, new byte[] { 0x01, 0x01, 0x00 });
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_WeaponChange_Offset, new byte[] { 0x00, 0x00, 0x01 });
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    //With "Pedal mode", enable right-click to shoot only when hiding
                    if (_Pedal2_Enable != 0)
                    {
                        if (!_isPedal2_Pushed)
                            WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, new byte[] { 0x01, 0x01, 0x00 });
                    }
                    else
                        WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, new byte[] { 0x01, 0x01, 0x00 });
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    WriteBytes((int)_TargetProcess_MemoryBaseAddress + _P2_Trigger_Offset, new byte[] { 0x00, 0x00, 0x01 });
                }
            }
        }

        #endregion
    }
}
