using System.Net.Sockets;
using Ap.Proxy.Https;
using Ap.Proxy.HttpsAgent.Configuration;

namespace Ap.Proxy.HttpsAgent
{
    public class HttpsAgentModule : HttpsModuleBase
    {
        private readonly HttpsAgentConfig _config;

        public HttpsAgentModule(HttpsAgentConfig config)
            : base(config.Host, config.Port)
        {
            _config = config;
        }

        protected override void OnAccept(TcpClient client)
        {
            var connection = new HttpsConnection(new HttpBridge(_config.HttpBridge, client), RemoveConnection);
            AddConnection(connection);
            connection.Open();
        }
    }
}