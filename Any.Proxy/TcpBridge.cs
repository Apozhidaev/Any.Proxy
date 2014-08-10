using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Any.Proxy
{
    public class TcpBridge : IDisposable
    {
        private readonly byte[] _buffer = new byte[40960];
        private readonly byte[] _remoteBuffer = new byte[10240];
        private readonly IPEndPoint _remotePoint;
        private readonly Socket _remoteSocket;
        private readonly Socket _socket;
        private readonly TaskCompletionSource<int> _tcsRelayFrom = new TaskCompletionSource<int>();
        private readonly TaskCompletionSource<int> _tcsRelayTo = new TaskCompletionSource<int>();

        public TcpBridge(Socket socket, IPEndPoint remotePoint, bool isKeepAlive = false)
        {
            _socket = socket;
            _remotePoint = remotePoint;
            _remoteSocket = new Socket(remotePoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (isKeepAlive)
            {
                _remoteSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
            }
        }

        public TcpBridge(Socket socket, string host, int port, bool isKeepAlive = false)
            : this(socket, new IPEndPoint(Dns.GetHostAddresses(host)[0], port), isKeepAlive)
        {
        }

        public Socket RemoteSocket
        {
            get
            {
                return _remoteSocket;
            }
        }

        public void Dispose()
        {
            _remoteSocket.Dispose();
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
                    _tcsRelayTo.SetResult(0);
                    return;
                }
                _remoteSocket.BeginSend(_buffer, 0, ret, SocketFlags.None, OnRemoteSent, null);
            }
            catch (Exception e)
            {
                _tcsRelayTo.SetException(e);
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
                _tcsRelayTo.SetResult(0);
            }
            catch (Exception e)
            {
                _tcsRelayTo.SetException(e);
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
                    _tcsRelayFrom.SetResult(0);
                    return;
                }
                _socket.BeginSend(_remoteBuffer, 0, ret, SocketFlags.None, OnClientSent, null);
            }
            catch (Exception e)
            {
                _tcsRelayFrom.SetException(e);
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
                _tcsRelayFrom.SetResult(0);
            }
            catch (Exception e)
            {
                _tcsRelayFrom.SetException(e);
            }
        }

        #endregion
    }
}