using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityPlugin_BepInEx_Core
{
    public class PluginController
    {
        public enum PluginButton : int
        {
            Trigger = 0,
            Reload,
            Action
        }
        public static readonly int ButtonsCount = 3;

        private byte[] _ButtonsBefore;
        private byte[] _ButtonsCurrent;
        private byte[] _ButtonsChangedStateRead;

        private float _AxisX;
        private float _AxisY;

        private int _PlayerId;

        public PluginController(int PlayerId)
        {
            _PlayerId = PlayerId;
            _ButtonsBefore = new byte[ButtonsCount];
            _ButtonsCurrent = new byte[ButtonsCount];
            _ButtonsChangedStateRead = new byte[ButtonsCount];
        }
        


        public void SetButton(PluginButton Button, byte State)
        {
            if (_ButtonsCurrent[(int)Button] != State)
            {
                _ButtonsBefore[(int)Button] = _ButtonsCurrent[(int)Button];
                _ButtonsCurrent[(int)Button] = State;
                _ButtonsChangedStateRead[(int)Button] = 0;
            }            
        }
        public void SetAxis(UInt16 AxisX, UInt16 AxisY)
        {
            _AxisX = (float)AxisX;
            _AxisY = (float)AxisY;
        }

        public bool GetButtonDown(PluginButton Button)
        {
            if (_ButtonsChangedStateRead[(int)Button] == 0)
            {
                if (_ButtonsCurrent[(int)Button] == 1 && _ButtonsBefore[(int)Button] == 0)
                {
                    _ButtonsChangedStateRead[(int)Button] = 1;
                    return true;
                }
            }
            return false;
        }
        public bool GetButtonUp(PluginButton Button)
        {
            if (_ButtonsChangedStateRead[(int)Button] == 0)
            {
                if (_ButtonsCurrent[(int)Button] == 0 && _ButtonsBefore[(int)Button] == 1)
                {
                    _ButtonsChangedStateRead[(int)Button] = 1;
                    return true;
                }
            }
            return false;
        }
        public bool GetButton(PluginButton Button)
        {
            if (_ButtonsCurrent[(int)Button] == 1 && _ButtonsChangedStateRead[(int)Button] == 1)
            {
                return true;
            }
            return false;
        }
        public float GetAxisX()
        {
            return _AxisX;
        }
        public float GetAxisY()
        {
            return _AxisY;
        }


    }
}
