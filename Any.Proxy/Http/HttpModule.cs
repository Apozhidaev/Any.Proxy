using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Any.Proxy.Http.Configuration;

namespace Any.Proxy.Http
{
    public class HttpModule : HttpModuleBase
    {
        public HttpModule(HttpElement config)
            :base(config.Host,config.Port)
        {
        }

        protected override void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket NewSocket = _listenSocket.EndAccept(ar);
                if (NewSocket != null)
                {
                    var NewClient = new Connection(NewSocket,
                        (connectionId, host, port, isKeepAlive) => new TcpBridge(connectionId, NewSocket, host, port, isKeepAlive), RemoveConnection);
                    AddConnection(NewClient);
                    NewClient.StartHandshake();
                }
            }
            catch
            {
            }
            try
            {
                //Restart Listening
                _listenSocket.BeginAccept(OnAccept, _listenSocket);
            }
            catch
            {
                Dispose();
            }
        }
    }
}