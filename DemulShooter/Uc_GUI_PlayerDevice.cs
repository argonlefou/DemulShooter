using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace DemulShooter
{
    public partial class Uc_GUI_PlayerDevice : UserControl
    {
        private const String XINPUT_DEVICE_PREFIX = "XInput Gamepad #";
        private ControllerDevice _PlayerDevice;
        private WndParam _WndParam;
        
        public Uc_GUI_PlayerDevice(ControllerDevice Device, WndParam MainWindow)
        {
            InitializeComponent();
            _PlayerDevice = Device;
            _WndParam = MainWindow;

            Lbl_Player.Text = "P" + _PlayerDevice.Player.ToString() + " Device :";

            //GUI init
            AddDevice("");

            //Gamepad buttons
            SetGamepadKey(Cbox_Pad_MouseLeft, _PlayerDevice.Gamepad_LeftClick);
            SetGamepadKey(Cbox_Pad_MouseMiddle, _PlayerDevice.Gamepad_MiddleClick);
            SetGamepadKey(Cbox_Pad_MouseRight, _PlayerDevice.Gamepad_RightClick);

            //Analog stick
            if (_PlayerDevice.Gamepad_Stick.Equals("L"))
                SetGamepadAxis("Left Stick");
            else
                SetGamepadAxis("Right Stick");

            //Vibrations
            if (_PlayerDevice.Gamepad_VibrationEnabled == 1)
                SetVibrationEnabled(true);
            else
                SetVibrationEnabled(false);
            SetVibrationLength(_PlayerDevice.Gamepad_VibrationLength);
            SetVibrationStrength(_PlayerDevice.Gamepad_VibrationStrength);

        }

        /// <summary>
        /// Add a device in the drop down list
        /// </summary>
        /// <param name="DeviceName">Name of the device to display</param>
        public void AddDevice(string DeviceName)
        {
            Cbo_Device.Items.Add(DeviceName);
        }

        /// <summary>
        /// Display the analog stick choosen to be used for axis with the gamepad
        /// </summary>
        /// <param name="Axis">Should be "Left Stick" or "Right Stick"</param>
        public void SetGamepadAxis(string Axis)
        {
            Cbox_Pad_Axis.Text = Axis;
        }

        /// <summary>
        /// Display whethet the vibration are enabled or not for the gamepad
        /// </summary>
        /// <param name="Enabled">True or False</param>
        public void SetVibrationEnabled(bool Enabled)
        {
            Chk_VibrationEnable.Checked = Enabled;
        }

        /// <summary>
        /// Display vibration length
        /// </summary>
        /// <param name="Length">50 - 200 (milliseconds)</param>
        public void SetVibrationLength(int Length)
        {
            Tbar_VibrationLength.Value = Length;
        }

        /// <summary>
        /// Display vibration strength
        /// </summary>
        /// <param name="Strength">0 - 65535</param>
        public void SetVibrationStrength(int Strength)
        {
            Tbar_VibrationStrength.Value = Strength;
        }


        /// <summary>
        /// Return int value of Button flag in structure to store it in config
        /// And use it during filtering input messages
        /// </summary>
        private int GetGamepadKey(ComboBox cbo)
        {
            switch (cbo.Text)
            {
                case "A":
                    return (int)ButtonFlags.XINPUT_GAMEPAD_A;
                case "B":
                    return (int)ButtonFlags.XINPUT_GAMEPAD_B;
                case "X":
                    return (int)ButtonFlags.XINPUT_GAMEPAD_X;
                case "Y":
                    return (int)ButtonFlags.XINPUT_GAMEPAD_Y;
                case "L Shoulder":
                    return (int)ButtonFlags.XINPUT_GAMEPAD_LEFT_SHOULDER;
                case "R Shoulder":
                    return (int)ButtonFlags.XINPUT_GAMEPAD_RIGHT_SHOULDER;
                case "L Thumb":
                    return (int)ButtonFlags.XINPUT_GAMEPAD_LEFT_THUMB;
                case "R Thumb":
                    return (int)ButtonFlags.XINPUT_GAMEPAD_RIGHT_THUMB;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Show correct string value according to Key value loaded in config file
        /// </summary>
        private void SetGamepadKey(ComboBox cbo, int Key)
        {
            try
            {
                switch (Key)
                {
                    case (int)ButtonFlags.XINPUT_GAMEPAD_A:
                        cbo.Text = "A"; break;
                    case (int)ButtonFlags.XINPUT_GAMEPAD_B:
                        cbo.Text = "B"; break;
                    case (int)ButtonFlags.XINPUT_GAMEPAD_X:
                        cbo.Text = "X"; break;
                    case (int)ButtonFlags.XINPUT_GAMEPAD_Y:
                        cbo.Text = "Y"; break;
                    case (int)ButtonFlags.XINPUT_GAMEPAD_LEFT_SHOULDER:
                        cbo.Text = "L Shoulder"; break;
                    case (int)ButtonFlags.XINPUT_GAMEPAD_RIGHT_SHOULDER:
                        cbo.Text = "R Shoulder"; break;
                    case (int)ButtonFlags.XINPUT_GAMEPAD_LEFT_THUMB:
                        cbo.Text = "L Thumb"; break;
                    case (int)ButtonFlags.XINPUT_GAMEPAD_RIGHT_THUMB:
                        cbo.Text = "R Thumb"; break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }

        /// <summary>
        /// Setting default values, this will be of use if no .INI file is found at launch
        /// </summary>
        public void RefreshSettings()
        {
            Cbo_Device.Text = _PlayerDevice.DeviceName;
            if (Cbo_Device.Text.Length > 0)
            {
                if (_PlayerDevice.DeviceName.StartsWith(XINPUT_DEVICE_PREFIX))
                {
                   Gbox_PadOptions.Enabled = true;
                }
                else
                {
                    Gbox_GunOptions.Enabled = true;                    
                }
            }
            SetGamepadKey(Cbox_Pad_MouseLeft, _PlayerDevice.Gamepad_LeftClick);
            SetGamepadKey(Cbox_Pad_MouseMiddle, _PlayerDevice.Gamepad_MiddleClick);
            SetGamepadKey(Cbox_Pad_MouseRight, _PlayerDevice.Gamepad_RightClick);

            if (_PlayerDevice.Gamepad_Stick.Equals("L"))
                Cbox_Pad_Axis.Text = "Left Stick";
            else if (_PlayerDevice.Gamepad_Stick.Equals("R"))
                Cbox_Pad_Axis.Text = "Right Stick";

            if (_PlayerDevice.Gamepad_VibrationEnabled != 0)
                    Chk_VibrationEnable.Checked = true;
                else
                    Chk_VibrationEnable.Checked = false;
            Tbar_VibrationLength.Value = _PlayerDevice.Gamepad_VibrationLength;
            Tbar_VibrationStrength.Value = _PlayerDevice.Gamepad_VibrationStrength;


            if (_PlayerDevice.EnableVirtualMiddleClick != 0)
            {
                Chk_VirtualMiddleBtn.Checked = true;
                Txt_VirtualMiddleBtn.Enabled = true;
            }

            Txt_VirtualMiddleBtn.Text = GetKeyStringFromScanCode(_PlayerDevice.DiK_VirtualMiddleButton);
        }

        private String GetKeyStringFromScanCode(int ScanCode)
        {
            uint Vk = Win32.MapVirtualKey((uint)ScanCode, Win32.MAPVK_VSC_TO_VK);
            return GetKeyStringFromVkCode((int)Vk);
        }
        private String GetKeyStringFromVkCode(int vkCode)
        {
            KeysConverter kc = new KeysConverter();
            return kc.ConvertToString((Keys)vkCode);
        }

        #region Accessors

        public TextBox VirtualMiddleButton
        {
            get { return Txt_VirtualMiddleBtn; }
        }

        public ComboBox Combobox_Pad_MouseLeft
        {
            get { return Cbox_Pad_MouseLeft; }
        }
        public ComboBox Combobox_Pad_MouseMiddle
        {
            get { return Cbox_Pad_MouseMiddle; }
        }
        public ComboBox Combobox_Pad_MouseRight
        {
            get { return Cbox_Pad_MouseRight; }
        }

        #endregion

        #region GUI

        private void Cbo_Device_SelectionChangeCommitted(object sender, EventArgs e)
        {
            _PlayerDevice.DeviceName = Cbo_Device.Text;            
            if (Cbo_Device.Text.Length > 0)
            {
                if (Cbo_Device.Text.StartsWith(XINPUT_DEVICE_PREFIX))
                {
                    Gbox_GunOptions.Enabled = false;
                    Gbox_PadOptions.Enabled = true;
                }
                else
                {
                    Gbox_PadOptions.Enabled = false;
                    Gbox_GunOptions.Enabled = true;
                }
            }
            else
            {
                Gbox_GunOptions.Enabled = false;
                Gbox_GunOptions.Enabled = false;
            }
        }

        private void Cbox_Pad_MouseLeft_SelectionChangeCommitted(object sender, EventArgs e)
        {
            _PlayerDevice.Gamepad_LeftClick = GetGamepadKey(Cbox_Pad_MouseLeft);
        }

        private void Cbox_Pad_MouseMiddle_SelectionChangeCommitted(object sender, EventArgs e)
        {
            _PlayerDevice.Gamepad_MiddleClick = GetGamepadKey(Cbox_Pad_MouseMiddle);
        }

        private void Cbox_Pad_MouseRight_SelectionChangeCommitted(object sender, EventArgs e)
        {
            _PlayerDevice.Gamepad_RightClick = GetGamepadKey(Cbox_Pad_MouseRight);
        }

        private void Cbox_Pad_Axis_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (Cbox_Pad_Axis.Text.Equals("Left Stick"))
                _PlayerDevice.Gamepad_Stick = "L";
            else if (Cbox_Pad_Axis.Text.Equals("Right Stick"))
                _PlayerDevice.Gamepad_Stick = "R";
        }

        private void Chk_VibrationEnable_CheckedChanged(object sender, EventArgs e)
        {
            if (Chk_VibrationEnable.Checked)
                _PlayerDevice.Gamepad_VibrationEnabled = 1;
            else
                _PlayerDevice.Gamepad_VibrationEnabled = 0;
        }

        private void Tbar_VibrationLength_ValueChanged(object sender, EventArgs e)
        {
            _PlayerDevice.Gamepad_VibrationLength = Tbar_VibrationLength.Value;
        }

        private void Tbar_VibrationStrength_ValueChanged(object sender, EventArgs e)
        {
            _PlayerDevice.Gamepad_VibrationStrength = Tbar_VibrationStrength.Value;
        }

        private void Chk_VirtualMiddleBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (Chk_VirtualMiddleBtn.Checked)
            {
                _PlayerDevice.EnableVirtualMiddleClick = 1;
                Txt_VirtualMiddleBtn.Enabled = true;
            }
            else
            {
                _PlayerDevice.EnableVirtualMiddleClick = 0;
                Txt_VirtualMiddleBtn.Enabled = false;
            }
        }

        private void Txt_VirtualMiddleBtn_Click(object sender, MouseEventArgs e)
        {
            _WndParam.TXT_DirectInput_MouseClick(sender, e);
        }

        #endregion
    }
}
