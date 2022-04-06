using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MemoryX64;
using DsCore.Win32;

namespace DemulShooterX64
{
    public class Game_Bhapc : Game
    {        
        /*** MEMORY ADDRESSES **/
        private const UInt64 P1_X_OFFSET = 0x000000B8;
        private const UInt64 P1_Y_OFFSET = 0x000000BC;
        private const UInt64 P1_STRUCT_PTR_OFFSET = 0x0123A658;
        private const UInt64 P1_MOUSEDELTAX_PTR_OFFSET = 0x01285E70;
        private NopStruct _Nop_P1_Axis_1 = new NopStruct(0x00124814, 7);
        private NopStruct _Nop_P1_Axis_2 = new NopStruct(0x00124F7B, 7);

        private UInt64 _P1_StructAddress = 0;
        private UInt64 _P1_MouseDeltaX_Address = 0;
        
        //Custom data to inject
        private float _P1_X_Value;
        private float _P1_Y_Value;
        private float _P2_X_Value;
        private float _P2_Y_Value;

        //Even when changing coordinates, it's not working in-game as long as ONE of the native RAWINPUT axis delta is not modified
        //So we will modify one of them with some custom delta value (value itself doesn't matter, it's just to change it)
        private float _P1_LastX;
        private float _P1_DeltaX;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Bhapc(String RomName, bool DisableInputHack, bool Verbose) : base(RomName, "Buck", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Big Buck Hunter Arcade v5.3.6 - PLAZA", "2a6f04726a2471adf68a27386898eabc");
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

                        Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));

                        if (_TargetProcess_MemoryBaseAddress != null)
                        {
                            byte[] bBuffer = ReadBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + P1_STRUCT_PTR_OFFSET), 8);
                            _P1_StructAddress = BitConverter.ToUInt64(bBuffer, 0);
                            bBuffer = ReadBytes((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + P1_MOUSEDELTAX_PTR_OFFSET), 8);
                            _P1_MouseDeltaX_Address = BitConverter.ToUInt64(bBuffer, 0) + 8;

                            if (_P1_StructAddress != 0 && _P1_MouseDeltaX_Address != 0)
                            {                                
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
                                Logger.WriteLog("P1_StructAddress = 0x" + _P1_StructAddress.ToString("X16"));
                                Logger.WriteLog("P1_MouseDeltaXAddress = 0x" + _P1_MouseDeltaX_Address.ToString("X16"));
                                CheckExeMd5();
                                if (_DisableInputHack)
                                    SetHack();
                                else
                                    Logger.WriteLog("Input Hack disabled");
                                _ProcessHooked = true;                                
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Error trying to hook " + _Target_Process_Name + ".exe");
                    Logger.WriteLog(ex.Message.ToString());
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
                    //Window size
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X => [0 -> ClientWidth]
                    //Y => [ClientHeight -> 0]

                    PlayerData.RIController.Computed_Y = (int)TotalResY - PlayerData.RIController.Computed_Y;

                    if (PlayerData.RIController.Computed_X < 0)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 0)
                        PlayerData.RIController.Computed_Y = 0;
                    if (PlayerData.RIController.Computed_X > (int)TotalResX)
                        PlayerData.RIController.Computed_X = (int)TotalResX;
                    if (PlayerData.RIController.Computed_Y > (int)TotalResY)
                        PlayerData.RIController.Computed_Y = (int)TotalResY;

                    if (PlayerData.ID == 1)
                    {
                        _P1_X_Value = (float)(PlayerData.RIController.Computed_X);
                        _P1_Y_Value = (float)(PlayerData.RIController.Computed_Y);
                        _P1_DeltaX = _P1_X_Value - _P1_LastX;
                        _P1_LastX = _P1_X_Value;
                    }
                    else if (PlayerData.ID == 2)
                    {
                        _P2_X_Value = (float)(PlayerData.RIController.Computed_X);
                        _P2_Y_Value = (float)(PlayerData.RIController.Computed_Y);
                    }

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

        private void SetHack()
        {
            SetNops(_TargetProcess_MemoryBaseAddress, _Nop_P1_Axis_1);
            SetNops(_TargetProcess_MemoryBaseAddress, _Nop_P1_Axis_2);
            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");           
        }
        
        public override void SendInput(PlayerSettings PlayerData)
        {
            if (PlayerData.ID == 1)
            { 
                //Setting Values in memory for the Codecave to read it
                byte[] buffer = BitConverter.GetBytes(_P1_X_Value);
                WriteBytes((IntPtr)(_P1_StructAddress + P1_X_OFFSET), buffer);
                buffer = BitConverter.GetBytes(_P1_Y_Value);
                WriteBytes((IntPtr)(_P1_StructAddress + P1_Y_OFFSET), buffer);
                //Modifying native RAWINPUT mouse delta handling so that the game accepts our values
                buffer = BitConverter.GetBytes(_P1_DeltaX);
                WriteBytes((IntPtr)_P1_MouseDeltaX_Address, buffer);

/*
                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Win32.SendMessage(_TargetProcess.MainWindowHandle, 0x0204, IntPtr.Zero, IntPtr.Zero);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Win32.SendMessage(_TargetProcess.MainWindowHandle, 0x0205, IntPtr.Zero, IntPtr.Zero);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    
                }*/
            }
            else if (PlayerData.ID == 2)
            {               
                
                
            }
        }

        #endregion
                
    }
}
