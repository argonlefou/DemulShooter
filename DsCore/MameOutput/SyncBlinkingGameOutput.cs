using System;
using System.Timers;

namespace DsCore.MameOutput
{
    /// <summary>
    /// This kind of output is used to get synchronised blinking outputs between players
    /// Examples: Start buttons in pc game not supporting genuine outputs system
    /// The synchronised-to-be outputs are created at the same time, and immediately blinking.
    /// At the same time, we can set a fixed value.
    /// Then, according to the what we want to read, we ask for the fixed value or the blinking one
    /// </summary>
    public class SyncBlinkingGameOutput : GameOutput
    {
        private Timer _SyncBlinkingTimer;
        private int _BlinkingValue = 0;
        private bool _EnableBlinking = true;

        public override int OutputValue
        {
            get
            {
                if (_EnableBlinking)
                    return _BlinkingValue;
                else
                    return _OutputValue;
            }
            set
            {
                if (value == 0)
                {
                    _EnableBlinking = false;
                    _OutputValue = value;                    
                }
                else if (value == 1)
                {
                    _EnableBlinking = false;
                    _OutputValue = value;                    
                }
                else if (value == -1)
                {
                    _EnableBlinking = true;
                }
            }
        }

        public SyncBlinkingGameOutput(String Name, OutputId Id, int BlinkingTimerInterval): base(Name, Id)
        {
            _SyncBlinkingTimer = new Timer();
            _SyncBlinkingTimer.Interval = BlinkingTimerInterval;
            _SyncBlinkingTimer.Enabled = true;
            _SyncBlinkingTimer.Elapsed += new ElapsedEventHandler(BlinkingTimer_Elapsed);
            _SyncBlinkingTimer.Start();
        }

        private void BlinkingTimer_Elapsed(Object sender, EventArgs e)
        {
            if (_BlinkingValue == 0)
                _BlinkingValue = 1;
            else if (_BlinkingValue == 1)
                _BlinkingValue = 0;
        }
    }
}
