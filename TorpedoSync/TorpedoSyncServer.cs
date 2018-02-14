using fastBinaryJSON;
using fastJSON;
using RaptorDB;
using RaptorDB.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

namespace TorpedoSync
{
    class TorpedoSyncServer
    {
        public TorpedoSyncServer()
        {
            BJSON.Parameters.UseUTCDateTime = true;
            NetworkClient.Config.KillConnectionSeconds = 1;

            Directory.CreateDirectory("Computers");
            ReadConfigFiles();

            _tcpport = Global.TCPPort;
            _udpport = Global.UDPPort;
            _webport = Global.WebPort;

            PrintHeaderConsoleMessages();

            _log.Debug("Starting Server...");
            _log.Debug("UDP server on port = " + _udpport);
            Task.Factory.StartNew(() => UDP.StartUDPServer(_udpport));
            _log.Debug("TCP server on port = " + _tcpport);
            _tcpserver.Start(_tcpport, handler);
            _log.Debug("WEB server on port = " + _webport);

            ClientCommands.FillServerList();

            _webserver = new TorpedoWeb(
                _webport,
                Global.LocalOnlyWeb,
                Global.UseEmbeddedWebResources,
                StartQue,
                RemoveQue,
                GetConnInfo,
                System.Net.AuthenticationSchemes.Anonymous,
                "api");

            StartQueueProcessors();

            _timer.AutoReset = true;
            _timer.Elapsed += timer_elapsed;
            _timer.Start();
        }

        private void PrintHeaderConsoleMessages()
        {
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(this.GetType().Assembly.Location);
            string[] msg = new string[]
            {
                "-----------------------------------------------------",
                @" ______                     __     ____             ",
                @"/_  __/__  _______  ___ ___/ /__  / __/_ _____  ____",
                @" / / / _ \/ __/ _ \/ -_) _  / _ \_\ \/ // / _ \/ __/",
                @"/_/  \___/_/ / .__/\__/\_,_/\___/___/\_, /_//_/\__/ ",
                @"            /_/                     /___/           ",
                "-----------------------------------------------------",
                "TorpedoSync - Folder Sync Application",
                "Version      : " + this.GetType().Assembly.GetName().Version,
                "File Version : " + fvi.FileVersion,
                "-----------------------------------------------------"
            };

            foreach (var s in msg)
                Console.WriteLine(s);
        }

        private static ILog _log = LogManager.GetLogger(typeof(TorpedoSyncServer));
        private NetworkServer _tcpserver = new NetworkServer();
        private int _tcpport = 21001;
        private int _udpport = 21000;
        private int _webport = 88;
        private readonly Timer _timer = new Timer(10 * 1000); // 10 sec udp check 
        private List<QueueProcessor> _queprocessors = new List<QueueProcessor>();
        private TorpedoWeb _webserver;

        private DateTime _lastGC = DateTime.Now;
        private void timer_elapsed(object sender, ElapsedEventArgs e)
        {
            if (FastDateTime.Now.Subtract(_lastGC).TotalMinutes > Global.FreeMemoryTimerMin)
            {
                _log.Info("GC.......");
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                _log.Info("  GC Collection 0 = " + GC.CollectionCount(0));
                _log.Info("  GC Collection 1 = " + GC.CollectionCount(1));
                _log.Info("  GC Collection 2 = " + GC.CollectionCount(2));

                _lastGC = FastDateTime.Now;
            }
            Task.Factory.StartNew(ClientCommands.FillServerList);
        }

        private void ReadConfigFiles()
        {
            string path = Path.GetDirectoryName(this.GetType().Assembly.Location);
            if (path.EndsWith("" + Path.DirectorySeparatorChar) == false)
                path += Path.DirectorySeparatorChar;

            Global.EXEPath = path;
            // read settings
            if (File.Exists("settings.config"))
                JSON.FillObject(new Global(), File.ReadAllText("settings.config"));
            else
                ServerCommands.SaveConfig();
            foreach (var s in Global.Shares)
            {
                //if (s.id == null)
                //    s.id = Guid.NewGuid();
                if (s.ReadOnlyToken == "")
                    s.ReadOnlyToken = Guid.NewGuid().ToString();
                if (s.ReadWriteToken == "")
                    s.ReadWriteToken = Guid.NewGuid().ToString();
            }
            Global.isWindows = ("" + Environment.OSVersion).ToLower().Contains("windows");
        }

