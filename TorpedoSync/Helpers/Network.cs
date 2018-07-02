using System;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.IO;

namespace TorpedoSync
{
    //
    // Header bits format : 0 - json = 1 , bin = 0
    //                      1 - binary json = 1 , text json = 0
    //                      2 - compressed = 1 , uncompressed = 0
    //
    //     0   : data format
    //     1-4 : data length
    //     n   : data bytes

    public class NetworkClient
    {
        public static class Config
        {
            /// <summary>
            /// Block buffer size (default = 32kb)
            /// </summary>
            public static int BufferSize = 32 * 1024;
            /// <summary>
            /// Log data if over (default = 1,000,000)
            /// </summary>
            public static int LogDataSizesOver = 1000 * 1000;
            /// <summary>
            /// Compress data if over (default = 1,000,000)
            /// </summary>
            public static int CompressDataOver = 1000 * 1000;
            /// <summary>
            /// Kill inactive client connections (default = 30sec)
            /// </summary>
            public static int KillConnectionSeconds = 30;
        }

        public NetworkClient(string server, int port)
        {
            _server = server;
            _port = port;
        }
#if minilzo
        private RaptorDB.ILog log = RaptorDB.LogManager.GetLogger(typeof(NetworkClient));
#endif
        private TcpClient _client;
        private string _server;
        private int _port;

        public Guid ClientID = Guid.NewGuid();
        public bool UseBJSON = true;

        public void Connect()
        {
            _client = new TcpClient(_server, _port);
            _client.ReceiveBufferSize = 0;
            _client.SendBufferSize = Config.BufferSize;
        }

        public object Send(object data)
        {
            try
            {
                CheckConnection();

                byte[] dat = fastBinaryJSON.BJSON.ToBJSON(data);
                bool compressed = false;
#if minilzo
                if (dat.Length > NetworkClient.Config.CompressDataOver)
                {
                    log.Info("compressing data over limit : " + dat.Length.ToString("#,#"));
                    compressed = true;
                    dat = RaptorDB.MiniLZO.Compress(dat);
                    log.Info("new size : " + dat.Length.ToString("#,#"));
                }
#endif
                byte[] len = Helper.GetBytes(dat.Length, false);

                using (NetworkStream ns = new NetworkStream(_client.Client))
                {
                    BufferedStream n = new BufferedStream(ns, Config.BufferSize);
                    // header
                    n.WriteByte((byte)((UseBJSON ? 3 : 0) + (compressed ? 4 : 0)));
                    n.Write(len, 0, 4);
                    // data
                    n.Write(dat, 0, dat.Length);
                    // read response
                    byte[] hdr = new byte[5];
                    n.Read(hdr, 0, 5);
                    int count = Helper.ToInt32(hdr, 1);
                    byte[] recd = new byte[count];
                    int bytesRead = 0;
                    int chunksize = 1;
                    while (bytesRead < count && chunksize > 0)
                        bytesRead +=
                          chunksize = n.Read
                            (recd, bytesRead, count - bytesRead);
#if minilzo
                    if ((hdr[0] & (byte)4) == (byte)4)
                        recd = RaptorDB.MiniLZO.Decompress(recd);
#endif
                    if ((hdr[0] & (byte)3) == (byte)3)
                        return fastBinaryJSON.BJSON.ToObject(recd);
                }
            }
            catch
            {

            }
            return null;
        }

        private void CheckConnection()
        {
            // check connected state before sending

            if (_client == null || !_client.Connected)
                Connect();
        }

        public void Close()
        {
            if (_client != null)
            {
                _client.Close();
            }
        }
    }

    public class NetworkServer
    {
        public delegate object ProcessPayload(object data);
#if minilzo
        private RaptorDB.ILog log = RaptorDB.LogManager.GetLogger(typeof(NetworkServer));
#endif
        ProcessPayload _handler;
        private bool _run = true;
        private int _port;

