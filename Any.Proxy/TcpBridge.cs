using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Any.Proxy
{
    public class TcpBridge : IBridge
    {
        private readonly byte[] _buffer;
        private readonly byte[] _remoteBuffer;
        private readonly IPEndPoint _remotePoint;
        private readonly Socket _remoteSocket;
        private readonly Socket _socket;
        private readonly TaskCompletionSource<int> _tcsRelayFrom = new TaskCompletionSource<int>();
        private readonly TaskCompletionSource<int> _tcsRelayTo = new TaskCompletionSource<int>();

        private DateTime _lastActivity = DateTime.Now.AddYears(1);
        private readonly Timer _timer;
        private bool _isDisposed = false;

        public TcpBridge(Socket socket, IPEndPoint remotePoint, bool isKeepAlive = false)
        {
            _socket = socket;
            _remotePoint = remotePoint;
            _remoteSocket = new Socket(remotePoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (isKeepAlive)
            {
                _remoteSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
            }
            _buffer = new byte[socket.ReceiveBufferSize];
            _remoteBuffer = new byte[_remoteSocket.ReceiveBufferSize];
            _timer = new Timer(_ => DoWork());
            _timer.Change(1000, 100);
        }

        public TcpBridge(Socket socket, string host, int port, bool isKeepAlive = false)
            : this(socket, new IPEndPoint(Dns.GetHostAddresses(host)[0], port), isKeepAlive)
        {
        }

        public void Dispose()
        {
            _tcsRelayFrom.TrySetResult(0);
            _tcsRelayTo.TrySetResult(0);
            _timer.Dispose();
            lock (_remoteSocket)
            {
                if (!_isDisposed)
                {
                    try
                    {
                        _remoteSocket.Shutdown(SocketShutdown.Both);
                        _remoteSocket.Close();
                    }
                    catch (Exception)
                    {
                    }
                    _remoteSocket.Dispose();
                    _isDisposed = true;
                }
            }
            
            

        }

        private void DoWork()
        {
            if (DateTime.Now > _lastActivity && DateTime.Now - _lastActivity > TimeSpan.FromSeconds(1))
            {
                Dispose();
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
                catch (Exception)
                {
                }
                
            });
        }

        #endregion

        #region RelayTo

        public Task RelayToAsync()
        {
            _lastActivity = DateTime.Now;
            _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnClientReceive, null);
            return _tcsRelayTo.Task;
        }

        private void OnClientReceive(IAsyncResult ar)
        {
            try
            {
                _lastActivity = DateTime.Now;
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
                _tcsRelayTo.TrySetException(e);
            }
        }

        private void OnRemoteSent(IAsyncResult ar)
        {
            try
            {
                _lastActivity = DateTime.Now;
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
                _tcsRelayTo.TrySetException(e);
            }
        }

        #endregion

        #region RelayFrom

        public Task RelayFromAsync()
        {
            _lastActivity = DateTime.Now;
            _remoteSocket.BeginReceive(_remoteBuffer, 0, _remoteBuffer.Length, SocketFlags.None, OnRemoteReceive, null);
            return _tcsRelayFrom.Task;
        }

        private void OnRemoteReceive(IAsyncResult ar)
        {
            try
            {
                _lastActivity = DateTime.Now;
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
                _tcsRelayFrom.TrySetException(e);
            }
        }

        private void OnClientSent(IAsyncResult ar)
        {
            try
            {
                _lastActivity = DateTime.Now;
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