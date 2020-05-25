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
    class Game_RwSDR : Game
    {
        private const string GAMEDATA_FOLDER = @"MemoryData\ringwide\sdr";

        /*** MEMORY ADDRESSES **/
        private UInt32 _Controls_Base_Address;
        private UInt32 _Data_Base_Address_Ptr_Offset = 0x0308A0C4;
        private UInt32 _P1_X_Offset = 0x00000017;
        private UInt32 _P1_Y_Offset = 0x00000015;
        private UInt32 _P1_Buttons_Offset = 0x00000008;
        private UInt32 _P2_X_Offset = 0x0000001B;
        private UInt32 _P2_Y_Offset = 0x00000019;
        private UInt32 _P2_Buttons_Offset = 0x0000000A;
        private NopStruct _Nop_Axis = new NopStruct(0x000DB450, 5);
        private UInt32 _Buttons_Injection_Offset = 0x000DB3EC;
        private UInt32 _Buttons_Injection_Return_Offset = 0x000DB3F1;

        //Outputs
        private UInt32 _Outputs_Offset = 0x0308A0A4;
        private UInt32 _P1_Status_Offset = 0x0118FF60;
        private UInt32 _P1_Life_Offset = 0x0118FECC;
        private UInt32 _P2_Status_Offset = 0x0118FFFC;
        private UInt32 _P2_Life_Offset = 0x0118FEE0;
        private int _P1_LastLife = 0;
        private int _P2_LastLife = 0;
        private int _P1_Life = 0;
        private int _P2_Life = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_RwSDR(String RomName, double _ForcedXratio, bool Verbose)
            : base(RomName, "game", _ForcedXratio, Verbose)
        {
            _KnownMd5Prints.Add("Sega Dream Raider - For TeknoParrot", "4264540b2a24f3359a3deb5f1e95e392");
            _KnownMd5Prints.Add("Sega Dream Raider - For JConfig", "da993b12f7572f828c578c9eb73b3111");
            _tProcess.Start();

            Logger.WriteLog("Waiting for RingWide " + _RomName + " game to hook.....");
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
                            byte[] buffer = ReadBytes((UInt32)_TargetProcess_MemoryBaseAddress + _Data_Base_Address_Ptr_Offset, 4);
                            _Controls_Base_Address = BitConverter.ToUInt32(buffer, 0);
                            if (_Controls_Base_Address != 0)
                            {
                                Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                                Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                                Logger.WriteLog("Controls base address = 0x" + _Controls_Base_Address.ToString("X8"));
                                CheckExeMd5();
                                ReadGameDataFromMd5Hash(GAMEDATA_FOLDER);
                                SetHack();
                                _ProcessHooked = true;                                
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
        /// Convert client area pointer location to Game speciffic data for memory injection
        /// </summary>
        public override bool GameScale(PlayerSettings PlayerData)
        {
            if (_ProcessHandle != IntPtr.Zero)
            {
                try
                {
                    Rect TotalRes = new Rect();
                    Win32API.GetClientRect(_TargetProcess.MainWindowHandle, ref TotalRes);
                    double TotalResX = TotalRes.Right - TotalRes.Left;
                    double TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X => [0-E1] = 225
                    //Y => [0-E1] = 225
                    //Axes inversés : 0 = Bas et Droite
                    double dMaxX = 225.0;
                    double dMaxY = 225.0;

                    PlayerData.RIController.Computed_X = Convert.ToInt32(dMaxX - Math.Round(dMaxX * PlayerData.RIController.Computed_X / TotalResX));
                    PlayerData.RIController.Computed_Y = Convert.ToInt32(dMaxY - Math.Round(dMaxY * PlayerData.RIController.Computed_Y / TotalResY));
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

        /// <summary>
        /// Genuine Hack, just blocking Axis and Triggers input to replace them
        /// Reverse back to it when DumbJVSCommand will be working with ParrotLoader, without DumbJVSManager
        /// </summary>
        private void SetHack()
        {
            //NOPing axis proc
            SetNops((UInt32)_TargetProcess_MemoryBaseAddress, _Nop_Axis);

            //Hacking buttons proc : 
            //Same byte is used for both triggers, start and service (for each player)
            //0b10000000 is Start
            //0b01000000 is Service
            //0b00000010 is Trigger
            //So we need to make a mask to accept Start button moodification and block other so we can inject
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);
            List<Byte> Buffer = new List<Byte>();
            //push esi
            CaveMemory.Write_StrBytes("56");
            //and esi,00000080
            CaveMemory.Write_StrBytes("81 E6 80 00 00 00");
            //cmp esi,00
            CaveMemory.Write_StrBytes("83 FE 00");
            //jg @ => if Start PRessed
            CaveMemory.Write_StrBytes("0F 8F 0D 00 00 00");
            //and dword ptr [ebp+ecx*2+08],7F => Putting the start bit to 0
            CaveMemory.Write_StrBytes("81 64 4D 08 7F FF FF FF");
            //jmp @
            CaveMemory.Write_StrBytes("E9 08 00 00 00");
            //or [ebp+ecx*2+08],00000080 ==> start is pressed, putting bit to 1
            CaveMemory.Write_StrBytes("81 4C 4D 08 80 00 00 00");
            //pop esi
            CaveMemory.Write_StrBytes("5E");
            //and esi,00000040
            CaveMemory.Write_StrBytes("83 E6 40");
            //cmp esi,00
            CaveMemory.Write_StrBytes("83 FE 00");
            //jg @ => if Service PRessed
            CaveMemory.Write_StrBytes("0F 8F 0A 00 00 00");
            //and [ebp+ecx*2+08],000000BF => Putting the Service bit to 0
            CaveMemory.Write_StrBytes("83 64 4D 08 BF");
            //jmp @
            CaveMemory.Write_StrBytes("E9 05 00 00 00");
            //or dword ptr [ebp+ecx*2+08],40 ==> Service is pressed, putting bit to 1
            CaveMemory.Write_StrBytes("83 4C 4D 08 40");
            //Jump back
            CaveMemory.Write_jmp((UInt32)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Return_Offset);

            Logger.WriteLog("Adding CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess.MainModule.BaseAddress + _Buttons_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);

            Logger.WriteLog("Memory Hack complete !");
            Logger.WriteLog("-");
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
                WriteByte(_Controls_Base_Address + _P1_X_Offset, bufferX[0]);
                WriteByte(_Controls_Base_Address + _P1_Y_Offset, bufferY[0]);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Controls_Base_Address + _P1_Buttons_Offset, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Controls_Base_Address + _P1_Buttons_Offset, 0xFD);
            }
            else if (PlayerData.ID == 2)
            {
                WriteByte(_Controls_Base_Address + _P2_X_Offset, bufferX[0]);
                WriteByte(_Controls_Base_Address + _P2_Y_Offset, bufferY[0]);

                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerDown) != 0)
                    Apply_OR_ByteMask(_Controls_Base_Address + _P2_Buttons_Offset, 0x02);
                if ((PlayerData.RIController.Computed_Buttons & RawInputcontrollerButtonEvent.OnScreenTriggerUp) != 0)
                    Apply_AND_ByteMask(_Controls_Base_Address + _P2_Buttons_Offset, 0xFD);
            }
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            //Gun motor : ??            
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_R, OutputId.P1_Lmp_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_G, OutputId.P1_Lmp_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Lmp_B, OutputId.P1_Lmp_B));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_R, OutputId.P2_Lmp_R));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_G, OutputId.P2_Lmp_G));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Lmp_B, OutputId.P2_Lmp_B));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpRear_R, OutputId.LmpRear_R));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpRear_G, OutputId.LmpRear_G));
            _Outputs.Add(new GameOutput(OutputDesciption.LmpRear_B, OutputId.LmpRear_B));
            _Outputs.Add(new GameOutput(OutputDesciption.Blower_Lvl, OutputId.Blower_Level));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunMotor, OutputId.P1_GunMotor));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunMotor, OutputId.P2_GunMotor));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilDelay));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_CtmRecoil, OutputId.P2_CtmRecoil, MameOutputHelper.CustomRecoilDelay));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_Life, OutputId.P1_Life));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_Life, OutputId.P2_Life));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_Damaged, OutputId.P1_Damaged, MameOutputHelper.CustomDamageDelay));
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P2_Damaged, OutputId.P2_Damaged, MameOutputHelper.CustomDamageDelay)); 
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            byte OutputData1 = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset);
            byte OutputData2 = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset + 1);
            byte OutputData3 = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _Outputs_Offset + 2);
            SetOutputValue(OutputId.P1_LmpStart, OutputData1 >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, OutputData1 >> 4 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_R, OutputData2 >> 7 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_G, OutputData2 >> 6 & 0x01);
            SetOutputValue(OutputId.P1_Lmp_B, OutputData2 >> 5 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_R, OutputData2 >> 4 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_G, OutputData2 >> 3 & 0x01);
            SetOutputValue(OutputId.P2_Lmp_B, OutputData2 >> 2 & 0x01);
            SetOutputValue(OutputId.LmpRear_R, OutputData2 >> 1 & 0x01);
            SetOutputValue(OutputId.LmpRear_G, OutputData2 & 0x01);
            SetOutputValue(OutputId.LmpRear_B, OutputData3 >> 7 & 0x01);
            SetOutputValue(OutputId.Blower_Level, (OutputData1 & 0x03) + (OutputData3 >> 4 & 0x04));
            SetOutputValue(OutputId.P1_GunMotor, OutputData1 >> 6 & 0x01);
            SetOutputValue(OutputId.P2_GunMotor, OutputData1 >> 3 & 0x01);
            
            //Custom Outputs
            int P1_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Status_Offset);
            int P2_Status = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Status_Offset);
            _P1_Life = 0;
            _P2_Life = 0;

            if (P1_Status == 1)
            {
                _P1_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P1_Life_Offset);
           
                //[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);
            }
            if (P2_Status == 1)
            {
                _P2_Life = ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + _P2_Life_Offset);
            
                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);
            }
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);
            SetOutputValue(OutputId.Credits, ReadByte((UInt32)_TargetProcess_MemoryBaseAddress + 0x00607FF0));
        }

        #endregion
    }
}
