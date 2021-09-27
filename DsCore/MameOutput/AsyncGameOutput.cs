using System;
using System.Timers;

namespace DsCore.MameOutput
{
    public class AsyncGameOutput : GameOutput
    {
        private Timer _AsyncResetTimer;
        private int _AsyncResetTimerOnInterval = 50;
        private int _AsyncResetTimerOffInterval = 50;
        private bool _IsTimerRunning = false;
        private int _OffValue = 0;

        public override int OutputValue
        {
            get
            {
                { return _OutputValue; }
            }
            set
            {
                if (!_IsTimerRunning && value != _OffValue)
                {
                    if (value != _OffValue)
                    {
                        _IsTimerRunning = true;
                        _OutputValue = value;
                        _AsyncResetTimer.Interval = _AsyncResetTimerOnInterval;
                        _AsyncResetTimer.Start();                        
                    }
                }
            }
        }

        public AsyncGameOutput(String Name, OutputId Id, int AsyncResetTimerOnInterval, int AsyncResetTimerOffInterval, int RestValue)
            : base(Name, Id)
        {
            _OffValue = RestValue;
            _AsyncResetTimerOnInterval = AsyncResetTimerOnInterval;
            _AsyncResetTimerOffInterval = AsyncResetTimerOffInterval;
            _AsyncResetTimer = new Timer();
            _AsyncResetTimer.Interval = AsyncResetTimerOnInterval;
            _AsyncResetTimer.Enabled = true;
            _AsyncResetTimer.Stop();
            _AsyncResetTimer.Elapsed += new ElapsedEventHandler(AsyncResetTimer_Elapsed);
        }

        private void AsyncResetTimer_Elapsed(Object sender, EventArgs e)
        {
            if (_OutputValue != _OffValue)
            {
                _AsyncResetTimer.Interval = _AsyncResetTimerOffInterval;
                _OutputValue = _OffValue; 
            }
            else
            {
                _AsyncResetTimer.Stop();
                _IsTimerRunning = false;
            }            
        }
    }
}
