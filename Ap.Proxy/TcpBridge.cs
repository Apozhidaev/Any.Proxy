using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Ap.Logs;
using Ap.Proxy.Loggers;

namespace Ap.Proxy
{
    public class TcpBridge : IBridge
    {
        private readonly byte[] _buffer;
        private readonly byte[] _remoteBuffer;
        private readonly IPEndPoint _remotePoint;
        private readonly Socket _remoteSocket;
        private readonly string _connectionId;
        private readonly Socket _socket;
        private readonly TaskCompletionSource<int> _tcsRelayFrom = new TaskCompletionSource<int>();
        private readonly TaskCompletionSource<int> _tcsRelayTo = new TaskCompletionSource<int>();

        private bool _isDisposed = false;

        public TcpBridge(string connectionId, Socket socket, IPEndPoint remotePoint, bool isKeepAlive = false)
        {
            _connectionId = connectionId;
            _socket = socket;
            _remotePoint = remotePoint;
            _remoteSocket = new Socket(remotePoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (isKeepAlive)
            {
                _remoteSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
            }
            _buffer = new byte[socket.ReceiveBufferSize];
            _remoteBuffer = new byte[_remoteSocket.ReceiveBufferSize];
        }

        public TcpBridge(string connectionId, Socket socket, string host, int port, bool isKeepAlive = false)
            : this(connectionId, socket, new IPEndPoint(Dns.GetHostAddresses(host)[0], port), isKeepAlive)
        {
        }

        public void Dispose()
        {
            _tcsRelayFrom.TrySetResult(0);
            _tcsRelayTo.TrySetResult(0);
            lock (_remoteSocket)
            {
                if (!_isDisposed)
                {
                    try
                    {
                        _remoteSocket.Shutdown(SocketShutdown.Both);
                        _remoteSocket.Close();
                    }
                    catch (Exception e)
                    {
                        Log.Out.Error(e, _connectionId, "Dispose");
                    }
                    _remoteSocket.Dispose();
                    _isDisposed = true;
                }
            }
            
            

        }

        #region Handshake

        public Task HandshakeAsync()
        {
            var tcsHandshake = new TaskCompletionSource<int>();
            _remoteSocket.BeginConnect(_remotePoint, ar =>
            {
                try
                {
                    _remoteSocket.EndConnect(ar);
                    tcsHandshake.SetResult(0);
                }
                catch (Exception e)
                {
                    Log.Out.Error(e, _connectionId, "HandshakeAsync");
                    tcsHandshake.SetException(e);
                }
            }, null);
            return tcsHandshake.Task;
        }

        #endregion

        #region Relay

        public Task RelayAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    Task.WaitAll(RelayToAsync(), RelayFromAsync());
                }
                catch (Exception e)
                {
                    Log.Out.Error(e, _connectionId, "RelayAsync");
                }
                
            });
        }

        #endregion

        #region RelayTo

        public Task RelayToAsync()
        {
            _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnClientReceive, null);
            return _tcsRelayTo.Task;
        }

        private void OnClientReceive(IAsyncResult ar)
        {
            try
            {
                int ret = _socket.EndReceive(ar);
                if (ret <= 0)
                {
                    _tcsRelayTo.TrySetResult(0);
                    return;
                }
                _remoteSocket.BeginSend(_buffer, 0, ret, SocketFlags.None, OnRemoteSent, null);
            }
            catch (Exception e)
            {
                Log.Out.Error(e, _connectionId, "OnClientReceive");
                _tcsRelayTo.TrySetException(e);
            }
        }

        private void OnRemoteSent(IAsyncResult ar)
        {
            try
            {
                int ret = _remoteSocket.EndSend(ar);
                if (ret > 0)
                {
                    _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnClientReceive, null);
                    return;
                }
                _tcsRelayTo.TrySetResult(0);
            }
            catch (Exception e)
            {
                Log.Out.Error(e, _connectionId, "OnRemoteSent");
                _tcsRelayTo.TrySetException(e);
            }
        }

        #endregion

        #region RelayFrom

        public Task RelayFromAsync()
        {
            _remoteSocket.BeginReceive(_remoteBuffer, 0, _remoteBuffer.Length, SocketFlags.None, OnRemoteReceive, null);
            return _tcsRelayFrom.Task;
        }

        private void OnRemoteReceive(IAsyncResult ar)
        {
            try
            {
                int ret = _remoteSocket.EndReceive(ar);
                if (ret <= 0)
                {
                    _tcsRelayFrom.TrySetResult(0);
                    return;
                }
                _socket.BeginSend(_remoteBuffer, 0, ret, SocketFlags.None, OnClientSent, null);
            }
            catch (Exception e)
            {
                Log.Out.Error(e, _connectionId, "OnRemoteReceive");
                _tcsRelayFrom.TrySetException(e);
            }
        }

        private void OnClientSent(IAsyncResult ar)
        {
            try
            {
                int ret = _socket.EndSend(ar);
                if (ret > 0)
                {
                    _remoteSocket.BeginReceive(_remoteBuffer, 0, _remoteBuffer.Length, SocketFlags.None, OnRemoteReceive, null);
                    return;
                }
                _tcsRelayFrom.TrySetResult(0);
            }
            catch (Exception e)
            {
                Log.Out.Error(e, _connectionId, "OnClientSent");
                _tcsRelayFrom.TrySetException(e);
            }
        }

        #endregion

        public Task WriteAsync(byte[] bytes)
        {
            return _remoteSocket.WriteAsync(bytes);
        }
    }
}