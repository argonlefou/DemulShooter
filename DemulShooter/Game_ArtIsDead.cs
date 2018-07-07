using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace DemulShooter
{
    class ArtDead_Game : Game
    {
        private int _CaveAddress = 0;
        /*** MEMORY ADDRESSES **/        
        protected int _P1_X_Address;
        protected int _P1_Y_Address;
        protected int _P2_X_Address;
        protected int _P2_Y_Address;
        protected int _P1_Trigger_Address;
        protected int _P2_Trigger_Address;


        /// <summary>
        /// Constructor
        /// </summary>
        public ArtDead_Game(string RomName, bool Verbose)
            : base()
        {
            GetScreenResolution();
            
            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "gungallery";

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
       
        #region MemoryHack

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
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x20);
            _CaveAddress = CaveMemory.CaveAddress;
            WriteLog("Allocating memory at : 0x" + _CaveAddress.ToString("X8"));
            _P1_X_Address = _CaveAddress;
            _P1_Y_Address = _CaveAddress + 4;
            _P1_Trigger_Address = _CaveAddress + 8;
            _P2_X_Address = _CaveAddress + 9;
            _P2_Y_Address = _CaveAddress + 13;            
            _P2_Trigger_Address = _CaveAddress + 17;


            //Second Part
            //Step 1)            
            WriteByte((int)_TargetProcess_MemoryBaseAddress + 0x20F38, 0x85);   //P1 (2P mode)
            WriteByte((int)_TargetProcess_MemoryBaseAddress + 0x2106C, 0x85);   //P2 (2P mode)
            WriteByte((int)_TargetProcess_MemoryBaseAddress + 0x21245, 0x85);   //P1 (Solo)

            //Step 2)
            //P1 (2P mode)
            byte[] b = BitConverter.GetBytes(_P1_Trigger_Address);
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + 0x20FDD, b);
            b = BitConverter.GetBytes(_P1_X_Address);
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + 0x2100E, b);
            b = BitConverter.GetBytes(_P1_Y_Address);
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + 0x21001, b);
            //P2 (2P mode)
            b = BitConverter.GetBytes(_P2_Trigger_Address);
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + 0x211B2, b);
            b = BitConverter.GetBytes(_P2_X_Address);
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + 0x211E3, b);
            b = BitConverter.GetBytes(_P2_Y_Address);
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + 0x211D6, b);
            //P1 (Solo)
            b = BitConverter.GetBytes(_P1_Trigger_Address);
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + 0x212E4, b);
            b = BitConverter.GetBytes(_P1_X_Address);
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + 0x2130E, b);
            b = BitConverter.GetBytes(_P1_Y_Address);
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + 0x21301, b);

            //Step 3)
            //P1 (2P mode)
            b = BitConverter.GetBytes(_P1_Trigger_Address);
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + 0x2104D, b);
            //P2 (2P mode)
            b = BitConverter.GetBytes(_P2_Trigger_Address);
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + 0x21222, b);
            //P1 (Solo)
            b = BitConverter.GetBytes(_P1_Trigger_Address);
            WriteBytes((int)_TargetProcess_MemoryBaseAddress + 0x21353, b);

            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            byte[] bufferX = { (byte)(mouse.pTarget.X & 0xFF), (byte)(mouse.pTarget.X >> 8) };
            byte[] bufferY = { (byte)(mouse.pTarget.Y & 0xFF), (byte)(mouse.pTarget.Y >> 8) };

            if (Player == 1)
            {
                //Write Axis
                WriteBytes(_P1_X_Address, bufferX);
                WriteBytes(_P1_Y_Address, bufferY);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte((int)_P1_Trigger_Address, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte((int)_P1_Trigger_Address, 0x00);
                }
            }
            else if (Player == 2)
            {
                //Write Axis
                WriteBytes(_P2_X_Address, bufferX);
                WriteBytes(_P2_Y_Address, bufferY);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte((int)_P2_Trigger_Address, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte((int)_P2_Trigger_Address, 0x00);
                }
            }
        }

        #endregion
    }
}
