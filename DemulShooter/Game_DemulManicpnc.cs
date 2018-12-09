using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;

namespace DemulShooter
{
    class Game_DemulManicpnc : Game
    {
        private IntPtr _GpuModuleBaseAddress = IntPtr.Zero;
        private int _GpuDisplayType_Offset = 0;

        /*** MEMORY ADDRESSES **/
        private int _Injection_Offset       =   0x001A1355;
        private int _Injection_ReturnOffset =   0x001A1361;
        private int _P1_Data;
        private int _P2_Data;

        private int[] _Btn_Status = new int[] { 0, 0};
        private int[] _TriggerPushed = new int[] { 0, 0 };
        private Timer[] _TimerHoldTrigger = new Timer[2];

        public Game_DemulManicpnc(String RomName, bool Verbose, bool DisableWindow, bool WidescreenHack)
            : base()
        {
            GetScreenResolution();

            _RomName = RomName;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = "demul";

            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            _TimerHoldTrigger[0] = new Timer();
            _TimerHoldTrigger[0].Interval = 20;
            _TimerHoldTrigger[0].Tick += new EventHandler(tHoldTriggerP1_Tick);
            _TimerHoldTrigger[0].Enabled = true;

            _TimerHoldTrigger[1] = new Timer();
            _TimerHoldTrigger[1].Interval = 20;
            _TimerHoldTrigger[1].Tick += new EventHandler(tHoldTriggerP2_Tick);
            _TimerHoldTrigger[1].Enabled = true;

            WriteLog("Waiting for Demul Game " + _RomName + " game to hook.....");
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

                        ProcessModuleCollection c = _TargetProcess.Modules;
                        foreach (ProcessModule m in c)
                        {
                            if (m.ModuleName.ToLower().Equals("gpudx11.dll"))
                            {
                                _GpuModuleBaseAddress = m.BaseAddress;
                                WriteLog("gpuDX11.dll base address = 0x" + _GpuModuleBaseAddress.ToString("X8"));
                                _GpuDisplayType_Offset = 0x0007F9DC;
                                break;
                            }
                            else if (m.ModuleName.ToLower().Equals("gpudx11old.dll"))
                            {
                                _GpuModuleBaseAddress = m.BaseAddress;
                                WriteLog("gpuDX11old.dll base address = 0x" + _GpuModuleBaseAddress.ToString("X8"));
                                _GpuDisplayType_Offset = 0x0005F920;
                                break;
                            }
                            else if (m.ModuleName.ToLower().StartsWith("gpudx") && m.ModuleName.ToLower().EndsWith(".dll"))
                            {
                                _GpuModuleBaseAddress = m.BaseAddress;
                                WriteLog("Only found " + m.ModuleName.ToLower() + " loaded. Incompatible module, reverting to old method");
                            }
                        }

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero && _GpuModuleBaseAddress != IntPtr.Zero)
                        {
                            _ProcessHooked = true;
                            if (_DisableWindow)
                                //Disabling left-click for resize-bug upper left corner
                                //DisableWindow(true);                                        
                                ApplyMouseHook();
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

        #region Screen

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// For These 2 games, there are no calibration available so we face a ratio issue problem
        /// Exemple : 16/9 monitor + 4/3 option in demul in fullscreen : aim is not good because of black borders
        /// 
        /// To fix it, we try to read the setting (4/3, 16/9 or stretch) and resolution in demul's memory (in gpuDX11.dll)
        /// this way, we can do some math to know the exact position
        /// </summary>
        public override bool GameScale(MouseInfo Mouse, int Player)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                //if no gpudx11.dll used or not found, default behavior with black border issue
                if (_GpuModuleBaseAddress == IntPtr.Zero)
                {
                    try
                    {
                        //Demul Window size
                        Win32.Rect TotalRes = new Win32.Rect();
                        Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                        double TotalResX = TotalRes.Right - TotalRes.Left;
                        double TotalResY = TotalRes.Bottom - TotalRes.Top;

                        WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                        double dMaxX = 640;
                        double dMaxY = 480;

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
                else
                {
                    try
                    {
                        //Display option in demul menu : 0=Stretch / 1=4:3 / 2 = 16:9
                        byte DisplayType = ReadByte((int)_GpuModuleBaseAddress + _GpuDisplayType_Offset);;
                        WriteLog("Demul display type is : " + DisplayType.ToString());
                        
                        //Demul Window size
                        Win32.Rect TotalRes = new Win32.Rect();
                        Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                        double TotalResX = TotalRes.Right - TotalRes.Left;
                        double TotalResY = TotalRes.Bottom - TotalRes.Top;

                        WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                       
                        //If stretch the whole window is used so, no change 
                        //If 4:3 we keep correct Y but have to change X because of black borders
                        if (DisplayType == 1)
                        {
                            double RealX = TotalResY * 4.0 / 3.0;
                            WriteLog("Game real resolution (Px) = [ " + RealX.ToString() + "x" + TotalResY.ToString() + " ]");
                            Mouse.pTarget.X -= ((int)TotalResX - (int)RealX) / 2;
                            TotalResX = RealX;
                        }
                        //If 6:9 we keep the correct X but we have to change Y because of black borders
                        if (DisplayType == 2)
                        {
                            double RealY = TotalResX * 9.0 / 16.0;
                            WriteLog("Game real resolution (Px) = [ " + TotalResX.ToString() + "x" + RealY.ToString() + " ]");
                            Mouse.pTarget.Y -= ((int)TotalResY - (int)RealY) / 2;
                            TotalResY = RealY;
                        }
                        
                        double dMaxX = 640;
                        double dMaxY = 480;

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
            }
            return false;
        }

        #endregion

        protected void SetHack()
        {
            //Creating data bank
            //Codecave :
            Memory CaveMemoryInput = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemoryInput.Open();
            CaveMemoryInput.Alloc(0x800);
            _P1_Data = CaveMemoryInput.CaveAddress;
            _P2_Data = CaveMemoryInput.CaveAddress + 0x03;
            WriteLog("Custom data will be stored at : 0x" + _P1_Data.ToString("X8"));


            /************ Replace data by ours when writing to IO board *****/
           
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov ecx, []P1_data
            byte[] b = BitConverter.GetBytes(_P1_Data);
            CaveMemory.Write_StrBytes("8B 0D");
            CaveMemory.Write_Bytes(b);
            //call demul.exe+1A1400
            CaveMemory.Write_call((int)_TargetProcess.MainModule.BaseAddress + 0x001A1400);
            //mov ecx, [P2_data]
            b = BitConverter.GetBytes(_P2_Data);
            CaveMemory.Write_StrBytes("8B 0D");
            CaveMemory.Write_Bytes(b);
            //call demul.exe+1A1400
            CaveMemory.Write_call((int)_TargetProcess.MainModule.BaseAddress + 0x001A1400);
            //return
            CaveMemory.Write_jmp((int)_TargetProcess.MainModule.BaseAddress + _Injection_ReturnOffset);

            WriteLog("Adding CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((int)_TargetProcess.MainModule.BaseAddress + _Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_TargetProcess.MainModule.BaseAddress + _Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);
            
            WriteLog("Pokasuka Memory Hack complete !");
            WriteLog("-");
        }

        /// <summary>
        /// For this one, P2 is totally disabled in Demul emulation
        /// Coordinates by player are 2x2bytes, encoded on 3 bytes. So we have to recreate the encoding to inject it on the IO board emulation
        /// The total output is : 6 bytes containing Buttons+X+Y for each players
        /// </summary>
        public override void SendInput(MouseInfo mouse, int Player)
        {
            int Data = 0; 
            int CurrentBtnStatus;

            if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                _Btn_Status[Player - 1] |= 1;
            else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
            {
                _Btn_Status[Player - 1] &= 0xE;
                _TriggerPushed[Player - 1] = 0;
                _TimerHoldTrigger[Player - 1].Stop();  
            }
            else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_DOWN)
                _Btn_Status[Player - 1] |= 2;
            else if (mouse.button == Win32.RI_MOUSE_MIDDLE_BUTTON_UP)
                _Btn_Status[Player - 1] &= 0xD;

            Data = _Btn_Status[Player - 1];
            Data &= 0x01;
            Data = ~Data + 1;
            Data &= 0x00000C00;

            CurrentBtnStatus = _Btn_Status[Player - 1];
            if ((CurrentBtnStatus & 0x01) != 0)
            {
                if (_TriggerPushed[Player - 1] == 0)
                {
                    _TimerHoldTrigger[Player - 1].Start();  
                }
                else
                {
                    CurrentBtnStatus &= 0xFE;
                }
            }

            CurrentBtnStatus = CurrentBtnStatus << 0x0C;
            Data |= CurrentBtnStatus;
            Data |= mouse.pTarget.Y;
            Data = Data << 0xA;
            Data |= mouse.pTarget.X;

            byte[] buffer = BitConverter.GetBytes(Data);
           
            if (Player == 1)
            {
                WriteByte(_P1_Data, buffer[0]);
                WriteByte(_P1_Data + 1, buffer[1]);
                WriteByte(_P1_Data + 2, buffer[2]);           
            }
            else if (Player == 2)
            {

                WriteByte(_P2_Data, buffer[0]);
                WriteByte(_P2_Data + 1, buffer[1]);
                WriteByte(_P2_Data + 2, buffer[2]);
            }    
        }

        /// <summary>
        /// To handle "drag and drop" on the game, we need to handle differently a first pull on trigger
        /// And a continous pull
        /// Fir this I'm using a timer to switch from the 1st state to the 2nd one because without it, the game is
        /// not registering the first trigger press and nothing works
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="e"></param>
        private void tHoldTriggerP1_Tick(Object Sender, EventArgs e)
        {
            _TriggerPushed[0] = 1;
            _TimerHoldTrigger[0].Stop();
        }
        private void tHoldTriggerP2_Tick(Object Sender, EventArgs e)
        {
            _TriggerPushed[1] = 1;
            _TimerHoldTrigger[1].Stop();
        }
    }
}
