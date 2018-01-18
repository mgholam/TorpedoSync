using System;
using System.Reflection;
using System.ServiceProcess;
using System.IO;
using RaptorDB;
using System.Diagnostics;

namespace TorpedoSync
{
    internal class Service : ServiceBase
    {
        private static ILog _log = LogManager.GetLogger(typeof(Service));
        static TorpedoSyncServer _server;
        const string _serviceName = "TorpedoSync";
        const string _serviceDescription = "Torpedo Sync Server";


        protected override void OnStart(string[] args)
        {
            string s = Path.GetDirectoryName(this.GetType().Assembly.Location);
            Directory.SetCurrentDirectory(s);

            _path = s;
            if (_path.EndsWith("" + Path.DirectorySeparatorChar) == false) _path += Path.DirectorySeparatorChar;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            _server = new TorpedoSyncServer();
        }

        protected override void OnStop()
        {
            base.RequestAdditionalTime(2 * 60 * 1000);
            _server.Stop();
        }

        public static void runConsole()
        {
            LogManager.ConsoleMode();
            _path = Path.GetDirectoryName(typeof(Service).Assembly.Location);
            Directory.SetCurrentDirectory(_path);

            if (_path.EndsWith("" + Path.DirectorySeparatorChar) == false) _path += Path.DirectorySeparatorChar;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    
            _server = new TorpedoSyncServer();
            Console.WriteLine("\r\n\r\nPress 'q' to exit...");
            if (Global.isWindows && Global.StartWebUI)
                Process.Start("http://localhost:" + Global.WebPort);
            Console.CancelKeyPress += Console_CancelKeyPress;
            while (true)
            {
                var i = Console.ReadKey();
                if (i.Key == ConsoleKey.Q)
                    break;
            }
            _server.Stop();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            _server.Stop();
            Environment.Exit(0);
        }

        public static void runService()
        {
            ServiceBase.Run(new Service());
        }

        public static void serviceInstall(DirectServiceInstaller srv)
        {
            if (srv.InstallService(Assembly.GetEntryAssembly().Location + " /s", _serviceName, _serviceDescription))
            {
                Console.WriteLine("Service installed.");
            }
            else
            {
                Console.WriteLine("There was a problem with installation.");
            }
        }

        public static void serviceUninstall(DirectServiceInstaller srv)
        {
            if (srv.UnInstallService(_serviceName))
            {
                Console.WriteLine("Service uninstalled.");
            }
            else
            {
                Console.WriteLine("There was a problem with uninstallation.");
            }
        }

        static string _path;
        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (File.Exists(args.Name))
                return Assembly.LoadFrom(args.Name);
            string[] ss = args.Name.Split(',');
            string fname = ss[0] + ".dll";
            if (File.Exists(fname))
                return Assembly.LoadFrom(fname);
            else if (File.Exists(_path + "bin" + Path.DirectorySeparatorChar + fname))
                return Assembly.LoadFrom(_path + "bin" + Path.DirectorySeparatorChar + fname);

            else return null;
        }
    }
}

