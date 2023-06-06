using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;
using DsCore;
using DsCore.Config;
using DsCore.RawInput;
using DsCore.Win32;

namespace DemulShooter_GUI
{    
    public partial class Wnd_DemulShooterGui : Form
    {
        private static Wnd_DemulShooterGui _This;
        private Configurator _Configurator;
        private const string CONF_FILENAME = "config.ini";

        //Non-GUI settings (W.I.P -- they should be incliuded next)
        private int _HookTimeout = 0;

        //Available RawInput devices (filters by thir Type)
        private RawInputController[] _AvailableControllers;

        //Low-Level Hooks
        private Win32API.HookProc _KeyboardHookProc;
        private IntPtr _KeyboardHookID = IntPtr.Zero;        
      
        /*** XInput Controllers data ***/
        /*private const String XINPUT_DEVICE_PREFIX = "XInput Gamepad #";
        private XInputState[] _XInputStates;        
        private int _XInput_PollingInterval = 20; //ms*/
       
        /**** Directinput data ***/        
        private bool _Start_KeyRecord = false;
        private TextBox _SelectedTextBox;
        private String _SelectedTextBoxTextBackup = String.Empty;

        /*** Controllers ***/
        private List<GUI_Player> _GUI_Players;
        private List<GUI_AnalogCalibration> _GUI_AnalogCalibrations;

        /// <summary>
        /// Construcor
        /// </summary>
        public Wnd_DemulShooterGui(bool IsVerbose)
        {           
            InitializeComponent();
            Logger.IsEnabled = IsVerbose;

            _This = this;
            this.Text = "DemulShooter_GUI " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString();

            Logger.WriteLog("");
            Logger.WriteLog("---------------- Program Start -- DemulShooter_GUI v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString() + " ----------------");

            //Finding plugged devices
            _AvailableControllers = RawInputHelper.GetRawInputDevices(new RawInputDeviceType[] { RawInputDeviceType.RIM_TYPEHID, RawInputDeviceType.RIM_TYPEMOUSE });
            Logger.WriteLog("Found " + _AvailableControllers.Length + " available RawInput devices :");
            foreach (RawInputController c in _AvailableControllers)
            {
                try
                {
                    Logger.WriteLog(" + [" + c.DeviceType.ToString() + "] " + c.DeviceName);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog("Wnd_DemulShooterGui() ERROR : " + ex.Message.ToString());
                }
            }            

            //Reading config file to get parameters
            _Configurator = new Configurator();
            _Configurator.ReadDsConfig(AppDomain.CurrentDomain.BaseDirectory + CONF_FILENAME);
            _Configurator.Read_Sha_Conf();

            Logger.WriteLog("Initializing GUI [Players] pages...");
            int TabPageIndex = 0;
            _GUI_Players = new List<GUI_Player>();
            foreach (PlayerSettings PlayerData in _Configurator.PlayersSettings)
            {
                if (PlayerData.Mode == PlayerSettings.PLAYER_MODE_RAWINPUT)
                {
                    //For each player, create a tab on the GUI
                    GUI_Player gPlayer = new GUI_Player(PlayerData, _AvailableControllers);
                    Logger.WriteLog("Adding PlayerData to the list...");
                    _GUI_Players.Add(gPlayer);
                    Logger.WriteLog("Adding Player tab to the GUI...");
                    tabControl1.TabPages[TabPageIndex].Controls.Add(gPlayer);
                    TabPageIndex++;
                }
                else
                {
                    //Logger.WriteLog("P" + Player.ID + " Gamepad ID = " + Player.GamepadID);
                }
            }

            //Fill Analog calibration Tab
            Logger.WriteLog("Initializing GUI [Analog calibration] page...");
            _GUI_AnalogCalibrations = new List<GUI_AnalogCalibration>();
            for (int i = 1; i <= 4; i++)
            {
                GUI_AnalogCalibration Calib = new GUI_AnalogCalibration(i, _Configurator.GetPlayerSettings(i));
                TableLayout_Calib.Controls.Add(Calib);
                _GUI_AnalogCalibrations.Add(Calib);
            }            

            //Fill ActLabs tab
            Logger.WriteLog("Initializing GUI [Act Lab] page...");
            Cb_ActLabsOffset.Checked = _Configurator.Act_Labs_Offset_Enable;
            Chk_DspCorrectedCrosshair.Checked = _Configurator.Act_Labs_Display_Crosshair;
            Txt_ActLabs_X1.Text = _Configurator.GetPlayerSettings(1).Act_Labs_Offset_X.ToString();
            Txt_ActLabs_Y1.Text = _Configurator.GetPlayerSettings(1).Act_Labs_Offset_Y.ToString();
            Txt_ActLabs_X2.Text = _Configurator.GetPlayerSettings(2).Act_Labs_Offset_X.ToString();
            Txt_ActLabs_Y2.Text = _Configurator.GetPlayerSettings(2).Act_Labs_Offset_Y.ToString();
            Txt_ActLabs_X3.Text = _Configurator.GetPlayerSettings(3).Act_Labs_Offset_X.ToString();
            Txt_ActLabs_Y3.Text = _Configurator.GetPlayerSettings(3).Act_Labs_Offset_Y.ToString();
            Txt_ActLabs_X4.Text = _Configurator.GetPlayerSettings(4).Act_Labs_Offset_X.ToString();
            Txt_ActLabs_Y4.Text = _Configurator.GetPlayerSettings(4).Act_Labs_Offset_Y.ToString();            

            //Fill Model2 Tab
            TXT_CH_P1.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_M2_Crosshair_P1);
            TXT_CH_P2.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_M2_Crosshair_P2);
            TXT_CH_VIS.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_M2_Crosshair_Visibility);

