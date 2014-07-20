using System;
using System.Net;
using System.Net.Sockets;

namespace Any.Proxy.PortMap
{

    ///<summary>Listens on a specific port on the proxy server and forwards all incoming data to a specific port on another server.</summary>
    public sealed class PortMapListener : ListenerBase
    {
        private IPEndPoint _mapTo;
        ///<summary>Initializes a new instance of the PortMapListener class.</summary>
        ///<param name="port">The port to listen on.</param>
        ///<param name="mapToIP">The address to forward to.</param>
        ///<remarks>The object will listen on all network addresses on the computer.</remarks>
        ///<exception cref="ArgumentException"><paramref name="port">Port</paramref> is not positive.</exception>
        ///<exception cref="ArgumentNullException"><paramref name="mapToIP">MapToIP</paramref> is null.</exception>
        public PortMapListener(int port, IPEndPoint mapToIP) : this(IPAddress.Any, port, mapToIP) { }
        ///<summary>Initializes a new instance of the PortMapListener class.</summary>
        ///<param name="port">The port to listen on.</param>
        ///<param name="address">The network address to listen on.</param>
        ///<param name="mapToIP">The address to forward to.</param>
        ///<remarks>For security reasons, <paramref name="address">Address</paramref> should not be IPAddress.Any.</remarks>
        ///<exception cref="ArgumentNullException">Address or <paramref name="mapToIP">MapToIP</paramref> is null.</exception>
        ///<exception cref="ArgumentException">Port is not positive.</exception>
        public PortMapListener(IPAddress address, int port, IPEndPoint mapToIP)
            : base(port, address)
        {
            MapTo = mapToIP;
        }
        ///<summary>Initializes a new instance of the PortMapListener class.</summary>
        ///<param name="port">The port to listen on.</param>
        ///<param name="address">The network address to listen on.</param>
        ///<param name="mapToPort">The port to forward to.</param>
        ///<param name="mapToAddress">The IP address to forward to.</param>
        ///<remarks>For security reasons, Address should not be IPAddress.Any.</remarks>
        ///<exception cref="ArgumentNullException">Address or MapToAddress is null.</exception>
        ///<exception cref="ArgumentException">Port or MapToPort is invalid.</exception>
        public PortMapListener(IPAddress address, int port, IPAddress mapToAddress, int mapToPort) : this(address, port, new IPEndPoint(mapToAddress, mapToPort)) { }
        ///<summary>Gets or sets the IP EndPoint to map all incoming traffic to.</summary>
        ///<value>An IPEndPoint that holds the IP address and port to use when redirecting incoming traffic.</value>
        ///<exception cref="ArgumentNullException">The specified value is null.</exception>
        ///<returns>An IP EndPoint specifying the host and port to map all incoming traffic to.</returns>
        private IPEndPoint MapTo
        {
            get
            {
                return _mapTo;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                _mapTo = value;
            }
        }
        ///<summary>Called when there's an incoming client connection waiting to be accepted.</summary>
        ///<param name="ar">The result of the asynchronous operation.</param>
        public override void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket NewSocket = ListenSocket.EndAccept(ar);
                if (NewSocket != null)
                {
                    var NewClient = new PortMapClient(NewSocket, RemoveClient, MapTo);
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
        ///<summary>Returns a string representation of this object.</summary>
        ///<returns>A string with information about this object.</returns>
        public override string ToString()
        {
            return "PortMap service on " + Address + ":" + Port;
        }
        
    }

}
