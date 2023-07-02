using System;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    public class Game_WndArtIsDead : Game
    {        
        /*** MEMORY ADDRESSES **/        
        private UInt32 _P1_X_CaveAddress;
        private UInt32 _P1_Y_CaveAddress;
        private UInt32 _P2_X_CaveAddress;
        private UInt32 _P2_Y_CaveAddress;
        private UInt32 _P1_Trigger_CaveAddress;
        private UInt32 _P2_Trigger_CaveAddress;    

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndArtIsDead(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "gungallery", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Art Is Dead", "4cc4e814647a1d9f315fa1be4ac3d92a");

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
                            if (!_DisableInputHack)
                                SetHack();
                            else
                                Logger.WriteLog("Input Hack disabled");
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
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X => [0-640] = 640
                    //Y => [0-480] = 480
                    double dMaxX = 640;
                    double dMaxY = 480;

                    PlayerData.RIController.Computed_X = Convert.ToInt32(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt32(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));
                    if (PlayerData.RIController.Computed_X < 0)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 0)
                        PlayerData.RIController.Computed_Y = 0;
                    if (PlayerData.RIController.Computed_X > (int)dMaxX)
                        PlayerData.RIController.Computed_X = (int)dMaxX;
                    if (PlayerData.RIController.Computed_Y > (int)dMaxY)
                        PlayerData.RIController.Computed_Y = (int)dMaxY;

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
            /***
             * This game is made to work with ActLabs CRT guns, so no gun = no movement
             * The first part will be to allocate some memory to store P1 & P2 Aimtrak data (axis + trigger)
             * The second part is to patch the memory in 3 steps :
             * 1) Force the update of P1 and P2 values even if no act lab gun detected
             * 2) Read cursor data in newlly allocated memory instead of the original one
             * 3) Reset trigger state by writing the new allocated memory instead of the original one
             * 
             * Theses patch must be done separatly for P1 (solo mode), P1 (2P mode) and P2 (2P mode)
             * as the procedures and memory location are not the same
            ***/            
            
            //First part = Allocating Memory to store P1 and P2 axis values and trigger
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x20);
            Logger.WriteLog("Allocating memory at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
            _P1_X_CaveAddress = CaveMemory.CaveAddress;
            _P1_Y_CaveAddress = CaveMemory.CaveAddress + 4;
            _P1_Trigger_CaveAddress = CaveMemory.CaveAddress + 8;
            _P2_X_CaveAddress = CaveMemory.CaveAddress + 9;
            _P2_Y_CaveAddress = CaveMemory.CaveAddress + 13;
            _P2_Trigger_CaveAddress = CaveMemory.CaveAddress + 17;

            //Second Part
            //Step 1)            
            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x20F38, 0x85);   //P1 (2P mode)
            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x2106C, 0x85);   //P2 (2P mode)
            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x21245, 0x85);   //P1 (Solo)

            //Step 2)
            //P1 (2P mode)
            byte[] b = BitConverter.GetBytes(_P1_Trigger_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x20FDD, b);
            b = BitConverter.GetBytes(_P1_X_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x2100E, b);
            b = BitConverter.GetBytes(_P1_Y_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x21001, b);
            //P2 (2P mode)
            b = BitConverter.GetBytes(_P2_Trigger_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x211B2, b);
            b = BitConverter.GetBytes(_P2_X_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x211E3, b);
            b = BitConverter.GetBytes(_P2_Y_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x211D6, b);
            //P1 (Solo)
            b = BitConverter.GetBytes(_P1_Trigger_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x212E4, b);
            b = BitConverter.GetBytes(_P1_X_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x2130E, b);
            b = BitConverter.GetBytes(_P1_Y_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x21301, b);

            //Step 3)
            //P1 (2P mode)
            b = BitConverter.GetBytes(_P1_Trigger_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x2104D, b);
            //P2 (2P mode)
            b = BitConverter.GetBytes(_P2_Trigger_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x21222, b);
            //P1 (Solo)
            b = BitConverter.GetBytes(_P1_Trigger_CaveAddress);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + 0x21353, b);

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }
        
        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>   
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes(_P1_X_CaveAddress, bufferX);
                WriteBytes(_P1_Y_CaveAddress, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                   WriteByte((UInt32)_P1_Trigger_CaveAddress, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte((UInt32)_P1_Trigger_CaveAddress, 0x00);               
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes(_P2_X_CaveAddress, bufferX);
                WriteBytes(_P2_Y_CaveAddress, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteByte((UInt32)_P2_Trigger_CaveAddress, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte((UInt32)_P2_Trigger_CaveAddress, 0x00);      
            }
        }

        #endregion
    }
}
