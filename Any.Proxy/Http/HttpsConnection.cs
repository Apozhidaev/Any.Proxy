using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Any.Proxy.Http
{
    public class HttpsConnection : IDisposable
    {
        private readonly byte[] _buffer = new byte[40960];
        private readonly Action<HttpsConnection> _destroyer;
        private Socket _clientSocket;
        private StringDictionary _headerFields;
        private string _httpQuery = "";
        private string _httpRequestType;
        private string _httpVersion;
        private string _requestedPath;
        private TcpBridge _tcpBridge;

        public HttpsConnection(Socket clientSocket, Action<HttpsConnection> destroyer)
        {
            _httpRequestType = "";
            _httpVersion = "";
            _clientSocket = clientSocket;
            _destroyer = destroyer;
        }

        public void Dispose()
        {
            try
            {
                _clientSocket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

            //Close the sockets
            if (_clientSocket != null)
                _clientSocket.Close();
            if (_tcpBridge != null)
                _tcpBridge.Dispose();
            //Clean up
            _clientSocket = null;
            _tcpBridge = null;
            if (_destroyer != null)
                _destroyer(this);
        }

        public void StartHandshake()
        {
            try
            {
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnReceiveQuery, _clientSocket);
            }
            catch
            {
                Dispose();
            }
        }

        private void OnReceiveQuery(IAsyncResult ar)
        {
            int Ret;
            try
            {
                Ret = _clientSocket.EndReceive(ar);
            }
            catch
            {
                Ret = -1;
            }
            if (Ret <= 0)
            {
                //Connection is dead :(
                Dispose();
                return;
            }
            _httpQuery += Encoding.UTF8.GetString(_buffer, 0, Ret);

            //if received data is valid HTTP request...
            if (IsValidQuery(_httpQuery))
            {
                ProcessQuery(_httpQuery);
                //else, keep listening
            }
            else
            {
                try
                {
                    _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnReceiveQuery,
                        _clientSocket);
                }
                catch
                {
                    Dispose();
                }
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
            {
                //HTTPS
                ret = _requestedPath.IndexOf(":", StringComparison.InvariantCulture);
                if (ret >= 0)
                {
                    Host = _requestedPath.Substring(0, ret);
                    if (_requestedPath.Length > ret + 1)
                        Port = int.Parse(_requestedPath.Substring(ret + 1));
                    else
                        Port = 443;
                }
                else
                {
                    Host = _requestedPath;
                    Port = 443;
                }
            }
            else
            {
                throw new Exception("Normal HTTP");
            }
            try
            {
                var DestinationEndPoint = new IPEndPoint(Dns.GetHostAddresses(Host)[0], Port);

                _tcpBridge = new TcpBridge(_clientSocket, DestinationEndPoint,
                    _headerFields.ContainsKey("Proxy-Connection") &&
                    _headerFields["Proxy-Connection"].ToLower().Equals("keep-alive"));
                _tcpBridge.HandshakeAsync().ContinueWith(_ =>
                {
                    string rq;
                    if (_httpRequestType.ToUpper().Equals("CONNECT"))
                    {
                        //HTTPS
                        rq = _httpVersion + " 200 Connection established\r\nProxy-Agent: Mentalis Proxy Server\r\n\r\n";
                        _clientSocket.WriteAsync(Encoding.UTF8.GetBytes(rq))
                            .ContinueWith(__ => _tcpBridge.RelayAsync().ContinueWith(___ => Dispose()));
                    }
                    else
                    {
                        throw new Exception("Normal HTTP");
                    }
                });
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
                    _requestedPath = Lines[0].Substring(0, Ret);
                }
                else
                {
                    _requestedPath = Lines[0];
                }
                //Remove http:// if present
                if (_requestedPath.Length >= 7 && _requestedPath.Substring(0, 7).ToLower().Equals("http://"))
                {
                    Ret = _requestedPath.IndexOf('/', 7);
                    if (Ret == -1)
                        _requestedPath = "/";
                    else
                        _requestedPath = _requestedPath.Substring(Ret);
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
                    catch
                    {
                    }
                }
            }
            return retdict;
        }

        private void SendBadRequest()
        {
            const string brs =
                "HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Type: text/html\r\n\r\n<html><head><title>400 Bad Request</title></head><body><div align=\"center\"><table border=\"0\" cellspacing=\"3\" cellpadding=\"3\" bgcolor=\"#C0C0C0\"><tr><td><table border=\"0\" width=\"500\" cellspacing=\"3\" cellpadding=\"3\"><tr><td bgcolor=\"#B2B2B2\"><p align=\"center\"><strong><font size=\"2\" face=\"Verdana\">400 Bad Request</font></strong></p></td></tr><tr><td bgcolor=\"#D1D1D1\"><font size=\"2\" face=\"Verdana\"> The proxy server could not understand the HTTP request!<br><br> Please contact your network administrator about this problem.</font></td></tr></table></center></td></tr></table></div></body></html>";
            _clientSocket.WriteAsync(Encoding.UTF8.GetBytes(brs)).ContinueWith(_ => Dispose());
        }
    }
}