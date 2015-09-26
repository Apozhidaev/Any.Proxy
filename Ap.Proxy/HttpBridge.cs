using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Ap.Proxy.Configuration;

namespace Ap.Proxy
{
    public class HttpBridge : IBridge
    {
        private string _connectionId;
        private readonly TcpClient _client;
        private readonly NetworkStream _clientNetwork;
        private readonly Uri _connectUri;
        private readonly Uri _receiveUri;
        private readonly Uri _sendUri;
        private readonly HttpClient _httpClient;

        public HttpBridge(HttpBridgeConfig config, TcpClient client)
        {
            _client = client;
            _clientNetwork = _client.GetStream();
            _connectUri = new Uri($"{config.Url}?a=hc");
            _receiveUri = new Uri($"{config.Url}?a=hr");
            _sendUri = new Uri($"{config.Url}?a=hs");

            var handler = new HttpClientHandler { UseProxy = config.UseProxy };
            if(config.UseProxy)
            {
                WebProxy proxy;
                if(config.UseDefaultCredentials)
                {
                    proxy = new WebProxy(config.Proxy);
                }
                else
                {
                    var auth = new NetworkCredential(config.UserName, config.Password);
                    proxy = new WebProxy(config.Proxy, true, null, auth);
                }
                proxy.UseDefaultCredentials = config.UseDefaultCredentials;
                handler.Proxy = proxy;
            }

            _httpClient = new HttpClient(handler);
        }

        public Task HandshakeAsync(string connectionId, string host, int port)
        {
            _connectionId = connectionId;
            var tcsHandshake = new TaskCompletionSource<int>();
            _httpClient.PostAsync(_connectUri, new StringContent($"{connectionId}:{host}:{port}"))
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
            _httpClient?.Dispose();
            _clientNetwork.Close();
            _client.Close();
        }

        public bool Connected => _client.Connected;

        public async Task<byte[]> ReadToAsync(Func<string, bool> end)
        {
            var res = new List<byte>();
            var query = String.Empty;
            var buffer = new byte[1024 * 1024];
            do
            {
                var count = await _clientNetwork.ReadAsync(buffer, 0, buffer.Length);
                res.AddRange(buffer.Take(count));
                query += Encoding.ASCII.GetString(buffer, 0, count);
            } while (Connected && !end(query));
            return res.ToArray();
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
                var res = new byte[count];
                Array.Copy(buffer, res, res.Length);
                await RemoteSend(res);
            }
        }

        public async Task RelayFromAsync()
        {
            while (Connected)
            {
                var buffer = await RemoteReceive();
                await _clientNetwork.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        public async Task RelayFromAsync(Func<string, bool> end)
        {
            var query = String.Empty;
            do
            {
                var buffer = await RemoteReceive();
                await _clientNetwork.WriteAsync(buffer, 0, buffer.Length);
                query += Encoding.ASCII.GetString(buffer, 0, buffer.Length);
            } while (Connected && !end(query));
        }

        public Task WriteFromAsync(byte[] bytes)
        {
            var tcsWrite = new TaskCompletionSource<int>();
            _httpClient.PostAsync(_sendUri, new StringContent(FormMessage(bytes))).ContinueWith(_ =>
            {
                try
                {
                    HttpResponseMessage response = _.Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        tcsWrite.TrySetResult(0);
                    }
                    else
                    {
                        tcsWrite.TrySetException(new WebException("Bad response"));
                    }
                }
                catch (Exception ex)
                {
                    tcsWrite.TrySetException(ex);
                }
            });
            return tcsWrite.Task;
        }

        public Task WriteToAsync(byte[] bytes)
        {
            return _clientNetwork.WriteAsync(bytes, 0, bytes.Length);
        }

        private string FormMessage(byte[] bytes)
        {
            return $"{_connectionId}:{Convert.ToBase64String(bytes)}";
        }

        private async Task<byte[]> RemoteReceive()
        {
            HttpResponseMessage response = await _httpClient.PostAsync(_receiveUri, new StringContent(_connectionId));
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string strResponse = await response.Content.ReadAsStringAsync();
                return Convert.FromBase64String(strResponse);
            }
            return new byte[0];
        }

        private async Task<bool> RemoteSend(byte[] bytes)
        {
            HttpResponseMessage response = await _httpClient.PostAsync(_sendUri, new StringContent(FormMessage(bytes)));
            return response.StatusCode == HttpStatusCode.OK;
        }
    }
}