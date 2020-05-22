using System;
using System.Windows.Forms;

namespace DemulShooter_GUI
{    
    static class Program
    {
        /// <summary>
        /// Application entry point
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);                       
            Application.Run(new Wnd_DemulShooterGui());
        }        
    }
}
