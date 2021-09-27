using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;
using DsCore.MameOutput;

namespace DemulShooter
{
    class Game_Friction : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\windows\friction";

        /*** Memory Addresses for VSIOBOARD V1.0 Hack***/
        private UInt32 _P1_X_Injection_Offset = 0x00001F5B;
        private UInt32 _P1_Y_Injection_Offset = 0x00001F67;
        private UInt32 _P2_X_Injection_Offset = 0x00002314;
        private UInt32 _P2_Y_Injection_Offset = 0x00002320;
        private UInt32 _P1_Trigger_Injection_Offset = 0x00001F00;
        private UInt32 _P1_Reload_Injection_Offset = 0x00001F85;
        private UInt32 _P2_Trigger_Injection_Offset = 0x000022B9;
        private UInt32 _P2_Reload_Injection_Offset = 0x0000233E;

        /*** Memory Addresses for VSIOBOARD V3.0 Hack***/
        private UInt32 _X_Injection_Offset = 0x00002228;
        private UInt32 _X_Injection_Return_Offset = 0x00002230;
        private UInt32 _Y_Injection_Offset = 0x00002252;
        private UInt32 _Y_Injection_Return_Offset = 0x00002259;
        private UInt32 _Trigger_Injection_Offset = 0x00002269;
        private UInt32 _Trigger_Injection_Return_Offset = 0x000022AA;
        private UInt32 _Reload_Injection_Offset = 0x00002307;
        private UInt32 _Reload_Injection_Return_Offset = 0x00002348;

        /*** Common Memory Addresses ***/
        private UInt32 _P1_X_CaveAddress = 0;
        private UInt32 _P1_Y_CaveAddress = 0;
        private UInt32 _P1_Trigger_CaveAddress = 0;
        private UInt32 _P1_Reload_CaveAddress = 0;
        private UInt32 _P2_X_CaveAddress = 0;
        private UInt32 _P2_Y_CaveAddress = 0;
        private UInt32 _P2_Trigger_CaveAddress = 0;
        private UInt32 _P2_Reload_CaveAddress = 0;

        private IntPtr _VsIOBoard_Module_BaseAddress = IntPtr.Zero;

        //Custom Outputs
        private int[] _LastLife = new int[] { 0, 0 };
        private int[] _LastAmmo = new int[] { 0, 0 };
        private int[] _Life = new int[] { 0, 0 };
        private int[] _Ammo = new int[] { 0, 0 };

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Friction(String RomName, double _ForcedXratio, bool Verbose)
            : base(RomName, "Friction", _ForcedXratio, Verbose)
        {
            _KnownMd5Prints.Add("Friction original dump", "931146ddcac8429634401d08158b8bec");
            _KnownMd5Prints.Add("Friction hacked VSIOBOARD.dll - v1.0", "9bbe8c9bf916826d6ab22703f6d83f7b");
            _KnownMd5Prints.Add("Friction hacked VSIOBOARD.dll - v2.0", "a86819412ce393fdb17e6ae6dbd5bd27");
            _KnownMd5Prints.Add("Friction hacked VSIOBOARD.dll - v3.0", "03c872a3d141ee186412dfc0636510bc");
            _KnownMd5Prints.Add("Friction original vsIOBoard.dll", "25a0495caecaf9bee66b09f13da5e8fa");
            _tProcess.Start();

            Logger.WriteLog("Waiting for Windows " + _RomName + " game to hook.....");
        }

