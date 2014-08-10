using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Any.Proxy.Http
{
    public class HttpsUnit
    {
        private readonly IPAddress _address;
        private readonly LinkedList<HttpsConnection> _connections = new LinkedList<HttpsConnection>();
        private readonly int _port;
        public bool _isDisposed;
        private Socket _listenSocket;

        public HttpsUnit(int Port) : this(IPAddress.Any, Port)
        {
        }

        public HttpsUnit(IPAddress address, int port)
        {
            _isDisposed = false;
            _port = port;
            _address = address;
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

        protected void AddConnection(HttpsConnection connection)
        {
            if (!_connections.Contains(connection))
            {
                _connections.AddLast(connection);
            }
        }

        protected void RemoveConnection(HttpsConnection connection)
        {
            _connections.Remove(connection);
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
                    var NewClient = new HttpsConnection(NewSocket, RemoveConnection);
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