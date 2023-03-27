using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.MemoryX64;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooterX64
{
    public class Game_Es3Tc5 : Game
    {
        /*** MEMORY ADDRESSES **/
        //Standalone App Hack
        private UInt64 _Standalone_Axis_CaveAddress = 0;
        private const Int32 _STANDALONE_X_OFFSET = 0;
        private const Int32 _STANDALONE_Y_OFFSET = 0x08;
        private const Int32 _STANDALONE_MAXX_OFFSET = 0x10;
        private const Int32 _STANDALONE_MAXY_OFFSET = 0x18;

        private UInt64 SCREEN_WIDTH_OFFSET = 0x0185E388;
        private UInt64 SCREEN_HEIGHT_OFFSET = 0x0185E38C;
        private UInt64 GUN_MAX_X_OFFSET = 0x01697E78;
        private UInt64 GUN_MAX_Y_OFFSET = 0x01697E7C;
        private int _Gun_Max_X = 0;
        private int _Gun_Max_Y = 0;

        private UInt64 _Standalone_Trigger_Ptr_Offset = 0x185E4D0;
        private UInt64 _Standalone_Trigger_BaseAddress = 0;
        private NopStruct _Standalone_Nop_Trigger_On = new NopStruct(0xA24357, 11);
        private NopStruct _Standalone_Nop_Trigger_Off = new NopStruct(0xA24462, 11);

        //Because of 64bits process, I only know how to alloc memory and use a long jump (14 bytes instruction !)
        //In this case, I have to use a 15 bytes long "0xCC" between 2 function to write the long jump
        //So the original hack will short jump (not enough available bytes) to the place where I can put the long jump
        private UInt64 _Standalone_Injection_Offset = 0x0006B063;
        private UInt64 _LongJump_Offset = 0x000ABA41;
        private UInt64 _Standalone_Injection_Return_Offset = 0x0006B069;

        //JConfig Hack
        private UInt64 _JVS_TriggerButton_CaveAddress = 0;
        private UInt64 _JVS_Trigger_LongJump_Offset = 0x000ABB21;
        private UInt64 _JVS_Trigger_Injection_Offset = 0xA55035;
        private UInt64 _JVS_Trigger_Injection_Return_Offset = 0xA5503C;

        private UInt64 _JVS_WeaponButton_CaveAddress = 0;
        private UInt64 _JVS_Weapon_LongJump_Offset = 0x000AB821;
        private UInt64 _JVS_Weapon_Injection_Offset = 0xA56E4E;
        private UInt64 _JVS_Weapon_Injection_Return_Offset = 0xA56E54;
        
        private UInt64 _JVS_AxisX_CaveAddress = 0;
        private UInt64 _JVS_AxisY_CaveAddress = 0;
        private UInt64 _JVS_Axis_LongJump_Offset = 0x000ABA41;
        private UInt64 _JVS_Axis_Injection_Offset = 0x6B33A;
        private UInt64 _JVS_Axis_Injection_Return_Offset = 0x6B342;
        private float _FloatXvalue = 0;
        private float _FloatYvalue = 0;
        
        
        //Check instruction for game loaded
        private const UInt64 ROM_LOADED_CHECK_INSTRUCTION_OFFSET = 0x0006B060;

        //Custom Outputs
        private int _P1_LastLife = 0;
        private int _P1_LastAmmo = 0;
        private int _P1_LastWeapon = 0;
        private int _P1_Life = 0;
        private int _P1_Ammo = 0;
        private int _P1_Weapon = 0;
        private int _P1_LastRecoil = 0;

        //Flag to differenciate standalone/Jconfig hack to do
        private bool _IsStandalone = true;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_Es3Tc5(String RomName, bool DisableInputHack, bool Verbose) : base(RomName, "TimeCrisisGame-Win64-Shipping", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("TimeCrisisGame-Win64-Shipping.exe - Original Dump", "5297b9296708d4f83181f244ee2bc3db");
            _tProcess.Start();
            Logger.WriteLog("Waiting for Namco ES3 " + _RomName + " game to hook.....");
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
                            UInt64 aTest = (UInt64)_TargetProcess_MemoryBaseAddress + SCREEN_WIDTH_OFFSET;
                            byte[] buffer = ReadBytes((IntPtr)aTest, 4);
                            int x = BitConverter.ToInt32(buffer, 0);

                            aTest = (UInt64)_TargetProcess_MemoryBaseAddress + SCREEN_HEIGHT_OFFSET;
                            buffer = ReadBytes((IntPtr)aTest, 4);
                            int y = BitConverter.ToInt32(buffer, 0);

                            aTest = (UInt64)_TargetProcess_MemoryBaseAddress + GUN_MAX_X_OFFSET;
                            buffer = ReadBytes((IntPtr)aTest, 4);
                            _Gun_Max_X = BitConverter.ToInt32(buffer, 0);

                            aTest = (UInt64)_TargetProcess_MemoryBaseAddress + GUN_MAX_Y_OFFSET;
                            buffer = ReadBytes((IntPtr)aTest, 4);
                            _Gun_Max_Y = BitConverter.ToInt32(buffer, 0);

                            _Standalone_Trigger_BaseAddress = ReadPtrChain((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _Standalone_Trigger_Ptr_Offset), new UInt64[ ]{ 0 });                    
                    

                            if(x != 0 && y != 0 && _Standalone_Trigger_BaseAddress != 0)
                            {
                                //Check for Rslaucher ( = Jconfig use)
                                Process[] procs = Process.GetProcessesByName("rslauncher");
                                if (procs.Length > 0)
                                    _IsStandalone = false;

                                Logger.WriteLog("Screen size detected by game = [ " + x.ToString() + " x " + y.ToString() + " ]");
                                Logger.WriteLog("Maximum axis values = [ " + _Gun_Max_X.ToString() + " ; " + _Gun_Max_Y.ToString() + " ]");
                                Logger.WriteLog("Trigger Address = 0x" + _Standalone_Trigger_BaseAddress.ToString("X16"));
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
                                Logger.WriteLog("MainWindowHandle = 0x" + _TargetProcess.MainWindowHandle.ToString("X16"));
                                Logger.WriteLog("MainWindowTitle" + _TargetProcess.MainWindowTitle);
                                Logger.WriteLog("IsStandalone = " + _IsStandalone.ToString());

                                CheckExeMd5();
                                if (!_DisableInputHack)
                                    SetHack();
                                else
                                    Logger.WriteLog("Input Hack disabled");
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
                            }
                            else
                            {
                                Logger.WriteLog("ROM not Loaded...");
                            }
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
        /// Fullscreen mode causes issue with windows size, so for now this will only work with fullscreen mode
        /// Game resolution will be read in memory
        /// </summary>
        /// <param name="PlayerData"></param>
        /// <returns></returns>
        /*public override bool ClientScale(PlayerSettings PlayerData)
        {
            //Convert Screen location to Client location
            if (_TargetProcess != null)
            {
                //Window size
                Rect TotalRes = new Rect();
                Win32API.GetWindowRect(_TargetProcess.MainWindowHandle, ref TotalRes);

                Logger.WriteLog("Window position (Px) = [ " + TotalRes.Left + ";" + TotalRes.Top + " ]");

                PlayerData.RIController.Computed_X = PlayerData.RIController.Computed_X - TotalRes.Left;
                PlayerData.RIController.Computed_Y = PlayerData.RIController.Computed_Y - TotalRes.Top;
                Logger.WriteLog("Onclient window position (Px) = [ " + PlayerData.RIController.Computed_X + "x" + PlayerData.RIController.Computed_Y + " ]");

            }
            return true;
        }*/

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// Windowed mode is not working (can't get window size) so full screen mode only is simpler....
        /// </summary>
        public override bool GameScale(PlayerSettings PlayerData)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                if (_IsStandalone)
                {
                    return true;
                }
                else
                {
                    try
                    {
                        //Window size
                        Rect TotalRes = new Rect();
                        Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                        float TotalResX = TotalRes.Right - TotalRes.Left;
                        float TotalResY = TotalRes.Bottom - TotalRes.Top;

                        Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                        _FloatXvalue = PlayerData.RIController.Computed_X / TotalResX;
                        _FloatYvalue = PlayerData.RIController.Computed_Y / TotalResY;
                        Logger.WriteLog("Game scale result (float) = [ " + _FloatXvalue.ToString() + "; " + _FloatYvalue.ToString() + " ]");

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

        private void SetHack()
        {
            //Standalone (old) hack
            if (_IsStandalone)
            {
                SetHackAxis();

                UInt64 aTest = (UInt64)_TargetProcess_MemoryBaseAddress + GUN_MAX_X_OFFSET;
                byte[] buffer = ReadBytes((IntPtr)aTest, 4);
                WriteBytes((IntPtr)(_Standalone_Axis_CaveAddress + _STANDALONE_MAXX_OFFSET), buffer);

                aTest = (UInt64)_TargetProcess_MemoryBaseAddress + GUN_MAX_Y_OFFSET;
                buffer = ReadBytes((IntPtr)aTest, 4);
                WriteBytes((IntPtr)(_Standalone_Axis_CaveAddress + _STANDALONE_MAXY_OFFSET), buffer);
            }
            else
            {
                //JVS input memory hack
                Create_JVS_DataBank();
                SetHack_JVS_Trigger();
                SetHack_JVS_Weapon();
                SetHack_JVS_Axis();
                SetNops(_TargetProcess_MemoryBaseAddress, _Standalone_Nop_Trigger_On);  //JConfig does not disable mouse so we will to avoid conflict with lightgun handling
            }
            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// The game is working well with mouse but causes issue with a lightgun :
        /// Except in the menu (everything OK) it read Absolute values as relative movements -> stick in the corners
        /// This is a first try to inject data into memory on the fly, as I can't find a unique procedure for Axis writing.
        /// </summary>
        private void SetHackAxis()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            /* Because of 64bit asm, I dont know how to load a 64bit data segment address into a register.
             * But there is an instruction to load address with an offset from RIP (instruction pointer) 
             * That's why data are stored just after the code (to know the offset)
             * */
            _Standalone_Axis_CaveAddress = CaveMemory.CaveAddress + 0x40;
            Logger.WriteLog("_Standalone_Axis_CaveAddress = 0x" + _Standalone_Axis_CaveAddress.ToString("X16"));

            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[rsp+60]
            CaveMemory.Write_StrBytes("8B 44 24 60");
            //cmp eax,[RIP+45] => (MAx_X)
            CaveMemory.Write_StrBytes("3B 05 45 00 00 00");
            //je AxisX
            CaveMemory.Write_StrBytes("74 0B");
            //cmp eax,[RIP+45] => (MAx_Y)
            CaveMemory.Write_StrBytes("3B 05 45 00 00 00");
            //je AxisY
            CaveMemory.Write_StrBytes("74 0D");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //jmp exit
            CaveMemory.Write_StrBytes("EB 12");

            //AxisX:
            //pop rax
            CaveMemory.Write_StrBytes("58");
            ////mov rax, [RIP+20]     (X ==> _Standalone_Axis_CaveAddress + 0)
            CaveMemory.Write_StrBytes("48 8B 05 20 00 00 00");
            //jmp exit
            CaveMemory.Write_StrBytes("EB 08");

            //AxisY:
            //pop rax
            CaveMemory.Write_StrBytes("58");
            ////mov rax, [RIP+1E]     (Y ==> _Standalone_Axis_CaveAddress + 4)
            CaveMemory.Write_StrBytes("48 8B 05 1E 00 00 00");

            //Exit:
            //mov [rdi],eax
            CaveMemory.Write_StrBytes("89 07");
            //add rsp,20
            CaveMemory.Write_StrBytes("48 83 C4 20");
            //jmp back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _Standalone_Injection_Return_Offset);
            Logger.WriteLog("Adding P1 Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

            //Code Injection
            //1st Step : writing the long jump
            IntPtr ProcessHandle = _TargetProcess.Handle;
            List<Byte> Buffer = new List<Byte>();
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress));
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _LongJump_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);

            //2nd step : writing the short jump
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.Add(0xD9);
            Buffer.Add(0x09);
            Buffer.Add(0x04);
            Buffer.Add(0x00);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _Standalone_Injection_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }





        /// <summary>
        /// The game is working well with mouse but causes issue with a lightgun :
        /// Except in the menu (everything OK) it read Absolute values as relative movements -> stick in the corners
        /// This is a first try to inject data into memory on the fly, as I can't find a unique procedure for Axis writing.
        /// </summary>
        private void SetHackAxis_v2()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            /* Because of 64bit asm, I dont know how to load a 64bit data segment address into a register.
             * But there is an instruction to load address with an offset from RIP (instruction pointer) 
             * That's why data are stored just after the code (to know the offset)
             * */
            _Standalone_Axis_CaveAddress = CaveMemory.CaveAddress + 0x40;
            Logger.WriteLog("_P1_Axis_CaveAddress = 0x" + _Standalone_Axis_CaveAddress.ToString("X16"));

            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[rsp+60]
            CaveMemory.Write_StrBytes("8B 44 24 60");
            //cmp eax,[RIP+45] => (MAx_X)
            CaveMemory.Write_StrBytes("3B 05 45 00 00 00");
            //je AxisX
            CaveMemory.Write_StrBytes("74 0B");
            //cmp eax,[RIP+45] => (MAx_Y)
            CaveMemory.Write_StrBytes("3B 05 45 00 00 00");
            //je AxisY
            CaveMemory.Write_StrBytes("74 0D");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //jmp exit
            CaveMemory.Write_StrBytes("EB 12");

            //AxisX:
            //pop rax
            CaveMemory.Write_StrBytes("58");
            ////mov rax, [RIP+20]     (X ==> _P1_Axis_CaveAddress + 0)
            CaveMemory.Write_StrBytes("48 8B 05 20 00 00 00");
            //jmp exit
            CaveMemory.Write_StrBytes("EB 08");

            //AxisY:
            //pop rax
            CaveMemory.Write_StrBytes("58");
            ////mov rax, [RIP+1E]     (Y ==> _P1_Axis_CaveAddress + 4)
            CaveMemory.Write_StrBytes("48 8B 05 1E 00 00 00");

            //Exit:
            //mov [rdi],eax
            CaveMemory.Write_StrBytes("89 07");
            //add rsp,20
            CaveMemory.Write_StrBytes("48 83 C4 20");
            //jmp back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _Standalone_Injection_Return_Offset);
            Logger.WriteLog("Adding P1 Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

            //Code Injection
            //1st Step : writing the long jump
            IntPtr ProcessHandle = _TargetProcess.Handle;
            List<Byte> Buffer = new List<Byte>();
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress));
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _JVS_Axis_LongJump_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);

            //2nd step : writing the short jump
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.Add(0xD9);
            Buffer.Add(0x09);
            Buffer.Add(0x04);
            Buffer.Add(0x00);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _Standalone_Injection_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        /// <summary>
        /// As a standalone, buttons events are handled by Windows Message (WM_xBUTTONDOWN / WM_xBUTTONUP, x = M/L/R)
        /// 1st Method is to block the writing of the byte and do it ourselves (byte address is known by pointer)
        /// 2nd method if needed, is to inject code @+A25AC3 (check RAX value to get good call) to replace read value
        /// </summary>
        private void SetHack_Buttons_v2()
        {
            SetNops(_TargetProcess_MemoryBaseAddress, _Standalone_Nop_Trigger_On);
            SetNops(_TargetProcess_MemoryBaseAddress, _Standalone_Nop_Trigger_Off);
        }






        private void Create_JVS_DataBank()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            _JVS_AxisX_CaveAddress = CaveMemory.CaveAddress;
            _JVS_AxisY_CaveAddress = CaveMemory.CaveAddress + 08;
            _JVS_TriggerButton_CaveAddress = CaveMemory.CaveAddress + 0x10;
            _JVS_WeaponButton_CaveAddress = CaveMemory.CaveAddress + 0x18;
            Logger.WriteLog("Custom data will be stored at : 0x" + CaveMemory.CaveAddress.ToString("X16"));
        }

        /// <summary>
        /// Safest way is to replace the reading part of the "Trigger" byte with our own value
        /// </summary>
        private void SetHack_JVS_Trigger()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);        

            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov rax,[JVS_Trigger_Btn]
            CaveMemory.Write_StrBytes("48 A1");
            byte[] b = BitConverter.GetBytes(_JVS_TriggerButton_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov r8d, rax
            CaveMemory.Write_StrBytes("4C 8B C0");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //Jump back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _JVS_Trigger_Injection_Return_Offset);
            Logger.WriteLog("Adding JVS Trigger COdecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));
            
            //Code Injection
            //1st Step : writing the long jump
            IntPtr ProcessHandle = _TargetProcess.Handle;
            List<Byte> Buffer = new List<Byte>();
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress));
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _JVS_Trigger_LongJump_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);

            //2nd step : writing the short jump
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.Add(0xE7);
            Buffer.Add(0x6A);
            Buffer.Add(0x65);
            Buffer.Add(0xFF);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _JVS_Trigger_Injection_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);        
        }

        /// <summary>
        /// Weapon is read along with other buttons on a loaded WORD
        /// So we will just patch the corresponding bit with our own value
        /// </summary>
        private void SetHack_JVS_Weapon()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //mov rax,[TimeCrisisGame-Win64-Shipping.exe+1860938]
            CaveMemory.Write_StrBytes("48 A1");
            byte[] b = BitConverter.GetBytes((UInt64)((UInt64)_TargetProcess_MemoryBaseAddress + 0x1860938));
            CaveMemory.Write_Bytes(b);    
            //eax,FF7FFFFF
            CaveMemory.Write_StrBytes("25 FF FF 7F FF");
            //mov ebx, eax
            CaveMemory.Write_StrBytes("8B D8");
            //mov eax, [_JVS_Weapon_Btn]
            CaveMemory.Write_StrBytes("48 A1");
            b = BitConverter.GetBytes(_JVS_WeaponButton_CaveAddress);
            CaveMemory.Write_Bytes(b);  
            //or ebx, eax
            CaveMemory.Write_StrBytes("09 C3");
            //pop rax
            CaveMemory.Write_StrBytes("58");
            //Jump back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _JVS_Weapon_Injection_Return_Offset);
            Logger.WriteLog("Adding JVS Weapon Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

            //Code Injection
            //1st Step : writing the long jump
            IntPtr ProcessHandle = _TargetProcess.Handle;
            List<Byte> Buffer = new List<Byte>();
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress));
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _JVS_Weapon_LongJump_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);

            //2nd step : writing the short jump
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.Add(0xCE);
            Buffer.Add(0x49);
            Buffer.Add(0x65);
            Buffer.Add(0xFF);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _JVS_Weapon_Injection_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);
        }

        /// <summary>
        /// On my computer axis are not working with JConfig, as the base values (Int) are sent to the game but for unknown reason the float computed value is "Not A Number"
        /// On this procedure, multiple calls are made to compute the value but only 2 have NaN (0xFFC00000) values, we will overrid result with our own float value.
        /// The lower bytes of RAX seem to be constant values for X (0x2251) and Y (0x22C1)
        /// If the "auto" check for X/Y is not working well, this will need more code to check between the 2 changing values
        /// </summary>
        private void SetHack_JVS_Axis()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push rax
            CaveMemory.Write_StrBytes("50");
            //divss xmm0,xmm1
            CaveMemory.Write_StrBytes("F3 0F 5E C1");
            //cmp [rsp+50], ffc00000
            CaveMemory.Write_StrBytes("81 7C 24 58 00 00 C0 FF");
            //jne OriginalCode
            CaveMemory.Write_StrBytes("0F 85 43 00 00 00");
            //Hack Part:
            //and eax, 0xFFFF
            CaveMemory.Write_StrBytes("25 FF FF 00 00");
            //cmp eax, 0x2251
            CaveMemory.Write_StrBytes("3D 51 22 00 00");
            //je Axis_X
            CaveMemory.Write_StrBytes("0F 84 10 00 00 00");
            //cmp eax, 0x22C1
            CaveMemory.Write_StrBytes("3D C1 22 00 00");
            //je Axis_Y
            CaveMemory.Write_StrBytes("0F 84 16 00 00 00");
            //jmp OriginalCode (default)
            CaveMemory.Write_StrBytes("E9 23 00 00 00");
            //Axis_X:
            //mov rax, [AxisX_CaveAddress]
            CaveMemory.Write_StrBytes("48 A1");
            byte[] b = BitConverter.GetBytes(_JVS_AxisX_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov [rdi], eax
            CaveMemory.Write_StrBytes("89 07");
            //jmp exit
            CaveMemory.Write_StrBytes("E9 16 00 00 00");
            //Axis_Y:
            //mov rax, [AxisY_CaveAddress]
            CaveMemory.Write_StrBytes("48 A1");
            b = BitConverter.GetBytes(_JVS_AxisY_CaveAddress);
            CaveMemory.Write_Bytes(b);
            //mov [rdi], eax
            CaveMemory.Write_StrBytes("89 07");
            //jmp exit
            CaveMemory.Write_StrBytes("0F 84 04 00 00 00");
            //OriginalCode:
            //movss [rdi], xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 07");
            //pop rax
            CaveMemory.Write_StrBytes("58");

            //Jump back
            CaveMemory.Write_jmp((UInt64)_TargetProcess_MemoryBaseAddress + _JVS_Axis_Injection_Return_Offset);
            Logger.WriteLog("Adding JVS Axis Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X16"));

            //Code Injection
            //1st Step : writing the long jump
            IntPtr ProcessHandle = _TargetProcess.Handle;
            List<Byte> Buffer = new List<Byte>();
            UIntPtr bytesWritten = UIntPtr.Zero;
            Buffer.Add(0xFF);
            Buffer.Add(0x25);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.Add(0x00);
            Buffer.AddRange(BitConverter.GetBytes(CaveMemory.CaveAddress));
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _JVS_Axis_LongJump_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);

            //2nd step : writing the short jump
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.Add(0x02);
            Buffer.Add(0x07);
            Buffer.Add(0x04);
            Buffer.Add(0x00);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Buffer.Add(0x90);
            Win32API.WriteProcessMemoryX64(ProcessHandle, (IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + _JVS_Axis_Injection_Offset), Buffer.ToArray(), (UIntPtr)Buffer.Count, out bytesWritten);        
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>   
        public override void SendInput(PlayerSettings PlayerData)
        {
            if (_IsStandalone)
            {
                byte[] bufferX = BitConverter.GetBytes(PlayerData.RIController.Computed_X);
                byte[] bufferY = BitConverter.GetBytes(PlayerData.RIController.Computed_Y);

                if (PlayerData.ID == 1)
                {
                    WriteBytes((IntPtr)(_Standalone_Axis_CaveAddress + _STANDALONE_X_OFFSET), bufferX);
                    WriteBytes((IntPtr)(_Standalone_Axis_CaveAddress + _STANDALONE_Y_OFFSET), bufferY);
                }
            }
            else
            {
                byte[] bufferX = BitConverter.GetBytes(_FloatXvalue);
                byte[] bufferY = BitConverter.GetBytes(_FloatYvalue);

                if (PlayerData.ID == 1)
                {
                    WriteBytes((IntPtr)_JVS_AxisX_CaveAddress, bufferX);
                    WriteBytes((IntPtr)_JVS_AxisY_CaveAddress, bufferY);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        WriteByte((IntPtr)(_JVS_TriggerButton_CaveAddress), 0x20);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        WriteByte((IntPtr)(_JVS_TriggerButton_CaveAddress), 0x00);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        WriteByte((IntPtr)(_JVS_WeaponButton_CaveAddress + 2), 0x80);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        WriteByte((IntPtr)(_JVS_WeaponButton_CaveAddress + 2), 0x00);

                    //Game is handling inputs via WM_ message.
                    //Lightguns (Aimtrak, Sinden...) are seen as mice and are sending WM_LBUTTON message when triger is pressed
                    //Gamepads are not, so n that case we can send the message as if they were mice buttons
                    /*if (PlayerData.RIController.DeviceType != RawInputDeviceType.RIM_TYPEMOUSE)
                    {
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                            //Win32API.PostMessage(_TargetProcess.MainWindowHandle, 0x201, IntPtr.Zero, IntPtr.Zero); //WM_LBUTTONDOWN
                            WriteByte((IntPtr)(_JVS_Trigger_CaveAddress + 0x10), 0x20);
                        if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                            //Win32API.PostMessage(_TargetProcess.MainWindowHandle, 0x202, IntPtr.Zero, IntPtr.Zero); //WM_LBUTTON_UP
                            WriteByte((IntPtr)(_JVS_Trigger_CaveAddress + 0x10), 0x00);
                    } */
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
            //Gun motor : Is activated for every bullet fired AND when player gets
            _Outputs = new List<GameOutput>();

            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpSpeaker, OutputId.LmpSpeaker));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpBillboard, OutputId.LmpBillboard));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, MameOutputHelper.CustomDamageDelay, 100, 0));            
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            byte bLEDs = ReadByte((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x18609B9));
            SetOutputValue(OutputId.LmpBillboard, bLEDs >> 6 & 0x01);
            SetOutputValue(OutputId.LmpSpeaker, bLEDs >> 6 & 0x01);

            //There are many ways to compute Original Recoil :
            //#1 - @Offset +1860630, the byte is set to 1 after a bullet is fired and then shortly after reset back to 0 (almost non-readable spead, very fast refresh rate needed)
            //#2 - @Offset +18609B8, the byte is changing between 0x40 and 0x80 for each bullet fired ( start is 0x00 )
            //#3 - @InstructionOffset +A56F7b, the option#1 byte is set to 1 (reset to zero @+A56F83). An interception codecave might be possible. 
            
            //#1 (need high speed refresh)
            //byte bRecoil = ReadByte((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x1860630));
            //SetOutputValue(OutputId.P1_GunRecoil, bRecoil);
            
            //#2
            //Only Jconfig with emulated JVS will get these events
            if (!_IsStandalone)
            {
                byte bRecoil = ReadByte((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x1860630));
                if (bRecoil != 0)
                {
                    if (_P1_LastRecoil == 0x40 && bRecoil == 0x80)
                    {
                        SetOutputValue(OutputId.P1_GunRecoil, 1);
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);
                    }
                    else if (_P1_LastRecoil == 0x80 && bRecoil == 0x40)
                    {
                        SetOutputValue(OutputId.P1_GunRecoil, 1);
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);
                    }
                }
                _P1_LastRecoil = bRecoil;
            }

            _P1_Life = 0;
            _P1_Ammo = 0;
            _P1_Weapon = 0;
            int P1_Clip = 0;

            UInt64 PtrAmmoLife = ReadPtrChain((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x0185BE18), new UInt64[] { 0x1C, 0x08, 0x70, 0x1FC });
            //To compute custom outputs, we will read Life and Ammo values
            //Pointers are constantly changing, removed, created, etc....so the value might not exist
            //On top of that, Ammo number can also be decreased when changing Weapon, so this is a supplementary check to do 
            if (PtrAmmoLife != 0)
            {
                //Life is value x 2 in memory....
                //Also, can be found statically at 
                _P1_Life = ReadByte((IntPtr)(PtrAmmoLife + 0x10)) >> 1; //Life also available in fixed pointer @BaseMemory + 0x01860B78
                _P1_Weapon = ReadByte((IntPtr)(PtrAmmoLife + 0x118));
                _P1_Ammo = ReadByte((IntPtr)(PtrAmmoLife + 0x170));
                
                //Computing custom Recoil with the following way (= using Ammo count) will not work during STAGE 2 with unimited ammo weapon              
                if (_P1_Weapon == _P1_LastWeapon)
                {  
                    //Custom Recoil
                    //As we don't have a "STATUS" variable telling us if the player is playing or not,
                    //we will smoothly filter for decrease to limit false recoil events if the game sets back Ammo to default value after game-over
                    if (_IsStandalone && _P1_Ammo == _P1_LastAmmo - 1)
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (_P1_Ammo > 0)
                        P1_Clip = 1;
                }
                
                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);
                
            }

            _P1_LastAmmo = _P1_Ammo;
            _P1_LastLife = _P1_Life;
            _P1_LastWeapon = _P1_Weapon;
            

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
        }

        #endregion
    }
}
