using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Any.Logs;
using Any.Logs.Extentions;

namespace Any.Proxy.Http
{
    public sealed class HttpClient : ClientBase
    {
        private string _httpQuery = "";

        private string _httpPost;

        private StringDictionary _headerFields;

        private string _httpVersion;

        private string _httpRequestType;

        public HttpClient(Socket clientSocket, Action<ClientBase> destroyer) 
            : base(clientSocket, destroyer)
        {
            _httpRequestType = "";
            _httpVersion = "";
        }

        public string RequestedPath { get; set; }

        private string HttpQuery
        {
            get
            {
                return _httpQuery;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                _httpQuery = value;
            }
        }

        public override void StartHandshake()
        {
            try
            {
                ClientSocket.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, OnReceiveQuery, ClientSocket);
            }
            catch
            {
                Dispose();
            }
        }

        private bool IsValidQuery(string Query)
        {
            int index = Query.IndexOf("\r\n\r\n", StringComparison.InvariantCulture);
            if (index == -1)
            {
                return false;
            }
                
            _headerFields = ParseQuery(Query);
            if (_httpRequestType.ToUpper().Equals("POST"))
            {
                try
                {
                    int length = int.Parse(_headerFields["Content-Length"]);
                    return Query.Length >= index + 6 + length;
                }
                catch
                {
                    SendBadRequest();
                    return true;
                }
            }
            return true;
        }

        private void ProcessQuery(string Query)
        {
            _headerFields = ParseQuery(Query);
            if (_headerFields == null || !_headerFields.ContainsKey("Host"))
            {
                SendBadRequest();
                return;
            }
            int Port;
            string Host;
            int ret;
            if (_httpRequestType.ToUpper().Equals("CONNECT"))
            { //HTTPS
                ret = RequestedPath.IndexOf(":", StringComparison.InvariantCulture);
                if (ret >= 0)
                {
                    Host = RequestedPath.Substring(0, ret);
                    if (RequestedPath.Length > ret + 1)
                        Port = int.Parse(RequestedPath.Substring(ret + 1));
                    else
                        Port = 443;
                }
                else
                {
                    Host = RequestedPath;
                    Port = 443;
                }
            }
            else
            { //Normal HTTP
                ret = _headerFields["Host"].IndexOf(":", StringComparison.InvariantCulture);
                if (ret > 0)
                {
                    Host = _headerFields["Host"].Substring(0, ret);
                    Port = int.Parse(_headerFields["Host"].Substring(ret + 1));
                }
                else
                {
                    Host = _headerFields["Host"];
                    Port = 80;
                }
                if (_httpRequestType.ToUpper().Equals("POST"))
                {
                    int index = Query.IndexOf("\r\n\r\n", StringComparison.InvariantCulture);
                    _httpPost = Query.Substring(index + 4);
                }
            }
            try
            {
                //Log.Out.Message(RebuildQuery());
                var DestinationEndPoint = new IPEndPoint(Dns.GetHostAddresses(Host)[0], Port);
                DestinationSocket = new Socket(DestinationEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (_headerFields.ContainsKey("Proxy-Connection") && _headerFields["Proxy-Connection"].ToLower().Equals("keep-alive"))
                    DestinationSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                //else if (_headerFields.ContainsKey("Connection") && _headerFields["Connection"].ToLower().Equals("keep-alive"))
                //    DestinationSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                DestinationSocket.BeginConnect(DestinationEndPoint, OnConnected, DestinationSocket);
            }
            catch
            {
                SendBadRequest();
            }
        }

        private StringDictionary ParseQuery(string Query)
        {
            var retdict = new StringDictionary();
            string[] Lines = Query.Replace("\r\n", "\n").Split('\n');
            int Cnt, Ret;
            //Extract requested URL
            if (Lines.Length > 0)
            {
                //Parse the Http Request Type
                Ret = Lines[0].IndexOf(' ');
                if (Ret > 0)
                {
                    _httpRequestType = Lines[0].Substring(0, Ret);
                    Lines[0] = Lines[0].Substring(Ret).Trim();
                }
                //Parse the Http Version and the Requested Path
                Ret = Lines[0].LastIndexOf(' ');
                if (Ret > 0)
                {
                    _httpVersion = Lines[0].Substring(Ret).Trim();
                    RequestedPath = Lines[0].Substring(0, Ret);
                }
                else
                {
                    RequestedPath = Lines[0];
                }
                //Remove http:// if present
                if (RequestedPath.Length >= 7 && RequestedPath.Substring(0, 7).ToLower().Equals("http://"))
                {
                    Ret = RequestedPath.IndexOf('/', 7);
                    if (Ret == -1)
                        RequestedPath = "/";
                    else
                        RequestedPath = RequestedPath.Substring(Ret);
                }
            }
            for (Cnt = 1; Cnt < Lines.Length; Cnt++)
            {
                Ret = Lines[Cnt].IndexOf(":", StringComparison.InvariantCulture);
                if (Ret > 0 && Ret < Lines[Cnt].Length - 1)
                {
                    try
                    {
                        retdict.Add(Lines[Cnt].Substring(0, Ret), Lines[Cnt].Substring(Ret + 1).Trim());
                    }
                    catch { }
                }
            }
            return retdict;
        }

        private void SendBadRequest()
        {
            string brs = "HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Type: text/html\r\n\r\n<html><head><title>400 Bad Request</title></head><body><div align=\"center\"><table border=\"0\" cellspacing=\"3\" cellpadding=\"3\" bgcolor=\"#C0C0C0\"><tr><td><table border=\"0\" width=\"500\" cellspacing=\"3\" cellpadding=\"3\"><tr><td bgcolor=\"#B2B2B2\"><p align=\"center\"><strong><font size=\"2\" face=\"Verdana\">400 Bad Request</font></strong></p></td></tr><tr><td bgcolor=\"#D1D1D1\"><font size=\"2\" face=\"Verdana\"> The proxy server could not understand the HTTP request!<br><br> Please contact your network administrator about this problem.</font></td></tr></table></center></td></tr></table></div></body></html>";
            try
            {
                ClientSocket.BeginSend(Encoding.UTF8.GetBytes(brs), 0, brs.Length, SocketFlags.None, OnErrorSent, ClientSocket);
            }
            catch
            {
                Dispose();
            }
        }

        private string RebuildQuery()
        {
            string ret = _httpRequestType + " " + RequestedPath + " " + _httpVersion + "\r\n";
            if (_headerFields != null)
            {
                foreach (string sc in _headerFields.Keys)
                {
                    if (sc.Length < 6 || !sc.Substring(0, 6).Equals("proxy-"))
                        ret += System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(sc) + ": " + _headerFields[sc] + "\r\n";
                }
                ret += "\r\n";
                if (_httpPost != null)
                    ret += _httpPost;
            }
            return ret;
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool WithUrl)
        {
            string Ret;
            try
            {
                if (DestinationSocket == null || DestinationSocket.RemoteEndPoint == null)
                    Ret = "Incoming HTTP connection from " + ((IPEndPoint)ClientSocket.RemoteEndPoint).Address;
                else
                    Ret = "HTTP connection from " + ((IPEndPoint)ClientSocket.RemoteEndPoint).Address + " to " + ((IPEndPoint)DestinationSocket.RemoteEndPoint).Address + " on port " + ((IPEndPoint)DestinationSocket.RemoteEndPoint).Port;
                if (_headerFields != null && _headerFields.ContainsKey("Host") && RequestedPath != null)
                    Ret += "\r\n" + " requested URL: http://" + _headerFields["Host"] + RequestedPath;
            }
            catch
            {
                Ret = "HTTP Connection";
            }
            return Ret;
        }

        private void OnReceiveQuery(IAsyncResult ar)
        {
            int Ret;
            try
            {
                Ret = ClientSocket.EndReceive(ar);
            }
            catch
            {
                Ret = -1;
            }
            if (Ret <= 0)
            { //Connection is dead :(
                Dispose();
                return;
            }
            HttpQuery += Encoding.UTF8.GetString(Buffer, 0, Ret);
            Log.Out.Info(HttpQuery);
            //if received data is valid HTTP request...
            if (IsValidQuery(HttpQuery))
            {
                ProcessQuery(HttpQuery);
                //else, keep listening
            }
            else
            {
                try
                {
                    ClientSocket.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, OnReceiveQuery, ClientSocket);
                }
                catch
                {
                    Dispose();
                }
            }
        }