        private void RunThread(Action st)
        {
            System.Threading.Tasks.Task.Factory.StartNew(st);
            //var t = new Thread(st.Invoke);
            //t.IsBackground = true;
            //t.Start();
        }

        public void Start(int port, ProcessPayload handler)
        {
            _handler = handler;
            _port = port;
            ThreadPool.SetMinThreads(50, 50);
            RunThread(Run);
        }

        private void Run()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, _port);
            listener.Server.ReceiveBufferSize = 0;
            listener.Server.SendBufferSize = NetworkClient.Config.BufferSize;
            listener.Start();

            while (_run)
            {
                try
                {
                    TcpClient c = listener.AcceptTcpClient();
                    RunThread(() => Accept(c));
                }
                catch { }// (Exception ex) { log.Error(ex); }
            }
        }

        public void Stop()
        {
            _run = false;
        }

        void Accept(TcpClient client)
        {
            client.ReceiveBufferSize = 0;
            client.SendBufferSize = NetworkClient.Config.BufferSize;
            using (NetworkStream ns = client.GetStream())
            {
                BufferedStream n = new BufferedStream(ns, NetworkClient.Config.BufferSize);
                {
                    byte[] hdr = new byte[5];
                    n.Read(hdr, 0, 5);
                    int count = BitConverter.ToInt32(hdr, 1);
                    byte[] data = new byte[count];
                    int bytesRead = 0;
                    int chunksize = 1;
                    while (bytesRead < count && chunksize > 0)
                        bytesRead +=
                          chunksize = n.Read
                            (data, bytesRead, count - bytesRead);
#if minilzo
                    if ((hdr[0] & (byte)4) == (byte)4)
                        data = RaptorDB.MiniLZO.Decompress(data);
#endif
                    object o = fastBinaryJSON.BJSON.ToObject(data);
                    // handle payload
                    object r = _handler(o);
                    bool compressed = false;
                    var dataret = fastBinaryJSON.BJSON.ToBJSON(r);
#if minilzo
                    if (dataret.Length > NetworkClient.Config.CompressDataOver)
                    {
                        log.Info("compressing data over limit : " + dataret.Length.ToString("#,#"));
                        compressed = true;
                        dataret = RaptorDB.MiniLZO.Compress(dataret);
                        log.Info("new size : " + dataret.Length.ToString("#,#"));
                    }
                    if (dataret.Length > NetworkClient.Config.LogDataSizesOver)
                        log.Info("data size (bytes) = " + dataret.Length.ToString("#,#"));
#endif
                    byte[] len = Helper.GetBytes(dataret.Length, false);
                    // header
                    n.WriteByte((byte)((true ? 3 : 0) + (compressed ? 4 : 0)));
                    n.Write(len, 0, 4);
                    // data
                    n.Write(dataret, 0, dataret.Length);
                    n.Flush();
                    //n.Close();

                    //client.Close();
                    //int wait = 0;
                    //bool close = false;
                    //var dt = FastDateTime.Now;
                    //while (n.DataAvailable == false && close == false)
                    //{
                    //    wait++;
                    //    if (wait < 10000) // kludge : for insert performance
                    //        Thread.Sleep(0);
                    //    else
                    //    {
                    //        Thread.Sleep(1);
                    //        // wait done -> close connection
                    //        if (FastDateTime.Now.Subtract(dt).TotalSeconds > NetworkClient.Config.KillConnectionSeconds)
                    //            close = true;
                    //    }
                    //}
                    //if (close)
                    //    break;
                }
            }
            client.Close();
        }
    }

    internal static class Helper
    {
        public static unsafe int ToInt32(byte[] value, int startIndex)
        {
            fixed (byte* numRef = &(value[startIndex]))
            {
                return *((int*)numRef);
            }
        }

        public static unsafe byte[] GetBytes(int num, bool reverse)
        {
            byte[] buffer = new byte[4];
            fixed (byte* numRef = buffer)
            {
                *((int*)numRef) = num;
            }
            if (reverse)
                Array.Reverse(buffer);
            return buffer;
        }
    }
}
