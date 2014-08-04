using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Any.Proxy.PortMap
{

    public class PortMapListener : IDisposable
    {
        private IPEndPoint _fromPoint;
        private IPEndPoint _toPoint;
        private TcpListener _listener;

        public PortMapListener(IPEndPoint fromPoint, IPEndPoint toPoint)
        {
            _fromPoint = fromPoint;
            _toPoint = toPoint;
            _listener = new TcpListener(_fromPoint);
        }

        public async void Start()
        {
            _listener.Start();

            while (true)
            {
                await Accept(await _listener.AcceptSocketAsync());
            }
        }

        private async Task Accept(Socket socket)
        {
            await Task.Yield();
            using (socket)
            using (var bridge = new TcpBridge(socket, _toPoint))
            {
                await bridge.HandshakeAsync();
                bridge.Relay();
            }
        }

        public void Dispose()
        {
            _listener.Stop();
        }
    }

}
