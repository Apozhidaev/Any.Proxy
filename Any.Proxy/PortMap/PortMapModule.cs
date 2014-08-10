using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Any.Proxy.PortMap
{
    public class PortMapModule : IProxyModule
    {
        private readonly TcpListener _listener;
        private readonly IPEndPoint _toPoint;

        public PortMapModule(IPEndPoint fromPoint, IPEndPoint toPoint)
        {
            _toPoint = toPoint;
            _listener = new TcpListener(fromPoint);
        }

        public async void Start()
        {
            _listener.Start();

            while (true)
            {
                await Accept(await _listener.AcceptSocketAsync());
            }
        }

        public void Dispose()
        {
            _listener.Stop();
        }

        private async Task Accept(Socket socket)
        {
            await Task.Yield();
            using (socket)
            using (var bridge = new TcpBridge(socket, _toPoint))
            {
                await bridge.HandshakeAsync();
                await bridge.RelayAsync();
            }
        }
    }
}