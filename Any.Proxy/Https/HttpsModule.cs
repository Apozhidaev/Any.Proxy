using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Any.Proxy.Https
{
    public class HttpsModule :  IProxyModule
    {
        private readonly int _port;
        private readonly IPAddress _address;
        private Socket _listenSocket;
        public bool _isDisposed;
        private readonly LinkedList<HttpsClient> _clients = new LinkedList<HttpsClient>();

        public HttpsModule(int Port) : this(IPAddress.Any, Port) { }

        public HttpsModule(IPAddress address, int port)
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

        protected void AddClient(HttpsClient client)
        {
            if (!_clients.Contains(client))
            {
                _clients.AddLast(client);
            }
        }

        protected void RemoveClient(HttpsClient client)
        {
            _clients.Remove(client);
        }

        public int GetClientCount()
        {
            return _clients.Count;
        }

        public bool Listening
        {
            get
            {
                return _listenSocket != null;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            while (_clients.Count > 0)
            {
                _clients.First.Value.Dispose();
            }
            try
            {
                _listenSocket.Shutdown(SocketShutdown.Both);
            }
            catch { }
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
                    var NewClient = new HttpsClient(NewSocket, RemoveClient);
                    AddClient(NewClient);
                    NewClient.StartHandshake();
                }
            }
            catch { }
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

        public override string ToString()
        {
            return "Http service on " + _address + ":" + _port;
        }
    }

}