            //Fill Silent Hill tab
            Logger.WriteLog("Initializing GUI [Silent Hill] pages...");
            TXT_P1_S.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Sha_P1_Start);
            TXT_P1_T.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Sha_P1_Trigger);
            TXT_P2_S.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Sha_P2_Start);
            TXT_P2_T.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Sha_P2_Trigger);
            TXT_EXIT.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Sha_Exit);
            TXT_SERVICE.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Sha_Service);
            TXT_TEST.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Sha_Test);

            //Fill Elevator Action Invasion tab
            Txt_EAI_P1_Start.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Eai_P1_Start);
            Txt_EAI_P2_Start.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Eai_P2_Start);
            Txt_EAI_P1_Credits.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Eai_P1_Credits);
            Txt_EAI_P2_Credits.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Eai_P2_Credits);
            Txt_EAI_Settings.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Eai_Settings);
            Txt_EAI_Up.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Eai_MenuUp);
            Txt_EAI_Down.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Eai_MenuDown);
            Txt_EAI_Enter.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Eai_MenuEnter);

            //Fill Gundam tab
            Logger.WriteLog("Initializing GUI [Gundam] pages...");
            Chk_GundamP1Pedal.Checked = _Configurator.Gsoz_Pedal_P1_Enabled;
            Chk_GundamP2Pedal.Checked = _Configurator.Gsoz_Pedal_P2_Enabled;
            TXT_GSOZ_PEDAL_1.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Gsoz_Pedal_P1);
            TXT_GSOZ_PEDAL_2.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Gsoz_Pedal_P2);

            //Fill Heavy Fire Afghanistan tab
            Logger.WriteLog("Initializing GUI [Heavy Fire Afghanistan] pages...");
            Txt_HF3_Browse.Text = _Configurator.HF3_Path;
            TrackBar_HF3_Cover.Value = _Configurator.HF3_CoverSensibility;

            //Fill Heavy Fire Shattered Spear tab
            Logger.WriteLog("Initializing GUI [Heavy Fire S.S] pages...");
            Txt_HF4_Browse.Text = _Configurator.HF4_Path;
            TrackBar_HF4_Cover.Value = _Configurator.HF4_CoverSensibility;

            //Fill Lethal Enforcers 3 tab
            Logger.WriteLog("Initializing GUI [Lethal Enforcers 3] pages...");
            Chk_Le3_EnablePedal1.Checked = _Configurator.Le3_Pedal_P1_Enabled;
            Chk_Le3_EnablePedal2.Checked = _Configurator.Le3_Pedal_P2_Enabled;
            TXT_LE3_PEDAL_1.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Le3_Pedal_P1);
            TXT_LE3_PEDAL_2.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Le3_Pedal_P2);            

            //Fill Operation GHOST Tab
            Logger.WriteLog("Initializing GUI [Operation G.H.O.S.T] pages...");
            if (_Configurator.OpGhost_EnableFreeplay)
                Cbox_OpGhost_Freeplay.SelectedIndex = 1;
            else
                Cbox_OpGhost_Freeplay.SelectedIndex = 0;
            Cbox_OpGhost_CreditsByCoin.SelectedIndex = _Configurator.OpGhost_CoinsPerCredits - 1;
            Cbox_OpGhost_CreditsToStart.SelectedIndex = _Configurator.OpGhost_CreditsToStart - 1;
            Cbox_OpGhost_CreditsToContinue.SelectedIndex = _Configurator.OpGhost_CreditsToContinue - 1;
            Chk_OpGhost_SeparateButton.Checked = _Configurator.OpGhost_SeparateButtons;
            TXT_OPGHOST_ACTION_P1.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_OpGhost_Action_P1);
            TXT_OPGHOST_ACTION_P2.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_OpGhost_Action_P2);

            //Fill Rabbids Hollywood tab
            Logger.WriteLog("Initializing GUI [Rabbids Hollywood] pages...");
            Txt_Rha_GamePath.Text = _Configurator.Rha_Path;

            //Fill RPCS3 Tab
            Logger.WriteLog("Initializing GUI [RPCS3] pages...");
            Txt_Rpcs3_P1_Start.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Rpcs3_P1_Start);
            Txt_Rpcs3_P2_Start.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Rpcs3_P2_Start);
            Txt_Rpcs3_Service.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Rpcs3_Service);
            Txt_Rpcs3_Up.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Rpcs3_Up);
            Txt_Rpcs3_Down.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Rpcs3_Down);
            Txt_Rpcs3_Enter.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Rpcs3_Enter);
            Txt_Rpcs3_3D_Switch.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Rpcs3_3D_Switch);

            //Fill Wild West Shoutout tab
            Logger.WriteLog("Initializing GUI [Wild West Shoutout] pages...");
            Txt_Wws_GamePath.Text = _Configurator.Wws_Path;
            Txt_Wws_P1Coin.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Wws_P1Coin);
            Txt_Wws_P2Coin.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Wws_P2Coin);
            Txt_Wws_Test.Text = GetKeyStringFromScanCode((int)_Configurator.DIK_Wws_Test);

            //Fill Output Tab
            Logger.WriteLog("Initializing GUI [Output] pages...");
            Cbox_Outputs.Checked = _Configurator.OutputEnabled;
            Txt_OutputDelay.Text = _Configurator.OutputPollingDelay.ToString();
            Txt_OutputRecoilOn.Text = _Configurator.OutputCustomRecoilOnDelay.ToString();
            Txt_OutputRecoilOff.Text = _Configurator.OutputCustomRecoilOffDelay.ToString();
            Txt_OutputDamaged.Text = _Configurator.OutputCustomDamagedDelay.ToString();           

            //Non-GUI settings
            _HookTimeout = _Configurator.HookTimeout;

            // Register to rawinput
            Logger.WriteLog("Registering to RawInput service...");
            RawInputDevice[] rid = new RawInputDevice[3];
            rid[0].UsagePage = HidUsagePage.GENERIC;
            rid[0].Usage = HidUsage.Joystick;
            rid[0].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
            rid[0].hwndTarget = this.Handle;

            rid[1].UsagePage = HidUsagePage.GENERIC;
            rid[1].Usage = HidUsage.Mouse;
            rid[1].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
            rid[1].hwndTarget = this.Handle;

            rid[2].UsagePage = HidUsagePage.GENERIC;
            rid[2].Usage = HidUsage.Gamepad;
            rid[2].dwFlags = RawInputDeviceFlags.RIDEV_INPUTSINK;
            rid[2].hwndTarget = this.Handle;
            if (!Win32API.RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                MessageBox.Show("Failed to register raw input device(s).", "DemulShooter Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            //Install Low Level keyboard hook
            Logger.WriteLog("Installing Low-Level Keyboard Hook...");
            ApplyKeyboardHook();                               
            
            Cbo_PageSettings.SelectedIndex = 0;
        }
               

        #region RAW_INPUT

        /// <summary>
        /// Get movements/click and process to Demul memory or GUI when visible
        /// </summary>
        private void ProcessRawInputMessage(IntPtr RawInputHandle)
        {
            foreach (RawInputController Controller in _AvailableControllers)
            {
                if (Controller.isSourceOfRawInputMessage(RawInputHandle))
                {
                    foreach (PlayerSettings Player in _Configurator.PlayersSettings)
                    {
                        if (Player.DeviceName == Controller.DeviceName)
                        {
                            Controller.ProcessRawInputData(RawInputHandle);
                            _GUI_Players[Player.ID - 1].UpdateGui();
                            _GUI_AnalogCalibrations[Player.ID - 1].UpdateValues();                           
                        }  
                    }
                }
            }
        }
        
        #endregion           
         
        /// <summary>
        /// Page Selection
        /// </summary>
        private void Cbo_PageSettings_SelectionChangeCommitted(object sender, EventArgs e)
        {
            tabControl1.SelectTab(Cbo_PageSettings.SelectedIndex);
        }

        #region Analog Calibration

        private void UpdateCalibration(int Player, int CurrentX, int CurrentY)
        {
            if (CurrentX > _Configurator.PlayersSettings[Player - 1].AnalogManual_Xmax)
                _Configurator.PlayersSettings[Player - 1].AnalogManual_Xmax = CurrentX;
            if (CurrentX < _Configurator.PlayersSettings[Player - 1].AnalogManual_Xmin)
                _Configurator.PlayersSettings[Player - 1].AnalogManual_Xmin = CurrentX;
            if (CurrentY > _Configurator.PlayersSettings[Player - 1].AnalogManual_Ymax)
                _Configurator.PlayersSettings[Player - 1].AnalogManual_Ymax = CurrentY;
            if (CurrentY < _Configurator.PlayersSettings[Player - 1].AnalogManual_Ymin)
                _Configurator.PlayersSettings[Player - 1].AnalogManual_Ymin = CurrentY;

        }

        private void Btn_SaveAnalog_Click(object sender, EventArgs e)
        {            
            if (_Configurator.WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show("Configuration saved !");
            else
                MessageBox.Show("Impossible to save DemulShooter config file.", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        #region ActLabs Offset tab

        private void Cb_ActLabsOffset_CheckedChanged(object sender, EventArgs e)
        {
            if (Cb_ActLabsOffset.Checked)
            {
                Txt_ActLabs_X1.Enabled = true;
                Txt_ActLabs_Y1.Enabled = true;
                Txt_ActLabs_X2.Enabled = true;
                Txt_ActLabs_Y2.Enabled = true;
                Txt_ActLabs_X3.Enabled = true;
                Txt_ActLabs_Y3.Enabled = true;
                Txt_ActLabs_X4.Enabled = true;
                Txt_ActLabs_Y4.Enabled = true;
                _Configurator.Act_Labs_Offset_Enable = true;
            }
            else
            {
                Txt_ActLabs_X1.Enabled = false;
                Txt_ActLabs_Y1.Enabled = false;
                Txt_ActLabs_X2.Enabled = false;
                Txt_ActLabs_Y2.Enabled = false;
                Txt_ActLabs_X3.Enabled = false;
                Txt_ActLabs_Y3.Enabled = false;
                Txt_ActLabs_X4.Enabled = false;
                Txt_ActLabs_Y4.Enabled = false;
                _Configurator.Act_Labs_Offset_Enable = false;
            }
        }


        private void Chk_DspCorrectedCrosshair_CheckedChanged(object sender, EventArgs e)
        {
            _Configurator.Act_Labs_Display_Crosshair = Chk_DspCorrectedCrosshair.Checked;
        }

        private void Txt_ActLabs_X1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _Configurator.GetPlayerSettings(1).Act_Labs_Offset_X = Convert.ToInt32(Txt_ActLabs_X1.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_X1.Text + " is not a valid X offset value. Please enter a non-decimal number");
                Txt_ActLabs_X1.Text = _Configurator.GetPlayerSettings(1).Act_Labs_Offset_X.ToString();
            }
        }
        private void Txt_ActLabs_Y1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _Configurator.GetPlayerSettings(1).Act_Labs_Offset_Y = Convert.ToInt32(Txt_ActLabs_Y1.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_Y1.Text + " is not a valid Y offset value. Please enter a non-decimal number");
                Txt_ActLabs_Y1.Text = _Configurator.GetPlayerSettings(1).Act_Labs_Offset_Y.ToString();
            }
        }
        private void Txt_ActLabs_X2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _Configurator.PlayersSettings[1].Act_Labs_Offset_X = Convert.ToInt32(Txt_ActLabs_X2.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_X2.Text + " is not a valid X offset value. Please enter a non-decimal number");
                Txt_ActLabs_X2.Text = _Configurator.PlayersSettings[1].Act_Labs_Offset_X.ToString();
            }
        }
        private void Txt_ActLabs_Y2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _Configurator.PlayersSettings[1].Act_Labs_Offset_Y = Convert.ToInt32(Txt_ActLabs_Y2.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_Y2.Text + " is not a valid Y offset value. Please enter a non-decimal number");
                Txt_ActLabs_Y2.Text = _Configurator.PlayersSettings[1].Act_Labs_Offset_Y.ToString();
            }
        }
        private void Txt_ActLabs_X3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _Configurator.PlayersSettings[2].Act_Labs_Offset_X = Convert.ToInt32(Txt_ActLabs_X3.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_X3.Text + " is not a valid X offset value. Please enter a non-decimal number");
                Txt_ActLabs_X3.Text = _Configurator.PlayersSettings[2].Act_Labs_Offset_X.ToString();
            }
        }
        private void Txt_ActLabs_Y3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _Configurator.PlayersSettings[2].Act_Labs_Offset_Y = Convert.ToInt32(Txt_ActLabs_Y3.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_Y3.Text + " is not a valid Y offset value. Please enter a non-decimal number");
                Txt_ActLabs_Y3.Text = _Configurator.PlayersSettings[2].Act_Labs_Offset_Y.ToString();
            }
        }
        private void Txt_ActLabs_X4_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _Configurator.PlayersSettings[3].Act_Labs_Offset_X = Convert.ToInt32(Txt_ActLabs_X4.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_X4.Text + " is not a valid X offset value. Please enter a non-decimal number");
                Txt_ActLabs_X4.Text = _Configurator.PlayersSettings[3].Act_Labs_Offset_X.ToString();
            }
        }
        private void Txt_ActLabs_Y4_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _Configurator.PlayersSettings[3].Act_Labs_Offset_Y = Convert.ToInt32(Txt_ActLabs_Y4.Text);
            }
            catch
            {
                MessageBox.Show(Txt_ActLabs_Y4.Text + " is not a valid Y offset value. Please enter a non-decimal number");
                Txt_ActLabs_Y4.Text = _Configurator.PlayersSettings[3].Act_Labs_Offset_Y.ToString();
            }
        }         
        private void Btn_ActLabs_Save_Click(object sender, EventArgs e)
        {
            if (_Configurator.WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show("Configuration saved !");
            else
                MessageBox.Show("Impossible to save DemulShooter config file.", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        #region Modify/Create SHA key mapping

        private void Save_Sha_Keys_Click(object sender, EventArgs e)
        {
            if (_Configurator.Write_Sha_Config())
                MessageBox.Show("Key mapping saved !");
            else
                MessageBox.Show("Impossible to save SHA config file.", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        
        #endregion

        #region Gundam:SoZ Tab

        private void Chk_GundamP1Pedal_CheckedChanged(object sender, EventArgs e)
        {
            _Configurator.Gsoz_Pedal_P1_Enabled = Chk_GundamP1Pedal.Checked;
            if (_Configurator.Gsoz_Pedal_P1_Enabled)
                TXT_GSOZ_PEDAL_1.Enabled = true;
            else
                TXT_GSOZ_PEDAL_1.Enabled = false;
        }
        private void Chk_GundamP2Pedal_CheckedChanged(object sender, EventArgs e)
        {
            _Configurator.Gsoz_Pedal_P2_Enabled = Chk_GundamP2Pedal.Checked;
            if (_Configurator.Gsoz_Pedal_P2_Enabled)
                TXT_GSOZ_PEDAL_2.Enabled = true;
            else
                TXT_GSOZ_PEDAL_2.Enabled = false;
        }
        private void Btn_Save_Gsoz_Click(object sender, EventArgs e)
        {
            if (_Configurator.WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show("Configuration saved !");
            else
                MessageBox.Show("Impossible to save DemulShooter config file.", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion
        
        #region Dolphin Wiimote configuration

        private void Btn_Dolphin5_Click(object sender, EventArgs e)
        {
            InstallWiimoteconfigFile(DemulShooter_GUI.Properties.Resources.WiimoteNew_v5);
        }
        private void InstallWiimoteconfigFile(String ResourceFile)
        {
            String Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Dolphin Emulator\Config";
            bool IsAimtrak = false;
            bool Overwritten = false;
            try
            {
                if (File.Exists(Path + @"\WiimoteNew.ini"))
                {
                    File.Copy(Path + @"\WiimoteNew.ini", Path + @"\WiimoteNew.bak.ini", true);
                    Overwritten = true;
                }
                //Copying file
                using (StreamWriter sw = new StreamWriter(Path + @"\WiimoteNew.ini", false))
                {
                    sw.Write(ResourceFile);
                    sw.Close();
                }
                //Modifying Devices Names according to config.ini
                int P2_Atrak_ID = 0;
                try
                {
                    String VID = (_Configurator.PlayersSettings[0].DeviceName.ToUpper().Split(new string[] { "VID_" }, System.StringSplitOptions.RemoveEmptyEntries))[1].Substring(0, 4);
                    String PID = (_Configurator.PlayersSettings[1].DeviceName.ToUpper().Split(new string[] { "PID_" }, System.StringSplitOptions.RemoveEmptyEntries))[1].Substring(0, 4);
                    if (VID.Equals("D209") && PID.StartsWith("16"))
                    {
                        IsAimtrak = true;
                        P2_Atrak_ID = int.Parse(PID.Substring(2, 2));
                        int bID = 0x30 + P2_Atrak_ID;
                        using (BinaryWriter fileWriter = new BinaryWriter(File.Open(Path + @"\WiimoteNew.ini", System.IO.FileMode.Open)))
                        {
                            fileWriter.BaseStream.Position = 0x2E7; // set the offset
                            fileWriter.Write((byte)bID);
                            fileWriter.BaseStream.Position = 0x315; // set the offset
                            fileWriter.Write((byte)bID);
                            fileWriter.BaseStream.Position = 0x343; // set the offset
                            fileWriter.Write((byte)bID);
                            fileWriter.BaseStream.Position = 0x372; // set the offset
                            fileWriter.Write((byte)bID);
                        }                        
                    }
                }
                catch 
                {
                    //String m = "P2 Wiimote axis device could not be modified, default is :\n \"DInput/0/ATRAK Device #2\"";
                    //MessageBox.Show(m, "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }                

                //Dialog Message
                String message = "File \"" + Path + "\\WiimoteNew.ini\" successfully written !";
                if (Overwritten)
                    message += "\n\nThe existing WiimoteNew.ini was backed-up to \"WiimoteNew.bak.ini\"\n\n";
                MessageBox.Show(message, "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                if (IsAimtrak)
                {
                    message = "Aimtrak detected for Player2 device, Aimtrak ID = " + P2_Atrak_ID;
                    message += "\n\nAccording to DemulShooter config, P2 Wiimote axis device modified to : \n\"DInput/0/ATRAK Device #" + P2_Atrak_ID + "\"";
                    MessageBox.Show(message, "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Information);                
                }
                else
                {
                    message = "No Aimtrak detected for Player2.";
                    message += "\n\nP2 Wiimote axis device will keep default value :\n \"DInput/0/ATRAK Device #2\"";
                    MessageBox.Show(message, "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Warning);                
                }                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't install WiimoteNew.ini : \n\n" + ex.Message.ToString(), "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Elevator Action Invasion

        private string _EAI_ExeFilename = "ESGame-Win64-Shipping.exe";
        private string _EAI_UsbDllFilename = "UsbPluginsDll.dll";

        private void Btn_EAI_Open_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select your " + _EAI_ExeFilename + " location :";
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (fbd.SelectedPath != string.Empty)
                    {
                        if (File.Exists(fbd.SelectedPath + "\\" + _EAI_ExeFilename))
                        {
                            Txt_EAI_FolderPath.Text = fbd.SelectedPath;
                            Btn_EAI_Patch.Enabled = true;
                        }
                        else
                        {
                            MessageBox.Show(_EAI_ExeFilename + " not found in following folder : " + fbd.SelectedPath, this.Text,  MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void Btn_EAI_Patch_Click(object sender, EventArgs e)
        {
            string ExeFilePath = Txt_EAI_FolderPath.Text + "\\" + _EAI_ExeFilename;
            string UsbDllFilePath = Txt_EAI_FolderPath.Text + "\\..\\..\\Plugins\\" + _EAI_UsbDllFilename;

            //Main exe patching
            try
            {
                using (BinaryWriter bw = new BinaryWriter(File.Open(ExeFilePath, FileMode.Open, FileAccess.ReadWrite)))
                {
                    //Force overriding read data from the COM port by our own
                    //Creating codecave
                    bw.BaseStream.Seek(0x292C9D4, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0x4C, 0x8D, 0x35, 0x95, 0x44, 0x16, 0x01, 0x41, 0x0F, 0xB6, 0x9E, 0xCE, 0x02, 0x00, 0x00, 0xE9, 0xF6, 0x50, 0xE6, 0xFD });
                    //Injection
                    bw.BaseStream.Seek(0x791AD6, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xE9, 0xF9, 0xAE, 0x19, 0x02, 0x90, 0x90, 0x90 });


                    //For Credits we will force the game to read our own value
                    //Codecave space too short at the end of the code page, so we will use some 21-byte long Align space between 2 functions
                    bw.BaseStream.Seek(0x24B12EB, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0x48, 0x8D, 0x05, 0x6A, 0xFB, 0x5D, 0x01, 0x48, 0xC1, 0xE1, 0x04, 0x48, 0x01, 0xC8, 0x8B, 0x00, 0xE9, 0xAC, 0x0A, 0x2E, 0xFE });
                    //Injection
                    bw.BaseStream.Seek(0x791DA7, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xE9, 0x3F, 0xF5, 0xD1, 0x01 });

                    //Force overriding bad read values from GUN api with good flag, and force values to be read from another memory location
                    //which then will be written by any tool to feed data
                    //Creating codecave
                    bw.BaseStream.Seek(0x292C97F, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xCC, 0xCC, 0xCC, 0x50, 0x53, 0x48, 0xC1, 0xE3, 0x04, 0x48, 0x31, 0xC0, 0x42, 0xC7, 0x44, 0x39, 0x34, 0x01, 0x00, 0x00, 0x00, 0x48, 0x8D, 0x05, 0xB5, 0x44, 0x16, 0x01, 0x48, 0x01, 0xD8, 0x8B, 0x00, 0x42, 0x89, 0x44, 0x39, 0x14, 0x48, 0x8D, 0x05, 0xA8, 0x44, 0x16, 0x01, 0x48, 0x01, 0xD8, 0x8B, 0x00, 0x42, 0x89, 0x44, 0x39, 0x18, 0x48, 0x8D, 0x05, 0x9B, 0x44, 0x16, 0x01, 0x48, 0x01, 0xD8, 0x8B, 0x00, 0x42, 0x89, 0x44, 0x39, 0x51, 0x5B, 0x58, 0x42, 0x83, 0x7C, 0x39, 0x34, 0x00, 0xE9, 0xD9, 0x60, 0xE8, 0xFD });
                    //Injection
                    bw.BaseStream.Seek(0x7B2AA7, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xE9, 0xD6, 0x9E, 0x17, 0x02, 0x90 });

                    //Force return OK to IsGunCalibrated call
                    bw.BaseStream.Seek(0x772D17, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xB0, 0x01 });
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Error patching " + ExeFilePath + " : \n" + Ex.Message.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            //USB dll patching
            try
            {
                using (BinaryWriter bw = new BinaryWriter(File.Open(UsbDllFilePath, FileMode.Open, FileAccess.ReadWrite)))
                {

                    //Force return true for BIND_InitDongle()
                    bw.BaseStream.Seek(0x134CB, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0x90, 0x90, });
                    //Force return true for BIND_IsDongleVerify()
                    bw.BaseStream.Seek(0x136F7, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xB0, 0x01 });

                    //Force return true to BIND_OpenESIODevice without opening COM port at all
                    bw.BaseStream.Seek(0x137B0, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 });

                    //Force return true for BIND_IsExistESIODevice()                    
                    bw.BaseStream.Seek(0xA5A0, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0x90, 0x90 });

                    //Force return true for BIND_GetESIoData()
                    //That way the game will constantly call the ABPGameState::PArseIoData where we will inject our values
                    bw.BaseStream.Seek(0x12F10, SeekOrigin.Begin);
                    bw.Write(new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 });
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Error patching " + UsbDllFilePath + " : \n" + Ex.Message.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            MessageBox.Show("Patch Complete !", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Btn_EAI_Save_Click(object sender, EventArgs e)
        {
            if (_Configurator.WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show("Configuration saved !");
            else
                MessageBox.Show("Impossible to save DemulShooter config file.", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        #region Model2 Emulator tab

        private void Btn_M2Scripts_Click(object sender, EventArgs e)
        {
            InstallM2Scripts();
        }
        private void InstallM2Scripts()
        {
            if (folderBrowserDialog1.ShowDialog() != DialogResult.OK)
                return;
            try
            {
                StreamWriter streamWriter = new StreamWriter(folderBrowserDialog1.SelectedPath + "\\scripts\\demulshooter.lua", false);
                streamWriter.WriteLine("-- This file is automatically generated by Demulshooter\n");
                streamWriter.WriteLine("P1_ChangeCrosshairKey=0x" + ((byte)_Configurator.DIK_M2_Crosshair_P1).ToString("X2"));
                streamWriter.WriteLine("P2_ChangeCrosshairKey=0x" + ((byte)_Configurator.DIK_M2_Crosshair_P2).ToString("X2"));
                streamWriter.WriteLine("CrosshairVisibilityKey=0x" + ((byte)_Configurator.DIK_M2_Crosshair_Visibility).ToString("X2"));
                streamWriter.WriteLine();
                streamWriter.WriteLine("function lines_from(file)");
                streamWriter.WriteLine("\tlines = {}");
                streamWriter.WriteLine("\tfor line in io.lines(file) do");
                streamWriter.WriteLine("\t\tlines[#lines + 1] = line");
                streamWriter.WriteLine("\tend");
                streamWriter.WriteLine("\treturn lines");
                streamWriter.WriteLine("end");
                streamWriter.Close();
                Directory.CreateDirectory(folderBrowserDialog1.SelectedPath + "\\artwork\\crosshairs");
                foreach (FileInfo file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\m2emulator\\artwork\\crosshairs").GetFiles())
                    file.CopyTo(folderBrowserDialog1.SelectedPath + "\\artwork\\crosshairs\\" + file.Name, true);

                String FlashState = "flash";
                if (Cbox_M2_Flash.Checked)
                    FlashState = "noflash";
                foreach (FileInfo file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\m2emulator\\scripts\\" + FlashState).GetFiles())
                    file.CopyTo(folderBrowserDialog1.SelectedPath + "\\scripts\\" + file.Name, true);
                MessageBox.Show("Scripts installed !");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't install m2emulator lua scripts : \n\n" + ex.Message.ToString(), "", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        #endregion

        #region Heavy Fire Afghanistan tab

        private void Btn_HF3_Browse_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "Please select \"Heavy Fire Afghanistan\" installation folder";
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _Configurator.HF3_Path = folderBrowserDialog1.SelectedPath;
                Txt_HF3_Browse.Text = _Configurator.HF3_Path;
            }
        }
        private void Txt_HF3_Browse_TextChanged(object sender, EventArgs e)
        {
            _Configurator.HF3_Path = Txt_HF3_Browse.Text;
            if (Txt_HF3_Browse.Text.Length > 0)
            {
                Lbl_HFA_Version.Text = "Game version : ";
                Lbl_HFA_Command.Text = "DemulShooter parameter : ";
                Lbl_HFA_Version.Visible = true;
                Lbl_HFA_Command.Visible = true;

                //Getting game information
                string sMD5 = string.Empty;
                if (File.Exists(_Configurator.HF3_Path + @"\HeavyFire3_Final.exe"))
                {
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = File.OpenRead(_Configurator.HF3_Path + @"\HeavyFire3_Final.exe"))
                        {
                            //Getting md5 calculation of destination file
                            sMD5 = BitConverter.ToString(md5.ComputeHash(stream));
                            if (sMD5.Equals("18-41-E5-71-28-6A-CB-17-A9-85-46-A4-39-C0-8E-57"))
                            {
                                Lbl_HFA_Version.Text += "Steam release";
                                Lbl_HFA_Command.Text += "-rom=hfa or -rom=hfa2p";
                            }
                            else
                            {
                                Lbl_HFA_Version.Text += "Unknown version";
                                Lbl_HFA_Command.Visible = false;
                            }
                        }
                    }                      
                }
                else if (File.Exists(_Configurator.HF3_Path + @"\HeavyFire3.exe"))
                {
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = File.OpenRead(_Configurator.HF3_Path + @"\HeavyFire3.exe"))
                        {
                            sMD5 = BitConverter.ToString(md5.ComputeHash(stream));
                            if (sMD5.Equals("3F-49-95-1A-E8-23-28-17-A9-1E-F5-50-33-74-D6-B3"))
                            {
                                Lbl_HFA_Version.Text += "SKIDROW release";
                                Lbl_HFA_Command.Text += "-rom=hfa or -rom=hfa2p";
                            }
                            else
                            {
                                Lbl_HFA_Version.Text += "Unknown version";
                                Lbl_HFA_Command.Visible = false;
                            }
                        }
                    }
                }                
                else
                {
                    Lbl_HFA_Version.Text = "No known executable name found ! ";
                    Lbl_HFA_Command.Visible = false;
                }                
            }
            else
            {
                Lbl_HFA_Version.Visible = false;
                Lbl_HFA_Command.Visible = false;
            }
        }
        private void Btn_HF3_Install_Click(object sender, EventArgs e)
        {
            InstallHeavyFireDll(_Configurator.HF3_Path);                  
        }
        private void InstallHeavyFireDll(string Path)
        {
            if (Path != string.Empty)
            {
                //Installing dinput8.dll DirectInput blocker  
                try
                {
                    File.WriteAllBytes(Path + @"\dinput8.dll", DemulShooter_GUI.Properties.Resources.dinput8_blocker);
                    MessageBox.Show(Path + @"\dinput8.dll succesfully installed", "DirectInput Blocker installation", MessageBoxButtons.OK, MessageBoxIcon.Information);                                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error installing " + Path + "\\dinput8.dll : \n\n" + ex.Message.ToString(), "DirectInput Blocker installation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                //Installing xinput1_3.dll
                try
                {
                    File.WriteAllBytes(Path + @"\xinput1_3.dll", DemulShooter_GUI.Properties.Resources.xinput1_3);
                    MessageBox.Show(Path + @"\xinput1_3.dll succesfully installed", "Xinput simulator installation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error installing " + Path + "\\xinput1_3.dll : \n\n" + ex.Message.ToString(), "Xinput simulator installation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }                        
            }
        }
        private void TrackBar_HF3_Cover_ValueChanged(object sender, EventArgs e)
        {
            _Configurator.HF4_CoverSensibility = TrackBar_HF3_Cover.Value;
        }
        private void Btn_HF3_Save_Click(object sender, EventArgs e)
        {
            if (_Configurator.WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show("Configuration saved !");
            else
                MessageBox.Show("Impossible to save DemulShooter config file.", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        #region Heavy Fire Shattered Spear tab

        private void Btn_HF4_Browse_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "Please select \"Heavy Fire Shattered Spear\" installation folder";
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _Configurator.HF4_Path = folderBrowserDialog1.SelectedPath;
                Txt_HF4_Browse.Text = _Configurator.HF4_Path;
            }
        }
        private void Txt_HF4_Browse_TextChanged(object sender, EventArgs e)
        {
            _Configurator.HF4_Path = Txt_HF4_Browse.Text;
            if (Txt_HF4_Browse.Text.Length > 0)
            {
                Lbl_HF4_Version.Text = "Game version : ";
                Lbl_HF4_Command.Text = "DemulShooter parameter : ";
                Lbl_HF4_Version.Visible = true;
                Lbl_HF4_Command.Visible = true;

                //Getting game information
                string sMD5 = string.Empty;
                if (File.Exists(_Configurator.HF4_Path + @"\hf4.exe"))
                {
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = File.OpenRead(_Configurator.HF4_Path + @"\hf4.exe"))
                        {
                            //Getting md5 calculation of destination file
                            sMD5 = BitConverter.ToString(md5.ComputeHash(stream));
                            if (sMD5.Equals("7F-8B-F2-0A-AB-A8-0A-C1-23-9E-FC-55-3D-94-A5-3F"))
                            {
                                Lbl_HF4_Version.Text += "Steam release";
                                Lbl_HF4_Command.Text += "-rom=hfss or -rom=hfss2p";
                            }
                            else
                            {
                                Lbl_HF4_Version.Text += "Unknown version";
                                Lbl_HF4_Command.Visible = false;
                            }
                        }
                    }
                }
                else if (File.Exists(_Configurator.HF4_Path + @"\HeavyFire4.exe"))
                {
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = File.OpenRead(_Configurator.HF4_Path + @"\HeavyFire4.exe"))
                        {
                            sMD5 = BitConverter.ToString(md5.ComputeHash(stream));
                            if (sMD5.Equals("94-76-F9-BB-A4-8A-EA-6C-A0-4D-06-15-8B-E0-7F-1C"))
                            {
                                Lbl_HF4_Version.Text += "SKIDROW release";
                                Lbl_HF4_Command.Text += "-rom=hfss or -rom=hfss2p";
                            }
                            else
                            {
                                Lbl_HF4_Version.Text += "Unknown version";
                                Lbl_HF4_Command.Visible = false;
                            }
                        }
                    }
                }
                else
                {
                    Lbl_HF4_Version.Text = "No known executable name found ! ";
                    Lbl_HF4_Command.Visible = false;
                }
            }
            else
            {
                Lbl_HF4_Version.Visible = false;
                Lbl_HF4_Command.Visible = false;
            }
        }
        private void Btn_HF4_Install_Click(object sender, EventArgs e)
        {
            InstallHeavyFireDll(_Configurator.HF4_Path);

            //Getting game information
            string sMD5 = string.Empty;
            if (File.Exists(_Configurator.HF4_Path + @"\hf4.exe"))
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(_Configurator.HF4_Path + @"\hf4.exe"))
                    {
                        //Getting md5 calculation of destination file
                        sMD5 = BitConverter.ToString(md5.ComputeHash(stream));
                        if (sMD5.Equals("7F-8B-F2-0A-AB-A8-0A-C1-23-9E-FC-55-3D-94-A5-3F"))
                            MessageBox.Show("Steam version of game found.\n\nUse -rom=hfss or -rom=hfss2p for DemulShooter", "Game exe informations", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Unknown version of the game, this may not work with DemulShooter", "Game exe informations", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            else if (File.Exists(_Configurator.HF4_Path + @"\HeavyFire4.exe"))
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(_Configurator.HF4_Path + @"\HeavyFire4.exe"))
                    {
                        sMD5 = BitConverter.ToString(md5.ComputeHash(stream));
                        if (sMD5.Equals("94-76-F9-BB-A4-8A-EA-6C-A0-4D-06-15-8B-E0-7F-1C"))
                            MessageBox.Show("SKIDROW version of game found.\n\nUse -rom=hfss or -rom=hfss2p for DemulShooter", "Game exe informations", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("Unknown version of the game, this may not work with DemulShooter", "Game exe informations", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            else
            {
                MessageBox.Show("Can't find any known game executable. Please verify your path", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }    
        }
        private void TrackBar_HF4_Cover_ValueChanged(object sender, EventArgs e)
        {
            _Configurator.HF4_CoverSensibility = TrackBar_HF4_Cover.Value;
        }
        private void Btn_HF4_Save_Click(object sender, EventArgs e)
        {
            if (_Configurator.WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show("Configuration saved !");
            else
                MessageBox.Show("Impossible to save DemulShooter config file.", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion                  
        
        #region Lethal Enforcer 3 tab

        private void Chk_Le3_EnablePedal1_CheckedChanged(object sender, EventArgs e)
        {
            _Configurator.Le3_Pedal_P1_Enabled = Chk_Le3_EnablePedal1.Checked;
            if (_Configurator.Le3_Pedal_P1_Enabled)
                TXT_LE3_PEDAL_1.Enabled = true;
            else
                TXT_LE3_PEDAL_1.Enabled = false;
        }

        private void Chk_Le3_EnablePedal2_CheckedChanged(object sender, EventArgs e)
        {
            _Configurator.Le3_Pedal_P2_Enabled = Chk_Le3_EnablePedal2.Checked;
            if (_Configurator.Le3_Pedal_P2_Enabled)
                TXT_LE3_PEDAL_2.Enabled = true;
            else
                TXT_LE3_PEDAL_2.Enabled = false;
        }

        private void Btn_Save_Le3_Click(object sender, EventArgs e)
        {
            if (_Configurator.WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show("Configuration saved !");
            else
                MessageBox.Show("Impossible to save DemulShooter config file.", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        #region Operation Ghost Tab

        private void Cbox_OpGhost_Freeplay_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Cbox_OpGhost_Freeplay.SelectedIndex == 0)
                _Configurator.OpGhost_EnableFreeplay = false;
            else
                _Configurator.OpGhost_EnableFreeplay = true;
        }

        private void Cbox_OpGhost_CreditsByCoin_SelectedIndexChanged(object sender, EventArgs e)
        {
            _Configurator.OpGhost_CoinsPerCredits = Cbox_OpGhost_CreditsByCoin.SelectedIndex + 1;
        }

        private void Cbox_OpGhost_CreditsToStart_SelectedIndexChanged(object sender, EventArgs e)
        {
            _Configurator.OpGhost_CreditsToStart = Cbox_OpGhost_CreditsToStart.SelectedIndex + 1;
        }

        private void Cbox_OpGhost_CreditsToContinue_SelectedIndexChanged(object sender, EventArgs e)
        {
            _Configurator.OpGhost_CreditsToContinue = Cbox_OpGhost_CreditsToContinue.SelectedIndex + 1;
        } 

        private void Chk_OpGhost_SeparateButton_CheckedChanged(object sender, EventArgs e)
        {
            _Configurator.OpGhost_SeparateButtons = Chk_OpGhost_SeparateButton.Checked;
            if (_Configurator.OpGhost_SeparateButtons)
            {
                TXT_OPGHOST_ACTION_P1.Enabled = true;
                TXT_OPGHOST_ACTION_P2.Enabled = true;
            }
            else
            {
                TXT_OPGHOST_ACTION_P1.Enabled = false;
                TXT_OPGHOST_ACTION_P2.Enabled = false;
            }
        }

        private void Btn_Save_OpGhost_Click(object sender, EventArgs e)
        {
            if (_Configurator.WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show("Configuration saved !");
            else
                MessageBox.Show("Impossible to save DemulShooter config file.", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        #region Wild West Shoutoout tab

        private void Btn_Wws_GamePath_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "Please select \"CowBoy.exe\" installation folder";
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _Configurator.Wws_Path = folderBrowserDialog1.SelectedPath;
                Txt_Wws_GamePath.Text = _Configurator.Wws_Path;
            }
        }

        private void Btn_Wws_InstallUnity_Click(object sender, EventArgs e)
        {
            //foreach (FileInfo file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\Unity\\WildWestShoutout").GetFiles())
            //    file.CopyTo(folderBrowserDialog1.SelectedPath + "\\artwork\\crosshairs\\" + file.Name, true);
            if ( !CloneDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\Unity\\WildWestShoutout", Txt_Wws_GamePath.Text))
                MessageBox.Show("Impossible to copy Unity plugin in the followinf folder :\n" + Txt_Wws_GamePath.Text, "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                MessageBox.Show("Unity plugin installed !");

            if (_Configurator.WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show("Configuration saved !");
            else
                MessageBox.Show("Impossible to save DemulShooter config file.", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);            
        }

        private void Btn_Wws_SaveKeys_Click(object sender, EventArgs e)
        {
            if (_Configurator.WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show("Configuration saved !");
            else
                MessageBox.Show("Impossible to save DemulShooter config file.", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);            
        }

        #endregion

        #region Rabbids Hollywood Tab

        private void Btn_Rha_GamePath_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.Description = "Please select \"Game.exe\" installation folder";
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _Configurator.Rha_Path = folderBrowserDialog1.SelectedPath;
                Txt_Rha_GamePath.Text = _Configurator.Rha_Path;
            }
        }

        private void Btn_Rha_InstallUnity_Click(object sender, EventArgs e)
        {
            if (!CloneDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\Unity\\RabbidsHollywood", Txt_Rha_GamePath.Text))
                MessageBox.Show("Impossible to copy Unity plugin in the followinf folder :\n" + Txt_Rha_GamePath.Text, "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                MessageBox.Show("Unity plugin installed !");

            if (_Configurator.WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show("Configuration saved !");
            else
                MessageBox.Show("Impossible to save DemulShooter config file.", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }        

        #endregion

        #region RPCS3 Tab

        private void Btn_Rpcs3_DeadStorm_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select \"DeadStorm Pirates\" rpcs3-gun.exe folder :";
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && fbd.SelectedPath != string.Empty)
                {
                    string CacheFolder = fbd.SelectedPath + @"\cache\SCEEXE000\ppu-obiMX8TqMzUsChXLV1Ln5TAJegSZ-EBOOT.BIN\";
                    if (!Directory.Exists(CacheFolder))
                    {
                        MessageBox.Show("Directory not found :\n " + CacheFolder + "\n\nPlease run the game once before patching the PPU-Cache", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    foreach (FileInfo file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\RPCS3\\DeadStorm").GetFiles())
                    {
                        Patch_RPCS3_PPU_CacheFile(CacheFolder, file.Name, file);
                    }
                }
            }
        }

        private void Btn_Rpcs3_DarkEscape_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select \"Dark Escape\" rpcs3-gun.exe folder :";
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && fbd.SelectedPath != string.Empty)
                {
                    string CacheFolder = fbd.SelectedPath + @"\cache\SCEEXE000\ppu-gfm17oJj1cUecjZQ8dVv46oQv2iW-EBOOT.BIN\";
                    if (!Directory.Exists(CacheFolder))
                    {
                        MessageBox.Show("Directory not found :\n " + CacheFolder + "\n\nPlease run the game once before patching the PPU-Cache", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    foreach (FileInfo file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\RPCS3\\DarkEscape").GetFiles())
                    {
                        Patch_RPCS3_PPU_CacheFile(CacheFolder, file.Name, file);
                    }
                }
            }
        }

        private void Btn_Rpcs3_SailorZombie_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select \"Sailor Zombie\" rpcs3-gun.exe folder :";
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && fbd.SelectedPath != string.Empty)
                {
                    string CacheFolder = fbd.SelectedPath + @"\cache\SCEEXE000\ppu-se1PtZVS5iF9A6M5Y1vu0wNqiASu-EBOOT.BIN\";
                    if (!Directory.Exists(CacheFolder))
                    {
                        MessageBox.Show("Directory not found :\n " + CacheFolder + "\n\nPlease run the game once before patching the PPU-Cache", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    foreach (FileInfo file in new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\RPCS3\\SailorZombie").GetFiles())
                    {
                        Patch_RPCS3_PPU_CacheFile(CacheFolder, file.Name, file);
                    }
                }
            }
        }

        private void Btn_Rpcs3_RazingStorm_Click(object sender, EventArgs e)
        {

        }

        private void Txt_Rpcs3_Save_Click(object sender, EventArgs e)
        {
            if (_Configurator.WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show("Configuration saved !");
            else
                MessageBox.Show("Impossible to save DemulShooter config file.", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        
        private void Patch_RPCS3_PPU_CacheFile(string CacheFolder, string CacheFileName, FileInfo SourceFile)
        {
            string CPU_Type = string.Empty;
            foreach (String sCacheFile in Directory.GetFiles(CacheFolder))
            {
                String sCacheFileNameShort = Path.GetFileName(sCacheFile);

                if (sCacheFileNameShort.StartsWith(CacheFileName))
                {
                    try
                    {
                        SourceFile.CopyTo(sCacheFile, true);
                        MessageBox.Show("Successfully replaced PPU-Cache file : " + sCacheFile, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error writing PPU-Cache file :" + sCacheFile + "\n\n" + ex.Message.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }                    
                    return;
                }
            }
            MessageBox.Show("PPU-Cache file not found :\n " + CacheFileName + "\n\nPlease run the game once before patching the PPU-Cache", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        #endregion

        #region Outputs tab

        private void Cbox_Outputs_CheckedChanged(object sender, EventArgs e)
        {
            Txt_OutputDelay.Enabled = Cbox_Outputs.Checked;
            Txt_OutputRecoilOn.Enabled = Cbox_Outputs.Checked;
            Txt_OutputRecoilOff.Enabled = Cbox_Outputs.Checked;
            Txt_OutputDamaged.Enabled = Cbox_Outputs.Checked;
            _Configurator.OutputEnabled = Cbox_Outputs.Checked;
        }

        private void Txt_OutputDelay_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _Configurator.OutputPollingDelay = Convert.ToInt32(Txt_OutputDelay.Text);
            }
            catch
            {
                MessageBox.Show(Txt_OutputDelay.Text + " is not a valid delay. Please enter a non-decimal number");
                Txt_OutputDelay.Text = _Configurator.OutputPollingDelay.ToString();
            }
        }

        private void Txt_OutputRecoilOn_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _Configurator.OutputCustomRecoilOnDelay = Convert.ToInt32(Txt_OutputRecoilOn.Text);
            }
            catch
            {
                MessageBox.Show(Txt_OutputRecoilOn.Text + " is not a valid delay. Please enter a non-decimal number");
                Txt_OutputRecoilOn.Text = _Configurator.OutputCustomRecoilOnDelay.ToString();
            }
        }

        private void Txt_OutputRecoilOff_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _Configurator.OutputCustomRecoilOffDelay = Convert.ToInt32(Txt_OutputRecoilOff.Text);
            }
            catch
            {
                MessageBox.Show(Txt_OutputRecoilOff.Text + " is not a valid delay. Please enter a non-decimal number");
                Txt_OutputRecoilOff.Text = _Configurator.OutputCustomRecoilOffDelay.ToString();
            }
        }

        private void Txt_OutputDamaged_TextChanged(object sender, EventArgs e)
        {
            try
            {
                _Configurator.OutputCustomDamagedDelay = Convert.ToInt32(Txt_OutputDamaged.Text);
            }
            catch
            {
                MessageBox.Show(Txt_OutputDamaged.Text + " is not a valid delay. Please enter a non-decimal number");
                Txt_OutputDamaged.Text = _Configurator.OutputCustomDamagedDelay.ToString();
            }
        }

        private void Btn_Save_Cfg_Click(object sender, EventArgs e)
        {
            if (_Configurator.WriteConf(AppDomain.CurrentDomain.BaseDirectory + @"\" + CONF_FILENAME))
                MessageBox.Show("Configuration saved !");
            else
                MessageBox.Show("Impossible to save DemulShooter config file.", "DemulShooter", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        #region Keyboard Handling

        /// <summary>
        /// Low-level Keyboard hook.
        /// </summary>
        protected void ApplyKeyboardHook()
        {
            _KeyboardHookProc = new Win32API.HookProc(GuiKeyboardHookCallback);
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
                _KeyboardHookID = Win32API.SetWindowsHookEx(Win32Define.WH_KEYBOARD_LL, _KeyboardHookProc, Win32API.GetModuleHandle(curModule.ModuleName), 0);
            if (_KeyboardHookID == IntPtr.Zero)
            {
                MessageBox.Show("Failed to register LowLevel Keyboard Hook.", "DemulShooter Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Keyboard hook for the GUI part, to detect buttons for config
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private IntPtr GuiKeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if ((UInt32)wParam == Win32Define.WM_KEYDOWN)
                {
                    if (_This._Start_KeyRecord)
                    {
                        KBDLLHOOKSTRUCT s = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
                        //_SelectedTextBox.Text = s.scanCode.ToString();
                        _SelectedTextBox.Text = GetKeyStringFromVkCode(s.vkCode);

                        if (_SelectedTextBox == TXT_P1_S)
                            _Configurator.DIK_Sha_P1_Start = s.scanCode;
                        else if (_SelectedTextBox == TXT_P1_T)
                            _Configurator.DIK_Sha_P1_Trigger = s.scanCode;
                        else if (_SelectedTextBox == TXT_P2_S)
                            _Configurator.DIK_Sha_P2_Start = s.scanCode;
                        else if (_SelectedTextBox == TXT_P2_T)
                            _Configurator.DIK_Sha_P2_Trigger = s.scanCode;
                        else if (_SelectedTextBox == TXT_EXIT)
                            _Configurator.DIK_Sha_Exit = s.scanCode;
                        else if (_SelectedTextBox == TXT_TEST)
                            _Configurator.DIK_Sha_Test = s.scanCode;
                        else if (_SelectedTextBox == TXT_SERVICE)
                            _Configurator.DIK_Sha_Service = s.scanCode;
                        else if (_SelectedTextBox == TXT_CH_P1)
                            _Configurator.DIK_M2_Crosshair_P1 = s.scanCode;
                        else if (_SelectedTextBox == TXT_CH_P2)
                            _Configurator.DIK_M2_Crosshair_P2 = s.scanCode;
                        else if (_SelectedTextBox == TXT_CH_VIS)
                            _Configurator.DIK_M2_Crosshair_Visibility = s.scanCode;
                        else if (_SelectedTextBox == TXT_GSOZ_PEDAL_1)
                            _Configurator.DIK_Gsoz_Pedal_P1 = s.scanCode;
                        else if (_SelectedTextBox == TXT_GSOZ_PEDAL_2)
                            _Configurator.DIK_Gsoz_Pedal_P2 = s.scanCode;
                        else if (_SelectedTextBox == Txt_Wws_P1Coin)
                            _Configurator.DIK_Wws_P1Coin = s.scanCode;
                        else if (_SelectedTextBox == Txt_Wws_P2Coin)
                            _Configurator.DIK_Wws_P2Coin = s.scanCode;
                        else if (_SelectedTextBox == Txt_Wws_Test)
                            _Configurator.DIK_Wws_Test = s.scanCode;
                        else if (_SelectedTextBox == Txt_Rpcs3_P1_Start)
                            _Configurator.DIK_Rpcs3_P1_Start = s.scanCode;
                        else if (_SelectedTextBox == Txt_Rpcs3_P2_Start)
                            _Configurator.DIK_Rpcs3_P2_Start = s.scanCode;
                        else if (_SelectedTextBox == Txt_Rpcs3_Service)
                            _Configurator.DIK_Rpcs3_Service = s.scanCode;
                        else if (_SelectedTextBox == Txt_Rpcs3_Up)
                            _Configurator.DIK_Rpcs3_Up = s.scanCode;
                        else if (_SelectedTextBox == Txt_Rpcs3_Down)
                            _Configurator.DIK_Rpcs3_Down = s.scanCode;
                        else if (_SelectedTextBox == Txt_Rpcs3_Enter)
                            _Configurator.DIK_Rpcs3_Enter = s.scanCode;
                        else if (_SelectedTextBox == Txt_Rpcs3_3D_Switch)
                            _Configurator.DIK_Rpcs3_3D_Switch = s.scanCode;
                        else if (_SelectedTextBox == TXT_LE3_PEDAL_1)
                            _Configurator.DIK_Le3_Pedal_P1 = s.scanCode;
                        else if (_SelectedTextBox == TXT_LE3_PEDAL_2)
                            _Configurator.DIK_Le3_Pedal_P2 = s.scanCode;
                        else if (_SelectedTextBox == TXT_OPGHOST_ACTION_P1)
                            _Configurator.DIK_OpGhost_Action_P1 = s.scanCode;
                        else if (_SelectedTextBox == TXT_OPGHOST_ACTION_P2)
                            _Configurator.DIK_OpGhost_Action_P2 = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_P1_Start)
                            _Configurator.DIK_Eai_P1_Start = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_P2_Start)
                            _Configurator.DIK_Eai_P2_Start = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_P1_Credits)
                            _Configurator.DIK_Eai_P1_Credits = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_P2_Credits)
                            _Configurator.DIK_Eai_P2_Credits = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_Settings)
                            _Configurator.DIK_Eai_Settings = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_Up)
                            _Configurator.DIK_Eai_MenuUp = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_Down)
                            _Configurator.DIK_Eai_MenuDown = s.scanCode;
                        else if (_SelectedTextBox == Txt_EAI_Enter)
                            _Configurator.DIK_Eai_MenuEnter = s.scanCode;
                        
                        _SelectedTextBox = null;
                        _Start_KeyRecord = false;

                        return new IntPtr(1);
                    }
                }
            }
            return Win32API.CallNextHookEx(_This._KeyboardHookID, nCode, wParam, lParam);
        }

        private String GetKeyStringFromScanCode(int ScanCode)
        {
            uint Vk = Win32API.MapVirtualKey((uint)ScanCode, VirtualKeyMapType.MAPVK_VSC_TO_VK);
            return GetKeyStringFromVkCode((int)Vk);
        }

        private String GetKeyStringFromVkCode(int vkCode)
        {
            KeysConverter kc = new KeysConverter();
            return kc.ConvertToString((Keys)vkCode);
        }

        /// <summary>
        /// Enable a Textbox to be filled with a DIK key code
        /// </summary>
        public void TXT_DirectInput_MouseClick(object sender, MouseEventArgs e)
        {
            if (_SelectedTextBox != null && _SelectedTextBox != ((TextBox)sender))
            {
                _SelectedTextBox.Text = _SelectedTextBoxTextBackup;
            }
            _SelectedTextBox = ((TextBox)sender);
            _SelectedTextBoxTextBackup = _SelectedTextBox.Text;
            _SelectedTextBox.Text = "";
            _Start_KeyRecord = true;
        }

        #endregion

        #region WINDOW MESSAGE LOOP

        /*************** WM Boucle *********************/
        protected const int WM_INPUT = 0x00FF;
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_INPUT:
                    //read in new mouse values.
                    ProcessRawInputMessage(m.LParam);
                    break;
            }
            base.WndProc(ref m);
        }

        #endregion  

        private void Tab_AnalogCalib_Click(object sender, EventArgs e)
        {

        }

        private bool CloneDirectory(string root, string dest)
        {
            try
            {

                foreach (var directory in Directory.GetDirectories(root))
                {
                    string dirName = Path.GetFileName(directory);
                    if (!Directory.Exists(Path.Combine(dest, dirName)))
                    {
                        Directory.CreateDirectory(Path.Combine(dest, dirName));
                    }
                    CloneDirectory(directory, Path.Combine(dest, dirName));
                }

                foreach (var file in Directory.GetFiles(root))
                {
                    File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
    }        
}