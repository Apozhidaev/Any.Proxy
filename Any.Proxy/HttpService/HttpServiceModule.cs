using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Any.Proxy.HttpService.Configuration;

namespace Any.Proxy.HttpService
{
    public class HttpServiceModule : IProxyModule
    {
        private readonly HttpListener _listener;

        public HttpServiceModule(HttpServiceElement config)
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
                        CreateResponse(context.Response, HttpStatusCode.BadRequest);
                        break;
                }
            }
            catch (Exception)
            {
                CreateResponse(context.Response, HttpStatusCode.BadRequest);
            }
        }

        private void HttpConnect(HttpListenerContext context)
        {
            string httpRequest = ReadAsString(context.Request);
            string[] sp = httpRequest.Split(':');
            RemoteConnection connection = RemoteConnection.Open(sp[0], sp[1], Int32.Parse(sp[2]));
            connection.HandshakeAsync().Wait();
            CreateResponse(context.Response, HttpStatusCode.OK);
        }

        private void HttpReceive(HttpListenerContext context)
        {
            string id = ReadAsString(context.Request);
            RemoteConnection connection = RemoteConnection.Find(id);
            if (connection == null)
            {
                CreateResponse(context.Response, HttpStatusCode.BadRequest);
                return;
            }
            int length = connection.RelayFromAsync().Result;
            if (length > 0)
            {
                CreateResponse(context.Response, HttpStatusCode.OK, Convert.ToBase64String(connection.Buffer, 0, length));
                return;
            }
            connection.Dispose();
            CreateResponse(context.Response, HttpStatusCode.BadRequest);
        }

        private void HttpSend(HttpListenerContext context)
        {
            string httpRequest = ReadAsString(context.Request);
            string[] sp = httpRequest.Split(':');
            string id = sp[0];
            byte[] httpResponse = Convert.FromBase64String(sp[1]);
            RemoteConnection connection = RemoteConnection.Find(id);
            if (connection == null)
            {
                CreateResponse(context.Response, HttpStatusCode.BadRequest);
                return;
            }
            int length = connection.RelayToAsync(httpResponse).Result;
            if (length > 0)
            {
                CreateResponse(context.Response, HttpStatusCode.OK);
                return;
            }
            connection.Dispose();
            CreateResponse(context.Response, HttpStatusCode.BadRequest);
        }

        private string ReadAsString(HttpListenerRequest context)
        {
            using (Stream stream = context.InputStream)
            {
                return new StreamReader(stream).ReadToEnd();
            }
        }

        private void CreateResponse(HttpListenerResponse context, HttpStatusCode status, string response)
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
            catch (Exception)
            {
                try
                {
                    context.Abort();
                }
                catch (Exception)
                {
                }
            }
        }

        private void CreateResponse(HttpListenerResponse context, HttpStatusCode status)
        {
            try
            {
                context.ContentLength64 = 0;
                context.StatusCode = (int) status;
                context.Close();
            }
            catch (Exception e1)
            {
                try
                {
                    context.Abort();
                }
                catch (Exception e2)
                {
                }
            }
        }
    }
}