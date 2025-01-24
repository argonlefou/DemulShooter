using UnityEngine;

namespace UnityPlugin_BepInEx_Core
{
    public class PluginController
    {
        private const int INPUTBUTTONS_LENGTH = 3;

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

        public byte[] InputButtons { get; set; }

        private bool _TriggerButtonPressed = false;
        private bool _TriggerButtonReleased = false;        

        public float Axis_X { get; private set; }
        public float Axis_Y { get; private set; }

        public PluginController(int ID)
        {
            _ID = ID;
            InputButtons = new byte[INPUTBUTTONS_LENGTH];
        }

        public void SetAimingValues(Vector2 Position)
        {
            Axis_X = Position.x;
            Axis_Y = Position.y;
        }

        public void SetButton(MyInputButtons ButtonId, byte Value)
        {
            if (InputButtons[(int)ButtonId] != Value)
            {
                if (InputButtons[(int)ButtonId] == 1)
                {
                    _TriggerButtonPressed = false;
                    _TriggerButtonReleased = true;
                }
                else
                {
                    _TriggerButtonPressed = true;
                    _TriggerButtonReleased = false;
                }
                InputButtons[(int)ButtonId] = Value;
            }            
        }

        public bool GetButton(MyInputButtons ButtonId)
        {
            if (InputButtons[(int)ButtonId] == 1)
                return true;
            return false;
        }

        public bool GetButtonDown(MyInputButtons ButtonId)
        {
            if (_TriggerButtonPressed)
            {
                _TriggerButtonPressed = false;
                return true;
            }
            return false;
        }

        public bool GetButtonUp(MyInputButtons ButtonId)
        {            
            if (_TriggerButtonReleased)
            {
                _TriggerButtonReleased = false;
                return true;
            }
            return false;
        }

        public Vector2 GetAiming()
        {
            return new Vector2(Axis_X, Axis_Y);
        }
    }
}
