using System;
using System.Collections.Generic;
using DsCore.MameOutput;

namespace DemulShooterX64
{
    public class Game_FlycastNaomi : Game_Flycast
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Game_FlycastNaomi(String RomName, bool DisableInputHack, bool Verbose) : base(RomName, DisableInputHack, Verbose)
        { }        

        #region Outputs

        /// <summary>
        /// Create the Output list that we will be looking for and forward to MameHooker
        /// </summary>
        protected override void CreateOutputList()
        {
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
            if (_RomName.Equals("confmiss"))
                Compute_Confmiss_Outputs();             
            else if (_RomName.Equals("deathcox"))
                Compute_Deathcox_Outputs();
            else if (_RomName.Equals("deathcoxo"))
                Compute_Deathcoxo_Outputs(); 
            else if (_RomName.Equals("hotd2"))
                Compute_Hotd2_Outputs(0x00096FA0);
            /*else if (_RomName.Equals("hotd2e"))       //Need to find memory values, no available on Demul
                Compute_Hotd2_Outputs(0x00096FA0);*/  
            else if (_RomName.Equals("hotd2o"))
                Compute_Hotd2_Outputs(0x00096F58);   
            else if (_RomName.Equals("hotd2p"))
                Compute_Hotd2_Outputs(0x00082D00);
            else if (_RomName.Equals("lupinsho"))   
                Compute_Lupinsho_Outputs();
            else if (_RomName.Equals("mok"))        
                Compute_Mok_Outputs();
        }

