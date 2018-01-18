using System;
using System.Net;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO.Compression;
using RaptorDB;
using System.Threading;
using System.Reflection;

namespace TorpedoSync
{
    //public enum HTTP
    //{
    //    OK = 200,
    //    NOT_FOUND = 404,
    //    INT_ERROR = 500
    //}

    internal abstract class CoreWebServer
    {
        public CoreWebServer(int HttpPort, bool localonly, AuthenticationSchemes authenticationType, string apiPrefix, string startpage)
        {
            _apiPrefix = apiPrefix;
            _authenticationType = authenticationType;
            _localonly = localonly;
            _port = HttpPort;
            _startPage = startpage;
            //Console.WriteLine("web port = " + _port);
            Task.Factory.StartNew(() => Start(), TaskCreationOptions.LongRunning);
        }

        public delegate void Handler(HttpListenerContext ctx);
        public abstract void InitializeCommandHandler(Dictionary<string, Handler> handler, Dictionary<string, string> apihelp);

        #region [  properties  ]
        private string _S = Path.DirectorySeparatorChar.ToString();
        static public ILog _log = LogManager.GetLogger(typeof(CoreWebServer));
        private bool _run = true;
        private int _port;
        private HttpListener _server;
        private bool _localonly = false;
        private Dictionary<string, Handler> _handler = new Dictionary<string, Handler>();
        private Dictionary<string, string> _apihelp = new Dictionary<string, string>();
        internal Dictionary<string, string> _WebCache = new Dictionary<string, string>();
        private AuthenticationSchemes _authenticationType = AuthenticationSchemes.None;
        private string _apiPrefix = "myapi";
        private string _startPage = "app.html";
        #endregion

        #region [  internal  ]
        public void Stop()
        {
            _run = false;
        }


        public static bool OutPutContentType(HttpListenerContext ctx, string path)
        {
            bool compress = false;
            switch (Path.GetExtension(path).ToLower())
            {
                case ".jpg":
                case ".jpeg":
                    ctx.Response.ContentType = "image/jpeg"; break;
                case ".gif":
                    ctx.Response.ContentType = "image/gif"; break;
                case ".mht":
                    compress = true;
                    ctx.Response.ContentType = "multipart/related"; break;
                case ".eml":
                    compress = true;
                    ctx.Response.ContentType = "message/rfc822"; break;//"application/x-mimearchive"; break;
                case ".json":
                    ctx.Response.ContentEncoding = UTF8Encoding.UTF8;
                    ctx.Response.ContentType = "application/json"; break;
                case ".js":
                    compress = true;
                    ctx.Response.ContentEncoding = UTF8Encoding.UTF8;
                    ctx.Response.ContentType = "application/javascript"; break;
                case ".css":
                    ctx.Response.ContentType = "text/css"; break;
                case ".html":
                case ".htm":
                    compress = true;
                    ctx.Response.ContentEncoding = UTF8Encoding.UTF8;
                    ctx.Response.ContentType = "text/html"; break;
                case ".pdf":
                    ctx.Response.ContentType = "application/pdf"; break;
                case ".doc":
                    ctx.Response.ContentType = "application/msword"; break;
                case ".docx":
                    ctx.Response.ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"; break;
                case ".xls":
                    ctx.Response.ContentType = "application/vnd.ms-excel"; break;
                case ".xlsx":
                    ctx.Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"; break;
                case ".ppt":
                    ctx.Response.ContentType = "application/vnd.ms-powerpoint"; break;
                case ".pptx":
                    ctx.Response.ContentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation"; break;
                case ".docin":
                case ".docout":
                    ctx.Response.ContentEncoding = UTF8Encoding.UTF8;
                    ctx.Response.ContentType = "application/json"; break;
                case ".tabella":
                    ctx.Response.ContentType = "application/tabella"; break;

                default:
                    ctx.Response.ContentType = "application/x-binary"; break;

            }
            return compress;
        }

        public static void WriteResponse(HttpListenerContext ctx, int code, string msg)
        {
            WriteResponse(ctx, code, Encoding.UTF8.GetBytes(msg), true);
        }

        public static void WriteResponse(HttpListenerContext ctx, int code, byte[] data, bool compress)
        {
            ctx.Response.StatusCode = code;
            ctx.Response.AppendHeader("Access-Control-Allow-Origin", "*");
            //ctx.Response.AppendHeader("Access-Control-Allow-Methods", "GET, PUT");
            //ctx.Response.AppendHeader("Access-Control-Allow-Credentials", "true");
            byte[] b = data;
            if (compress == true && b.Length > 10 * 1024)
            {
                _log.Info("original data size : " + b.Length.ToString("#,0"));
                using (var ms = new MemoryStream())
                {
                    using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
                        zip.Write(b, 0, b.Length);
                    b = ms.ToArray();
                }
                _log.Info("compressed size : " + b.Length.ToString("#,0"));
                ctx.Response.AppendHeader("Content-Encoding", "gzip");
            }
            ctx.Response.ContentLength64 = b.LongLength;
            ctx.Response.OutputStream.Write(b, 0, b.Length);
        }
        #endregion

