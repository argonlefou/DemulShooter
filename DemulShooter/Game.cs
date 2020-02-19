using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace DemulShooter
{
    class Game
    {
        protected string _Target_Process_Name = String.Empty;
        protected const string LOG_FILENAME = "debug.txt";

        /*** Process variables **/
        protected Timer _tProcess;
        protected bool _ProcessHooked;
        protected Process _TargetProcess;
        protected IntPtr _ProcessHandle = IntPtr.Zero;
        protected IntPtr _TargetProcess_MemoryBaseAddress = IntPtr.Zero;
        protected bool _VerboseEnable;
        protected bool _DisableWindow = false;
        protected string _TargetProcess_Md5Hash = string.Empty;
        protected Dictionary<string, string> _KnownMd5Prints;

        public bool ProcessHooked
        { get { return _ProcessHooked; } }

        /*** SCREEN ***/
        protected int _screenWidth;
        public int ScreenWidth
        {
            get { return _screenWidth; }
        }
        protected int _screenHeight;
        public int ScreenHeight
        {
            get { return _screenHeight; }
        }
        protected int _screenCursorPosX;
        public int screenCursorPosX
        {
            get { return _screenCursorPosX; }
            set { _screenCursorPosX = value; }
        }
        protected int _screenCursorPosY;
        public int screenCursorPosY
        {
            get { return _screenCursorPosY; }
            set { _screenCursorPosY = value; }
        }

        protected Win32.HookProc _MouseHookProc;
        protected IntPtr _MouseHookID = IntPtr.Zero;

        protected Win32.HookProc _KeyboardHookProc;
        protected IntPtr _KeyboardHookID = IntPtr.Zero;        

        protected XOutput _XOutputManager = null;
        protected int _Player1_X360_Gamepad_Port = 0;
        protected int _Player2_X360_Gamepad_Port = 0;

        protected string _RomName = string.Empty;

        public Game()
        {
            _KnownMd5Prints = new Dictionary<string, string>();
        }

        ~Game()
        {
            if (_XOutputManager != null)
            {
                if (_Player1_X360_Gamepad_Port != 0)
                    UninstallX360Gamepad(1);
                if (_Player2_X360_Gamepad_Port != 0)
                    UninstallX360Gamepad(2);
            }
        }

        #region MD5 Verification

        protected void ChecExeMd5()
        {
            GetMd5HashAsString();
            WriteLog("MD5 hash of " + _TargetProcess.MainModule.FileName + " = " + _TargetProcess_Md5Hash); 

            string FoundExe = string.Empty;
            foreach (KeyValuePair<string, string> pair in _KnownMd5Prints)
            {
                if (pair.Value == _TargetProcess_Md5Hash)
                {
                    FoundExe = pair.Key;
                    break;
                }
            }

            if (FoundExe == string.Empty)
            {
                WriteLog(@"/!\ MD5 Hash unknown, DemulShooter may not work correctly with this target /!\");
            }
            else
            {
                WriteLog("MD5 Hash is corresponding to a known target = " + FoundExe);
            }
            
        }

        private void GetMd5HashAsString()
        {
            string process_path = _TargetProcess.MainModule.FileName;
            if (File.Exists(process_path))
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(process_path))
                    {
                        var hash = md5.ComputeHash(stream);
                        _TargetProcess_Md5Hash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
        }        

        #endregion

        #region Screen

        public virtual void GetScreenResolution()
        {
            _screenWidth = Screen.PrimaryScreen.Bounds.Width;
            _screenHeight = Screen.PrimaryScreen.Bounds.Height;
        }

        public void GetScreenresolution2()
        {
            IntPtr hDesktop = Win32.GetDesktopWindow();
            Win32.Rect DesktopRect = new Win32.Rect();
            Win32.GetWindowRect(hDesktop, ref DesktopRect);
            _screenWidth = DesktopRect.Right;
            _screenHeight = DesktopRect.Bottom;
        }


        /// <summary>
        /// Contains value inside min-max range
        /// </summary>
        protected int Clamp(int val, int minVal, int maxVal)
        {
            if (val > maxVal) return maxVal;
            else if (val < minVal) return minVal;
            else return val;
        }

        /// <summary>
        /// Transforming 0x0000-0xFFFF absolute rawdata to absolute x,y position on Desktop resolution
        /// </summary>
        public int ScreenScale(int val, int fromMinVal, int fromMaxVal, int toMinVal, int toMaxVal)
        {
            return ScreenScale(val, fromMinVal, fromMinVal, fromMaxVal, toMinVal, toMinVal, toMaxVal);
        }
        protected int ScreenScale(int val, int fromMinVal, int fromOffVal, int fromMaxVal, int toMinVal, int toOffVal, int toMaxVal)
        {
            double fromRange;
            double frac;
            if (fromMaxVal > fromMinVal)
            {
                val = Clamp(val, fromMinVal, fromMaxVal);
                if (val > fromOffVal)
                {
                    fromRange = (double)(fromMaxVal - fromOffVal);
                    frac = (double)(val - fromOffVal) / fromRange;
                }
                else if (val < fromOffVal)
                {
                    fromRange = (double)(fromOffVal - fromMinVal);
                    frac = (double)(val - fromOffVal) / fromRange;
                }
                else
                    return toOffVal;
            }
            else if (fromMinVal > fromMaxVal)
            {
                val = Clamp(val, fromMaxVal, fromMinVal);
                if (val > fromOffVal)
                {
                    fromRange = (double)(fromMinVal - fromOffVal);
                    frac = (double)(fromOffVal - val) / fromRange;
                }
                else if (val < fromOffVal)
                {
                    fromRange = (double)(fromOffVal - fromMaxVal);
                    frac = (double)(fromOffVal - val) / fromRange;
                }
                else
                    return toOffVal;
            }
            else
                return toOffVal;
            double toRange;
            if (toMaxVal > toMinVal)
            {
                if (frac >= 0)
                    toRange = (double)(toMaxVal - toOffVal);
                else
                    toRange = (double)(toOffVal - toMinVal);
                return toOffVal + (int)(toRange * frac);
            }
            else
            {
                if (frac >= 0)
                    toRange = (double)(toOffVal - toMaxVal);
                else
                    toRange = (double)(toMinVal - toOffVal);
                return toOffVal - (int)(toRange * frac);
            }
        }

        /// <summary>
        /// Convert screen location of pointer to Client area location
        /// </summary>
        public virtual bool ClientScale(MouseInfo mouse)
        {
            if (_TargetProcess != null)
                //Convert Screen location to Client location
                return Win32.ScreenToClient(_TargetProcess.MainWindowHandle, ref mouse.pTarget);
            else
                return false;
        }
        
        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// </summary>
        public virtual bool GameScale(MouseInfo Mouse, int Player)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    //Window size
                    Win32.Rect TotalRes = new Win32.Rect();
                    Win32.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);

                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    if (Mouse.pTarget.X < 0)
                        Mouse.pTarget.X = 0;
                    if (Mouse.pTarget.Y < 0)
                        Mouse.pTarget.Y = 0;
                    if (Mouse.pTarget.X > (int)TotalResX)
                        Mouse.pTarget.X = (int)TotalResX;
                    if (Mouse.pTarget.Y > (int)TotalResX)
                        Mouse.pTarget.Y = (int)TotalResX;
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

        #region I/O FILE

        /// <summary>
        /// Read memory values in .cfg file
        /// </summary>
        protected virtual void ReadGameData()
        {            
        }

        /// <summary>
        /// Writing Log only if verbose arg given in cmdline
        /// </summary>
        protected void WriteLog(String Data)
        {
            if (_VerboseEnable)
            {
                try
                {
                    using (StreamWriter sr = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + @"\" + LOG_FILENAME, true))
                    {
                        sr.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffffff") + " : " + Data);
                        sr.Close();
                    }
                }
                catch { }
            }
        }

        #endregion

        #region MemoryHack x86

        public virtual void SendInput(MouseInfo mouse, int Player)
        {
        }

        protected Byte ReadByte(int Address)
        {
            byte[] Buffer = { 0 };
            int bytesRead = 0;
            if (!Win32.ReadProcessMemory((int)_ProcessHandle, Address, Buffer, 1, ref bytesRead))
            {
                WriteLog("Cannot read memory at address 0x" + Address.ToString("X8"));
            }
            return Buffer[0];
        } 

        protected Byte[] ReadBytes(int Address, int Bytes)
        {
            byte[] Buffer = new byte[Bytes];
            int bytesRead = 0;
            if (!Win32.ReadProcessMemory((int)_ProcessHandle, Address, Buffer, Buffer.Length, ref bytesRead))
            {
                WriteLog("Cannot read memory at address 0x" + Address.ToString("X8"));
            }
            return Buffer;
        }

        protected bool WriteByte(int Address, byte Value)
        {
            int bytesWritten = 0;
            Byte[] Buffer = { Value };
            if (Win32.WriteProcessMemory((int)_ProcessHandle, Address, Buffer, 1, ref bytesWritten))
            {
                if (bytesWritten == 1)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        protected bool WriteBytes(int Address, byte[] Buffer)
        {
            int bytesWritten = 0;
            if (Win32.WriteProcessMemory((int)_ProcessHandle, Address, Buffer, Buffer.Length, ref bytesWritten))
            {
                if (bytesWritten == Buffer.Length)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
        
        protected void SetNops(int BaseAddress, string OffsetAndNumber)
        {
            if (OffsetAndNumber != null)
            {
                try
                {
                    int n = int.Parse((OffsetAndNumber.Split('|'))[1]);
                    int address = int.Parse((OffsetAndNumber.Split('|'))[0].Substring(3).Trim(), NumberStyles.HexNumber);
                    for (int i = 0; i < n; i++)
                    {
                        WriteByte(BaseAddress + address + i, 0x90);
                    }
                }
                catch
                {
                    WriteLog("Impossible de traiter le NOP : " + OffsetAndNumber);
                }
            }
        }

        protected void Apply_OR_ByteMask(int MemoryAddress, byte Mask)
        {
            byte b = ReadByte(MemoryAddress);
            b |= Mask;
            WriteByte(MemoryAddress, b);
        }

        protected void Apply_AND_ByteMask(int MemoryAddress, byte Mask)
        {
            byte b = ReadByte(MemoryAddress);
            b &= Mask;
            WriteByte(MemoryAddress, b);
        }

        #endregion

        #region MemoryHack x64

        protected byte ReadByteX64(IntPtr Address)
        {
            byte[] Buffer = { 0 };
            UIntPtr bytesRead = UIntPtr.Zero;
            WriteLog(Address.ToString("X8"));
            if (!Win32.ReadProcessMemoryX64((IntPtr)_ProcessHandle, Address, Buffer, (UIntPtr)1, out bytesRead))
            {
                WriteLog("Cannot read memory at address 0x" + Address.ToString("X8"));
            }
            return Buffer[0];
        }
        
        protected Byte[] ReadBytesX64(IntPtr Address, int Bytes)
        {
            byte[] Buffer = new byte[Bytes];
            UIntPtr bytesRead = UIntPtr.Zero;
            if (!Win32.ReadProcessMemoryX64((IntPtr)_ProcessHandle, Address, Buffer, (UIntPtr)Buffer.Length, out bytesRead))
            {
                WriteLog("Cannot read memory at address 0x" + Address.ToString("X8"));
            }
            return Buffer;
        }
        
        protected bool WriteByteX64(IntPtr Address, byte Value)
        {
            UIntPtr bytesWritten = UIntPtr.Zero;
            Byte[] Buffer = { Value };
            if (Win32.WriteProcessMemoryX64((IntPtr)_ProcessHandle, Address, Buffer, (UIntPtr)1, out bytesWritten))
            {
                if ((int)bytesWritten == 1)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        protected bool WriteBytesX64(IntPtr Address, byte[] Buffer)
        {
            UIntPtr bytesWritten = UIntPtr.Zero;
            if (Win32.WriteProcessMemoryX64((IntPtr)_ProcessHandle, Address, Buffer, (UIntPtr)Buffer.Length, out bytesWritten))
            {
                if ((int)bytesWritten == Buffer.Length)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }        

        protected void SetNopsX64(IntPtr BaseAddress, string OffsetAndNumber)
        {
            if (OffsetAndNumber != null)
            {
                try
                {
                    int n = int.Parse((OffsetAndNumber.Split('|'))[1]);
                    UInt64 address = UInt64.Parse((OffsetAndNumber.Split('|'))[0].Substring(3).Trim(), NumberStyles.HexNumber);
                    for (int i = 0; i < n; i++)
                    {
                        WriteByteX64((IntPtr)((UInt64)BaseAddress + (UInt64)address + (UInt64)i), 0x90);
                    }
                }
                catch
                {
                    WriteLog("Impossible de traiter le NOP : " + OffsetAndNumber);
                }
            }
        }

        protected void Apply_OR_ByteMaskX64(IntPtr MemoryAddress, byte Mask)
        {
            byte b = ReadByteX64(MemoryAddress);
            b |= Mask;
            WriteByteX64(MemoryAddress, b);
        }

        protected void Apply_AND_ByteMaskX64(IntPtr MemoryAddress, byte Mask)
        {
            byte b = ReadByteX64(MemoryAddress);
            b &= Mask;
            WriteByteX64(MemoryAddress, b);
        }

        #endregion


        #region XOutput

        protected virtual void InstallX360Gamepad(int Player)
        {
            if (_XOutputManager != null)
            {
                if (_XOutputManager.isVBusExists())
                {
                    for (int i = 1; i < 5; i++)
                    {
                        if (_XOutputManager.PlugIn(i))
                        {
                            if (Player == 1)
                            {
                                WriteLog("Plugged P1 virtual Gamepad to port " + i.ToString());
                                _Player1_X360_Gamepad_Port = i;
                            }
                            else if (Player == 2)
                            {
                                WriteLog("Plugged P2 virtual Gamepad to port " + i.ToString());
                                _Player2_X360_Gamepad_Port = i;
                            }
                            break;
                        }
                        else
                            WriteLog("Failed to plug virtual GamePad to port " + i.ToString() + ". (Port already used ?)");
                    }
                }
                else
                {
                    WriteLog("ScpBus driver not found or not installed");
                }
            }
            else
            {
                WriteLog("XOutputManager Creation Failed !");
            }
        }

        protected bool UninstallX360Gamepad(int Player)
        {
            if (_XOutputManager != null)
            {
                if (Player == 1 && _Player1_X360_Gamepad_Port != 0)
                {
                    if (_XOutputManager.Unplug(_Player1_X360_Gamepad_Port, true))
                    {
                        WriteLog("Succesfully unplug P1 virtual Gamepad on port " + _Player1_X360_Gamepad_Port.ToString());
                        return true;
                    }
                    else
                    {
                        WriteLog("Failed to unplug P1 virtual Gamepad on port " + _Player1_X360_Gamepad_Port.ToString());
                        return false;
                    }
                }
                else if (Player == 2 && _Player2_X360_Gamepad_Port != 0)
                {
                    if (_XOutputManager.Unplug(_Player2_X360_Gamepad_Port, true))
                    {
                        WriteLog("Succesfully unplug P2 virtual Gamepad on port " + _Player2_X360_Gamepad_Port.ToString());
                        return true;
                    }
                    else
                    {
                        WriteLog("Failed to unplug P2 virtual Gamepad on port " + _Player2_X360_Gamepad_Port.ToString());
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion

        #region Keyboard SendKeys

        private void Send_Key(short Keycode, int KeyUpOrDown)
        {
            INPUT[] InputData = new INPUT[1];

            InputData[0].type = 1;
            InputData[0].ki.wScan = Keycode;
            InputData[0].ki.dwFlags = KeyUpOrDown;
            InputData[0].ki.time = 0;
            InputData[0].ki.dwExtraInfo = IntPtr.Zero;

            if ( Win32.SendInput(1, InputData, Marshal.SizeOf(typeof(INPUT))) == 0)
            {
                WriteLog("SendInput API failed : wScan=" + Keycode.ToString() + ", dwFlags=" + KeyUpOrDown.ToString());
                WriteLog("GetLastError returned : " + Marshal.GetLastWin32Error().ToString());
            }
        }
        protected void SendKeyDown(short Keycode)
        {
            Send_Key(Keycode, Win32.KEYEVENTF_SCANCODE);
        }
        protected void SendKeyUp(short Keycode)
        {
            Send_Key(Keycode, Win32.KEYEVENTF_KEYUP | Win32.KEYEVENTF_SCANCODE);
        }
        protected void SendKeyStroke(short Keycode, int DelayPressed)
        {
            SendKeyDown(Keycode);
            System.Threading.Thread.Sleep(DelayPressed);
            SendKeyUp(Keycode);
        }

        protected void Send_VK_KeyDown(byte Keycode)
        {
            Win32.keybd_event(Keycode, 0, Win32.KEYEVENTF_EXTENDEDKEY | 0, 0);
        }
        protected void Send_VK_KeyUp(byte Keycode)
        {
            Win32.keybd_event(Keycode, 0, Win32.KEYEVENTF_EXTENDEDKEY | Win32.KEYEVENTF_KEYUP, 0);
        }

        #endregion

        #region LowLevel Hooks

        /*** Mouse Hook ***/
        /*** Each class will have it's own overriding function as callback ***/
        protected void ApplyMouseHook()
        {
            _MouseHookProc = new Win32.HookProc(MouseHookCallback);
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            _MouseHookID = Win32.SetWindowsHookEx(Win32.WH_MOUSE_LL, _MouseHookProc, Win32.GetModuleHandle(curModule.ModuleName), 0);
            if (_MouseHookID == IntPtr.Zero)
            {
                WriteLog("MouseHook Error : " + Marshal.GetLastWin32Error());
            }
            else
            {
                WriteLog("LowLevelMouseHook installed !");
            } 
        }
        protected virtual IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            return Win32.CallNextHookEx(_MouseHookID, nCode, wParam, lParam);
        }
        protected void RemoveMouseHook()
        {
            Win32.UnhookWindowsHookEx(_MouseHookID);
        }


        /*** Keyboard Hook ***/
        /*** Each class will have it's own overriding function as callback ***/
        protected void ApplyKeyboardHook()
        {
            _KeyboardHookProc = new Win32.HookProc(KeyboardHookCallback);
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            _KeyboardHookID = Win32.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, _KeyboardHookProc, Win32.GetModuleHandle(curModule.ModuleName), 0);
            if (_KeyboardHookID == IntPtr.Zero)
            {
                WriteLog("KeyboardHook Error : " + Marshal.GetLastWin32Error());
            }
            else
            {
                WriteLog("LowLevel-KeyboardHook installed !");
            }
        }
        protected virtual IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            return Win32.CallNextHookEx(_KeyboardHookID, nCode, wParam, lParam);
        }
        protected void RemoveKeyboardHook()
        {
            Win32.UnhookWindowsHookEx(_KeyboardHookID);
        }        

        #endregion

    }
}
