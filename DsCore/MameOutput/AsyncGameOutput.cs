using System;
using System.Timers;

namespace DsCore.MameOutput
{
    public class AsyncGameOutput : GameOutput
    {
        private Timer _AsyncResetTimer;
        private bool _IsTimerRunning = false;

        public override int OutputValue
        {
            get
            {
                { return _OutputValue; }
            }
            set
            {
                if (!_IsTimerRunning)
                {
                    _IsTimerRunning = true;
                    _OutputValue = value;
                    _AsyncResetTimer.Start();                    
                }
            }
        }

        public AsyncGameOutput(String Name, OutputId Id, int AsyncResetTimerInterval): base(Name, Id)
        {
            _AsyncResetTimer = new Timer();
            _AsyncResetTimer.Interval = AsyncResetTimerInterval;
            _AsyncResetTimer.Enabled = true;
            _AsyncResetTimer.Elapsed += new ElapsedEventHandler(AsyncResetTimer_Elapsed);
        }

        private void AsyncResetTimer_Elapsed(Object sender, EventArgs e)
        {
            _IsTimerRunning = false;
            _OutputValue = 0;
            _AsyncResetTimer.Stop();            
        }
    }
}
