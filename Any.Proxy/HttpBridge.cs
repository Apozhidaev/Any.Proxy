using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Any.Proxy
{
    public class HttpBridge : IBridge
    {
        private readonly Socket _socket;
        private readonly string _host;
        private readonly int _port;
        private readonly byte[] _buffer = new byte[40960];
        private readonly TaskCompletionSource<int> _tcsHandshake = new TaskCompletionSource<int>();
        private readonly TaskCompletionSource<int> _tcsRelayTo = new TaskCompletionSource<int>();
        private readonly TaskCompletionSource<int> _tcsRelayFrom = new TaskCompletionSource<int>();

        private readonly Uri _connectUri;
        private readonly Uri _receiveUri;
        private readonly Uri _sendUri;
        private readonly HttpClient _httpClient;

        private string _connectionId;
        public HttpBridge(Socket socket, string host, int port, bool isKeepAlive = false)
        {
            _socket = socket;
            _host = host;
            _port = port;
            _connectUri = new Uri("http://lifehttp.com/hs/c");
            _receiveUri = new Uri("http://lifehttp.com/hs/r");
            _sendUri = new Uri("http://lifehttp.com/hs/s");
            _httpClient = new HttpClient(new HttpClientHandler { UseProxy = false });
        }

        public Task HandshakeAsync()
        {
            _httpClient.PostAsync(_connectUri, new StringContent(String.Format("{0}:{1}", _host, _port))).ContinueWith(_ =>
            {
                _connectionId = _.Result.Content.ReadAsStringAsync().Result.Trim('\"');
                _tcsHandshake.SetResult(0);
            });
            return _tcsHandshake.Task;
        }

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
                _httpClient.PostAsync(_sendUri, new StringContent(FormMessage(ret))).ContinueWith(_ =>
                {
                    var response = _.Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        try
                        {
                            _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnClientReceive, _socket);
                        }
                        catch (Exception ex)
                        {
                            _tcsRelayTo.SetException(ex);
                        }
                         
                    }
                    else
                    {
                        _tcsRelayTo.SetResult(0);
                    }
                    
                });
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
            RemoteReceive();
            return _tcsRelayFrom.Task;
        }

        private void RemoteReceive()
        {
            _httpClient.PostAsync(_receiveUri, new StringContent(_connectionId)).ContinueWith(_ =>
            {
                var response = _.Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var strResponse = response.Content.ReadAsStringAsync().Result;
                    byte[] remoteBuffer = Convert.FromBase64String(strResponse.Trim('\"'));
                    _socket.BeginSend(remoteBuffer, 0, remoteBuffer.Length, SocketFlags.None, OnClientSent, _socket);
                }
                else
                {
                    _tcsRelayFrom.SetResult(0);
                }

            });
        }

        private void OnClientSent(IAsyncResult ar)
        {
            try
            {
                int ret = _socket.EndSend(ar);
                if (ret > 0)
                {
                    RemoteReceive();
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

        private string FormMessage(int length)
        {
            return String.Format("{0}:{1}", _connectionId, Convert.ToBase64String(_buffer, 0, length));
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}