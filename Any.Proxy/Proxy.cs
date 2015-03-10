using System;
using System.Collections.Generic;
using System.Net;
using Any.Logs;
using Any.Proxy.Configuration;
using Any.Proxy.Http;
using Any.Proxy.HttpAgent;
using Any.Proxy.HttpBridgeService;
using Any.Proxy.Https;
using Any.Proxy.HttpsAgent;
using Any.Proxy.Loggers;
using Any.Proxy.PortMap;
using Any.Proxy.Redirect;

namespace Any.Proxy
{
    public class Proxy
    {
        private readonly Dictionary<string, IProxyModule> _listeners = new Dictionary<string, IProxyModule>();

        public void Start()
        {
            Log.Initialize(new EventLogger());
            var configuration = ProxyConfig.Load();
            if (configuration.PortMap != null)
            {
                foreach (var config in configuration.PortMap)
                {
                    _listeners.Add(String.Format("PortMap-{0}", config.Name), new PortMapModule(config));
                }
            }
            if (configuration.Http != null)
            {
                foreach (var config in configuration.Http)
                {
                    _listeners.Add(String.Format("Http-{0}", config.Name), new HttpModule(config));
                }
            }
            if (configuration.Https != null)
            {
                foreach (var config in configuration.Https)
                {
                    _listeners.Add(String.Format("Https-{0}", config.Name), new HttpsModule(config));
                }
            }
            if (configuration.HttpAgent != null)
            {
                foreach (var config in configuration.HttpAgent)
                {
                    _listeners.Add(String.Format("HttpAgent-{0}", config.Name), new HttpAgentModule(config));
                }
            }
            if (configuration.HttpsAgent != null)
            {
                foreach (var config in configuration.HttpsAgent)
                {
                    _listeners.Add(String.Format("HttpsAgent-{0}", config.Name), new HttpsAgentModule(config));
                }
            }
            if (configuration.HttpBridgeService != null)
            {
                foreach (var config in configuration.HttpBridgeService)
                {
                    _listeners.Add(String.Format("HttpBridgeService-{0}", config.Name),
                        new HttpBridgeServiceModule(config));
                }
            }
            if (configuration.Redirect != null)
            {
                foreach (var config in configuration.Redirect)
                {
                    _listeners.Add(String.Format("Redirect-{0}", config.Name), new RedirectModule(config));
                }
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