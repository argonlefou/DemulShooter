using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;

namespace DemulShooter
{
    class Game_DemulAtomiswave : Game
    {
        /*** Process variables **/
        private IntPtr _PadDemul_ModuleBaseAddress = IntPtr.Zero;
        private IntPtr _GpuModuleBaseAddress = IntPtr.Zero;
        private int _GpuDisplayType_Offset = 0;

        /*** MEMORY ADDRESSES **/
        protected int _Paddemul_Injection_Offset            =   0x0002757A;
        protected int _Paddemul_Injection_Return_Offset     =   0x00027582;
        protected int _Paddemul_PtrButtons_Offset           =   0x00037E30;
        protected int _Paddemul_P1_Buttons_Offset           =   0x00037E32;
        protected int _Paddemul_Aw_P1_Start_Button_Offset   =   0x00037E30;
        protected int _Paddemul_Aw_P1_Fire_Button_Offset    =   0x00037E32;
        protected int _Paddemul_P1_X_Offset                 =   0x00037E34;
        protected int _Paddemul_P1_Y_Offset                 =   0x00037E36;
        protected int _Paddemul_P2_Buttons_Offset           =   0x00037EB2;
        protected int _Paddemul_Aw_P2_Start_Button_Offset   =   0x00037EB0;
        protected int _Paddemul_Aw_P2_Fire_Button_Offset    =   0x00037EB2;
        protected int _Paddemul_P2_X_Offset                 =   0x00037EB4;
        protected int _Paddemul_P2_Y_Offset                 =   0x00037EB6;

        protected bool _WidescreenHack;
        private List<WidescreenData> _ListWidescreenHacks;
       

