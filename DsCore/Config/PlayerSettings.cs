using System;
using System.Windows.Forms;
using DsCore.RawInput;
using DsCore.Win32;
using DsCore.XInput;

namespace DsCore.Config
{
    /// <summary>
    /// This class will store all Player-relative settings.
    /// This will be filled at start by parsing the config.ini file, and at runtime by adding a reference to the desired
    /// selected controller (if available and plugged)
    /// </summary>
    public class PlayerSettings
    {
        public const string PLAYER_MODE_RAWINPUT = "RAWINPUT";
        public const string PLAYER_MODE_XINPUT = "XINPUT";

        // General Data
        private int _ID = 0;
        private string _Mode = PLAYER_MODE_RAWINPUT;
        private string _DeviceName = string.Empty;        
        #region Accessors
        public int ID
        {
            get { return _ID; }
        }
        public string Mode
        {
            get { return _Mode; }
        }
        public string DeviceName
        {
            get { return _DeviceName; }
        }
        #endregion

        // RawInput Data
        private RawInputController _RIController;
        private byte _HidAxis_X = 0x30;
        private byte _HidAxis_Y = 0x31;
        private bool _InvertAxis_X = false;
        private bool _InvertAxis_Y = false;
        private int _HidButton_OnScreenTrigger = 1;
        private int _HidButton_OffScreenTrigger = 2;
        private int _HidButton_Action = 3;
        #region Accessors
        public RawInputController RIController
        {
            get { return _RIController; }
            set 
            {
                _RIController = value;
                if (value != null)
                {
                    
                    _DeviceName = _RIController.DeviceName;
                    _RIController.Selected_AxisX = _HidAxis_X;
                    _RIController.Selected_AxisY = _HidAxis_Y;
                    RIController.Selected_ActionButton = _HidButton_Action;
                    RIController.Selected_OnScreenTriggerButton = _HidButton_OnScreenTrigger;
                    RIController.Selected_OffScreenTriggerButton = _HidButton_OffScreenTrigger;
                }
                else
                    _DeviceName = String.Empty;
            }
        }
        public byte HidAxisX
        {
            get { return _HidAxis_X; }
        }
        public byte HidAxisY
        {
            get { return _HidAxis_Y; }
        }
        public bool InvertAxis_X
        {
            get { return _InvertAxis_X; }
            set { _InvertAxis_X = value; }
        }
        public bool InvertAxis_Y
        {
            get { return _InvertAxis_Y; }
            set { _InvertAxis_Y = value; }
        }
        public int HidButton_OnScreenTrigger
        {
            get { return _HidButton_OnScreenTrigger; }
        }
        public int HidButton_OffScreenTrigger
        {
            get { return _HidButton_OffScreenTrigger; }
        }
        public int HidButton_Action
        {
            get { return _HidButton_Action; }
        }
        #endregion

        // Virtual Middle click
        private bool _VirtualMouseButtonsEnabled = false;
        private HardwareScanCode _DIK_VirtualMouseButton_Left;
        private HardwareScanCode _DIK_VirtualMouseButton_Middle;
        private HardwareScanCode _DIK_VirtualMouseButton_Right;
        #region Accessors
        public bool isVirtualMouseButtonsEnabled
        {
            get { return _VirtualMouseButtonsEnabled; }
            set {_VirtualMouseButtonsEnabled = value;}
        }
        public HardwareScanCode DIK_VirtualMouseButton_Left
        {
            get { return _DIK_VirtualMouseButton_Left; }
            set { _DIK_VirtualMouseButton_Left = value; }
        }
        public HardwareScanCode DIK_VirtualMouseButton_Middle
        {
            get { return _DIK_VirtualMouseButton_Middle; }
            set { _DIK_VirtualMouseButton_Middle = value; }
        }
        public HardwareScanCode DIK_VirtualMouseButton_Right
        {
            get { return _DIK_VirtualMouseButton_Right; }
            set { _DIK_VirtualMouseButton_Right = value; }

        }
        #endregion

