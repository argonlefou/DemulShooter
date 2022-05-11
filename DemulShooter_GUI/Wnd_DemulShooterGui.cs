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