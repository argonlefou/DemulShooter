using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using System.Windows.Forms;

namespace DemulShooter
{
    class Game_Lindbergh2spicy : Game
    {
        //Address to find InputStruct values (read at instruction 0x82EFC63)
        private const int INPUTSTRUCT_ADDRESS = 0x0C8B2430;

        //INPUT_STRUCT offset in game
        private const int INPUT_X_OFFSET = 0x17D;
        private const int INPUT_Y_OFFSET = 0x183;

        //NOP for Gun axis and buttons in-game
        private const string AXIS_X_NOP_ADDRESS_1 = "0x082F0109|7";
        private const string AXIS_X_NOP_ADDRESS_2 = "0x082EFEFC|7";
        private const string AXIS_Y_NOP_ADDRESS_1 = "0x082F0153|7";
        private const string AXIS_Y_NOP_ADDRESS_2 = "0x082EFF13|7";

        private int _Custom_Buttons_Bank_Ptr = 0;
        private const int TRIGGER_INJECTION_ADDRESS = 0x080820B9;
        private const int TRIGGER_INJECTION_RETURN_ADDRESS = 0x080820C0;
        private const int RELOAD_INJECTION_ADDRESS = 0x080820F3;

        private const int ROM_LOADED_CHECK_INSTRUCTION = 0x082EFC63;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_Lindbergh2spicy(string RomName, bool Verbose)
            : base()
        {
            GetScreenResolution();

            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "BudgieLoader";

            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for Lindbergh " + _RomName + " game to hook.....");
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
                            //To make sure BurgieLoader has loaded the rom entirely, we're looking for some random instruction to be present in memory before starting 
                            byte[] buffer = ReadBytes(ROM_LOADED_CHECK_INSTRUCTION, 5);
                            if (buffer[0] == 0xB8 && buffer[1] == 0x30 && buffer[2] == 0x24 && buffer[3] == 0x8B && buffer[4] == 0x0C)
                            {
                                _ProcessHooked = true;
                                WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));

                                SetHack();
                            }
                            else
                            {
                                WriteLog("Game not Loaded, waiting...");
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


        #region Screen

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// </summary>
        public override bool GameScale(MouseInfo Mouse, int Player)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    //Demul Window size
                    Win32.Rect TotalRes = new Win32.Rect();
                    Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");
                    //X => [0x0A - 0xF5]
                    //Y => [0x06 - 0xFA]                   
                    double dMaxX = 236.0;
                    double dMaxY = 245.0;

                    Mouse.pTarget.X = Convert.ToInt16(Math.Round(dMaxX * Mouse.pTarget.X / TotalResX) + 10);
                    Mouse.pTarget.Y = Convert.ToInt16(Math.Round(dMaxY * Mouse.pTarget.Y / TotalResY) + 6);
                    /*if (Mouse.pTarget.X < 10)
                        Mouse.pTarget.X = 10;
                    if (Mouse.pTarget.Y < 6)
                        Mouse.pTarget.Y = 6;
                    if (Mouse.pTarget.X > 245)
                        Mouse.pTarget.X = 245;
                    if (Mouse.pTarget.Y > 250)
                        Mouse.pTarget.Y = 250;*/
                    if (Mouse.pTarget.X < 10)
                        Mouse.pTarget.X = 0;
                    if (Mouse.pTarget.Y < 6)
                        Mouse.pTarget.Y = 0;
                    if (Mouse.pTarget.X > 245)
                        Mouse.pTarget.X = 255;
                    if (Mouse.pTarget.Y > 250)
                        Mouse.pTarget.Y = 255;

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
            SetHack_DataBank();
            SetHack_OverwriteTrigger();
            SetHack_OverwriteReload();

            SetNops(0, AXIS_X_NOP_ADDRESS_1);
            SetNops(0, AXIS_X_NOP_ADDRESS_2);
            SetNops(0, AXIS_Y_NOP_ADDRESS_1);
            SetNops(0, AXIS_Y_NOP_ADDRESS_2);
            
            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        /// <summary>
        /// Creating a custom memory bank to store our Buttons data
        /// </summary>
        private void SetHack_DataBank()
        {
            //1st Codecave : storing P1 and P2 input structure data, read from register in main program code
            Memory DataCaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            DataCaveMemory.Open();
            DataCaveMemory.Alloc(0x800);

            _Custom_Buttons_Bank_Ptr = DataCaveMemory.CaveAddress;

            WriteLog("Custom data will be stored at : 0x" + _Custom_Buttons_Bank_Ptr.ToString("X8"));
        }

        private void SetHack_OverwriteTrigger()
        {
            List<Byte> Buffer = new List<Byte>();
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //movzx edx, [_Custom_Buttons_Bank_Ptr]
            CaveMemory.Write_StrBytes("0F B6 15");
            Buffer.AddRange(BitConverter.GetBytes(_Custom_Buttons_Bank_Ptr));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //and [_Custom_Buttons_Bank_Ptr], 0xFFFFFFFD
            CaveMemory.Write_StrBytes("80 25");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes(_Custom_Buttons_Bank_Ptr));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("FD");
            CaveMemory.Write_jmp(TRIGGER_INJECTION_RETURN_ADDRESS);

            WriteLog("Adding Trigger Codecave_1 at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - (TRIGGER_INJECTION_ADDRESS) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, TRIGGER_INJECTION_ADDRESS, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        }

        private void SetHack_OverwriteReload()
        {
            List<byte> Buffer = new List<byte>();
            Buffer.Add(0x0F);
            Buffer.Add(0xB6);
            Buffer.Add(0x15);
            Buffer.AddRange(BitConverter.GetBytes(_Custom_Buttons_Bank_Ptr));
            WriteBytes(RELOAD_INJECTION_ADDRESS, Buffer.ToArray());
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            if (Player == 1)
            {
                //Write Axis
                WriteByte(INPUTSTRUCT_ADDRESS + INPUT_X_OFFSET, (byte)mouse.pTarget.X);
                WriteByte(INPUTSTRUCT_ADDRESS + INPUT_Y_OFFSET, (byte)mouse.pTarget.Y);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_Custom_Buttons_Bank_Ptr, 0x02);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_Custom_Buttons_Bank_Ptr, 0xFD);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_Custom_Buttons_Bank_Ptr, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_Custom_Buttons_Bank_Ptr, 0xFE);
                }
            }           
        }

        #endregion
    }
}
