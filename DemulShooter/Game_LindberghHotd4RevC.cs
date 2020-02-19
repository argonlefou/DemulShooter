using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace DemulShooter
{
    class Game_LindberghHotd4RevC : Game
    {
        private int _P1_InputStruct_Address_Ptr = 0;
        private int _P2_InputStruct_Address_Ptr = 0;

        //INPUT_STRUCT offset in game
        private const int INPUT_X_OFFSET = 0x04;
        private const int INPUT_Y_OFFSET = 0x05;
        private const int INPUT_TRIGGER_OFFSET = 0x08;
        private const int INPUT_RELOAD_OFFSET = 0x0C;
        private const int INPUT_WEAPONBTN_OFFSET = 0x10;

        //NOP for Gun init procedure
        private const string P1_X_INIT_NOP_ADDRESS          = "0x081C9ED5|3";
        private const string P1_Y_INIT_NOP_ADDRESS          = "0x081C9ED8|3";
        private const string P1_TRIGGER_INIT_NOP_ADDRESS    = "0x081C9EDB|3";
        private const string P1_RELOAD_INIT_NOP_ADDRESS     = "0x081C9EDE|3";
        private const string P1_WEAPONBTN_INIT_NOP_ADDRESS  = "0x081C9EE1|3";

        //NOP for Gun axis and buttons in-game
        private const string AXIS_X_NOP_ADDRESS     = "0x081536EF|3";
        private const string AXIS_Y_NOP_ADDRESS     = "0x081536F7|3";
        private const string TRIGGER_NOP_ADDRESS    = "0x0815337D|3";
        private const string RELOAD_NOP_ADDRESS_1   = "0x08153380|3";
        private const string RELOAD_NOP_ADDRESS_2   = "0x08153714|7";
        private const string WEAPONBTN_NOP_ADDRESS  = "0x08153383|3";

        //Codecave injection : Get INPUT_STRUCT addresses from register to write into later
        private const int P1_INPUTSTRUCT_INJECTION_ADDRESS = 0x0815306A;
        private const int P1_INPUTSTRUCT_INJECTION_RETURN_ADDRESS = 0x0815306F;
        private const int P2_INPUTSTRUCT_INJECTION_ADDRESS = 0x08153076;
        private const int P2_INPUTSTRUCT_INJECTION_RETURN_ADDRESS = 0x0815307B;

        private const int ROM_LOADED_CHECK_INSTRUCTION = 0x08153065;

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_LindberghHotd4RevC(string RomName, bool Verbose)
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
                            if (buffer[0] == 0xE8 && buffer[1] == 0x42 && buffer[2] == 0x0D && buffer[3] == 0x00 && buffer[4] == 0x00)
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
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;

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

            SetHackEnableP2();

            SetNops(0, AXIS_X_NOP_ADDRESS);
            SetNops(0, AXIS_Y_NOP_ADDRESS);
            SetNops(0, TRIGGER_NOP_ADDRESS);
            SetNops(0, RELOAD_NOP_ADDRESS_1);
            SetNops(0, RELOAD_NOP_ADDRESS_2);
            SetNops(0, WEAPONBTN_NOP_ADDRESS);

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
        /// This codecave will read register at a certain instruction to get Input register address for P1
        /// </summary>
        private void SetHack_GetP1InputPtr()
        {
            List<Byte> Buffer = new List<Byte>();
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //lea edx, [esi+34]
            CaveMemory.Write_StrBytes("8D 56 34");
            //mov eax, esi
            CaveMemory.Write_StrBytes("89 F0");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx, _P1_InputStruct_Address
            CaveMemory.Write_StrBytes("BB");
            byte[] b = BitConverter.GetBytes(_P1_InputStruct_Address_Ptr);
            CaveMemory.Write_Bytes(b);
            //mov [ebx], eax
            CaveMemory.Write_StrBytes("89 13");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
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
            Win32.WriteProcessMemory((int)ProcessHandle, P1_INPUTSTRUCT_INJECTION_ADDRESS, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        }


        /// <summary>
        /// This codecave will read register at a certain instruction to get Input register address for P2
        /// </summary>
        private void SetHack_GetP2InputPtr()
        {
            List<Byte> Buffer = new List<Byte>();
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //lea edx, [esi+58]
            CaveMemory.Write_StrBytes("8D 56 58");
            //mov eax, esi
            CaveMemory.Write_StrBytes("89 F0");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx, _P2_InputStruct_Address
            CaveMemory.Write_StrBytes("BB");
            byte[] b = BitConverter.GetBytes(_P2_InputStruct_Address_Ptr);
            CaveMemory.Write_Bytes(b);
            //mov [ebx], edx
            CaveMemory.Write_StrBytes("89 13");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
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
            Win32.WriteProcessMemory((int)ProcessHandle, P2_INPUTSTRUCT_INJECTION_ADDRESS, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
        }

        // CgunMgr::Init() => 0x081C95F4 ~ 0x081C9619
        // Init Axis and buttons values to 0
        // Not called that often (maybe after START or CONTINUE or new level), maybe not necessary to block them
        private void SetHack_GunInit()
        {
            SetNops(0, P1_X_INIT_NOP_ADDRESS);
            SetNops(0, P1_Y_INIT_NOP_ADDRESS);
            //SetNops(0, P1_TRIGGER_INIT_NOP_ADDRESS);
            //SetNops(0, P1_RELOAD_INIT_NOP_ADDRESS);
            //SetNops(0, P1_WEAPONBTN_INIT_NOP_ADDRESS);
        }

        //BLocking Gun axis and buttons update during the game
        private void SetHackV2()
        {
            //Axis blocking
            SetNops(0, "0x08152EDB|3");
            SetNops(0, "0x08152EE3|3");
            //Trigger
            SetNops(0, "0x08152B69|3");
            //Weapon
            SetNops(0, "0x08152B6F|3");
            //Reload
            SetNops(0, "0x08152B6C|3");
            SetNops(0, "0x08152F00|7");
        }

        // amCreditIsEnough() => 0x0831D800 ~~ 0x0831D895
        // Even though Freeplay is forced by TeknoParrot, this procedure always find "NO CREDITS" for P2
        // Replacing conditionnal Jump by single Jump force OK (for both players)
        private void SetHackEnableP2()
        {
            WriteByte(0x831d827, 0xEB);
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
                            WriteByte(InputStructAddress + INPUT_TRIGGER_OFFSET, 0x01);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                        {
                            WriteByte(InputStructAddress + INPUT_TRIGGER_OFFSET, 0x00);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                        {
                            WriteByte(InputStructAddress + INPUT_WEAPONBTN_OFFSET, 0x01);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                        {
                            WriteByte(InputStructAddress + INPUT_WEAPONBTN_OFFSET, 0x00);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                        {
                            WriteByte(InputStructAddress + INPUT_RELOAD_OFFSET, 0x01);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                        {
                            WriteByte(InputStructAddress + INPUT_RELOAD_OFFSET, 0x00);
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
                            WriteByte(InputStructAddress + INPUT_TRIGGER_OFFSET, 0x01);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                        {
                            WriteByte(InputStructAddress + INPUT_TRIGGER_OFFSET, 0x00);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                        {
                            WriteByte(InputStructAddress + INPUT_WEAPONBTN_OFFSET, 0x01);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                        {
                            WriteByte(InputStructAddress + INPUT_WEAPONBTN_OFFSET, 0x00);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                        {
                            WriteByte(InputStructAddress + INPUT_RELOAD_OFFSET, 0x01);
                        }
                        else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                        {
                            WriteByte(InputStructAddress + INPUT_RELOAD_OFFSET, 0x00);
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