        // XInput data
        private int _GamepadID = -1;
        private XinputButtonFlags _Gamepad_LeftClick = XinputButtonFlags.XINPUT_GAMEPAD_B;
        private XinputButtonFlags _Gamepad_MiddleClick = XinputButtonFlags.XINPUT_GAMEPAD_X;
        private XinputButtonFlags _Gamepad_RightClick = XinputButtonFlags.XINPUT_GAMEPAD_A;
        private String _Gamepad_Stick = "L";
        private int _Gamepad_VibrationEnabled = 0;
        private int _Gamepad_VibrationLength = 50;
        private int _Gamepad_VibrationStrength = 0;
        #region Accessors
        public int GamepadID
        {
            get { return _GamepadID; }
        }
        #endregion

        // Act Lab calibration offset data
        private int _Act_Labs_OffsetX = 0;
        private int _Act_Labs_OffsetY = 0;
        #region Accessors
        public int Act_Labs_Offset_X
        {
            get { return _Act_Labs_OffsetX; }
            set { _Act_Labs_OffsetX = value; }
        }
        public int Act_Labs_Offset_Y
        {
            get { return _Act_Labs_OffsetY; }
            set { _Act_Labs_OffsetY = value; }
        }
        #endregion

        //Analog Gun axis calibration
        private bool _AnalogAxisRangeOverride = false;
        private int _AnalogManual_Xmin = 0;
        private int _AnalogManual_Xmax = 0;
        private int _AnalogManual_Ymin = 0;
        private int _AnalogManual_Ymax = 0;
        #region Accessors
        public bool AnalogAxisRangeOverride
        {
            get { return _AnalogAxisRangeOverride; }
            set { _AnalogAxisRangeOverride = value; }
        }
        public int AnalogManual_Xmin
        {
            get { return _AnalogManual_Xmin; }
            set { _AnalogManual_Xmin = value; }
        }
        public int AnalogManual_Xmax
        {
            get { return _AnalogManual_Xmax; }
            set { _AnalogManual_Xmax = value; }
        }
        public int AnalogManual_Ymin
        {
            get { return _AnalogManual_Ymin; }
            set { _AnalogManual_Ymin = value; }
        }
        public int AnalogManual_Ymax
        {
            get { return _AnalogManual_Ymax; }
            set { _AnalogManual_Ymax = value; }
        }
        #endregion

        /// <summary>
        /// Constructors, Setting default values. 
        /// This will be of use if no .INI file is found at launch
        /// </summary>
        /// <param name="PlayerID"></param>
        public PlayerSettings(int PlayerID)
        {
            _ID = PlayerID;
            if (_ID == 1)
            {
                _DIK_VirtualMouseButton_Left = HardwareScanCode.DIK_T;
                _DIK_VirtualMouseButton_Middle = HardwareScanCode.DIK_C;
                _DIK_VirtualMouseButton_Right = HardwareScanCode.DIK_F;
            }
            else if (_ID == 2)
            {
                _DIK_VirtualMouseButton_Left = HardwareScanCode.DIK_Y;
                _DIK_VirtualMouseButton_Middle = HardwareScanCode.DIK_V;
                _DIK_VirtualMouseButton_Right = HardwareScanCode.DIK_G;
            }
            else if (_ID == 3)
            {
                _DIK_VirtualMouseButton_Left = HardwareScanCode.DIK_U;
                _DIK_VirtualMouseButton_Middle = HardwareScanCode.DIK_B;
                _DIK_VirtualMouseButton_Right = HardwareScanCode.DIK_H;
            }
            else if (_ID == 4)
            {
                _DIK_VirtualMouseButton_Left = HardwareScanCode.DIK_I;
                _DIK_VirtualMouseButton_Middle = HardwareScanCode.DIK_N;
                _DIK_VirtualMouseButton_Right = HardwareScanCode.DIK_J;
            }
        }

