using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Any.Proxy.Http
{
    public class HttpClientModule : IProxyModule
    {
        private readonly Uri _uri;
        private readonly TcpListener _listener;
        private readonly HttpClient _httpClient;

        public HttpClientModule(IPAddress address, int port)
        {
            _uri = new Uri("http://lifehttp.com/h/r");
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
                    await Accept(await _listener.AcceptSocketAsync());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }
        }

        private async Task Accept(Socket myClient)
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
                        byte[] httpRequest = ReadToEnd(myClient, 100);

                        var response = await _httpClient.PostAsync(_uri, new ByteArrayContent(httpRequest));

                        if (response.IsSuccessStatusCode)
                        {
                            var strResponse = await response.Content.ReadAsStringAsync();
                            byte[] httpResponse = Convert.FromBase64String(strResponse.Trim('\"'));
                            myClient.Send(httpResponse, httpResponse.Length, SocketFlags.None);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private static byte[] ReadToEnd(Socket mySocket, int wait)
        {
            var b = new byte[mySocket.ReceiveBufferSize];
            using (var m = new MemoryStream())
            {
                int len = 0;
                while (mySocket.Poll(wait, SelectMode.SelectRead) && (len = mySocket.Receive(b, b.Length, SocketFlags.None)) > 0)
                {
                    m.Write(b, 0, len);
                }
                return m.ToArray();
            }

        }

        public void Dispose()
        {
            _listener.Stop();
        }
    }
}