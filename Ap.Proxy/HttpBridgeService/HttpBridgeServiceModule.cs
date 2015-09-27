using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ap.Logs;
using Ap.Proxy.HttpBridgeService.Configuration;
using Ap.Proxy.Loggers;

namespace Ap.Proxy.HttpBridgeService
{
    public class HttpBridgeServiceModule : IProxyModule
    {
        private readonly HttpListener _listener;
        private Timer _timer;

        public HttpBridgeServiceModule(HttpBridgeServiceConfig config)
        {
            _listener = new HttpListener();
            var prefixes = config.Prefixes.Split(',');
            foreach (var prefix in prefixes)
            {
                _listener.Prefixes.Add(prefix);
            }    
        }

        public async void Start()
        {
            _listener.Start();
            _timer = new Timer(_ => Check());
            _timer.Change(1000, 1000);
            while (true)
            {
                try
                {
                    ProcessRequestAsync(await _listener.GetContextAsync());
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    break;
                }
            }
        }

        private void Check()
        {
            lock (_connections)
            {
                foreach (var connection in _connections.Where(connection => connection.Value.Expired).ToList())
                {
                    connection.Value.Dispose();
                    _connections.Remove(connection.Key);
                }
            }
#if DEBUG
            Console.WriteLine($"r_{_connections.Count}");
#endif
        }

        public void Dispose()
        {
            _listener.Stop();
            _timer?.Dispose();
        }

        public void Stop()
        {
            _listener.Stop();
        }

        private async void ProcessRequestAsync(HttpListenerContext context)
        {
            await Task.Yield();
            try
            {
                switch (context.Request.QueryString["a"])
                {
                    case "c":
                        await HttpConnect(context);
                        break;
                    case "r":
                        await HttpReceive(context);
                        break;
                    case "s":
                        await HttpSend(context);
                        break;
                    case "p":
                        HttpPing(context);
                        break;
                    default:
                        CreateResponse(context.Response, HttpStatusCode.BadRequest, String.Empty);
                        break;
                }
            }
            catch (Exception)
            {
                CreateResponse(context.Response, HttpStatusCode.BadRequest, String.Empty);
            }
        }

        private async Task HttpConnect(HttpListenerContext context)
        {
            string httpRequest = ReadAsString(context.Request);
            string[] sp = httpRequest.Split(':');
            var connectionId = sp[0];
            Log.Out.BeginInfo(connectionId, context.Request.Headers.ToString(), "HttpConnect host: {0}, port: {1}", sp[1], sp[2]);
            RemoteConnection connection = Open(connectionId, sp[1], Int32.Parse(sp[2]));
            await connection.HandshakeAsync();
            CreateResponse(context.Response, HttpStatusCode.OK, connectionId);
            Log.Out.EndInfo(connectionId, "", "HttpConnect host: {0}, port: {1}", sp[1], sp[2]);
        }

        private async Task HttpReceive(HttpListenerContext context)
        {
            string connectionId = ReadAsString(context.Request);
            Log.Out.BeginInfo(connectionId, context.Request.Headers.ToString(), "HttpReceive");
            RemoteConnection connection = Find(connectionId);
            if (connection == null)
            {
                CreateResponse(context.Response, HttpStatusCode.BadRequest, connectionId);
                Log.Out.EndInfo(connectionId, "", "HttpReceive => connection == null");
                return;
            }
            var buffer = await connection.ReadAsync();
            if (buffer.Length > 0)
            {
                CreateResponse(context.Response, HttpStatusCode.OK, Convert.ToBase64String(buffer, 0, buffer.Length), connectionId);
                Log.Out.EndInfo(connectionId, "", "HttpReceive length == {0}", buffer.Length);
                return;
            }
            connection.Dispose();
            CreateResponse(context.Response, HttpStatusCode.BadRequest, connectionId);
            Log.Out.EndInfo(connectionId, "", "HttpReceive => length == 0");
        }

        private async Task HttpSend(HttpListenerContext context)
        {
            string httpRequest = ReadAsString(context.Request);
            string[] sp = httpRequest.Split(':');
            string connectionId = sp[0];
            Log.Out.BeginInfo(connectionId, context.Request.Headers.ToString(), "HttpSend");
            byte[] httpResponse = Convert.FromBase64String(sp[1]);
            RemoteConnection connection = Find(connectionId);
            if (connection == null)
            {
                CreateResponse(context.Response, HttpStatusCode.BadRequest, connectionId);
                Log.Out.EndInfo(connectionId, "", "HttpSend => connection == null");
                return;
            }
            await connection.WriteAsync(httpResponse);
            CreateResponse(context.Response, HttpStatusCode.OK, connectionId);
            Log.Out.EndInfo(connectionId, "", "HttpSend length == {0}", httpResponse.Length);
        }

        private void HttpPing(HttpListenerContext context)
        {
            string connectionId = ReadAsString(context.Request);
            var connection = Find(connectionId);
            CreateResponse(context.Response, connection != null ? HttpStatusCode.OK : HttpStatusCode.NotFound, connectionId);
        }

        private string ReadAsString(HttpListenerRequest context)
        {
            using (Stream stream = context.InputStream)
            {
                return new StreamReader(stream).ReadToEnd();
            }
        }

        private void CreateResponse(HttpListenerResponse context, HttpStatusCode status, string response, string connectionId)
        {
            try
            {
                byte[] responseData = Encoding.ASCII.GetBytes(response);
                context.ContentLength64 = responseData.Length;
                context.StatusCode = (int) status;
                using (Stream stream = context.OutputStream)
                {
                    stream.Write(responseData, 0, responseData.Length);
                }
                context.Close();
            }
            catch (Exception eClose)
            {
                Log.Out.Error(eClose, connectionId, "context.Close()");
                try
                {
                    context.Abort();
                }
                catch (Exception eAbort)
                {
                    Log.Out.Error(eAbort, connectionId, "context.Abort()");
                }
            }
        }

        private void CreateResponse(HttpListenerResponse context, HttpStatusCode status, string connectionId)
        {
            try
            {
                context.ContentLength64 = 0;
                context.StatusCode = (int) status;
                context.Close();
            }
            catch (Exception eClose)
            {
                Log.Out.Error(eClose, connectionId, "context.Close()");
                try
                {
                    context.Abort();
                }
                catch (Exception eAbort)
                {
                    Log.Out.Error(eAbort, connectionId, "context.Abort()");
                }
            }
        }


        private readonly Dictionary<string, RemoteConnection> _connections =
            new Dictionary<string, RemoteConnection>();

        private RemoteConnection Open(string connectionId, string host, int port)
        {
            var connection = new RemoteConnection(connectionId, host, port);
            lock (_connections)
            {
                _connections.Add(connection.Id, connection);
            }
            return connection;
        }

        private RemoteConnection Find(string id)
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
    }
}