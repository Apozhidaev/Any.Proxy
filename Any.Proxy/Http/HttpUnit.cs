using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Any.Proxy.Http
{
    public class HttpUnit
    {
        private readonly TcpListener _listener;

        public HttpUnit(IPAddress address, int port)
        {
            _listener = new TcpListener(address, port);
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
                        // ищем хост и порт
                        var myReg = new Regex(@"Host: (((?<host>.+?):(?<port>\d+?))|(?<host>.+?))\s+",
                            RegexOptions.Multiline | RegexOptions.IgnoreCase);
                        Match m = myReg.Match(Encoding.ASCII.GetString(httpRequest));
                        string host = m.Groups["host"].Value;
                        int port = 0;
                        // если порта нет, то используем 80 по умолчанию
                        if (!int.TryParse(m.Groups["port"].Value, out port))
                        {
                            port = 80;
                        }

                        // получаем апишник по хосту
                        IPHostEntry myIPHostEntry = Dns.GetHostEntry(host);

                        // создаем точку доступа
                        var myIPEndPoint = new IPEndPoint(myIPHostEntry.AddressList[0], port);

                        // создаем сокет и передаем ему запрос
                        using (
                            var myRerouting = new Socket(myIPHostEntry.AddressList[0].AddressFamily, SocketType.Stream,
                                ProtocolType.Tcp))
                        {
                            myRerouting.Connect(myIPEndPoint);
                            if (myRerouting.Send(httpRequest, httpRequest.Length, SocketFlags.None) !=
                                httpRequest.Length)
                            {
                                Console.WriteLine("При отправке данных удаленному серверу произошла ошибка...");
                            }
                            else
                            {
                                // получаем ответ
                                byte[] httpResponse = myRerouting.ReadToEnd(1000000);
                                // передаем ответ обратно клиенту
                                if (httpResponse != null && httpResponse.Length > 0)
                                {
                                    myClient.Send(httpResponse, httpResponse.Length, SocketFlags.None);
                                }
                            }
                            myRerouting.Shutdown(SocketShutdown.Both);
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