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
        private readonly string _host;
        private readonly int _port;

        public PortMapModule(PortMapConfig config)
        {
            _host = config.ToHost;
            _port = config.ToPort;
            _listener = new TcpListener(new IPEndPoint(Proxy.GetIP(config.FromHost), config.FromPort));
        }

        public async void Start()
        {
            _listener.Start();

            while (true)
            {
                await Accept(await _listener.AcceptTcpClientAsync());
            }
        }

        public void Dispose()
        {
            _listener.Stop();
        }

        private async Task Accept(TcpClient client)
        {
            await Task.Yield();
            using (client)
            using (var bridge = new TcpBridge(client))
            {
                await bridge.HandshakeAsync(null, _host, _port);
                await bridge.RelayAsync();
            }
        }
    }
}