using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_DemulManicpnc : Game
    {
        private IntPtr _GpuModuleBaseAddress = IntPtr.Zero;
        private UInt32 _GpuDisplayType_Offset = 0;

        /*** MEMORY ADDRESSES **/
        private UInt32 _Injection_Offset       = 0x001A1355;
        private UInt32 _Injection_ReturnOffset = 0x001A1361;
        private UInt32 _P1_Data_CaveAddress;
        private UInt32 _P2_Data_CaveAddress;

        private int[] _Btn_Status = new int[] { 0, 0};
        private int[] _TriggerPushed = new int[] { 0, 0 };
        private Timer[] _TimerHoldTrigger = new Timer[2];

        public Game_DemulManicpnc(String RomName, bool DisableInputHack, bool Verbose, bool DisableWindow, bool WidescreenHack)
            : base(RomName, "demul", DisableInputHack, Verbose)
        {
            _DisableWindow = DisableWindow;
            _tProcess.Start();

            _TimerHoldTrigger[0] = new Timer();
            _TimerHoldTrigger[0].Interval = 20;
            _TimerHoldTrigger[0].Tick += new EventHandler(tHoldTriggerP1_Elapsed);
            _TimerHoldTrigger[0].Enabled = true;

            _TimerHoldTrigger[1] = new Timer();
            _TimerHoldTrigger[1].Interval = 20;
            _TimerHoldTrigger[1].Tick += new EventHandler(tHoldTriggerP2_Elapsed);
            _TimerHoldTrigger[1].Enabled = true;

            Logger.WriteLog("Waiting for Demul Game " + _RomName + " game to hook.....");
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

                        ProcessModuleCollection c = _TargetProcess.Modules;
                        foreach (ProcessModule m in c)
                        {
                            if (m.ModuleName.ToLower().Equals("gpudx11.dll"))
                            {
                                _GpuModuleBaseAddress = m.BaseAddress;
                                Logger.WriteLog("gpuDX11.dll base address = 0x" + _GpuModuleBaseAddress.ToString("X8"));
                                _GpuDisplayType_Offset = 0x0007F9DC;
                                break;
                            }
                            else if (m.ModuleName.ToLower().Equals("gpudx11old.dll"))
                            {
                                _GpuModuleBaseAddress = m.BaseAddress;
                                Logger.WriteLog("gpuDX11old.dll base address = 0x" + _GpuModuleBaseAddress.ToString("X8"));
                                _GpuDisplayType_Offset = 0x0005F920;
                                break;
                            }
                            else if (m.ModuleName.ToLower().StartsWith("gpudx") && m.ModuleName.ToLower().EndsWith(".dll"))
                            {
                                _GpuModuleBaseAddress = m.BaseAddress;
                                Logger.WriteLog("Only found " + m.ModuleName.ToLower() + " loaded. Incompatible module, reverting to old method");
                            }
                        }

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero && _GpuModuleBaseAddress != IntPtr.Zero)
                        {
                            _GameWindowHandle = _TargetProcess.MainWindowHandle;
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            if (!_DisableInputHack)
                                Apply_InputsMemoryHack();
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
        /// For These 2 games, there are no calibration available so we face a ratio issue problem
        /// Exemple : 16/9 monitor + 4/3 option in demul in fullscreen : aim is not good because of black borders
        /// 
        /// To fix it, we try to read the setting (4/3, 16/9 or stretch) and resolution in demul's memory (in gpuDX11.dll)
        /// this way, we can do some math to know the exact position
        /// </summary>
        public override bool GameScale(PlayerSettings PlayerData)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                //if no gpudx11.dll used or not found, default behavior with black border issue
                if (_GpuModuleBaseAddress == IntPtr.Zero)
                {
                    try
                    {
                        double TotalResX = _ClientRect.Right - _ClientRect.Left;
                        double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                        Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                        double dMaxX = 640;
                        double dMaxY = 480;

                        PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                        PlayerData.RIController.Computed_Y = Convert.ToInt16(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));

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
                else
                {
                    try
                    {
                        //Display option in demul menu : 0=Stretch / 1=4:3 / 2 = 16:9
                        byte DisplayType = ReadByte((UInt32)_GpuModuleBaseAddress + _GpuDisplayType_Offset);;
                        Logger.WriteLog("Demul display type is : " + DisplayType.ToString());

                        double TotalResX = _ClientRect.Right - _ClientRect.Left;
                        double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                        Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                       
                        //If stretch the whole window is used so, no change 
                        //If 4:3 we keep correct Y but have to change X because of black borders
                        if (DisplayType == 1)
                        {
                            double RealX = TotalResY * 4.0 / 3.0;
                            Logger.WriteLog("Game real resolution (Px) = [ " + RealX.ToString() + "x" + TotalResY.ToString() + " ]");
                            PlayerData.RIController.Computed_X -= ((int)TotalResX - (int)RealX) / 2;
                            TotalResX = RealX;
                        }
                        //If 6:9 we keep the correct X but we have to change Y because of black borders
                        if (DisplayType == 2)
                        {
                            double RealY = TotalResX * 9.0 / 16.0;
                            Logger.WriteLog("Game real resolution (Px) = [ " + TotalResX.ToString() + "x" + RealY.ToString() + " ]");
                            PlayerData.RIController.Computed_Y -= ((int)TotalResY - (int)RealY) / 2;
                            TotalResY = RealY;
                        }
                        
                        double dMaxX = 640;
                        double dMaxY = 480;

                        PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                        PlayerData.RIController.Computed_Y = Convert.ToInt16(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));

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
            }
            return false;
        }

        #endregion

        #region Memory Hack

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _P1_Data_CaveAddress = _InputsDatabank_Address;
            _P2_Data_CaveAddress = _InputsDatabank_Address + 0x03;
        }

        /// <summary>
        /// Replace data by ours when writing to IO board
        /// </summary>
        private void SetHack_ButtonsAndAxis()
        {          
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov ecx, []P1_data
            byte[] b = BitConverter.GetBytes(_P1_Data_CaveAddress);
            CaveMemory.Write_StrBytes("8B 0D");
            CaveMemory.Write_Bytes(b);
            //call demul.exe+1A1400
            CaveMemory.Write_call((UInt32)_TargetProcess.MainModule.BaseAddress + 0x001A1400);
            //mov ecx, [P2_data]
            b = BitConverter.GetBytes(_P2_Data_CaveAddress);
            CaveMemory.Write_StrBytes("8B 0D");
            CaveMemory.Write_Bytes(b);
            //call demul.exe+1A1400
            CaveMemory.Write_call((UInt32)_TargetProcess.MainModule.BaseAddress + 0x001A1400);
            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _Injection_ReturnOffset);

            Logger.WriteLog("Adding CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
            
            Logger.WriteLog("Pokasuka Memory Hack complete !");
            Logger.WriteLog("-");
        }

        #endregion

        #region Inputs

        /// <summary>
        /// For this one, P2 is totally disabled in Demul emulation
        /// Coordinates by player are 2x2bytes, encoded on 3 bytes. So we have to recreate the encoding to inject it on the IO board emulation
        /// The total output is : 6 bytes containing Buttons+X+Y for each players
        /// </summary>
        public override void SendInput(PlayerSettings PlayerData)
        {
            int Data = 0; 
            int CurrentBtnStatus;

            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                _Btn_Status[PlayerData.ID - 1] |= 1;
            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
            {
                _Btn_Status[PlayerData.ID - 1] &= 0xE;
                _TriggerPushed[PlayerData.ID - 1] = 0;
                _TimerHoldTrigger[PlayerData.ID - 1].Stop();  
            }

            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                _Btn_Status[PlayerData.ID - 1] |= 2;
            if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                _Btn_Status[PlayerData.ID - 1] &= 0xD;

            Data = _Btn_Status[PlayerData.ID - 1];
            Data &= 0x01;
            Data = ~Data + 1;
            Data &= 0x00000C00;

            CurrentBtnStatus = _Btn_Status[PlayerData.ID - 1];
            if ((CurrentBtnStatus & 0x01) != 0)
            {
                if (_TriggerPushed[PlayerData.ID - 1] == 0)
                {
                    _TimerHoldTrigger[PlayerData.ID - 1].Start();  
                }
                else
                {
                    CurrentBtnStatus &= 0xFE;
                }
            }

            CurrentBtnStatus = CurrentBtnStatus << 0x0C;
            Data |= CurrentBtnStatus;
            Data |= PlayerData.RIController.Computed_Y;
            Data = Data << 0xA;
            Data |= PlayerData.RIController.Computed_X;

            byte[] buffer = BitConverter.GetBytes(Data);
           
            if (PlayerData.ID == 1)
            {
                WriteByte(_P1_Data_CaveAddress, buffer[0]);
                WriteByte(_P1_Data_CaveAddress + 1, buffer[1]);
                WriteByte(_P1_Data_CaveAddress + 2, buffer[2]);           
            }
            else if (PlayerData.ID == 2)
            {

                WriteByte(_P2_Data_CaveAddress, buffer[0]);
                WriteByte(_P2_Data_CaveAddress + 1, buffer[1]);
                WriteByte(_P2_Data_CaveAddress + 2, buffer[2]);
            }    
        }

        /// <summary>
        ///  Mouse callback for low level hook
        ///  This is used to block LeftClick events on the window, because double clicking on the upper-left corner
        ///  makes demul switch from Fullscreen to Windowed mode
        /// </summary>        
        public override IntPtr MouseHookCallback(IntPtr MouseHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (UInt32)wParam == Win32Define.WM_LBUTTONDOWN)
            {
                //Just blocking left clicks
                if (_DisableWindow)
                    return new IntPtr(1);
            }
            return Win32API.CallNextHookEx(MouseHookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// To handle "drag and drop" on the game, we need to handle differently a first pull on trigger
        /// And a continous pull
        /// For this I'm using a timer to switch from the 1st state to the 2nd one because without it, the game is
        /// not registering the first trigger press and nothing works
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="e"></param>
        private void tHoldTriggerP1_Elapsed(Object Sender, EventArgs e)
        {
            _TriggerPushed[0] = 1;
            _TimerHoldTrigger[0].Stop();
        }
        private void tHoldTriggerP2_Elapsed(Object Sender, EventArgs e)
        {
            _TriggerPushed[1] = 1;
            _TimerHoldTrigger[1].Stop();
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_R, OutputId.P1_Lmp_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_B, OutputId.P1_Lmp_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_R, OutputId.P2_Lmp_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_B, OutputId.P2_Lmp_B));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //From demul.exe+16E9F2 instruction bloc
            UInt32 OutputsAddress = ((UInt32)_TargetProcess_MemoryBaseAddress + 0x4F57F00) + ((UInt32)_TargetProcess_MemoryBaseAddress + 0x3219D28);
            OutputsAddress = ReadPtr(ReadPtr(OutputsAddress + 0x04)) + 0x01;
            Logger.WriteLog("Outputs Address = 0x" + OutputsAddress.ToString("X8"));

            //[FF FF]
            //P1 Red 0-F = Output Address
            //P1 Blue 0-F = Output Address
            //P2 Red 0-F = Output Address +1
            //P2 Blue 0-F = Output Address +1
            //Genuine Outputs
            SetOutputValue(OutputId.P1_Lmp_R, ReadByte(OutputsAddress) >> 4 & 0x0F);
            SetOutputValue(OutputId.P1_Lmp_B, ReadByte(OutputsAddress) & 0x0F);
            SetOutputValue(OutputId.P2_Lmp_R, ReadByte(OutputsAddress + 1) >> 4 & 0x0F);
            SetOutputValue(OutputId.P2_Lmp_B, ReadByte(OutputsAddress + 1) & 0x0F);

            SetOutputValue(OutputId.Credits, ReadByte(0x20200010));
        }
        
        #endregion
    }
}
