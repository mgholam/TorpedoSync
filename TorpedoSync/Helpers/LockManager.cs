using RaptorDB.Common;
using System;
using System.IO;

namespace TorpedoSync
{ 
    class lobj
    {
        public int count = 0;
    }

    internal class LockManager
    {
        static SafeDictionary<string, lobj> _locks = new SafeDictionary<string, lobj>();
        //static ConcurrentDictionary<string, lobj> _locks = new ConcurrentDictionary<string, lobj>();
        private static lobj GetLock(string filename)
        {
            lobj o = null;
            if(_locks.TryGetValue(filename.ToLower(), out o))
            {
                o.count++;
                return o;
            }
            else
            {
                o = new lobj();
                _locks.Add(filename.ToLower(), o);
                o.count++;
                return o;
            }
        }

        public static void GetLock(string filename, Action action)
        {
            lock(GetLock(filename))
            {
                action();
                Unlock(filename);
            }
        }

        private static void Unlock(string filename)
        {
            lobj o = null;
            if (_locks.TryGetValue(filename.ToLower(), out o))
            {
                o.count--;
                if (o.count == 0)
                    _locks.Remove(filename.ToLower());
            }
        }

        public static T Lockload<T>(string filename)
        {
            T o = default(T);
            LockManager.GetLock(filename, () =>
            {
                o = fastJSON.JSON.ToObject<T>(File.ReadAllText(filename));
            });
            return o;
        }
    }
}
