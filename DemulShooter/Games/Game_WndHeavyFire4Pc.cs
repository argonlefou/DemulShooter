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
    /*** Heavy Fire 4 => Heavy Fire Shattered Spear ***/
    class Game_WndHeavyFire4Pc : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\windows\hfss";
        private const String HFA_STEAM_FILENAME = "hf4";
        private const String HFA_SKIDROW_FILENAME = "heavyfire4";

        /*** MEMORY ADDRESSES **/
        private UInt32 _Axis_X_CaveAddress = 0;
        private UInt32 _Axis_Y_CaveAddress = 0;
        private UInt32 _Trigger_CaveAddress = 0;
        private UInt32 _Reload_CaveAddress = 0;
        private UInt32 _Grenade_CaveAddress = 0;
        private UInt32 _PlayerOnOff_CaveAddress = 0;
        private UInt32 _Mouse_Buttons_PatchOffset = 0x0013D7D0;
        private UInt32 _NoGun_Patch_Offset = 0x000A953F;
        private NopStruct _Nop_MouseCursorWhenGetFocus = new NopStruct(0x0013E44A, 8);
        private NopStruct _Nop_MouseCursorWhenLooseFocus = new NopStruct(0x0013E576, 8);
        private InjectionStruct _PlayersOnOff_InjectionStruct = new InjectionStruct(0x0002165D, 7);
        private InjectionStruct _Mouse_Buttons_InjectionStruct = new InjectionStruct(0x000218B0, 6);
        private InjectionStruct _Mouse_X_InjectionStruct = new InjectionStruct(0x000218DF, 7);
        private InjectionStruct _Mouse_Y_InjectionStruct = new InjectionStruct(0x000218FC, 7);
        private InjectionStruct _Gamepad_X_InjectionStruct = new InjectionStruct(0x000238B2, 7);
        private InjectionStruct _Gamepad_Y_InjectionStruct = new InjectionStruct(0x000238CB, 7);
        private InjectionStruct _Recoil_InjectionStruct = new InjectionStruct(0x0011AD4F, 6);
        private InjectionStruct _Ammo_InjectionStruct = new InjectionStruct(0x0011A8EE, 5);
        private InjectionStruct _Damage_InjectionStruct = new InjectionStruct(0x000C081A, 8);
        private InjectionStruct _GamePlaying_InjectionStruct = new InjectionStruct(0x0012A090, 7);
        private InjectionStruct _NoCrosshair_InjectionStruct = new InjectionStruct(0x000C3BB7, 7);
        private NopStruct _Nop_SteamForceMouseMainController = new NopStruct(0x000215F5, 6);

        //Outputs
        private UInt32 _GamePlaying_CaveAddress = 0;
        private UInt32 _Recoil_CaveAddress = 0;     //4 bytes : P1~P4
        private UInt32 _Damage_CaveAddress = 0;     //4 bytes : P1~P4
        private UInt32 _Ammo_CaveAddress = 0;       //16 bytes : P1~P4    

        //custom Keys
        private HardwareScanCode _P1_OnOff_Key = HardwareScanCode.DIK_1;
        private HardwareScanCode _P2_OnOff_Key = HardwareScanCode.DIK_2;
        private HardwareScanCode _P3_OnOff_Key = HardwareScanCode.DIK_3;
        private HardwareScanCode _P4_OnOff_Key = HardwareScanCode.DIK_4;

        //Keys to send
        //For Player 1, hardcoded by the game :
        //Cover Left = A
        //Cover Bottom = S
        //Cover Right = D
        //QTE = Space
        private VirtualKeyCode _P1_QTE_W_VK = VirtualKeyCode.VK_W;
        private VirtualKeyCode _P1_CoverLeft_VK = VirtualKeyCode.VK_A;
        private VirtualKeyCode _P1_CoverRight_VK = VirtualKeyCode.VK_D;
        private VirtualKeyCode _P1_CoverBottom_VK = VirtualKeyCode.VK_S;
        private VirtualKeyCode _P1_QTE_Space_VK = VirtualKeyCode.VK_SPACE;
        
        protected float _Axis_X_Min;
        protected float _Axis_X_Max;
        protected bool _Reversecover = false;
        protected float _CoverDelta = 0.3f;
        protected bool _CoverLeftEnabled = false;
        protected bool _CoverBottomEnabled = false;
        protected bool _CoverRightEnabled = false;
        protected bool _QTE_Spacebar_Enabled = false;
        protected bool _QTE_W_Enabled = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndHeavyFire4Pc(String RomName)
            : base(RomName, "hf4")
        {
            _KnownMd5Prints.Add("Heavy Fire 4 - SKIDROW", "9476f9bba48aea6ca04d06158be07f1c");
            _KnownMd5Prints.Add("Heavy Fire 4 - STEAM", "7f8bf20aaba80ac1239efc553d94a53f");

            _Reversecover = Configurator.GetInstance().HF_ReverseCover;
            _CoverDelta = (float)Configurator.GetInstance().HF_CoverSensibility / 10.0f;
            Logger.WriteLog("Setting Cover delta to screen border to " + _CoverDelta.ToString());

            _tProcess.Start();
            Logger.WriteLog("Waiting for Windows Game " + _RomName + " game to hook.....");
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
                            ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                            Apply_MemoryHacks();
                            if (_HideGuns)
                                Apply_NoGunsMemoryHack();
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

                    //Y => [-1 ; 1] float
                    //X => depend on ration Width/Height (ex : [-1.7777; 1.7777] with 1920x1080)

                    float fRatio = (float)TotalResX / (float)TotalResY;
                    _Axis_X_Min = -fRatio;
                    _Axis_X_Max = fRatio;

                    float Y_Value = (2.0f * PlayerData.RIController.Computed_Y / (float)TotalResY) - 1.0f;
                    float X_Value = (fRatio * 2 * PlayerData.RIController.Computed_X / (float)TotalResX) - fRatio;

                    if (X_Value < _Axis_X_Min)
                        X_Value = _Axis_X_Min;
                    if (Y_Value < -1.0f)
                        Y_Value = -1.0f;
                    if (X_Value > _Axis_X_Max)
                        X_Value = _Axis_X_Max;
                    if (Y_Value > 1.0f)
                        Y_Value = 1.0f;

                    Logger.WriteLog("Computed float values = [ " + X_Value + "x" + Y_Value + " ]");

                    //Store data in [0-1000] range to store as Int and divise later and get float value
                    PlayerData.RIController.Computed_X = Convert.ToInt16(X_Value * 1000);
                    PlayerData.RIController.Computed_Y = Convert.ToInt16(Y_Value * 1000);                   
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

        protected override void Apply_InputsMemoryHack()
        {
            Create_InputsDataBank();
            _Axis_X_CaveAddress = _InputsDatabank_Address;
            _Axis_Y_CaveAddress = _InputsDatabank_Address + 0x10;
            _Trigger_CaveAddress = _InputsDatabank_Address + 0x20;
            _Reload_CaveAddress = _InputsDatabank_Address + 0x24;
            _Grenade_CaveAddress = _InputsDatabank_Address + 0x28;
            _PlayerOnOff_CaveAddress = _InputsDatabank_Address + 0x30;

            //Game is changing windows cursor position en enter/quit window focus, causing trouble to debug with a mouse as a lightgun
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_MouseCursorWhenGetFocus);
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_MouseCursorWhenLooseFocus);

            //STEAM version and ISO version does not have the same controller procedure, so the patch will be different
            //to enable/disable players dynamically
            if (_Target_Process_Name.ToLower().Equals(HFA_SKIDROW_FILENAME))
            {
                SetHack_EnablePlayers_Skidrow();
            }
            else if (_Target_Process_Name.ToLower().Equals(HFA_STEAM_FILENAME))
            {
                //Steam game has a setting to force mouse as main controller if a gamepad is plugged (or not)
                //Forcing check to act as if the setting is 1
                SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_SteamForceMouseMainController);
                SetHack_EnablePlayers_Steam();
            }

            SetHack_Mouse_Buttons();
            SetHack_Mouse_X();
            SetHack_Mouse_Y();
            //Not used anymore as all players will be redirected to the Mouse functions
            /*SetHack_Gamepad_X();
            SetHack_Gamepad_Y();*/

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// The game constantly sets Players controllers to either 0(OFF), 1(PAD) or 4(MOUSE)
        /// Forcing it to be our own value allows us to enable disable P2~P4 as we want
        /// </summary>
        private void SetHack_EnablePlayers_Skidrow()
        {
            //First we need to deactivate original set
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersOnOff_InjectionStruct.InjectionOffset + 0x0C, new byte[] { 0xEB, 0x12 });

            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,esi
            CaveMemory.Write_StrBytes("8B C6");
            //add eax,_PlayerOnOff_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_PlayerOnOff_CaveAddress + 1));    //Only looping for P2~P4 (not P1)
            //mov al,[eax]
            CaveMemory.Write_StrBytes("8A 00");
            //mov [edi],al
            CaveMemory.Write_StrBytes("88 07");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_PlayersOnOff_InjectionStruct, "Players ON/OFF (Skidrow)");
        }

        /// <summary>
        /// The game constantly sets Players controllers to either 0(OFF), 1(PAD) or 4(MOUSE)
        /// Forcing it to be our own value allows us to enable disable P2~P4 as we want
        /// </summary>
        private void SetHack_EnablePlayers_Steam()
        {
            //First we need to force some check
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersOnOff_InjectionStruct.InjectionOffset - 0x31, new byte[] { 0xEB, 0x2F });

            //And force the byte to be stored
            //mov [edi], al
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersOnOff_InjectionStruct.InjectionOffset + 0x09, new byte[] { 0x88, 0x07 });

            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            
            //mov eax,esi
            CaveMemory.Write_StrBytes("8B C6");
            //add eax,_PlayerOnOff_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_PlayerOnOff_CaveAddress + 1));    //Only looping for P2~P4 (not P1)
            //mov al,[eax]
            CaveMemory.Write_StrBytes("8A 00");
            //cmp [esi*4+hf4.exe+264660],ebp
            CaveMemory.Write_StrBytes("39 2C B5 60 46 66 00");

            //Inject it
            CaveMemory.InjectToOffset(_PlayersOnOff_InjectionStruct, "Players ON/OFF (Steam)");
        }

        /// <summary>
        /// Overwriting results when the game is checking buttons one after the other
        /// also possible to change keys and/or override keyboard input if needed
        /// </summary>
        private void SetHack_Mouse_Buttons()
        {
            //First set to 0 any mouse input when the game is reading them
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Mouse_Buttons_PatchOffset, new byte[] { 0x31, 0xC0, 0xC3 });

            //Then overriding with our own values
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov [esi+000000B3],al
            CaveMemory.Write_StrBytes("88 86 B3 00 00 00");
            //push ebx
            CaveMemory.Write_StrBytes("53");
            //mov ebx,[esp+14]                          //Player Id
            CaveMemory.Write_StrBytes("8B 5C 24 14");
            //add ebx,_P1_Trigger_CaveAddress
            CaveMemory.Write_StrBytes("81 C3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Trigger_CaveAddress));
            //mov al,[ebx]
            CaveMemory.Write_StrBytes("8A 03");            
            //mov [esi+00000094],al
            CaveMemory.Write_StrBytes("88 86 94 00 00 00");
            //mov [esi+0000009A],al
            CaveMemory.Write_StrBytes("88 86 9A 00 00 00");
            //add ebx,04
            CaveMemory.Write_StrBytes("83 C3 04");      //Reload byte is +4 after Trigger cave address
            //mov al,[ebx]
            CaveMemory.Write_StrBytes("8A 03"); 
            //mov [esi+00000097],al
            CaveMemory.Write_StrBytes("88 86 97 00 00 00");          
            //mov ebx,[esp+14]
            CaveMemory.Write_StrBytes("8B 5C 24 14");

            //Overiding Grenade command only for P2~P4 if P1 needs middle click cover
            //Overwise, Overriding Grenade for all players
            if (!Configurator.GetInstance().HF_UseMiddleButtonAsGrenade)
            {
                //test ebx,ebx
                CaveMemory.Write_StrBytes("85 DB");
                //je exit
                CaveMemory.Write_StrBytes("74 0E");
            }
            else
            {
                //Do nothing
                CaveMemory.Write_StrBytes("90 90 90 90");
            }

            //test ebx,ebx
            CaveMemory.Write_StrBytes("85 DB");
            //je exit
            CaveMemory.Write_StrBytes("74 0E");
            //add ebx,_Grenade_CaveAddress
            CaveMemory.Write_StrBytes("81 C3");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Grenade_CaveAddress));
            //mov al,[ebx]
            CaveMemory.Write_StrBytes("8A 03");
            //mov [esi+00000096],al
            CaveMemory.Write_StrBytes("88 86 96 00 00 00");
            //Exit:
            //pop ebx
            CaveMemory.Write_StrBytes("5B");

            //Inject it
            CaveMemory.InjectToOffset(_Mouse_Buttons_InjectionStruct, "P1 Buttons");
        }

        /// <summary>
        /// All Axis codecave are the same :
        /// The game use some fstp [XXX] instruction, but we can't just NOP it as graphical glitches may appear.
        /// So we just add another set of instructions instruction immediatelly after to change the register 
        /// to our own desired value
        /// </summary>
        private void SetHack_Mouse_X()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //fstp [esi+edi*8+00000114]
            CaveMemory.Write_StrBytes("D9 9C FE 14 01 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,edi
            CaveMemory.Write_StrBytes("8B C7");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax,_Axis_X_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axis_X_CaveAddress));
            //mov eax,[eax]
            CaveMemory.Write_StrBytes("8B 00");
            //mov [esi+edi*8+00000114], eax
            CaveMemory.Write_StrBytes("89 84 FE 14 01 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_Mouse_X_InjectionStruct, "P1 X Axis");
        }

        private void SetHack_Mouse_Y()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //fstp [esi+edi*8+00000118]
            CaveMemory.Write_StrBytes("D9 9C FE 18 01 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,edi
            CaveMemory.Write_StrBytes("8B C7");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax,_Axis_Y_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axis_Y_CaveAddress));
            //mov eax,[eax]
            CaveMemory.Write_StrBytes("8B 00");
            //mov [esi+edi*8+00000118], eax
            CaveMemory.Write_StrBytes("89 84 FE 18 01 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_Mouse_Y_InjectionStruct, "P1 Y Axis");
        }

        private void SetHack_Gamepad_X()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //fstp [edi+esi*8+00000114]
            CaveMemory.Write_StrBytes("D9 9C F7 14 01 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[esi*4+_Axis_X_CaveAddress]
            CaveMemory.Write_StrBytes("8B 04 B5");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axis_X_CaveAddress));
            //mov [edi+esi*8+00000114], eax
            CaveMemory.Write_StrBytes("89 84 F7 14 01 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_Gamepad_X_InjectionStruct, "P2~P4 X Axis");
        }

        private void SetHack_Gamepad_Y()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //fstp [edi+esi*8+00000118]
            CaveMemory.Write_StrBytes("D9 9C F7 18 01 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[esi*4+_Axis_Y_CaveAddress]
            CaveMemory.Write_StrBytes("8B 04 B5");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Axis_Y_CaveAddress));
            //mov [edi+esi*8+00000118], eax
            CaveMemory.Write_StrBytes("89 84 F7 18 01 00 00");
            //pop eax
            CaveMemory.Write_StrBytes("58");

            //Inject it
            CaveMemory.InjectToOffset(_Gamepad_Y_InjectionStruct, "P2~P4 Y Axis");
        }

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _GamePlaying_CaveAddress = _OutputsDatabank_Address;
            _Recoil_CaveAddress = _OutputsDatabank_Address + 0x04;
            _Damage_CaveAddress = _OutputsDatabank_Address + 0x08;
            _Ammo_CaveAddress = _OutputsDatabank_Address + 0x10;

            SetHack_Recoil();
            SetHack_Ammo();
            SetHack_IsPlaying();
            SetHack_Damage();
            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// This code is run constantly and sets some 0/1 flag we can intercept for our own use
        /// </summary>
        private void SetHack_IsPlaying()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov [_P1_Playing_CaveAddress],al
            CaveMemory.Write_StrBytes("A2");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_GamePlaying_CaveAddress));
            //mov ecx,[esp+0000024C]
            CaveMemory.Write_StrBytes("8B 8C 24 4C 02 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_GamePlaying_InjectionStruct, "Player Playing");
        }

        /// <summary>
        /// Set a flag when the game calls for a shooting procedure
        /// </summary>
        private void SetHack_Recoil()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[esi+14]
            CaveMemory.Write_StrBytes("8B 46 14");
            //add eax,_Recoil_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Recoil_CaveAddress));
            //mov byte ptr[eax], 01
            CaveMemory.Write_StrBytes("C6 00 01");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //cmp [ecx+0000019C],ebx
            CaveMemory.Write_StrBytes("39 99 9C 01 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Recoil_InjectionStruct, "Recoil");
        }

        /// <summary>
        /// This function is called in a loop while the player is playing
        /// We can get Ammo status in it
        /// </summary>
        private void SetHack_Ammo()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,ebp
            CaveMemory.Write_StrBytes("8B C5");
            //shl eax,02
            CaveMemory.Write_StrBytes("C1 E0 02");
            //add eax,_Ammo_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Ammo_CaveAddress));
            //mov esi,[ecx+18]
            CaveMemory.Write_StrBytes("8B 71 18");
            //mov [eax],esi
            CaveMemory.Write_StrBytes("89 30");
            //mov esi,ecx
            CaveMemory.Write_StrBytes("8B F1");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //or ebx,
            CaveMemory.Write_StrBytes("83 CB FF");
            //test eax,eax
            CaveMemory.Write_StrBytes("85 C0");

            //Inject it
            CaveMemory.InjectToOffset(_Ammo_InjectionStruct, "Ammo");
        }

        /// <summary>
        /// That code is called when the game needs to increment the hit counter of the player, and also run the "hit_00" soune effect ?
        /// We can get the player ID in ESP+2C after pushing EAX
        /// </summary>
        private void SetHack_Damage()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax,[esp+2C]
            CaveMemory.Write_StrBytes("8B 44 24 2C");
            //dec eax
            CaveMemory.Write_StrBytes("48");
            //add eax,_Damage_CaveAddress
            CaveMemory.Write_StrBytes("05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Damage_CaveAddress));
            //mov byte ptr[eax],01
            CaveMemory.Write_StrBytes("C6 00 01");  
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //movsx edx,word ptr [esp+000000C8]
            CaveMemory.Write_StrBytes("0F BF 94 24 C8 00 00 00");

            //Inject it
            CaveMemory.InjectToOffset(_Damage_InjectionStruct, "Damage");
        }

        /// <summary>
        /// Changing X/Y values to -10.0 for every elements in the HUD (crosshair, reload, etc...)
        /// </summary>
        protected override void Apply_NoCrosshairMemoryHack()
        {
            List<Byte> Buffer = new List<Byte>();
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //fst qword ptr [esp+00000184]
            CaveMemory.Write_StrBytes("DD 94 24 84 01 00 00");
            //mov [esp+4C],C1200000
            CaveMemory.Write_StrBytes("C7 44 24 4C 00 00 20 C1");
            //mov [esp+50],C1200000
            CaveMemory.Write_StrBytes("C7 44 24 50 00 00 20 C1");

            //Inject it
            CaveMemory.InjectToOffset(_NoCrosshair_InjectionStruct, "No Crosshair");
        }

        /// <summary>
        /// Game is displaying gun on screen if less than 2 players, forcing it to hide
        /// </summary>
        private void Apply_NoGunsMemoryHack()
        {
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _NoGun_Patch_Offset, new byte[] { 0xE9, 0xB2, 0x00, 0x00, 0x00, 0x90});
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>  
        public override void SendInput(PlayerSettings PlayerData)
        {
            if (!_DisableInputHack)
            {
                float fX = (float)PlayerData.RIController.Computed_X / 1000.0f;
                float fY = (float)PlayerData.RIController.Computed_Y / 1000.0f;
                int PlayerIndex = (PlayerData.ID - 1) * 4;

                //Setting Values in memory for the Codecave to read it
                byte[] buffer = BitConverter.GetBytes(fX);
                WriteBytes(_Axis_X_CaveAddress + (uint)PlayerIndex, buffer);
                buffer = BitConverter.GetBytes(fY);
                WriteBytes(_Axis_Y_CaveAddress + (uint)PlayerIndex, buffer);

                if (PlayerData.ID == 1)
                {
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        WriteByte(_Trigger_CaveAddress, 0x01);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        WriteByte(_Trigger_CaveAddress, 0x00);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                        WriteByte(_Reload_CaveAddress, 0x01);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                        WriteByte(_Reload_CaveAddress, 0x00);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                    {
                        if (Configurator.GetInstance().HF_UseMiddleButtonAsGrenade)
                        {
                            WriteByte(_Grenade_CaveAddress, 0x01);
                        }
                        else
                        {
                            //If the player is aiming on the left side of the screen before pressing the button
                            //=> Cover Left
                            if ((fX < _Axis_X_Min + _CoverDelta) && !_Reversecover)
                            {
                                Send_VK_KeyDown(_P1_CoverLeft_VK);
                                _CoverLeftEnabled = true;
                            }
                            //If the player is aiming on the right side of the screen before pressing the button
                            //=> Cover Left
                            else if ((fX > _Axis_X_Max - _CoverDelta) && _Reversecover)
                            {
                                Send_VK_KeyDown(_P1_CoverLeft_VK);
                                _CoverLeftEnabled = true;
                            }

                            //If the player is aiming on the right side of the screen before pressing the button
                            //=> Cover Right
                            else if ((fX > _Axis_X_Max - _CoverDelta) && !_Reversecover)
                            {
                                Send_VK_KeyDown(_P1_CoverRight_VK);
                                _CoverRightEnabled = true;
                            }
                            //If the player is aiming on the left side of the screen before pressing the button
                            //=> Cover Right
                            else if ((fX < _Axis_X_Min + _CoverDelta) && _Reversecover)
                            {
                                Send_VK_KeyDown(_P1_CoverRight_VK);
                                _CoverRightEnabled = true;
                            }

                            //If the player is aiming on the bottom side of the screen before pressing the button
                            //=> Cover Down
                            else if (fY > (1.0f - _CoverDelta))
                            {
                                Send_VK_KeyDown(_P1_CoverBottom_VK);
                                _CoverBottomEnabled = true;
                            }
                            //If the player is aiming on the top side of the screen before pressing the button
                            //=> W [QTE]
                            else if (fY < (-1.0f + _CoverDelta))
                            {
                                Send_VK_KeyDown(_P1_QTE_W_VK);
                                _QTE_W_Enabled = true;
                            }
                            //If nothing above
                            //=> Spacebar [QTE]
                            else
                            {
                                Send_VK_KeyDown(_P1_QTE_Space_VK);
                                _QTE_Spacebar_Enabled = true;
                            }
                        }
                    }
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                    {
                        if (Configurator.GetInstance().HF_UseMiddleButtonAsGrenade)
                        {
                            WriteByte(_Grenade_CaveAddress, 0x00);
                        }
                        else
                        {
                            if (_CoverLeftEnabled)
                            {
                                Send_VK_KeyUp(_P1_CoverLeft_VK);
                                _CoverLeftEnabled = false;
                            }
                            if (_CoverBottomEnabled)
                            {
                                Send_VK_KeyUp(_P1_CoverBottom_VK);
                                _CoverBottomEnabled = false;
                            }
                            if (_CoverRightEnabled)
                            {
                                Send_VK_KeyUp(_P1_CoverRight_VK);
                                _CoverRightEnabled = false;
                            }
                            if (_QTE_W_Enabled)
                            {
                                Send_VK_KeyUp(_P1_QTE_W_VK);
                                _QTE_W_Enabled = false;
                            }
                            if (_QTE_Spacebar_Enabled)
                            {
                                Send_VK_KeyUp(_P1_QTE_Space_VK);
                                _QTE_Spacebar_Enabled = false;
                            }
                        }
                    }                    
                }
                else
                {
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                        WriteByte(_Trigger_CaveAddress + (uint)PlayerData.ID - 1, 0x01);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                        WriteByte(_Trigger_CaveAddress + (uint)PlayerData.ID - 1, 0x00);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionDown) != 0)
                        WriteByte(_Grenade_CaveAddress + (uint)PlayerData.ID - 1, 0x01);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.ActionUp) != 0)
                        WriteByte(_Grenade_CaveAddress + (uint)PlayerData.ID - 1, 0x00);

                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerDown) != 0)
                        WriteByte(_Reload_CaveAddress + (uint)PlayerData.ID - 1, 0x01);
                    if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OffScreenTriggerUp) != 0)
                        WriteByte(_Reload_CaveAddress + (uint)PlayerData.ID - 1, 0x00);
                }
            }
        }

        /// <summary>
        /// Low-level Keyboard hook callback.
        /// Using those custom keys, we can force players to be created (4=mouse controller) or not in the game
        /// Setting value to 1 ==> Auto validate popup to select profile, but need some patch to force the game to compute mouse procedure
        /// Setting value to 4 ==> Need validate popup to select profile (genuine way ??)
        /// </summary>
        public override IntPtr KeyboardHookCallback(IntPtr KeyboardHookID, int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    if (s.scanCode == _P1_OnOff_Key)
                    {
                        if (ReadByte(_PlayerOnOff_CaveAddress) != 0)
                            WriteByte(_PlayerOnOff_CaveAddress, 0x00);
                        else
                            WriteByte(_PlayerOnOff_CaveAddress, 0x04);
                    }
                    if (s.scanCode == _P2_OnOff_Key)
                    {
                        if (ReadByte(_PlayerOnOff_CaveAddress + 0x01) != 0)
                            WriteByte(_PlayerOnOff_CaveAddress + 0x01, 0x00);
                        else
                            WriteByte(_PlayerOnOff_CaveAddress + 0x01, 0x04);
                    }
                    if (s.scanCode == _P3_OnOff_Key)
                    {
                        if (ReadByte(_PlayerOnOff_CaveAddress + 0x02) != 0)
                            WriteByte(_PlayerOnOff_CaveAddress + 0x02, 0x00);
                        else
                            WriteByte(_PlayerOnOff_CaveAddress + 0x02, 0x04);
                    }
                    if (s.scanCode == _P4_OnOff_Key)
                    {
                        if (ReadByte(_PlayerOnOff_CaveAddress + 0x03) != 0)
                            WriteByte(_PlayerOnOff_CaveAddress + 0x03, 0x00);
                        else
                            WriteByte(_PlayerOnOff_CaveAddress + 0x03, 0x04);
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
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Ammo, OutputId.P1_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Ammo, OutputId.P2_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Ammo, OutputId.P3_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Ammo, OutputId.P4_Ammo));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Clip, OutputId.P1_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Clip, OutputId.P2_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_Clip, OutputId.P3_Clip));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_Clip, OutputId.P4_Clip));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P3_CtmRecoil, OutputId.P3_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P4_CtmRecoil, OutputId.P4_CtmRecoil, Configurator.GetInstance().OutputCustomRecoilOnDelay, Configurator.GetInstance().OutputCustomRecoilOffDelay, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P3_Damaged, OutputId.P3_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P4_Damaged, OutputId.P4_Damaged, Configurator.GetInstance().OutputCustomDamagedDelay, 100, 0));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            _P3_Ammo = 0;
            _P4_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;
            int P3_Clip = 0;
            int P4_Clip = 0;

            if (ReadByte(_GamePlaying_CaveAddress) == 1)
            {
                //Recoil
                if (ReadByte(_Recoil_CaveAddress) == 1)
                {
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);
                    WriteByte(_Recoil_CaveAddress, 0);
                }
                if (ReadByte(_Recoil_CaveAddress + 0x01) == 1)
                {
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);
                    WriteByte(_Recoil_CaveAddress + 0x01, 0);
                }
                if (ReadByte(_Recoil_CaveAddress + 0x02) == 1)
                {
                    SetOutputValue(OutputId.P3_CtmRecoil, 1);
                    WriteByte(_Recoil_CaveAddress + 0x02, 0);
                }
                if (ReadByte(_Recoil_CaveAddress + 0x03) == 1)
                {
                    SetOutputValue(OutputId.P3_CtmRecoil, 1);
                    WriteByte(_Recoil_CaveAddress + 0x03, 0);
                }

                //Ammo + Clip
                _P1_Ammo = BitConverter.ToInt32(ReadBytes(_Ammo_CaveAddress, 4), 0);
                if (_P1_Ammo == -1)
                    _P1_Ammo = 99;
                if (_P1_Ammo > 0)
                    P1_Clip = 1;
                
                _P2_Ammo = BitConverter.ToInt32(ReadBytes(_Ammo_CaveAddress + 0x04, 4), 0);
                if (_P2_Ammo == -1)
                    _P2_Ammo = 99;
                if (_P2_Ammo > 0)
                    P2_Clip = 1;
                
                _P3_Ammo = BitConverter.ToInt32(ReadBytes(_Ammo_CaveAddress + 0x08, 4), 0);
                if (_P3_Ammo == -1)
                    _P3_Ammo = 99;
                if (_P3_Ammo > 0)
                    P3_Clip = 1;
                
                _P4_Ammo = BitConverter.ToInt32(ReadBytes(_Ammo_CaveAddress + 0x0C, 4), 0);
                if (_P4_Ammo == -1)
                    _P4_Ammo = 99;
                if (_P4_Ammo > 0)
                    P4_Clip = 1;

                //Damage
                if (ReadByte(_Damage_CaveAddress) == 1)
                {
                    SetOutputValue(OutputId.P1_Damaged, 1);
                    WriteByte(_Damage_CaveAddress, 0);
                }
                if (ReadByte(_Damage_CaveAddress + 0x01) == 1)
                {
                    SetOutputValue(OutputId.P2_Damaged, 1);
                    WriteByte(_Damage_CaveAddress + 0x01, 0);
                }
                if (ReadByte(_Damage_CaveAddress + 0x02) == 1)
                {
                    SetOutputValue(OutputId.P3_Damaged, 1);
                    WriteByte(_Damage_CaveAddress + 0x02, 0);
                }
                if (ReadByte(_Damage_CaveAddress + 0x03) == 1)
                {
                    SetOutputValue(OutputId.P4_Damaged, 1);
                    WriteByte(_Damage_CaveAddress + 0x03, 0);
                }
            }

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P3_Ammo, _P3_Ammo);
            SetOutputValue(OutputId.P4_Ammo, _P4_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P3_Clip, P3_Clip);
            SetOutputValue(OutputId.P4_Clip, P4_Clip);
        }

        #endregion
    }
}
