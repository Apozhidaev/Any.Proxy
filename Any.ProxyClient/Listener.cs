using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Any.ProxyClient
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
                    await Accept(await _listener.AcceptSocketAsync());
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
                        byte[] httpRequest = ReadToEnd(myClient);


                        using (var tcpClient = new TcpClient("lifehttp.com", 50000))
                        using (var stream = tcpClient.GetStream())
                        {
                            var writer = new BinaryWriter(stream);
                            writer.Write(httpRequest.Length);
                            writer.Write(httpRequest);

                            var reader = new BinaryReader(stream);
                            var lenght = reader.ReadInt32();

                            byte[] httpResponse = reader.ReadBytes(lenght);
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