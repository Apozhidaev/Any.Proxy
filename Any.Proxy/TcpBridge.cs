using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Any.Proxy
{
    public class TcpBridge : IDisposable
    {
        private readonly Socket _socket;
        private readonly Socket _remoteSocket;
        private readonly IPEndPoint _remotePoint;
        private readonly byte[] _buffer = new byte[40960];
        private readonly byte[] _remoteBuffer = new byte[10240];
        private readonly TaskCompletionSource<int> _tcsHandshake = new TaskCompletionSource<int>();
        private readonly TaskCompletionSource<int> _tcsRelayTo = new TaskCompletionSource<int>();
        private readonly TaskCompletionSource<int> _tcsRelayFrom = new TaskCompletionSource<int>();

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

        public void Dispose()
        {
            _remoteSocket.Dispose();
        }

        #region Handshake

        public Task HandshakeAsync()
        {
            _remoteSocket.BeginConnect(_remotePoint, OnConnected, _remoteSocket);
            return _tcsHandshake.Task;
        }

        private void OnConnected(IAsyncResult ar)
        {
            try
            {
                _remoteSocket.EndConnect(ar);
                _tcsHandshake.SetResult(0);
            }
            catch (Exception e)
            {
                _tcsHandshake.SetException(e);
            }
        }

        #endregion

        #region Relay

        public Task RelayAsync()
        {
            return Task.Factory.StartNew(Relay);
        }

        public void Relay()
        {
            Task.WaitAll(RelayToAsync(), RelayFromAsync());
        }

        #endregion

        #region RelayTo

        public Task RelayToAsync()
        {
            _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnClientReceive, _socket);
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
                _remoteSocket.BeginSend(_buffer, 0, ret, SocketFlags.None, OnRemoteSent, _remoteSocket);
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
                    _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnClientReceive, _socket);
                    return;
                }
            }
            catch (Exception e)
            {
                _tcsRelayTo.SetException(e);
                return;
            }
            _tcsRelayTo.SetResult(0);
        }

        #endregion

        #region RelayFrom

        public Task RelayFromAsync()
        {
            _remoteSocket.BeginReceive(_remoteBuffer, 0, _remoteBuffer.Length, SocketFlags.None, OnRemoteReceive, _remoteSocket);
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
                _socket.BeginSend(_remoteBuffer, 0, ret, SocketFlags.None, OnClientSent, _socket);
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
                    _remoteSocket.BeginReceive(_remoteBuffer, 0, _remoteBuffer.Length, SocketFlags.None, OnRemoteReceive, _remoteSocket);
                    return;
                }
            }
            catch (Exception e)
            {
                _tcsRelayFrom.SetException(e);
                return;
            }
            _tcsRelayFrom.SetResult(0);
        }

        #endregion
    }
}
