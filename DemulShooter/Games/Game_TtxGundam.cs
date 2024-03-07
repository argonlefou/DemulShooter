using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter
{
    class Game_TtxGundam : Game
    {
        /*** MEMORY ADDRESSES **/
        private UInt32 _P1_X_Offset = 0x0026F990;
        private UInt32 _P1_Y_Offset = 0x0026F992;
        private UInt32 _P1_TriggerDown_Offset = 0x0026F995;
        private UInt32 _P1_TriggerStatus_Offset = 0x0026F996;
        private UInt32 _P1_TriggerUp_Offset = 0x0026F997;
        private UInt32 _P1_WeaponChangeDown_Offset = 0x0026F998;
        private UInt32 _P1_WeaponChangeStatus_Offset = 0x0026F999;
        private UInt32 _P1_WeaponChangeUp_Offset = 0x0026F99A;
        private UInt32 _P2_X_Offset = 0x0026F9A0;
        private UInt32 _P2_Y_Offset = 0x0026F9A2;
        private UInt32 _P2_TriggerDown_Offset = 0x0026F9A5;
        private UInt32 _P2_TriggerStatus_Offset = 0x0026F9A6;
        private UInt32 _P2_TriggerUp_Offset = 0x0026F9A7;
        private UInt32 _P2_WeaponChangeDown_Offset = 0x0026F9A8;
        private UInt32 _P2_WeaponChangeStatus_Offset = 0x0026F9A9;
        private UInt32 _P2_WeaponChangeUp_Offset = 0x0026F9AA;
        private UInt32 _Border_Check1_Injection_Offset = 0x000A7A50;
        private UInt32 _Border_Check1_Injection_Return_Offset = 0x000A7A7E;
        private UInt32 _Border_Check2_Injection_Offset = 0x000A7AEE;
        private UInt32 _Border_Check2_Injection_Return_Offset = 0x000A7B0C;

        private NopStruct _Nop_P1_X_1 = new NopStruct(0x000D038C, 7);
        private NopStruct _Nop_P1_Y = new NopStruct(0x000D0374, 7);        
        private NopStruct _Nop_Btn_Down_1 = new NopStruct(0x000D0302, 12);
        private NopStruct _Nop_Btn_Down_2 = new NopStruct(0x000D030F, 12);
        private NopStruct _Nop_Btn_Up_1 = new NopStruct(0x000D032F, 13);
        private NopStruct _Nop_Btn_Up_2 = new NopStruct(0x000D033D, 13);
        private NopStruct _Nop_Btn_Reset = new NopStruct(0x000D1262, 3);
        // This one is tricky ! Only used when on 2P game.exe (when mouse X > 640) and is screwing 1P X axis :
        private NopStruct _Nop_P1_X_2 = new NopStruct(0x000D0383, 6);        
        
        private bool _Pedal1_Enable;
        private HardwareScanCode _Pedal1_Key;
        private bool _isPedal1_Pushed = false;
        private bool _Pedal2_Enable;
        private HardwareScanCode _Pedal2_Key;
        private bool _isPedal2_Pushed = false;

        private Codecave _Cave_Check1, _Cave_Check2;

        /*** Outputs ***/
        private UInt32 _P1_GunMotorOffset = 0x0026FB1C;
        private UInt32 _P2_GunMotorOffset = 0x0026FB20;
        //dword_66FB24 also set to 0 and 1 at the same time than motors sometimes ??
        private UInt32 _P1_Playing_Offset = 0x0026FB9C;
        private UInt32 _P2_Playing_Offset = 0x0026FB9D;
        private UInt32 _Ammo_Injection_Offset = 0x000A7BA3;
        private UInt32 _Ammo_Injection_ReturnOffset = 0x000A7BA9;
        private UInt32 _Life_Injection_Offset = 0x00099964;
        private UInt32 _Life_Injection_ReturnOffset = 0x0009996A;

        //Sets value to 1 when active, 0 un-initialized, 2 otherwise
        private UInt32 _SetRumbleGunShot_Offset = 0x000A60FF;
        private UInt32 _SetRumbleCanonShot_Offset = 0x000A7649;
        private UInt32 _SetRumbleOnHit_Offset = 0x009B06B;
        private UInt32 _SetRumbleUnkownReason01_Offset = 0x000A70F3;
        private UInt32 _SetRumbleUnkownReason02_Offset = 0x000E19CA;
        private UInt32 _SetRumbleUnkownReason03_Offset = 0x000E1A31;
        private UInt32 _SetRumbleStateFuntionOffset = 0x000D0B40;

        private UInt32 _P1_RecoilEnabled_CustomAddress;
        private UInt32 _P2_RecoilEnabled_CustomAddress;
        private UInt32 _P1_DamagedEnabled_CustomAddress;
        private UInt32 _P2_DamagedEnabled_CustomAddress;
        private UInt32 _P1_Ammo_CustomAddress;
        private UInt32 _P2_Ammo_CustomAddress;
        private UInt32 _P1_Life_CustomAddress;
        private UInt32 _P2_Life_CustomAddress;



        /// <summary>
        /// Constructor
        /// </summary>
        public Game_TtxGundam(String RomName, bool Pedal1_Enable, HardwareScanCode Pedal1_Key, bool Pedal2_Enable, HardwareScanCode Pedal2_Key, bool DisableInputHack, bool Verbose)
            : base(RomName, "game", DisableInputHack, Verbose)
        {
            _Pedal1_Enable = Pedal1_Enable;
            _Pedal1_Key = Pedal1_Key;
            _Pedal2_Enable = Pedal2_Enable;
            _Pedal2_Key = Pedal2_Key;
            _KnownMd5Prints.Add("Gundam : SoZ v1.01 - Dual Player, Unpatched I/O", "70af03a21a42d9042065fc65b7eb56f9");
            _KnownMd5Prints.Add("Gundam : SoZ v1.01 - Single Player, Pathched I/O", "d8cd539967cc3c23f620139ab4669d30");
            _KnownMd5Prints.Add("Gundam : SoZ v1.01 - Dual Player, Patched I/O", "c31187a57fd5ab6864b78eaa755ae3f0");

            _tProcess.Start();
            Logger.WriteLog("Waiting for Taito Type X " + _RomName + " game to hook.....");
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
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            if (!_DisableInputHack)
                                SetHack();
                            else
                                Logger.WriteLog("Input Hack disabled");
                            SetHack_Outputs();
                            CheckExeMd5();
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

        public override bool ClientScale(PlayerSettings PayerData)
        {
            /*************************************************************************************************
             * Screen has a weird behavior with this game and the background dosbox activated to launch it.
             * Process mainhandle is giving DOS windows size...
             * Foreground Windows sometimes fail on top of where the DOS box is...
             * With Game All RH launcher, fullscreen is an obligation so the game res = screen res,
             * so in-game cursor position = screen cursor position .
             * This is preventing bugs on axis positionning
             ************************************************************************************************/
            return true;
        }

        /// <summary>
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// </summary>
        public override bool GameScale(PlayerSettings PlayerData)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    double dMinX = 0.0;
                    double dMaxX = 640.0;
                    double dMinY = 0.0;
                    double dMaxY = 480.0;
                    double dRangeX = dMaxX - dMinX + 1;
                    double dRangeY = dMaxY - dMinY + 1;                    

                    if (_RomName.Equals("gsoz2p"))
                    {
                        dMaxX = 1280.0;
                        dMaxY = 480.0;

                        //Side by Side dual screen, each one 640x480 max coordonates
                        //So total X is 1280
                        PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dMaxX * PlayerData.RIController.Computed_X / _screenWidth));

                        //For Y we need to change reference from Screen[X, Y] to real Game [X, Y] with black border;
                        double Ratio = _screenWidth / dMaxX;
                        Logger.WriteLog("Ratio = " + Ratio.ToString());
                        double GameHeight = Ratio * dMaxY;
                        Logger.WriteLog("Real game height (px) = " + GameHeight);
                        double Border = (_screenHeight - GameHeight) / 2.0;
                        Logger.WriteLog("Black boder top and bottom (px) = " + Border.ToString());
                        double y = PlayerData.RIController.Computed_Y - Border;
                        double percent = y * 100.0 / GameHeight;
                        PlayerData.RIController.Computed_Y = (int)(percent * dMaxY / 100.0);

                        //Player One will have X value cut-off to [0-639] next
                        //For player 2 we first shift value to the left
                        if (PlayerData.ID == 2)
                            PlayerData.RIController.Computed_X -= 640;
                    }
                    else if (_RomName.Equals("gsoz"))
                    {                        
                        //Single screen, each one 640x480 max coordonates
                        //So total Y is 480
                        PlayerData.RIController.Computed_Y = Convert.ToInt16(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / _screenHeight));

                        //For X we need to change reference from Screen[X, Y] to real Game [X, Y] with black border
                        dMaxX = 640.0;

                        double Ratio = (double)_screenHeight / dMaxY;
                        Logger.WriteLog("Ratio = " + Ratio.ToString());
                        double GameWidth = Ratio * dMaxX;
                        Logger.WriteLog("Real game width (px) = " + GameWidth);
                        double Border = (_screenWidth - GameWidth) / 2.0;
                        Logger.WriteLog("Black boder left and right (px) = " + Border.ToString());
                        double x = PlayerData.RIController.Computed_X - Border;
                        double percent = x * 100.0 / GameWidth;
                        PlayerData.RIController.Computed_X = (int)(percent * dMaxX / 100.0);
                        
                        //In case of forced scren ration
                        /*if (_ForcedXratio > 0)
                        {
                            Logger.WriteLog("Forcing X Ratio to = " + _ForcedXratio.ToString());
                            double ViewportHeight = GameResY;
                            double ViewportWidth = GameResY * _ForcedXratio;
                            double SideBarsWidth = (GameResX - ViewportWidth) / 2;
                            Logger.WriteLog("Game Viewport size (Px) = [ " + ViewportWidth + "x" + ViewportHeight + " ]");
                            Logger.WriteLog("SideBars Width (px) = " + SideBarsWidth.ToString());
                            dRangeX = dRangeX + SideBarsWidth * 2;
                            PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dRangeX * PlayerData.RIController.Computed_X / GameResX) - SideBarsWidth);
                        }
                        else
                          PlayerData.RIController.Computed_X = Convert.ToInt16(Math.Round(dRangeX * PlayerData.RIController.Computed_X / GameResX));                       
                      */}

                    if (PlayerData.RIController.Computed_X < 1)
                        PlayerData.RIController.Computed_X = 1;
                    if (PlayerData.RIController.Computed_X > 639)
                        PlayerData.RIController.Computed_X = 639;
                    if (PlayerData.RIController.Computed_Y < 1)
                        PlayerData.RIController.Computed_Y = 1;
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
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_X_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_X_2);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_P1_Y);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Btn_Down_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Btn_Down_2);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Btn_Up_1);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Btn_Up_2);
            //Reset Buttons should be left but this does not work, so we have to handle the reset too and NOP this one
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Btn_Reset);

            /***
             * If neither Pedal1 nor Pedal2 are enabled, no need for a codecave (default game).
             * Else, We need to make 2 codecave for the 2 checking procedure (X-Y min-max)
             * Each one will have to be split for P1 and P2 separatelly
             ***/
            if (_Pedal1_Enable || _Pedal2_Enable)
            {
                _Cave_Check1 = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
                _Cave_Check1.Open();
                _Cave_Check1.Alloc(0x800);
                //cmp [esi+1C], 0
                _Cave_Check1.Write_StrBytes("83 7E 1C 00");
                //je @player1
                _Cave_Check1.Write_StrBytes("0F 84 0A 00 00 00");
                //cmp [esi+1C], 1
                _Cave_Check1.Write_StrBytes("83 7E 1C 01");
                //je @player2
                _Cave_Check1.Write_StrBytes("0F 84 33 00 00 00");

                //player1:
                if (_Pedal1_Enable)
                    //cmp di, 00
                    _Cave_Check1.Write_StrBytes("66 83 FF 00");
                else
                    //cmp di, 10
                    _Cave_Check1.Write_StrBytes("66 83 FF 10");
                //jng game.exe+A7B85
                _Cave_Check1.Write_jng((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                if (_Pedal1_Enable)
                    //cmp di, 00
                    _Cave_Check1.Write_StrBytes("66 81 FF 00 00");
                else
                    //cmp di, 0270
                    _Cave_Check1.Write_StrBytes("66 81 FF 70 02");
                //jnl game.exe+A7B85
                _Cave_Check1.Write_jnl((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                //mov eax, edi
                _Cave_Check1.Write_StrBytes("8B C7");
                //shr eax, 10
                _Cave_Check1.Write_StrBytes("C1 E8 10");
                if (_Pedal1_Enable)
                    //cmp ax, 00
                    _Cave_Check1.Write_StrBytes("66 3D 00 00");
                else
                    //cmp ax, 10
                    _Cave_Check1.Write_StrBytes("66 3D 10 00");
                //jng game.exe+A7B85
                _Cave_Check1.Write_jng((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                if (_Pedal1_Enable)
                    //cmp ax, 00
                    _Cave_Check1.Write_StrBytes("66 3D E0 01");
                else
                    //cmp ax, 01D0
                    _Cave_Check1.Write_StrBytes("66 3D D0 01");
                //jnl game.exe+A7B85
                _Cave_Check1.Write_jnl((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                //jmp EXIT
                _Cave_Check1.Write_StrBytes("E9 2E 00 00 00");

                //player2:
                if (_Pedal2_Enable)
                    //cmp di, 00
                    _Cave_Check1.Write_StrBytes("66 83 FF 00");
                else
                    //cmp di, 10
                    _Cave_Check1.Write_StrBytes("66 83 FF 10");
                //jng game.exe+A7B85
                _Cave_Check1.Write_jng((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                if (_Pedal2_Enable)
                    //cmp di, 00
                    _Cave_Check1.Write_StrBytes("66 81 FF 00 00");
                else
                    //cmp di, 0270
                    _Cave_Check1.Write_StrBytes("66 81 FF 70 02");
                //jnl game.exe+A7B85
                _Cave_Check1.Write_jnl((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                //mov eax, edi
                _Cave_Check1.Write_StrBytes("8B C7");
                //shr eax, 10
                _Cave_Check1.Write_StrBytes("C1 E8 10");
                if (_Pedal2_Enable)
                    //cmp ax, 00
                    _Cave_Check1.Write_StrBytes("66 3D 00 00");
                else
                    //cmp ax, 10
                    _Cave_Check1.Write_StrBytes("66 3D 10 00");
                //jng game.exe+A7B85
                _Cave_Check1.Write_jng((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                if (_Pedal2_Enable)
                    //cmp ax, 00
                    _Cave_Check1.Write_StrBytes("66 3D E0 01");
                else
                    //cmp ax, 01D0
                    _Cave_Check1.Write_StrBytes("66 3D D0 01");
                //jnl game.exe+A7B85
                _Cave_Check1.Write_jnl((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                //jmp EXIT
                _Cave_Check1.Write_jmp((UInt32)_TargetProcess_MemoryBaseAddress + _Border_Check1_Injection_Return_Offset);

                Logger.WriteLog("Adding check1 CodeCave at : 0x" + _Cave_Check1.CaveAddress.ToString("X8"));
                IntPtr ProcessHandle = _TargetProcess.Handle;
                UInt32 bytesWritten = 0;
                UInt32 jumpTo = 0;
                jumpTo = _Cave_Check1.CaveAddress - ((UInt32)_TargetProcess_MemoryBaseAddress + _Border_Check1_Injection_Offset) - 5;
                List<byte> Buffer = new List<byte>();
                Buffer.Add(0xE9);
                Buffer.AddRange(BitConverter.GetBytes(jumpTo));
                Buffer.Add(0x90);
                Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess_MemoryBaseAddress + _Border_Check1_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

                _Cave_Check2 = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
                _Cave_Check2.Open();
                _Cave_Check2.Alloc(0x800);
                //cmp [esi+1C], 0
                _Cave_Check2.Write_StrBytes("83 7E 1C 00");
                //je @player1
                _Cave_Check2.Write_StrBytes("0F 84 0A 00 00 00");
                //cmp [esi+1C], 1
                _Cave_Check2.Write_StrBytes("83 7E 1C 01");
                //je @player2
                _Cave_Check2.Write_StrBytes("0F 84 33 00 00 00");
                //player1:
                if (_Pedal1_Enable)
                    //cmp di, 00
                    _Cave_Check2.Write_StrBytes("66 83 FF 00");
                else
                    //cmp di, 10
                    _Cave_Check2.Write_StrBytes("66 83 FF 10");
                //jl game.exe+A7B85
                _Cave_Check2.Write_jl((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B0C);
                if (_Pedal1_Enable)
                    //cmp di, 00
                    _Cave_Check2.Write_StrBytes("66 81 FF 00 00");
                else
                    //cmp di, 0270
                    _Cave_Check2.Write_StrBytes("66 81 FF 70 02");
                //jg game.exe+A7B85
                _Cave_Check2.Write_jg((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B0C);
                //mov eax, [esp+26]
                _Cave_Check2.Write_StrBytes("66 8B 44 24 26");
                if (_Pedal1_Enable)
                    //cmp ax, 00
                    _Cave_Check2.Write_StrBytes("66 3D 00 00");
                else
                    //cmp ax, 10
                    _Cave_Check2.Write_StrBytes("66 3D 10 00");
                //jl game.exe+A7B85
                _Cave_Check2.Write_jl((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B0C);
                if (_Pedal1_Enable)
                    //cmp ax, 00
                    _Cave_Check2.Write_StrBytes("66 3D E0 01");
                else
                    //cmp ax, 01D0
                    _Cave_Check2.Write_StrBytes("66 3D D0 01");
                //jle game.exe+A7B85
                _Cave_Check2.Write_jng((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                //jmp EXIT
                _Cave_Check2.Write_StrBytes("E9 2E 00 00 00");

                //player2:
                if (_Pedal2_Enable)
                    //cmp di, 00
                    _Cave_Check2.Write_StrBytes("66 83 FF 00");
                else
                    //cmp di, 10
                    _Cave_Check2.Write_StrBytes("66 83 FF 10");
                //jl game.exe+A7B85
                _Cave_Check2.Write_jl((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B0C);
                if (_Pedal2_Enable)
                    //cmp di, 00
                    _Cave_Check2.Write_StrBytes("66 81 FF 00 00");
                else
                    //cmp di, 0270
                    _Cave_Check2.Write_StrBytes("66 81 FF 70 02");
                //jg game.exe+A7B85
                _Cave_Check2.Write_jg((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B0C);
                //mov eax, [esp+26]
                _Cave_Check2.Write_StrBytes("66 8B 44 24 26");
                if (_Pedal2_Enable)
                    //cmp ax, 00
                    _Cave_Check2.Write_StrBytes("66 3D 00 00");
                else
                    //cmp ax, 10
                    _Cave_Check2.Write_StrBytes("66 3D 10 00");
                //jl game.exe+A7B85
                _Cave_Check2.Write_jl((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B0C);
                if (_Pedal2_Enable)
                    //cmp ax, 00
                    _Cave_Check2.Write_StrBytes("66 3D E0 01");
                else
                    //cmp ax_Cave_Check2 01D0
                    _Cave_Check2.Write_StrBytes("66 3D D0 01");
                //jle game.exe+A7B85
                _Cave_Check2.Write_jng((UInt32)_TargetProcess_MemoryBaseAddress + 0xA7B85);
                //jmp EXIT
                _Cave_Check2.Write_jmp((UInt32)_TargetProcess_MemoryBaseAddress + _Border_Check2_Injection_Return_Offset);

                Logger.WriteLog("Adding check2 CodeCave at : 0x" + _Cave_Check2.CaveAddress.ToString("X8"));
                bytesWritten = 0;
                jumpTo = _Cave_Check2.CaveAddress - ((UInt32)_TargetProcess_MemoryBaseAddress + _Border_Check2_Injection_Offset) - 5;
                Buffer = new List<byte>();
                Buffer.Add(0xE9);
                Buffer.AddRange(BitConverter.GetBytes(jumpTo));
                Buffer.Add(0x90);
                Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess_MemoryBaseAddress + _Border_Check2_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
            }

            //Initializing values
            byte[] initX = { 0x10, 0 };
            byte[] initY = { 0x10, 0 };
            //Write Axis
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, initX);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, initY);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, initX);
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, initY);
            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
        }

        private void SetHack_Outputs()
        {
            CreateDataBank();
            GetRecoilEvent(_SetRumbleGunShot_Offset);
            GetRecoilEvent(_SetRumbleCanonShot_Offset);
            GetDamagedEvent(_SetRumbleOnHit_Offset);

            //Not knowing when these these part of code are called to vibrate, we will put it in the "Damage" output (less likely to be used than an unwanted recoil)
            GetDamagedEvent(_SetRumbleUnkownReason01_Offset);
            GetDamagedEvent(_SetRumbleUnkownReason02_Offset);
            GetDamagedEvent(_SetRumbleUnkownReason03_Offset);

            Hack_GetAmmo();
            Hack_GetLife();
        }

        /// <summary>
        /// Creating a zone in memory where we will save our recoil and hit events
        /// This memory will then be read by the game thanks to the following hacks.
        /// </summary>
        private void CreateDataBank()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            _P1_RecoilEnabled_CustomAddress = CaveMemory.CaveAddress;
            _P2_RecoilEnabled_CustomAddress = CaveMemory.CaveAddress + 0x04;
            _P1_DamagedEnabled_CustomAddress = CaveMemory.CaveAddress + 0x08;
            _P2_DamagedEnabled_CustomAddress = CaveMemory.CaveAddress + 0x0C;
            _P1_Ammo_CustomAddress = CaveMemory.CaveAddress + 0x10;
            _P2_Ammo_CustomAddress = CaveMemory.CaveAddress + 0x14;
            _P1_Life_CustomAddress = CaveMemory.CaveAddress + 0x18;
            _P2_Life_CustomAddress = CaveMemory.CaveAddress + 0x1C;

            Logger.WriteLog("Custom data will be stored at : 0x" + CaveMemory.CaveAddress.ToString("X8"));
        }

        /// <summary>
        /// The function to set Rumble state Up is called at many places
        /// These one will catch when the Rumble is called because of gun fire, or cannon fire
        /// and put a flag in out data bank
        private void GetRecoilEvent(UInt32 Injection_Offset)
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [esp+4]
            CaveMemory.Write_StrBytes("8B 44 24 04");
            //shl eax, 2
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _P1_RecoilEnabled_customAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_RecoilEnabled_CustomAddress));
            //mov [eax],00000001
            CaveMemory.Write_StrBytes("C7 00 01 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //call game.exe+D0B40
            CaveMemory.Write_call((UInt32)_TargetProcess_MemoryBaseAddress + _SetRumbleStateFuntionOffset);
            //jmp return
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + Injection_Offset + 5);

            Logger.WriteLog("Adding Recoil Detetcion Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));            
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        /// <summary>
        /// Same thing but injected at the moment the game is setting rumble on Damage Received
        private void GetDamagedEvent(UInt32 Injection_Offset)
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, [esp+4]
            CaveMemory.Write_StrBytes("8B 44 24 04");
            //shl eax, 2
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _P1_RecoilEnabled_customAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_DamagedEnabled_CustomAddress));
            //mov [eax],00000001
            CaveMemory.Write_StrBytes("C7 00 01 00 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //call game.exe+D0B40
            CaveMemory.Write_call((UInt32)_TargetProcess_MemoryBaseAddress + _SetRumbleStateFuntionOffset);
            //jmp return
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + Injection_Offset + 5);

            Logger.WriteLog("Adding Damage Detetcion Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }


        //Weapon Struct:
        //+ 0x08 -> Weapon Type (0 = Gaitling, 1 = Canon, 2 = Mines)
        //+ 0x2C -> Gaitling Ammo
        //+ 0x30 -> Canon Ammo
        //+ 0x34 -> Mines Ammo
        //+ 0xC0 -> Player ID
        //We will read values at a time when the Struct Pointer created and is used by the game during playthrough
        private void Hack_GetAmmo()
        {            
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov ecx,[esi+54]
            CaveMemory.Write_StrBytes("8B 4E 54");
            //mov [edx+0C],eax
            CaveMemory.Write_StrBytes("89 42 0C");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //eax,[ecx+000000C0]
            CaveMemory.Write_StrBytes("8B 81 C0 00 00 00");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax, _P1_Ammo_CustomAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Ammo_CustomAddress));
            //mov ebx,[ecx+8]
            CaveMemory.Write_StrBytes("8B 59 08");
            //shl ebx, 2
            CaveMemory.Write_StrBytes("C1 E3 02");
            //add ebx, 2C
            CaveMemory.Write_StrBytes("83 C3 2C");
            //add ebx, ecx
            CaveMemory.Write_StrBytes("01 CB");
            //mov ebx, [ebx]
            CaveMemory.Write_StrBytes("8B 1B");
            //mov [eax],ebx
            CaveMemory.Write_StrBytes("89 18");
            //pop ebx
            CaveMemory.Write_StrBytes("5B");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //jmp return
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _Ammo_Injection_ReturnOffset);

            Logger.WriteLog("Adding Ammo Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _Ammo_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _Ammo_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten); 
        }

        //Struct :
        //+ 0x1C -> Player ID
        //+ 0x4C -> Life
        private void Hack_GetLife()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //mov eax,[esi+4C]
            CaveMemory.Write_StrBytes("8B 46 4C");
            //ecx,[esi+1C]
            CaveMemory.Write_StrBytes("8B 4E 1C");
            //shl ecx,02
            CaveMemory.Write_StrBytes("C1 E1 02");
            //add ecx, _P1_Life_CustomAddress
            CaveMemory.Write_StrBytes("81 C1");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Life_CustomAddress));
            //mov [ecx], eax
            CaveMemory.Write_StrBytes("89 01");
            //mov ecx,[esi+50]
            CaveMemory.Write_StrBytes("8B 4E 50");
            //jmp return
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _Life_Injection_ReturnOffset);

            Logger.WriteLog("Adding Life Codecave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _Life_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _Life_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }


        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>        
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((Int16)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerDown_Offset, 0x01);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerStatus_Offset, 0x01);
                    System.Threading.Thread.Sleep(20);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerDown_Offset, 0x00);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerUp_Offset, 0x01);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerStatus_Offset, 0x00);
                    System.Threading.Thread.Sleep(20);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerUp_Offset, 0x00);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_WeaponChangeDown_Offset, 0x01);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_WeaponChangeStatus_Offset, 0x01);
                    System.Threading.Thread.Sleep(20);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_WeaponChangeDown_Offset, 0x00);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_WeaponChangeUp_Offset, 0x01);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_WeaponChangeStatus_Offset, 0x00);
                    System.Threading.Thread.Sleep(20);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_WeaponChangeUp_Offset, 0x00);
                }
                
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    //With "Pedal mode", enable right-click to shoot only when hiding
                    if(_Pedal1_Enable)
                    {
                        if (!_isPedal1_Pushed)
                        {
                            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerDown_Offset, 0x01);
                            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerStatus_Offset, 0x01);
                            System.Threading.Thread.Sleep(20);
                            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerDown_Offset, 0x00);
                        }
                    }
                    else
                    {
                        WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerDown_Offset, 0x01);
                        WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerStatus_Offset, 0x01);
                        System.Threading.Thread.Sleep(20);
                        WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerDown_Offset, 0x00);
                    }
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerUp_Offset, 0x01);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_TriggerStatus_Offset, 0x00);
                    System.Threading.Thread.Sleep(20);
                }
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_TriggerDown_Offset, 0x01);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_TriggerStatus_Offset, 0x01);
                    System.Threading.Thread.Sleep(20);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_TriggerDown_Offset, 0x00);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_TriggerUp_Offset, 0x01);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_TriggerStatus_Offset, 0x00);
                    System.Threading.Thread.Sleep(20);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_TriggerUp_Offset, 0x00);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_WeaponChangeDown_Offset, 0x01);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_WeaponChangeStatus_Offset, 0x01);
                    System.Threading.Thread.Sleep(20);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_WeaponChangeDown_Offset, 0x00);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_WeaponChangeUp_Offset, 0x01);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_WeaponChangeStatus_Offset, 0x00);
                    System.Threading.Thread.Sleep(20);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_WeaponChangeUp_Offset, 0x00);
                }

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                {
                    //With "Pedal mode", enable right-click to shoot only when hiding
                    if (_Pedal2_Enable)
                    {
                        if (!_isPedal2_Pushed)
                        {
                            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_TriggerDown_Offset, 0x01);
                            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_TriggerStatus_Offset, 0x01);
                            System.Threading.Thread.Sleep(20);
                            WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_TriggerDown_Offset, 0x00);
                        }
                    }
                    else
                    {
                        WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_TriggerDown_Offset, 0x01);
                        WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_TriggerStatus_Offset, 0x01);
                        System.Threading.Thread.Sleep(20);
                        WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_TriggerDown_Offset, 0x00);
                    }
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_TriggerUp_Offset, 0x01);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_TriggerStatus_Offset, 0x00);
                    System.Threading.Thread.Sleep(20);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_TriggerUp_Offset, 0x00);
                }
            }
        }

        /// <summary>
        /// Low-level Keyboard hook callback.
        /// This is used to detect Pedal action for "Pedal-Mode" hack of DemulShooter
        /// </summary>
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    if (s.scanCode == _Pedal1_Key && _Pedal1_Enable)
                    {
                        _isPedal1_Pushed = true;
                        WriteBytes((UInt32)_Cave_Check1.CaveAddress + 0x21, new byte[] { 0x80, 0x02 }); //640
                        WriteBytes((UInt32)_Cave_Check2.CaveAddress + 0x21, new byte[] { 0x80, 0x02 }); //640
                    }
                    else if (s.scanCode == _Pedal2_Key && _Pedal2_Enable)
                    {
                        _isPedal2_Pushed = true;
                        WriteBytes((UInt32)_Cave_Check1.CaveAddress + 0x54, new byte[] { 0x80, 0x02 }); //640
                        WriteBytes((UInt32)_Cave_Check2.CaveAddress + 0x54, new byte[] { 0x80, 0x02 }); //640
                    }
                }
                else if ((UInt32)wParam == Win32Define.WM_KEYUP)
                {
                    if (s.scanCode == _Pedal1_Key && _Pedal1_Enable)
                    {
                        _isPedal1_Pushed = false;
                        WriteBytes((UInt32)_Cave_Check1.CaveAddress + 0x21, new byte[] { 0x00, 0x00 }); //0
                        WriteBytes((UInt32)_Cave_Check2.CaveAddress + 0x21, new byte[] { 0x00, 0x00 }); //0
                    }
                    else if (s.scanCode == _Pedal2_Key && _Pedal2_Enable)
                    {
                        _isPedal2_Pushed = false;
                        WriteBytes((UInt32)_Cave_Check1.CaveAddress + 0x54, new byte[] { 0x00, 0x00 }); //0
                        WriteBytes((UInt32)_Cave_Check2.CaveAddress + 0x54, new byte[] { 0x00, 0x00 }); //0
                    }
                }
            }
            return Win32API.CallNextHookEx(KeyboardHookID, nCode, wParam, lParam);
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
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));           
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Clip, OutputId.P2_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x0026FA49) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x0026FA4B) >> 7 & 0x01);
            //Motor byte value is 1 if enabled, 0 or 2 if disabled
            SetOutputValue(OutputId.P1_GunMotor, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_GunMotorOffset) & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_GunMotorOffset) & 0x01);

            //Gun motor is activated on shoot and dammage, and stays activated on dammaged untill the player hides
            //So Custom outputs will be based on game values (ammo, life)

            _P1_Life = 0;
            _P1_Ammo = 0;
            int P1_Clip = 0;
            _P2_Life = 0;
            _P2_Ammo = 0;
            int P2_Clip = 0;

            if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Playing_Offset) == 1)
            {
                if (ReadByte(_P1_RecoilEnabled_CustomAddress) == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);
                    WriteByte(_P1_RecoilEnabled_CustomAddress, 0x00);
                }

                if (ReadByte(_P1_DamagedEnabled_CustomAddress) == 1)
                {
                    SetOutputValue(OutputId.P1_Damaged, 1);
                    WriteByte(_P1_DamagedEnabled_CustomAddress, 0x00);
                }

                _P1_Ammo = ReadByte(_P1_Ammo_CustomAddress);
                _P1_Life = ReadByte(_P1_Life_CustomAddress);
            }

            if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Playing_Offset) == 1)
            {
                if (ReadByte(_P2_RecoilEnabled_CustomAddress) == 1)
                {
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);
                    WriteByte(_P2_RecoilEnabled_CustomAddress, 0x00);
                }

                if (ReadByte(_P2_DamagedEnabled_CustomAddress) == 1)
                {
                    SetOutputValue(OutputId.P2_Damaged, 1);
                    WriteByte(_P2_DamagedEnabled_CustomAddress, 0x00);
                }

                _P2_Ammo = ReadByte(_P2_Ammo_CustomAddress);
                _P2_Life = ReadByte(_P2_Life_CustomAddress);
            }


            //@game.exe+a6051 -> decrease ammo (mov, ESI+2C)
            //Pointer address + 2c = ammo
            //pointer address + c0 = player ID
            //Need to separate betwen gameplay and attract mode :(
            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x0026FB88));
        }

        #endregion
    }
}
