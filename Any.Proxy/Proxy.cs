using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using Any.Logs;
using Any.Proxy.Configuration;
using Any.Proxy.Http;
using Any.Proxy.Http.Configuration;
using Any.Proxy.HttpAgent;
using Any.Proxy.HttpAgent.Configuration;
using Any.Proxy.HttpService;
using Any.Proxy.HttpService.Configuration;
using Any.Proxy.Loggers;
using Any.Proxy.PortMap;
using Any.Proxy.PortMap.Configuration;

namespace Any.Proxy
{
    public class Proxy
    {
        private readonly Dictionary<string, IProxyModule> _listeners = new Dictionary<string, IProxyModule>();

        public void Start()
        {
            Log.Initialize(new EventLogger());
            var configuration = (ProxySection) ConfigurationManager.GetSection("proxy");

            foreach (var config in configuration.PortMap.OfType<PortMapElement>())
            {
                _listeners.Add(String.Format("PortMap-{0}", config.Name), new PortMapModule(config));
            }
            foreach (var config in configuration.Http.OfType<HttpElement>())
            {
                _listeners.Add(String.Format("Http-{0}", config.Name), new HttpModule(config));
            }
            foreach (var config in configuration.HttpAgent.OfType<HttpAgentElement>())
            {
                _listeners.Add(String.Format("HttpAgent-{0}", config.Name), new HttpAgentModule(config));
            }
            foreach (var config in configuration.HttpService.OfType<HttpServiceElement>())
            {
                _listeners.Add(String.Format("HttpService-{0}", config.Name), new HttpServiceModule(config));
            }

            foreach (var listener in _listeners)
            {
                listener.Value.Start();
            }
        }

        public void Stop()
        {
            foreach (var listener in _listeners)
            {
                listener.Value.Dispose();
            }
        }

        public static IPAddress GetIP(string host)
        {
            return !String.IsNullOrEmpty(host) ? Dns.GetHostAddresses(host)[0] : IPAddress.Any;
        }
    }
}