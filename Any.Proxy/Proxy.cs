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
using Any.Proxy.HttpBridgeService;
using Any.Proxy.HttpBridgeService.Configuration;
using Any.Proxy.Https;
using Any.Proxy.Https.Configuration;
using Any.Proxy.HttpsAgent;
using Any.Proxy.HttpsAgent.Configuration;
using Any.Proxy.Loggers;
using Any.Proxy.PortMap;
using Any.Proxy.PortMap.Configuration;
using Any.Proxy.Redirect;
using Any.Proxy.Redirect.Configuration;

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
            foreach (var config in configuration.Https.OfType<HttpsElement>())
            {
                _listeners.Add(String.Format("Https-{0}", config.Name), new HttpsModule(config));
            }
            foreach (var config in configuration.HttpAgent.OfType<HttpAgentElement>())
            {
                _listeners.Add(String.Format("HttpAgent-{0}", config.Name), new HttpAgentModule(config));
            }
            foreach (var config in configuration.HttpsAgent.OfType<HttpsAgentElement>())
            {
                _listeners.Add(String.Format("HttpsAgent-{0}", config.Name), new HttpsAgentModule(config));
            }
            foreach (var config in configuration.HttpBridgeService.OfType<HttpBridgeServiceElement>())
            {
                _listeners.Add(String.Format("HttpBridgeService-{0}", config.Name), new HttpBridgeServiceModule(config));
            }
            foreach (var config in configuration.Redirect.OfType<RedirectElement>())
            {
                _listeners.Add(String.Format("Redirect-{0}", config.Name), new RedirectModule(config));
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
            _listeners.Clear();
        }

        public static IPAddress GetIP(string host)
        {
            return !String.IsNullOrEmpty(host) ? Dns.GetHostAddresses(host)[0] : IPAddress.Any;
        }
    }
}