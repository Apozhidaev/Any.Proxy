using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Any.Proxy
{
    public class HttpBridge : IBridge
    {
        private readonly byte[] _buffer = new byte[40960];

        private readonly Uri _connectUri;
        private readonly string _host;
        private HttpClient _httpClient;
        private readonly int _port;
        private readonly Uri _receiveUri;
        private readonly Uri _sendUri;
        private readonly Socket _socket;
        private readonly TaskCompletionSource<int> _tcsRelayFrom = new TaskCompletionSource<int>();
        private readonly TaskCompletionSource<int> _tcsRelayTo = new TaskCompletionSource<int>();

        private string _connectionId;

        private DateTime _lastActivity = DateTime.Now.AddYears(1);
        private readonly Timer _timer;

        public HttpBridge(Socket socket, string host, int port, string serviceUrl, bool isKeepAlive = false)
        {
            _socket = socket;
            _host = host;
            _port = port;
            _connectUri = new Uri(String.Format("{0}?a=hc", serviceUrl));
            _receiveUri = new Uri(String.Format("{0}?a=hr", serviceUrl));
            _sendUri = new Uri(String.Format("{0}?a=hs", serviceUrl));
            _httpClient = new HttpClient(new HttpClientHandler {UseProxy = false});

            _timer = new Timer(_ => DoWork());
            _timer.Change(1000, 100);
        }

        public Task HandshakeAsync()
        {
            var tcsHandshake = new TaskCompletionSource<int>();
            _httpClient.PostAsync(_connectUri, new StringContent(String.Format("{0}:{1}", _host, _port)))
                .ContinueWith(_ =>
                {
                    if (_.Exception != null)
                    {
                        tcsHandshake.SetException(_.Exception);
                    }
                    else
                    {
                        if (_.Result.StatusCode == HttpStatusCode.OK)
                        {
                            _connectionId = _.Result.Content.ReadAsStringAsync().Result;
                            tcsHandshake.SetResult(0);
                        }
                        else
                        {
                            tcsHandshake.SetException(new WebException("Bad response"));
                        }
                    }
                });
            return tcsHandshake.Task;
        }

        public void Dispose()
        {
            _timer.Dispose();
            if (_httpClient != null)
            {
                _httpClient.Dispose();
                _httpClient = null;
            }
        }

        private void DoWork()
        {
            if (DateTime.Now > _lastActivity && DateTime.Now - _lastActivity > TimeSpan.FromSeconds(3))
            {
                Dispose();
            }
        }

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
                    _tcsRelayTo.SetResult(0);
                    return;
                }
                _httpClient.PostAsync(_sendUri, new StringContent(FormMessage(ret))).ContinueWith(_ =>
                {
                    try
                    {
                        _lastActivity = DateTime.Now;
                        HttpResponseMessage response = _.Result;
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, OnClientReceive, null);
                        }
                        else
                        {
                            _tcsRelayTo.SetResult(0);
                        }
                    }
                    catch (Exception ex)
                    {
                        _tcsRelayTo.SetException(ex);
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
            _lastActivity = DateTime.Now;
            _httpClient.PostAsync(_receiveUri, new StringContent(_connectionId)).ContinueWith(_ =>
            {
                try
                {
                    _lastActivity = DateTime.Now;
                    HttpResponseMessage response = _.Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string strResponse = response.Content.ReadAsStringAsync().Result;
                        byte[] remoteBuffer = Convert.FromBase64String(strResponse);
                        _socket.BeginSend(remoteBuffer, 0, remoteBuffer.Length, SocketFlags.None, OnClientSent, null);
                    }
                    else
                    {
                        _tcsRelayFrom.SetResult(0);
                    }
                }
                catch (Exception e)
                {
                    _tcsRelayFrom.SetException(e);
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

        public Task WriteAsync(byte[] bytes)
        {
            var tcsWrite = new TaskCompletionSource<int>();
            _httpClient.PostAsync(_sendUri, new StringContent(FormMessage(bytes))).ContinueWith(_ =>
            {
                try
                {
                    HttpResponseMessage response = _.Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        tcsWrite.SetResult(0);
                    }
                    else
                    {
                        tcsWrite.SetException(new WebException("Bad response"));
                    }
                }
                catch (Exception ex)
                {
                    tcsWrite.SetException(ex);
                }
            });
            return tcsWrite.Task;
        }

        private string FormMessage(int length)
        {
            return String.Format("{0}:{1}", _connectionId, Convert.ToBase64String(_buffer, 0, length));
        }

        private string FormMessage(byte[] bytes)
        {
            return String.Format("{0}:{1}", _connectionId, Convert.ToBase64String(bytes));
        }
    }
}