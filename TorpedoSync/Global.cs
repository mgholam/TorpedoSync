using fastBinaryJSON;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace TorpedoSync
{
    internal class Global
    {
        [XmlIgnore]
        public static int KB = 1024;
        [XmlIgnore]
        public static int MB = KB * KB;


        public static int UDPPort = 21000;
        public static int TCPPort = 21001;
        public static int WebPort = 88;

        public static bool LocalOnlyWeb = true;

        public static List<Share> Shares = new List<Share>();

        public static int DownloadBlockSizeMB = 10;// * MB;

        public static bool BatchZip = true;
        public static int BatchZipFilesUnderMB = 100;

        public static int SaveQueueEvery_X_Minute = 1;
        public static bool StartWebUI= true;

        public static int NewSyncTimerSecs = 2*60;

        [XmlIgnore]
        public static BJSONParameters BJSON_PARAM = new BJSONParameters { UseUnicodeStrings = false, UseExtensions = false, UseUTCDateTime = true };
        [XmlIgnore]
        public static string EXEPath = "";
        [XmlIgnore]
        public static double FreeMemoryTimerMin = 1;
        [XmlIgnore]
        public static List<Connection> ConnectionList = new List<Connection>();
        [XmlIgnore]
        public static bool isWindows = true;
        [XmlIgnore]
        public static long SmallFileSize = 100 * KB;
        [XmlIgnore]
        public const long tickfilter = 10*1000*1000;
        [XmlIgnore]
        public static bool UseEmbeddedWebResources = true;
        [XmlIgnore]
        public static bool PauseAll = false;
    }

}
