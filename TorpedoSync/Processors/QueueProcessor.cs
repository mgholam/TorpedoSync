using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using fastBinaryJSON;
using RaptorDB;
using RaptorDB.Common;
using System.Diagnostics;
using fastJSON;

namespace TorpedoSync
{
    class QueueProcessor
    {
        // TODO : better handling of locked files
        public QueueProcessor(string path, Connection connection)
        {
            _path = path;
            _conn = connection;
            _quefile = _path + _conn.Name + ".queue";

            string archivefolder = _conn.Path + ".ts" + _S + "old" + _S;
            Directory.CreateDirectory(archivefolder);

            if (Directory.Exists(_conn.Path + ".ts" + _S + "Temp"))
                Directory.Delete(_conn.Path + ".ts" + _S + "Temp", true);

            if (File.Exists(_conn.Path + ".ts" + _S + "readme.txt") == false)
                File.WriteAllText(_conn.Path + ".ts" + _S + "readme.txt", "This is the TorpedoSync folder for old archived files and settings.");

            if (File.Exists(_conn.Path + ".ts" + _S + ".ignore") == false)
                File.WriteAllText(_conn.Path + ".ts" + _S + ".ignore", "# comment line\r\n");

            // cleanup failed .!torpedosync files
            if (TorpedoSync.Global.isWindows == true)
            {
                foreach (var i in FastDirectoryEnumerator.EnumerateFiles(_conn.Path, "*.!torpedosync", SearchOption.AllDirectories))
                    LongFile.Delete(i.Path);
            }
            else
            {
                //foreach (var i in Directory.EnumerateFiles(_conn.Path, "*.!torpedosync", SearchOption.AllDirectories))
                //    LongFile.Delete(i);
            }
            _timer.AutoReset = true;
            _timer.Elapsed += timer_elapsed;
            _timer.Start();
            LoadQueue();
            StartWatcher();
            Task.Factory.StartNew(DoWork);
            Task.Factory.StartNew(() => { timer_elapsed(null, null); });
        }

        private static ILog _log = LogManager.GetLogger(typeof(QueueProcessor));
        private Connection _conn;
        private string _path;
        private string _quefile;
        private ConcurrentQueue<SyncFile> _que = new ConcurrentQueue<SyncFile>();
        private ConcurrentQueue<SyncFile> _ErrorQue = new ConcurrentQueue<SyncFile>();
        private bool _shutdown = false;
        private readonly System.Timers.Timer _timer = new System.Timers.Timer(Global.NewSyncTimerSecs * 1000); // 30 sec sync 
        private string _S = "" + Path.DirectorySeparatorChar;
        private fswatch _fsw = null;
        private DateTime _lastSyncTime = DateTime.MinValue;
        private ConnectionInfo _connInfo = new ConnectionInfo();

        private void StartWatcher()
        {
            var fsw = new fswatch();
            fsw.watcher = new FileSystemWatcher(cs.Path);
            fsw.watcher.IncludeSubdirectories = true;
            fsw.watcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
            fsw.watcher.EnableRaisingEvents = true;
            fsw.watcher.Filter = "*.*";
            fsw.watcher.Renamed += (s, e) =>
            {
                FileSystemWatch_Event(e.FullPath);
                fsw.LastChanged = FastDateTime.Now;
            };
            fsw.watcher.Created += (s, e) =>
            {
                FileSystemWatch_Event(e.FullPath);
                fsw.LastChanged = FastDateTime.Now;
            };
            fsw.watcher.Deleted += (s, e) =>
            {
                FileSystemWatch_Event(e.FullPath);
                fsw.LastChanged = FastDateTime.Now;
            };
            fsw.watcher.Changed += (s, e) =>
            {
                FileSystemWatch_Event(e.FullPath);
                fsw.LastChanged = FastDateTime.Now;
            };
            _fsw = fsw;
        }

