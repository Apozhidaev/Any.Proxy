using System;
using System.Net;
using System.Net.Sockets;

namespace Any.Proxy.Https
{
    public sealed class HttpsModule : ListenerBase, IProxyModule
    {
        public HttpsModule(int Port) : this(IPAddress.Any, Port) { }

        public HttpsModule(IPAddress Address, int Port) : base(Port, Address) { }

        public override void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket NewSocket = ListenSocket.EndAccept(ar);
                if (NewSocket != null)
                {
                    var NewClient = new HttpClient(NewSocket, RemoveClient);
                    AddClient(NewClient);
                    NewClient.StartHandshake();
                }
            }
            catch { }
            try
            {
                //Restart Listening
                ListenSocket.BeginAccept(OnAccept, ListenSocket);
            }
            catch
            {
                Dispose();
            }
        }

        public override string ToString()
        {
            return "Http service on " + Address + ":" + Port;
        }
    }

}
