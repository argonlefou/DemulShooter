using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
        
        const uint  WM_CHAR                 = 0x0102;
        const int   VK_ENTER                = 0x0D;
        const int   ATTACH_PARENT_PROCESS   = -1;

        static IntPtr ConsoleHwnd = IntPtr.Zero;

        static String strTarget = string.Empty;
        static String strRom = string.Empty;

        static void Main(string[] args)
        {
            // Attach to the parent process via AttachConsole SDK call
            AttachConsole(ATTACH_PARENT_PROCESS);
            ConsoleHwnd = GetConsoleWindow();

            bool isVerbose = false;

            String[] _Targets = new string[] { "chihiro", "demul057", "demul058", "demul07a", "dolphin5", "globalvr", "lindbergh", "model2", "ringwide", "ttx", "windows", "wip" };
            String[] _DemulRoms = new string[] { "braveff", "claychal", "confmiss", "deathcox", "hotd2", "hotd2o", "hotd2p", "lupinsho", "manicpnc", "mok", "ninjaslt", "ninjaslta", "ninjasltj", "ninjasltu", "pokasuka", "rangrmsn", "sprtshot", "xtrmhunt", "xtrmhnt2" };
            String[] _Model2Roms = new string[] { "bel", "gunblade", "hotd", "rchase2", "vcop", "vcop2" };
            String[] _WindowsRoms = new string[] { "artdead", "friction", "hfa", "hfa2p", "hfa2p", "hfss", "hfss2p", "hod2pc", "hod3pc", "hodo", "reload" };
            String[] _TTXRoms = new string[] { "bkbs", "sha", "eadp", "gattack4", "gsoz", "gsoz2p", "hmuseum", "hmuseum2", "mgungun2" };
            String[] _GlobalVrRoms = new string[] { "aliens", "farcry", "fearland" };
            String[] _LindberghRoms = new string[] { "2spicy", "hotd4", "lgj", "lgjsp", "rambo" };
            String[] _RingWideRoms = new string[] { "sgg", "lgi", "lgi3d", "og", "sdr", "tha" };
            String[] _ChihiroRoms = new string[] { "vcop3" };
            String[] _WipRoms = new string[] { "bestate", "wartran", "bhapc" };

            if (args.Length > 0)
            {      
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLower().Equals("-h") || args[i].ToLower().Equals("--help") || args[i].ToLower().Equals("-?"))
                    {
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine("DemulShooter v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString());
                        Console.WriteLine("Build date : January, 18th 2022");
                        Console.WriteLine("");
                        Console.WriteLine("usage : DemulShooter.exe -target=[target] -rom=[rom] [options]");
                        Console.WriteLine("");
                        Console.WriteLine("Supported [target] :");
                        Console.WriteLine("chihiro\t\tCxbx-Reloaded");
                        Console.WriteLine("demul057\tDemul v0.57");
                        Console.WriteLine("demul058\tDemul v0.582");
                        Console.WriteLine("demul07a\tDemul v0.7a 180428");
                        Console.WriteLine("dolphin5\tDolphin v5.0");
                        Console.WriteLine("globalvr\tGlobal VR");
                        Console.WriteLine("lindbergh\tTeknoParrot Loader");
                        Console.WriteLine("model2\t\tNebula Model2Emulator v1.1a");
                        Console.WriteLine("ringwide\tTeknoParrot Loader");
                        Console.WriteLine("ttx\t\tTaito Type X");
                        Console.WriteLine("windows\t\tWindows games");
                        Console.WriteLine("");
                        Console.WriteLine("Supported [rom] :");
                        Console.WriteLine("Chihiro roms :");
                        Console.WriteLine(" vcop3\t\tVirtua Cop 3");
                        Console.WriteLine("");
                        Console.WriteLine("Demul roms :");
                        Console.WriteLine(" braveff\tBrave Fire Fighters");
                        Console.WriteLine(" claychal\tSega Clay Challenge");
                        Console.WriteLine(" confmiss\tConfidential Mission");
                        Console.WriteLine(" deathcox\tDeath Crimson OX");
                        Console.WriteLine(" hotd2\t\tHouse of the Dead II (USA)");
                        Console.WriteLine(" hotd2o\t\tHouse of the Dead II");
                        Console.WriteLine(" hotd2p\t\tHouse of the Dead II (Prototype)");
                        Console.WriteLine(" lupinsho\tLupin the Third");
                        Console.WriteLine(" manicpnc\tManic Panic Ghosts");
                        Console.WriteLine(" mok\t\tThe Maze of the Kings");
                        Console.WriteLine(" ninjaslt\tNinja Assault (World)");
                        Console.WriteLine(" ninjaslta\tNinja Assault (Asia)");
                        Console.WriteLine(" ninjasltj\tNinja Assault (Japan)");
                        Console.WriteLine(" ninjasltu\tNinja Assault (US)");
                        Console.WriteLine(" pokasuka\tPokasuka Ghost");
                        Console.WriteLine(" rangrmsn\tRanger Mission");
                        Console.WriteLine(" sprtshot\tSports Shooting USA");
                        Console.WriteLine(" xtrmhunt\tExtreme Hunting");
                        Console.WriteLine(" xtrmhnt2\tExtreme Hunting 2");
                        Console.WriteLine("");
                        Console.WriteLine("Dolphin roms :");
                        Console.WriteLine(" - Parameter not used - ");
                        Console.WriteLine("");
                        Console.WriteLine("Global VR Games :");
                        Console.WriteLine(" aliens\t\tAliens Extermination Dehasped (2nd dump, x86 and x64, no need for VM)");
                        Console.WriteLine(" fearland\tFright Fear Land");
                        Console.WriteLine("");
                        Console.WriteLine("Lindbergh roms :");
                        Console.WriteLine(" 2spicy\t\tToo Spicy");
                        Console.WriteLine(" hotd4\t\tHouse of The Dead 4");
                        Console.WriteLine(" lgj\t\tLet's Go Jungle");
                        Console.WriteLine(" lgjsp\t\tLet's Go Jungle Special");
                        Console.WriteLine(" rambo\t\tRambo Arcade");
                        Console.WriteLine("");
                        Console.WriteLine("Model2 roms :");
                        Console.WriteLine(" bel\t\tBehind Enemy Lines");
                        Console.WriteLine(" gunblade\tGunblade NY");
                        Console.WriteLine(" hotd\t\tHouse of the dead");
                        Console.WriteLine(" rchase2\tRail Chase 2");
                        Console.WriteLine(" vcop\t\tVirtua Cop");
                        Console.WriteLine(" vcop2\t\tVirtua Cop 2");
                        Console.WriteLine("");
                        Console.WriteLine("Ringwide roms :");
                        Console.WriteLine(" sgg\t\tSega Golden Gun");
                        Console.WriteLine(" lgi\t\tLet's Go Island: Lost on the Island of Tropics");
                        Console.WriteLine(" lgi3d\t\tLet's Go Island 3D");
                        Console.WriteLine(" og\t\tOperation G.H.O.S.T.");
                        Console.WriteLine(" sdr\t\tSega Dream Riders");
                        Console.WriteLine(" tha\t\tTransformers Human Alliance");
                        Console.WriteLine("");
                        Console.WriteLine("Taito Type X Games :");
                        Console.WriteLine(" bkbs\t\tBlock King Ball Shooter");
                        Console.WriteLine(" eadp\t\tElevator Action Death Parade");
                        Console.WriteLine(" sha\t\tSilent Hill the Arcade");
                        Console.WriteLine(" gattack4\tGaia Attack 4");
                        Console.WriteLine(" gsoz\t\tGundam : Spirit of Zeon");
                        Console.WriteLine(" gsoz2p\t\tGundam : Spirit of Zeon (Dual Player)");
                        Console.WriteLine(" hmuseum\tHaunted Museum");
                        Console.WriteLine(" hmuseum2\tHaunted Museum 2");
                        Console.WriteLine(" mgungun2\tMusic Gun Gun! 2");
                        Console.WriteLine("");
                        Console.WriteLine("Windows Games :");
                        Console.WriteLine(" artdead\tArt Is Dead");
                        //Console.WriteLine(" bestate\tBlue Estate");
                        Console.WriteLine(" hfa\t\tHeavy Fire Afghanistan");
                        Console.WriteLine(" hfa2p\t\tHeavy Fire Afghanistan (Dual player)");
                        Console.WriteLine(" hfss\t\tHeavy Fire Shattered Spear");
                        Console.WriteLine(" hfss2p\t\tHeavy Fire Shattered Spear (Dual player)");
                        Console.WriteLine(" hod2pc\t\tHouse of the Dead 2");
                        Console.WriteLine(" hod3pc\t\tHouse of the Dead 3");
                        Console.WriteLine(" hodo\t\tHouse of the Dead Overkill");
                        Console.WriteLine(" reload\t\tReload");
                        Console.WriteLine("");
                        Console.WriteLine("Supported [options] :");
                        Console.WriteLine(" -noresize \tFix Demul exit fullscreen bug, when shooting upper left corner");
                        Console.WriteLine(" \t\tNote : Demul GUI will not respond anymore to clicks");
                        Console.WriteLine(" -widescreen \tDemul Widescreen hack");
                        Console.WriteLine(" -ddinumber \tDolphin's DirectInput number for P2 device");
                        Console.WriteLine(" -noautofire \tDisable in-game autofire for SEGA Golden Gun");
                        Console.WriteLine(" -noautoreload \tDisable ingame automatic reload for hod3pc");
                        Console.WriteLine(" -noguns \tRemove guns on screen for hod3pc (like real arcade machine)");
                        Console.WriteLine(" -nocrosshair \tHide in-game crosshair (Only for \"Reload\" on Windows and \"Rambo\" on Lindbergh");
                        Console.WriteLine(" -hardffl \tAlternative gameplay for Fright Fear Land / Haunted Museum 2(see README.TXT)");

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
                            Console.WriteLine("\n\n\tUnsupported [target] parameter : \"" + strTarget + "\". See help for supported targets");
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
                        if (strTarget.StartsWith("chihiro"))
                        {
                            if (!CheckParameter(strRom, _ChihiroRoms))
                            {
                                Console.WriteLine("\n\n\tUnsupported Chihiro rom parameter : \"" + strRom + "\". See help for supported roms list");
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
                        else if (strTarget.Equals("globalvr"))
                        {
                            if (!CheckParameter(strRom, _GlobalVrRoms))
                            {
                                Console.WriteLine("Unsupported GlobalVR rom parameter : \"" + strRom + "\". See help for supported roms list");
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
                        else if (strTarget.Equals("ringwide"))
                        {
                            if (!CheckParameter(strRom, _RingWideRoms))
                            {
                                Console.WriteLine("Unsupported RingWide rom parameter : \"" + strRom + "\". See help for supported roms list");
                                ExitConsole();
                            }
                        }
                        else if (strTarget.Equals("ttx"))
                        {
                            if (!CheckParameter(strRom, _TTXRoms))
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
                        else if (strTarget.Equals("wip"))
                        {
                            if (!CheckParameter(strRom, _WipRoms))
                            {
                                Console.WriteLine("Unsupported W.I.P rom parameter : \"" + strRom + "\". See help for supported roms list");
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
                Application.Run(new DemulShooterWindow(args, isVerbose));
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
