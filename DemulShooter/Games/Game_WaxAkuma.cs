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
    class Game_WaxAkuma : Game
    {
        private const String GAMEDATA_FOLDER = @"MemoryData\gamewax\akuma";

        //Memory values
        private UInt32 _PlayersInfo_BaseOffset = 0x00190CE8;
        private UInt32 _Player1_X_Offset = 0x30;
        private UInt32 _Player1_Y_Offset = 0x32;
        private UInt32 _Player1_ForceNullAxis_Offset = 0x38;
        private UInt32 _Player1_ActivateAim_Offset = 0x39;
        private UInt32 _Player2_X_Offset = 0x50;
        private UInt32 _Player2_Y_Offset = 0x52;
        private UInt32 _Player2_ForceNullAxis_Offset = 0x58;
        private UInt32 _Player2_ActivateAim_Offset = 0x59;
        //private UInt32 _Buttons_Offset = 0x74;
        private UInt32 _Outputs_Offset = 0x78;
        private UInt32 _Player1_InGame_Offset = 0x7E;
        private UInt32 _Player2_InGame_Offset = 0x7F;

        //private UInt32 _RecoilEnable_Offset = 0x00190F12;
        private UInt32 _Player1_SHotCount_Offset = 0x5E3B48; //Also writing in 0x5E3B4C, 0x5E3B64, 0x5E3B68
        private UInt32 _Player2_SHotCount_Offset = 0x5E3BDC;
        private UInt32 _Player1_Life_Offset = 0x5E3B08;
        private UInt32 _Player2_Life_Offset = 0x5E3B9C;
        private UInt32 _Credits_Offset = 0x00190F02;
        private NopStruct _Axis_Nop_Offset = new NopStruct(0x000158D4, 5);
        private UInt32 _P1_Trigger_Injection_Offset = 0x00015800;
        private UInt32 _P1_Trigger_Injection_Return_Offset = 0x00015806;
        private UInt32 _P1_Trigger_InitialValueArray_Offset = 0x000D93C0;
        private UInt32 _P2_Trigger_Injection_Offset = 0x00015831;
        private UInt32 _P2_Trigger_InitialValueArray_Offset = 0x000D93CC;
        private UInt32 _P2_Trigger_Injection_Return_Offset = 0x00015837;

        //Custom values
        private UInt32 _P1_Trigger_BankAddress = 0;
        private UInt32 _P2_Trigger_BankAddress = 0;

        private int _P1_Last_ShotCount = 0;
        private int _P2_Last_ShotCount = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WaxAkuma(String RomName)
            : base(RomName, "game")
        {
            _KnownMd5Prints.Add("Akuma Mortis Immortal v81.07 - Original Dump", "4143b362a0cced0f6633974c710d91f9");
            _KnownMd5Prints.Add("Akuma Mortis Immortal v81.07 - Boot patched v1", "a8deac2b2c90187cce90fb09f767a695");
            _tProcess.Start();

            Logger.WriteLog("Waiting for GameWax " + _RomName + " game to hook.....");
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

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            _GameWindowHandle = _TargetProcess.MainWindowHandle;
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();
                            ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                            Apply_MemoryHacks();
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

                    //Axis values : 0x00 -> 0xFF
                    double dMaxX = 640.0;
                    double dMaxY = 480.0;

                    PlayerData.RIController.Computed_X = Convert.ToInt32(Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt32(Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));
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

        protected override void Apply_InputsMemoryHack()
        {
            //One of the existing package has a modified inpout32.dll acting on axis values with Mouse
            //Original dump does nothing
            //So to be sure values are clean for Demulshooter, disabling the call to the original functions filling the values
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Axis_Nop_Offset);

            Create_InputsDataBank();
            _P1_Trigger_BankAddress = _InputsDatabank_Address;
            _P2_Trigger_BankAddress = _InputsDatabank_Address + 0x04;

            SetHack_P1Trigger();
            SetHack_P2Trigger();

            Logger.WriteLog("Inputs Memory Hack complete !");
            Logger.WriteLog("-");
        }
        
        /// <summary>
        /// Usingour own flag to update the original flag during the looped procedure to update game inputs
        /// </summary>
        private void SetHack_P1Trigger()
        {

            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov edx,[eax+game.exe+D93C0]
            CaveMemory.Write_StrBytes("8B 90");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Trigger_InitialValueArray_Offset));
            //cmp eax, 90
            CaveMemory.Write_StrBytes("3D 90 00 00 00");
            //jne Exit
            CaveMemory.Write_StrBytes("0F 85 18 00 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P1_Trigger_BankAddress
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P1_Trigger_BankAddress));
            //test eax, eax
            CaveMemory.Write_StrBytes("85 C0");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //je Exit
            CaveMemory.Write_StrBytes("0F 84 08 00 00 00");
            //or [esp+edx+18],00000080
            CaveMemory.Write_StrBytes("81 4C 14 18 80 00 00 00");
            
            //Jump back
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Trigger_Injection_Return_Offset);

            Logger.WriteLog("Adding P1 Trigger CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Trigger_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _P1_Trigger_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        /// <summary>
        /// Usingour own flag to update the original flag during the looped procedure to update game inputs
        /// </summary>
        private void SetHack_P2Trigger()
        {

            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();

            //mov edx,[eax+game.exe+D93CC]
            CaveMemory.Write_StrBytes("8B 90");
            CaveMemory.Write_Bytes(BitConverter.GetBytes((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Trigger_InitialValueArray_Offset));
            //cmp eax, 90
            CaveMemory.Write_StrBytes("3D 90 00 00 00");
            //jne Exit
            CaveMemory.Write_StrBytes("0F 85 18 00 00 00");
            //push eax
            CaveMemory.Write_StrBytes("50");
            //mov eax, _P2_Trigger_BankAddress
            CaveMemory.Write_StrBytes("8B 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_P2_Trigger_BankAddress));
            //test eax, eax
            CaveMemory.Write_StrBytes("85 C0");
            //pop eax
            CaveMemory.Write_StrBytes("58");
            //je Exit
            CaveMemory.Write_StrBytes("0F 84 08 00 00 00");
            //or [esp+edx+18],00000080
            CaveMemory.Write_StrBytes("81 4C 14 18 80 00 00 00");

            //Jump back
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Trigger_Injection_Return_Offset);

            Logger.WriteLog("Adding P2 Trigger CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Trigger_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _P2_Trigger_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>  
        public override void SendInput(PlayerSettings PlayerData)
        {
            byte[] bufferX = BitConverter.GetBytes((UInt16)PlayerData.RIController.Computed_X);
            byte[] bufferY = BitConverter.GetBytes((UInt16)PlayerData.RIController.Computed_Y);

            if (PlayerData.ID == 1)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersInfo_BaseOffset + _Player1_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersInfo_BaseOffset + _Player1_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersInfo_BaseOffset + _Player1_ActivateAim_Offset, 0x01);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersInfo_BaseOffset + _Player1_ForceNullAxis_Offset, 0x00);
                    WriteByte(_P1_Trigger_BankAddress, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte(_P1_Trigger_BankAddress, 0x00);            
            }
            else if (PlayerData.ID == 2)
            {
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersInfo_BaseOffset + _Player2_X_Offset, bufferX);
                WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersInfo_BaseOffset + _Player2_Y_Offset, bufferY);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                {
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersInfo_BaseOffset + _Player2_ActivateAim_Offset, 0x01);
                    WriteByte((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersInfo_BaseOffset + _Player2_ForceNullAxis_Offset, 0x00);
                    WriteByte(_P2_Trigger_BankAddress, 0x01);
                }
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    WriteByte(_P2_Trigger_BankAddress, 0x00);  
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : stays activated when trigger is pulled
            //Gun recoil : not used ??
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpLeft, OutputId.LmpLeft));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpRight, OutputId.LmpRight));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunRecoil, OutputId.P2_GunRecoil));
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
            //Genuine Outputs
            byte OutputData = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersInfo_BaseOffset + _Outputs_Offset);
            SetOutputValue(OutputId.P1_LmpStart, OutputData & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, OutputData >> 1 & 0x01);
            SetOutputValue(OutputId.LmpLeft, OutputData >> 4 & 0x01);
            SetOutputValue(OutputId.LmpRight, OutputData >> 5 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, OutputData >> 6 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, OutputData >> 7 & 0x01);

            _P1_Life = 0;
            _P2_Life = 0;
            int P1_ShotCount = 0;
            int P2_ShotCount = 0;

            //Custom Outputs
            if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersInfo_BaseOffset + _Player1_InGame_Offset) == 1)
            {
                //Recoil byte don't move a lot (except sometimes during TEST menu)
                //To compute custom Recoil, we can read the shot number incresing value with each bullet
                P1_ShotCount = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Player1_SHotCount_Offset, 4), 0);
                if (P1_ShotCount > _P1_Last_ShotCount)
                    SetOutputValue(OutputId.P1_CtmRecoil, 1);

                _P1_Life = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Player1_Life_Offset, 4), 0);
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }

            if (ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _PlayersInfo_BaseOffset + _Player2_InGame_Offset) == 1)
            {
                P2_ShotCount = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Player2_SHotCount_Offset, 4), 0);
                if (P2_ShotCount > _P2_Last_ShotCount)
                    SetOutputValue(OutputId.P2_CtmRecoil, 1);

                _P2_Life = BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Player2_Life_Offset, 4), 0);
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);
            }

            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            _P1_Last_ShotCount = P1_ShotCount;
            _P2_Last_ShotCount = P2_ShotCount;
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.Credits, BitConverter.ToInt32(ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Credits_Offset, 4), 0));
        }

        #endregion
    }
}
