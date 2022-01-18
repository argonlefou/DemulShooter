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
    public class Game_AllsHodSd : Game
    {

        //Custom Outputs
        private int _P1_LastLife = 0;
        private int _P2_LastLife = 0;
        private int _P1_LastAmmo = 0;
        private int _P2_LastAmmo = 0;
        private int _P1_Life = 0;
        private int _P2_Life = 0;
        private int _P1_Ammo = 0;
        private int _P2_Ammo = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_AllsHodSd(String RomName, bool Verbose) : base(RomName, "Hodzero-Win64-Shipping", Verbose)
        {
            _KnownMd5Prints.Add("Hodzero-Win64-Shipping.exe - Original Dump", "cde48c217d04caa64ee24a72f73dcce4");
            //Add amdaemon check ? (difference between original/Jconfig)
            //amdaemon.exe - original : 2ccb852ba8f98d6adf42ed62d1d1b759
            //amdaemon.exe - Jconfig  : 2cd0d9d4771f84724b6ea9a008f53ea4
            _tProcess.Start();
            Logger.WriteLog("Waiting for SEGA ALLS " + _RomName + " game to hook.....");
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
                            Logger.WriteLog(_Target_Process_Name + ".exe = 0x" + _TargetProcess_MemoryBaseAddress.ToString("X16"));
                            CheckExeMd5();
                            //SetHack();
                            _ProcessHooked = true;
                            RaiseGameHookedEvent();
                        }
                        else
                        {
                            Logger.WriteLog("ROM not Loaded...");
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
            /*UInt64 PtrOutput1 = ReadPtr((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032FC700));
            PtrOutput1 = ReadPtr((IntPtr)(PtrOutput1 + 0x40));
            PtrOutput1 = ReadPtr((IntPtr)(PtrOutput1 + 0xA0));
            PtrOutput1 = ReadPtr((IntPtr)PtrOutput1);*/

            UInt64 PtrOutput1 = ReadPtrChain((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032FC700), new UInt64[] { 0x40, 0xA0, 0x00 });                  
            if (PtrOutput1 != 0)
            {
                SetOutputValue(OutputId.P1_LmpStart, ReadByte((IntPtr)(PtrOutput1 + 0x4A0)) >> 6 & 0x01);
                SetOutputValue(OutputId.P2_LmpStart, ReadByte((IntPtr)(PtrOutput1 + 0x4A0)) >> 3 & 0x01);
                //SetOutputValue(OutputId.P1_CoinBlocker, ReadByte((IntPtr)(PtrOutput1 + 0x478)) >> 5 & 0x01);
                //SetOutputValue(OutputId.P2_CoinBlocker, ReadByte((IntPtr)(PtrOutput1 + 0x478)) >> 2 & 0x01);
            }

            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            UInt64 Ptr1 = ReadPtr((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x03087750));            
            if (Ptr1 != 0)
            {
                //Customs Outputs
                //Player Status :
                //[0] : Inactive
                //[1] : In-Game
                int P1_Status = ReadByte((IntPtr)(Ptr1 + 0x231));
                int P2_Status = ReadByte((IntPtr)(Ptr1 + 0x232));

                if (P1_Status != 0)
                { 
                    /*UInt64 PtrAmmo = ReadPtr((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032B5B88));
                    PtrAmmo = ReadPtr((IntPtr)(PtrAmmo + 0x30));
                    PtrAmmo = ReadPtr((IntPtr)(PtrAmmo + 0xB0));
                    PtrAmmo = ReadPtr((IntPtr)(PtrAmmo + 0x90));*/
                    UInt64 PtrAmmo = ReadPtrChain((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032B5B88), new UInt64[] { 0x30, 0xB0, 0x90 });
                    _P1_Ammo = ReadByte((IntPtr)(PtrAmmo + 0x378));
                    _P1_Life = ReadByte((IntPtr)(Ptr1 + 0x364));

                    //Custom Recoil
                    if (_P1_Ammo < _P1_LastAmmo)
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (_P1_Ammo > 0)
                        P1_Clip = 1;

                    //[Damaged] custom Output                
                    if (_P1_Life < _P1_LastLife)
                        SetOutputValue(OutputId.P1_Damaged, 1);
                }

                if (P2_Status != 0)
                {
                    /*UInt64 PtrAmmo = ReadPtr((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032B5B88));
                    PtrAmmo = ReadPtr((IntPtr)(PtrAmmo + 0x30));
                    PtrAmmo = ReadPtr((IntPtr)(PtrAmmo + 0xB0));
                    PtrAmmo = ReadPtr((IntPtr)(PtrAmmo + 0x98));*/
                    UInt64 PtrAmmo = ReadPtrChain((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032B5B88), new UInt64[] { 0x30, 0xB0, 0x98 });                    
                    _P2_Ammo = ReadByte((IntPtr)(PtrAmmo + 0x378));
                    _P2_Life = ReadByte((IntPtr)(Ptr1 + 0x43C));

                    //Custom Recoil
                    if (_P2_Ammo < _P2_LastAmmo)
                        SetOutputValue(OutputId.P2_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (_P2_Ammo > 0)
                        P2_Clip = 1;

                    //[Damaged] custom Output  
                    if (_P2_Life < _P2_LastLife)
                        SetOutputValue(OutputId.P2_Damaged, 1);
                }   
            }

            _P1_LastAmmo = _P1_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastAmmo = _P2_Ammo;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            /*UInt64 PtrCredits = ReadPtr((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032FC700));
            PtrCredits = ReadPtr((IntPtr)(PtrCredits + 0x88));
            PtrCredits = ReadPtr((IntPtr)(PtrCredits + 0x80));
            PtrCredits = ReadPtr((IntPtr)(PtrCredits + 0x50));*/
            UInt64 PtrCredits = ReadPtrChain((IntPtr)((UInt64)_TargetProcess_MemoryBaseAddress + 0x032FC700), new UInt64[] { 0x88, 0x90, 0x50 });                   
            SetOutputValue(OutputId.Credits, ReadByte((IntPtr)(PtrCredits + 0x21C)));
        }

        #endregion
    
    }
}
