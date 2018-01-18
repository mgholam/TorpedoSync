using RaptorDB;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TorpedoSync
{
    class UDP
    {
        private static int _port = 20000;
        private static int _timeouts = 2000;
        private static bool _done = false;
        private static ILog _log = LogManager.GetLogger(typeof(UDP));


        public static void StartUDPServer(int port)
        {
            _port = port;
            var Server = new UdpClient(_port);
            var servername = Environment.MachineName;

            List<string> ips = new List<string>();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ips.Add("" + ip);
                    _log.Debug("my ip = " + ip);
                }
            }

            while (!_done)
            {
                try
                {
                    var ClientEp = new IPEndPoint(IPAddress.Any, _port);
                    var ClientRequestData = Server.Receive(ref ClientEp);
                    var ClientRequest = Encoding.ASCII.GetString(ClientRequestData);

                    if (ips.Contains(ClientRequest) == false) // skip if from own IP
                    {
                        var ResponseData = fastBinaryJSON.BJSON.ToBJSON(new ServerInfo(), Global.BJSON_PARAM);
                        Server.Send(ResponseData, ResponseData.Length, ClientEp);
                    }
                }
                catch// (Exception ex)
                {

                }
                Thread.Sleep(10);
            }
        }

        public static List<ServerAddress> DoUDPSearch()
        {
            List<ServerAddress> list = new List<ServerAddress>();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            List<Task> tasks = new List<Task>();
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    tasks.Add(Task.Factory.StartNew(() => searchIP(_port, ip, list)));
                }
            }
            Task.WaitAll(tasks.ToArray());
            return list;
        }

        private static void searchIP(int port, IPAddress ip, List<ServerAddress> list)
        {
            var ServerEp = new IPEndPoint(ip, 0);
            var Client = new UdpClient(ServerEp);
            Client.Client.SendTimeout = _timeouts;
            Client.Client.ReceiveTimeout = _timeouts;
            var RequestData = Encoding.ASCII.GetBytes("" + ip);
            try
            {
                Client.Send(RequestData, RequestData.Length, new IPEndPoint(IPAddress.Broadcast, port));
                while (true)
                {
                    var ServerResponseData = Client.Receive(ref ServerEp);
                    var h = fastBinaryJSON.BJSON.ToObject<ServerInfo>(ServerResponseData);
                    var datediff = DateTime.Now.Subtract(h.ServerTime).TotalMinutes;
                    var t = Math.Abs(datediff);
                    if (t > 10)
                    {
                        _log.Debug("Clocks out of sync by " + t + " mins");
                    }
                    else
                    {
                        // TODO : check version?
                        list.Add(new ServerAddress { IP = ServerEp.Address, Name = h.Name });
                    }
                    Thread.Sleep(1);
                }
            }
            catch { }
            finally { Client.Close(); }
        }
    }
}
