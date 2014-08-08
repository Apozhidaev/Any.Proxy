using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Any.Proxy.Service.Https
{
    public class HttpsConnection : IDisposable
    {
        private readonly Socket _remoteSocket;
        private readonly IPEndPoint _remotePoint;
        private readonly byte[] _remoteBuffer = new byte[10240];
        private readonly TaskCompletionSource<string> _tcsHandshake = new TaskCompletionSource<string>();
        private TaskCompletionSource<int> _tcsRelayTo;
        private TaskCompletionSource<int> _tcsRelayFrom;
        public HttpsConnection(string host, int port)
        {
            var address = Dns.GetHostAddresses(host)[0];
            _remotePoint = new IPEndPoint(address, port);
            _remoteSocket = new Socket(_remotePoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _remoteSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
            Id = Guid.NewGuid().ToString();
        }
        public string Id { get; private set; }

        public byte[] RemoteBuffer
        {
            get
            {
                return _remoteBuffer;
            }
        }

        #region Handshake

        public Task<string> HandshakeAsync()
        {
            _remoteSocket.BeginConnect(_remotePoint, OnConnected, _remoteSocket);
            return _tcsHandshake.Task;
        }

        private void OnConnected(IAsyncResult ar)
        {
            try
            {
                _remoteSocket.EndConnect(ar);              
                _tcsHandshake.SetResult(Id);
            }
            catch (Exception e)
            {
                _tcsHandshake.SetException(e);
            }
        }

        #endregion

        #region RelayTo

        public Task<int> RelayToAsync(byte[] buffer)
        {
            _tcsRelayTo = new TaskCompletionSource<int>();
            _remoteSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, OnRemoteSent, _remoteSocket);
            return _tcsRelayTo.Task;
        }

        private void OnRemoteSent(IAsyncResult ar)
        {
            try
            {
                int ret = _remoteSocket.EndSend(ar);
                _tcsRelayTo.SetResult(ret);
            }
            catch (Exception e)
            {
                _tcsRelayTo.SetException(e);
            }
            
        }

        #endregion

        #region RelayFrom

        public Task<int> RelayFromAsync()
        {
            _tcsRelayFrom = new TaskCompletionSource<int>();
            try
            {
                _remoteSocket.BeginReceive(_remoteBuffer, 0, _remoteBuffer.Length, SocketFlags.None, OnRemoteReceive, _remoteSocket);
            }
            catch (Exception e)
            {
                _tcsRelayFrom.SetException(e);
            }
            
            return _tcsRelayFrom.Task;
        }

        private void OnRemoteReceive(IAsyncResult ar)
        {
            try
            {
                int ret = _remoteSocket.EndReceive(ar);
                _tcsRelayFrom.SetResult(ret);
            }
            catch (Exception e)
            {
                _tcsRelayFrom.SetException(e);
            }
        }

        #endregion

        public void Dispose()
        {
            HttpsConnectionManager.Instance.Remove(this);
            _remoteSocket.Dispose();
        }
    }
}