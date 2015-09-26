using System.Net.Sockets;
using Ap.Proxy.Http.Configuration;

namespace Ap.Proxy.Http
{
    public class HttpModule : HttpModuleBase
    {
        public HttpModule(HttpConfig config)
            :base(config.Host,config.Port)
        {
        }

        protected override void OnAccept(TcpClient client)
        {
            var connection = new Connection(new TcpBridge(client), RemoveConnection);
            AddConnection(connection);
            connection.Open();
        }
    }
}