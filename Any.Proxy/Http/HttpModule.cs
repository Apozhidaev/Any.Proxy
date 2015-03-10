using System;
using System.Net.Sockets;
using Any.Proxy.Http.Configuration;

namespace Any.Proxy.Http
{
    public class HttpModule : HttpModuleBase
    {
        public HttpModule(HttpConfig config)
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
                    var connection = new Connection(socket, (connectionId, host, port, isKeepAlive) => new TcpBridge(connectionId, socket, host, port, isKeepAlive), RemoveConnection);
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