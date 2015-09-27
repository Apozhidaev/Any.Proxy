using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ap.Proxy.Http
{
    public abstract class HttpModuleBase : IProxyModule
    {
        private readonly IPAddress _address;
        protected readonly LinkedList<Connection> _connections = new LinkedList<Connection>();
        private readonly int _port;
        protected TcpListener _tcpListener;
        private Timer _timer;

        protected HttpModuleBase(string host, int port)
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
                _timer = new Timer(_ => Check());
                _timer.Change(1000, 1000);
                while (true)
                {
                    await Accept(await _tcpListener.AcceptTcpClientAsync());
                }
            }
            finally 
            {
                _tcpListener?.Stop();
                _timer?.Dispose();
            }
        }

        protected virtual void Check()
        {
            lock (_connections)
            {
                foreach (var connection in _connections.Where(connection => connection.Expired).ToList())
                {
                    connection.Dispose();
                    _connections.Remove(connection);
                }
            }
#if DEBUG
            Console.WriteLine(_connections.Count);
#endif
        }

        protected void AddConnection(Connection connection)
        {
            lock (_connections)
            {
                _connections.AddLast(connection);
            }
        }

        public void Dispose()
        {
            while (_connections.Count > 0)
            {
                _connections.First.Value.Dispose();
                _connections.RemoveFirst();
            }
            _tcpListener?.Stop();
            _timer?.Dispose();
        }

        protected async Task Accept(TcpClient client)
        {
            await Task.Yield();
            OnAccept(client);
        }

        protected abstract void OnAccept(TcpClient client);
    }
}
