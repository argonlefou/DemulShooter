using System;
using System.Timers;

namespace DsCore.MameOutput
{
    public class BlinkGameOutput : GameOutput
    {
        private Timer _BlinkTimer;
        private bool _IsBlinking = false;

        public override int OutputValue
        {
            get
            {
                { return _OutputValue; }
            }
            set
            {
                if (value == 0)
                {
                    _BlinkTimer.Stop();
                    _OutputValue = value;
                    _IsBlinking = false;
                }
                else if (value == 1)
                {
                    _BlinkTimer.Stop();
                    _OutputValue = value;
                    _IsBlinking = false;
                }
                else
                {
                    if (!_IsBlinking)
                    {
                        _OutputValue = 1;
                        _BlinkTimer.Start();
                        _IsBlinking = true;
                    }
                }
            }
        }

        public BlinkGameOutput(String Name, OutputId Id, int BlinkTimerInterval): base(Name, Id)
        {
            _BlinkTimer = new Timer();
            _BlinkTimer.Interval = BlinkTimerInterval;
            _BlinkTimer.Enabled = true;
            _BlinkTimer.Elapsed += new ElapsedEventHandler(BlinkTimer_Elapsed);
        }

        private void BlinkTimer_Elapsed(Object sender, EventArgs e)
        {
            if (_OutputValue == 1)
                _OutputValue = 0;
            else
                _OutputValue = 1;
        }
    }
}
