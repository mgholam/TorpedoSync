using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml.Serialization;

namespace TorpedoSync
{
    public class SyncFile
    {
        /// <summary>
        /// File path
        /// </summary>
        public string F;
        /// <summary>
        /// File datetime
        /// </summary>
        public DateTime D;
        /// <summary>
        /// File size
        /// </summary>
        public long S;
    }

    public class Delta
    {
        public List<string> FoldersDeleted = new List<string>();
        public List<string> FilesDeleted = new List<string>();
        public List<SyncFile> FilesAdded = new List<SyncFile>();
        public List<SyncFile> FilesChanged = new List<SyncFile>();
    }

    public class State
    {
        public DateTime Date = DateTime.Now;
        public List<string> Folders = new List<string>();
        public List<SyncFile> Files = new List<SyncFile>();
    }

    public class Share
    {
        public string Path;
        public string Name;
        public string ReadOnlyToken = new Guid().ToString();
        public string ReadWriteToken = new Guid().ToString();
    }

    public enum ShareType
    {
        ReadOnly,
        ReadWrite
    }

    // used for "sharename.config" in "computer" folder
    public class Connection
    {
        /// <summary>
        /// Share name
        /// </summary>
        public string Name; // share name
        /// <summary>
        /// Path to share files on disk 
        /// </summary>
        public string Path; // share path on disk
        /// <summary>
        /// Machine name to connect to if a client
        /// </summary>
        public string MachineName; // machine name to connect to 
        public string Token;
        public bool isConfirmed; // share confirmed for sync
        public bool ReadOnly;
        public bool isPaused;
        /// <summary>
        /// this config file folder
        /// </summary>
        [XmlIgnore]
        public string csfolder;

        public bool isClient = true;

        [XmlIgnore]
        public List<string> ignorelist = new List<string>() { ".ts", "*.!torpedosync" };

        public bool ReadyForSync()
        {
            return isConfirmed && !isPaused;
        }

        [XmlIgnore]
        public bool isChanged = true;

        [XmlIgnore]
        public State CurrentState = new State();

        public bool Allowed(string filepath)
        {
            // check against ignore list
            string fpl = filepath.ToLower().Replace("/","\\");
            string[] ss = fpl.Split('\\');
            foreach (var i in ignorelist)
            {
                string il = i.ToLower();
                if (i.Contains("*") || i.Contains("?"))
                {
                    if (IsMatch(fpl, il, '?', '*'))
                        return false;
                }
                else
                {
                    // split filepath on / \ then check
                    if (i.Contains("\\") == false)
                    {
                        foreach (var j in ss)
                            if (j == il)
                                return false;
                    }
                    else
                    {
                        if (fpl.Contains(il)) // as a folder
                            return false;
                    }
                }
            }
            return true;
        }

