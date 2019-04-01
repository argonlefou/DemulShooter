using System;
using System.Windows.Forms;

namespace DemulShooter
{    
    static class Program
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool FreeConsole();
        /*[System.Runtime.InteropServices.DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        internal static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);
        internal const int VK_RETURN = 0x0D;
        internal const int WM_KEYDOWN = 0x100;*/
        private const int ATTACH_PARENT_PROCESS = -1;

        /// <summary>
        /// Application entry point
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool isVerbose = false;
            string _target = string.Empty;

            string[] _Targets = new string[] { "chihiro", "demul057", "demul058", "demul07a", "dolphin4", "dolphin5", "globalvr", "lindbergh", "model2", "model2m", "ringwide", "ttx", "windows", "wip" };
            string[] _DemulRoms = new string[] { "braveff", "claychal", "confmiss", "deathcox", "hotd2", "hotd2o", "hotd2p", "lupinsho", "manicpnc", "mok", "ninjaslt", "ninjaslta", "ninjasltj", "ninjasltu", "pokasuka", "rangrmsn", "sprtshot", "xtrmhunt", "xtrmhnt2" };
            string[] _Model2Roms = new string[] { "bel", "gunblade", "hotd", "rchase2", "vcop", "vcop2" };
            string[] _WindowsRoms = new string[] { "artdead", "hfa", "hfa2p", "hfa_s", "hfa2p_s", "hfss", "hfss2p", "hfss_s", "hfss2p_s", "hod2pc", "hod3pc", "reload" };
            string[] _TTXRoms = new string[] { "sha", "eadp", "gattack4", "gsoz", "gsoz2p", "hmuseum", "hmuseum2", "mgungun2" };
            string[] _GlobalVrRoms = new string[] { "aliens", "alienshasp", "farcry", "fearland" };
            string[] _LindberghRoms = new string[] { "hotd4", "lgj" };
            string[] _RingWideRoms = new string[] { "sgg", "lgi", "lgi3d", "og", "sdr" };
            string[] _ChihiroRoms = new string[] { "vcop3" };
            string[] _WipRoms = new string[] { "bestate", "wartran", "bhapc"};
        
            if (args.Length > 0)
            {
                // Do command line/silent logic here...
                // Attach to the parent process via AttachConsole SDK call
                AttachConsole(ATTACH_PARENT_PROCESS);
                
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLower().Equals("-h") || args[i].ToLower().Equals("--help") || args[i].ToLower().Equals("-?"))
                    {
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine("DemulShooter v" + System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString());
                        Console.WriteLine("Build date : March, 29th 2019");
                        Console.WriteLine("");
                        Console.WriteLine("usage : DemulShooter.exe -target=[target] -rom=[rom] [options]");
                        Console.WriteLine("");
                        Console.WriteLine("Supported [target] :");
                        Console.WriteLine("chihiro\t\tCxbx-Reloaded 001100eaf (30 Oct 2018)");
                        Console.WriteLine("demul057\tDemul v0.57");
                        Console.WriteLine("demul058\tDemul v0.582");
                        Console.WriteLine("demul07a\tDemul v0.7a 180428");
                        Console.WriteLine("dolphin4\tDolphin v4.0.2");
                        Console.WriteLine("dolphin5\tDolphin v5.0");
                        Console.WriteLine("globalvr\tGlobal VR");
                        Console.WriteLine("model2\t\tNebula Model2Emulator (emulator.exe) v1.1a");
                        Console.WriteLine("model2m\t\tNebula Model2Emulator (emulator_multicpu.exe) v1.1a");
                        Console.WriteLine("ringwide\tTeknoParrot Loader");
                        Console.WriteLine("lindbergh\tTeknoParrot Loader");
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
                        Console.WriteLine(" alienshasp\tAliens Extermination (1st dump, x86 only)");
                        Console.WriteLine(" aliens\t\tAliens Extermination Dehasped (2nd dump, x86 and x64, no need for VM)");
                        Console.WriteLine(" fearland\tFright Fear Land");
                        Console.WriteLine("");
                        Console.WriteLine("Lindbergh roms (TeknoParrot 1.93 only) :");
                        Console.WriteLine(" hotd4\t\tHouse of The Dead 4");
                        Console.WriteLine(" lgj\t\tLet's Go Jungle");
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
                        Console.WriteLine("");
                        Console.WriteLine("Taito Type X Games :");
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
                        Console.WriteLine(" hfa_s\t\tHeavy Fire Afghanistan [STEAM]");
                        Console.WriteLine(" hfa2p_s\tHeavy Fire Afghanistan (Dual player) [STEAM]");
                        Console.WriteLine(" hfss\t\tHeavy Fire Shattered Spear");
                        Console.WriteLine(" hfss2p\t\tHeavy Fire Shattered Spear (Dual player)");
                        Console.WriteLine(" hfss_s\t\tHeavy Fire Shattered Spear [STEAM]");
                        Console.WriteLine(" hfss2p_s\tHeavy Fire Shattered Spear (Dual player) [STEAM]");
                        Console.WriteLine(" hod2pc\t\tHouse of the Dead 2");
                        Console.WriteLine(" hod3pc\t\tHouse of the Dead 3");
                        Console.WriteLine(" reload\t\tReload");
                        Console.WriteLine("");
                        Console.WriteLine("Supported [options] :");
                        Console.WriteLine(" -noresize \tFix Demul exit fullscreen bug, when shooting upper left corner");
                        Console.WriteLine(" \t\tNote : Demul GUI will not respond anymore to clicks");
                        Console.WriteLine(" -widescreen \tDemul Widescreen hack");
                        Console.WriteLine(" -ddinumber \tDolphin's DirectInput number for P2 device");
                        Console.WriteLine(" -noautoreload \tDisable ingame automatic reload for hod3pc");
                        Console.WriteLine(" -noguns \tRemove guns on screen for hod3pc (like real arcade machine)");
                        Console.WriteLine(" -nocrosshair \tHide in-game crosshair (Only for \"Reload\" game from MASTIFF");
                        Console.WriteLine(" -hardffl \tAlternative gameplay for Fright Fear Land / Haunted Museum 2(see README.TXT)");
                        Console.WriteLine(" -parrotloader \tTemporary hack for parrot loader (see README.TXT)");

                        Console.WriteLine(" -? -h --help\tShow this help");
                        Console.WriteLine(" -v --verbose\tEnable output to log file");
                        

                        FreeConsole();
                        //Dangerous : send ENTER to active window
                        System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                        Environment.Exit(0);
                    }
                    else if (args[i].ToLower().Equals("-v") || args[i].ToLower().Equals("--verbose"))
                    {
                        isVerbose = true;
                    }
           