        public ConnectionInfo GetConnectionInfo()
        {
            if (_connInfo.TotalFileCount == 0)
            {
                if (File.Exists(_conn.csfolder + cs.Name + ".info"))
                {
                    BJSON.FillObject(_connInfo, File.ReadAllBytes(_conn.csfolder + _conn.Name + ".info"));
                }
            }
            _connInfo.EstimatedTimeSecs = 0;
            _connInfo.Failed = new List<string>();
            _connInfo.FailedFiles = _ErrorQue.Count;
            _connInfo.FilesInQue = _que.Count;
            _connInfo.Mbs = 0;
            _connInfo.Que = new List<string>();
            _connInfo.QueDataSize = _que.ToArray().Sum(x => x.S);

            return _connInfo;
        }

        internal void SetInfo(State masterstate)
        {
            _connInfo.TotalFileCount = masterstate.Files.Count;
        }

        public void FileSystemWatch_Event(string fn)
        {
            // check against ignore list
            fn = fn.Replace("/", "\\"); // for unix systems
            if (_conn.Allowed(fn))
            {
                if (_conn.isChanged == false)
                {
                    _conn.CurrentState = new State();
                    _conn.isChanged = true;
                }
            }
        }

        public Connection cs
        {
            get { return _conn; }
        }

        private void timer_elapsed(object sender, ElapsedEventArgs e)
        {
            if (ClientCommands.isConnected(_conn.MachineName))
            {
                if (_que.Count == 0 && _conn.isClient && _downloading == false)
                {
                    NewSync();
                }
            }
            else
            {
                // clear cache if connection lost
                _log.Info("Clearing cache for : " + _conn.Name);
                _conn.CurrentState = new State();
                _conn.isChanged = true;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            }
        }

        private object _lock = new object();
        private void NewSync()
        {
            lock (_lock)
            {
                if (ClientCommands.isConnected(_conn.MachineName) == false)
                    return;
                if (_conn.isConfirmed == false)
                {
                    _conn.isConfirmed = ClientCommands.isConfirmed(_conn);
                    _conn.isPaused = false;
                    ServerCommands.SaveConnection(_conn);
                }
                if (_que.Count > 0)
                    return;

                if (_conn.ReadyForSync())
                {
                    if (_conn.ReadOnly)
                    {
                        if (ClientCommands.isChanged(_conn) || _conn.isChanged)
                        {
                            _log.Info("syncing readonly : " + _conn.Name);
                            var state = DeltaProcessor.GetCurrentState(_conn.Path, _conn);
                            _connInfo.TotalFileCount = state.Files.Count;
                            var d = ClientCommands.SyncMeReadonly(state, _conn);
                            if (d != null)
                            {
                                QueueDelta(d);
                                _conn.isChanged = false;
                            }
                        }
                    }
                    else
                    {
                        if (ClientCommands.isChanged(_conn) || _conn.isChanged)
                        {
                            _log.Info("syncing read write : " + _conn.Name);
                            var laststate = DeltaProcessor.GetLastState(_conn);
                            // sync read write
                            var state = DeltaProcessor.GetCurrentState(_conn.Path, _conn);
                            _connInfo.TotalFileCount = state.Files.Count;
                            var delta = DeltaProcessor.ComputeDelta(state, laststate);
                            if (laststate.Files.Count > 0 || laststate.Folders.Count > 0)
                            {
                                delta.FilesChanged = new List<SyncFile>();
                                delta.FilesAdded = new List<SyncFile>();
                            }

                            var d = ClientCommands.SyncMeReadWrite(state, delta, _conn);
                            if (d != null)
                            {
                                QueueDelta(d);
                                _conn.isChanged = false;
                                DeltaProcessor.SaveState(_conn, state);
                            }
                        }
                    }
                }

                SaveQueue();
            }
        }

        public void Shutdown()
        {
            _shutdown = true;
            if (_connInfo.TotalFileCount > 0)
                File.WriteAllBytes(_conn.csfolder + _conn.Name + ".info", BJSON.ToBJSON(_connInfo, Global.BJSON_PARAM));
            SaveQueue();
        }