        public Game_DemulAtomiswave(String Rom, String DemulVersion, bool Verbose, bool DisableWindow, bool WidescreenHack)
            : base()
        {
            GetScreenResolution();
            _RomName = Rom;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _DisableWindow = DisableWindow;
            _WidescreenHack = WidescreenHack;
            _Target_Process_Name = "demul";
            _ListWidescreenHacks = new List<WidescreenData>();

            _tProcess = new Timer();
            _tProcess.Interval = 500;
            _tProcess.Tick += new EventHandler(tProcess_Tick);
            _tProcess.Enabled = true;
            _tProcess.Start();

            WriteLog("Waiting for Demul Atomiswave game " + _RomName + " to hook.....");


            if (_WidescreenHack)
            {
                //ReadWidescreenData();
            }
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
                            if (m.ModuleName.ToLower().Equals("paddemul.dll"))
                            {
                                _PadDemul_ModuleBaseAddress = m.BaseAddress;
                                break;
                            }
                        }
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

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero && _GpuModuleBaseAddress != IntPtr.Zero && _PadDemul_ModuleBaseAddress != IntPtr.Zero)
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
        /// For These games, there are no calibration available so we face a ratio issue problem
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
                        byte DisplayType = ReadByte((int)_GpuModuleBaseAddress + _GpuDisplayType_Offset); ;
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
            }
            return false;
        }

        #endregion

        #region Memory Hack

        private void SetHack()
        {
            Memory CaveMemory = new Memory(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //cmp ecx, 2
            CaveMemory.Write_StrBytes("83 F9 02");
            //je @
            CaveMemory.Write_StrBytes("0F 84 47 00 00 00");
            //cmp ecx, 3
            CaveMemory.Write_StrBytes("83 F9 03");
            //je @
            CaveMemory.Write_StrBytes("0F 84 3E 00 00 00");
            //cmp ecx, 42
            CaveMemory.Write_StrBytes("83 F9 42");
            //je @
            CaveMemory.Write_StrBytes("0F 84 35 00 00 00");
            //cmp ecx, 43
            CaveMemory.Write_StrBytes("83 F9 43");
            //je @
            CaveMemory.Write_StrBytes("0F 84 2C 00 00 00");
            //cmp ecx, 0
            CaveMemory.Write_StrBytes("83 F9 00");
            //je @
            CaveMemory.Write_StrBytes("0F 84 28 00 00 00");
            //cmp ecx, 1
            CaveMemory.Write_StrBytes("83 F9 01");
            //je @
            CaveMemory.Write_StrBytes("0F 84 1A 00 00 00");
            //cmp ecx, 40
            CaveMemory.Write_StrBytes("83 F9 40");
            //je @
            CaveMemory.Write_StrBytes("0F 84 16 00 00 00");
            //cmp ecx, 41
            CaveMemory.Write_StrBytes("83 F9 41");
            //je @
            CaveMemory.Write_StrBytes("0F 84 08 00 00 00");
            //mov [ecx*2+padDemul.dll+OFFSET],ax
            CaveMemory.Write_StrBytes("66 89 04 4D");
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            //jmp @
            CaveMemory.Write_jmp((int)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Return_Offset);
            //and eax, 08
            CaveMemory.Write_StrBytes("83 E0 08");
            //cmp eax, 0
            CaveMemory.Write_StrBytes("83 F8 00");
            //je @
            CaveMemory.Write_StrBytes("0F 84 0A 00 00 00");
            //or dword ptr [ecx*2+padDemul.dll+OFFSET],08
            CaveMemory.Write_StrBytes("83 0C 4D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("08");
            //jmp @
            CaveMemory.Write_StrBytes("EB E5");
            //and dword ptr [ecx*2+padDemul.dll+OFFSET],-09 { 247 }
            CaveMemory.Write_StrBytes("83 24 4D");
            Buffer.Clear();
            Buffer.AddRange(BitConverter.GetBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_PtrButtons_Offset));
            CaveMemory.Write_Bytes(Buffer.ToArray());
            CaveMemory.Write_StrBytes("F7");
            //jmp @
            CaveMemory.Write_StrBytes("EB DB");

            WriteLog("Adding CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Injection de code
            IntPtr ProcessHandle = _TargetProcess.Handle;
            int bytesWritten = 0;
            int jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((int)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32.WriteProcessMemory((int)ProcessHandle, (int)_PadDemul_ModuleBaseAddress + _Paddemul_Injection_Offset, Buffer.ToArray(), Buffer.Count, ref bytesWritten);

            //Initialise pour prise en compte des guns direct
            byte[] init = { 0, 0x7f, 0, 0x7f };
            WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, init);
            WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Start_Button_Offset, 0xFF);
            WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Fire_Button_Offset, 0xFF);
            WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, init);
            WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Start_Button_Offset, 0xFF);
            WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Fire_Button_Offset, 0xFF);

            WriteLog("Atomiswave Memory Hack complete !");
            WriteLog("-");
        }

        public override void SendInput(MouseInfo mouse, int Player)
        {
            byte[] buffer = { (byte)(mouse.pTarget.X >> 8), (byte)(mouse.pTarget.X & 0xFF), (byte)(mouse.pTarget.Y >> 8), (byte)(mouse.pTarget.Y & 0xFF) };
            if (Player == 1)
            {
                //Write Axis
                WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, buffer);
                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Fire_Button_Offset, 0xFB);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Fire_Button_Offset, 0xFF);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    /* For Sprtshot, we force offscreen shoot for reload */
                    if (_RomName.Equals("sprtshot"))
                    {
                        buffer[0] = (byte)(0);
                        buffer[1] = (byte)(0);
                        buffer[2] = (byte)(0);
                        buffer[3] = (byte)(0);
                        WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P1_X_Offset, buffer);
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Fire_Button_Offset, 0xFB);
                        System.Threading.Thread.Sleep(50);
                    }
                    else
                    {
                        //Read Value and set Bit 2 to 0
                        byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Start_Button_Offset);
                        val = (byte)(val & 0xFB);
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Start_Button_Offset, val);
                    }
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    if (_RomName.Equals("sprtshot"))
                    {
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Fire_Button_Offset, 0xFF);
                    }
                    else
                    {
                        //Read Value and set Bit 2 to 1
                        byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Start_Button_Offset);
                        val = (byte)(val | 0x04);
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P1_Start_Button_Offset, val);
                    }
                }
            }
            else if (Player == 2)
            {
                //Write Axis
                WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, buffer);

                //Inputs
                if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_DOWN)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Fire_Button_Offset, 0xFB);
                }
                else if (mouse.button == Win32.RI_MOUSE_LEFT_BUTTON_UP)
                {
                    WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Fire_Button_Offset, 0xFF);
                }
                if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_DOWN)
                {
                    /* For Sprtshot, we force offscreen shoot for reload */
                    if (_RomName.Equals("sprtshot"))
                    {
                        buffer[0] = (byte)(0);
                        buffer[1] = (byte)(0);
                        buffer[2] = (byte)(0);
                        buffer[3] = (byte)(0);
                        WriteBytes((int)_PadDemul_ModuleBaseAddress + _Paddemul_P2_X_Offset, buffer);
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Fire_Button_Offset, 0xFB);
                        System.Threading.Thread.Sleep(50);
                    }
                    else
                    {
                        //Read Value and set Bit 2 to 0
                        byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Start_Button_Offset);
                        val = (byte)(val & 0xFB);
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Start_Button_Offset, val);
                    }
                }
                else if (mouse.button == Win32.RI_MOUSE_RIGHT_BUTTON_UP)
                {
                    if (_RomName.Equals("sprtshot"))
                    {
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Fire_Button_Offset, 0xFF);
                    }
                    else
                    {
                        //Read Value and set Bit 2 to 1
                        byte val = ReadByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Start_Button_Offset);
                        val = (byte)(val | 0x04);
                        WriteByte((int)_PadDemul_ModuleBaseAddress + _Paddemul_Aw_P2_Start_Button_Offset, val);
                    }
                }
            }
        }

        #endregion
    }
}