        private void Compute_Confmiss_Outputs()
        {
            //Player status :
            //[0] = Calibration/InGame
            //[1] = InGame
            //[2] = Continue
            //[4] = Game Over / Attract Mode / Menu
            UInt64 P1_Status_Address = _GameRAM_Address + (UInt64)(BitConverter.ToUInt32(ReadBytes((IntPtr)(_GameRAM_Address + 0x2FBAC), 4), 0) & 0x01FFFFFF);
            UInt64 P2_Status_Address = P1_Status_Address + 0x40;
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            UInt64 P1_Ammo_Address = _GameRAM_Address + (UInt64)(BitConverter.ToUInt32(ReadBytes((IntPtr)(_GameRAM_Address + 0x2AA50), 4), 0) & 0x01FFFFFF) - 0x14;  // = P1_Status_Address + 0xB4C ?
            UInt64 P2_Ammo_Address = _GameRAM_Address + (UInt64)(BitConverter.ToUInt32(ReadBytes((IntPtr)(_GameRAM_Address + 0x2AA50), 4), 0) & 0x01FFFFFF) + 0x100;
            UInt64 Credits_Address = _GameRAM_Address + (UInt64)(BitConverter.ToUInt32(ReadBytes((IntPtr)(_GameRAM_Address + 0x2F88C), 4), 0) & 0x01FFFFFF);

            if (ReadByte((IntPtr)P1_Status_Address) == 0 || ReadByte((IntPtr)P1_Status_Address) == 1)
            {
                _P1_Life = ReadByte((IntPtr)(P1_Status_Address + 0x14));
                _P1_Ammo = ReadByte((IntPtr)P1_Ammo_Address);

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

            if (ReadByte((IntPtr)P2_Status_Address) == 0 || ReadByte((IntPtr)P2_Status_Address) == 1)
            {
                _P2_Life = ReadByte((IntPtr)(P2_Status_Address + 0x14));
                _P2_Ammo = ReadByte((IntPtr)P2_Ammo_Address);

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

            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((IntPtr)(_GameRAM_Address + 0x000152AEA)) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((IntPtr)(_GameRAM_Address + 0x000152AEA)) >> 4 & 0x01);

            SetOutputValue(OutputId.Credits, ReadByte((IntPtr)Credits_Address));
        }

        private void Compute_Deathcox_Outputs()
        {
            //InGame Status : 0 = AttractMode/Demo/Menu, 1 = InGame
            UInt64 InGame_Address = _GameRAM_Address + 0x000982B8;
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            UInt64 P1_Ammo_Address = _GameRAM_Address + 0x0018E395;
            UInt64 P2_Ammo_Address = P1_Ammo_Address + 0x3C;
            UInt64 Credits_Address = _GameRAM_Address + 0x009917C;
            //P1 and P2 Enable : Display ammo and life when it's 0 ( not reliable but well...)
            //UInt64 P1_Enable_Address = _GameRAM_Address + 0x00096634;
            //UInt64 P2_Enable_Address = _GameRAM_Address + 0x00096638;

            //---> Ammo + 0xC => Byte going from 0 to 1 when alive...use for status ???
            UInt64 P1_Enable_Address = P1_Ammo_Address + 0xC;
            UInt64 P2_Enable_Address = P2_Ammo_Address + 0xC;

            if (ReadByte((IntPtr)P1_Enable_Address) == 1 && ReadByte((IntPtr)InGame_Address) == 1)
            {
                _P1_Life = (int)(BitConverter.ToSingle(ReadBytes((IntPtr)(Credits_Address + 0x04), 4), 0) * 100);
                _P1_Ammo = ReadByte((IntPtr)P1_Ammo_Address);

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

            if (ReadByte((IntPtr)P2_Enable_Address) == 1 && ReadByte((IntPtr)InGame_Address) == 1)
            {
                _P2_Life = (int)(BitConverter.ToSingle(ReadBytes((IntPtr)(Credits_Address + 0x08), 4), 0) * 100);
                _P2_Ammo = ReadByte((IntPtr)P2_Ammo_Address);

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

            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            //Genuine Outputs
            //Genuine Outputs memory location still need to be find on this rom
            //SetOutputValue(OutputId.P1_LmpStart, ReadByte((IntPtr)(_GameRAM_Address + 0x0001C8D16)) >> 7 & 0x01);
            //SetOutputValue(OutputId.P2_LmpStart, ReadByte((IntPtr)(_GameRAM_Address + 0x0001C8D16)) >> 4 & 0x01);
            //In the meantime, there is some values that can be used, but does not blink Lamps on the attract/menu screen
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((IntPtr)(_GameRAM_Address + 0x000098258)));
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((IntPtr)(_GameRAM_Address + 0x00009825C)));

            SetOutputValue(OutputId.Credits, ReadByte((IntPtr)Credits_Address));
        }

        private void Compute_Deathcoxo_Outputs()
        {
            //InGame Status : 0 = AttractMode/Demo/Menu, 1 = InGame
            UInt64 InGame_Address = _GameRAM_Address + 0x00096680;
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            UInt64 P1_Ammo_Address = _GameRAM_Address + 0x0018BFC9;
            UInt64 P2_Ammo_Address = P1_Ammo_Address + 0x3C;
            UInt64 Credits_Address = _GameRAM_Address + 0x00974A8;
            //P1 and P2 Enable : Display ammo and life when it's 0 ( not reliable but well...)
            //UInt64 P1_Enable_Address = _GameRAM_Address + 0x00096634;
            //UInt64 P2_Enable_Address = _GameRAM_Address + 0x00096638;

            //---> Ammo + 0xC => Byte going from 0 to 1 when alive...use for status ???
            UInt64 P1_Enable_Address = P1_Ammo_Address + 0xC;
            UInt64 P2_Enable_Address = P2_Ammo_Address + 0xC;

            if (ReadByte((IntPtr)P1_Enable_Address) == 1 && ReadByte((IntPtr)InGame_Address) == 1)
            {
                _P1_Life = (int)(BitConverter.ToSingle(ReadBytes((IntPtr)(Credits_Address + 0x04), 4), 0) * 100);
                _P1_Ammo = ReadByte((IntPtr)P1_Ammo_Address);

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

            if (ReadByte((IntPtr)P2_Enable_Address) == 1 && ReadByte((IntPtr)InGame_Address) == 1)
            {
                _P2_Life = (int)(BitConverter.ToSingle(ReadBytes((IntPtr)(Credits_Address + 0x08), 4), 0) * 100);
                _P2_Ammo = ReadByte((IntPtr)P2_Ammo_Address);

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

            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((IntPtr)(_GameRAM_Address + 0x0001C8D16)) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((IntPtr)(_GameRAM_Address + 0x0001C8D16)) >> 4 & 0x01);
            SetOutputValue(OutputId.Credits, ReadByte((IntPtr)Credits_Address));
        }

        private void Compute_Hotd2_Outputs(UInt32 DataPtr)
        {
            //Player status :
            //[4] = Continue Screen
            //[5] = InGame
            //[6] = Game Over
            //[9] = Menu or Attract Mode             
            UInt64 P1_Status_Address = _GameRAM_Address + (UInt64)(BitConverter.ToUInt32(ReadBytes((IntPtr)(_GameRAM_Address + DataPtr), 4), 0) & 0x01FFFFFF) + 0x04;
            UInt64 P2_Status_Address = P1_Status_Address + 0x100;

            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            if (ReadByte((IntPtr)P1_Status_Address) == 5)
            {
                _P1_Life = ReadByte((IntPtr)(P1_Status_Address + 0x0C));
                _P1_Ammo = ReadByte((IntPtr)(P1_Status_Address + 0x20));

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

            if (ReadByte((IntPtr)P2_Status_Address) == 5)
            {
                _P2_Life = ReadByte((IntPtr)(P2_Status_Address + 0x0C));
                _P2_Ammo = ReadByte((IntPtr)(P2_Status_Address + 0x20));

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

            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((IntPtr)(P1_Status_Address + 0xFB)) >> 6 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((IntPtr)(P2_Status_Address + 0xFB)) >> 6 & 0x01);
            SetOutputValue(OutputId.Credits, ReadByte((IntPtr)(P1_Status_Address + 0x75C))); 
        }

        private void Compute_Lupinsho_Outputs()
        {
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            //Game Status (float) :
            //0 : Title Screen
            //1 : Gameplay
            //2 : Demo play
            float GameStatus = BitConverter.ToSingle(ReadBytes((IntPtr)(_GameRAM_Address + 0x00A4C780), 4), 0);

            if (GameStatus == 1.0f)
            {
                //Check if P1 and P2 are active to display their information
                float P1_Active = BitConverter.ToSingle(ReadBytes((IntPtr)(_GameRAM_Address + 0x00A4C760), 4), 0);
                float P2_Active = BitConverter.ToSingle(ReadBytes((IntPtr)(_GameRAM_Address + 0x00A4C764), 4), 0);

                if (P1_Active == 1.0f)
                {
                    _P1_Life = (int)(BitConverter.ToSingle(ReadBytes((IntPtr)(_GameRAM_Address + 0x00A4C8B0), 4), 0));
                    _P1_Ammo = (int)(BitConverter.ToSingle(ReadBytes((IntPtr)(_GameRAM_Address + 0x00A4C830), 4), 0));

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

                if (P2_Active == 1.0f)
                {
                    _P2_Life = (int)(BitConverter.ToSingle(ReadBytes((IntPtr)(_GameRAM_Address + 0x00A4C8B4), 4), 0));
                    _P2_Ammo = (int)(BitConverter.ToSingle(ReadBytes((IntPtr)(_GameRAM_Address + 0x00A4C834), 4), 0));

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
            _P2_LastAmmo = _P2_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((IntPtr)(_GameRAM_Address + 0x00B77C16)) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((IntPtr)(_GameRAM_Address + 0x00B77C16)) >> 4 & 0x01);
            SetOutputValue(OutputId.Credits, ReadByte((IntPtr)(_GameRAM_Address + 0x00B69FF8)));
        }

        private void Compute_Mok_Outputs()
        {
            //Player status :
            UInt64 P1_Data_Address = _GameRAM_Address + (UInt64)(BitConverter.ToUInt32(ReadBytes((IntPtr)(_GameRAM_Address + 0x00023464), 4), 0) & 0x01FFFFFF);
            UInt64 P2_Data_Address = P1_Data_Address + 0x64;
            _P1_Life = 0;
            _P2_Life = 0;
            _P1_Ammo = 0;
            _P2_Ammo = 0;
            int P1_Clip = 0;
            int P2_Clip = 0;

            //Player Status :
            //1 : Title Screen
            //2,3,4 : Demo
            //5 : Attract Mode
            //6 : Game Over
            //7 : Playing (cut scene)
            //8 : Playing
            //9 : continue Screen
            Byte P1_Status = ReadByte((IntPtr)(P1_Data_Address + 0x48));
            Byte P2_Status = ReadByte((IntPtr)(P2_Data_Address + 0x48));
            if (P1_Status == 8 || P1_Status == 7)
            {
                _P1_Life = ReadByte((IntPtr)(P1_Data_Address + 0x5C));
                _P1_Ammo = ReadByte((IntPtr)(P1_Data_Address + 0x58));

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
            if (P2_Status == 8 || P2_Status == 7)
            {
                _P2_Life = ReadByte((IntPtr)(P2_Data_Address + 0x5C));
                _P2_Ammo = ReadByte((IntPtr)(P2_Data_Address + 0x58));

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

            _P1_LastAmmo = _P1_Ammo;
            _P2_LastAmmo = _P2_Ammo;
            _P1_LastLife = _P1_Life;
            _P2_LastLife = _P2_Life;

            SetOutputValue(OutputId.P1_Ammo, _P1_Ammo);
            SetOutputValue(OutputId.P2_Ammo, _P2_Ammo);
            SetOutputValue(OutputId.P1_Clip, P1_Clip);
            SetOutputValue(OutputId.P2_Clip, P2_Clip);
            SetOutputValue(OutputId.P1_Life, _P1_Life);
            SetOutputValue(OutputId.P2_Life, _P2_Life);

            //Genuine Outputs
            SetOutputValue(OutputId.P1_LmpStart, ReadByte((IntPtr)(_GameRAM_Address + 0x000EC396)) >> 7 & 0x01);
            SetOutputValue(OutputId.P2_LmpStart, ReadByte((IntPtr)(_GameRAM_Address + 0x000EC396)) >> 4 & 0x01);
            SetOutputValue(OutputId.Credits, ReadByte((IntPtr)(P1_Data_Address + 0x7C)));
        }

        #endregion
    }
}
