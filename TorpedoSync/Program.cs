using RaptorDB;
using System;
using System.IO;

namespace TorpedoSync
{
    class Program
    {
        private static void help()
        {
            Console.WriteLine("Command line parameters:");
            Console.WriteLine("\t/i\tInstalls the service");
            Console.WriteLine("\t/u\tUninstalls the service");
            Console.WriteLine("\t/r\tRun in console");
        }

        static void Main(string[] args)
        {
            LogManager.Configure("LOGS\\log.txt".Replace('\\', Path.DirectorySeparatorChar), 500, false);
            LogManager.SetLogLevel(2);
            if (args.Length == 1)
            {
                DirectServiceInstaller srv = new DirectServiceInstaller();
                switch (args[0].ToUpper())
                {
                    case "/I":
                        Service.serviceInstall(srv);
                        return;

                    case "/U":
                        Service.serviceUninstall(srv);
                        return;

                    case "/R":
                        Service.runConsole();
                        return;

                    case "/S":
                        Service.runService();
                        return;
                    case "/H":
                    case "/?":
                    case "-H":
                    case "-?":
                    case "?":
                        help();
                        return;
                }
            }
            Service.runConsole();
        }
    }
}
