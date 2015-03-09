using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Dictionary<string, Dictionary<string, string>> _replaces = new Dictionary<string, Dictionary<string, string>>();

        public RedirectModule(RedirectElement config)
        {
            var wrh = new WebRequestHandler();
            wrh.AutomaticDecompression = DecompressionMethods.GZip;
            _client = new HttpClient(wrh);
            _listener = new HttpListener();
            _listener.Prefixes.Add(config.FromUrl);
            _toUrl = new Uri(config.ToUrl);
            foreach (var result in config.Replace.Cast<ReplaceElement>())
            {
                if (!_replaces.ContainsKey(result.MediaType))
                {
                    _replaces.Add(result.MediaType, new Dictionary<string, string>());
                }
                _replaces[result.MediaType].Add(result.OldValue, result.NewValue);
            }
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
                if (_replaces.ContainsKey(res.Content.Headers.ContentType.MediaType))
                {
                    var temp = Encoding.UTF8.GetString(bytes);
                    temp = _replaces[res.Content.Headers.ContentType.MediaType]
                        .Aggregate(temp, (current, replace) => current.Replace(replace.Key, replace.Value));
                    bytes = Encoding.UTF8.GetBytes(temp);
                }
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} - {1}",e.Message, e.StackTrace);
            }
        }
    }
}