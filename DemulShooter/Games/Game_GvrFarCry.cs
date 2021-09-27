using System;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_GvrFarCry : Game
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Game_GvrFarCry(String RomName, double _ForcedXratio,bool Verbose)
            : base(RomName, "FarCry_r", _ForcedXratio, Verbose)
        {
            _KnownMd5Prints.Add("Far Cry Paradise", "263325AC3F7685CBA12B280D8E927A5D");
            _KnownMd5Prints.Add("Far Cry Paradise by Mohkerz", "557d065632eaa3c8adb5764df1609976");

            _XOutputManager = new XOutput();
            InstallX360Gamepad(1);
            InstallX360Gamepad(2);

            _tProcess.Start();
            Logger.WriteLog("Waiting for Global VR " + _RomName + " game to hook.....");
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
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();
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

        #region Screen

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// </summary>
        public override bool GameScale(PlayerSettings PlayerData)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    int TotalResX = TotalRes.Right - TotalRes.Left;
                    int TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X,Y => [-32768 ; 32767] = 0xFFFF
                    double dMaxX = 65535.0;
                    double dMaxY = 65535.0;

                    PlayerData.RIController.Computed_X = Convert.ToInt32(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX) - 32768);
                    PlayerData.RIController.Computed_Y = Convert.ToInt32(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY) - 32768) * -1;
                    if (PlayerData.RIController.Computed_X < -32768)
                        PlayerData.RIController.Computed_X = -32768;
                    if (PlayerData.RIController.Computed_Y < -32768)
                        PlayerData.RIController.Computed_Y = -32768;
                    if (PlayerData.RIController.Computed_X > 32767)
                        PlayerData.RIController.Computed_X = 32767;
                    if (PlayerData.RIController.Computed_Y > 32767)
                        PlayerData.RIController.Computed_Y = 32767;

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Error scaling mouse coordonates to GameFormat : " + ex.Message.ToString());
                }
            }
            return false;
        }

        #endregion

        #region Memory Hack

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>  
        public override void SendInput(PlayerSettings PlayerData)
        {
            _XOutputManager.SetLAxis_X(PlayerData.ID, (short)PlayerData.RIController.Computed_X);
            _XOutputManager.SetLAxis_Y(PlayerData.ID, (short)PlayerData.RIController.Computed_Y);

            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0) 
                _XOutputManager.SetButton_A(PlayerData.ID, true);
            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0) 
                _XOutputManager.SetButton_A(PlayerData.ID, false);
            
            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0) 
                _XOutputManager.SetButton_B(PlayerData.ID, true);
            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0) 
                _XOutputManager.SetButton_B(PlayerData.ID, false);
        }

        #endregion
    }
}
