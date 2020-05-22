using RaptorDB;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TorpedoSync
{
    class ClientCommands
    {
        private static ILog _log = LogManager.GetLogger(typeof(ServerCommands));
        private static List<ServerAddress> ServerList = new List<ServerAddress>();

        /// <summary>
        /// Connect to a server and register for a share access
        /// </summary>
        public static string Connect(string servername, string share, string token)
        {
            try
            {
                var s = GetServerIP(servername);
                if (s != null)
                {
                    Packet p = new Packet();
                    p.Command = "" + COMMANDS.RegisterMachine;
                    p.Data = new object[]
                    {
                        Environment.MachineName,
                        share,
                        token
                    };

                    var ret = (ReturnPacket)send(s.IP.ToString(), p);
                    return (string)ret.Data;
                }
            }
            catch { }
            return "";
        }

        public static Connection CanConnect(string token)
        {
            Connection cc = null;
            List<Task> tasks = new List<Task>();
            foreach (var s in ServerList)
            {
                var t = Task.Factory.StartNew(() =>
                {
                    var p = new Packet();
                    p.Command = "" + COMMANDS.CanConnect;
                    p.Data = token;

                    var ret = send("" + s.IP, p);
                    if (ret != null)
                    {
                        var c = ((ReturnPacket)ret).Data;
                        if (c != null)
                            cc = (Connection)c;
                    }
                });
                tasks.Add(t);
            }
            Task.WaitAll(tasks.ToArray());

            return cc;
        }

        public static bool isChanged(Connection share)
        {
            try
            {
                var s = GetServerIP(share.MachineName);
                if (s != null)
                {
                    Packet p = new Packet();
                    p.Command = "" + COMMANDS.isChanged;
                    p.Data = new object[]
                    {
                        share.Name,
                        Environment.MachineName
                    };

                    var ret = (ReturnPacket)send(s.IP.ToString(), p);
                    return (bool)ret.Data;
                }
            }
            catch { }
            return false;
        }

        public static DFile Download(Connection share, SyncFile file, long start, int size)
        {
            try
            {
                var s = GetServerIP(share.MachineName);
                if (s != null)
                {
                    Packet p = new Packet();
                    p.Command = "" + COMMANDS.Download;
                    p.Data = new object[]
                    {
                        share.Name,
                        share.Token,
                        file,
                        start,
                        size
                    };

                    var ret = (ReturnPacket)send(s.IP.ToString(), p);
                    if (ret.OK)
                        return (DFile)ret.Data;

                    return null;
                }
            }
            catch (Exception ex) { _log.Error(ex); }
            return null;
        }

        public static SyncFile CreateZip(Connection share, List<SyncFile> files)
        {
            try
            {
                var s = GetServerIP(share.MachineName);
                if (s != null)
                {
                    Packet p = new Packet();
                    p.Command = "" + COMMANDS.CreateZip;
                    p.Data = new object[]
                    {
                        share.Name,
                        share.Token,
                        files
                    };

                    var ret = (ReturnPacket)send(s.IP.ToString(), p);
                    if (ret.OK)
                        return (SyncFile)ret.Data;

                    return null;
                }
            }
            catch { }
            return null;
        }

        public static DFile DownloadZip(Connection share, SyncFile file, long start, int size)
        {
            try
            {
                var s = GetServerIP(share.MachineName);
                if (s != null)
                {
                    Packet p = new Packet();
                    p.Command = "" + COMMANDS.DownloadZip;
                    p.Data = new object[]
                    {
                        share.Name,
                        share.Token,
                        file,
                        start,
                        size
                    };

                    var ret = (ReturnPacket)send(s.IP.ToString(), p);
                    if (ret.OK)
                        return (DFile)ret.Data;

                    return null;
                }
            }
            catch { }
            return null;
        }

        public static void DeleteZip(Connection share, SyncFile file)
        {
            try
            {
                var s = GetServerIP(share.MachineName);
                if (s != null)
                {
                    Packet p = new Packet();
                    p.Command = "" + COMMANDS.DeleteZip;
                    p.Data = new object[]
                    {
                        share.Name,
                        share.Token,
                        file
                    };

                    var ret = (ReturnPacket)send(s.IP.ToString(), p);
                }
            }
            catch { }
        }

        public static bool isConfirmed(Connection share)
        {
            try
            {
                var s = GetServerIP(share.MachineName);
                if (s != null)
                {
                    Packet p = new Packet();
                    p.Command = "" + COMMANDS.isConfirmed;
                    p.Data = new object[]
                    {
                        Environment.MachineName,
                        share.Name,
                        share.Token
                    };

                    var ret = (ReturnPacket)send(s.IP.ToString(), p);
                    return (bool)ret.Data;
                }
            }
            catch { }
            return false;
        }

        public static Delta SyncMeReadonly(State mystate, Connection share)
        {
            try
            {
                var s = GetServerIP(share.MachineName);
                if (s != null)
                {
                    Packet p = new Packet();
                    p.Command = "" + COMMANDS.SyncReadOnly;
                    p.Data = new object[]
                    {
                        Environment.MachineName,
                        share.Name,
                        share.Token,
                        mystate
                    };
                    p.Token = "";

                    var ret = (ReturnPacket)send(s.IP.ToString(), p);
                    Delta d = null;
                    if (ret.OK)
                        d = (Delta)ret.Data;

                    return d;
                }
            }
            catch (Exception ex) { _log.Error(ex); }
            //Console.Write
            _log.Info("syncread null");
            return null;
        }

        public static Delta SyncMeReadWrite(State mystate, Delta mydelta, Connection share)
        {
            try
            {
                var s = GetServerIP(share.MachineName);
                if (s != null)
                {
                    Packet p = new Packet();
                    p.Command = "" + COMMANDS.SyncReadWrite;
                    p.Data = new object[]
                    {
                        Environment.MachineName,
                        share.Name,
                        share.Token,
                        mystate,
                        mydelta
                    };
                    p.Token = "";

                    var ret = (ReturnPacket)send(s.IP.ToString(), p);
                    Delta d = null;
                    if (ret.OK)
                        d = (Delta)ret.Data;

                    return d;
                }
            }
            catch (Exception ex) { _log.Error(ex); }
            _log.Info("syncrw null");

            return null;
        }

        //public static ServerInfo GetServerInfo(string ip)
        //{
        //    Packet p = new Packet();
        //    p.Command = "" + COMMANDS.GetServerInfo;

        //    ReturnPacket ret = (ReturnPacket)send(ip, p);

        //    return (ServerInfo)ret.Data;
        //}

        private static object _lock = new object();
        public static void FillServerList()
        {
            int c = ServerList.Count;
            var l = UDP.DoUDPSearch();
            lock (_lock)
            {
                ServerList = l;
            }
            if (l.Count != c)
            {
                //Console.WriteLine
                _log.Info("List of currently connected machines :");
                foreach (var i in ServerList)
                {
                    //Console.WriteLine
                    _log.Info("  Connected to : " + i.IP);
                }
            }
        }

        public static ServerAddress GetServerIP(string name)
        {
            var retry = 0;
            ServerAddress i = null;
            while (i == null && retry < 5)
            {
                lock (_lock)
                    i = ServerList.Find(x => x.Name == name);
                if (i == null)
                {
                    System.Threading.Thread.Sleep(1000);
                    retry++;
                }
            }
            return i;
        }

        public static bool isConnected(string mainServer)
        {
            // check udp for mainserver
            var s = GetServerIP(mainServer);
            if (s != null)
                return true;
            else
                return false;
        }

        private static object send(string server, object data)
        {
            NetworkClient nc = new NetworkClient(server, Global.TCPPort);
            object ret = null;
            try
            {
                nc.UseBJSON = true;
                ret = nc.Send(data);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            finally
            {
                nc.Close();
            }
            return ret;
        }
    }
}
