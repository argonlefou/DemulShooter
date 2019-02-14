using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace DemulShooter
{
    public class ControllerDevice
    {
        private const String XINPUT_DEVICE_PREFIX = "XInput Gamepad #";
        private WndParam _WndParam;

        // General Data
        private int _Player = 0;
        private string _DeviceName = string.Empty;        

        // RawInput type Data
        private IntPtr _MouseHandle = IntPtr.Zero;
        private byte _DiK_VirtualMiddleButton;
        private byte _DiK_VirtualRightButton;

        // GamePad type data
        private int _GamepadID = -1;
        private int _Gamepad_LeftClick = (int)ButtonFlags.XINPUT_GAMEPAD_B;
        private int _Gamepad_MiddleClick = (int)ButtonFlags.XINPUT_GAMEPAD_X;
        private int _Gamepad_RightClick = (int)ButtonFlags.XINPUT_GAMEPAD_A;
        private String _Gamepad_Stick = "L";
        private int _Gamepad_VibrationEnabled = 0;
        private int _Gamepad_VibrationLength = 50;
        private int _Gamepad_VibrationStrength = 0;
        private Timer _VibrationTimer;

        // Act Lab calibration offset data
        private int _Act_Labs_OffsetX = 0;
        private int _Act_Labs_OffsetY = 0;

        // Virtual Middle click
        private int _EnableGunVirtualButtons = 0;

        //
        private MouseInfo _LastMouseInfo;

        #region Accessors

        public int Player
        {
            get { return _Player; }
        }

        public string DeviceName
        {
            get { return _DeviceName; }
            set 
            { 
                _DeviceName = value;

                if (_DeviceName.Length > 0)
                {
                    if (DeviceName.StartsWith(XINPUT_DEVICE_PREFIX))
                    {
                        if (int.TryParse(_DeviceName.Substring(_DeviceName.Length - 1, 1), out _GamepadID))
                        {
                            _GamepadID = _GamepadID - 1;
                        }
                    }
                    else
                    {
                        _MouseHandle = GetRawMouseHandleFromName(_DeviceName);
                        _GamepadID = -1;
                    }
                }
                else
                {
                    _GamepadID = -1;
                }
            }
        }

        public IntPtr MouseHandle
        {
            get { return _MouseHandle; }
        }

        public byte DiK_VirtualMiddleButton
        {
            get { return _DiK_VirtualMiddleButton; }
            set { _DiK_VirtualMiddleButton = value; }
        }

        public byte DiK_VirtualRightButton
        {
            get { return _DiK_VirtualRightButton; }
            set { _DiK_VirtualRightButton = value; }
        }

        public int GamepadID
        {
            get { return _GamepadID; }
        }

        public int Gamepad_LeftClick
        {
            get { return _Gamepad_LeftClick; }
            set { _Gamepad_LeftClick = value; }
        }

        public int Gamepad_MiddleClick
        {
            get { return _Gamepad_MiddleClick; }
            set { _Gamepad_MiddleClick = value; }
        }

        public int Gamepad_RightClick
        {
            get { return _Gamepad_RightClick; }
            set { _Gamepad_RightClick = value; }
        }

        public string Gamepad_Stick
        {
            get { return _Gamepad_Stick; }
            set { _Gamepad_Stick = value; }
        }

        public int Gamepad_VibrationEnabled
        {
            get { return _Gamepad_VibrationEnabled; }
            set { _Gamepad_VibrationEnabled = value; }
        }

        public int Gamepad_VibrationLength
        {
            get { return _Gamepad_VibrationLength; }
            set { _Gamepad_VibrationLength = value; }
        }

        public int Gamepad_VibrationStrength
        {
            get { return _Gamepad_VibrationStrength; }
            set { _Gamepad_VibrationStrength = value; }
        }

        public int Act_Labs_OffsetX
        {
            get { return _Act_Labs_OffsetX; }
            set { _Act_Labs_OffsetX = value; }
        }

        public int Act_Labs_OffsetY
        {
            get { return _Act_Labs_OffsetY; }
            set { _Act_Labs_OffsetY = value; }
        }            

        public int EnableGunVirtualButtons
        {
            get { return _EnableGunVirtualButtons; }
            set { _EnableGunVirtualButtons = value; }
        }

        public MouseInfo LastMouseInfo
        {
            get { return _LastMouseInfo; }
            set { _LastMouseInfo = value; }
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="PlayerNumber">Player ID (1-4)</param>
        public ControllerDevice(int PlayerNumber, WndParam MainWindow)
        {
            _Player = PlayerNumber;
            _WndParam = MainWindow;

            // Default virtual buttons are :
            // P1 Middle => 0x2E   [C]
            // P2 Middle => 0x2F   [V]
            // P3 Middle => 0x30   [B]
            // P4 Middle => 0x31   [N]
            _DiK_VirtualMiddleButton = (byte)(0x2D + _Player);
            // P1 Right => 0x21   [F]
            // P2 Right => 0x22   [G]
            // P3 Right => 0x23   [H]
            // P4 Right => 0x24   [J]            
            _DiK_VirtualRightButton = (byte)(0x20 + _Player);
        }

        /// <summary>
        /// Initialize the Vibration Timer and link it to the event callback
        /// This Timer will be used to set the length of the vibration for Xbox compatible controller
        /// </summary>
        public void InitVibrationTimer()
        {
            _VibrationTimer = new Timer();
            _VibrationTimer.Tick += new EventHandler(VibrationTimer_Tick);
        }
        public void RunVibrationTimer()
        {
            _VibrationTimer.Interval = _Gamepad_VibrationLength;
            _VibrationTimer.Start();
        }
        private void VibrationTimer_Tick(object sender, EventArgs e)
        {
            XInputVibration vibration = new XInputVibration() { LeftMotorSpeed = (ushort)0, RightMotorSpeed = (ushort)0 };
            XInput.XInputSetState(_GamepadID, ref vibration);
            _VibrationTimer.Stop();
        }

        /// <summary>
        /// Setting default values, this will be of use if no .INI file is found at launch
        /// </summary>
        public bool ParseIniParameter(string StrKey, string StrValue)
        {
            if (StrKey.Equals("device"))
            {
                DeviceName = StrValue;               
            }
            else if (StrKey.Equals("gamepadleftclick"))
            {
                if (!int.TryParse(StrValue, out _Gamepad_LeftClick))
                    return false;
            }
            else if (StrKey.Equals("gamepadmiddleclick"))
            {
                if (!int.TryParse(StrValue, out _Gamepad_MiddleClick))
                    return false;
            }
            else if (StrKey.Equals("gamepadrightclick"))
            {
                if (!int.TryParse(StrValue, out _Gamepad_RightClick))
                    return false;
            }
            else if (StrKey.Equals("gamepadstick"))
            {
                _Gamepad_Stick = StrValue.ToUpper();
                    return false;               
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
            else if (StrKey.Equals("virtualbuttons_enable"))
            {
                if (!int.TryParse(StrValue, out _EnableGunVirtualButtons))
                    return false;;
            }
            else if (StrKey.Equals("virtualmiddle_key"))
            {
                if (!byte.TryParse(StrValue, out _DiK_VirtualMiddleButton))
                    return false;
            }
            else if (StrKey.Equals("virtualright_key"))
            {
                if (!byte.TryParse(StrValue, out _DiK_VirtualRightButton))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Get Handle from mouseName
        /// </summary>
        private IntPtr GetRawMouseHandleFromName(String MouseName)
        {
            foreach (MouseInfo mouse in _WndParam.MiceList)
            {
                if (mouse.devName == MouseName)
                {
                    return mouse.devHandle;
                }
            }
            return IntPtr.Zero;
        }
    }
}
