using System;
using System.Configuration;
using System.Diagnostics;
using Ap.Proxy.Api.Configuration;
using Microsoft.Owin.Hosting;

namespace Ap.Proxy.Api
{
    public class App
    {
        private static readonly Proxy _proxy = new Proxy();
        public static Proxy Proxy => _proxy;

        public static string Password { get; private set; }

        private readonly RemoteSection _config;
        private IDisposable _webApp;

        public App()
        {
            _config = (RemoteSection)ConfigurationManager.GetSection("remote");
            Password = _config.Password;
        }

        public void Start()
        {
            _proxy.Start();
            var startOptions = new StartOptions();
            var urls = _config.Prefixes.Split(',');
            foreach (var url in urls)
            {
                startOptions.Urls.Add(url);
            }
            _webApp = WebApp.Start<Startup>(startOptions);

#if DEBUG
            Process.Start(@"C:\Program Files\Internet Explorer\iexplore.exe", urls[0]);
#endif
        }

        public void Stop()
        {
            _webApp.Dispose();
            _proxy.Stop();
        }
    }
}