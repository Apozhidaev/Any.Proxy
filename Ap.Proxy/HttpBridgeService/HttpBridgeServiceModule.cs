using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Ap.Logs;
using Ap.Proxy.HttpBridgeService.Configuration;
using Ap.Proxy.Loggers;

namespace Ap.Proxy.HttpBridgeService
{
    public class HttpBridgeServiceModule : IProxyModule
    {
        private readonly HttpListener _listener;

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

        public void Dispose()
        {
            _listener.Stop();
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
                    case "hc":
                        HttpConnect(context);
                        break;
                    case "hr":
                        HttpReceive(context);
                        break;
                    case "hs":
                        HttpSend(context);
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

        private void HttpConnect(HttpListenerContext context)
        {
            string httpRequest = ReadAsString(context.Request);
            string[] sp = httpRequest.Split(':');
            var connectionId = sp[0];
            Log.Out.BeginInfo(connectionId, context.Request.Headers.ToString(), "HttpConnect host: {0}, port: {1}", sp[1], sp[2]);
            RemoteConnection connection = RemoteConnection.Open(connectionId, sp[1], Int32.Parse(sp[2]));
            connection.HandshakeAsync().Wait();
            CreateResponse(context.Response, HttpStatusCode.OK, connectionId);
            Log.Out.EndInfo(connectionId, "", "HttpConnect host: {0}, port: {1}", sp[1], sp[2]);
        }

        private void HttpReceive(HttpListenerContext context)
        {
            string connectionId = ReadAsString(context.Request);
            Log.Out.BeginInfo(connectionId, context.Request.Headers.ToString(), "HttpReceive");
            RemoteConnection connection = RemoteConnection.Find(connectionId);
            if (connection == null)
            {
                CreateResponse(context.Response, HttpStatusCode.BadRequest, connectionId);
                Log.Out.EndInfo(connectionId, "", "HttpReceive => connection == null");
                return;
            }
            int length = connection.RelayFromAsync().Result;
            if (length > 0)
            {
                CreateResponse(context.Response, HttpStatusCode.OK, Convert.ToBase64String(connection.Buffer, 0, length), connectionId);
                Log.Out.EndInfo(connectionId, "", "HttpReceive length == {0}", length);
                return;
            }
            connection.Dispose();
            CreateResponse(context.Response, HttpStatusCode.BadRequest, connectionId);
            Log.Out.EndInfo(connectionId, "", "HttpReceive => length == 0");
        }

        private void HttpSend(HttpListenerContext context)
        {
            string httpRequest = ReadAsString(context.Request);
            string[] sp = httpRequest.Split(':');
            string connectionId = sp[0];
            Log.Out.BeginInfo(connectionId, context.Request.Headers.ToString(), "HttpSend");
            byte[] httpResponse = Convert.FromBase64String(sp[1]);
            RemoteConnection connection = RemoteConnection.Find(connectionId);
            if (connection == null)
            {
                CreateResponse(context.Response, HttpStatusCode.BadRequest, connectionId);
                Log.Out.EndInfo(connectionId, "", "HttpSend => connection == null");
                return;
            }
            int length = connection.RelayToAsync(httpResponse).Result;
            if (length > 0)
            {
                CreateResponse(context.Response, HttpStatusCode.OK, connectionId);
                Log.Out.EndInfo(connectionId, "", "HttpSend length == {0}", length);
                return;
            }
            connection.Dispose();
            CreateResponse(context.Response, HttpStatusCode.BadRequest, connectionId);
            Log.Out.EndInfo(connectionId, "", "HttpSend => length == 0");
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
    }
}