        internal void QueueDelta(Delta d)
        {
            string archivefolder = _conn.Path + ".ts" + _S + "old" + _S;
            Directory.CreateDirectory(archivefolder);
            if (d != null)
            {
                // empty old error queue
                _ErrorQue = new ConcurrentQueue<SyncFile>();

                d.FilesDeleted.ForEach(x =>
                {
                    try
                    {
                        if (LongFile.Exists(_conn.Path + x))
                        {
                            _log.Info("Deleting file : " + _conn.Path + x);
                            string fn = archivefolder + x;
                            LongFile.Move(_conn.Path + x, fn);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex);
                    }
                });
                d.FoldersDeleted.ForEach(x =>
                {
                    _log.Info("Deleting folder : " + _conn.Path + x);
                    if (LongDirectory.Exists(_conn.Path + x))
                    {
                        try
                        {
                            LongDirectory.Move(_conn.Path + x, archivefolder + x);
                        }
                        catch { }
                        try
                        {
                            if (LongDirectory.Exists(_conn.Path + x))
                            {
                                var f = LongDirectory.GetFiles(_conn.Path + x, "*.*", SearchOption.AllDirectories);
                                if (f.Length == 0)
                                {
                                    LongDirectory.Delete(_conn.Path + x, true);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex);
                        }
                    }
                });
                d.FilesAdded.ForEach(x =>
                {
                    _que.Enqueue(x);
                });
                d.FilesChanged.ForEach(x =>
                {
                    _que.Enqueue(x);
                });

                _log.Info("delfolders = " + d.FoldersDeleted.Count);
                _log.Info("delfiles = " + d.FilesDeleted.Count);
                _log.Info("files added = " + d.FilesAdded.Count);
                _log.Info("files changed = " + d.FilesChanged.Count);
                _log.Info("que count = " + _que.Count);

                SaveQueue();
            }
        }

        internal bool isChanged()
        {
            DateTime v = _fsw.LastChanged;
            DateTime last = _lastSyncTime;

            _lastSyncTime = FastDateTime.Now;
            if (v > last)
                return true;
            return false;
        }

        private void LoadQueue()
        {
            _log.Debug("loading queue for " + _conn.Name);
            LockManager.GetLock(_quefile, () =>
            {
                try
                {
                    if (File.Exists(_quefile))
                        _que = new ConcurrentQueue<SyncFile>(BJSON.ToObject<List<SyncFile>>(File.ReadAllBytes(_quefile), Global.BJSON_PARAM));
                }
                catch (Exception ex) { _log.Error(ex); }
            });
            LockManager.GetLock(_quefile + "error", () =>
            {
                try
                {
                    if (File.Exists(_quefile + "error"))
                        _ErrorQue = new ConcurrentQueue<SyncFile>(BJSON.ToObject<List<SyncFile>>(File.ReadAllBytes(_quefile + "error"), Global.BJSON_PARAM));
                }
                catch (Exception ex) { _log.Error(ex); }
            });
        }

        private int _lastc = -1;
        private void SaveQueue()
        {
            if (_que.Count == _lastc)
                return;
            _lastc = _que.Count;
            LockManager.GetLock(_quefile, () =>
            {
                File.WriteAllBytes(_quefile, BJSON.ToBJSON(_que, Global.BJSON_PARAM));
            });

            LockManager.GetLock(_quefile + "error", () =>
            {
                File.WriteAllBytes(_quefile + "error", BJSON.ToBJSON(_ErrorQue, Global.BJSON_PARAM));
            });
        }

        private void DoWork()
        {
            while (!_shutdown)
            {
                DateTime _lastSave = DateTime.MinValue;
                while (_conn.isPaused == false && Global.PauseAll == false)
                {
                    if (_que.Count == 0)
                    {
                        SaveQueue();
                        break;
                    }
                    if (_que.Count > 0 && ClientCommands.isConnected(_conn.MachineName))
                    {
                        SyncFile file = null;

                        DownloadAsZipFile();

                        try
                        {
                            _que.TryDequeue(out file);
                            if (file != null)
                            {
                                ProcessFile(file);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex);
                            if (file != null)
                                _ErrorQue.Enqueue(file);
                        }
                        if (FastDateTime.Now.Subtract(_lastSave).TotalMinutes > Global.SaveQueueEvery_X_Minute)
                        {
                            SaveQueue();
                            _lastSave = FastDateTime.Now;
                        }
                    }
                    else
                    {
                        Thread.Sleep(3000);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        private void DownloadAsZipFile()
        {
            SyncFile file = null;
            if (Global.BatchZip)
            {
                int size = 0;
                int mb = Global.BatchZipFilesUnderMB * Global.MB;
                List<SyncFile> ziplist = new List<SyncFile>();
                while (_que.Count > 10)
                {
                    if (_que.TryPeek(out file))
                    {
                        if (file.S + size < mb &&
                            file.S < Global.SmallFileSize) // small files
                        {
                            _que.TryDequeue(out file);
                            ziplist.Add(file);
                        }
                        else
                            break;
                    }
                }
                if (ziplist.Count > 0)
                {
                    try
                    {
                        _downloading = true;
                        var sf = ClientCommands.CreateZip(_conn, ziplist);
                        if (sf != null)
                        {
                            string path = _conn.Path + ".ts" + _S + "Temp" + _S;
                            LongDirectory.CreateDirectory(path);

                            if (downloadfile(sf, path + sf.F, ClientCommands.DownloadZip))
                            {
                                string zfn = path + sf.F;
                                string fn = sf.F;

                                if (TorpedoSync.Global.isWindows == false)
                                    zfn = zfn.Replace("\\", "/");

                                var zf = ZipStorer.Open(zfn, FileAccess.Read);
                                zf.EncodeUTF8 = true;
                                foreach (var z in zf.ReadCentralDir())
                                {
                                    if (TorpedoSync.Global.isWindows)
                                        fn = z.FilenameInZip.Replace("/", "\\");
                                    else
                                        fn = z.FilenameInZip;

                                    MoveExistingFileToArchive(fn);
                                    LongDirectory.CreateDirectory(LongDirectory.GetDirectoryName(_conn.Path + fn));
                                    try
                                    {
                                        _log.Info("unzip : " + fn);
                                        //_log.Debug("unzip : " + fn);
                                        using (var fs = new FileStream(_conn.Path + fn, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                                        {
                                            _connInfo.LastFileNameDownloaded = fn;
                                            zf.ExtractFile(z, fs);
                                            fs.Close();
                                        }
                                        var dt = z.ModifyTime;
                                        if (z.Comment != "")
                                        {
                                            //_log.Info($"date {z.Comment}");
                                            if (DateTime.TryParse(z.Comment, out dt) == false)
                                                dt = z.ModifyTime;
                                        }
                                        var dtt = dt;
                                        if (TorpedoSync.Global.isWindows)
                                            dtt = dt.ToUniversalTime();
                                        LongFile.SetLastWriteTime(_conn.Path + fn, dtt);
                                    }
                                    catch (Exception ex) { _log.Error(ex); }
                                }
                                zf.Close();
                                ClientCommands.DeleteZip(_conn, sf);
                                _log.Info("Decompress zip done : " + sf.F);
                                LongFile.Delete(zfn);
                            }
                        }
                        _downloading = false;
                    }
                    catch (Exception ex) { _log.Error(ex); }
                }
            }
        }

        private bool _downloading = false;
        private const int _tickfilter = 10 * 1000 * 1000;
        private void ProcessFile(SyncFile file)
        {
            string fn = _conn.Path + file.F;
            //_log.Info("create dir : " + LongDirectory.GetDirectoryName(fn));
            LongDirectory.CreateDirectory(LongDirectory.GetDirectoryName(fn));

            if (LongFile.Exists(fn))
            {
                // kludge : for seconds resolution
                var fi = new LongFileInfo(fn);
                var d1 = fi.LastWriteTime.Ticks / _tickfilter;
                var d2 = file.D.Ticks / _tickfilter;
                if (d1 >= d2) // skip if already copied or newer
                    return;
            }
            _connInfo.LastFileNameDownloaded = fn;
            fn += ".!torpedosync";
            // FEATURE : resume semi downloaded file

            // remove old semi downloaded file
            LongFile.Delete(fn);

            if (TorpedoSync.Global.isWindows == false)
                fn = fn.Replace("\\", "/");

            _downloading = true;

            if (downloadfile(file, fn, ClientCommands.Download))
            {
                MoveExistingFileToArchive(file.F);
                // rename downloaded file
                LongFile.Move(fn, _conn.Path + file.F);

                LongFile.SetLastWriteTime(_conn.Path + file.F, file.D);
            }
            _downloading = false;
        }

        private void MoveExistingFileToArchive(string file)
        {
            try
            {
                if (LongFile.Exists(_conn.Path + file))
                {
                    string archivepath = _conn.Path + ".ts" + _S + "old" + _S + file;
                    // move existing file to archive
                    LongFile.Move(_conn.Path + file, archivepath);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        private bool downloadfile(SyncFile file, string saveto, Func<Connection, SyncFile, long, int, DFile> func)
        {
            long left = file.S;
            //int retry = 0;
            int mb = Global.DownloadBlockSizeMB * Global.MB;

            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            while (left > 0)
            {
                long len = left;
                long start = file.S - left;
                if (len > mb)
                    len = mb;
                //sw.Reset();
                DFile df = func(_conn, file, start, (int)len);
                //_connInfo.Mbs = (len / sw.ElapsedTicks) / (1000*TimeSpan.TicksPerSecond) ;
                if (df == null)
                {
                    //retry++;
                    //Thread.Sleep(new Random((int)FastDateTime.Now.Ticks).Next(2000) + 500);
                    //if (retry > 10)
                    {
                        _log.Info("null data : " + saveto);
                        //_que.Enqueue(file);
                        //if (LongFile.Exists(saveto))
                        LongFile.Delete(saveto);
                        return false;
                    }
                }
                else if (df.err == DownloadError.OK)
                {
                    //retry = 0;
                    string ifn = saveto;
                    if (TorpedoSync.Global.isWindows == false)
                        ifn = saveto.Replace("\\", "/");
                    _log.Info("len = " + len + " , " + saveto.Replace(".!torpedosync", ""));
                    left -= len;
                    LongDirectory.CreateDirectory(LongDirectory.GetDirectoryName(ifn));
                    // save to disk
                    var fs = new FileStream(ifn, FileMode.OpenOrCreate);
                    fs.Seek(0L, SeekOrigin.End);
                    fs.Write(df.data, 0, df.data.Length);
                    fs.Close();
                    if (left == 0)
                    {
                        var fi = new LongFileInfo(saveto);
                        if (fi.Length != file.S)
                        {
                            // FEATURE : file length don't match
                            _log.Info("file length don't match.");
                            return false;
                        }
                    }
                }
                else if (df.err == DownloadError.OLDER || df.err == DownloadError.LOCKED)
                {
                    _ErrorQue.Enqueue(file);
                    _connInfo.LastFileNameDownloaded = "";
                    _log.Info("Locked/Older : " + saveto);
                    return false;
                }
                else if (df.err == DownloadError.NOTFOUND)
                {
                    _log.Info("Not Found : " + saveto);
                    _connInfo.LastFileNameDownloaded = "";
                    _ErrorQue.Enqueue(file);
                    LongFile.Delete(saveto);
                    return false;
                }
            }
            return true;
        }
    }
}
