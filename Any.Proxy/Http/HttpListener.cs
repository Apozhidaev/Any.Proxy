using System;
using System.Net;
using System.Net.Sockets;

namespace Any.Proxy.Http
{
    public sealed class HttpListener : ListenerBase
    {
        public HttpListener(int Port) : this(IPAddress.Any, Port) { }

        public HttpListener(IPAddress Address, int Port) : base(Port, Address) { }

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
