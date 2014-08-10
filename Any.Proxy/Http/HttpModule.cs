using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Any.Proxy.Http.Configuration;

namespace Any.Proxy.Http
{
    public class HttpModule : IProxyModule
    {
        private readonly IPAddress _address;
        private readonly LinkedList<HttpConnection> _connections = new LinkedList<HttpConnection>();
        private readonly int _port;
        public bool _isDisposed;
        private Socket _listenSocket;

        public HttpModule(HttpElement config)
        {
            _isDisposed = false;
            _port = config.Port;
            _address = Proxy.GetIP(config.Host);
        }

        public void Start()
        {
            try
            {
                _listenSocket = new Socket(_address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _listenSocket.Bind(new IPEndPoint(_address, _port));
                _listenSocket.Listen(500);
                _listenSocket.BeginAccept(OnAccept, _listenSocket);
            }
            catch
            {
                _listenSocket = null;
                throw new SocketException();
            }
        }

        protected void Restart()
        {
            //If we weren't listening, do nothing
            if (_listenSocket == null)
                return;
            _listenSocket.Close();
            Start();
        }

        protected void AddConnection(HttpConnection connection)
        {
            if (!_connections.Contains(connection))
            {
                _connections.AddLast(connection);
            }
        }

        protected void RemoveConnection(HttpConnection connection)
        {
            lock (_connections)
            {
                if (_connections.Contains(connection))
                {
                    _connections.Remove(connection);
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            while (_connections.Count > 0)
            {
                _connections.First.Value.Dispose();
            }
            try
            {
                _listenSocket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }
            if (_listenSocket != null)
                _listenSocket.Close();
            _isDisposed = true;
        }

        public void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket NewSocket = _listenSocket.EndAccept(ar);
                if (NewSocket != null)
                {
                    var NewClient = new HttpConnection(NewSocket, (host, port, isKeepAlive) => new TcpBridge(NewSocket, host, port, isKeepAlive), RemoveConnection);
                    AddConnection(NewClient);
                    NewClient.StartHandshake();
                }
            }
            catch
            {
            }
            try
            {
                //Restart Listening
                _listenSocket.BeginAccept(OnAccept, _listenSocket);
            }
            catch
            {
                Dispose();
            }
        }
    }
}