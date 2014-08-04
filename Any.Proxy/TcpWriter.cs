using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Any.Proxy
{
    public class TcpWriter
    {
        private readonly Socket _socket;
        private readonly TaskCompletionSource<int> _tcsWriter = new TaskCompletionSource<int>();

        public TcpWriter(Socket socket)
        {
            _socket = socket;
        }

        #region RelayTo

        public Task WriteAsync(byte[] bytes)
        {
            _socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, OnClientSend, _socket);
            return _tcsWriter.Task;
        }

        private void OnClientSend(IAsyncResult ar)
        {
            try
            {
                _socket.EndReceive(ar);
            }
            catch (Exception e)
            {
                _tcsWriter.SetException(e);
            }
            _tcsWriter.SetResult(0);
        }

        #endregion 
    }
}