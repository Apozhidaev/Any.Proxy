using System.IO;
using System.Net.Sockets;

namespace Any.Proxy.Com
{
    public static class SocketExtentions
    {
        public static byte[] ReadToEnd(this Socket socket, int wait)
        {
            var b = new byte[socket.ReceiveBufferSize];
            using (var m = new MemoryStream())
            {
                int len = 0;
                while (socket.Poll(wait, SelectMode.SelectRead) && (len = socket.Receive(b, b.Length, SocketFlags.None)) > 0)
                {
                    m.Write(b, 0, len);
                }
                return m.ToArray();
            }
        }
    }
}