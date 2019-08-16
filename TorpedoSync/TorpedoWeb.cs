using fastJSON;
using RaptorDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace TorpedoSync
{
    class TorpedoWeb : CoreWebServer
    {
        public TorpedoWeb(int HttpPort,
            bool localonly,
            bool embeddedresources,
            Action<Connection> startque,
            Action<Connection> removeque,
            Func<Connection, ConnectionInfo> getconninfo,
            AuthenticationSchemes authenticationType,
            string apiPrefix) : base(HttpPort, localonly, embeddedresources, authenticationType, "api", "index.html")
        {
            StartQue = startque;
            RemoveQue = removeque;
            GetConnInfo = getconninfo;
        }

        private Action<Connection> StartQue;
        private Action<Connection> RemoveQue;
        private Func<Connection, ConnectionInfo> GetConnInfo;

        public override void InitializeCommandHandler(Dictionary<string, Handler> handler, Dictionary<string, string> apihelp)
        {
            var jp = new JSONParameters { UseExtensions = false, UseFastGuid = false, EnableAnonymousTypes = true };

            handler.Add("createshare", ctx => // ? sharename & path
            {
                OutPutContentType(ctx, "*.json");
                string str = ctx.Request.Url.GetComponents(UriComponents.Query, UriFormat.Unescaped);
                var s = str.Split('&');
                var sh = s[0];
                var path = s[1];
                if (path.EndsWith("" + Path.DirectorySeparatorChar) == false)
                    path += Path.DirectorySeparatorChar;

                var f = Global.Shares.Find(x => x.Name.ToLower() == sh.ToLower());
                if (f == null)
                {
                    Share ns = new Share();
                    ns.Name = sh;
                    ns.Path = path;
                    Global.Shares.Add(ns);
                    ServerCommands.SaveConfig();

                    WriteResponse(ctx, 200, "true");
                    return;
                }
                WriteResponse(ctx, 200, "false");
            });

            handler.Add("connect", ctx => //  ? token  & path  
            {
                OutPutContentType(ctx, "*.json");
                string str = ctx.Request.Url.GetComponents(UriComponents.Query, UriFormat.Unescaped);
                var ss = str.Split('&');
                var c = ClientCommands.CanConnect(ss[0].Trim());
                if (c != null)
                {
                    var cc = ClientCommands.Connect(c.MachineName, c.Name, c.Token);
                    if (cc != "")
                    {
                        c.Path = ss[1].Trim();
                        if (c.Path.EndsWith("" + Path.DirectorySeparatorChar) == false)
                            c.Path += Path.DirectorySeparatorChar;

                        if (ServerCommands.Connect(c) != "")
                        {
                            StartQue(c);
                            WriteResponse(ctx, 200, "true");
                            return;
                        }
                    }
                }
                WriteResponse(ctx, 200, "false");
            });

            handler.Add("connection.confirm", ctx => // ? machinename & share & bool
            {
                OutPutContentType(ctx, "*.json");
                string str = ctx.Request.Url.GetComponents(UriComponents.Query, UriFormat.Unescaped);
                var s = str.Split('&');
                var mn = s[0];
                var sh = s[1];
                var t = s[2];
                var f = Global.ConnectionList.Find(x => x.MachineName == mn && x.Name == sh);
                if (f != null)
                {
                    f.isConfirmed = t.ToLower() == "true" || t.ToLower() == "t";

                    ServerCommands.SaveConnectionList();

                    WriteResponse(ctx, 200, "true");
                    return;
                }
                WriteResponse(ctx, 200, "false");
            });

            handler.Add("connection.remove", ctx => // ? machinename & share 
            {
                OutPutContentType(ctx, "*.json");
                string str = ctx.Request.Url.GetComponents(UriComponents.Query, UriFormat.Unescaped);
                var s = str.Split('&');
                var mn = s[0];
                var sh = s[1];
                var f = Global.ConnectionList.Find(x => x.MachineName == mn && x.Name == sh);
                if (f != null)
                {
                    RemoveQue(f);

                    WriteResponse(ctx, 200, "true");
                    return;
                }
                WriteResponse(ctx, 200, "false");
            });

            handler.Add("connection.getinfo", ctx => // ? machine & share
            {
                OutPutContentType(ctx, "*.json");
                string str = ctx.Request.Url.GetComponents(UriComponents.Query, UriFormat.Unescaped);
                var s = str.Split('&');
                var mn = s[0];
                var sh = s[1];
                var f = Global.ConnectionList.Find(x => x.MachineName == mn && x.Name == sh);
                if (f != null)
                {
                    var o = GetConnInfo(f);
                    var j = JSON.ToJSON(o, jp);

                    WriteResponse(ctx, 200, j);
                    return;
                }
                WriteResponse(ctx, 200, "{}");
            });

            handler.Add("connection.pauseresume", ctx => // ? machinename & share & true
            {
                OutPutContentType(ctx, "*.json");
                string str = ctx.Request.Url.GetComponents(UriComponents.Query, UriFormat.Unescaped);
                var ss = str.Split('&');

                var cs = Global.ConnectionList.Find(x => x.MachineName == ss[0].Trim() && x.Name == ss[1].Trim());
                if (cs != null)
                {
                    cs.isPaused = ss[2].Trim() == "true" ? true : false;
                    ServerCommands.SaveConnectionList();

                    WriteResponse(ctx, 200, "true");
                    return;
                }

                WriteResponse(ctx, 200, "false");
            });

            handler.Add("share.pauseresume", ctx => // ? share & true
            {
                OutPutContentType(ctx, "*.json");
                string str = ctx.Request.Url.GetComponents(UriComponents.Query, UriFormat.Unescaped);
                var ss = str.Split('&');

                var s = Global.Shares.Find(x => x.Name == ss[0].Trim());
                if (s != null)
                {
                    foreach (var cs in Global.ConnectionList.FindAll(x => x.Name == ss[0].Trim()))
                    {
                        cs.isPaused = ss[1].Trim() == "true" ? true : false;
                    }
                    ServerCommands.SaveConnectionList();
                    WriteResponse(ctx, 200, "true");
                    return;
                }
                WriteResponse(ctx, 200, "false");
            });

            handler.Add("share.add", ctx =>
            {
                OutPutContentType(ctx, "*.json");
                string text = null;
                using (var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                {
                    text = reader.ReadToEnd();
                }

                var o = JSON.ToObject<Share>(text);
                var s = Global.Shares.Find(x => x.Name == o.Name);
                if (s == null)
                {
                    if (o.Path.EndsWith("" + Path.DirectorySeparatorChar) == false)
                        o.Path += Path.DirectorySeparatorChar;
                    Global.Shares.Add(o);

                    ServerCommands.SaveConfig();

                    WriteResponse(ctx, 200, "true");
                    return;
                }
                WriteResponse(ctx, 200, "false");
            });

            handler.Add("share.remove", ctx => // ? sharename
            {
                OutPutContentType(ctx, "*.json");
                string str = ctx.Request.Url.GetComponents(UriComponents.Query, UriFormat.Unescaped);

                var sh = Global.Shares.Find(x => x.Name == str.Trim());
                if(sh!=null)
                {
                    Global.Shares.Remove(sh);

                    // remove connections also
                    foreach(var c in Global.ConnectionList.FindAll(x=>x.Name == str.Trim()))
                    {
                        RemoveQue(c);
                    }

                    ServerCommands.SaveConfig();

                    WriteResponse(ctx, 200, "true");
                    return;
                }
                WriteResponse(ctx, 200, "false");
            });

            handler.Add("getshares", ctx =>
            {
                OutPutContentType(ctx, "*.json");
                WriteResponse(ctx, 200, fastJSON.JSON.ToJSON(Global.Shares, jp));
            });

            handler.Add("getconnections", ctx =>
            {
                OutPutContentType(ctx, "*.json");
                WriteResponse(ctx, 200, fastJSON.JSON.ToJSON(Global.ConnectionList, jp));
            });

            handler.Add("pausets", ctx => // ? 0 or 1
            {
                string str = ctx.Request.Url.GetComponents(UriComponents.Query, UriFormat.Unescaped);
                if (str == "1")
                    Global.PauseAll = true;
                else
                    Global.PauseAll = false;
                OutPutContentType(ctx, ".json");
                WriteResponse(ctx, 200, "true");
            });

            handler.Add("getlogs", ctx =>
            {
                OutPutContentType(ctx, ".json");
                WriteResponse(ctx, 200, JSON.ToJSON(LogManager.GetLastLogs(), jp));
            });

            handler.Add("getconfigs", ctx =>
            {
                var o = new
                {
                    Version = FileVersionInfo.GetVersionInfo(this.GetType().Assembly.Location).FileVersion,
                    Globals = new Global(),
                    Global.FreeMemoryTimerMin,
                    Global.SaveQueueEvery_X_Minute,
                    Global.PauseAll
                };

                OutPutContentType(ctx, ".json");
                WriteResponse(ctx, 200, JSON.ToJSON(o, jp));
            });
        }
    }
}
