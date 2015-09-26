﻿using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Ap.Proxy.Https
{
    public abstract class HttpsModuleBase : IProxyModule
    {
        private readonly IPAddress _address;
        private readonly LinkedList<HttpsConnection> _connections = new LinkedList<HttpsConnection>();
        private readonly int _port;
        protected TcpListener _tcpListener;

        protected HttpsModuleBase(string host, int port)
        {
            _port = port;
            _address = Proxy.GetIP(host);
        }

        public async void Start()
        {
            try
            {
                _tcpListener = new TcpListener(_address, _port);
                _tcpListener.Start();
                while (true)
                {
                    await Accept(await _tcpListener.AcceptTcpClientAsync());
                }
            }
            finally
            {
                _tcpListener?.Stop();
            }
        }

        protected void Restart()
        {
            _tcpListener?.Stop();
            Start();
        }

        protected void AddConnection(HttpsConnection connection)
        {
            lock (_connections)
            {
                if (!_connections.Contains(connection))
                {
                    _connections.AddLast(connection);
                }
            }
        }

        protected void RemoveConnection(HttpsConnection connection)
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
            while (_connections.Count > 0)
            {
                _connections.First.Value.Dispose();
            }
            _tcpListener?.Stop();
        }

        protected async Task Accept(TcpClient client)
        {
            await Task.Yield();
            OnAccept(client);
        }

        protected abstract void OnAccept(TcpClient client);
    }
}
