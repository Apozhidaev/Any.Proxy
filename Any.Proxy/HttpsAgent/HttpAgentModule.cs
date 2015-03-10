using System;
using System.Net.Sockets;
using Any.Proxy.Https;
using Any.Proxy.HttpsAgent.Configuration;

namespace Any.Proxy.HttpsAgent
{
    public class HttpsAgentModule : HttpsModuleBase
    {
        private readonly HttpsAgentConfig _config;

        public HttpsAgentModule(HttpsAgentConfig config)
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
                    var connection = new HttpsConnection(socket, (connectionId, host, port, isKeepAlive) => new HttpBridge(connectionId, _config.HttpBridge, socket, host, port, isKeepAlive), RemoveConnection);
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