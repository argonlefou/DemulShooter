using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace DemulShooter
{
    class Game_LindberghLgj : Game
    {

        //INPUT_STRUCT offset in game
        private const int INPUT_X_OFFSET = 0x134;
        private const int INPUT_Y_OFFSET = 0x138;

        //NOP for gun axis
        private const string AXIS_X_NOP_ADDRESS = "0x080A9AD2|8";
        private const string AXIS_Y_NOP_ADDRESS = "0x080A97FE|8";

        //Codecave injection for Buttons
        private const int BUTTONS_INJECTION_ADDRESS = 0x0840F40C;
        private const int BUTTONS_INJECTION_RETURN_ADDRESS = 0x0840F412;
        
        //Base PTR to find P1 & P2 Input struct
        private const int PLAYER1_INPUT_PTR_ADDRESS = 0x087D3BB0;
        private const int PLAYER2_INPUT_PTR_ADDRESS = 0x087D3BAC;

        //Base PTR to find Buttons values
        private const int BUTTONS_ADDRESS = 0x08BECB79;   
     
        //Check instruction for game loaded
        private const int ROM_LOADED_CHECK_INSTRUCTION = 0x0807925B;

        private int _Player1_InputStruct_Address = 0;
        private int _Player2_InputStruct_Address = 0;
        private float _P1_X_Float;
        private float _P1_Y_Float;
        private float _P2_X_Float;
        private float _P2_Y_Float;             

        /// <summary>
        /// Constructor
        /// </summary>
        ///  public Naomi_Game(String DemulVersion, bool Verbose, bool DisableWindow)
        public Game_LindberghLgj(string RomName, bool Verbose)
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
                            byte[] buffer = ReadBytes(ROM_LOADED_CHECK_INSTRUCTION, 6);
                            if (buffer[0] == 0x8B && buffer[1] == 0x1D && buffer[2] == 0xB0 && buffer[3] == 0x3B && buffer[4] == 0x7D && buffer[5] == 0x08)
                            {
                                buffer = ReadBytes(PLAYER1_INPUT_PTR_ADDRESS, 4);
                                _Player1_InputStruct_Address = BitConverter.ToInt32(buffer, 0);

                                buffer = ReadBytes(PLAYER2_INPUT_PTR_ADDRESS, 4);
                                _Player2_InputStruct_Address = BitConverter.ToInt32(buffer, 0);

                                if (_Player1_InputStruct_Address != 0 && _Player2_InputStruct_Address != 0)
                                {
                                    _ProcessHooked = true;
                                    WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));                                    

                                    WriteLog("P1 InputStruct address = 0x" + _Player1_InputStruct_Address.ToString("X8"));
                                    WriteLog("P2 InputStruct address = 0x" + _Player2_InputStruct_Address.ToString("X8"));

                                    SetHack();
                                }
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
                    int TotalResX = TotalRes.Right - TotalRes.Left;
                    int TotalResY = TotalRes.Bottom - TotalRes.Top;

                    WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X => [-1 ; 1] float
                    //Y => [-1 ; 1] float

                    float X_Value = (2.0f * Mouse.pTarget.X / TotalResX) - 1.0f;
                    float Y_Value = 1.0f - (2.0f * Mouse.pTarget.Y / TotalResY);

                    if (X_Value < -1.0f)
                        X_Value = -1.0f;
                    if (Y_Value < -1.0f)
                        Y_Value = -1.0f;
                    if (X_Value > 1.0f)
                        X_Value = 1.0f;
                    if (Y_Value > 1.0f)
                        Y_Value = 1.0f;

                    if (Player == 1)
                    {
                        _P1_X_Float = X_Value;
                        _P1_Y_Float = Y_Value;
                    }
                    else if (Player == 2)
                    {
                        _P2_X_Float = X_Value;
                        _P2_Y_Float = Y_Value;
                    }
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
            SetHack_Buttons();
            SetHack_Axis();
            SetHackEnableP2();

            WriteLog("Memory Hack complete !");
            WriteLog("-");
        }
        
        private void SetHack_Axis()
        {
            SetNops(0, AXIS_X_NOP_ADDRESS);
            SetNops(0, AXIS_Y_NOP_ADDRESS);
        }

        /// <summary>
        /// Start and Trigger are on the same Byte, so we can't simply NOP, to keep Teknoparrot Start button working
        /// </summary>
        private void SetHack_Buttons()
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //cmp edx, 0
            CaveMemory.Write_StrBytes("83 FA 00");
            //je Hack
            CaveMemory.Write_StrBytes("0F 84 0E 00 00 00");
            //cmp edx, 0
            CaveMemory.Write_StrBytes("83 FA 04");
            //je Hack
            CaveMemory.Write_StrBytes("0F 84 05 00 00 00");
            //je original code
            CaveMemory.Write_StrBytes("E9 17 00 00 00");
            //Hack
            //and al, 0xF0
            CaveMemory.Write_StrBytes("24 F0");
            //and [edx+08BECB79],FFFFFF0F
            CaveMemory.Write_StrBytes("81 A2 79 CB BE 08 0F FF FF FF");
            //or [edx+08BECB79],al
            CaveMemory.Write_StrBytes("08 82 79 CB BE 08");
            //jmp exit
            CaveMemory.Write_StrBytes("E9 06 00 00 00");
            //OriginalCode
            //mov [edx+08BECB79],al
            CaveMemory.Write_StrBytes("88 82 79 CB BE 08");
            CaveMemory.Write_jmp(BUTTONS_INJECTION_RETURN_ADDRESS);

            WriteLog("Adding Buttons Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Injection de code
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - BUTTONS_INJECTION_ADDRESS - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, BUTTONS_INJECTION_ADDRESS, Buffer.ToArray(), Buffer.Count, ref bytesWritten); 
        }

        // amCreditIsEnough() => 0x084E4A2F ~~ 0x084E4AC4
        // Even though Freeplay is forced by TeknoParrot, this procedure always find "NO CREDITS" for P2
        // Replacing conditionnal Jump by single Jump force OK (for both players)
        private void SetHackEnableP2()
        {
            WriteByte(0x84E4A56, 0xEB);
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            if (Player == 1)
            {
                //Write Axis
                WriteBytes(_Player1_InputStruct_Address + INPUT_X_OFFSET, BitConverter.GetBytes(_P1_X_Float));
                WriteBytes(_Player1_InputStruct_Address + INPUT_Y_OFFSET, BitConverter.GetBytes(_P1_Y_Float));

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(BUTTONS_ADDRESS, 0x02);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    Apply_AND_ByteMask(BUTTONS_ADDRESS, 0xFD);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(BUTTONS_ADDRESS, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Apply_AND_ByteMask(BUTTONS_ADDRESS, 0xFE);
                }
            }
            else if (Player == 2)
            {
                //Write Axis
                WriteBytes(_Player2_InputStruct_Address + INPUT_X_OFFSET, BitConverter.GetBytes(_P2_X_Float));
                WriteBytes(_Player2_InputStruct_Address + INPUT_Y_OFFSET, BitConverter.GetBytes(_P2_Y_Float));

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(BUTTONS_ADDRESS + 4, 0x02);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    Apply_AND_ByteMask(BUTTONS_ADDRESS + 4, 0xFD);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(BUTTONS_ADDRESS + 4, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Apply_AND_ByteMask(BUTTONS_ADDRESS + 4, 0xFE);
                }
            }
        }

        #endregion
    }
}
