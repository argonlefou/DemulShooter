using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

namespace DemulShooter
{
    class Game_LindberghLgj : Game
    {
        private const int BUTTONS_INJECTION_ADDRESS = 0x0840F40C;
        private const int BUTTONS_INJECTION_RETURN_ADDRESS = 0x0840F412;

        private const string AXIS_X_NOP_ADDRESS = "0x080A9AD2|8";
        private const string AXIS_Y_NOP_ADDRESS = "0x080A97FE|8";

        private byte[] _AxisX_HexCode = new byte[] { 0xF3, 0x0F, 0x11, 0x83, 0x34, 0x01, 0x00, 0x00};

        //Base PTR to find P1 & P2 float axis values
        private const int PLAYER_AXIS_PTR_OFFSET = 0x00130824;
        private int _Player1_Float_Axis_Address = 0;
        private int _Player2_Float_Axis_Address = 0;
        private float _P1_X_Float;
        private float _P1_Y_Float;
        private float _P2_X_Float;
        private float _P2_Y_Float;

        //Base PTR to find Buttons values
        //private const int BUTTONS_PTR_OFFSET = 0x0011ED08;
        private int _Buttons_Address = 0x08BECB79;

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
                            //Reading the code to be sure that the game is fully loaded by the emulator before hacking it
                            bool GameLoaded = true;
                            byte[] AxisBuffer = ReadBytes(0x080A9AD2, 8);                            

                            for (int k = 0; k < _AxisX_HexCode.Length; k++)
                            {
                                if (_AxisX_HexCode[k] != AxisBuffer[k])
                                {
                                    GameLoaded = false;
                                    break;
                                }
                            }
                            
                            byte[] Buffer = ReadBytes((int)_TargetProcess_MemoryBaseAddress + PLAYER_AXIS_PTR_OFFSET, 4);
                            int i1 = BitConverter.ToInt32(Buffer, 0);
                            int i2 = i1;

                            Buffer = ReadBytes(i1 + 0x51C, 4);
                            i1 = BitConverter.ToInt32(Buffer, 0);
                            Buffer = ReadBytes(i2 + 0xC4, 4);
                            i2 = BitConverter.ToInt32(Buffer, 0);

                            Buffer = ReadBytes(i1 + 0x0, 4);
                            i1 = BitConverter.ToInt32(Buffer, 0);
                            Buffer = ReadBytes(i2 + 0x0, 4);
                            i2 = BitConverter.ToInt32(Buffer, 0); 



                            if (i1 != 0 && i2 != 0 && GameLoaded)
                            {
                                _Player1_Float_Axis_Address = i1 + 0x134;
                                _Player2_Float_Axis_Address = i2 + 0x134;

                                WriteLog("Player1_Axis_Address = 0x" + _Player1_Float_Axis_Address.ToString("X8"));
                                WriteLog("Player2_Axis_Address = 0x" + _Player2_Float_Axis_Address.ToString("X8"));

                                _ProcessHooked = true;
                                WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));

                                SetHack();
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
                WriteBytes(_Player1_Float_Axis_Address, BitConverter.GetBytes(_P1_X_Float));
                WriteBytes(_Player1_Float_Axis_Address + 4, BitConverter.GetBytes(_P1_Y_Float));

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_Buttons_Address, 0x02);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_Buttons_Address, 0xFD);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_Buttons_Address, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_Buttons_Address, 0xFE);
                }
            }
            else if (Player == 2)
            {
                //Write Axis
                WriteBytes(_Player2_Float_Axis_Address, BitConverter.GetBytes(_P2_X_Float));
                WriteBytes(_Player2_Float_Axis_Address + 4, BitConverter.GetBytes(_P2_Y_Float));

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_Buttons_Address + 4, 0x02);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_Buttons_Address + 4, 0xFD);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                {
                    Apply_OR_ByteMask(_Buttons_Address + 4, 0x01);
                }
                else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                {
                    Apply_AND_ByteMask(_Buttons_Address + 4, 0xFE);
                }
            }
        }

        #endregion
    }
}
