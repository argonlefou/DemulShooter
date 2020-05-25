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
            bool isVerbose = false;

            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLower().Equals("-v") || args[i].ToLower().Equals("--verbose"))
                    {
                        isVerbose = true;
                    }
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);                       
            Application.Run(new Wnd_DemulShooterGui(isVerbose));
        }        
    }
}
