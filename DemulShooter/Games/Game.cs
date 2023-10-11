using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Timers;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.Win32;
using System.Text;

namespace DemulShooter
{
    public class Game
    {
        public delegate void GameHookedHandler(object sender, EventArgs e);
        public event GameHookedHandler OnGameHooked;

        #region Process variables

        protected System.Timers.Timer _tProcess;
        protected String _RomName = string.Empty;
        protected Process _TargetProcess;        
        protected String _Target_Process_Name = String.Empty;
        protected IntPtr _TargetProcess_MemoryBaseAddress = IntPtr.Zero;        
        protected IntPtr _ProcessHandle = IntPtr.Zero;
        protected IntPtr _GameWindowHandle = IntPtr.Zero;
        protected bool _VerboseEnable;
        protected bool _DisableWindow = false;
        protected bool _DisableInputHack = false;

        //MD5 check of target binaries, may help to know if it's the wrong version or not compatible
        protected Dictionary<string, string> _KnownMd5Prints;
        protected String _TargetProcess_Md5Hash = string.Empty;

        //Output values handling
        protected List<GameOutput> _Outputs;
        public List<GameOutput> Outputs
        {
            get { return _Outputs; }
        }

        protected bool _ProcessHooked;
        public bool ProcessHooked
        { 
            get { return _ProcessHooked; } 
        }

        #endregion

