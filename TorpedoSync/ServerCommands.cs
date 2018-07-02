using fastJSON;
using RaptorDB;
using System;
using System.Collections.Generic;
using System.IO;

namespace TorpedoSync
{
    class ServerCommands
    {
        private static ILog _log = LogManager.GetLogger(typeof(ServerCommands));
        private static char _S = Path.DirectorySeparatorChar;


        public static Connection CanConnect(string token)
        {
            var s = Global.Shares.Find(x => x.ReadOnlyToken == token || x.ReadWriteToken == token);
            if (s != null)
            {
                var c = new Connection()
                {
                    Name = s.Name,
                    MachineName = Environment.MachineName,
                    Token = token,
                    isConfirmed = false,
                    ReadOnly = s.ReadOnlyToken == token,
                    isPaused = true // wait for confim
                };
                return c;
            }
            return null;
        }

        public static void SaveConnectionList()
        {
            foreach (var cs in Global.ConnectionList)
            {
                var fn = cs.csfolder + cs.Name + ".config";
                LockManager.GetLock(fn, () =>
                {
                    File.WriteAllText(fn, JSON.ToNiceJSON(cs, new JSONParameters { UseExtensions = false }));
                });
            }
        }

        public static DFile Download(string sharename, string token, SyncFile file, long start, int size)
        {
            var share = Global.Shares.Find(x => x.Name == sharename);
            DFile ret = new DFile();
            ret.err = DownloadError.NOTFOUND;
            if (share == null)
            {
                var cs = Global.ConnectionList.Find(x => x.Name == sharename);
                if (cs != null && cs.Token == token)
                    sendfile(file, start, size, cs.Path, ret);
            }
            else if (share.ReadOnlyToken == token || share.ReadWriteToken == token)
            {
                sendfile(file, start, size, share.Path, ret);
            }
            return ret;
        }

        public static SyncFile CreateZip(string sharename, string token, object[] files)
        {
            var share = Global.Shares.Find(x => x.Name == sharename);
            if (share == null)
            {
                var cs = Global.ConnectionList.Find(x => x.Name == sharename && x.Token == token);
                if (cs != null)
                    return internalcreatezip(files, cs.Path);
            }
            else if ((share.ReadOnlyToken == token || share.ReadWriteToken == token))
            {
                return internalcreatezip(files, share.Path);
            }
            return null;
        }