        /// <summary>
        /// Timer event when looking for Demul Process (auto-Hook and auto-close)
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
                            if (m.ModuleName.ToLower().Equals("vsioboard.dll"))
                            {
                                _VsIOBoard_Module_BaseAddress = m.BaseAddress;
                                if (_VsIOBoard_Module_BaseAddress != IntPtr.Zero)
                                {                                    
                                    Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                    Logger.WriteLog("Friction.exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8") + ", vsIOBoard.dll = 0x" + _VsIOBoard_Module_BaseAddress.ToString("X8"));
                                    String VsIoBoardDll_Path = _TargetProcess.MainModule.FileName.Replace(_Target_Process_Name + ".exe", "vsioboard.dll");                                  
                                    CheckMd5(VsIoBoardDll_Path);
                                    ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                                    SetHack();
                                    _ProcessHooked = true;
                                    RaiseGameHookedEvent();
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Error trying to hook " + _Target_Process_Name + ".exe");
                    Logger.WriteLog(ex.Message.ToString());
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
                    //Window size
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X => 160 - 480 ==> 321
                    //Y => 120 - 360 ==> 241                 
                    double dMaxX = 321.0;
                    double dMaxY = 241.0;

                    /*double dMinX = 160.0;
                    double dMaxX = 480.0;
                    double dMinY = 120.0;
                    double dMaxY = 360.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;

                    //Test for forced ration 1.33 (4:3)
                    double ForcedRatio = 1.33;
                    double GameHeight = TotalResY;
                    double GameWidth = TotalResY * ForcedRatio;
                    double SideOffsetPercent = ((TotalResX - GameWidth) / 2) / TotalResX;
                    Logger.WriteLog("Game Viewport size (Px) = [ " + GameWidth + "x" + GameHeight + " ]");
                    Logger.WriteLog("SideBars Width (%) = " + SideOffsetPercent.ToString());

                    double HorizontalRatio = TotalResX / GameWidth;
                    dRangeX = 640.0 * HorizontalRatio;
                    dMaxX = (dRangeX / 2);
                    dMinX = -dMaxX;
                    Logger.WriteLog("Horizontal Ratio = " + HorizontalRatio.ToString());
                    Logger.WriteLog("New dMaxX = " + dMaxX.ToString());
                    Logger.WriteLog("New dMinX = " + dMinX.ToString());*/

                    PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX) + 160);
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY) + 120);
                    if (PlayerData.RIController.Computed_X < 160)
                        PlayerData.RIController.Computed_X = 160;
                    if (PlayerData.RIController.Computed_Y < 120)
                        PlayerData.RIController.Computed_Y = 120;
                    if (PlayerData.RIController.Computed_X > 480)
                        PlayerData.RIController.Computed_X = 480;
                    if (PlayerData.RIController.Computed_Y > 360)
                        PlayerData.RIController.Computed_Y = 360;

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
            CreateDataBank();
               
            //VSIOBOARD.dd v1.0 has a different Hack
            if (_TargetProcess_Md5Hash == _KnownMd5Prints["Friction hacked VSIOBOARD.dll - v1.0"])
            {
                SetHack_P1_X();
                SetHack_P1_Y();
                SetHack_P1_Trigger();
                SetHack_P1_Reload();
                SetHack_P2_X();
                SetHack_P2_Y();
                SetHack_P2_Trigger();
                SetHack_P2_Reload();
            }
            //VSIOBOARD.dll v2.0 and v3.0 are sharing the same Hack
            else
            {
                SetHack_X();
                SetHack_Y();
                SetHack_Trigger();
                SetHack_Reload();
            }

            Logger.WriteLog("Friction Arcade Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Custom data storage
        /// </summary>
        private void CreateDataBank()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            _P1_X_CaveAddress = CaveMemory.CaveAddress;
            _P2_X_CaveAddress = CaveMemory.CaveAddress + 0x08;
            _P1_Y_CaveAddress = CaveMemory.CaveAddress + 0x10;
            _P2_Y_CaveAddress = CaveMemory.CaveAddress + 0x18;
            _P1_Trigger_CaveAddress = CaveMemory.CaveAddress + 0x20;
            _P2_Trigger_CaveAddress = CaveMemory.CaveAddress + 0x28;
            _P1_Reload_CaveAddress = CaveMemory.CaveAddress + 0x30;
            _P2_Reload_CaveAddress = CaveMemory.CaveAddress + 0x38;

            Logger.WriteLog("Custom data will be stored at : 0x" + _P1_X_CaveAddress.ToString("X8"));
        }
        
        #region VSIOBOARD V1.0 Hack

        /// <summary>
        /// Not having found a secured way to access X and Y offsets, we can't NOP
        /// procedures to inject custom values...
        /// Instead we will modify each procedure so that it will read our values instad of original ones
        /// </summary>
        private void SetHack_P1_X()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer;
            byte[] b = BitConverter.GetBytes(_P1_X_CaveAddress);
            //mov eax, [_P1_X_CaveAddress]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(b);
            //mov [esp + 9A], ax
            CaveMemory.Write_StrBytes("66 89 84 24 9A 00 00 00");
            CaveMemory.Write_jmp((UInt32)_VsIOBoard_Module_BaseAddress + _P1_X_Injection_Offset + 8);

            Logger.WriteLog("Adding P1_X Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_VsIOBoard_Module_BaseAddress + _P1_X_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_VsIOBoard_Module_BaseAddress + _P1_X_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }
        private void SetHack_P2_X()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer;
            byte[] b = BitConverter.GetBytes(_P2_X_CaveAddress);
            //mov eax, [_P2_X_CaveAddress]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(b);
            //mov [esp + AA], ax
            CaveMemory.Write_StrBytes("66 89 84 24 AA 00 00 00");
            CaveMemory.Write_jmp((UInt32)_VsIOBoard_Module_BaseAddress + _P2_X_Injection_Offset + 8);

            Logger.WriteLog("Adding P2_X Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_VsIOBoard_Module_BaseAddress + _P2_X_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_VsIOBoard_Module_BaseAddress + _P2_X_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }
        private void SetHack_P1_Y()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer;
            byte[] b = BitConverter.GetBytes(_P1_Y_CaveAddress);
            //mov eax, [_P1_Y_CaveAddress]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(b);
            //mov [esp + 9C], ax
            CaveMemory.Write_StrBytes("66 89 84 24 9C 00 00 00");
            CaveMemory.Write_jmp((UInt32)_VsIOBoard_Module_BaseAddress + _P1_Y_Injection_Offset + 8);

            Logger.WriteLog("Adding P1_Y Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_VsIOBoard_Module_BaseAddress + _P1_Y_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_VsIOBoard_Module_BaseAddress + _P1_Y_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }
        private void SetHack_P2_Y()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer;
            byte[] b = BitConverter.GetBytes(_P2_Y_CaveAddress);
            //mov eax, [_P2_Y_CaveAddress]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(b);
            //mov [esp + AC], ax
            CaveMemory.Write_StrBytes("66 89 84 24 AC 00 00 00");
            CaveMemory.Write_jmp((UInt32)_VsIOBoard_Module_BaseAddress + _P2_Y_Injection_Offset + 8);

            Logger.WriteLog("Adding P2_Y Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_VsIOBoard_Module_BaseAddress + _P2_Y_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_VsIOBoard_Module_BaseAddress + _P2_Y_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        /// <summary>
        /// For buttons, the game is calling GetAsyncKeyState() for each Buttons.
        /// So for Trigger and Reload Buttons, we will modify the procedure to make it read our own values
        /// and overwrite GetAsyncKeyState return value.
        /// This way, the original behaviour of autofire for machinegun should be intact.
        /// </summary>
        private void SetHack_P1_Trigger()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer;
            byte[] b = BitConverter.GetBytes(_P1_Trigger_CaveAddress);
            //mov eax, [_P1_Trigger_CaveAddress]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(b);
            //shr eax,0F
            CaveMemory.Write_StrBytes("C1 E8 0F");
            CaveMemory.Write_jmp((UInt32)_VsIOBoard_Module_BaseAddress + _P1_Trigger_Injection_Offset + 6);

            Logger.WriteLog("Adding P1_Trigger Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_VsIOBoard_Module_BaseAddress + _P1_Trigger_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_VsIOBoard_Module_BaseAddress + _P1_Trigger_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }
        private void SetHack_P1_Reload()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer;
            byte[] b = BitConverter.GetBytes(_P1_Reload_CaveAddress);
            //mov eax, [_P1_Reload_CaveAddress]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(b);
            //shr eax,0F
            CaveMemory.Write_StrBytes("C1 E8 0F");
            CaveMemory.Write_jmp((UInt32)_VsIOBoard_Module_BaseAddress + _P1_Reload_Injection_Offset + 6);

            Logger.WriteLog("Adding P1_Reload Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = CaveMemory.CaveAddress - ((UInt32)_VsIOBoard_Module_BaseAddress + _P1_Reload_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_VsIOBoard_Module_BaseAddress + _P1_Reload_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

        }
        private void SetHack_P2_Trigger()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer;
            byte[] b = BitConverter.GetBytes(_P2_Trigger_CaveAddress);
            //mov eax, [_P2_Trigger_CaveAddress]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(b);
            //shr eax,0F
            CaveMemory.Write_StrBytes("C1 E8 0F");
            CaveMemory.Write_jmp((UInt32)_VsIOBoard_Module_BaseAddress + _P2_Trigger_Injection_Offset + 6);

            Logger.WriteLog("Adding P2_Trigger Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_VsIOBoard_Module_BaseAddress + _P2_Trigger_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_VsIOBoard_Module_BaseAddress + _P2_Trigger_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }
        private void SetHack_P2_Reload()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer;
            byte[] b = BitConverter.GetBytes(_P2_Reload_CaveAddress);
            //mov eax, [_P2_Reload_CaveAddress]
            CaveMemory.Write_StrBytes("A1");
            CaveMemory.Write_Bytes(b);
            //shr eax,0F
            CaveMemory.Write_StrBytes("C1 E8 0F");
            CaveMemory.Write_jmp((UInt32)_VsIOBoard_Module_BaseAddress + _P2_Reload_Injection_Offset + 6);

            Logger.WriteLog("Adding P2_Reload Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = CaveMemory.CaveAddress - ((UInt32)_VsIOBoard_Module_BaseAddress + _P2_Reload_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_VsIOBoard_Module_BaseAddress + _P2_Reload_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        #endregion

        #region VSIOBOARD V3.0 Hack

        /// <summary>
        /// For this game, we are hokking inside the vsIOBoard.dll, in the loop where Players axis are calculated
        /// We are injecting our own data after the final calculated original values.
        /// The loop is the same for P1 and P2, so we will use the loop index (base 4) at ESP+20 to automatically 
        /// access our own P1 or P2 axis X data without creating a codecave for each player
        /// P2_X_Address = P1_X_Address + (2*4)
        /// </summary>
        private void SetHack_X()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer;
            //comisd xmm0,xmm4
            CaveMemory.Write_StrBytes("66 0F 2F C4");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov eax, _P1_X_CaveAddress
            byte[] b = BitConverter.GetBytes(_P1_X_CaveAddress);
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(b);
            //mov ebx, [esp+20]
            CaveMemory.Write_StrBytes("8B 5C 24 20");
            //shl ebx,1
            CaveMemory.Write_StrBytes("D1 E3");
            //add eax, ebx
            CaveMemory.Write_StrBytes("01 D8");
            //movsd xmm2, [eax]
            CaveMemory.Write_StrBytes("F2 0F 10 10");
            //movsd [ecx],xmm2
            CaveMemory.Write_StrBytes("F2 0F 11 11");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            CaveMemory.Write_jmp((UInt32)_VsIOBoard_Module_BaseAddress + _X_Injection_Return_Offset);

            Logger.WriteLog("Adding Axis_X Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_VsIOBoard_Module_BaseAddress + _X_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_VsIOBoard_Module_BaseAddress + _X_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);      
        }
        private void SetHack_Y()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer;
            //push eax
            CaveMemory.Write_StrBytes("50");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov eax, _P1_Y_CaveAddress
            byte[] b = BitConverter.GetBytes(_P1_Y_CaveAddress);
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(b);
            //mov ebx, [esp+20]
            CaveMemory.Write_StrBytes("8B 5C 24 20");
            //shl ebx,1
            CaveMemory.Write_StrBytes("D1 E3");
            //add eax, ebx
            CaveMemory.Write_StrBytes("01 D8");
            //movsd xmm1, [eax]
            CaveMemory.Write_StrBytes("F2 0F 10 08");
            //movsd [ecx],xmm2
            CaveMemory.Write_StrBytes("F2 0F 11 0F");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movzx eax, ax
            CaveMemory.Write_StrBytes("0F B7 C0");
            CaveMemory.Write_jmp((UInt32)_VsIOBoard_Module_BaseAddress + _Y_Injection_Return_Offset);

            Logger.WriteLog("Adding Axis_Y Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_VsIOBoard_Module_BaseAddress + _Y_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_VsIOBoard_Module_BaseAddress + _Y_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);       
        }

        /// <summary>
        /// For this game, we are hokking inside the vsIOBoard.dll, in the loop Gun data is generated.
        /// We are injecting our codecave to force the value of InputTrigger without letting the game test if it's keyboard or mouse based
        /// The vsIOBoard.dll is handling autofire in it's loop so we can't simple force "trigger press value" inside the main exe.
        /// The loop is the same for P1 and P2, so we will use the loop index (base 4) at ESP+10 to automatically 
        /// access our own P1 or P2 Trigger data without creating a codecave for each player
        /// P2_Trigger_Address = P1_Trigger_Address + (2*4)
        /// If Down, injecting 0x7FFF, else injecting 0x0000
        private void SetHack_Trigger()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer;
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov eax, _P1_Trigger_CaveAddress
            byte[] b = BitConverter.GetBytes(_P1_Trigger_CaveAddress);
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(b);
            //mov ebx, [esp+10]
            CaveMemory.Write_StrBytes("8B 5C 24 1C");
            //shl ebx,1
            CaveMemory.Write_StrBytes("D1 E3");
            //add eax, ebx
            CaveMemory.Write_StrBytes("01 D8");
            //mov esi,[eax]
            CaveMemory.Write_StrBytes("8B 30");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            CaveMemory.Write_jmp((UInt32)_VsIOBoard_Module_BaseAddress + _Trigger_Injection_Return_Offset);

            Logger.WriteLog("Adding Trigger Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_VsIOBoard_Module_BaseAddress + _Trigger_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_VsIOBoard_Module_BaseAddress + _Trigger_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);       
        }
        /// <summary>
        /// Same thing as Trigger
        /// </summary>
        private void SetHack_Reload()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer;
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov eax, _P1_Trigger_CaveAddress
            byte[] b = BitConverter.GetBytes(_P1_Reload_CaveAddress);
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(b);
            //mov ebx, [esp+10]
            CaveMemory.Write_StrBytes("8B 5C 24 1C");
            //shl ebx,1
            CaveMemory.Write_StrBytes("D1 E3");
            //add eax, ebx
            CaveMemory.Write_StrBytes("01 D8");
            //mov edi,[eax]
            CaveMemory.Write_StrBytes("8B 38");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            CaveMemory.Write_jmp((UInt32)_VsIOBoard_Module_BaseAddress + _Reload_Injection_Return_Offset);

            Logger.WriteLog("Adding Reload Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_VsIOBoard_Module_BaseAddress + _Reload_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_VsIOBoard_Module_BaseAddress + _Reload_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        #endregion

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>
        public override void SendInput(PlayerSettings PlayerData)
        {
            if (_TargetProcess_Md5Hash == _KnownMd5Prints["Friction hacked VSIOBOARD.dll - v1.0"])
            {
                byte[] bufferX = BitConverter.GetBytes((Int32)PlayerData.RIController.Computed_X);
                byte[] bufferY = BitConverter.GetBytes((Int32)PlayerData.RIController.Computed_Y);

                //VSIOBOARD.dll v1.0 has it's own different Input handling system
                if (PlayerData.ID == 1)
                {
                    WriteBytes(_P1_X_CaveAddress, bufferX);
                    WriteBytes(_P1_Y_CaveAddress, bufferY);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        WriteBytes(_P1_Trigger_CaveAddress, BitConverter.GetBytes(0x00008000));
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        WriteBytes(_P1_Trigger_CaveAddress, BitConverter.GetBytes(0x00000000));

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                        WriteBytes(_P1_Reload_CaveAddress, BitConverter.GetBytes(0x00008000));
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                        WriteBytes(_P1_Reload_CaveAddress, BitConverter.GetBytes(0x00000000));
                }
                else if (PlayerData.ID == 2)
                {
                    WriteBytes(_P2_X_CaveAddress, bufferX);
                    WriteBytes(_P2_Y_CaveAddress, bufferY);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        WriteBytes(_P2_Trigger_CaveAddress, BitConverter.GetBytes(0x00008000));
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        WriteBytes(_P2_Trigger_CaveAddress, BitConverter.GetBytes(0x00000000));

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                        WriteBytes(_P2_Reload_CaveAddress, BitConverter.GetBytes(0x00008000));
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                        WriteBytes(_P2_Reload_CaveAddress, BitConverter.GetBytes(0x00000000));
                }
            }
            //VSIOBOARD.dll v2.0 and v3.0 are sharing the same Input handling system
            else
            {

                byte[] bufferX = BitConverter.GetBytes(Convert.ToDouble(PlayerData.RIController.Computed_X));
                byte[] bufferY = BitConverter.GetBytes(Convert.ToDouble(PlayerData.RIController.Computed_Y));

                if (PlayerData.ID == 1)
                {
                    WriteBytes(_P1_X_CaveAddress, bufferX);
                    WriteBytes(_P1_Y_CaveAddress, bufferY);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        WriteBytes(_P1_Trigger_CaveAddress, new byte[] { 0xFF, 0x7F });
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        WriteBytes(_P1_Trigger_CaveAddress, new byte[] { 0x00, 0x00 });

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                        WriteBytes(_P1_Reload_CaveAddress, new byte[] { 0xFF, 0x7F });
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                        WriteBytes(_P1_Reload_CaveAddress, new byte[] { 0x00, 0x00 });
                }
                else if (PlayerData.ID == 2)
                {
                    WriteBytes(_P2_X_CaveAddress, bufferX);
                    WriteBytes(_P2_Y_CaveAddress, bufferY);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        WriteBytes(_P2_Trigger_CaveAddress, new byte[] { 0xFF, 0x7F });
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        WriteBytes(_P2_Trigger_CaveAddress, new byte[] { 0x00, 0x00 });

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                        WriteBytes(_P2_Reload_CaveAddress, new byte[] { 0xFF, 0x7F });
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                        WriteBytes(_P2_Reload_CaveAddress, new byte[] { 0x00, 0x00 });
                }
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P1_CtmLmpStart, OutputId.P1_CtmLmpStart, 500));
            _Outputs.Add(new SyncBlinkingGameOutput(OutputDesciption.P2_CtmLmpStart, OutputId.P2_CtmLmpStart, 500));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Clip, OutputId.P2_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            UInt32 _GameDataPtr = ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + 0x00145088);            
            //In this game, the engine is swapping P1/P2 data according to the 1st Player to be started
            //To know which data is corresponding to which player, we need to process a little bit...
            //When players are uninitialized, both values are set to [-1]
            //When a player starts a game, PlayerA get a value of [0] if it's Player1, or [1] if it's player 2. This is used later by the game
            //to add an offset to determine which player is playing for accessing data.
            //When a second player is entering the game, PlayerB gets [0] if it's player1, [1] if it's player2
            byte[] Players_Offset = new byte[] { 0x00, 0x00 };
            Players_Offset[0] = ReadByte(_GameDataPtr + 0x28 + 0x45C);
            Players_Offset[1] = ReadByte(_GameDataPtr + 0x28 + 0x460);

            Array.Clear(_Life, 0, _Life.Length);
            Array.Clear(_Ammo, 0, _Ammo.Length);
            int[] Clip = new int[] {0, 0};
            int[] StartLmp = new int[] {-1, -1};

            for (uint i = 0; i < Players_Offset.Length; i++)
            {
                if (Players_Offset[i] != 0xFF)
                {
                    //We can now look at the "InGame" flag for the player
                    //1 = OnGame, 0 = Not Playing (GameOver or Continue)
                    if (ReadByte(_GameDataPtr + 0x28 + 0x439 + i) == 1)
                    {
                        //Force Start Lamp to Off
                        StartLmp[Players_Offset[i]] = 0;

                        _Life[Players_Offset[i]] = ReadByte(_GameDataPtr + 0x28 + 0x44C + (i * 4));
                        _Ammo[Players_Offset[i]] = ReadByte(ReadPtr(_GameDataPtr + 0x28 + 0x560 + (i * 4)) + 8);

                        //Custom Recoil
                        if (_Ammo[Players_Offset[i]] < _LastAmmo[Players_Offset[i]])
                        {
                            if (i == 0)
                                SetOutputValue(OutputId.P1_CtmRecoil, 1);
                            else if (i == 1)
                                SetOutputValue(OutputId.P2_CtmRecoil, 1);
                        }

                        //[Clip Empty] custom Output
                        if (_Ammo[Players_Offset[i]] > 0)
                            Clip[Players_Offset[i]] = 1;

                        //[Damaged] custom Output                
                        if (_Life[Players_Offset[i]] < _LastLife[Players_Offset[i]])
                        {
                            if (i == 0)
                                SetOutputValue(OutputId.P1_Damaged, 1);
                            else if (i == 1)
                                SetOutputValue(OutputId.P2_Damaged, 1);
                        }
                    }
                }
            }

            for (int i = 0; i < 2; i++)
            {
                _LastAmmo[i] = _Ammo[i];
                _LastLife[i] = _Life[i];
            }

            SetOutputValue(OutputId.P1_CtmLmpStart, StartLmp[0]);
            SetOutputValue(OutputId.P2_CtmLmpStart, StartLmp[1]);
            SetOutputValue(OutputId.P1_Ammo, _Ammo[0]);
            SetOutputValue(OutputId.P2_Ammo, _Ammo[1]);
            SetOutputValue(OutputId.P1_Clip, Clip[0]);
            SetOutputValue(OutputId.P2_Clip, Clip[1]);
            SetOutputValue(OutputId.P1_Life, _Life[0]);
            SetOutputValue(OutputId.P2_Life, _Life[1]);
            SetOutputValue(OutputId.Credits, ReadByte(ReadPtr((UInt32)_TargetProcess_MemoryBaseAddress + 0x001450C4)));
        }

        #endregion
    }
}
