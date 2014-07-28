using System;
using System.Net;
using System.Net.Sockets;

namespace Any.Proxy
{
    public abstract class ClientBase : IDisposable
    {
        private readonly Action<ClientBase> _destroyer;
        private Socket _clientSocket;
        private Socket _destinationSocket;
        private readonly byte[] _buffer = new byte[40960];
        private readonly byte[] _remoteBuffer = new byte[10240];

        protected ClientBase(Socket clientSocket, Action<ClientBase> destroyer)
        {
            ClientSocket = clientSocket;
            _destroyer = destroyer;
        }
        protected ClientBase()
        {
            ClientSocket = null;
            _destroyer = null;
        }

        internal Socket ClientSocket
        {
            get
            {
                return _clientSocket;
            }
            set
            {
                if (_clientSocket != null)
                    _clientSocket.Close();
                _clientSocket = value;
            }
        }

        internal Socket DestinationSocket
        {
            get
            {
                return _destinationSocket;
            }
            set
            {
                if (_destinationSocket != null)
                    _destinationSocket.Close();
                _destinationSocket = value;
            }
        }

        protected byte[] Buffer
        {
            get
            {
                return _buffer;
            }
        }

        protected byte[] RemoteBuffer
        {
            get
            {
                return _remoteBuffer;
            }
        }

        public void Dispose()
        {
            try
            {
                ClientSocket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            try
            {
                DestinationSocket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            //Close the sockets
            if (ClientSocket != null)
                ClientSocket.Close();
            if (DestinationSocket != null)
                DestinationSocket.Close();
            //Clean up
            ClientSocket = null;
            DestinationSocket = null;
            if (_destroyer != null)
                _destroyer(this);
        }

        public override string ToString()
        {
            try
            {
                return "Incoming connection from " + ((IPEndPoint)DestinationSocket.RemoteEndPoint).Address;
            }
            catch
            {
                return "Client connection";
            }
        }
        ///<summary>Starts relaying data between the remote host and the local client.</summary>
        ///<remarks>This method should only be called after all protocol specific communication has been finished.</remarks>
        public void StartRelay()
        {
            try
            {
                ClientSocket.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, OnClientReceive, ClientSocket);
                DestinationSocket.BeginReceive(RemoteBuffer, 0, RemoteBuffer.Length, SocketFlags.None, OnRemoteReceive, DestinationSocket);
            }
            catch
            {
                Dispose();
            }
        }
        ///<summary>Called when we have received data from the local client.<br>Incoming data will immediately be forwarded to the remote host.</br></summary>
        ///<param name="ar">The result of the asynchronous operation.</param>
        protected void OnClientReceive(IAsyncResult ar)
        {
            try
            {
                int Ret = ClientSocket.EndReceive(ar);
                if (Ret <= 0)
                {
                    Dispose();
                    return;
                }
                DestinationSocket.BeginSend(Buffer, 0, Ret, SocketFlags.None, OnRemoteSent, DestinationSocket);
            }
            catch
            {
                Dispose();
            }
        }
        ///<summary>Called when we have sent data to the remote host.<br>When all the data has been sent, we will start receiving again from the local client.</br></summary>
        ///<param name="ar">The result of the asynchronous operation.</param>
        protected void OnRemoteSent(IAsyncResult ar)
        {
            try
            {
                int Ret = DestinationSocket.EndSend(ar);
                if (Ret > 0)
                {
                    ClientSocket.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, OnClientReceive, ClientSocket);
                    return;
                }
            }
            catch { }
            Dispose();
        }
        ///<summary>Called when we have received data from the remote host.<br>Incoming data will immediately be forwarded to the local client.</br></summary>
        ///<param name="ar">The result of the asynchronous operation.</param>
        protected void OnRemoteReceive(IAsyncResult ar)
        {
            try
            {
                int Ret = DestinationSocket.EndReceive(ar);
                if (Ret <= 0)
                {
                    Dispose();
                    return;
                }
                ClientSocket.BeginSend(RemoteBuffer, 0, Ret, SocketFlags.None, OnClientSent, ClientSocket);
            }
            catch
            {
                Dispose();
            }
        }
        ///<summary>Called when we have sent data to the local client.<br>When all the data has been sent, we will start receiving again from the remote host.</br></summary>
        ///<param name="ar">The result of the asynchronous operation.</param>
        protected void OnClientSent(IAsyncResult ar)
        {
            try
            {
                int Ret = ClientSocket.EndSend(ar);
                if (Ret > 0)
                {
                    DestinationSocket.BeginReceive(RemoteBuffer, 0, RemoteBuffer.Length, SocketFlags.None, OnRemoteReceive, DestinationSocket);
                    return;
                }
            }
            catch { }
            Dispose();
        }
        ///<summary>Starts communication with the local client.</summary>
        public abstract void StartHandshake();
    }

}