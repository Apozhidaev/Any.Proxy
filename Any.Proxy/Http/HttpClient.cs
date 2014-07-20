using System;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Any.Proxy.Http
{

    ///<summary>Relays HTTP data between a remote host and a local client.</summary>
    ///<remarks>This class supports both HTTP and HTTPS.</remarks>
    public sealed class HttpClient : ClientBase
    {
        // private variables
        /// <summary>Holds the value of the HttpQuery property.</summary>
        private string _httpQuery = "";

        /// <summary>Holds the POST data</summary>
        private string _httpPost;

        ///<summary>Gets or sets a StringDictionary that stores the header fields.</summary>
        ///<value>A StringDictionary that stores the header fields.</value>
        private StringDictionary _headerFields;

        ///<summary>Gets or sets the HTTP version the client uses.</summary>
        ///<value>A string representing the requested HTTP version.</value>
        private string _httpVersion;

        ///<summary>Gets or sets the HTTP request type.</summary>
        ///<remarks>
        ///Usually, this string is set to one of the three following values:
        ///<list type="bullet">
        ///<item>GET</item>
        ///<item>POST</item>
        ///<item>CONNECT</item>
        ///</list>
        ///</remarks>
        ///<value>A string representing the HTTP request type.</value>
        private string _httpRequestType;

        ///<summary>Initializes a new instance of the HttpClient class.</summary>
        ///<param name="clientSocket">The <see cref ="Socket">Socket</see> connection between this proxy server and the local client.</param>
        ///<param name="destroyer">The callback method to be called when this Client object disconnects from the local client and the remote server.</param>
        public HttpClient(Socket clientSocket, Action<ClientBase> destroyer) 
            : base(clientSocket, destroyer)
        {
            _httpRequestType = "";
            _httpVersion = "";
        }

        

        ///<summary>Gets or sets the requested path.</summary>
        ///<value>A string representing the requested path.</value>
        public string RequestedPath { get; set; }

        ///<summary>Gets or sets the query string, received from the client.</summary>
        ///<value>A string representing the HTTP query string.</value>
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
        ///<summary>Starts receiving data from the client connection.</summary>
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
        ///<summary>Checks whether a specified string is a valid HTTP query string.</summary>
        ///<param name="Query">The query to check.</param>
        ///<returns>True if the specified string is a valid HTTP query, false otherwise.</returns>
        private bool IsValidQuery(string Query)
        {
            int index = Query.IndexOf("\r\n\r\n", StringComparison.InvariantCulture);
            if (index == -1)
                return false;
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
        ///<summary>Processes a specified query and connects to the requested HTTP web server.</summary>
        ///<param name="Query">A string containing the query to process.</param>
        ///<remarks>If there's an error while processing the HTTP request or when connecting to the remote server, the Proxy sends a "400 - Bad Request" error to the client.</remarks>
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
                var DestinationEndPoint = new IPEndPoint(Dns.Resolve(Host).AddressList[0], Port);
                DestinationSocket = new Socket(DestinationEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (_headerFields.ContainsKey("Proxy-Connection") && _headerFields["Proxy-Connection"].ToLower().Equals("keep-alive"))
                    DestinationSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                DestinationSocket.BeginConnect(DestinationEndPoint, OnConnected, DestinationSocket);
            }
            catch
            {
                SendBadRequest();
            }
        }
        ///<summary>Parses a specified HTTP query into its header fields.</summary>
        ///<param name="Query">The HTTP query string to parse.</param>
        ///<returns>A StringDictionary object containing all the header fields with their data.</returns>
        ///<exception cref="ArgumentNullException">The specified query is null.</exception>
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
        ///<summary>Sends a "400 - Bad Request" error to the client.</summary>
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
        ///<summary>Rebuilds the HTTP query, starting from the HttpRequestType, RequestedPath, HttpVersion and HeaderFields properties.</summary>
        ///<returns>A string representing the rebuilt HTTP query string.</returns>
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
        ///<summary>Returns text information about this HttpClient object.</summary>
        ///<returns>A string representing this HttpClient object.</returns>
        public override string ToString()
        {
            return ToString(false);
        }
        ///<summary>Returns text information about this HttpClient object.</summary>
        ///<returns>A string representing this HttpClient object.</returns>
        ///<param name="WithUrl">Specifies whether or not to include information about the requested URL.</param>
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
        ///<summary>Called when we received some data from the client connection.</summary>
        ///<param name="ar">The result of the asynchronous operation.</param>
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
        ///<summary>Called when the Bad Request error has been sent to the client.</summary>
        ///<param name="ar">The result of the asynchronous operation.</param>
        private void OnErrorSent(IAsyncResult ar)
        {
            try
            {
                ClientSocket.EndSend(ar);
            }
            catch { }
            Dispose();
        }
        ///<summary>Called when we're connected to the requested remote host.</summary>
        ///<param name="ar">The result of the asynchronous operation.</param>
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
                    DestinationSocket.BeginSend(Encoding.UTF8.GetBytes(rq), 0, rq.Length, SocketFlags.None, OnQuerySent, DestinationSocket);
                }
            }
            catch
            {
                Dispose();
            }
        }
        ///<summary>Called when the HTTP query has been sent to the remote host.</summary>
        ///<param name="ar">The result of the asynchronous operation.</param>
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
        ///<summary>Called when an OK reply has been sent to the local client.</summary>
        ///<param name="ar">The result of the asynchronous operation.</param>
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
