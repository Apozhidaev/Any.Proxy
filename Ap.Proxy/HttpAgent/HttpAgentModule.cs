using System;
using System.Net.Sockets;
using Ap.Proxy.Http;
using Ap.Proxy.HttpAgent.Configuration;

namespace Ap.Proxy.HttpAgent
{
    public class HttpAgentModule : HttpModuleBase
    {
        private readonly HttpAgentConfig _config;

        public HttpAgentModule(HttpAgentConfig config)
            : base(config.Host, config.Port)
        {
            _config = config;
        }

        protected override void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket socket = _listenSocket.EndAccept(ar);
                if (socket != null)
                {
                    var connection = new Connection(socket, (connectionId, host, port, isKeepAlive) => new HttpBridge(connectionId, _config.HttpBridge, socket, host, port, isKeepAlive), RemoveConnection);
                    AddConnection(connection);
                    connection.StartHandshake();
                }
            }
            catch
            {
            }
            try
            {
                _listenSocket.BeginAccept(OnAccept, _listenSocket);
            }
            catch
            {
                Dispose();
            }
        }
    }
}