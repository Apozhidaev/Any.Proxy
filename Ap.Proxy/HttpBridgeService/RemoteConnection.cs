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
        private const int Threshold = 100;

        private static readonly Dictionary<string, RemoteConnection> Connections =
            new Dictionary<string, RemoteConnection>();

        public static RemoteConnection Open(string connectionId, string host, int port)
        {
            var connection = new RemoteConnection(connectionId, host, port);
            lock (Connections)
            {
                if (Connections.Count > Threshold)
                {
                    DateTime expDate = DateTime.Now.AddMinutes(-10);
                    IEnumerable<string> removeKeys =
                        Connections.Where(c => c.Value._lastActive < expDate).Select(c => c.Key);
                    foreach (string removeKey in removeKeys)
                    {
                        Connections[removeKey].Dispose();
                        Connections.Remove(removeKey);
                    }
                }
                Connections.Add(connection.Id, connection);
            }
            return connection;
        }

        public static RemoteConnection Find(string id)
        {
            lock (Connections)
            {
                if (Connections.ContainsKey(id))
                {
                    return Connections[id];
                }
                return null;
            }
        }


        private readonly TcpClient _server;
        private NetworkStream _serverNetwork;
        private DateTime _lastActive = DateTime.Now;
        private readonly string _id;
        private readonly string _host;
        private readonly int _port;

        private RemoteConnection(string connectionId, string host, int port)
        {
            _server = new TcpClient();
            _id = connectionId;
            _host = host;
            _port = port;
        }

        public string Id => _id;

        public Task<string> HandshakeAsync()
        {
            _lastActive = DateTime.Now;
            var tcsHandshake = new TaskCompletionSource<string>();
            _server.ConnectAsync(_host, _port).ContinueWith(__ =>
            {
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
            _lastActive = DateTime.Now;
            return _serverNetwork.WriteAsync(bytes, 0, bytes.Length);
        }

        public async Task<byte[]> ReadAsync()
        {
            _lastActive = DateTime.Now;
            var buffer = new byte[1024 * 1024];
            var count = await _serverNetwork.ReadAsync(buffer, 0, buffer.Length);
            var res = new byte[count];
            Array.Copy(buffer, res, res.Length);
            return res;
        }

        public void Dispose()
        {
            lock (Connections)
            {
                Connections.Remove(Id);
            }
            _serverNetwork?.Close();
            _server?.Close();
        }
    }
}