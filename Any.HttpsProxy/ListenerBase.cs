using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Any.Proxy
{
    public abstract class ListenerBase : IDisposable
    {
        private int _port;
        private IPAddress _address;
        private Socket _listenSocket;
        private readonly LinkedList<ClientBase> _clients = new LinkedList<ClientBase>();

        protected ListenerBase(int port, IPAddress address)
        {
            IsDisposed = false;
            Port = port;
            Address = address;
        }

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

        protected LinkedList<ClientBase> Clients
        {
            get
            {
                return _clients;
            }
        }

        public bool IsDisposed { get; private set; }

        public void Start()
        {
            try
            {
                ListenSocket = new Socket(Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ListenSocket.Bind(new IPEndPoint(Address, Port));
                ListenSocket.Listen(500);
                ListenSocket.BeginAccept(OnAccept, ListenSocket);
            }
            catch
            {
                ListenSocket = null;
                throw new SocketException();
            }
        }

        protected void Restart()
        {
            //If we weren't listening, do nothing
            if (ListenSocket == null)
                return;
            ListenSocket.Close();
            Start();
        }

        protected void AddClient(ClientBase client)
        {
            if (!Clients.Contains(client))
            {
                Clients.AddLast(client);
            }
        }

        protected void RemoveClient(ClientBase client)
        {
            Clients.Remove(client);
        }

        public int GetClientCount()
        {
            return Clients.Count;
        }

        public bool Listening
        {
            get
            {
                return ListenSocket != null;
            }
        }

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

        ~ListenerBase()
        {
            Dispose();
        }

        public abstract void OnAccept(IAsyncResult ar);

        public override abstract string ToString();
        
    }

}