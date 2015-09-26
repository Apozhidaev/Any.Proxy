using System.Net.Sockets;
using Ap.Proxy.Https.Configuration;

namespace Ap.Proxy.Https
{
    public class HttpsModule : HttpsModuleBase
    {
        public HttpsModule(HttpsConfig config)
            :base(config.Host,config.Port)
        {
        }

        protected override void OnAccept(TcpClient client)
        {
            var connection = new HttpsConnection(new TcpBridge(client), RemoveConnection);
            AddConnection(connection);
            connection.Open();
        }
    }
}