        /// <summary>
        /// Common constructor for every single games
        /// </summary>
        /// <param name="RomName">DemumShooter [-rom] Parameter</param>
        /// <param name="TargetProcessName">Executable name to hook in process list</param>
        /// <param name="Verbose">Create a debug.txt file if TRUE</param>
        public Game(String RomName, String TargetProcessName, bool DisableInputHack, bool Verbose)
        {
            _KnownMd5Prints = new Dictionary<String, String>();
            GetScreenResolution();
            Logger.WriteLog("Windows screen scaling : " + GetScreenScaling());

            _WindowRect = new Rect();
            _clientWindowLocation = new POINT(0, 0);

            _RomName = RomName;
            _DisableInputHack = DisableInputHack;
            _VerboseEnable = Verbose;
            _ProcessHooked = false;
            _Target_Process_Name = TargetProcessName;

            CreateOutputList();

            _tProcess = new System.Timers.Timer();
            _tProcess.Interval = 500;
            _tProcess.Elapsed += new ElapsedEventHandler(tProcess_Elapsed); 
            _tProcess.Enabled = true; 
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

        protected virtual void tProcess_Elapsed(Object sender, EventArgs e)
        {}

        /// <summary>
        /// Raise custom event to main window (to change TrayIcon status)
        /// </summary>
        protected void RaiseGameHookedEvent()
        {
            // Make sure someone is listening to event
            if (OnGameHooked == null) return;

            OnGameHooked(this, new EventArgs());
        }

        #region MD5 Verification

        /// <summary>
        /// Compute the MD5 hash of the target executable and compare it to the known list of MD5 Hashes
        /// This can be usefull if people are using some unknown dump with different memory, 
        /// or a wrong version of emulator
        /// This is absolutely not blocking, just for debuging with output log
        /// </summary>
        protected void CheckExeMd5()
        {
            CheckMd5(_TargetProcess.MainModule.FileName);
        }
        protected void CheckMd5(String TargetFileName)
        {
            GetMd5HashAsString(TargetFileName);
            Logger.WriteLog("MD5 hash of " + TargetFileName + " = " + _TargetProcess_Md5Hash);

            String FoundMd5 = String.Empty;
            foreach (KeyValuePair<String, String> pair in _KnownMd5Prints)
            {
                if (pair.Value == _TargetProcess_Md5Hash)
                {
                    FoundMd5 = pair.Key;
                    break;
                }
            }

            if (FoundMd5 == String.Empty)
            {
                Logger.WriteLog(@"/!\ MD5 Hash unknown, DemulShooter may not work correctly with this target /!\");
            }
            else
            {
                Logger.WriteLog("MD5 Hash is corresponding to a known target = " + FoundMd5);
            }
            
        }

        /// <summary>
        /// Compute the MD5 hash from the target file.
        /// </summary>
        /// <param name="FileName">Full  filepath of the targeted executable.</param>
        private void GetMd5HashAsString(String FileName)
        {
            if (File.Exists(FileName))
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(FileName))
                    {
                        var hash = md5.ComputeHash(stream);
                        _TargetProcess_Md5Hash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
        }        

        #endregion

        #region Screen

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
        protected Rect _WindowRect;
        public Rect WindowRect
        {
            get { return _WindowRect; }
            set { _WindowRect = value; }
        }
        protected POINT _clientWindowLocation;
        public POINT clientWindowLocation
        {
            get { return _clientWindowLocation; }
            set { _clientWindowLocation = value; }
        }
        protected Rect _ClientRect;
        public Rect ClientRect
        {
            get { return _ClientRect; }
            set { _ClientRect = value; }
        }
        protected bool _IsFullscreen;
        public bool IsFullscreen
        {
            get { return _IsFullscreen; }
            set { _IsFullscreen = value; }
        }

        /// <summary>
        /// HKEY_CURRENT_USER\Control Panel\Desktop\
        ///Key: LogPixels
        ///Values:
        ///96   –  Smaller 100%
        ///120  –  Medium 125%
        ///144  -  Larger 150%
        ///192  –  Extra Large 200%
        ///240  –  Custom 250%
        ///288  –  Custom 300%
        ///384  –  Custom 400%
        ///480  –  Custom 500%*/
        /// </summary>
        private string GetScreenScaling()
        {
            IntPtr desktopDc = Win32API.GetDC(IntPtr.Zero);
            // Get native resolution
            //#DEFINE LOGPIXELSX       88
            //#DEFINE LOGPIXELSY       90
            int horizontalDPI = Win32API.GetDeviceCaps(desktopDc, 88);
            int verticalDPI = Win32API.GetDeviceCaps(desktopDc, 90);

            return (horizontalDPI * 100 / 96).ToString() + "% (HorizontalDPI=" + horizontalDPI + ", VerticalDPI=" + verticalDPI + ")";

            /*switch (horizontalDPI.ToString())
            {
                case "96": return "100%";
                case "120": return "125%";
                case "144": return "150%";
                case "192": return "200%";
                case "240": return "250%";
                case "288": return "300%";
                case "384": return "400%";
                case "480": return "500%";
                default: return @"Unknown value : " + horizontalDPI.ToString();

            }*/
        }

        public virtual bool GetFullscreenStatus()
        {
            QUERY_USER_NOTIFICATION_STATE state;
            int r = Win32API.SHQueryUserNotificationState(out state);
            if (r == 0)
            {
                Logger.WriteLog("NotificationState: " + state.ToString());
                switch (state)
                {
                    // A screen saver is displayed, the machine is locked, or a nonactive Fast User Switching session is in progress.
                    case QUERY_USER_NOTIFICATION_STATE.QUNS_NOT_PRESENT: return false;

                    // A full-screen application is running or Presentation Settings are applied. Presentation Settings allow a user to put their machine into a state fit for an uninterrupted presentation, such as a set of PowerPoint slides, with a single click.
                    case QUERY_USER_NOTIFICATION_STATE.QUNS_BUSY: return true;

                    // A full-screen (exclusive mode) Direct3D application is running.
                    case QUERY_USER_NOTIFICATION_STATE.QUNS_RUNNING_D3D_FULL_SCREEN: return true;

                    // The user has activated Windows presentation settings to block notifications and pop-up messages.
                    case QUERY_USER_NOTIFICATION_STATE.QUNS_PRESENTATION_MODE: return false;

                    // None of the other states are found, notifications can be freely sent.
                    case QUERY_USER_NOTIFICATION_STATE.QUNS_ACCEPTS_NOTIFICATIONS: return false;

                    // We are in OOBE quiet period
                    case QUERY_USER_NOTIFICATION_STATE.QUNS_QUIET_TIME: return false;

                    // A Windows Store app is running.
                    case QUERY_USER_NOTIFICATION_STATE.QUNS_APP: return false;

                    default: return false;
                }
            }
            else
            {
                Logger.WriteLog("Error running SHQueryUserNotificationState: " + r.ToString());
                return false;
            }
        }

        public virtual void GetScreenResolution()
        {
            _screenWidth = Win32API.GetSystemMetrics(SystemMetricsIndex.SM_CXSCREEN);
            _screenHeight = Win32API.GetSystemMetrics(SystemMetricsIndex.SM_CYSCREEN);
            /*_screenWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            _screenHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;*/
        }

        public void GetScreenresolution2()
        {
            IntPtr hDesktop = Win32API.GetDesktopWindow();
            Rect DesktopRect = new Rect();
            Win32API.GetWindowRect(hDesktop, ref DesktopRect);
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
        public virtual bool ClientScale(PlayerSettings PlayerData)
        {
            //Retrieve info for debug/replay purpose
            GetClientwindowInfo();

            //Convert Screen location to Client location
            if (_TargetProcess != null)
            {
                POINT p = new POINT(PlayerData.RIController.Computed_X, PlayerData.RIController.Computed_Y);
                if (Win32API.ScreenToClient(_GameWindowHandle, ref p))
                {
                    PlayerData.RIController.Computed_X = (p.X);
                    PlayerData.RIController.Computed_Y = (p.Y);
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        public void GetClientwindowInfo()
        {
            if (_TargetProcess != null)
            {
                if (Win32API.GetWindowRect(_GameWindowHandle, ref _WindowRect))
                {
                    _clientWindowLocation.X = _WindowRect.Left;
                    _clientWindowLocation.Y = _WindowRect.Top;
                }
                else
                {
                    _clientWindowLocation.X = 0;
                    _clientWindowLocation.Y = 0;
                }
            }
        }

        /// <summary>
        /// Get the Client rect to translate axis coordinate to game coordinates
        /// Only used in windowed Mode
        /// </summary>
        public virtual bool GetClientRect()
        {

            //Window size
            _ClientRect = new Rect();
            if (Win32API.GetClientRect(_GameWindowHandle, ref _ClientRect))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// </summary>
        public virtual bool GameScale(PlayerSettings PlayerData)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    double TotalResX = _ClientRect.Right - _ClientRect.Left;
                    double TotalResY = _ClientRect.Bottom - _ClientRect.Top;
                    Logger.WriteLog("Game Window Rect (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    if (PlayerData.RIController.Computed_X < 0)
                        PlayerData.RIController.Computed_X = 0;
                    if (PlayerData.RIController.Computed_Y < 0)
                        PlayerData.RIController.Computed_Y = 0;
                    if (PlayerData.RIController.Computed_X > (int)TotalResX)
                        PlayerData.RIController.Computed_X= (int)TotalResX;
                    if (PlayerData.RIController.Computed_Y > (int)TotalResX)
                        PlayerData.RIController.Computed_X = (int)TotalResY;
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

        #region Game Configuration

        /// <summary>
        /// Read memory values in .cfg file, whose name depends on the targeted system
        /// Used for DEMUL targets
        /// </summary>
        protected virtual void ReadGameData()
        { }

        /// <summary>
        /// Read memory values in .cfg file, whose name depends on the MD5 hash of the targeted exe.
        /// Mostly used for PC games
        /// </summary>
        /// <param name="GameData_Folder"></param>
        protected virtual void ReadGameDataFromMd5Hash(String GameData_Folder)
        {
            String ConfigFile = AppDomain.CurrentDomain.BaseDirectory + GameData_Folder + @"\" + _TargetProcess_Md5Hash + ".cfg";
            if (File.Exists(ConfigFile))
            {
                Logger.WriteLog("Reading game memory setting from " + ConfigFile);
                using (StreamReader sr = new StreamReader(ConfigFile))
                {
                    String line;
                    String FieldName = String.Empty;
                    line = sr.ReadLine();
                    while (line != null)
                    {
                        String[] buffer = line.Split('=');
                        if (buffer.Length > 1)
                        {
                            try
                            {
                                FieldName = "_" + buffer[0].Trim();
                                if (buffer[0].Contains("Nop"))
                                {
                                    NopStruct n = new NopStruct(buffer[1].Trim());
                                    this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, n);
                                    Logger.WriteLog(FieldName + " successfully set to following value : 0x" + n.MemoryOffset.ToString("X8") + "|" + n.Length.ToString());
                                }
                                else if (buffer[0].Contains("InjectionStruct"))
                                {
                                    InjectionStruct n = new InjectionStruct(buffer[1].Trim());
                                    this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, n);
                                    Logger.WriteLog(FieldName + " successfully set to following value : 0x" + n.InjectionOffset.ToString("X8") + "|" + n.Length.ToString());
                                }
                                else if (buffer[0].Contains("DIK"))
                                {
                                    HardwareScanCode sc = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), buffer[1].Trim());
                                    this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, sc);
                                    Logger.WriteLog(FieldName + " successfully set to following value :" + sc.ToString());
                                }
                                else if (buffer[0].Contains("VK"))
                                {
                                    VirtualKeyCode vk = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), buffer[1].Trim());
                                    this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, vk);
                                    Logger.WriteLog(FieldName + " successfully set to following value :" + vk.ToString());
                                }
                                else
                                {
                                    UInt32 v = UInt32.Parse(buffer[1].Substring(3).Trim(), NumberStyles.HexNumber);
                                    this.GetType().GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).SetValue(this, v);
                                    Logger.WriteLog(FieldName + " successfully set to following value : 0x" + v.ToString("X8"));
                                }                                
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog("Error reading game data for " + FieldName + " : " + ex.Message.ToString());
                            }
                        }
                        line = sr.ReadLine();
                    }
                    sr.Close();
                }
            }
            else
            {
                Logger.WriteLog("File not found : " + ConfigFile);
            }
        }
        
