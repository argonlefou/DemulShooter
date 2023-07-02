using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_WndWartran : Game
    {
        /** This is a W.I.P gzme, as for now the game can not be played **/
        /** Only for TEST menu and debug **/

        /*** MEMORY ADDRESSES **/
        private const UInt32 P1_X_OFFSET = 0x1630504;
        private const UInt32 P1_Y_OFFSET = 0x1630505;
        private const UInt32 P2_X_OFFSET = 0x1630506;
        private const UInt32 P2_Y_OFFSET = 0x1630507;
        private const UInt32 OUTOFSCREEN_OFFSET = 0x1630508;
        
        private NopStruct _Nop_Buttons = new NopStruct(0x00079797, 5);

        private const UInt32 BUTTONS_BASE_OFFSET = 0x014994F4;
        private enum ControlsButton
        {
            Test,
            Coin1,
            Coin2,
            Service,
            NA,
            P1_Start,
            P1_Trigger,
            P2_Start,
            P2_Trigger,
            P3_Start,
            P3_Trigger,
            P4_Start,
            P4_Trigger,
        };
        
        private const HardwareScanCode P1_START_KEY =  HardwareScanCode.DIK_1;    //[1]
        private const HardwareScanCode P2_START_KEY = HardwareScanCode.DIK_2;     //[2]
        private const HardwareScanCode P3_START_KEY = HardwareScanCode.DIK_3;     //[3]
        private const HardwareScanCode P4_START_KEY = HardwareScanCode.DIK_4;     //[4]
        private const HardwareScanCode COIN1_KEY = HardwareScanCode.DIK_5;        //[5]
        private const HardwareScanCode COIN2_KEY = HardwareScanCode.DIK_6;        //[6]
        private const HardwareScanCode SERVICE_KEY = HardwareScanCode.DIK_9;      //[9]
        private const HardwareScanCode TEST_KEY = HardwareScanCode.DIK_0;         //[0]

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndWartran(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "game", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("", "");

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

                    //X => [00-FF] = 255
                    //Y => [00-FF] = 255
                    double dMaxX = 255.0;
                    double dMaxY = 255.0;

                    //Inverted axis : origin in lower-right
                    PlayerData.RIController.Computed_X = Convert.ToInt32(dMaxX - Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt32(dMaxY - Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY)); 
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
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Buttons);
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary> 
        public override void SendInput(PlayerSettings PlayerData)
        {
            if (PlayerData.ID == 1)
            {
                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + P1_X_OFFSET, (byte)(PlayerData.RIController.Computed_X & 0xFF));
                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + P1_Y_OFFSET, (byte)(PlayerData.RIController.Computed_X & 0xFF));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.P1_Trigger, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.P1_Trigger, 0x00);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + OUTOFSCREEN_OFFSET, 0x80);
               if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                   Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + OUTOFSCREEN_OFFSET, 0x7F);
            }
            else if (PlayerData.ID == 2)
            {
                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + P2_X_OFFSET, (byte)(PlayerData.RIController.Computed_X & 0xFF));
                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + P2_Y_OFFSET, (byte)(PlayerData.RIController.Computed_X & 0xFF));

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.P2_Trigger, 0x01);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.P2_Trigger, 0x00);
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                    Apply_OR_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + OUTOFSCREEN_OFFSET, 0x40);
               if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                   Apply_AND_ByteMask((UInt32)_TargetProcess_MemoryBaseAddress + OUTOFSCREEN_OFFSET, 0xBF);
            }
        }

        /// <summary>
        /// Low-level mouse hook callback.
        /// For Watran, we use this to inject START, COIN and other buttons as there is no known emulator yet
        /// </summary>
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    switch (s.scanCode)
                    {
                        case P1_START_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.P1_Start, 0x01);
                            }break;
                        case P2_START_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.P2_Start, 0x01);
                            } break;
                        case P3_START_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.P3_Start, 0x01);
                            } break;
                        case P4_START_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.P4_Start, 0x01);
                            } break;
                        case COIN1_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.Coin1, 0x01);
                            } break;
                        case COIN2_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.Coin2, 0x01);
                            } break;
                        case SERVICE_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.Service, 0x01);
                            } break;
                        case TEST_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.Test, 0x01);
                            } break;
                        default:
                            break;
                    }  
                }
                else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                {
                    switch (s.scanCode)
                    {
                        case P1_START_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.P1_Start, 0x00);
                            } break;
                        case P2_START_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.P2_Start, 0x00);
                            } break;
                        case P3_START_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.P3_Start, 0x00);
                            } break;
                        case P4_START_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.P4_Start, 0x00);
                            } break;
                        case COIN1_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.Coin1, 0x00);
                            } break;
                        case COIN2_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.Coin2, 0x00);
                            } break;
                        case SERVICE_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.Service, 0x00);
                            } break;
                        case TEST_KEY:
                            {
                                WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + BUTTONS_BASE_OFFSET + (UInt32)ControlsButton.Test, 0x00);
                            } break;
                        default:
                            break;
                    }
                }
            }
            return Win32API.CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
        }

        

        #endregion

    }
}
