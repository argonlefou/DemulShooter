using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace DemulShooterX64
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        const uint WM_CHAR = 0x0102;
        const int VK_ENTER = 0x0D;
        const int ATTACH_PARENT_PROCESS = -1;

        static IntPtr ConsoleHwnd = IntPtr.Zero;

        static String strTarget = string.Empty;
        static String strRom = string.Empty;

        static void Main(string[] args)
        {
            // Attach to the parent process via AttachConsole SDK call
            AttachConsole(ATTACH_PARENT_PROCESS);
            ConsoleHwnd = GetConsoleWindow();

            bool isVerbose = false;

            String[] _Targets = new string[] { "es3", "seganu", "windows"};
            String[] _Es3Roms = new string[] { "tc5" };
            String[] _SegaNuRoms = new string[] { "lma" };
            String[] _WindowsRoms = new string[] { "bhap" };
            
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLower().Equals("-h") || args[i].ToLower().Equals("--help") || args[i].ToLower().Equals("-?"))
                    {
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine("DemulShooterX64 v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString());
                        Console.WriteLine("Build date : September, 2nd 2020");
                        Console.WriteLine("");
                        Console.WriteLine("usage : DemulShooterX64.exe -target=[target] -rom=[rom] [options]");
                        Console.WriteLine("");
                        Console.WriteLine("Supported [target] :");
                        Console.WriteLine("es3\t\tNamco ES3 Games");
                        Console.WriteLine("seganu\t\tTeknoParrot Loader");
                        Console.WriteLine("");
                        Console.WriteLine("Supported [rom] :");
                        Console.WriteLine("ES3 roms :");
                        Console.WriteLine(" tc5\t\tTime Crisis 5");
                        Console.WriteLine("");
                        Console.WriteLine("SEGA Nu roms :");
                        Console.WriteLine(" lma\t\tLuigi's Mansion Arcade");
                        Console.WriteLine("");
                        Console.WriteLine("Windows games :");
                        Console.WriteLine(" bhap\t\tBuck Hunt Arcade PC");
                        Console.WriteLine("");
                        Console.WriteLine("Supported [options] :");
                       
                        Console.WriteLine(" -? -h --help\tShow this help");
                        Console.WriteLine(" -v --verbose\tEnable output to log file");

                        ExitConsole();
                    }
                    else if (args[i].ToLower().Equals("-v") || args[i].ToLower().Equals("--verbose"))
                    {
                        isVerbose = true;
                    }

                    else if (args[i].ToLower().StartsWith("-target"))
                    {
                        strTarget = (args[i].ToLower().Split('='))[1].Trim();
                        if (!CheckParameter(strTarget, _Targets))
                        {
                            Console.WriteLine("\nUnsupported [target] parameter : \"" + strTarget + "\". See help for supported targets");
                            ExitConsole();
                        }
                    }
                }

                if (strTarget == String.Empty)
                {
                    Console.WriteLine("\n\n\tNo [target] parameter specified. See help for supported targets");
                    ExitConsole();
                }

                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLower().StartsWith("-rom"))
                    {
                        strRom = (args[i].ToLower().Split('='))[1].Trim();
                        if (strTarget.Equals("es3"))
                        {
                            if (!CheckParameter(strRom, _Es3Roms))
                            {
                                Console.WriteLine("Unsupported Namco ES3 rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("seganu"))
                        {
                            if (!CheckParameter(strRom, _SegaNuRoms))
                            {
                                Console.WriteLine("Unsupported Saga Nu rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("windows"))
                        {
                            if (!CheckParameter(strRom, _WindowsRoms))
                            {
                                Console.WriteLine("Unsupported Windows rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                    }
                }

                if (strRom == String.Empty)
                {
                    Console.WriteLine("\n\n\tNo [Rom] parameter specified. See help for supported targets");
                    ExitConsole();
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new DemulShooterWindowX64(args, isVerbose));
            }
            else
            {
                Console.WriteLine("\n\n\tMissing parameter!\n\tSee help with DemulShooter.exe -h for correct usage.");
                ExitConsole();
            }

        }

        static bool CheckParameter(String param, string[] list)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (param == list[i])
                    return true;
            }
            return false;
        }

        static void ExitConsole()
        {
            FreeConsole();
            //Send a {ENTER} key message to the attached console to show prompt
            SendMessage(ConsoleHwnd, WM_CHAR, (IntPtr)VK_ENTER, IntPtr.Zero);
            Environment.Exit(0);
        }
    }
}
