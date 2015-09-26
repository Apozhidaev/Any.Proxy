using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Ap.Logs;
using Ap.Proxy.Loggers;

namespace Ap.Proxy
{
    public class TcpBridge : IBridge
    {
        private readonly TcpClient _server;
        private readonly TcpClient _client;
        private NetworkStream _serverNetwork;
        private readonly NetworkStream _clientNetwork;


        public TcpBridge(TcpClient client)
        {
            _client = client;
            _clientNetwork = _client.GetStream();
            _server = new TcpClient();
        }

        public bool Connected => _client.Connected && _server.Connected;

        public void Dispose()
        {
            _serverNetwork?.Close();
            _server?.Close();
            _clientNetwork.Close();
            _client.Close();
        }

        public Task HandshakeAsync(string connectionId, string host, int port)
        {
            var tcsHandshake = new TaskCompletionSource<int>();
            _server.ConnectAsync(host, port).ContinueWith(__ =>
            {
                if (__.Exception != null)
                {
                    Log.Out.Error(__.Exception, connectionId, "HandshakeAsync");
                    tcsHandshake.SetException(__.Exception);
                }
                else
                {
                    _serverNetwork = _server.GetStream();
                    tcsHandshake.SetResult(0);
                }
            });
            return tcsHandshake.Task;
        }

        public async Task<byte[]> ReadToAsync(Func<string, bool> end)
        {
            var res = new List<byte>();
            var query = String.Empty;
            var buffer = new byte[1024*1024];
            do
            {
                var count = await _clientNetwork.ReadAsync(buffer, 0, buffer.Length);
                res.AddRange(buffer.Take(count));
                query += Encoding.ASCII.GetString(buffer, 0, count);
            } while (Connected && !end(query));
            return res.ToArray();
        }

        public async Task RelayFromAsync(Func<string, bool> end)
        {
            var query = String.Empty;
            var buffer = new byte[1024*1024];
            do
            {
                var count = await _serverNetwork.ReadAsync(buffer, 0, buffer.Length);
                await _clientNetwork.WriteAsync(buffer, 0, count);
                query += Encoding.ASCII.GetString(buffer, 0, count);
            } while (Connected && !end(query));
        }

        public async Task RelayFromAsync()
        {
            var buffer = new byte[1024 * 1024];
            while (Connected)
            {
                var count = await _serverNetwork.ReadAsync(buffer, 0, buffer.Length);
                await _clientNetwork.WriteAsync(buffer, 0, count);
            }
        }

        public Task RelayAsync()
        {
            return Task.WhenAll(RelayFromAsync(), RelayToAsync());
        }

        public async Task RelayToAsync()
        {
            var buffer = new byte[1024 * 1024];
            while (Connected)
            {
                var count = await _clientNetwork.ReadAsync(buffer, 0, buffer.Length);
                await _serverNetwork.WriteAsync(buffer, 0, count);
            }
        }

        public Task WriteFromAsync(byte[] bytes)
        {
            return _serverNetwork.WriteAsync(bytes, 0, bytes.Length);
        }

        public Task WriteToAsync(byte[] bytes)
        {
            return _clientNetwork.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}