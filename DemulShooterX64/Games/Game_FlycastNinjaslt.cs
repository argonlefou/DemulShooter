using System;
using System.Collections.Generic;
using DsCore.Config;
using DsCore.MameOutput;

namespace DemulShooterX64
{
    public class Game_FlycastNinjaslt : Game_Flycast
    {
        private UInt32 _Outputs_Outputs_Offset = 0x0022C42D;
        private UInt32 _Outputs_Credits_Offset = 0x00480D0C;
        private UInt32 _Outputs_PlayerData_Offset = 0x003D9D30;
        private int _P1_LastDammage = 0;
        private int _P2_LastDammage = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public Game_FlycastNinjaslt(String RomName) : base(RomName)
        { 
            if (RomName.Equals("ninjaslt"))
            {
                _Outputs_Outputs_Offset = 0x0022C42D;
                _Outputs_Credits_Offset = 0x00480D0C;
                //_Outputs_PlayerData_Offset = 0x003D9D30;     Demul
                _Outputs_PlayerData_Offset = 0x003D9CB0;
            }
            else if (RomName.Equals("ninjaslta"))
            {
                _Outputs_Outputs_Offset = 0x0022C4AD;
                _Outputs_Credits_Offset = 0x00480D8C;
                _Outputs_PlayerData_Offset = 0x003D9D30;
            }
            else if (RomName.Equals("ninjasltj"))
            {
                _Outputs_Outputs_Offset = 0x0022C3F1;
                _Outputs_Credits_Offset = 0x00480CD4;
                _Outputs_PlayerData_Offset = 0x003D9C78;
            }
            else if (RomName.Equals("ninjasltu"))
            {
                _Outputs_Outputs_Offset = 0x0022C4AD;
                _Outputs_Credits_Offset = 0x00480D8C;
                _Outputs_PlayerData_Offset = 0x003D9D30;
            }
        }

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
            _Outputs = new List<GameOutput>();
            _Outputs.Add(new GameOutput(OutputDesciption.P1_LmpStart, OutputId.P1_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_LmpStart, OutputId.P2_LmpStart));
            _Outputs.Add(new GameOutput(OutputDesciption.P1_GunRecoil, OutputId.P1_GunRecoil));
            _Outputs.Add(new GameOutput(OutputDesciption.P2_GunRecoil, OutputId.P2_GunRecoil));
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
            UInt64 Outputs_Address = _GameRAM_Address + _Outputs_Outputs_Offset;

            int P1_Life = 0;
            int P2_Life = 0;
            int P1_Ammo = 0;
            int P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            //Check if the game is in Gameplay mode
            if (ReadByte((IntPtr)(_GameRAM_Address + _Outputs_PlayerData_Offset + 0x1A)) == 1)
            {
                //Didn't find any reliable "player state", but Life seem to stay at 0 when not playing, so we will use that
                //Note that at start, life may be > 0 if the player has never entered a game :(
                P1_Life = (int)BitConverter.ToInt16(ReadBytes((IntPtr)(_GameRAM_Address + _Outputs_PlayerData_Offset + 0x54), 2), 0);
                P2_Life = (int)BitConverter.ToInt16(ReadBytes((IntPtr)(_GameRAM_Address + _Outputs_PlayerData_Offset + 0x56), 2), 0);

                //For custom dammaged : 
                //1) Solution 1 : Decrease life = small delay between the hit and the life beeing lost
                /*//[Damaged] custom Output                
                if (_P1_Life < _P1_LastLife)
                    SetOutputValue(OutputId.P1_Damaged, 1);

                //[Damaged] custom Output                
                if (_P2_Life < _P2_LastLife)
                    SetOutputValue(OutputId.P2_Damaged, 1);*/

                //2) solution 2 : Read a byte value wich is != 0 when hit (invicibility duration ?) but the "1" state duration is long and may trigger the output multiple times                
                int P1_Dammage = ReadByte((IntPtr)(_GameRAM_Address + _Outputs_PlayerData_Offset + 0xBC));
                int P2_Dammage = ReadByte((IntPtr)(_GameRAM_Address + _Outputs_PlayerData_Offset + 0xBE));
                if (P1_Dammage != 0 & _P1_LastDammage == 0)
                    SetOutputValue(OutputId.P1_Damaged, 1);
                if (P2_Dammage != 0 & _P2_LastDammage == 0)
                    SetOutputValue(OutputId.P2_Damaged, 1);
                _P1_LastDammage = P1_Dammage;
                _P2_LastDammage = P2_Dammage;

                if (P1_Life > 0)
                {
                    P1_Ammo = (int)BitConverter.ToInt16(ReadBytes((IntPtr)(_GameRAM_Address + _Outputs_PlayerData_Offset + 0x62), 2), 0);

                    //Custom Recoil
                    if (P1_Ammo < _P1_LastAmmo)
                        SetOutputValue(OutputId.P1_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (P1_Ammo > 0)
                        P1_Clip = 1;
                }

                if (P2_Life > 0)
                {
                    P2_Ammo = (int)BitConverter.ToInt16(ReadBytes((IntPtr)(_GameRAM_Address + _Outputs_PlayerData_Offset + 0x64), 2), 0);

                    //Custom Recoil
                    if (P2_Ammo < _P2_LastAmmo)
                        SetOutputValue(OutputId.P2_CtmRecoil, 1);

                    //[Clip Empty] custom Output
                    if (P2_Ammo > 0)
                        P2_Clip = 1;
                }
            }

            _P1_LastAmmo = P1_Ammo;
            _P2_LastAmmo = P2_Ammo;
            _P1_LastLife = P1_Life;
            _P2_LastLife = P2_Life;

            SetOutputValue(OutputId.P1_Ammo, P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, P1_Life);
            SetOutputValue(OutputId.P2_Life, P2_Life);

            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((IntPtr)Outputs_Address) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((IntPtr)Outputs_Address) >> 4 & 0x01);
            SetOutputValue(OutputId.P1_GunRecoil, ReadByte((IntPtr)Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_GunRecoil, ReadByte((IntPtr)Outputs_Address) >> 5 & 0x01);

            //Custom recoil will be activated just like original one
            //REMOVED !! The recoil is activated when shooting offscreen !!
            /*SetOutputValue(OutputId.P1_CtmRecoil, ReadByte((IntPtr)Outputs_Address) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_CtmRecoil, ReadByte((IntPtr)Outputs_Address) >> 5 & 0x01);*/

            //Credits
            SetOutputValue(OutputId.Credits, ReadByte((IntPtr)(_GameRAM_Address + _Outputs_Credits_Offset)));
        }

        #endregion
    }
}
