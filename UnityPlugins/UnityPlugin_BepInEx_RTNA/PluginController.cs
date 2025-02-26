using UnityEngine;

namespace UnityPlugin_BepInEx_Core
{
    public class PluginController
    {
        private const int INPUTBUTTONS_LENGTH = 4;

        public enum MyInputButtons
        {
            Start = 0,
            Trigger,
            Action,
            Reload
        }

        private int _ID = 0;
        public int ID
        { get { return _ID; } }

        public byte[] InputButtons { get; private set; }
        public byte[] InputButtonsBefore { get; private set; }

        public float Axis_X { get; private set; }
        public float Axis_Y { get; private set; }

        private bool _FlagButtonDown = false;
        private bool _FlagButtonUp = false;

        public PluginController(int ID)
        {
            _ID = ID;
            InputButtons = new byte[INPUTBUTTONS_LENGTH];
            InputButtonsBefore = new byte[INPUTBUTTONS_LENGTH];
        }

        public void SetAimingValues(Vector3 Position)
        {
            Axis_X = Position.x;
            Axis_Y = Position.y;
        }

        public void SetButton(MyInputButtons ButtonId, byte Value)
        {
            //Setting Up/Down events
            if (InputButtons[(int)ButtonId] != Value)
            {
                if (Value == 1)
                {
                    _FlagButtonDown = true;
                    _FlagButtonUp = false;
                }
                else if (Value == 0)
                {
                    _FlagButtonDown = false;
                    _FlagButtonUp = true;
                }
            }
            InputButtons[(int)ButtonId] = Value;
        }

        public bool GetButton(MyInputButtons ButtonId)
        {
            if (InputButtons[(int)ButtonId] == 1)
                return true;
            return false;
        }

        public bool GetButtonDown(MyInputButtons ButtonId)
        {
            if (_FlagButtonDown)
            {
                _FlagButtonDown = false;
                return true;
            }
            return false;
        }

        public bool GetButtonUp(MyInputButtons ButtonId)
        {
            if (_FlagButtonUp)
            {
                _FlagButtonUp = false;
                return true;
            }
            return false;
        }

        public Vector3 GetAimingPosition()
        {
            return new Vector3(Axis_X, Axis_Y);
        }
    }
}
