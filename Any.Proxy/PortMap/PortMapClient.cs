using System;
using System.Net;
using System.Net.Sockets;

namespace Any.Proxy.PortMap
{

    ///<summary>Relays data between a remote host and a local client.</summary>
    public sealed class PortMapClient : ClientBase
    {
        ///<summary>Initializes a new instance of the PortMapClient class.</summary>
        ///<param name="clientSocket">The <see cref ="Socket">Socket</see> connection between this proxy server and the local client.</param>
        ///<param name="destroyer">The callback method to be called when this Client object disconnects from the local client and the remote server.</param>
        ///<param name="mapTo">The IP EndPoint to send the incoming data to.</param>
        public PortMapClient(Socket clientSocket, Action<ClientBase> destroyer, IPEndPoint mapTo)
            : base(clientSocket, destroyer)
        {
            MapTo = mapTo;
        }
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
        ///<summary>Starts connecting to the remote host.</summary>
        override public void StartHandshake()
        {
            try
            {
                DestinationSocket = new Socket(MapTo.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                DestinationSocket.BeginConnect(MapTo, OnConnected, DestinationSocket);
            }
            catch
            {
                Dispose();
            }
        }
        ///<summary>Called when the socket is connected to the remote host.</summary>
        ///<remarks>When the socket is connected to the remote host, the PortMapClient begins relaying traffic between the host and the client, until one of them closes the connection.</remarks>
        ///<param name="ar">The result of the asynchronous operation.</param>
        private void OnConnected(IAsyncResult ar)
        {
            try
            {
                DestinationSocket.EndConnect(ar);
                StartRelay();
            }
            catch
            {
                Dispose();
            }
        }
        ///<summary>Returns text information about this PortMapClient object.</summary>
        ///<returns>A string representing this PortMapClient object.</returns>
        public override string ToString()
        {
            try
            {
                return "Forwarding port from " + ((IPEndPoint)ClientSocket.RemoteEndPoint).Address + " to " + MapTo;
            }
            catch
            {
                return "Incoming Port forward connection";
            }
        }
        // private variables
        /// <summary>Holds the value of the MapTo property.</summary>
        private IPEndPoint _mapTo;
    }

}
