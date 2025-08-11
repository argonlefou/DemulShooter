using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using DsCore;
using DsCore.MameOutput;
using DsCore.Memory;

namespace DemulShooter
{

    class Game_KonamiGashaaaan2 : Game
    {
        //MEMORY ADDRESSES
        private UInt32 _CreditsPtr_Offset = 0x002BB0D8;
        private UInt32 _ig2ACLibIOSetLamp_PtrOffset = 0x0002B5EEC;
        private UInt32 _ig2ACLibIOCoinBlockerOpen_PtrOffset = 0x0002B5F00;
        private UInt32 _ig2ACLibIOCoinBlockerClose_PtrOffset = 0x0002B5F04;
        private UInt32 _ig2ACLibIOSetBallSupply_PtrOffset = 0x0002B5EF0;
        private UInt32 _ig2ACLibIOSetBallSupplyStop_PtrOffset = 0x0002B5EF4;
        private UInt32 _ig2ACLibIOSetBallSupplyStopImmediate_PtrOffset = 0x0002B5EF8;

        //custom Values
        private UInt32 _Lamps_CaveAddress = 0;
        private UInt32 _CoinBlocker_CaveAddress = 0;
        private UInt32 _MotorUnits_CaveAddress = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_KonamiGashaaaan2(String RomName)
            : base(RomName, "gasya")
        {
            _KnownMd5Prints.Add("Gashaaaan Refills v. JA-A01:2009-01-13", "db158b447371831317229ecc79920ee8");
            _tProcess.Start();
            Logger.WriteLog("Waiting for Konami " + _RomName + " game to hook.....");
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
                    foreach (Process p in Process.GetProcessesByName(_Target_Process_Name))
                    {
                        _TargetProcess = p;
                        _ProcessHandle = _TargetProcess.Handle;
                        _TargetProcess_MemoryBaseAddress = _TargetProcess.MainModule.BaseAddress;

                        if (_TargetProcess_MemoryBaseAddress != IntPtr.Zero)
                        {
                            _GameWindowHandle = _TargetProcess.MainWindowHandle;
                            Logger.WriteLog("Attached to Process " + _Target_Process_Name + ".exe, ProcessHandle = " + _ProcessHandle);
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X8"));
                            CheckExeMd5();
                            Apply_MemoryHacks();
                            _ProcessHooked = true;
                            RaiseGameHookedEvent();
                            break;
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

        #region Memory Hack

        protected override void Apply_OutputsMemoryHack()
        {
            Create_OutputsDataBank();
            _Lamps_CaveAddress = _OutputsDatabank_Address;
            _CoinBlocker_CaveAddress = _OutputsDatabank_Address + 0x10;
            _MotorUnits_CaveAddress = _OutputsDatabank_Address + 0x14;

            SetHack_ig2ACLibIOSetLamp();
            SetHack_ig2ACLibIOCoinBlockerOpen();
            SetHack_ig2ACLibIOCoinBlockerClose();

            SetHack_ig2ACLibIOSetBallSupply();
            SetHack_ig2ACLibIOSetBallSupplyStop();

            Logger.WriteLog("Outputs Memory Hack complete !");
            Logger.WriteLog("-");
        }

        /// <summary>
        /// Create a simple function that gets the Lamp Id (ESP+4) and Lamp value (ESP+8) before RET
        /// </summary>
        private void SetHack_ig2ACLibIOSetLamp()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //push ecx
            CaveMemory.Write_StrBytes("51");
            //mov eax,_Lamps_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_Lamps_CaveAddress));
            //add eax,[esp+08]
            CaveMemory.Write_StrBytes("03 44 24 08");
            //mov ecx,[esp+0C]
            CaveMemory.Write_StrBytes("8B 4C 24 0C");
            //mov [eax],cl
            CaveMemory.Write_StrBytes("88 08");
            //pop ecx
            CaveMemory.Write_StrBytes("59");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _ig2ACLibIOSetLamp_PtrOffset, BitConverter.GetBytes(CaveMemory.CaveAddress));
        }

        /// <summary>
        /// Creating a simple function to set a flag
        /// </summary>
        private void SetHack_ig2ACLibIOCoinBlockerOpen()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov byte ptr[_CoinBlocker_CaveAddress], 1
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CoinBlocker_CaveAddress));
            CaveMemory.Write_StrBytes("01");
            //ret
            CaveMemory.Write_StrBytes("C3");

            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _ig2ACLibIOCoinBlockerOpen_PtrOffset, BitConverter.GetBytes(CaveMemory.CaveAddress));
        }

        /// <summary>
        /// Creating a simple function to set a flag
        /// </summary>
        private void SetHack_ig2ACLibIOCoinBlockerClose()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov byte ptr[_CoinBlocker_CaveAddress], 1
            CaveMemory.Write_StrBytes("C6 05");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_CoinBlocker_CaveAddress));
            CaveMemory.Write_StrBytes("00");
            //ret
            CaveMemory.Write_StrBytes("C3");

            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _ig2ACLibIOCoinBlockerClose_PtrOffset, BitConverter.GetBytes(CaveMemory.CaveAddress));
        }

        /// <summary>
        /// Creating simple functions to set a flag
        /// Motor ID (0/1) is in ESP+4
        /// </summary>
        private void SetHack_ig2ACLibIOSetBallSupply()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov eax,_Lamps_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_MotorUnits_CaveAddress));
            //add eax,[esp+04]
            CaveMemory.Write_StrBytes("03 44 24 04");
            //mov byte ptr[eax], 1
            CaveMemory.Write_StrBytes("C6 00 01");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _ig2ACLibIOSetBallSupply_PtrOffset, BitConverter.GetBytes(CaveMemory.CaveAddress));
        }

        /// <summary>
        /// Creating simple functions to set a flag
        /// Motor ID (0/1) is in ESP+4
        /// Applying the same function for the 2 different procedure (Stop + StopImmediate)
        /// </summary>
        private void SetHack_ig2ACLibIOSetBallSupplyStop()
        {
            Codecave CaveMemory = new Codecave(_TargetProcess, _TargetProcess.MainModule.BaseAddress);
            CaveMemory.Open();
            CaveMemory.Alloc(0x800);

            //mov eax,_MotorUnits_CaveAddress
            CaveMemory.Write_StrBytes("B8");
            CaveMemory.Write_Bytes(BitConverter.GetBytes(_MotorUnits_CaveAddress));
            //add eax,[esp+04
            CaveMemory.Write_StrBytes("03 44 24 04");
            //mov byte ptr[eax], 0
            CaveMemory.Write_StrBytes("C6 00 00");
            //xor eax,eax
            CaveMemory.Write_StrBytes("31 C0");
            //ret
            CaveMemory.Write_StrBytes("C3");

            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _ig2ACLibIOSetBallSupplyStop_PtrOffset, BitConverter.GetBytes(CaveMemory.CaveAddress));
            WriteBytes((UInt32)_TargetProcess_MemoryBaseAddress + _ig2ACLibIOSetBallSupplyStopImmediate_PtrOffset, BitConverter.GetBytes(CaveMemory.CaveAddress));
        }

        #endregion

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();

            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_LmpStart, OutputId.P3_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_LmpStart, OutputId.P4_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpFront, OutputId.P1_LmpFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpFront, OutputId.P2_LmpFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P3_LmpFront, OutputId.P3_LmpFront));
            _Outputs.Add(new GameOutput(OutputDesciption.P4_LmpFront, OutputId.P4_LmpFront));
            _Outputs.Add(new GameOutput(OutputDesciption.MotorUnit_1, OutputId.MotorUnit_1));
            _Outputs.Add(new GameOutput(OutputDesciption.MotorUnit_2, OutputId.MotorUnit_2));
            _Outputs.Add(new GameOutput(OutputDesciption.CoinBlocker, OutputId.CoinBlocker));
            _Outputs.Add(new GameOutput(OutputDesciption.Credits, OutputId.Credits));
        }

        /// <summary>
        /// Update all Outputs values before sending them to MameHooker
        /// </summary>
        public override void UpdateOutputValues()
        {
            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte(_Lamps_CaveAddress));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte(_Lamps_CaveAddress + 1));
            SetOutputValue(OutputId.P3_LmpStart, ReadByte(_Lamps_CaveAddress + 2));
            SetOutputValue(OutputId.P4_LmpStart, ReadByte(_Lamps_CaveAddress + 3));
            SetOutputValue(OutputId.P1_LmpFront, ReadByte(_Lamps_CaveAddress + 4));
            SetOutputValue(OutputId.P2_LmpFront, ReadByte(_Lamps_CaveAddress + 5));
            SetOutputValue(OutputId.P3_LmpFront, ReadByte(_Lamps_CaveAddress + 6));
            SetOutputValue(OutputId.P4_LmpFront, ReadByte(_Lamps_CaveAddress + 7));
            SetOutputValue(OutputId.MotorUnit_1, ReadByte(_MotorUnits_CaveAddress));
            SetOutputValue(OutputId.MotorUnit_2, ReadByte(_MotorUnits_CaveAddress + 1));
            SetOutputValue(OutputId.CoinBlocker, ReadByte(_CoinBlocker_CaveAddress));

            int Credits = (int)ReadPtrChain((UInt32)_TargetProcess_MemoryBaseAddress + _CreditsPtr_Offset, new UInt32[] { 0x20 });
            SetOutputValue(OutputId.Credits, Credits);
        }

        #endregion
    }
}
