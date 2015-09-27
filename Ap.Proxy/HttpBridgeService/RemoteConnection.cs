using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Ap.Logs;
using Ap.Proxy.Loggers;

namespace Ap.Proxy.HttpBridgeService
{
    public class RemoteConnection : IDisposable
    {
        private readonly TcpClient _server;
        private NetworkStream _serverNetwork;
        private readonly string _host;
        private readonly int _port;
        private bool _opened;

        public RemoteConnection(string connectionId, string host, int port)
        {
            _server = new TcpClient();
            Id = connectionId;
            _host = host;
            _port = port;
        }

        public string Id { get; }

        public DateTime LastActivity { get; private set; } = DateTime.UtcNow;

        public bool Expired
        {
            get
            {
                if (_opened)
                {
                    return LastActivity < DateTime.UtcNow.AddMilliseconds(-5000) || !_server.Connected;
                }
                return LastActivity < DateTime.UtcNow.AddSeconds(-30);
            }
        }

        public Task<string> HandshakeAsync()
        {
            _activity();
            var tcsHandshake = new TaskCompletionSource<string>();
            _server.ConnectAsync(_host, _port).ContinueWith(__ =>
            {
                _opened = true;
                if (__.Exception != null)
                {
                    Log.Out.Error(__.Exception, Id, "HandshakeAsync");
                    tcsHandshake.SetException(__.Exception);
                }
                else
                {
                    _serverNetwork = _server.GetStream();
                    tcsHandshake.SetResult(Id);
                }
            });
            return tcsHandshake.Task;
        }

        public Task WriteAsync(byte[] bytes)
        {
            _activity();
            return _serverNetwork.WriteAsync(bytes, 0, bytes.Length);
        }

        public async Task<byte[]> ReadAsync()
        {
            _activity();
            var buffer = new byte[1024 * 1024];
            var count = await _serverNetwork.ReadAsync(buffer, 0, buffer.Length);
            var res = new byte[count];
            Array.Copy(buffer, res, res.Length);
            return res;
        }

        public void Dispose()
        {
            _serverNetwork?.Close();
            _server?.Close();
        }

        private void _activity()
        {
            LastActivity = DateTime.UtcNow;
        }
    }
}