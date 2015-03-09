using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Any.Proxy.Redirect.Configuration;

namespace Any.Proxy.Redirect
{
    public class RedirectModule : IProxyModule
    {
        private readonly Uri _toUrl;
        private readonly HttpClient _client;
        private readonly HttpListener _listener;

        public RedirectModule(RedirectElement config)
        {
            _client = new HttpClient();
            _listener = new HttpListener();
            _listener.Prefixes.Add(config.FromUrl);
            _toUrl = new Uri(config.ToUrl);
        }

        public void Dispose()
        {
            _listener.Stop();
        }

        public void Start()
        {
            _listener.Start();
            Listen();
        }

        private async void Listen()
        {

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

        private async void ProcessRequestAsync(HttpListenerContext context)
        {
            await Task.Yield();
            try
            {
                var url = new UriBuilder(context.Request.Url)
                {
                    Scheme = _toUrl.Scheme,
                    Host = _toUrl.Host,
                    Port = _toUrl.Port
                };
                var res = await _client.GetAsync(url.Uri);
                context.Response.StatusCode = (int)res.StatusCode;
                foreach (var httpResponseHeader in res.Content.Headers)
                {
                    foreach (var value in httpResponseHeader.Value)
                    {
                        context.Response.AddHeader(httpResponseHeader.Key, value);
                    }
                }
                byte[] bytes = await res.Content.ReadAsByteArrayAsync();
                //if (res.Content.Headers.ContentType.MediaType == "text/html")
                //{
                //    var temp = Encoding.UTF8.GetString(bytes);
                //    temp = temp.Replace("habrastorage.org", "hashabc.com:50000").Replace("habrahabr.ru", "hashabc.com");
                //    bytes = Encoding.UTF8.GetBytes(temp);
                //}
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}