using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Any.Proxy.PortMaps
{

    public class PortMapModule : IProxyModule
    {
        private readonly IPEndPoint _toPoint;
        private readonly TcpListener _listener;

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
