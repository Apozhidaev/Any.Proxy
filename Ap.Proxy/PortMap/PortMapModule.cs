using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Ap.Proxy.PortMap.Configuration;

namespace Ap.Proxy.PortMap
{
    public class PortMapModule : IProxyModule
    {
        private readonly TcpListener _listener;
        private readonly IPEndPoint _toPoint;

        public PortMapModule(PortMapConfig config)
        {
            var fromPoint=new IPEndPoint(Proxy.GetIP(config.FromHost), config.FromPort);
            _toPoint = new IPEndPoint(Proxy.GetIP(config.ToHost), config.ToPort);
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
            using (var bridge = new TcpBridge(String.Format("pm_{0}", Guid.NewGuid()), socket, _toPoint))
            {
                await bridge.HandshakeAsync();
                await bridge.RelayAsync();
            }
        }
    }
}