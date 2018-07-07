using System;

namespace DemulShooter
{
    public class MouseInfo
    {
        public IntPtr devHandle;
        public string devName;
        public System.Drawing.Point pTarget;
        public int button;
        
        public MouseInfo()
        {
            devHandle = IntPtr.Zero;
            devName = String.Empty;
            pTarget.X = 0;
            pTarget.Y = 0;
            button = 0;
        }
    }
}
