using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Collections.Generic;

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

            Dictionary<String, String> _SystemTargets = new Dictionary<String, String>(){
                {"aagames","Adrenaline Amusements"},
                {"alls","SEGA Amusement Linkage Live System games"},
                {"es3","Namco ES3 games"},
                {"flycast","Flycast v2.0"},
                {"seganu","SEGA Nu games"},
                {"windows","Windows games"}                
            };

            Dictionary<String, String> _AagamesRoms = new Dictionary<String, String>(){
                {"rha", "Rabbids Hollywood Arcade"},
                {"tra", "Tomb Raider Arcade"}
            };

            Dictionary<String, String> _AllsRoms = new Dictionary<String, String>(){
                {"hodsd","House of the Dead : Scarlet Down"}
            };

            Dictionary<String, String> _Es3Roms = new Dictionary<String, String>(){
                {"tc5","Time Crisis 5"}
            };

            Dictionary<String, String> _FlycastRoms = new Dictionary<String, String>(){
                //{"braveff","Brave Fire Fighters"},
                {"claychal","SEGA Clay Challenge"},
                {"confmiss","Confidential Mission"},
                {"deathcox","Death Crimson OX"},
                {"hotd2","House of the Dead II (USA)"},
                {"hotd2o","House of the Dead II"},
                {"hotd2p","House of the Dead II (Prototype)"},
                {"lupinsho","Lupin the Third"},
                //{"manicpnc","Manic Panic Ghosts"},
                {"mok","The Maze of the Kings"},
                {"ninjaslt","Ninja Assault (World)"},
                {"ninjaslta","Ninja Assault (Asia)"},
                {"ninjasltj","Ninja Assault (Japan)"},
                {"ninjasltu","Ninja Assault (US)"},
                //{"pokasuka","Pokasuka Ghost"},
                {"rangrmsn","Ranger Mission"},
                {"sprtshot","Sports Shooting USA"},
                {"xtrmhunt","Extreme Hunting"},
                {"xtrmhnt2","Extreme Hunting 2"}
            };

            Dictionary<String, String> _SegaNuRoms = new Dictionary<String, String>(){
                {"lma","Luigi's Mansion Arcade"}
            };

            Dictionary<String, String> _WindowsRoms = new Dictionary<String, String>(){
                //{"bha","Buck Hunt Arcade PC"}
                {"hotdra","House of the Dead Remake - Arcade Mod"}
            };
            
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLower().Equals("-h") || args[i].ToLower().Equals("--help") || args[i].ToLower().Equals("-?"))
                    {
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine("DemulShooterX64 v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString());
                        DateTime CompileTime = new DateTime(Builtin.CompileTime, DateTimeKind.Utc);
                        String CompileDate = CompileTime.ToString("MMMM d", new CultureInfo("en-US"));
                        CompileDate = String.Format("{0}{1}, {2}", CompileDate, GetDaySuffix(CompileTime.Day), CompileTime.ToString("yyyy"));
                        Console.WriteLine("Build date : " + CompileDate);
                        Console.WriteLine("");
                        Console.WriteLine("usage : DemulShooterX64.exe -target=[target] -rom=[rom] [options]");
                        Console.WriteLine("");
                        Console.WriteLine("Supported [target] :");
                        DisplayDictionnaryList(_SystemTargets);
                        Console.WriteLine("");
                        Console.WriteLine("Supported [rom] :");
                        Console.WriteLine("Adrenaline Amusements Games roms :");
                        DisplayDictionnaryList(_AagamesRoms);
                        Console.WriteLine("");
                        Console.WriteLine("ALLS roms :");
                        DisplayDictionnaryList(_AllsRoms);
                        Console.WriteLine("");
                        Console.WriteLine("ES3 roms :");
                        DisplayDictionnaryList(_Es3Roms);
                        Console.WriteLine("");
                        Console.WriteLine("Flycast roms :");
                        DisplayDictionnaryList(_FlycastRoms);
                        Console.WriteLine("");
                        Console.WriteLine("SEGA Nu roms :");
                        DisplayDictionnaryList(_SegaNuRoms);
                        Console.WriteLine("");
                        Console.WriteLine("Windows games :");
                        DisplayDictionnaryList(_WindowsRoms);
                        Console.WriteLine("");
                        Console.WriteLine("Supported [options] :");
                        Console.WriteLine(" -noinput \tDisable any input hack");
                        Console.WriteLine(" -nocrosshair \tHide in-game crosshair (Only for Unity-based Games");
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
                        if (!CheckParameter(strTarget, _SystemTargets))
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
                        if (strTarget.StartsWith("aagames"))
                        {
                            if (!CheckParameter(strRom, _AagamesRoms))
                            {
                                Console.WriteLine("\n\n\tUnsupported Adrenaline Amusements Game rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("alls"))
                        {
                            if (!CheckParameter(strRom, _AllsRoms))
                            {
                                Console.WriteLine("Unsupported SEGA ALLS rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("es3"))
                        {
                            if (!CheckParameter(strRom, _Es3Roms))
                            {
                                Console.WriteLine("Unsupported Namco ES3 rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("flycast"))
                        {
                            if (!CheckParameter(strRom, _FlycastRoms))
                            {
                                Console.WriteLine("Unsupported Flycast rom parameter : \"" + strRom + "\". See help for supported roms list");
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
                Console.WriteLine("\n\n\tMissing parameter!\n\tSee help with DemulShooterX64.exe -h for correct usage.");
                ExitConsole();
            }

        }

        static string GetDaySuffix(int day)
        {
            switch (day)
            {
                case 1:
                case 21:
                case 31:
                    return "st";
                case 2:
                case 22:
                    return "nd";
                case 3:
                case 23:
                    return "rd";
                default:
                    return "th";
            }
        }

        static void DisplayDictionnaryList(Dictionary<String, String> list)
        {
            foreach (KeyValuePair<String, String> item in list)
            {
                Console.Write(" " + item.Key);
                for (int t = 0; t < (11 - item.Key.Length); t++)
                    Console.Write(" ");
                Console.WriteLine(item.Value);
            }
        }

        static bool CheckParameter(String param, Dictionary<String, String> list)
        {
            foreach (KeyValuePair<String, String> Item in list)
            {
                if (param.Equals(Item.Key))
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