        #endregion

        #region Memory Hack x86

        public virtual void SendInput(PlayerSettings PlayerData)
        {}

        protected Byte ReadByte(UInt32 Address)
        {
            byte[] Buffer = { 0 };
            UInt32 bytesRead = 0;
            if (!Win32API.ReadProcessMemory(_ProcessHandle, Address, Buffer, 1, ref bytesRead))
            {
                Logger.WriteLog("Cannot read memory at address 0x" + Address.ToString("X8"));
            }
            return Buffer[0];
        }

        protected Byte[] ReadBytes(UInt32 Address, UInt32 BytesCount)
        {
            byte[] Buffer = new byte[BytesCount];
            UInt32 bytesRead = 0;
            if (!Win32API.ReadProcessMemory(_ProcessHandle, Address, Buffer, (UInt32)Buffer.Length, ref bytesRead))
            {
                Logger.WriteLog("Cannot read memory at address 0x" + Address.ToString("X8"));
            }
            return Buffer;
        }

        protected UInt32 ReadPtr(UInt32 PtrAddress)
        {
            byte[] Buffer = ReadBytes(PtrAddress, 4);
            return BitConverter.ToUInt32(Buffer, 0);
        }

        protected UInt32 ReadPtrChain(UInt32 BaseAddress, UInt32[] Offsets)
        {
            byte[] Buffer = ReadBytes(BaseAddress, 4);
            UInt32 Ptr = BitConverter.ToUInt32(Buffer, 0);

            if (Ptr == 0)
            {
                return 0;
            }
            else
            {
                for (int i = 0; i < Offsets.Length; i++)
                {
                    Buffer = ReadBytes(Ptr + Offsets[i], 8);
                    Ptr = BitConverter.ToUInt32(Buffer, 0);

                    if (Ptr == 0)
                        return 0;
                }
            }

            return Ptr;
        }

