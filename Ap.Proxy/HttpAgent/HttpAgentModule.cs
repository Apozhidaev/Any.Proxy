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

        protected override void OnAccept(TcpClient client)
        {
            var connection = new Connection(new HttpBridge(_config.HttpBridge, client), RemoveConnection);
            AddConnection(connection);
            connection.Open();
        }
    }
}