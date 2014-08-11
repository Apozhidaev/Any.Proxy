using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Any.Proxy.Http;
using Any.Proxy.HttpAgent.Configuration;

namespace Any.Proxy.HttpAgent
{
    public class HttpAgentModule : HttpModuleBase
    {
        private readonly HttpAgentElement _config;

        public HttpAgentModule(HttpAgentElement config)
            : base(config.Host, config.Port)
        {
            _config = config;
        }

        protected override void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket NewSocket = _listenSocket.EndAccept(ar);
                if (NewSocket != null)
                {
                    var NewClient = new HttpConnection(NewSocket, 
                        (host, port, isKeepAlive) => new HttpBridge(_config.HttpBridge, NewSocket, host, port, isKeepAlive), RemoveConnection);
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