        /// <summary>
        /// Reading and parsing existing confil file
        /// </summary>
        public bool ParseIniParameter(string StrKey, string StrValue)
        {
            if (StrKey.Equals("mode"))
            {
                _Mode = StrValue.ToUpper();
            } 
            else if (StrKey.Equals("devicename"))
            {
                _DeviceName = StrValue;
            }
            else if (StrKey.Equals("hidaxisx"))
            {
                try
                {
                    _HidAxis_X = Convert.ToByte(StrValue, 16);
                }
                catch { return false; }
            }
            else if (StrKey.Equals("hidaxisy"))
            {
                try
                {
                    _HidAxis_Y = Convert.ToByte(StrValue, 16);
                }
                catch { return false; }
            }
            else if (StrKey.Equals("invertaxisx"))
            {
                if (!bool.TryParse(StrValue, out _InvertAxis_X))
                    return false;
            }
            else if (StrKey.Equals("invertaxisy"))
            {
                if (!bool.TryParse(StrValue, out _InvertAxis_Y))
                    return false;
            }
            else if (StrKey.Equals("hidbtnonscreentrigger"))
            {
                if (!int.TryParse(StrValue, out _HidButton_OnScreenTrigger))
                    return false;
            }
            else if (StrKey.Equals("hidbtnoffscreentrigger"))
            {
                if (!int.TryParse(StrValue, out _HidButton_OffScreenTrigger))
                    return false;
            }
            else if (StrKey.Equals("hidbtnaction"))
            {
                if (!int.TryParse(StrValue, out _HidButton_Action))
                    return false;
            }
            else if (StrKey.Equals("gamepadleftclick"))
            {
                try
                {
                    _Gamepad_LeftClick = (XinputButtonFlags)Enum.Parse(typeof(XinputButtonFlags), StrValue);
                }
                catch { return false; }
            }
            else if (StrKey.Equals("gamepadmiddleclick"))
            {
                try
                {
                    _Gamepad_MiddleClick = (XinputButtonFlags)Enum.Parse(typeof(XinputButtonFlags), StrValue);
                }
                catch { return false; }
            }
            else if (StrKey.Equals("gamepadrightclick"))
            {
                try
                {
                    _Gamepad_RightClick = (XinputButtonFlags)Enum.Parse(typeof(XinputButtonFlags), StrValue);
                }
                catch { return false; }
            }
            else if (StrKey.Equals("gamepadstick"))
            {
                _Gamepad_Stick = StrValue.ToUpper();
            }
            else if (StrKey.Equals("gamepadvibrationenabled"))
            {
                if (!int.TryParse(StrValue, out _Gamepad_VibrationEnabled))
                    return false;
            }
            else if (StrKey.Equals("gamepadvibrationlength"))
            {
                if (!int.TryParse(StrValue, out _Gamepad_VibrationLength))
                    return false;
            }
            else if (StrKey.Equals("gamepadvibrationstrength"))
            {
                if (!int.TryParse(StrValue, out _Gamepad_VibrationStrength))
                    return false;
            }            
            else if (StrKey.Equals("act_labs_offset_x"))
            {
                if (!int.TryParse(StrValue, out _Act_Labs_OffsetX))
                    return false;
            }
            else if (StrKey.Equals("act_labs_offset_y"))
            {
                if (!int.TryParse(StrValue, out _Act_Labs_OffsetY))
                    return false;
            }
            else if (StrKey.Equals("analog_calibration_override"))
            {
                if (!bool.TryParse(StrValue, out _AnalogAxisRangeOverride))
                    return false; ;
            }
            else if (StrKey.Equals("analog_manual_xmin"))
            {
                if (!int.TryParse(StrValue, out _AnalogManual_Xmin))
                    return false;
            }
            else if (StrKey.Equals("analog_manual_xmax"))
            {
                if (!int.TryParse(StrValue, out _AnalogManual_Xmax))
                    return false;
            }
            else if (StrKey.Equals("analog_manual_ymin"))
            {
                if (!int.TryParse(StrValue, out _AnalogManual_Ymin))
                    return false;
            }
            else if (StrKey.Equals("analog_manual_ymax"))
            {
                if (!int.TryParse(StrValue, out _AnalogManual_Ymax))
                    return false;
            }
            else if (StrKey.Equals("virtualmousebuttons_enable"))
            {
                if (!bool.TryParse(StrValue, out _VirtualMouseButtonsEnabled))
                    return false; ;
            }
            else if (StrKey.Equals("virtualmousebuttonleft_key"))
            {
                try
                {
                    _DIK_VirtualMouseButton_Left = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                }
                catch { return false; }
            }
            else if (StrKey.Equals("virtualmousebuttonmiddle_key"))
            {
                try
                {
                    _DIK_VirtualMouseButton_Middle = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                }
                catch { return false; }
            }
            else if (StrKey.Equals("virtualmousebuttonright_key"))
            {
                try
                {
                    _DIK_VirtualMouseButton_Right = (HardwareScanCode)Enum.Parse(typeof(HardwareScanCode), StrValue);
                }
                catch { return false; }
            }
            return true;
        }
    }
}