        private bool IsMatch(string input, string pattern, char singleWildcard, char multipleWildcard)
        {

            int[] inputPosStack = new int[(input.Length + 1) * (pattern.Length + 1)];   // Stack containing input positions that should be tested for further matching
            int[] patternPosStack = new int[inputPosStack.Length];                      // Stack containing pattern positions that should be tested for further matching
            int stackPos = -1;                                                          // Points to last occupied entry in stack; -1 indicates that stack is empty
            bool[,] pointTested = new bool[input.Length + 1, pattern.Length + 1];       // Each true value indicates that input position vs. pattern position has been tested

            int inputPos = 0;   // Position in input matched up to the first multiple wildcard in pattern
            int patternPos = 0; // Position in pattern matched up to the first multiple wildcard in pattern

            // Match beginning of the string until first multiple wildcard in pattern
            while (inputPos < input.Length && patternPos < pattern.Length && pattern[patternPos] != multipleWildcard && (input[inputPos] == pattern[patternPos] || pattern[patternPos] == singleWildcard))
            {
                inputPos++;
                patternPos++;
            }

            // Push this position to stack if it points to end of pattern or to a general wildcard
            if (patternPos == pattern.Length || pattern[patternPos] == multipleWildcard)
            {
                pointTested[inputPos, patternPos] = true;
                inputPosStack[++stackPos] = inputPos;
                patternPosStack[stackPos] = patternPos;
            }
            bool matched = false;

            // Repeat matching until either string is matched against the pattern or no more parts remain on stack to test
            while (stackPos >= 0 && !matched)
            {

                inputPos = inputPosStack[stackPos];         // Pop input and pattern positions from stack
                patternPos = patternPosStack[stackPos--];   // Matching will succeed if rest of the input string matches rest of the pattern

                if (inputPos == input.Length && patternPos == pattern.Length)
                    matched = true;     // Reached end of both pattern and input string, hence matching is successful
                else
                {
                    // First character in next pattern block is guaranteed to be multiple wildcard
                    // So skip it and search for all matches in value string until next multiple wildcard character is reached in pattern

                    for (int curInputStart = inputPos; curInputStart < input.Length; curInputStart++)
                    {

                        int curInputPos = curInputStart;
                        int curPatternPos = patternPos + 1;

                        if (curPatternPos == pattern.Length)
                        {   // Pattern ends with multiple wildcard, hence rest of the input string is matched with that character
                            curInputPos = input.Length;
                        }
                        else
                        {

                            while (curInputPos < input.Length && curPatternPos < pattern.Length && pattern[curPatternPos] != multipleWildcard &&
                                (input[curInputPos] == pattern[curPatternPos] || pattern[curPatternPos] == singleWildcard))
                            {
                                curInputPos++;
                                curPatternPos++;
                            }

                        }

                        // If we have reached next multiple wildcard character in pattern without breaking the matching sequence, then we have another candidate for full match
                        // This candidate should be pushed to stack for further processing
                        // At the same time, pair (input position, pattern position) will be marked as tested, so that it will not be pushed to stack later again
                        if (((curPatternPos == pattern.Length && curInputPos == input.Length) || (curPatternPos < pattern.Length && pattern[curPatternPos] == multipleWildcard))
                            && !pointTested[curInputPos, curPatternPos])
                        {
                            pointTested[curInputPos, curPatternPos] = true;
                            inputPosStack[++stackPos] = curInputPos;
                            patternPosStack[stackPos] = curPatternPos;
                        }
                    }
                }
            }

            return matched;
        }
    }

    public enum DownloadError
    {
        OK,
        NOTFOUND,
        OLDER,
        LOCKED
    }

    public class DFile
    {
        public DownloadError err;
        public byte[] data;
    }

    public class ServerAddress
    {
        public string Name;
        public IPAddress IP;
    }

    public class ServerInfo
    {
        public string Name = Environment.MachineName;
        public int VersionMajor = 1;
        public int VersionMinor = 0;
        public DateTime ServerTime = DateTime.Now;
        //public List<string> Shares = new List<string>();
    }

    public class ConnectionInfo
    {
        public int TotalFileCount;
        public int FilesInQue;
        public int FailedFiles;
        [XmlIgnore]
        public string LastFileNameDownloaded;
        public long QueDataSize;
        public decimal Mbs;
        public int EstimatedTimeSecs;
        [XmlIgnore]
        public List<string> Que;
        [XmlIgnore]
        public List<string> Failed;
    }


    public class fswatch
    {
        public FileSystemWatcher watcher;
        public DateTime LastChanged = DateTime.Now;
    }
    //------------- network --------------
    public enum COMMANDS
    {
        isConfirmed,
        GetServerInfo,
        Download,
        SyncReadOnly,
        SyncReadWrite,
        RegisterMachine,
        isChanged,
        CreateZip,
        DownloadZip,
        DeleteZip,
        CanConnect
        //Pause,
        //Start

    }


    public class Packet
    {
        public Packet()
        {
        }
        public string Token;
        public string Command;
        public object Data;
    }

    public class ReturnPacket
    {
        public ReturnPacket()
        {

        }
        public ReturnPacket(bool ok)
        {
            OK = ok;
        }
        public ReturnPacket(bool ok, string err)
        {
            OK = ok;
            Error = err;
        }
        public string Error;
        public bool OK;
        public object Data;
    }
}
