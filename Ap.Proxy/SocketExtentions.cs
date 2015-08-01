using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Ap.Proxy
{
    public static class SocketExtentions
    {
        public static byte[] ReadToEnd(this Socket socket, int wait)
        {
            var b = new byte[socket.ReceiveBufferSize];
            using (var m = new MemoryStream())
            {
                int len = 0;
                while (socket.Poll(wait, SelectMode.SelectRead) &&
                       (len = socket.Receive(b, b.Length, SocketFlags.None)) > 0)
                {
                    m.Write(b, 0, len);
                }
                return m.ToArray();
            }
        }

        public static Task WriteAsync(this Socket socket, byte[] bytes)
        {
            var tcsWriter = new TaskCompletionSource<int>();
            socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, ar =>
            {
                try
                {
                    socket.EndSend(ar);
                    tcsWriter.SetResult(0);
                }
                catch (Exception e)
                {
                    tcsWriter.SetException(e);
                }
            }, null);
            return tcsWriter.Task;
        }
    }
}