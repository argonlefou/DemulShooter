using System;
using System.IO;

namespace DsCore
{
    public static class Logger
    {
        private const string LOG_FILENAME = "debug.txt";
        public static bool IsEnabled { get; set; }

        /// <summary>
        /// Writing to Log only if verbose arg given in cmdline
        /// </summary>
        public static void WriteLog(String Data)
        {
            if (IsEnabled)
            {
                try
                {
                    using (StreamWriter sr = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + @"\" + LOG_FILENAME, true))
                    {
                        sr.WriteLine(DateTime.Now.ToString("HH:mm:ss.ffffff") + " : " + Data);
                        sr.Close();
                    }
                }
                catch { }
            }
        }   
    }
}