        #region [  private  ]
        private void ListenerCallback(IAsyncResult ar)
        {
            var listener = ar.AsyncState as HttpListener;

            var ctx = listener.EndGetContext(ar);

            string uname = "";

            try
            {
                //do some stuff
                string path = ctx.Request.Url.GetComponents(UriComponents.Path, UriFormat.Unescaped).ToLower();

                if (ctx.User != null && ctx.User.Identity != null)
                    uname = ctx.User.Identity.Name;

                string webpath = "WEB\\";
                webpath = webpath.Replace("\\", "/");
                bool handled = false;
                if (path.StartsWith(_apiPrefix))
                {
                    string command = path.Replace(_apiPrefix + "/", "");
                    if (command.Contains("?"))
                        command = command.Substring(0, command.IndexOf('?') - 1);
                    if (command.Contains("/"))
                        command = command.Substring(0, command.IndexOf('/'));

                    Handler handler = null;

                    if (_handler.TryGetValue(command, out handler))
                    {
                        handled = true;
                        try
                        {
                            handler(ctx);
                        }
                        catch (Exception ex)
                        {
                            _log.Error("Failed on command : " + command);
                            _log.Error("   by user : " + uname);
                            _log.Error(ex);
                        }
                    }
                }

                if (!handled)
                    ServeFile(ctx, path);

                ctx.Response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                _log.Error("   by user : " + uname);
            }
        }

        // serve web from resourse stream
        public virtual void ServeFile(HttpListenerContext ctx, string path)
        {
            string webpath = "WEB\\";
            webpath = webpath.Replace("\\", "/");
            if (path == "")
            {
                ctx.Response.ContentType = "text/html";
                if (Global.UseFileWebResources == false)
                    WriteResponse(ctx, 200, ReadFromStream(_WebCache[(webpath + _startPage).ToLower()]), false);
                else
                    WriteResponse(ctx, 200, File.ReadAllBytes("WEB\\" + _startPage), true);
            }
            else
            {
                if (path.EndsWith(_apiPrefix + ".png") && File.Exists("logo.png"))
                {
                    OutPutContentType(ctx, path);
                    WriteResponse(ctx, 200, File.ReadAllBytes("logo.png"), false);
                }
                else
                {
                    if (Global.UseFileWebResources == false)
                    {
                        if (_WebCache.ContainsKey((webpath + path).ToLower()))
                        {
                            bool compress = OutPutContentType(ctx, path);
                            var o = _WebCache[(webpath + path).ToLower()];
                            WriteResponse(ctx, 200, ReadFromStream(o), compress);
                        }
                        else
                        {
                            _log.Error("path not found : " + path);
                            WriteResponse(ctx, 404, "route path not found : " + ctx.Request.Url.GetComponents(UriComponents.Path, UriFormat.Unescaped));
                        }
                    }
                    else
                    {
                        path = path.Replace("/", "\\");
                        if (File.Exists("WEB\\" + path))
                        {
                            bool compress = OutPutContentType(ctx, path);
                            var s = File.ReadAllBytes("WEB\\" + path);
                            WriteResponse(ctx, 200, s, compress);
                        }
                        else
                        {
                            _log.Error("path not found : " + path);
                            WriteResponse(ctx, 404, "route path not found : " + ctx.Request.Url.GetComponents(UriComponents.Path, UriFormat.Unescaped));
                        }
                    }
                }
            }
        }

        public void Start()
        {
            try
            {
                InitializeCommandHandler(_handler, _apihelp);

                //SaveApiTextFile();

                ReadResources();

                _server = new HttpListener();

                _server.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                if (_authenticationType != AuthenticationSchemes.None)
                    _server.AuthenticationSchemes = _authenticationType;

                if (_localonly)
                {
                    _server.Prefixes.Add("http://localhost:" + _port + "/");
                    _server.Prefixes.Add("http://127.0.0.1:" + _port + "/");
                    _server.Prefixes.Add("http://" + Environment.MachineName + ":" + _port + "/");
                }
                else
                    _server.Prefixes.Add("http://*:" + _port + "/");


                _server.Start();
                while (_run)
                {
                    var context = _server.BeginGetContext(new AsyncCallback(ListenerCallback), _server);
                    context.AsyncWaitHandle.WaitOne();// (5000, true);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        //private void SaveApiTextFile()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.AppendLine("API on port = " + _port);
        //    sb.AppendLine();
        //    var keys = new string[_handler.Keys.Count];
        //    _handler.Keys.CopyTo(keys, 0);
        //    Array.Sort(keys);
        //    foreach (var k in keys)
        //    {
        //        sb.AppendLine(_apiPrefix + "/" + k);
        //    }
        //    File.WriteAllText("api on " + _port + ".txt", sb.ToString());
        //}

        private byte[] ReadFromStream(string name)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Assembly.GetExecutingAssembly().GetManifestResourceStream(name).CopyTo(ms);
                return ms.ToArray();
            }
        }

        private void ReadResources()
        {
            string name = Assembly.GetExecutingAssembly().GetName().Name + ".";
            foreach (var r in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                string s = r.Replace(name, "");
                if (s.StartsWith("WEB"))
                {
                    var ext = Path.GetExtension(s);
                    s = s.Replace(ext, "").Replace(".", "/");
                    var p = s + ext;
                    _WebCache.Add(p.ToLower(), r);
                    if (Global.UseFileWebResources)
                    {
                        string fn = p.Replace("_", "-"); // KLUDGE : for web filename with _
                        Directory.CreateDirectory(Path.GetDirectoryName(fn));
                        File.WriteAllBytes(fn, ReadFromStream(r));
                    }
                }
            }
        }

        #endregion
    }
}