using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Ap.Logs;
using Ap.Proxy.Loggers;

namespace Ap.Proxy.Http
{
    public class Connection : IDisposable
    {
        private readonly IBridge _bridge;
        private ConnectionType _type = ConnectionType.None;
        private string _host;
        private int _port;
        private bool _opened;

        public Connection(IBridge bridge)
        {
            _bridge = bridge;
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; }

        public bool Expired
        {
            get
            {
                if (_opened)
                {
                    return _bridge.LastActivity < DateTime.UtcNow.AddMilliseconds(-5000) || !_bridge.Connected;
                }
                return _bridge.LastActivity < DateTime.UtcNow.AddSeconds(-30);
            }
        }

        public Task<bool> Ping()
        {
            return _bridge.Ping();
        } 

        public void Dispose()
        {
            _bridge.Dispose();
        }

        public void Open()
        {
            _bridge.ReadToAsync(IsValidRequest).ContinueWith(_ =>
            {
                Init(_.Result);
                try
                {
                    _bridge.HandshakeAsync(Id, _host, _port).ContinueWith(__ =>
                    {
                        _opened = true;
                        if (__.Exception != null)
                        {
                            SendBadRequest().ContinueWith(___ => Dispose());
                            return;
                        }
                        if (_type == ConnectionType.Http)
                        {
                            _bridge.WriteFromAsync(_.Result)
                                .ContinueWith(___ => _bridge.RelayFromAsync(IsValidResponse)
                                    .ContinueWith(____ => Dispose()));
                        }
                        else if (_type == ConnectionType.Https)
                        {
                            string rq = "HTTP/1.1 200 Connection established\r\nProxy-Agent: Ap.Proxy\r\n\r\n";
                            _bridge.WriteToAsync(Encoding.ASCII.GetBytes(rq))
                                .ContinueWith(___ => _bridge.RelayAsync().ContinueWith(____ => Dispose()));
                        }
                        else
                        {
                            SendBadRequest().ContinueWith(___ => Dispose());
                        }
                        
                    });
                }
                catch
                {
                    SendBadRequest().ContinueWith(__ => Dispose());
                }
            });
        }

        private bool IsValidRequest(string query)
        {
            if (String.IsNullOrEmpty(query)) return true;
            int index = query.IndexOf("\r\n\r\n", StringComparison.InvariantCulture);
            if (index == -1)
            {
                return false;
            }

            var headerFields = ParseHeaders(query);
            if (headerFields.ContainsKey("Content-Length"))
            {
                int length = int.Parse(headerFields["Content-Length"]);
                return query.Length >= index + 4 + length;
            }
            return true;
        }

        private bool IsValidResponse(string query)
        {
            int index = query.IndexOf("\r\n\r\n", StringComparison.InvariantCulture);
            if (index == -1)
            {
                return false;
            }

            var headerFields = ParseHeaders(query);
            if (headerFields.ContainsKey("Content-Length"))
            {
                int length = int.Parse(headerFields["Content-Length"]);
                return query.Length >= index + 4 + length;
            }
            if(headerFields.ContainsKey("Transfer-Encoding") && headerFields["Transfer-Encoding"] == "chunked" && query.Length > 4)
            {
                var temp = query.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
                return temp[temp.Length - 1] == "0";
            }
            return true;
        }

        private void Init(byte[] bytes)
        {
            var query = Encoding.ASCII.GetString(bytes);
            var headerFields = ParseHeaders(query);
            if (headerFields == null || !headerFields.ContainsKey("Host"))
            {
                Log.Out.Error(Id, "Init.Host");
                return;
            }
            var ret = query.IndexOf(" ", StringComparison.InvariantCulture);
            var method = query.Substring(0, ret).ToUpper();
            _type = method == "CONNECT" ? ConnectionType.Https : ConnectionType.Http;
            ret = headerFields["Host"].IndexOf(":", StringComparison.Ordinal);
            if (ret > 0)
            {
                _host = headerFields["Host"].Substring(0, ret);
                _port = int.Parse(headerFields["Host"].Substring(ret + 1));
            }
            else
            {
                _host = headerFields["Host"];
                _port = _type == ConnectionType.Https ? 443 : 80;
            }
            Log.Out.Info(Id, query, _host);
        }

        private Dictionary<string, string> ParseHeaders(string query)
        {
            var retdict = new Dictionary<string, string>();
            if (!String.IsNullOrEmpty(query))
            {
                string[] lines = query.Split(new[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < lines.Length; i++)
                {
                    int ret = lines[i].IndexOf(":", StringComparison.InvariantCulture);
                    if (ret > 0 && ret < lines[i].Length - 1)
                    {
                        try
                        {
                            retdict.Add(lines[i].Substring(0, ret), lines[i].Substring(ret + 1).Trim());
                        }
                        catch (Exception e)
                        {
                            Log.Out.Error(e, Id, "ParseQuery");
                        }
                    }
                }
            }
            return retdict;
        }

        private Task SendBadRequest()
        {
            var brs = new StringBuilder();
              brs.Append("HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Type: text/html\r\n\r\n");
              brs.Append("<html>");
              brs.Append("<head><title>400 Bad Request</title></head>");
              brs.Append("<body><div align=\"center\">");
              brs.Append("<table border=\"0\" cellspacing=\"3\" cellpadding=\"3\" bgcolor=\"#90d5ec\"><tr><td>");
              brs.Append("<table border=\"0\" width=\"500\" cellspacing=\"3\" cellpadding=\"3\"><tr><td bgcolor=\"#e9f7fb\">");
              brs.Append("<tr><td bgcolor=\"#e9f7fb\">");
              brs.Append("<p align=\"center\"><strong><font size=\"2\" face=\"Verdana\">400 Bad Request</font></strong></p>");
              brs.Append("</td></tr>");
              brs.Append("<tr><td bgcolor=\"#e9f7fb\">");
              brs.Append("<font size=\"2\" face=\"Verdana\"> The proxy server could not understand the HTTP request!<br><br> Please contact your network administrator about this problem.</font>");
              brs.Append("</td></tr></table>");
              brs.Append("</td></tr></table>");
              brs.Append("</div></body>");
              brs.Append("</html>");
            return _bridge.WriteToAsync(Encoding.ASCII.GetBytes(brs.ToString()));
        }
    }
}