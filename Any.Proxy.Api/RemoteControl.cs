using System;
using System.Configuration;
using Any.Proxy.Api.Configuration;
using Microsoft.Owin.Hosting;

namespace Any.Proxy.Api
{
    public class RemoteControl
    {
        private static readonly Proxy _proxy = new Proxy();
        public static Proxy Proxy
        {
            get { return _proxy; }
        }

        public static string Password { get; private set; }

        private readonly RemoteSection _config;
        private IDisposable _webApp;

        public RemoteControl()
        {
            _config = (RemoteSection)ConfigurationManager.GetSection("remote");
            Password = _config.Password;
        }

        public void Start()
        {
            _proxy.Start();
            var startOptions = new StartOptions();
            foreach (var url in _config.Prefixes.Split(','))
            {
                startOptions.Urls.Add(url);
            }
            _webApp = WebApp.Start<Startup>(startOptions);
        }

        public void Stop()
        {
            _webApp.Dispose();
            _proxy.Stop();
        }
    }
}