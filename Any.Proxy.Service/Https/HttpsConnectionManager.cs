using System.Collections.Generic;

namespace Any.Proxy.Service.Https
{
    public class HttpsConnectionManager
    {
        private static readonly HttpsConnectionManager instance = new HttpsConnectionManager();

        public static HttpsConnectionManager Instance
        {
            get { return instance; }
        }
        protected HttpsConnectionManager() { }

        private readonly Dictionary<string, HttpsConnection> _connections = new Dictionary<string, HttpsConnection>(); 

        public HttpsConnection New(string host, int port)
        {
            var connection = new HttpsConnection(host, port);
            lock (_connections)
            {
                _connections.Add(connection.Id, connection);
            }
            return connection;
        }

        public HttpsConnection Get(string id)
        {
            lock (_connections)
            {
                if (_connections.ContainsKey(id))
                {
                    return _connections[id];
                }
                return null;
            }
        }

        public void Remove(HttpsConnection connection)
        {
            lock (_connections)
            {
                _connections.Remove(connection.Id);
            }
        }

    }
}