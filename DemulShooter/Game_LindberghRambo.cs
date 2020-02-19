using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace DemulShooter
{
    class Game_LindberghRambo : Game
    {
        //static load JVS data structure from code at 0x082C72B1 procedure
        private const int JVS_STRUCT_ADDRESS = 0x0ADDF360;
        private const int JVS_P1_TRIGGER_OFFSET = 0x19C;
        private const int JVS_P1_RAGE_OFFSET = 0x1A0;
        private const int JVS_P2_TRIGGER_OFFSET = 0x1A4;
        private const int JVS_P2_RAGE_OFFSET = 0x1A8;
        private const int JVS_P1_X_OFFSET = 0x1AD;
        private const int JVS_P1_Y_OFFSET = 0x1B3;
        private const int JVS_P2_X_OFFSET = 0x1B9;
        private const int JVS_P2_Y_OFFSET = 0x1BF;

        //INPUT_STRUCT offset in game
        //Hacking axis in this structure, to be sure there will be no issue with reload and aim offset
        private int _P1_InputStruct_Address_Ptr = 0;
        private int _P2_InputStruct_Address_Ptr = 0;        
        private const int INPUT_X_OFFSET = 0x04;
        private const int INPUT_Y_OFFSET = 0x05;

        //NOP for Gun init procedure
        private const string P1_X_INIT_NOP_ADDRESS = "0x08073833|4";
        private const string P1_Y_INIT_NOP_ADDRESS = "0x0807383A|4";

        //NOP for Gun axis and buttons in-game        
        //Used to NOP JVS data at source, this is used to validate coordinates On/Out of screen for reload
        private const string JVS_SRC_AXIS_NOP_ADDRESS_1 = "0x082C7E47|3";
        private const string JVS_SRC_AXIS_NOP_ADDRESS_2 = "0x082C78D3|3";
               
        //NOP for post-calculated cordinates
        private const string AXIS_X_NOP_ADDRESS = "0x08073EBB|3";
        private const string AXIS_Y_NOP_ADDRESS = "0x08073EC5|3";

        //Codecave injection : Get INPUT_STRUCT addresses from register to write into later
        private const int P1_INPUTSTRUCT_INJECTION_ADDRESS = 0x08073BC1;
        private const int P1_INPUTSTRUCT_INJECTION_RETURN_ADDRESS = 0x08073BC7;
        private const int P2_INPUTSTRUCT_INJECTION_ADDRESS = 0x08073BDE;
        private const int P2_INPUTSTRUCT_INJECTION_RETURN_ADDRESS = 0x08073BE4;
        //Codecave injection blocking gun buttons
        private const int BTN_INJECTION_ADDRESS = 0x082C7B3C;
        private const int BTN_INJECTION_RETURN_ADDRESS = 0x082C7B41;

        private const int ROM_LOADED_CHECK_INSTRUCTION = 0x08073BC7;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_LindberghRambo(string RomName, bool Verbose)
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
                            if (buffer[0] == 0xE8 && buffer[1] == 0x76 && buffer[2] == 0x01 && buffer[3] == 0x00 && buffer[4] == 0x00)
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
                    //X and Y axis => 0x00 - 0xFF                    
                    double dMaxX = 256.0;
                    double dMaxY = 256.0;

                    Mouse.pTarget.X = Convert.ToInt16(Math.Round(dMaxX * Mouse.pTarget.X / TotalResX));
                    Mouse.pTarget.Y = Convert.ToInt16(Math.Round(dMaxY * Mouse.pTarget.Y / TotalResY));
                    if (Mouse.pTarget.X < 0)
                        Mouse.pTarget.X = 0;
                    if (Mouse.pTarget.Y < 0)
                        Mouse.pTarget.Y = 0;
                    if (Mouse.pTarget.X > (int)dMaxX)
                        Mouse.pTarget.X = (int)dMaxX;
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
            SetHack_DataBank();
            SetHack_GetP1InputPtr();
            SetHack_GetP2InputPtr();
            SetHack_GunInit();
            SetHack_Btn();
            
            //These one were here to NOP axis data in JVS read procedure.
            //These values are used to compute aiming at screen with gun calibration parameters,
            //But we need to set these properly to allow shoot (on-screen values) or to allow Reload (out-of-screen values)
            //We init them on-screen and will set them out-of-screen on a right click event
            SetNops(0, JVS_SRC_AXIS_NOP_ADDRESS_1);
            SetNops(0, JVS_SRC_AXIS_NOP_ADDRESS_2);
            WriteByte(JVS_STRUCT_ADDRESS + JVS_P1_X_OFFSET, 0x80);
            WriteByte(JVS_STRUCT_ADDRESS + JVS_P1_Y_OFFSET, 0x80);
            WriteByte(JVS_STRUCT_ADDRESS + JVS_P2_X_OFFSET, 0x80);
            WriteByte(JVS_STRUCT_ADDRESS + JVS_P2_Y_OFFSET, 0x80);            

            //These one will block axis values later after calibration calculations and will give us our aim
            SetNops(0, AXIS_X_NOP_ADDRESS);
            SetNops(0, AXIS_Y_NOP_ADDRESS);

            //Change init for JVS values axis to set values to 0x80 instead of 00
            List<byte> Buffer = new List<byte>();
            Buffer.Add(0xC7);
            Buffer.Add(0x01);
            Buffer.Add(0x00);
            Buffer.Add(0x80);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            WriteBytes(0x082C7EE8, Buffer.ToArray());

            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }

        /// <summary>
        /// Creating a custom memory bank to store our data
        /// </summary>
        private void SetHack_DataBank()
        {
            //1st Codecave : storing P1 and P2 input structure data, read from register in main program code
            Memory DataCaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            DataCaveMemory.Open();
            DataCaveMemory.Alloc(0x800);

            _P1_InputStruct_Address_Ptr = DataCaveMemory.CaveAddress;
            _P2_InputStruct_Address_Ptr = DataCaveMemory.CaveAddress + 0x04;

            WriteLog("Custom data will be stored at : 0x" + _P1_InputStruct_Address_Ptr.ToString("X8"));
        }

        /// <summary>
        /// This codecave will read register at a certain instruction to get InputStruct register address for P1
        /// </summary>
        private void SetHack_GetP1InputPtr()
        {
            List<Byte> Buffer = new List<Byte>();
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx, _P1_InputStruct_Address
            CaveMemory.Write_StrBytes("BB");
            byte[] b = BitConverter.GetBytes(_P1_InputStruct_Address_Ptr);
            CaveMemory.Write_Bytes(b);
            //mov [ebx], eax
            CaveMemory.Write_StrBytes("89 03");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //mov eax, [ebp+8]
            CaveMemory.Write_StrBytes("8B 45 08");
            //mov [esp], eax
            CaveMemory.Write_StrBytes("89 04 24");
            CaveMemory.Write_jmp(P1_INPUTSTRUCT_INJECTION_RETURN_ADDRESS);

            WriteLog("Adding P1 InputStruct Codecave_1 at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - (P1_INPUTSTRUCT_INJECTION_ADDRESS) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, P1_INPUTSTRUCT_INJECTION_ADDRESS, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        }

        /// <summary>
        /// This codecave will read register at a certain instruction to get InputStruct register address for P2 
        /// </summary>
        private void SetHack_GetP2InputPtr()
        {
            List<Byte> Buffer = new List<Byte>();
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx, _P2_InputStruct_Address
            CaveMemory.Write_StrBytes("BB");
            byte[] b = BitConverter.GetBytes(_P2_InputStruct_Address_Ptr);
            CaveMemory.Write_Bytes(b);
            //mov [ebx], eax
            CaveMemory.Write_StrBytes("89 03");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //mov eax, [ebp+8]
            CaveMemory.Write_StrBytes("8B 45 08");
            //mov [esp], eax
            CaveMemory.Write_StrBytes("89 04 24");
            CaveMemory.Write_jmp(P2_INPUTSTRUCT_INJECTION_RETURN_ADDRESS);

            WriteLog("Adding P2 InputStruct Codecave_1 at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - (P2_INPUTSTRUCT_INJECTION_ADDRESS) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, P2_INPUTSTRUCT_INJECTION_ADDRESS, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        }

        // CgunMgr::Init() => 0x0807382A ~ 0x080738D1
        // Init Axis and buttons values to 0
        // Not called that often (maybe after START or CONTINUE or new level), maybe not necessary to block them
        private void SetHack_GunInit()
        {
            SetNops(0, P1_X_INIT_NOP_ADDRESS);
            SetNops(0, P1_Y_INIT_NOP_ADDRESS);
        }

        /// <summary>
        /// Gun Buttons and Start are on same memory word, so we need to filter to let Teknoparrot Start button work
        /// </summary>
        private void SetHack_Btn()
        {
            List<Byte> Buffer = new List<Byte>();
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //cmp [ebp-10], 1
            CaveMemory.Write_StrBytes("83 7D F0 01");
            //je exit
            CaveMemory.Write_StrBytes("0F 84 0A 00 00 00");
            //and al, 0x80
            CaveMemory.Write_StrBytes("24 80");
            //and [edx], 0xFFFFFF0F
            CaveMemory.Write_StrBytes("81 22 0F FF FF FF");
            //or [edx], al
            CaveMemory.Write_StrBytes("08 02");
            //Exit:
            //mov eax, [ebp-0C]
            CaveMemory.Write_StrBytes("8B 45 F4");
            //jmp back
            CaveMemory.Write_jmp(BTN_INJECTION_RETURN_ADDRESS);

            WriteLog("Adding Buttons Codecave_1 at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - (BTN_INJECTION_ADDRESS) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32.WriteProcessMemory((int)ProcessHandle, BTN_INJECTION_ADDRESS, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            int InputStructAddress = 0;
            byte[] buffer;

            if (Player == 1)
            {
                try
                {
                    buffer = ReadBytes(_P1_InputStruct_Address_Ptr, 4);
                    InputStructAddress = BitConverter.ToInt32(buffer, 0);

                    if (InputStructAddress != 0)
                    {
                        WriteByte(InputStructAddress + INPUT_X_OFFSET, (byte)mouse.pTarget.X);
                        WriteByte(InputStructAddress + INPUT_Y_OFFSET, (byte)mouse.pTarget.Y);

                        //Inputs
                        if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                        {
                            Apply_OR_ByteMask(JVS_STRUCT_ADDRESS + JVS_P1_TRIGGER_OFFSET, 0x02);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                        {
                            Apply_AND_ByteMask(JVS_STRUCT_ADDRESS + JVS_P1_TRIGGER_OFFSET, 0xFD);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                        {
                            Apply_OR_ByteMask(JVS_STRUCT_ADDRESS + JVS_P1_RAGE_OFFSET, 0x80);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                        {
                            Apply_AND_ByteMask(JVS_STRUCT_ADDRESS + JVS_P1_RAGE_OFFSET, 0x0F);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                        {
                            WriteByte(JVS_STRUCT_ADDRESS + JVS_P1_X_OFFSET, 0xFF);
                            WriteByte(JVS_STRUCT_ADDRESS + JVS_P1_Y_OFFSET, 0xFF);
                            Apply_OR_ByteMask(JVS_STRUCT_ADDRESS + JVS_P1_TRIGGER_OFFSET, 0x01);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                        {
                            WriteByte(JVS_STRUCT_ADDRESS + JVS_P1_X_OFFSET, 0x80);
                            WriteByte(JVS_STRUCT_ADDRESS + JVS_P1_Y_OFFSET, 0x80);
                            Apply_AND_ByteMask(JVS_STRUCT_ADDRESS + JVS_P1_TRIGGER_OFFSET, 0xFE);
                        }
                    }
                }
                catch
                { }
            }
            else if (Player == 2)
            {
                try
                {
                    buffer = ReadBytes(_P2_InputStruct_Address_Ptr, 4);
                    InputStructAddress = BitConverter.ToInt32(buffer, 0);

                    if (InputStructAddress != 0)
                    {
                        WriteByte(InputStructAddress + INPUT_X_OFFSET, (byte)mouse.pTarget.X);
                        WriteByte(InputStructAddress + INPUT_Y_OFFSET, (byte)mouse.pTarget.Y);

                        //Inputs
                        if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                        {
                            Apply_OR_ByteMask(JVS_STRUCT_ADDRESS + JVS_P2_TRIGGER_OFFSET, 0x02);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                        {
                            Apply_AND_ByteMask(JVS_STRUCT_ADDRESS + JVS_P2_TRIGGER_OFFSET, 0xFD);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                        {
                            Apply_OR_ByteMask(JVS_STRUCT_ADDRESS + JVS_P2_RAGE_OFFSET, 0x80);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                        {
                            Apply_AND_ByteMask(JVS_STRUCT_ADDRESS + JVS_P2_RAGE_OFFSET, 0x0F);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                        {
                            WriteByte(JVS_STRUCT_ADDRESS + JVS_P2_X_OFFSET, 0xFF);
                            WriteByte(JVS_STRUCT_ADDRESS + JVS_P2_Y_OFFSET, 0xFF);
                            Apply_OR_ByteMask(JVS_STRUCT_ADDRESS + JVS_P2_TRIGGER_OFFSET, 0x01);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                        {
                            WriteByte(JVS_STRUCT_ADDRESS + JVS_P2_X_OFFSET, 0x80);
                            WriteByte(JVS_STRUCT_ADDRESS + JVS_P2_Y_OFFSET, 0x80);
                            Apply_AND_ByteMask(JVS_STRUCT_ADDRESS + JVS_P2_TRIGGER_OFFSET, 0xFE);
                        }
                    }
                }
                catch
                { }
            }
        }

        #endregion
    }
}