        private static SyncFile internalcreatezip(object[] files, string sharepath)
        {
            //Console.WriteLine
            _log.Info($"creating zip for share : {sharepath} ,file count = {files.Length}");
            string fn = Guid.NewGuid().ToString() + ".zip";
            string path = sharepath + ".ts" + _S + "Temp" + _S;
            Directory.CreateDirectory(path);

            var z = RaptorDB.Common.ZipStorer.Create(path + fn, "");
            foreach (var ff in files)
            {
                SyncFile f = (SyncFile)ff;
                try
                {
                    if (File.Exists(sharepath + f.F) == false)
                        continue;
                    //Console.WriteLine
                    _log.Info("ziping : " + f.F);
                    //_log.Debug("ziping : " + f.F);
                    using (var fs = new FileStream(sharepath + f.F, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        z.AddStream(RaptorDB.Common.ZipStorer.Compression.Deflate,
                            f.F,
                            fs, f.D, f.D.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                }
                catch (Exception ex) { _log.Error(ex); }
            }
            z.Close();
            return new SyncFile { F = fn, D = DateTime.Now, S = new FileInfo(path + fn).Length };
        }

        public static DFile DownloadZip(string sharename, string token, SyncFile file, long start, int size)
        {
            var share = Global.Shares.Find(x => x.Name == sharename);
            DFile ret = new DFile();
            ret.err = DownloadError.NOTFOUND;

            if (share == null)
            {
                var cs = Global.ConnectionList.Find(x => x.Name == sharename && x.Token == token);
                if (cs != null)
                {
                    string path = cs.Path + ".ts" + _S + "Temp" + _S;
                    sendzip(file.F, start, size, path, ret);
                }
            }
            else if (share.ReadOnlyToken == token || share.ReadWriteToken == token)
            {
                string path = share.Path + ".ts" + _S + "Temp" + _S;
                sendzip(file.F, start, size, path, ret);
            }
            return ret;
        }

        public static void DeleteZip(string sharename, string token, SyncFile file)
        {
            var share = Global.Shares.Find(x => x.Name == sharename);
            DFile ret = new DFile();
            ret.err = DownloadError.NOTFOUND;
            string path = share.Path + ".ts" + _S + "Temp" + _S;

            if (File.Exists(path + file.F))
                File.Delete(path + file.F);
        }

        private static void sendzip(string file, long start, int size, string path, DFile ret)
        {
            if (LongFile.Exists(path + file) == false)
            {
                ret.err = DownloadError.NOTFOUND;
                return;
            }

            FileStream fs = null;
            try
            {
                fs = new FileStream(path + file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] b = new byte[size];
                fs.Seek(start, SeekOrigin.Begin);
                fs.Read(b, 0, size);
                ret.err = DownloadError.OK;
                ret.data = b;
            }
            catch
            {
                ret.err = DownloadError.LOCKED;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
        }

        private static void sendfile(SyncFile file, long start, int size, string path, DFile ret)
        {
            if (LongFile.Exists(path + file.F) == false)
            {
                ret.err = DownloadError.NOTFOUND;
                return;
            }
            var fi = new LongFileInfo(path + file.F);
            // kludge : for seconds resolution
            var d1 = fi.LastWriteTime.Ticks / Global.tickfilter;
            var d2 = file.D.Ticks / Global.tickfilter;
            if (d1 == d2)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    byte[] b = new byte[size];
                    fs.Seek(start, SeekOrigin.Begin);
                    fs.Read(b, 0, size);
                    ret.err = DownloadError.OK;
                    ret.data = b;
                }
                catch
                {
                    ret.err = DownloadError.LOCKED;
                }
                finally
                {
                    if (fs != null)
                        fs.Close();
                }
            }
            else
                ret.err = DownloadError.OLDER;
        }

        public static void SaveConfig()
        {
            LockManager.GetLock("settings.config", () =>
            {
                File.WriteAllText("settings.config", JSON.ToNiceJSON(new Global(), new JSONParameters { UseExtensions = false, UseFastGuid = false }));
            });
        }

        public static Delta SyncReadWrite(List<QueueProcessor> qlist, string clientmachine, string sharename, string token, State clientstate, Delta clientdelta)
        {
            var cs = Global.ConnectionList.Find(x => x.Name == sharename && x.MachineName == clientmachine && x.Token == token);
            if (cs != null)
            {
                var q = qlist.Find(x => x.cs == cs);
                if (q != null)
                {
                    _log.Info("Sync RW : " + sharename);
                    return DeltaProcessor.SynchronizeSendRecieve(q, cs, clientstate, clientdelta);
                }
                else
                {
                    _log.Debug("Queue for share not found : " + cs);
                    return new Delta();
                }
            }
            _log.Debug($"{clientmachine}/{sharename} and token not found");
            return new Delta();
        }

        public static Delta SyncReadOnly(List<QueueProcessor> qlist, string clientmachine, string sharename, string token, State clientstate)
        {
            var cs = Global.ConnectionList.Find(x => x.Name == sharename && x.MachineName == clientmachine && x.Token == token);
            if (cs != null)
            {
                var q = qlist.Find(x => x.cs == cs);
                if (q != null)
                {
                    _log.Info("Sync RO : " + sharename);
                    var state = DeltaProcessor.GetCurrentState(cs.Path, cs);
                    q.SetInfo(state);
                    return DeltaProcessor.ComputeDelta(state, clientstate);
                }
            }
            _log.Debug($"{clientmachine}/{sharename} and token not found");
            return new Delta();
        }

        public static string Connect(Connection conn)
        {
            // check if cs exits else save to disk
            var f = Global.ConnectionList.Find(x => x.Name == conn.Name && x.MachineName == conn.MachineName);
            if (f == null)
            {
                string path = SaveConnection(conn);

                conn.csfolder = path;
                Global.ConnectionList.Add(conn);

                return conn.Token;
            }
            return "";
        }

        public static string SaveConnection(Connection conn)
        {
            var path = "Computers\\" + conn.MachineName + "\\";
            if (TorpedoSync.Global.isWindows == false)
                path = path.Replace('\\', Path.DirectorySeparatorChar);

            Directory.CreateDirectory(path);

            File.WriteAllText(path + conn.Name + ".config", JSON.ToNiceJSON(conn, new JSONParameters { UseExtensions = false }));
            return path;
        }

        public static string RegisterMachine(string clientmachine, string sharename, string token, Action<Connection> startque)
        {
            var sh = Global.Shares.Find(x => x.Name == sharename);
            if (sh != null)
            {
                if (sh.ReadOnlyToken == token || sh.ReadWriteToken == token)
                {
                    // check if cs exits else save to disk
                    var f = Global.ConnectionList.Find(x => x.Name == sharename && x.MachineName == clientmachine);
                    if (f == null)
                    {
                        Connection cs = new Connection();
                        cs.Path = sh.Path;
                        cs.Name = sh.Name;
                        cs.MachineName = clientmachine;
                        cs.Token = token;
                        cs.isConfirmed = false;
                        cs.ReadOnly = sh.ReadOnlyToken == token;
                        cs.isClient = false;

                        var path = SaveConnection(cs);

                        cs.csfolder = path;
                        Global.ConnectionList.Add(cs);
                        startque(cs);

                        return sh.ReadOnlyToken == token ? "readonly" : "readwrite";
                    }
                }
                return sh.ReadOnlyToken == token ? "readonly" : "readwrite"; // exists -> don't do anything
            }

            return ""; // share not found -> nothing
        }

        public static bool isConfirmed(string clientmachine, string sharename, string token)
        {
            var cs = Global.ConnectionList.Find(x => x.MachineName == clientmachine && x.Name == sharename && x.Token == token);
            if (cs != null)
                return cs.isConfirmed;

            _log.Debug($"{clientmachine}/{sharename} and token not found");
            return false;
        }

        public static ServerInfo GetServerInfo()
        {
            var si = new ServerInfo();

            //si.Shares = new List<string>();
            //Global.Shares.ForEach(x => si.Shares.Add(x.Name));

            return si;
        }
    }
}