        private void OnErrorSent(IAsyncResult ar)
        {
            try
            {
                ClientSocket.EndSend(ar);
            }
            catch { }
            Dispose();
        }

        private void OnConnected(IAsyncResult ar)
        {
            try
            {
                DestinationSocket.EndConnect(ar);
                string rq;
                if (_httpRequestType.ToUpper().Equals("CONNECT"))
                { //HTTPS
                    rq = _httpVersion + " 200 Connection established\r\nProxy-Agent: Mentalis Proxy Server\r\n\r\n";
                    ClientSocket.BeginSend(Encoding.UTF8.GetBytes(rq), 0, rq.Length, SocketFlags.None, OnOkSent, ClientSocket);
                }
                else
                { //Normal HTTP
                    rq = RebuildQuery();
                    //Log.Out.Message(rq);
                    DestinationSocket.BeginSend(Encoding.UTF8.GetBytes(rq), 0, rq.Length, SocketFlags.None, OnQuerySent, DestinationSocket);
                }
            }
            catch
            {
                Dispose();
            }
        }

        private void OnQuerySent(IAsyncResult ar)
        {
            try
            {
                if (DestinationSocket.EndSend(ar) == -1)
                {
                    Dispose();
                    return;
                }
                StartRelay();
            }
            catch
            {
                Dispose();
            }
        }

        private void OnOkSent(IAsyncResult ar)
        {
            try
            {
                if (ClientSocket.EndSend(ar) == -1)
                {
                    Dispose();
                    return;
                }
                StartRelay();
            }
            catch
            {
                Dispose();
            }
        }
        
    }

}
