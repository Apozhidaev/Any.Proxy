using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Ap.Logs;
using Ap.Proxy.Loggers;

namespace Ap.Proxy.Http
{
    public class Connection : IDisposable
    {
        private readonly Action<Connection> _destroyer;
        private readonly IBridge _bridge;
        private readonly string _id;

        public Connection(IBridge bridge, Action<Connection> destroyer)
        {
            _bridge = bridge;
            _destroyer = destroyer;
            _id = Guid.NewGuid().ToString();
        }

        public string Id => _id;

        public void Dispose()
        {
            _bridge?.Dispose();
            _destroyer(this);
        }

        public void Open()
        {
            _bridge.ReadToAsync(IsValidRequest).ContinueWith(__ =>
            {
                ProcessRequest(__.Result);
            });
        }

        private bool IsValidRequest(string query)
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
            if(headerFields.ContainsKey("Transfer-Encoding") && headerFields["Transfer-Encoding"] == "chunked")
            {
                var temp = query.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
                return temp[temp.Length - 1] == "0";
            }
            return true;
        }

        private void ProcessRequest(byte[] bytes)
        {
            var query = Encoding.ASCII.GetString(bytes);
            var headerFields = ParseHeaders(query);
            if (headerFields == null || !headerFields.ContainsKey("Host"))
            {
                Log.Out.Error(_id, "ProcessRequest");
                SendBadRequest();
                return;
            }
            int port;
            string host;
            int ret = headerFields["Host"].IndexOf(":", StringComparison.Ordinal);
            if (ret > 0)
            {
                host = headerFields["Host"].Substring(0, ret);
                port = int.Parse(headerFields["Host"].Substring(ret + 1));
            }
            else
            {
                host = headerFields["Host"];
                port = 80;
            }
            Log.Out.Info(_id, query, host);
            _bridge.HandshakeAsync(_id, host, port).ContinueWith(_ =>
            {
                if (_.Exception != null)
                {
                    Log.Out.Error(_.Exception, _id, "ProcessRequest.HandshakeAsync");
                    SendBadRequest();
                    return;
                }
                _bridge.WriteFromAsync(bytes)
                        .ContinueWith(__ => _bridge.RelayFromAsync(IsValidResponse)
                        .ContinueWith(____ => Dispose()));
            });
        }

        private Dictionary<string, string> ParseHeaders(string query)
        {
            var retdict = new Dictionary<string, string>();
            string[] lines = query.Split(new [] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)[0].Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
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
                        Log.Out.Error(e, _id, "ParseQuery");
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
            return _bridge.WriteToAsync(Encoding.ASCII.GetBytes(brs.ToString())).ContinueWith(_ => Dispose());
        }
    }
}