                    else if (args[i].ToLower().StartsWith("-target"))
                    {
                        _target = (args[i].ToLower().Split('='))[1].Trim();
                        if (!CheckParameter(_target, _Targets))
                        {
                            Console.WriteLine("\nUnsupported [target] parameter : \"" + _target + "\". See help for supported targets");
                            //Dangerous : send ENTER to active window
                            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                            Environment.Exit(0);
                        }
                    }
                }
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLower().StartsWith("-rom"))
                    {
                        string rom = (args[i].ToLower().Split('='))[1].Trim();
                        if (_target.StartsWith("chihiro"))
                        {
                            if (!CheckParameter(rom, _ChihiroRoms))
                            {
                                Console.WriteLine("Unsupported Chihiro rom parameter : \"" + rom + "\". See help for supported roms list");
                                //Dangerous : send ENTER to active window
                                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                                Environment.Exit(0);
                            }
                        }
                        else if (_target.StartsWith("demul"))
                        {
                            if (!CheckParameter(rom, _DemulRoms))
                            {
                                Console.WriteLine("Unsupported Demul rom parameter : \"" + rom + "\". See help for supported roms list");
                                //Dangerous : send ENTER to active window
                                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                                Environment.Exit(0);
                            }
                        }
                        else if (_target.StartsWith("lindbergh"))
                        {
                            if (!CheckParameter(rom, _LindberghRoms))
                            {
                                Console.WriteLine("Unsupported Lindbergh rom parameter : \"" + rom + "\". See help for supported roms list");
                                //Dangerous : send ENTER to active window
                                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                                Environment.Exit(0);
                            }
                        }
                        else if (_target.StartsWith("model2"))
                        {
                            if (!CheckParameter(rom, _Model2Roms))
                            {
                                Console.WriteLine("Unsupported Model2 rom parameter : \"" + rom + "\". See help for supported roms list");
                                //Dangerous : send ENTER to active window
                                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                                Environment.Exit(0);
                            }
                        }
                        else if (_target.Equals("windows"))
                        {
                            if (!CheckParameter(rom, _WindowsRoms))                            
                            {
                                Console.WriteLine("Unsupported Windows rom parameter : \"" + rom + "\". See help for supported roms list");
                                //Dangerous : send ENTER to active window
                                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                                Environment.Exit(0);
                            }
                        }
                        else if (_target.Equals("ttx"))
                        {
                            if (!CheckParameter(rom, _TTXRoms))
                            {
                                Console.WriteLine("Unsupported Taito Type X rom parameter : \"" + rom + "\". See help for supported roms list");
                                //Dangerous : send ENTER to active window
                                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                                Environment.Exit(0);
                            }
                        }
                        else if (_target.Equals("globalvr"))
                        {
                            if (!CheckParameter(rom, _GlobalVrRoms))
                            {
                                Console.WriteLine("Unsupported GlobalVR rom parameter : \"" + rom + "\". See help for supported roms list");
                                //Dangerous : send ENTER to active window
                                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                                Environment.Exit(0);
                            }
                        }
                        else if (_target.Equals("ringwide"))
                        {
                            if (!CheckParameter(rom, _RingWideRoms))
                            {
                                Console.WriteLine("Unsupported RingWide rom parameter : \"" + rom + "\". See help for supported roms list");
                                //Dangerous : send ENTER to active window
                                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                                Environment.Exit(0);
                            }
                        }
                    }
                }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);                       
            Application.Run(new WndParam(isVerbose));
        }

        static bool CheckParameter(string param, string[] list)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (param == list[i])
                    return true;
            }
            return false;
        }

        
    }
}
