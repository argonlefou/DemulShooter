using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DsCore.MameOutput
{
    /// <summary>
    /// Defines constants for the multimedia Timer's event types.
    /// </summary>
    public enum TimerMode
    {
        /// <summary>
        /// Timer event occurs once.
        /// </summary>
        OneShot,

        /// <summary>
        /// Timer event occurs periodically.
        /// </summary>
        Periodic
    };

    /// <summary>
    /// Represents information about the multimedia Timer's capabilities.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TimeCaps
    {
        /// <summary>
        /// Minimum supported period in milliseconds.
        /// </summary>
        public UInt32 wPeriodMin;

        /// <summary>
        /// Maximum supported period in milliseconds.
        /// </summary>
        public UInt32 wPeriodMax;
    }

    /// <summary>
    /// Represents the Windows multimedia timer.
    /// </summary>
    public sealed class Mmt
    {
        #region Timer Members

        #region Delegates

        // Represents the method that is called by Windows when a timer event occurs.
        private delegate void TimeProc(int id, int msg, int user, int param1, int param2);

        // Represents methods that raise events.
        private delegate void EventRaiser(EventArgs e);

        #endregion

        #region Win32 Multimedia Timer Functions

        // Gets timer capabilities.
        [DllImport("winmm.dll")]
        private static extern UInt32 timeGetDevCaps(ref TimeCaps ptc, int cbtc);

        // Creates and starts the timer.
        [DllImport("winmm.dll")]
        private static extern UInt32 timeSetEvent(UInt32 uDelay, UInt32 uResolution, TimeProc lpTimeProc, UIntPtr dwUser, UInt32 fuEvent);

        // Stops and destroys the timer.
        [DllImport("winmm.dll")]
        private static extern UInt32 timeKillEvent(UInt32 uTimerID);

        // Indicates that the operation was successful.
        private const int TIMERR_NOERROR = 0;

        #endregion

        #region Fields

        // Timer identifier.
        private UInt32 _TimerID;

        // Timer mode.
        private volatile TimerMode _Mode;

        // Period between timer events in milliseconds.
        private volatile UInt32 _Period;

        // Timer resolution in milliseconds.
        private volatile UInt32 _Resolution;

        // Called by Windows when a timer periodic event occurs.
        private TimeProc timeProcPeriodic;

        // Called by Windows when a timer one shot event occurs.
        private TimeProc timeProcOneShot;

        // Represents the method that raises the Tick event.
        private EventRaiser tickRaiser;

        // Indicates whether or not the timer is running.
        private bool _IsRunning = false;

        // Multimedia timer capabilities.
        private static TimeCaps caps;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the Timer has started;
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Occurs when the Timer has stopped;
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Occurs when the time period has elapsed.
        /// </summary>
        public event EventHandler Tick;

        #endregion

        #region Construction

        /// <summary>
        /// Initialize class.
        /// </summary>
        static Mmt()
        {
            // Get multimedia timer capabilities.
            timeGetDevCaps(ref caps, Marshal.SizeOf(caps));
        }

        /// <summary>
        /// Initializes a new instance of the Timer class.
        /// </summary>
        public Mmt()
        {
            Initialize();
        }

        ~Mmt()
        {
            if (IsRunning)
            {
                // Stop and destroy timer.
                timeKillEvent(_TimerID);
            }
        }

        // Initialize timer with default values.
        private void Initialize()
        {
            this._Mode = TimerMode.Periodic;
            this._Period = Capabilities.wPeriodMin;
            this._Resolution = 1;

            _IsRunning = false;

            timeProcPeriodic = new TimeProc(TimerPeriodicEventCallback);
            timeProcOneShot = new TimeProc(TimerOneShotEventCallback);
            tickRaiser = new EventRaiser(OnTick);
        }

        #endregion

        #region Methods

        public void Start()
        {
            if (IsRunning)
                return;

            if (Mode == TimerMode.Periodic)
                _TimerID = timeSetEvent(Period, Resolution, timeProcPeriodic, UIntPtr.Zero, (UInt32)Mode);
            else
                _TimerID = timeSetEvent(Period, Resolution, timeProcOneShot, UIntPtr.Zero, (UInt32)Mode);

            if (_TimerID != 0)
                _IsRunning = true;
            else
                throw new TimerStartException("Unable to start multimedia Timer.");
        }

        public void Stop()
        {
            if (!_IsRunning)
                return;

            UInt32 result = timeKillEvent(_TimerID);

            Debug.Assert(result == TIMERR_NOERROR);

            _IsRunning = false;

            OnStopped(EventArgs.Empty);
        }

        #region Callbacks

        // Callback method called by the Win32 multimedia timer when a timer
        // periodic event occurs.
        private void TimerPeriodicEventCallback(int id, int msg, int user, int param1, int param2)
        {
            OnTick(EventArgs.Empty);
        }

        // Callback method called by the Win32 multimedia timer when a timer
        // one shot event occurs.
        private void TimerOneShotEventCallback(int id, int msg, int user, int param1, int param2)
        {
            OnTick(EventArgs.Empty);
            Stop();
        }

        #endregion

        #region Event Raiser Methods

        // Raises the Started event.
        private void OnStarted(EventArgs e)
        {
            EventHandler handler = Started;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        // Raises the Stopped event.
        private void OnStopped(EventArgs e)
        {
            EventHandler handler = Stopped;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        // Raises the Tick event.
        private void OnTick(EventArgs e)
        {
            EventHandler handler = Tick;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// Gets or sets the time between Tick events.
        /// </summary>
        public UInt32 Period
        {
            get
            {
                return _Period;
            }
            set
            {
                if (value < Capabilities.wPeriodMin || value > Capabilities.wPeriodMax)
                {
                    throw new ArgumentOutOfRangeException("Period", value,
                        "Multimedia Timer period out of range.");
                }

                _Period = value;

                if (IsRunning)
                {
                    Stop();
                    Start();
                }
            }
        }

        /// <summary>
        /// Gets or sets the timer resolution.
        /// </summary>           
        /// <remarks>
        /// The resolution is in milliseconds. The resolution increases 
        /// with smaller values; a resolution of 0 indicates periodic events 
        /// should occur with the greatest possible accuracy. To reduce system 
        /// overhead, however, you should use the maximum value appropriate 
        /// for your application.
        /// </remarks>
        public UInt32 Resolution
        {
            get
            {
                return _Resolution;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Resolution", value,
                        "Multimedia timer resolution out of range.");
                }

                _Resolution = value;

                if (IsRunning)
                {
                    Stop();
                    Start();
                }
            }
        }

        /// <summary>
        /// Gets the timer mode.
        /// </summary>        
        public TimerMode Mode
        {
            get
            {
                return _Mode;
            }
            set
            {
                _Mode = value;

                if (IsRunning)
                {
                    Stop();
                    Start();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the Timer is running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return _IsRunning;
            }
        }

        /// <summary>
        /// Gets the timer capabilities.
        /// </summary>
        public static TimeCaps Capabilities
        {
            get
            {
                return caps;
            }
        }

        #endregion
    }

    /// <summary>
    /// The exception that is thrown when a timer fails to start.
    /// </summary>
    public class TimerStartException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the TimerStartException class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception. 
        /// </param>
        public TimerStartException(string message)
            : base(message)
        {
        }
    }
}
