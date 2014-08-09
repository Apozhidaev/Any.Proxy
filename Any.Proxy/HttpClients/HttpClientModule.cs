using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Any.Proxy.HttpClients
{
    public class HttpClientModule : IProxyModule
    {
        private readonly Uri _uri;
        private readonly TcpListener _listener;
        private readonly HttpClient _httpClient;

        public HttpClientModule(IPAddress address, int port)
        {
            _uri = new Uri("http://lifehttp.com?a=hr");
            _listener = new TcpListener(address, port);
            _httpClient = new HttpClient(new HttpClientHandler { UseProxy = false });
        }

        public async void Start()
        {
            _listener.Start();
            while (true)
            {
                try
                {
                    Accept(await _listener.AcceptSocketAsync());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }
        }

        private async void Accept(Socket myClient)
        {
            await Task.Yield();
            using (myClient)
            {
                try
                {
                    // соединяемся
                    if (myClient.Connected)
                    {
                        // получаем тело запроса
                        byte[] httpRequest = myClient.ReadToEnd(100);

                        var response = await _httpClient.PostAsync(_uri, new ByteArrayContent(httpRequest));

                        if (response.IsSuccessStatusCode)
                        {
                            var strResponse = await response.Content.ReadAsStringAsync();
                            byte[] httpResponse = Convert.FromBase64String(strResponse.Trim('\"'));
                            myClient.Send(httpResponse, httpResponse.Length, SocketFlags.None);
                        }
                        myClient.Shutdown(SocketShutdown.Both);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public void Dispose()
        {
            _listener.Stop();
        }
    }
}