        protected bool WriteByte(UInt32 Address, byte Value)
        {
            UInt32 bytesWritten = 0;
            Byte[] Buffer = { Value };
            if (Win32API.WriteProcessMemory(_ProcessHandle, Address, Buffer, 1, ref bytesWritten))
            {
                if (bytesWritten == 1)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        protected bool WriteBytes(UInt32 Address, byte[] Buffer)
        {
            UInt32 bytesWritten = 0;
            if (Win32API.WriteProcessMemory(_ProcessHandle, Address, Buffer, (UInt32)Buffer.Length, ref bytesWritten))
            {
                if (bytesWritten == Buffer.Length)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        protected void Write_Codecave(Codecave CodecaveToWrite, InjectionStruct Injection)
        {
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CodecaveToWrite.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + Injection.InjectionOffset) - 5;
            List<byte> Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            for (int i = 0; i < Injection.NeededNops; i++)
            {
                Buffer.Add(0x90);
            }
            Win32API.WriteProcessMemory(_TargetProcess.Handle, (UInt32)_TargetProcess.MainModule.BaseAddress + Injection.InjectionOffset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        protected void SetNops(UInt32 BaseAddress, NopStruct Nop)
        {
            for (UInt32 i = 0; i < Nop.Length; i++)
            {
                UInt32 Address = (UInt32)BaseAddress + Nop.MemoryOffset + i;
                if (!WriteByte(Address, 0x90))
                {
                    Logger.WriteLog("Impossible to NOP address 0x" + Address.ToString("X8"));
                    break;
                }
            }
        }

        protected void Apply_OR_ByteMask(UInt32 MemoryAddress, byte Mask)
        {
            byte b = ReadByte(MemoryAddress);
            b |= Mask;
            WriteByte(MemoryAddress, b);
        }

        protected void Apply_AND_ByteMask(UInt32 MemoryAddress, byte Mask)
        {
            byte b = ReadByte(MemoryAddress);
            b &= Mask;
            WriteByte(MemoryAddress, b);
        }

        #endregion

        #region MAME-Like Outputs

        protected virtual void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
        }

        public virtual void UpdateOutputValues()
        { }

        /// <summary>
        /// Return a GameOutput object corresponding to a desired GameOutputId
        /// </summary>
        /// <param name="Id">Desired GameOutputId</param>
        /// <returns>Desired GameOutput object</returns>
        protected GameOutput GetOutputById(OutputId Id)
        {
            foreach (GameOutput CurrentOutput in _Outputs)
            {
                if (CurrentOutput.Id == (uint)Id)
                    return CurrentOutput;
            }
            return null;
        }

        /// <summary>
        /// Update a value for the desired GameOutput
        /// </summary>
        /// <param name="Id">GameOutput Id to update</param>
        /// <param name="Value">Value to update the GameOutput object</param>
        protected void SetOutputValue(OutputId Id, int Value)
        {
            foreach (GameOutput CurrentOutput in _Outputs)
            {
                if (CurrentOutput.Id == (uint)Id)
                {
                    CurrentOutput.OutputValue = Value;
                    break;
                }
            }
        }
        
        /// <summary>
        /// Return the Text description for a desired Id
        /// </summary>
        /// <param name="Id">Desired GameOutput Id</param>
        /// <returns>GameOutput string description</returns>
        public String GetOutputDescriptionFromId(uint Id)
        {
            foreach (GameOutput o in _Outputs)
            {
                if (o.Id == Id)
                    return o.Name;
            }
            return String.Empty;
        }

        #endregion

        #region XOutput

        protected XOutput _XOutputManager = null;
        protected int _Player1_X360_Gamepad_Port = 0;
        protected int _Player2_X360_Gamepad_Port = 0;

        /// <summary>
        /// Create and plug a virtual XInput device
        /// </summary>
        /// <param name="Player">XInput ID of the device to create and plug (From 1 To 4)</param>
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
                                Logger.WriteLog("Plugged P1 virtual Gamepad to port " + i.ToString());
                                _Player1_X360_Gamepad_Port = i;
                            }
                            else if (Player == 2)
                            {
                                Logger.WriteLog("Plugged P2 virtual Gamepad to port " + i.ToString());
                                _Player2_X360_Gamepad_Port = i;
                            }
                            break;
                        }
                        else
                            Logger.WriteLog("Failed to plug virtual GamePad to port " + i.ToString() + ". (Port already used ?)");
                    }
                }
                else
                {
                    Logger.WriteLog("ScpBus driver not found or not installed");
                }
            }
            else
            {
                Logger.WriteLog("XOutputManager Creation Failed !");
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
                        Logger.WriteLog("Succesfully unplug P1 virtual Gamepad on port " + _Player1_X360_Gamepad_Port.ToString());
                        return true;
                    }
                    else
                    {
                        Logger.WriteLog("Failed to unplug P1 virtual Gamepad on port " + _Player1_X360_Gamepad_Port.ToString());
                        return false;
                    }
                }
                else if (Player == 2 && _Player2_X360_Gamepad_Port != 0)
                {
                    if (_XOutputManager.Unplug(_Player2_X360_Gamepad_Port, true))
                    {
                        Logger.WriteLog("Succesfully unplug P2 virtual Gamepad on port " + _Player2_X360_Gamepad_Port.ToString());
                        return true;
                    }
                    else
                    {
                        Logger.WriteLog("Failed to unplug P2 virtual Gamepad on port " + _Player2_X360_Gamepad_Port.ToString());
                        return false;
                    }
                }
            }
            return true;
        }

        #endregion

        #region Keyboard SendKeys

        /// <summary>
        /// Simulate a keyboard key action (up or down)
        /// </summary>
        /// <param name="Keycode">Hardware ScanCode of the key to simulate</param>
        /// <param name="KeybdInputFlags">State of the key. (0=Down, 1 = Up)</param>
        private void SendKey(HardwareScanCode Keycode, KeybdInputFlags KeybdInputFlags)
        {
            INPUT[] InputData = new INPUT[1];

            InputData[0].type = InputType.INPUT_KEYBOARD;
            InputData[0].ki.wScan = Keycode;
            InputData[0].ki.dwFlags = KeybdInputFlags;
            InputData[0].ki.time = 0;
            InputData[0].ki.dwExtraInfo = IntPtr.Zero;

            if ( Win32API.SendInput(1, InputData, Marshal.SizeOf(typeof(INPUT))) == 0)
            {
                Logger.WriteLog("SendInput API failed : wScan=" + Keycode.ToString() + ", dwFlags=" + KeybdInputFlags.ToString());
                Logger.WriteLog("GetLastError returned : " + Marshal.GetLastWin32Error().ToString());
            }
        }

        /// <summary>
        /// Send KeyUp and KeyDown separated by a desired delay
        /// </summary>
        /// <param name="Keycode">DirectInput Keycode (hardware scan code)</param>
        /// <param name="DelayPressed">Delay in milliseconds</param>
        protected void SendKeyStroke(HardwareScanCode Keycode, int DelayPressed)
        {
            SendKeyDown(Keycode);
            System.Threading.Thread.Sleep(DelayPressed);
            SendKeyUp(Keycode);
        }
        protected void SendKeyDown(HardwareScanCode Keycode)
        {
            SendKey(Keycode, KeybdInputFlags.KEYEVENTF_SCANCODE);
        }
        protected void SendKeyUp(HardwareScanCode Keycode)
        {
            SendKey(Keycode, KeybdInputFlags.KEYEVENTF_KEYUP | KeybdInputFlags.KEYEVENTF_SCANCODE);
        }
        
        /// <summary>
        /// VirtualKeyCode inputs to send
        /// </summary>
        /// <param name="Keycode"></param>
        protected void Send_VK_KeyDown(VirtualKeyCode Keycode)
        {
            Win32API.keybd_event(Keycode, 0, KeybdInputFlags.KEYEVENTF_EXTENDEDKEY | 0, 0);
        }
        protected void Send_VK_KeyUp(VirtualKeyCode Keycode)
        {
            Win32API.keybd_event(Keycode, 0, KeybdInputFlags.KEYEVENTF_EXTENDEDKEY | KeybdInputFlags.KEYEVENTF_KEYUP, 0);
        }

        /// <summary>
        /// Convert a HardwareScanCode to a corresponding VirtualKeyCode
        /// </summary>
        /// <param name="ScanCode">Hardware Scancode to convert</param>
        /// <returns></returns>
        public VirtualKeyCode MapScanCodeToVirtualKeyCode(HardwareScanCode ScanCode)
        {
            UInt32 Vk = Win32API.MapVirtualKey((UInt32)ScanCode, VirtualKeyMapType.MAPVK_VSC_TO_VK);
            return (VirtualKeyCode)Vk;
        }

        /// <summary>
        /// Convert a VirtualScanCode to a corresponding HardwareScanCode
        /// </summary>
        /// <param name="ScanCode">Hardware Scancode to convert</param>
        /// <returns></returns>
        public HardwareScanCode MapScanCodeToVirtualKeyCode(VirtualKeyCode ScanCode)
        {
            UInt32 Vk = Win32API.MapVirtualKey((UInt32)ScanCode, VirtualKeyMapType.MAPVK_VK_TO_VSC);
            return (HardwareScanCode)Vk;
        }

        #endregion

        #region LowLevel Hooks

        /// <summary>
        /// Low-Level mouse hook callback to process data.
        /// This procedure will be override by each Game according to it's needs
        /// </summary>
        public virtual IntPtr MouseHookCallback(IntPtr MouseHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            return Win32API.CallNextHookEx(MouseHookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// Low-Level keyboard hook callback to process data.
        /// This procedure will be override by each Game according to it's needs
        /// </summary>
        public virtual IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            return Win32API.CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
        }           

        #endregion

        /// <summary>
        /// Select a specific window from the target process where the title contains a wanted string
        /// </summary>
        /// <param name="TargetWindowTitle"></param>
        /// <returns></returns>
        protected bool FindGameWindow_Contains(string TargetWindowTitle)
        {
            foreach (IntPtr handle in EnumerateProcessWindowHandles(_TargetProcess.Id))
            {
                int length = Win32API.GetWindowTextLength(handle);
                if (length >= 0)
                {
                    StringBuilder builder = new StringBuilder(length);
                    Win32API.GetWindowText(handle, builder, length + 1);
                    string WindowTitle = builder.ToString();
                    Logger.WriteLog("Found a window : Handle = 0x" + handle.ToString("X8") + ", Title = " + WindowTitle);
                    if (WindowTitle.StartsWith("FPS:") || WindowTitle.Contains(TargetWindowTitle))
                    {
                        _GameWindowHandle = handle;
                        Logger.WriteLog("=> Selecting 0x" + handle.ToString("X8") + " as game Window Handle");
                        Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                        Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
                        Logger.WriteLog("MainWindowHandle = 0x" + _TargetProcess.MainWindowHandle.ToString("X16"));
                        Logger.WriteLog("MainWindowTitle" + _TargetProcess.MainWindowTitle);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Select a specific window from the target process where the title is excatly the specific string
        /// </summary>
        /// <param name="TargetWindowTitle"></param>
        /// <returns></returns>
        protected bool FindGameWindow_Equals(string TargetWindowTitle)
        {
            foreach (IntPtr handle in EnumerateProcessWindowHandles(_TargetProcess.Id))
            {
                int length = Win32API.GetWindowTextLength(handle);
                if (length >= 0)
                {
                    StringBuilder builder = new StringBuilder(length);
                    Win32API.GetWindowText(handle, builder, length + 1);
                    string WindowTitle = builder.ToString();
                    Logger.WriteLog("Found a window : Handle = 0x" + handle.ToString("X8") + ", Title = " + WindowTitle);
                    if (WindowTitle.StartsWith("FPS:") || WindowTitle.Equals(TargetWindowTitle))
                    {
                        _GameWindowHandle = handle;
                        Logger.WriteLog("=> Selecting 0x" + handle.ToString("X8") + " as game Window Handle");
                        Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                        Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
                        Logger.WriteLog("MainWindowHandle = 0x" + _TargetProcess.MainWindowHandle.ToString("X16"));
                        Logger.WriteLog("MainWindowTitle" + _TargetProcess.MainWindowTitle);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get the list of Windows for a given process
        /// </summary>
        protected static IEnumerable<IntPtr> EnumerateProcessWindowHandles(int processId)
        {
            List<IntPtr> handles = new List<IntPtr>();

            foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
            {
                Win32API.EnumThreadWindows(thread.Id, (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);
            }
            return handles;
        }

        /// <summary>
        /// Create a minimal output list with only orientation and pause
        /// </summary>
        public void CreatePauseOutputList()
        {
            _Outputs.Clear();
            _Outputs = new List<GameOutput>();

            _Outputs.Add(new GameOutput(OutputDesciption.MameOrientation, OutputId.MameOrientation));
            _Outputs.Add(new GameOutput(OutputDesciption.MamePause, OutputId.MamePause));

            SetOutputValue(OutputId.MameOrientation, 0);
            SetOutputValue(OutputId.MamePause, 1);
        }
    }
}
