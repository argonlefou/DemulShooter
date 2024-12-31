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
        public byte[] InputButtonsBefore { get; set; }

        public float Axis_X { get; private set; }
        public float Axis_Y { get; private set; }

        public PluginController(int ID)
        {
            _ID = ID;
            InputButtons = new byte[INPUTBUTTONS_LENGTH];
            InputButtonsBefore = new byte[INPUTBUTTONS_LENGTH];
        }

        public void SetAimingValues(Vector2 Position)
        {
            Axis_X = Position.x;
            Axis_Y = Position.y;
        }

        public void SetButton(MyInputButtons ButtonId, byte Value)
        {
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
            if (InputButtons[(int)ButtonId] == 1 && InputButtonsBefore[(int)ButtonId] == 0)
            {
                InputButtonsBefore[(int)ButtonId] = 1;
                return true;
            }
            return false;
        }

        public bool GetButtonUp(MyInputButtons ButtonId)
        {            
            if (InputButtons[(int)ButtonId] == 0 && InputButtonsBefore[(int)ButtonId] == 1)
            {
                InputButtonsBefore[(int)ButtonId] = 0;
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
