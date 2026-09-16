using UnityEngine;

namespace UnityPlugin_BepInEx_Core
{
    public class PluginController
    {
        private const int INPUTBUTTONS_LENGTH = 5;

        public enum MyInputButtons
        {
            Start = 0,
            Trigger,
            Action,
            Reload,
            Coin
        }

        private int _ID = 0;
        public int ID
        { get { return _ID; } }

        public PluginControllerButton[] InputButtons;

        public float Axis_X { get; private set; }
        public float Axis_Y { get; private set; }

        public PluginController(int ID)
        {
            _ID = ID;
            InputButtons = new PluginControllerButton[INPUTBUTTONS_LENGTH];
            for (int i = 0; i < INPUTBUTTONS_LENGTH; i++)
            {
                InputButtons[i] = new PluginControllerButton();
            }
            InputButtons[(int)MyInputButtons.Start].SetKeyCode(49 + ID);
            InputButtons[(int)MyInputButtons.Coin].SetKeyCode(53 + ID);
        }

        public void SetAimingValues(Vector3 Position)
        {
            Axis_X = Position.x;
            Axis_Y = Position.y;
        }

        public void SetButton(MyInputButtons ButtonId, byte Value)
        {
            InputButtons[(int)ButtonId].SetButton(Value == 1 ? true : false);
        }

        public bool GetButton(MyInputButtons ButtonId)
        {
            return InputButtons[(int)ButtonId].GetButton();
        }

        public bool GetButtonDown(MyInputButtons ButtonId)
        {
            return InputButtons[(int)ButtonId].GetButtonDown();
        }

        public bool GetButtonUp(MyInputButtons ButtonId)
        {
            return InputButtons[(int)ButtonId].GetButtonUp();
        }

        public Vector3 GetAimingPosition()
        {
            return new Vector3(Axis_X, Axis_Y);
        }
    }
}
