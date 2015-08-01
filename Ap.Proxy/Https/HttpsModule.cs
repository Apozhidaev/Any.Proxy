using System;
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

        protected override void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket socket = _listenSocket.EndAccept(ar);
                if (socket != null)
                {
                    var connection = new HttpsConnection(socket, (connectionId, host, port, isKeepAlive) => new TcpBridge(connectionId, socket, host, port, isKeepAlive), RemoveConnection);
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