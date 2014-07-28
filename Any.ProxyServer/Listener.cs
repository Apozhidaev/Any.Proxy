using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Any.ProxyServer
{
    public class Listener
    {
        private readonly TcpListener _listener;

        public Listener()
        {
            _listener = new TcpListener(IPAddress.Any, 50000);
        }

        public async void Start()
        {
            _listener.Start();
            while (true)
            {
                try
                {
                    await Accept(await _listener.AcceptTcpClientAsync());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

            }
        }

        public void Stop()
        {
            _listener.Stop();
        }

        private async Task Accept(TcpClient myClient)
        {
            await Task.Yield();
            using (myClient)
            using (var stream = myClient.GetStream())
            {
                var reader = new BinaryReader(stream);
                var lenght = reader.ReadInt32();

                byte[] httpRequest = reader.ReadBytes(lenght);

                // ищем хост и порт
                Regex myReg = new Regex(@"Host: (((?<host>.+?):(?<port>\d+?))|(?<host>.+?))\s+", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                Match m = myReg.Match(System.Text.Encoding.ASCII.GetString(httpRequest));
                string host = m.Groups["host"].Value;
                int port = 0;
                // если порта нет, то используем 80 по умолчанию
                if (!int.TryParse(m.Groups["port"].Value, out port)) { port = 80; }

                // получаем апишник по хосту
                IPHostEntry myIPHostEntry = Dns.GetHostEntry(host);

                // создаем точку доступа
                var myIPEndPoint = new IPEndPoint(myIPHostEntry.AddressList[0], port);

                // создаем сокет и передаем ему запрос
                using (var myRerouting = new Socket(myIPHostEntry.AddressList[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    myRerouting.Connect(myIPEndPoint);
                    if (myRerouting.Send(httpRequest, httpRequest.Length, SocketFlags.None) != httpRequest.Length)
                    {
                        Console.WriteLine("При отправке данных удаленному серверу произошла ошибка...");
                    }
                    else
                    {
                        // получаем ответ
                        byte[] httpResponse = ReadToEnd(myRerouting);
                        // передаем ответ обратно клиенту
                        if (httpResponse != null && httpResponse.Length > 0)
                        {
                            var writer = new BinaryWriter(stream);
                            writer.Write(httpResponse.Length);
                            writer.Write(httpResponse);
                        }
                    }
                }
            }
        }

        private static byte[] ReadToEnd(Socket mySocket)
        {
            var b = new byte[mySocket.ReceiveBufferSize];
            using (var m = new MemoryStream())
            {
                int len = 0;
                while (mySocket.Poll(1000000, SelectMode.SelectRead) && (len = mySocket.Receive(b, mySocket.ReceiveBufferSize, SocketFlags.None)) > 0)
                {
                    m.Write(b, 0, len);
                }
                return m.ToArray();
            }
        }
    }
}