        private object handler(object indata)
        {
            ReturnPacket ret = new ReturnPacket();
            ret.OK = false;
            try
            {
                Packet request = (Packet)indata;
                COMMANDS c = (COMMANDS)Enum.Parse(typeof(COMMANDS), request.Command);
                object[] p = null;
                switch (c)
                {
                    case COMMANDS.Download:
                        p = (object[])request.Data;
                        ret.Data = ServerCommands.Download((string)p[0], (string)p[1], (SyncFile)p[2], (long)p[3], (int)p[4]);
                        ret.OK = true;
                        break;
                    case COMMANDS.GetServerInfo:
                        ret.Data = ServerCommands.GetServerInfo();
                        ret.OK = true;
                        break;
                    case COMMANDS.isConfirmed:
                        p = (object[])request.Data;
                        ret.Data = ServerCommands.isConfirmed((string)p[0], (string)p[1], (string)p[2]);
                        ret.OK = true;
                        break;
                    case COMMANDS.RegisterMachine:
                        p = (object[])request.Data;
                        ret.Data = ServerCommands.RegisterMachine((string)p[0], (string)p[1], (string)p[2], StartQue);
                        ret.OK = true;
                        break;
                    case COMMANDS.SyncReadOnly:
                        p = (object[])request.Data;
                        ret.Data = ServerCommands.SyncReadOnly(_queprocessors, (string)p[0], (string)p[1], (string)p[2], (State)p[3]);
                        ret.OK = true;
                        break;
                    case COMMANDS.SyncReadWrite:
                        p = (object[])request.Data;
                        ret.Data = ServerCommands.SyncReadWrite(_queprocessors, (string)p[0], (string)p[1], (string)p[2], (State)p[3], (Delta)p[4]);
                        ret.OK = true;
                        break;
                    case COMMANDS.isChanged:
                        p = (object[])request.Data;
                        ret.Data = isChanged((string)p[0], (string)p[1]);
                        ret.OK = true;
                        break;
                    case COMMANDS.CreateZip:
                        p = (object[])request.Data;
                        ret.Data = ServerCommands.CreateZip((string)p[0], (string)p[1], (object[])p[2]);
                        ret.OK = true;
                        break;
                    case COMMANDS.DownloadZip:
                        p = (object[])request.Data;
                        ret.Data = ServerCommands.DownloadZip((string)p[0], (string)p[1], (SyncFile)p[2], (long)p[3], (int)p[4]);
                        ret.OK = true;
                        break;
                    case COMMANDS.DeleteZip:
                        p = (object[])request.Data;
                        ServerCommands.DeleteZip((string)p[0], (string)p[1], (SyncFile)p[2]);
                        ret.OK = true;
                        break;
                    case COMMANDS.CanConnect:
                        ret.Data = ServerCommands.CanConnect((string)request.Data);
                        ret.OK = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                ret.OK = false;
                ret.Error = ex.ToString();
            }
            return ret;
        }

        private ConnectionInfo GetConnInfo(Connection cs)
        {
            var q = _queprocessors.Find(x => x.cs == cs);
            if (q != null)
            {
                return q.GetConnectionInfo();
            }
            return new ConnectionInfo();
        }

        private void RemoveQue(Connection cs)
        {
            var q = _queprocessors.Find(x => x.cs == cs);
            if (q != null)
            {
                _log.Debug($"deleting connection {cs.Name} to machine {cs.MachineName}");
                q.Shutdown();
                _queprocessors.Remove(q);

                Global.ConnectionList.Remove(cs);
                ServerCommands.SaveConnectionList();

                foreach (var f in Directory.GetFiles(cs.csfolder, cs.Name + ".*"))
                {
                    File.Delete(f);
                }
            }
        }

        private void StartQue(Connection cs)
        {
            var q = new QueueProcessor(Path.GetFullPath(cs.csfolder), cs);
            _queprocessors.Add(q);
        }

        private bool isChanged(string sharename, string clientmachiine)
        {
            foreach (var q in _queprocessors)
                if (q.cs.Name == sharename && q.cs.MachineName == clientmachiine)
                    return q.isChanged();
            return false;
        }

        private void ReadCompShares()
        {
            string _S = "" + Path.DirectorySeparatorChar;
            Global.ConnectionList = new List<Connection>();
            foreach (var f in Directory.GetFiles("Computers", "*.config", SearchOption.AllDirectories))
            {
                var cs = JSON.ToObject<Connection>(File.ReadAllText(f));
                cs.csfolder = Path.GetDirectoryName(f) + Path.DirectorySeparatorChar;
                cs.isClient = true;
                if (Global.Shares.Find(x => x.Name == cs.Name) != null)
                    cs.isClient = false;
                // read ignore list
                if (File.Exists(cs.Path + ".ts" + _S + ".ignore"))
                {
                    var lines = File.ReadAllLines(cs.Path + ".ts" + _S + ".ignore");
                    foreach (var l in lines)
                    {
                        var ss = l.Split('#');
                        var line = ss[0].Trim();
                        if (line != "")
                        {
                            cs.ignorelist.Add(line);
                        }
                    }
                }
                Global.ConnectionList.Add(cs);
            }
        }

        private void StartQueueProcessors()
        {
            ReadCompShares();

            foreach (var cs in Global.ConnectionList)
            {
                StartQue(cs);
            }
        }

        internal void Stop()
        {
            _log.Debug("Shutting down.");

            _webserver.Stop();

            _tcpserver.Stop();

            foreach (var q in _queprocessors)
                q.Shutdown();

            _timer.Stop();

            ServerCommands.SaveConfig();
            LogManager.Shutdown();
        }
    }
}
