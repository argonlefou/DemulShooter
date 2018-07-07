using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace DemulShooter
{
    class Game_GvrFarCry : Game
    {
        private const string FOLDER_GAMEDATA = @"MemoryData\globalvr";

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_GvrFarCry(string RomName, bool Verbose) 
            : base ()
        {
            GetScreenResolution();
            
            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "FarCry_r";

            ReadGameData();

            _XOutputManager = new XOutput();
            InstallX360Gamepad(1);
            InstallX360Gamepad(2);

            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for Global VR " + _RomName + " game to hook.....");
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
                            //SetHack();
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
                    RemoveMouseHook();
                    _TargetProcess = null;
                    _ProcessHandle = IntPtr.Zero;
                    _TargetProcess_MemoryBaseAddress = IntPtr.Zero;
                    WriteLog(_Target_Process_Name + ".exe closed");
                    Environment.Exit(0);
                }
            }
        }

        #region Screen

        public override bool GameScale(MouseInfo Mouse, int Player)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    Win32.Rect TotalRes = new Win32.Rect();
                    Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    int TotalResX = TotalRes.Right - TotalRes.Left;
                    int TotalResY = TotalRes.Bottom - TotalRes.Top;

                    WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X,Y => [-32768 ; 32767] = 0xFFFF
                    double dMaxX = 65535.0;
                    double dMaxY = 65535.0;

                    Mouse.pTarget.X = Convert.ToInt32(Math.Round(dMaxX * Mouse.pTarget.X / TotalResX) - 32768);
                    Mouse.pTarget.Y = Convert.ToInt32(Math.Round(dMaxY * Mouse.pTarget.Y / TotalResY) - 32768) * -1;
                    if (Mouse.pTarget.X < -32768)
                        Mouse.pTarget.X = -32768;
                    if (Mouse.pTarget.Y < -32768)
                        Mouse.pTarget.Y = -32768;
                    if (Mouse.pTarget.X > 32767)
                        Mouse.pTarget.X = 32767;
                    if (Mouse.pTarget.Y > 32767)
                        Mouse.pTarget.Y = 32767;
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

        public override void SendInput(MouseInfo mouse, int Player)
        {
            _XOutputManager.SetLAxis_X(Player, (short)mouse.pTarget.X);
            _XOutputManager.SetLAxis_Y(Player, (short)mouse.pTarget.Y);

            //Inputs
            if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
            {
                _XOutputManager.SetButton_A(Player, true);
            }
            else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
            {
                _XOutputManager.SetButton_A(Player, false);
            }
            else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
            {
                _XOutputManager.SetButton_B(Player, true);
            }
            else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
            {
                _XOutputManager.SetButton_B(Player, false);
            }
        }

        #endregion
    }
}
