namespace UnityPlugin_BepInEx_Core
{
    public class PluginControllerButton
    {
        public bool ButtonState { get; private set; }
        public bool PreviousButtonState { get; private set; }

        private bool _FlagButtonDown;
        private bool _FlagButtonUp;

        public int KeyCode { get; private set; }

        public PluginControllerButton(int iKeyCode = 0)
        {
            KeyCode = iKeyCode;
        }

        public void SetKeyCode(string StrValue)
        {
            int iResult;
            if (int.TryParse(StrValue, out iResult))
                KeyCode = iResult;
        }

        public void SetKeyCode(int iValue)
        {
            KeyCode = iValue;
        }

        public void SetButton(bool Value)
        {
            //Setting Up/Down events
            if (ButtonState != Value)
            {
                if (Value)
                {
                    _FlagButtonDown = true;
                    _FlagButtonUp = false;
                }
                else
                {
                    _FlagButtonDown = false;
                    _FlagButtonUp = true;
                }
            }
            ButtonState = Value;
        }

        public bool GetButton()
        {
            return ButtonState;
        }

        public bool GetButtonDown()
        {
            if (_FlagButtonDown)
            {
                _FlagButtonDown = false;
                return true;
            }
            return false;
        }

        public bool GetButtonUp()
        {
            if (_FlagButtonUp)
            {
                _FlagButtonUp = false;
                return true;
            }
            return false;
        }
    }
}
