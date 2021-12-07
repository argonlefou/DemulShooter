using System;
using System.IO;

namespace DsCore
{
    public static class Logger
    {
        private static string LogFilename = "debug.txt";
        public static bool IsEnabled { get; set; }

        public static void InitLogFileName()
        {
            LogFilename = "Debug_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt";
        }


        /// <summary>
        /// Writing to Log only if verbose arg given in cmdline
        /// </summary>
        public static void WriteLog(String Data)
        {
            if (IsEnabled)
            {
                try
                {
                    using (StreamWriter sr = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + @"\" + LogFilename, true))
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
