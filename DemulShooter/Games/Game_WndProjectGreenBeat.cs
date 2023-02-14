using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.MameOutput;
using DsCore.Memory;
using DsCore.Win32;

namespace DemulShooter
{
    public class Game_WndProjectGreenBeat : Game
    {
        //Memory values
        private UInt32 _AxisX_Address = 0;
        private UInt32 _AxisY_Address = 0;
        private UInt32 _Axis_Injection_Offset = 0x00163CB1;
        private UInt32 _Axis_Injection_Return_Offset = 0x00163CB8;

        private UInt32 _AxisX_BankAddress = 0;
        private UInt32 _AxisY_BankAddress = 0;

        //Custom data to inject
        private float _P1_X_Value;
        private float _P1_Y_Value;

        //Custom Outputs
        private UInt32 _ShotCounts_Address = 0;
        private int _LastShotsCount = 0;
        private int _ShotsCount = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_WndProjectGreenBeat(String RomName, bool DisableInputHack, bool Verbose)
            : base(RomName, "ProjectGreenBeat", DisableInputHack, Verbose)
        {
            _KnownMd5Prints.Add("Project Green Beat - PROPHET", "7962b8b40d71464a988a7c6db96c88c0");

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
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();

                            FetchAxisAddress();

                            _ShotCounts_Address = ReadPtrChain((UInt32)_TargetProcess_MemoryBaseAddress + 0x0168E728, new UInt32[] { 0xEC }) + 0xD4;

                            Logger.WriteLog("AxisX_Address = 0x" + _AxisX_Address.ToString("X8"));
                            Logger.WriteLog("AxisY_Address = 0x" + _AxisY_Address.ToString("X8"));

                            if (_AxisX_Address != 0 && _AxisY_Address != 0)
                            {
                                if (!_DisableInputHack)
                                    SetHack();
                                else
                                    Logger.WriteLog("Input Hack disabled");
                                _ProcessHooked = true;
                                RaiseGameHookedEvent();
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
                //Axis address is changing during the game, need to update it
                FetchAxisAddress();

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

        private void FetchAxisAddress()
        {
            _AxisX_Address = ReadPtrChain((UInt32)_TargetProcess_MemoryBaseAddress + 0x022D0BF0, new UInt32[] { 0x04, 0xD8, 0x450, 0x3C4 }) + 0x360;
            _AxisY_Address = _AxisX_Address + 0x04;

            if (_AxisX_BankAddress != 0)
                WriteBytes(_AxisX_BankAddress, BitConverter.GetBytes(_AxisX_Address));

            if (_AxisY_BankAddress != 0)
                WriteBytes(_AxisY_BankAddress, BitConverter.GetBytes(_AxisY_Address));
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
                    int TotalResX = TotalRes.Right - TotalRes.Left;
                    int TotalResY = TotalRes.Bottom - TotalRes.Top;

                    Logger.WriteLog("Game client window resolution (Px) = [ " + TotalResX + "x" + TotalResY + " ]");

                    //X => [0 ; 1] float
                    //Y => [0 ; 1] float
                    float X_Value = PlayerData.RIController.Computed_X / TotalResX;
                    float Y_Value = PlayerData.RIController.Computed_Y / TotalResY;

                    if (X_Value < 0)
                        X_Value = 0;
                    if (Y_Value < 0)
                        Y_Value = 0;
                    if (X_Value > 1.0f)
                        X_Value = 1.0f;
                    if (Y_Value > 1.0f)
                        Y_Value = 1.0f;

                    _P1_X_Value = X_Value;
                    _P1_Y_Value = Y_Value;

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
        /// UE3 engine game, input in menu is fine with mouse+lightgun but does only work with mouse in-game
        /// Float [-1;1] computed values are updated with a generic function so we need to filter with the Axis pointer to stop it and write ourself later
        /// Axis address must be updated often as the game is changign it with each level
        /// </summary>
        private void SetHack()
        {
            CreateDataBank();

            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            List<Byte> Buffer = new List<Byte>();
            //movaps xmm0,xmm1
            CaveMemory.Write_StrBytes("0F 28 C1");
            //mov ecx, [_AxisX_BankAddress]
            CaveMemory.Write_StrBytes("8B 0D");
            byte[] b = BitConverter.GetBytes(_AxisX_BankAddress);
            CaveMemory.Write_Bytes(b);
            //cmp eax, ecx
            CaveMemory.Write_StrBytes("39 C8");            
            //je exit 
            CaveMemory.Write_StrBytes("74 0B");
            //add ecx, 04
            CaveMemory.Write_StrBytes("83 C1 04");
            //cmp eax, ecx
            CaveMemory.Write_StrBytes("39 C8");
            //je exit
            CaveMemory.Write_StrBytes("74 04");
            //movss [eax],xmm0
            CaveMemory.Write_StrBytes("F3 0F 11 00");            
            //return
            CaveMemory.Write_jmp((UInt32)_TargetProcess_MemoryBaseAddress + _Axis_Injection_Return_Offset);

            Logger.WriteLog("Adding Axis CodeCave at : 0x" + CaveMemory.CaveAddress.ToString("X8"));

            //Code injection
            IntPtr ProcessHandle = _TargetProcess.Handle;
            UInt32 bytesWritten = 0;
            UInt32 jumpTo = 0;
            jumpTo = CaveMemory.CaveAddress - ((UInt32)_TargetProcess_MemoryBaseAddress + _Axis_Injection_Offset) - 5;
            Buffer = new List<byte>();
            Buffer.Add(0xE9);
            Buffer.AddRange(BitConverter.GetBytes(jumpTo));
            Buffer.Add(0x90);
            Win32API.WriteProcessMemory(ProcessHandle, (UInt32)_TargetProcess_MemoryBaseAddress + _Axis_Injection_Offset, Buffer.ToArray(), (UInt32)Buffer.Count, ref bytesWritten);
        }

        /// <summary>
        /// Custom data storage
        /// </summary>
        private void CreateDataBank()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            _AxisX_BankAddress = CaveMemory.CaveAddress;
            _AxisY_BankAddress = CaveMemory.CaveAddress + 0x08;

            Logger.WriteLog("Custom data will be stored at : 0x" + _AxisX_BankAddress.ToString("X8"));
        }

        #endregion

        #region Inputs

        /// <summary>
        /// Writing Axis and Buttons data in memory
        /// </summary>   
        public override void SendInput(PlayerSettings PlayerData)
        {
            if (PlayerData.ID == 1)
            {
                //Setting Values in memory for the Codecave to read it
                byte[] buffer = BitConverter.GetBytes(_P1_X_Value);
                WriteBytes(_AxisX_Address, buffer);
                buffer = BitConverter.GetBytes(_P1_Y_Value);
                WriteBytes(_AxisY_Address, buffer);
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
            _Outputs.Add(new AsyncGameOutput(OutputDesciption.P1_CtmRecoil, OutputId.P1_CtmRecoil, MameOutputHelper.CustomRecoilOnDelay, MameOutputHelper.CustomRecoilOffDelay, 0));            
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            if (_ShotCounts_Address != 0)
                _ShotsCount = BitConverter.ToInt32(ReadBytes(_ShotCounts_Address, 4), 0);
            else
                _ShotsCount = 0;   

            if (_ShotsCount > _LastShotsCount)           
                SetOutputValue(OutputId.P1_CtmRecoil, 1);

            _LastShotsCount = _ShotsCount;
        }

        #endregion
    }
}
