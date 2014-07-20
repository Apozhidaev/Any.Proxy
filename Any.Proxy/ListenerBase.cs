using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Any.Proxy
{

    ///<summary>Specifies the basic methods and properties of a <c>Listener</c> object. This is an abstract class and must be inherited.</summary>
    ///<remarks>The Listener class provides an abstract base class that represents a listening socket of the proxy server. Descendant classes further specify the protocol that is used between those two connections.</remarks>
    public abstract class ListenerBase : IDisposable
    {
        // private variables
        /// <summary>Holds the value of the Port property.</summary>
        private int _port;
        /// <summary>Holds the value of the Address property.</summary>
        private IPAddress _address;
        /// <summary>Holds the value of the ListenSocket property.</summary>
        private Socket _listenSocket;
        /// <summary>Holds the value of the Clients property.</summary>
        private readonly LinkedList<ClientBase> _clients = new LinkedList<ClientBase>();

        ///<summary>Initializes a new instance of the Listener class.</summary>
        ///<param name="port">The port to listen on.</param>
        ///<param name="address">The address to listen on. You can specify IPAddress.Any to listen on all installed network cards.</param>
        ///<remarks>For the security of your server, try to avoid to listen on every network card (IPAddress.Any). Listening on a local IP address is usually sufficient and much more secure.</remarks>
        protected ListenerBase(int port, IPAddress address)
        {
            IsDisposed = false;
            Port = port;
            Address = address;
        }
        ///<summary>Gets or sets the port number on which to listen on.</summary>
        ///<value>An integer defining the port number to listen on.</value>
        ///<seealso cref ="Address"/>
        ///<exception cref="ArgumentException">The specified value is less than or equal to zero.</exception>
        protected int Port
        {
            get
            {
                return _port;
            }
            set
            {
                if (value <= 0)
                    throw new ArgumentException();
                _port = value;
                Restart();
            }
        }
        ///<summary>Gets or sets the address on which to listen on.</summary>
        ///<value>An IPAddress instance defining the IP address to listen on.</value>
        ///<seealso cref ="Port"/>
        ///<exception cref="ArgumentNullException">The specified value is null.</exception>
        protected IPAddress Address
        {
            get
            {
                return _address;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                _address = value;
                Restart();
            }
        }
        ///<summary>Gets or sets the listening Socket.</summary>
        ///<value>An instance of the Socket class that's used to listen for incoming connections.</value>
        ///<exception cref="ArgumentNullException">The specified value is null.</exception>
        protected Socket ListenSocket
        {
            get
            {
                return _listenSocket;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                _listenSocket = value;
            }
        }
        ///<summary>Gets the list of connected clients.</summary>
        ///<value>An instance of the ArrayList class that's used to store all the connections.</value>
        protected LinkedList<ClientBase> Clients
        {
            get
            {
                return _clients;
            }
        }

        ///<summary>Gets a value indicating whether the Listener has been disposed or not.</summary>
        ///<value>An boolean that specifies whether the object has been disposed or not.</value>
        public bool IsDisposed { get; private set; }

        ///<summary>Starts listening on the selected IP address and port.</summary>
        ///<exception cref="SocketException">There was an error while creating the listening socket.</exception>
        public void Start()
        {
            try
            {
                ListenSocket = new Socket(Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ListenSocket.Bind(new IPEndPoint(Address, Port));
                ListenSocket.Listen(50);
                ListenSocket.BeginAccept(OnAccept, ListenSocket);
            }
            catch
            {
                ListenSocket = null;
                throw new SocketException();
            }
        }
        ///<summary>Restarts listening on the selected IP address and port.</summary>
        ///<remarks>This method is automatically called when the listening port or the listening IP address are changed.</remarks>
        ///<exception cref="SocketException">There was an error while creating the listening socket.</exception>
        protected void Restart()
        {
            //If we weren't listening, do nothing
            if (ListenSocket == null)
                return;
            ListenSocket.Close();
            Start();
        }
        ///<summary>Adds the specified Client to the client list.</summary>
        ///<remarks>A client will never be added twice to the list.</remarks>
        ///<param name="client">The client to add to the client list.</param>
        protected void AddClient(ClientBase client)
        {
            if (Clients.Contains(client))
                Clients.AddLast(client);
        }
        ///<summary>Removes the specified Client from the client list.</summary>
        ///<param name="client">The client to remove from the client list.</param>
        protected void RemoveClient(ClientBase client)
        {
            Clients.Remove(client);
        }
        ///<summary>Returns the number of clients in the client list.</summary>
        ///<returns>The number of connected clients.</returns>
        public int GetClientCount()
        {
            return Clients.Count;
        }

        ///<summary>Gets a value indicating whether the Listener is currently listening or not.</summary>
        ///<value>A boolean that indicates whether the Listener is currently listening or not.</value>
        public bool Listening
        {
            get
            {
                return ListenSocket != null;
            }
        }
        ///<summary>Disposes of the resources (other than memory) used by the Listener.</summary>
        ///<remarks>Stops listening and disposes <em>all</em> the client objects. Once disposed, this object should not be used anymore.</remarks>
        ///<seealso cref ="System.IDisposable"/>
        public void Dispose()
        {
            if (IsDisposed)
                return;
            while (Clients.Count > 0)
            {
                Clients.First.Value.Dispose();
            }
            try
            {
                ListenSocket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            if (ListenSocket != null)
                ListenSocket.Close();
            IsDisposed = true;
        }
        ///<summary>Finalizes the Listener.</summary>
        ///<remarks>The destructor calls the Dispose method.</remarks>
        ~ListenerBase()
        {
            Dispose();
        }
        ///<summary>Called when there's an incoming client connection waiting to be accepted.</summary>
        ///<param name="ar">The result of the asynchronous operation.</param>
        public abstract void OnAccept(IAsyncResult ar);
        ///<summary>Returns a string representation of this object.</summary>
        ///<returns>A string with information about this object.</returns>
        public override abstract string ToString();
        
    }

}