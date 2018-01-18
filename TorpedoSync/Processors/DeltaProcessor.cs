using fastBinaryJSON;
using RaptorDB;
using RaptorDB.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace TorpedoSync
{
    class DeltaProcessor
    {
        private static ILog _log = LogManager.GetLogger(typeof(DeltaProcessor));

        public static State GetCurrentState(string path, Connection share)
        {
            if (share.isChanged == false && 
                share.CurrentState.Files.Count > 0 && 
                share.CurrentState.Folders.Count > 0)
                return share.CurrentState;

            State state = new State();
            foreach (var f in LongDirectory.GetDirectories(path, "*.*", SearchOption.AllDirectories))
            {
                string p = f.Replace(path, "");
                if (share.Allowed(p))
                    state.Folders.Add(p);
            }

            if (Global.isWindows == false)
            {
                foreach (var f in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                {
                    var fi = new FileInfo(f);
                    string fn = fi.FullName.Replace(path, "");
                    if (share.Allowed(fi.FullName))
                        state.Files.Add(new SyncFile { F = fn, D = fi.LastWriteTime, S = fi.Length });
                }
            }
            else
            {
                foreach (var f in FastDirectoryEnumerator.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                {
                    string fn = f.Path.Replace(path, "");
                    if (share.Allowed(f.Path))
                        state.Files.Add(new SyncFile { F = fn, D = f.LastWriteTime, S = f.Size });
                }
            }
            share.CurrentState = state;
            return state;
        }

        public static Delta ComputeDelta(State masterstate, State clientstate)
        {
            DateTime dt = FastDateTime.Now;
            Delta delta = new Delta();

            SortedList<string, SyncFile> sfiles = new SortedList<string, SyncFile>(clientstate.Files.Count);
            List<string> mfiles = new List<string>(masterstate.Files.Count);

            foreach (var i in clientstate.Files)
                sfiles.Add(i.F, i);

            foreach (var i in masterstate.Files)
                mfiles.Add(i.F);

            mfiles.Sort();
            masterstate.Folders.Sort();

            // if client folder not on master -> delete
            foreach (var i in clientstate.Folders)
            {
                int k = masterstate.Folders.BinarySearch(i);
                if (k < 0)
                    delta.FoldersDeleted.Add(i);
            }

            // if client file not on master -> delete
            foreach (var i in clientstate.Files)
            {
                int k = mfiles.BinarySearch(i.F);
                if (k < 0)
                    delta.FilesDeleted.Add(i.F);
            }

            foreach (var i in masterstate.Files)
            {
                SyncFile o = null;
                if (sfiles.TryGetValue(i.F, out o) == false)
                    delta.FilesAdded.Add(i);
                else
                {
                    // kludge : for seconds resolution
                    var d1 = i.D.Ticks / Global.tickfilter;
                    var d2 = o.D.Ticks / Global.tickfilter;
                    if (d1 > d2)
                        delta.FilesChanged.Add(i);
                }
            }
            var s = "Sync R time secs = " + FastDateTime.Now.Subtract(dt).TotalSeconds;
            //Console.WriteLine(s);
            _log.Debug(s);
            return delta;
        }
    
        public static Delta SynchronizeSendRecieve(QueueProcessor quep, Connection share, State clientstate, Delta clientdelta)
        {
            DateTime dt = FastDateTime.Now;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            State masterLastState = GetLastState(share);
            State masterstate = GetCurrentState(share.Path, share);

            quep.SetInfo(masterstate);

            _log.Info("  Reading state ms : " + sw.ElapsedMilliseconds);
            sw.Reset();
            var masterdelta = ComputeDelta(masterstate, masterLastState);
            _log.Info("  Master delta ms : " + sw.ElapsedMilliseconds);
            sw.Reset();

            Delta clientsend = new Delta();
            Delta masterque = new Delta();

            SortedList<string, SyncFile> sfiles = new SortedList<string, SyncFile>(clientstate.Files.Count);
            SortedList<string, SyncFile> mfiles = new SortedList<string, SyncFile>(masterstate.Files.Count);

            foreach (var i in clientstate.Files)
                sfiles.Add(i.F.ToLower(), i);

            foreach (var i in masterstate.Files)
                mfiles.Add(i.F.ToLower(), i);
            _log.Info("  Creating dictionaries ms : " + sw.ElapsedMilliseconds);
            sw.Reset();

            masterque.FilesDeleted = clientdelta.FilesDeleted;
            masterque.FoldersDeleted = clientdelta.FoldersDeleted;

            if (masterLastState.Files.Count > 0 || masterLastState.Folders.Count > 0)
            {
                clientsend.FilesDeleted = masterdelta.FilesDeleted;
                clientsend.FoldersDeleted = masterdelta.FoldersDeleted;
            }

            // check master files not exist or older on client -> que for client
            foreach (var i in masterstate.Files)
            {
                SyncFile o = null;
                if (sfiles.TryGetValue(i.F.ToLower(), out o) == false)
                    clientsend.FilesAdded.Add(i);
                else
                {
                    // kludge : for seconds resolution
                    var d1 = i.D.Ticks / Global.tickfilter;
                    var d2 = o.D.Ticks / Global.tickfilter;
                    if (d1 > d2)
                        clientsend.FilesChanged.Add(i);
                }
            }
            _log.Info("  Generate client delta ms : " + sw.ElapsedMilliseconds);
            sw.Reset();

            // check client files not exist or older on master -> que for master
            foreach (var i in clientstate.Files)
            {
                SyncFile o = null;
                if (mfiles.TryGetValue(i.F.ToLower(), out o) == false)
                    masterque.FilesAdded.Add(i);
                else
                {
                    // kludge : for seconds resolution
                    var d1 = i.D.Ticks / Global.tickfilter;
                    var d2 = o.D.Ticks / Global.tickfilter;
                    if (d1 > d2)
                        masterque.FilesChanged.Add(i);
                }
            }
            _log.Info("  Generating master que ms : " + sw.ElapsedMilliseconds);
            sw.Reset();

            var s = "Sync RW time secs = " + FastDateTime.Now.Subtract(dt).TotalSeconds;
            _log.Debug(s);
            quep.QueueDelta(masterque);

            SaveState(share, masterstate);

            return clientsend;
        }

        internal static void SaveState(Connection share, State state)
        {
            string fn = share.csfolder + share.Name + ".state";
            File.WriteAllBytes(fn, BJSON.ToBJSON(state, Global.BJSON_PARAM));
        }

        public static State GetLastState(Connection share)
        {
            string fn = share.csfolder + share.Name + ".state";
            try
            {
                if (File.Exists(fn))
                    return BJSON.ToObject<State>(File.ReadAllBytes(fn), Global.BJSON_PARAM);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return new State();
        }
    }
}
