using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Globalization;
using System.Collections.Generic;
namespace DemulShooter
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
        static String strCustomTargetProcessName = string.Empty;
        public static String CustomTargetProcessName
        { get {return strCustomTargetProcessName;} }


        static void Main(string[] args)
        {
            // Attach to the parent process via AttachConsole SDK call
            AttachConsole(ATTACH_PARENT_PROCESS);
            ConsoleHwnd = GetConsoleWindow();

            bool isVerbose = false;
            bool isTrace = false;

            Dictionary<String,String> _SystemTargets = new Dictionary<String,String> (){
                {"chihiro","Cxbx-Reloaded"},
                {"coastal","Coastal Arcade"},
                {"demul057","Demul v0.57"},
                {"demul058","Demul v0.582"},
                {"demul07a","Demul v0.7a 180428"},
                {"dolphin5","Dolphin v5.0"},
                {"es4","Namco ES4 games"},
                {"gamewax","Gamewax Arcade"},
                {"globalvr","Global VR"},
                {"ice","ICE Games"},
                {"konami","Konami Arcade"},
                {"lindbergh","TeknoParrot Loader"},
                {"model2","Nebula Model2Emulator v1.1a"},
                {"ppmarket","P&P Marketing Arcade"},
                {"rawthrill","TeknoParrot Loader"},
                {"ringedge2", "RingEdge2 Games"},
                {"ringwide","TeknoParrot Loader / JConfig"},
                {"ttx","Taito Type X"},
                {"windows","Windows games"}
            };            

            Dictionary<String, String> _ChihiroRoms = new Dictionary<String, String>(){
                {"vcop3","Virtua Cop 3"}
            };

            Dictionary<String, String> _CoastalRoms = new Dictionary<String, String>(){
                {"wws","Wild West Shootout"}
            };

            Dictionary<String, String> _DemulRoms = new Dictionary<String, String>(){
                {"braveff","Brave Fire Fighters"},
                {"claychal","SEGA Clay Challenge"},
                {"confmiss","Confidential Mission"},
                {"deathcox","Death Crimson OX"},
                {"hotd2","House of the Dead II (USA)"},
                {"hotd2o","House of the Dead II"},
                {"hotd2p","House of the Dead II (Prototype)"},
                {"lupinsho","Lupin the Third"},
                {"manicpnc","Manic Panic Ghosts"},
                {"mok","The Maze of the Kings"},
                {"ninjaslt","Ninja Assault (World)"},
                {"ninjaslta","Ninja Assault (Asia)"},
                {"ninjasltj","Ninja Assault (Japan)"},
                {"ninjasltu","Ninja Assault (US)"},
                {"pokasuka","Pokasuka Ghost"},
                {"rangrmsn","Ranger Mission"},
                {"sprtshot","Sports Shooting USA"},
                {"xtrmhunt","Extreme Hunting"},
                {"xtrmhnt2","Extreme Hunting 2"}
            };

            Dictionary<String, String> _Es4Roms = new Dictionary<String, String>(){
                {"pblankx","Point Blank X"}
            };

            Dictionary<String, String> _GamewaxRoms = new Dictionary<String, String>(){
                {"akuma","Akuma Mortis Immortal"}
            };

            Dictionary<String, String> _GlobalVrRoms = new Dictionary<String, String>(){
                {"aliens","Aliens Extermination Dehasped (2nd dump, x86 and x64, no need for VM)"},
                {"farcry","Far Cry : Paradise Lost"},
                {"fearland","Fright Fear Land"}
            };

            Dictionary<String, String> _IceRoms = new Dictionary<String, String>(){
                {"gbusters","Ghostbusters"}
            };

            Dictionary<String, String> _KonamiRoms = new Dictionary<String, String>(){
                {"hcv","Castlevania Arcade"},
                {"le3","Lethal Enforcers 3"},
                {"wartran","Wartran Troopers"}
            };

            Dictionary<String, String> _LindberghRoms = new Dictionary<String, String>(){
                {"2spicy","Too Spicy"},
                {"hotd4","House of The Dead 4"},
                {"hotd4sp","House of The Dead 4 : Special"},
                {"hotdex","House of The Dead : EX"},
                {"lgj","Let's Go Jungle"},
                {"lgjsp","Let's Go Jungle Special"},
                {"rambo","Rambo Arcade"}
            };

            Dictionary<String, String> _Model2Roms = new Dictionary<String, String>(){
                {"bel","Behind Ennemy Lines"},
                {"gunblade","Gunblade NY"},
                {"hotd","House of the Dead"},
                {"rchase2","Rail Chase 2"},
                {"vcop","Virtua Cop"},
                {"vcop2","Virtua Cop 2"}

            };

            Dictionary<String, String> _PpMarketRoms = new Dictionary<String, String>(){
                {"policetr2","Police Trainer 2"},
            };

            Dictionary<String, String> _RawThrillRoms = new Dictionary<String, String>(){
                {"aa","Aliens Armageddon"},
                {"jp","Jurassic Park"},
                {"ts","Terminator Salvation"},
                {"wd","Walking Dead"}
            };

            Dictionary<String, String> _RingEdge2Roms = new Dictionary<String, String>(){
                {"tsr","Transformers : Shadow Rising"}
            };

            Dictionary<String, String> _RingSystemRoms = new Dictionary<String, String>(){
                {"sgg","SEGA golden guns"},
                {"lgi","Let's Go Island: Lost on the Island of Tropics"},
                {"lgi3d","Let's go Island 3D"},
                {"og","Operation G.H.O.S.T"},
                {"sdr","SEGA Dream Raiders"},
                {"tha","Transformers : Human Alliance"}
            };

            Dictionary<String, String> _TtxRoms = new Dictionary<String, String>(){
                {"bkbs","Block King Ball Shooter"},
                {"sha","Silent Hill Arcade"},
                {"eadp", "Elevator Action : Death Parade"},
                {"gattack4","Gaia Attack 4"},
                {"gsoz","Gundam : Spirit of Zeon"},
                {"gsoz2p", "Gundam : Spirit of Zeon (Dual Player)"},
                {"hmuseum","Haunted Museum"},
                {"hmuseum2","Haunted Museum 2"},
                {"mgungun2","Music Gun Gun! 2"}
            };

            Dictionary<String, String> _WindowsRoms = new Dictionary<String, String>(){
                {"ads","Alien Disco Safari"},
                {"artdead","Art Is Dead"},
                {"bugbust","Bug Busters"},
                {"friction","Friction"},
                {"hfa", "Heavy Fire Afghanistan"},
                {"hfa2p","Heavy Fire Afghanistan (dual Player)"},
                {"hfss","Heavy Fire Shaterred Spear"},
                {"hfss2p", "Heavy Fire Shaterred Spear (dual player)"},
                {"hod2pc","House of the Dead II"},
                {"hod3pc","House of the Dead III"},
                {"hodo","House of the Dead Overkill"},
                {"pgbeat", "Project Green Beat"},
                {"reload","Reload"},
            };

            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLower().Equals("-h") || args[i].ToLower().Equals("--help") || args[i].ToLower().Equals("-?"))
                    {
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine("DemulShooter v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString());
                        DateTime CompileTime = new DateTime(Builtin.CompileTime, DateTimeKind.Utc);
                        String CompileDate = CompileTime.ToString("MMMM d", new CultureInfo("en-US"));
                        CompileDate = String.Format("{0}{1}, {2}", CompileDate, GetDaySuffix(CompileTime.Day), CompileTime.ToString("yyyy"));
                        Console.WriteLine("Build date : " + CompileDate);
                        Console.WriteLine("");
                        Console.WriteLine("usage : DemulShooter.exe -target=[target] -rom=[rom] [options]");
                        Console.WriteLine("");
                        Console.WriteLine("Supported [target] :");
                        DisplayDictionnaryList(_SystemTargets);
                        Console.WriteLine("");
                        Console.WriteLine("Supported [rom] :");                        
                        Console.WriteLine("Coastal roms :");
                        DisplayDictionnaryList(_CoastalRoms);
                        Console.WriteLine("");
                        Console.WriteLine("Chihiro roms :");
                        DisplayDictionnaryList(_ChihiroRoms);
                        Console.WriteLine("");
                        Console.WriteLine("Demul roms :");
                        DisplayDictionnaryList(_DemulRoms);
                        Console.WriteLine("");
                        Console.WriteLine("Dolphin roms :");
                        Console.WriteLine(" - Parameter not used - ");
                        Console.WriteLine("");
                        Console.WriteLine("ES4 roms :");
                        DisplayDictionnaryList(_Es4Roms);
                        Console.WriteLine("");
                        Console.WriteLine("Global VR Games :");
                        DisplayDictionnaryList(_GlobalVrRoms);
                        Console.WriteLine("");
                        Console.WriteLine("Gamewax Games :");
                        DisplayDictionnaryList(_GamewaxRoms);
                        Console.WriteLine("");
                        Console.WriteLine("ICE Games :");
                        DisplayDictionnaryList(_IceRoms);
                        Console.WriteLine("");
                        Console.WriteLine("Konami Games :");
                        DisplayDictionnaryList(_KonamiRoms);
                        Console.WriteLine("");
                        Console.WriteLine("Lindbergh roms :");
                        DisplayDictionnaryList(_LindberghRoms);
                        Console.WriteLine("");
                        Console.WriteLine("Model2 roms :");
                        DisplayDictionnaryList(_Model2Roms);
                        Console.WriteLine("");
                        Console.WriteLine("P&P Marketing roms :");
                        DisplayDictionnaryList(_PpMarketRoms);
                        Console.WriteLine("");
                        Console.WriteLine("Raw Thrill roms :");
                        DisplayDictionnaryList(_RawThrillRoms);
                        Console.WriteLine("");
                        Console.WriteLine("Ringwide roms :");
                        DisplayDictionnaryList(_RingSystemRoms);
                        Console.WriteLine("");
                        Console.WriteLine("Ringedge2 roms :");
                        DisplayDictionnaryList(_RingEdge2Roms);
                        Console.WriteLine("");
                        Console.WriteLine("Taito Type X Games :");
                        DisplayDictionnaryList(_TtxRoms);
                        Console.WriteLine("");
                        Console.WriteLine("Windows Games :");
                        DisplayDictionnaryList(_WindowsRoms);
                        Console.WriteLine("");
                        Console.WriteLine("Supported [options] :");
                        Console.WriteLine(" -pname=[ProcessName] : change the name of the expected executable instead of expecting specific name like game.exe");
                        Console.WriteLine(" -ddinumber \tDolphin's DirectInput number for P2 device");
                        Console.WriteLine(" -hardffl \tAlternative gameplay for Fright Fear Land / Haunted Museum 2(see README.TXT)");
                        Console.WriteLine(" -noautofire \tDisable in-game autofire for SEGA Golden Gun");
                        Console.WriteLine(" -nocrosshair \tHide in-game crosshair (Only for \"Reload\" on Windows and \"Rambo\" on Lindbergh");
                        Console.WriteLine(" -noinput \tDisable any input hack");
                        Console.WriteLine(" -noresize \tFix Demul exit fullscreen bug, when shooting upper left corner");
                        Console.WriteLine(" \t\tNote : Demul GUI will not respond anymore to clicks");
                        Console.WriteLine(" -usesinglemouse \tUse standard mouse instead of Lightguns");
                        Console.WriteLine(" -widescreen \tDemul Widescreen hack");                        
                        Console.WriteLine(" -? -h --help\tShow this help");
                        Console.WriteLine(" -v --verbose\tEnable output to log file");

                        ExitConsole();
                    }
                    else if (args[i].ToLower().Equals("-v") || args[i].ToLower().Equals("--verbose"))
                    {
                        isVerbose = true;
                    }
                    else if (args[i].ToLower().Equals("-t") || args[i].ToLower().Equals("--trace"))
                    {
                        isTrace = true;
                    }

                    else if (args[i].ToLower().StartsWith("-target"))
                    {
                        strTarget = (args[i].ToLower().Split('='))[1].Trim();
                        if (strTarget != "wip")
                        {
                            if (!CheckParameter(strTarget, _SystemTargets))
                            {
                                Console.WriteLine("\n\n\tUnsupported [target] parameter : \"" + strTarget + "\". See help for supported targets");
                                ExitConsole();
                            }
                        }
                    }

                    else if (args[i].ToLower().StartsWith("-pname"))
                    {
                        strCustomTargetProcessName = (args[i].ToLower().Split('='))[1].Trim();
                        if (strCustomTargetProcessName.EndsWith(".exe"))
                            strCustomTargetProcessName = strCustomTargetProcessName.Substring(0, strCustomTargetProcessName.Length - 4);
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
                        if (strTarget.StartsWith("chihiro"))
                        {
                            if (!CheckParameter(strRom, _ChihiroRoms))
                            {
                                Console.WriteLine("\n\n\tUnsupported Chihiro rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.StartsWith("coastal"))
                        {
                            if (!CheckParameter(strRom, _CoastalRoms))
                            {
                                Console.WriteLine("Unsupported Coastal rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.StartsWith("demul"))
                        {
                            if (!CheckParameter(strRom, _DemulRoms))
                            {
                                Console.WriteLine("Unsupported Demul rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.StartsWith("es4"))
                        {
                            if (!CheckParameter(strRom, _Es4Roms))
                            {
                                Console.WriteLine("Unsupported ES4 rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("gamewax"))
                        {
                            if (!CheckParameter(strRom, _GamewaxRoms))
                            {
                                Console.WriteLine("Unsupported Gamewax rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("globalvr"))
                        {
                            if (!CheckParameter(strRom, _GlobalVrRoms))
                            {
                                Console.WriteLine("Unsupported GlobalVR rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("ice"))
                        {
                            if (!CheckParameter(strRom, _IceRoms))
                            {
                                Console.WriteLine("Unsupported ICE rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("konami"))
                        {
                            if (!CheckParameter(strRom, _KonamiRoms))
                            {
                                Console.WriteLine("Unsupported Konami rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.StartsWith("lindbergh"))
                        {
                            if (!CheckParameter(strRom, _LindberghRoms))
                            {
                                Console.WriteLine("Unsupported Lindbergh rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("model2"))
                        {
                            if (!CheckParameter(strRom, _Model2Roms))
                            {
                                Console.WriteLine("Unsupported Model2 rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("ppmarket"))
                        {
                            if (!CheckParameter(strRom, _PpMarketRoms))
                            {
                                Console.WriteLine("Unsupported P&P Marketing rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("rawthrill"))
                        {
                            if (!CheckParameter(strRom, _RawThrillRoms))
                            {
                                Console.WriteLine("Unsupported Raw Thrill rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("ringedge2"))
                        {
                            if (!CheckParameter(strRom, _RingEdge2Roms))
                            {
                                Console.WriteLine("Unsupported RingEdge2 rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("ringwide"))
                        {
                            if (!CheckParameter(strRom, _RingSystemRoms))
                            {
                                Console.WriteLine("Unsupported RingWide rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("ttx"))
                        {
                            if (!CheckParameter(strRom, _TtxRoms))
                            {
                                Console.WriteLine("Unsupported Taito Type X rom parameter : \"" + strRom + "\". See help for supported roms list");
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

                if (strRom == String.Empty && strTarget != "dolphin5")
                {
                    Console.WriteLine("\n\n\tNo [Rom] parameter specified. See help for supported targets");
                    ExitConsole();
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new DemulShooterWindow(args, isVerbose, isTrace));
            }
            else
            {
                Console.WriteLine("\n\n\tMissing parameter!\n\tSee help with DemulShooter.exe -h for correct usage.");
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

        static bool CheckParameter(String param, Dictionary<String,String> list)
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
