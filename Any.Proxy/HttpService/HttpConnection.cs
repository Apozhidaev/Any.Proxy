using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Any.Proxy.HttpService
{
    public class HttpConnection : IDisposable
    {
        private const int Threshold = 100;

        private static readonly Dictionary<string, HttpConnection> Connections =
            new Dictionary<string, HttpConnection>();

        private readonly byte[] _buffer = new byte[10240];
        private readonly IPEndPoint _endPoint;
        private readonly Socket _socket;
        private DateTime _lastActive = DateTime.Now;

        private HttpConnection(string host, int port)
        {
            IPAddress address = Dns.GetHostAddresses(host)[0];
            _endPoint = new IPEndPoint(address, port);
            _socket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
            Id = Guid.NewGuid().ToString();
        }

        public string Id { get; private set; }

        public byte[] Buffer
        {
            get { return _buffer; }
        }

        #region Handshake

        public Task<string> HandshakeAsync()
        {
            _lastActive = DateTime.Now;
            var tcsHandshake = new TaskCompletionSource<string>();
            _socket.BeginConnect(_endPoint, ar =>
            {
                _lastActive = DateTime.Now;
                try
                {
                    _socket.EndConnect(ar);
                    tcsHandshake.SetResult(Id);
                }
                catch (Exception e)
                {
                    tcsHandshake.SetException(e);
                }
            }, null);
            return tcsHandshake.Task;
        }

        #endregion

        #region RelayTo

        public Task<int> RelayToAsync(byte[] buffer)
        {
            _lastActive = DateTime.Now;
            var tcsRelayTo = new TaskCompletionSource<int>();
            _socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, ar =>
            {
                _lastActive = DateTime.Now;
                try
                {
                    int ret = _socket.EndSend(ar);
                    tcsRelayTo.SetResult(ret);
                }
                catch (Exception e)
                {
                    tcsRelayTo.SetException(e);
                }
            }, null);
            return tcsRelayTo.Task;
        }

        #endregion

        #region RelayFrom

        public Task<int> RelayFromAsync()
        {
            _lastActive = DateTime.Now;
            var tcsRelayFrom = new TaskCompletionSource<int>();
            try
            {
                _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ar =>
                {
                    _lastActive = DateTime.Now;
                    try
                    {
                        int ret = _socket.EndReceive(ar);
                        tcsRelayFrom.SetResult(ret);
                    }
                    catch (Exception e)
                    {
                        tcsRelayFrom.SetException(e);
                    }
                }, null);
            }
            catch (Exception e)
            {
                tcsRelayFrom.SetException(e);
            }
            return tcsRelayFrom.Task;
        }

        #endregion

        public void Dispose()
        {
            lock (Connections)
            {
                Connections.Remove(Id);
            }
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Dispose();
        }

        public static HttpConnection Open(string host, int port)
        {
            var connection = new HttpConnection(host, port);
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

        public static HttpConnection Find(string id)
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
    }
}